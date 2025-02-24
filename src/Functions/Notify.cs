// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitHubIssueManager.Extensions;
using GitHubIssueManager.Models;
using GitHubIssueManager.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GitHubIssueManager.Functions;

/// <summary>
/// Azure function to receive webhook payloads from GitHub.
/// </summary>
/// <param name="options">The <see cref="GitHubOptions"/> loaded from local.settings.json.</param>
/// <param name="logger">The <see cref="ILogger"/> provided by dependency injection.</param>
public class Notify(IOptions<GitHubOptions> options, ILogger<Notify> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// The function invoked when a POST request comes to the /api/Notify endpoint.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <returns>A <see cref="WebhookMultiResponse"/> that contains a queue item to invoke connector ingestion and
    /// an HTTP result set to 202 if successful, 401 if the signature is invalid, or 400 if anything else is wrong.</returns>
    [Function(nameof(Notify))]
    public async Task<WebhookMultiResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        // Validate the request came from GitHub
        // and check the event type
        if (req.Headers.TryGetValue("X-Hub-Signature-256", out var hashValue) &&
            req.Headers.TryGetValue("X-GitHub-Event", out var eventValue))
        {
            var requestBody = await ExtractPayloadAsync(req);
            if (ValidateSignature(requestBody, hashValue.ToString()))
            {
                var eventName = eventValue.ToString();
                if (eventName.IsEqualIgnoringCase("issues") ||
                    eventName.IsEqualIgnoringCase("issue_comment"))
                {
                    var gitHubEvent = JsonSerializer.Deserialize<GitHubEvent>(requestBody);

                    logger.LogInformation(
                        "{action} event received for issue #{number} in {repo}",
                        gitHubEvent?.Action,
                        gitHubEvent?.Issue?.Number,
                        gitHubEvent?.Repository?.FullName);

                    // Return 202 to let GitHub know we've accepted the notification
                    // and queue an item in Azure storage queue to trigger the
                    // TODO function that will ingest the issue into our connector.
                    return new WebhookMultiResponse(new AcceptedResult(), new()
                    {
                        IssueNumber = gitHubEvent?.Issue?.Number ?? 0,
                        Owner = gitHubEvent?.Repository?.GetOwner(),
                        Repo = gitHubEvent?.Repository?.GetName(),
                    });
                }
                else
                {
                    logger.LogWarning("Unexpected event type {event}", eventName);
                }
            }
            else
            {
                return new WebhookMultiResponse(new UnauthorizedResult());
            }
        }

        return new WebhookMultiResponse(new BadRequestResult());
    }

    /// <summary>
    /// Extracts the request payload as a string and optionally logs it.
    /// </summary>
    /// <param name="req">The <see cref="HttpRequest"/> to extract the payload from.</param>
    /// <returns>The extracted payload.</returns>
    private async Task<string> ExtractPayloadAsync(HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        if (options.Value.LogWebhookPayloads)
        {
            logger.LogInformation("Headers ({count}):", req.Headers.Count);
            foreach (var header in req.Headers)
            {
                logger.LogInformation("{header}: {value}", header.Key, header.Value.ToString());
            }

            // Pretty print the JSON
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(requestBody);
            var jsonBody = JsonSerializer.Serialize(jsonElement, JsonOptions);
            logger.LogInformation("Body:");
            logger.LogInformation(jsonBody);
        }

        return requestBody;
    }

    /// <summary>
    /// Validates the HMAC hex digest signature sent by GitHub.
    /// This is how the receiver can verify that the request came from GitHub.
    /// </summary>
    /// <param name="payload">The POST payload as a string.</param>
    /// <param name="signature">The signature sent in the `X-Hub-Signature-256` header.</param>
    /// <returns>True if the signature is valid, false if not.</returns>
    private bool ValidateSignature(string payload, string signature)
    {
        // See https://docs.github.com/en/webhooks/using-webhooks/validating-webhook-deliveries
        // for details.
        var secret = options.Value.WebhookSecret;
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogError("Webhook secret was not loaded from settings");
            return false;
        }

        var payloadHash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes(payload));

        var builder = new StringBuilder("sha256=");
        for (int i = 0; i < payloadHash.Length; i++)
        {
            // Append each byte as hexadecimal
            builder.Append(payloadHash[i].ToString("x2"));
        }

        // The signature from the request must match the signature
        // we generate.
        return string.CompareOrdinal(signature, builder.ToString()) == 0;
    }
}

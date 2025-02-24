// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using GitHubIssueManager.Extensions;
using GitHubIssueManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ExternalConnectors;

namespace GitHubIssueManager.Functions;

/// <summary>
/// Azure function to initialize the Microsoft Graph connector and
/// do initial ingestion of issues.
/// </summary>
/// <param name="connectorService">The <see cref="GraphConnectorService"/> provided by dependency injection.</param>
/// <param name="issuesService">The <see cref="GitHubIssuesService"/> provided by dependency injection.</param>
/// <param name="logger">The <see cref="ILogger"/> provided by dependency injection.</param>
public class Initialize(
    GraphConnectorService connectorService,
    GitHubIssuesService issuesService,
    ILogger<Initialize> logger)
{
    /// <summary>
    /// The function invoked when a POST request comes to the /api/Initialize endpoint.
    /// </summary>
    /// <param name="req">The incoming HTTP request.</param>
    /// <param name="client">The durable task client.</param>
    /// <param name="operation">The operation to take.</param>
    /// <returns>HTTP 200 status.</returns>
    [Function(nameof(Initialize))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        [FromQuery] string operation = "create")
    {
        if (operation.IsEqualIgnoringCase("create"))
        {
            logger.LogInformation("Creating connection...");
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(InitializeNewConnection));
            logger.LogInformation("Started orchestration with ID: {instanceId}", instanceId);
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
        else if (operation.IsEqualIgnoringCase("delete"))
        {
            logger.LogInformation("Deleting connection...");
            await connectorService.DeleteConnectionAsync();
        }

        var response = HttpResponseData.CreateResponse(req);
        response.StatusCode = System.Net.HttpStatusCode.OK;
        return response;
    }

    /// <summary>
    /// A durable function that orchestrates the long-running process of creating
    /// a new connection and registering a schema.
    /// </summary>
    /// <param name="context">The orchestration context.</param>
    /// <returns>HTTP status code.</returns>
    [Function(nameof(InitializeNewConnection))]
    public async Task<IActionResult> InitializeNewConnection(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var replayLogger = context.CreateReplaySafeLogger(nameof(InitializeNewConnection));
        replayLogger.LogInformation("Creating a new connection and registering schema");

        try
        {
            // Get existing connection if it exists,
            // otherwise create a new one.
            var connection = await context.CallActivityAsync<ExternalConnection?>(nameof(EnsureConnection));
            if (connection == null)
            {
                replayLogger.LogWarning("Could not create or retrieve connection.");
                return new BadRequestResult();
            }

            // Check if schema exists, register it otherwise
            var pollUri = await context.CallActivityAsync<Uri?>(nameof(EnsureSchema));
            if (pollUri == null)
            {
                replayLogger.LogInformation("Schema already registered");
            }
            else
            {
                var registrationComplete = false;

                while (!registrationComplete)
                {
                    registrationComplete = await context.CallActivityAsync<bool>(nameof(PollSchemaOperation), pollUri);
                }

                replayLogger.LogInformation("Schema registration complete");
            }

            // Crawl issues from the repo
            await context.CallActivityAsync(nameof(CrawlIssues));
            return new OkResult();
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex);
        }
    }

    /// <summary>
    /// Ensures the connection exists.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>The connection.</returns>
    [Function(nameof(EnsureConnection))]
    public async Task<ExternalConnection?> EnsureConnection([ActivityTrigger] FunctionContext context)
    {
        var funcLogger = context.GetLogger(nameof(EnsureConnection));
        funcLogger.LogInformation("Ensuring connection...");
        try
        {
            return await connectorService.EnsureConnectionAsync();
        }
        catch (AdminConsentRequiredException ex)
        {
            funcLogger.LogWarning(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Ensures the schema is registered on the connection.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>If the schema was not previously registered, returns a URI to poll for registration status.</returns>
    [Function(nameof(EnsureSchema))]
    public async Task<Uri?> EnsureSchema([ActivityTrigger] FunctionContext context)
    {
        context.GetLogger(nameof(EnsureSchema)).LogInformation("Ensuring schema...");
        return await connectorService.EnsureSchemaAsync();
    }

    /// <summary>
    /// An activity to poll for schema registration status.
    /// </summary>
    /// <param name="pollUri">The polling URI provided by the schema registration API.</param>
    /// <param name="context">The function context.</param>
    /// <returns>True if the schema registration is complete, false if it is still in progress.</returns>
    [Function(nameof(PollSchemaOperation))]
    public async Task<bool> PollSchemaOperation(
        [ActivityTrigger] Uri pollUri,
        FunctionContext context)
    {
        var functionLogger = context.GetLogger(nameof(PollSchemaOperation));
        functionLogger.LogInformation("Waiting 60 seconds to check schema registration status");
        await Task.Delay(60000);
        return await connectorService.PollSchemaOperationAsync(pollUri);
    }

    /// <summary>
    /// Crawl all issues in the repository and ingest them into the connector.
    /// </summary>
    /// <param name="context">The function context.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    [Function(nameof(CrawlIssues))]
    public async Task CrawlIssues([ActivityTrigger] FunctionContext context)
    {
        var functionLogger = context.GetLogger(nameof(CrawlIssues));
        functionLogger.LogInformation("Beginning crawl of issues");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var issues = await issuesService.GetAllIssuesAsync();
        functionLogger.LogInformation("Found {count} issues in repository", issues?.Count);

        foreach (var issue in issues ?? [])
        {
            functionLogger.LogInformation("Ingesting issue #{number}", issue.Number);

            var externalItem = await issuesService.CreateExternalItemFromIssueAsync(issue);

            await connectorService.AddOrUpdateItemAsync(externalItem);
        }

        stopwatch.Stop();
        functionLogger.LogInformation("Crawl took {seconds} seconds", stopwatch.Elapsed.TotalSeconds);
    }
}

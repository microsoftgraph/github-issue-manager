// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Identity;
using GitHubIssueManager.Extensions;
using GitHubIssueManager.Options;
using GitHubIssueManager.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace GitHubIssueManager.Services;

/// <summary>
/// Service that handles interactions with Microsoft Graph to manage the connector.
/// </summary>
public class GraphConnectorService
{
    private readonly GraphOptions graphOptions;
    private readonly GitHubOptions gitHubOptions;
    private readonly ILogger<GraphConnectorService> logger;
    private readonly GraphServiceClient graphClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphConnectorService"/> class.
    /// </summary>
    /// <param name="graphOptions">The <see cref="GraphOptions"/> loaded from local.settings.json.</param>
    /// <param name="gitHubOptions">The <see cref="GitHubOptions"/> loaded from local.settings.json.</param>
    /// <param name="logger">The <see cref="ILogger"/> provided by dependency injection.</param>
    public GraphConnectorService(
        IOptions<GraphOptions> graphOptions,
        IOptions<GitHubOptions> gitHubOptions,
        ILogger<GraphConnectorService> logger)
    {
        this.graphOptions = graphOptions.Value;
        this.gitHubOptions = gitHubOptions.Value;
        this.logger = logger;
        graphClient = InitializeGraph();
    }

    /// <summary>
    /// Checks that the Microsoft Graph connector exists and creates it if it does not.
    /// </summary>
    /// <returns>The <see cref="ExternalConnection"/>.</returns>
    public async Task<ExternalConnection?> EnsureConnectionAsync()
    {
        // Check if the connection already exists
        try
        {
            var connection = await graphClient.External
                .Connections[graphOptions.ConnectorId]
                .GetAsync();
            logger.LogInformation("Connection exists");
            return connection;
        }
        catch (ODataError oDataError)
        {
            if (oDataError.Error?.Code?.IsEqualIgnoringCase("ItemNotFound") ?? false)
            {
                logger.LogInformation(
                    "Connection with id {connectionId} does not exist",
                    graphOptions.ConnectorId);
                return await CreateConnectionAsync();
            }

            if (oDataError.ResponseStatusCode == 401 ||
                oDataError.ResponseStatusCode == 403)
            {
                throw new AdminConsentRequiredException(graphOptions.ClientId, graphOptions.TenantId);
            }

            throw;
        }
    }

    /// <summary>
    /// Deletes the connection.
    /// </summary>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    public async Task DeleteConnectionAsync()
    {
        try
        {
            await graphClient.External
                .Connections[graphOptions.ConnectorId]
                .DeleteAsync();
        }
        catch (ODataError oDataError)
        {
            if (oDataError.Error?.Code?.IsEqualIgnoringCase("ItemNotFound") ?? false)
            {
                logger.LogInformation("Connection was not found");
            }

            if (oDataError.ResponseStatusCode == 401 ||
                oDataError.ResponseStatusCode == 403)
            {
                throw new AdminConsentRequiredException(graphOptions.ClientId, graphOptions.TenantId);
            }

            throw;
        }
    }

    /// <summary>
    /// Checks that a schema is registered on the Microsoft Graph connector
    /// and registers it if it is not.
    /// </summary>
    /// <returns>A <see cref="Uri"/> to use to poll for the registration status or null if the schema
    /// is already registered.</returns>
    public async Task<Uri?> EnsureSchemaAsync()
    {
        try
        {
            var schema = await graphClient.External
                .Connections[graphOptions.ConnectorId]
                .Schema
                .GetAsync();

            // Schema exists, return null
            return null;
        }
        catch (ODataError oDataError)
        {
            if (oDataError.Error?.Code?.IsEqualIgnoringCase("ItemNotFound") ?? false)
            {
                logger.LogInformation(
                    "Schema not registered on connection with id {connectionId}",
                    graphOptions.ConnectorId);
                return await RegisterSchemaAsync();
            }

            throw;
        }
    }

    /// <summary>
    /// Polls the URI provided by the API to check the status of the asynchronous schema registration.
    /// </summary>
    /// <param name="pollingUri">The <see cref="Uri"/> to poll.</param>
    /// <returns>True if schema registration is complete, false if it is still in progress.</returns>
    /// <exception cref="Exception">Thrown if schema registration fails or the request returns a non-success HTTP status.</exception>
    public async Task<bool> PollSchemaOperationAsync(Uri pollingUri)
    {
        var pollingRequest = new RequestInformation
        {
            URI = pollingUri,
            HttpMethod = Method.GET,
        };

        // By using the Graph client, the request will have the required
        // Authorization header
        var operation = await graphClient.RequestAdapter.SendAsync(
            pollingRequest,
            ConnectionOperation.CreateFromDiscriminatorValue);

        return operation?.Status == ConnectionOperationStatus.Completed;
    }

    /// <summary>
    /// Adds or updates an <see cref="ExternalItem"/>.
    /// </summary>
    /// <param name="externalItem">The item to add or update.</param>
    /// <returns>The updated item.</returns>
    public async Task<ExternalItem?> AddOrUpdateItemAsync(ExternalItem externalItem)
    {
        return await graphClient.External
            .Connections[graphOptions.ConnectorId]
            .Items[externalItem.Id]
            .PutAsync(externalItem);
    }

    /// <summary>
    /// Loads the Adaptive Card JSON template from a file.
    /// </summary>
    /// <param name="resultCardJsonFile">The path to the template file.</param>
    /// <returns>A dictionary deserialized from the JSON file.</returns>
    /// <exception cref="Exception">Thrown if the deserialization results in a null object.</exception>
    private static async Task<Dictionary<string, object>> GetResultTemplateAsync(
        string resultCardJsonFile)
    {
        var cardContents = await File.ReadAllTextAsync(resultCardJsonFile);
        return JsonSerializer.Deserialize<Dictionary<string, object>>(cardContents) ??
            throw new Exception($"Could not deserialize contents of {resultCardJsonFile}");
    }

    /// <summary>
    /// Initialize the <see cref="GraphServiceClient"/>.
    /// </summary>
    /// <returns>An authenticated <see cref="GraphServiceClient"/>.</returns>
    private GraphServiceClient InitializeGraph()
    {
        ArgumentException.ThrowIfNullOrEmpty(graphOptions.ClientId, nameof(graphOptions.ClientId));
        ArgumentException.ThrowIfNullOrEmpty(graphOptions.ClientSecret, nameof(graphOptions.ClientSecret));
        ArgumentException.ThrowIfNullOrEmpty(graphOptions.ConnectorId, nameof(graphOptions.ConnectorId));
        ArgumentException.ThrowIfNullOrEmpty(graphOptions.TenantId, nameof(graphOptions.TenantId));

        // Create an Azure identity ClientSecretCredential
        var credential = new ClientSecretCredential(
            graphOptions.TenantId,
            graphOptions.ClientId,
            graphOptions.ClientSecret);

        // Create a Microsoft Graph client that uses the HttpClient and credential
        return new GraphServiceClient(
            credential,
            ["https://graph.microsoft.com/.default"]);
    }

    /// <summary>
    /// Creates the connection for the Microsoft Graph connector.
    /// </summary>
    /// <returns>The <see cref="ExternalConnection"/>.</returns>
    private async Task<ExternalConnection?> CreateConnectionAsync()
    {
        try
        {
            var newConnection = new ExternalConnection
            {
                Id = graphOptions.ConnectorId,
                Name = "GitHub Issues Manager",
                Description = "GitHub issues for a project repository",
                ActivitySettings = new()
                {
                    // Enables the platform to detect when users share URLs
                    // to issues with each other. Microsoft 365 Copilot has a
                    // higher likelihood of displaying content that has been shared with that user.
                    // See https://learn.microsoft.com/microsoft-365-copilot/extensibility/overview-graph-connector
                    UrlToItemResolvers =
                    [
                        new ItemIdResolver
                    {
                        Priority = 1,
                        ItemId = "{issueId}",
                        UrlMatchInfo = new()
                        {
                            UrlPattern = $"/{gitHubOptions.RepoOwner}/{gitHubOptions.RepoName}/issues/(?<issueId>[0-9]+)",
                            BaseUrls = ["https://github.com"],
                        },
                    },
                ],
                },
                SearchSettings = new()
                {
                    // Defines an Adaptive Card template for displaying items
                    // in search results
                    SearchResultTemplates =
                    [
                        new()
                    {
                        Id = "issueDisplay",
                        Priority = 1,
                        Layout = new()
                        {
                            AdditionalData = await GetResultTemplateAsync("./Templates/search-result-issues.json"),
                        },
                    }
                    ],
                },
            };

            return await graphClient.External.Connections.PostAsync(newConnection);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating new connection");
            return null;
        }
    }

    /// <summary>
    /// Registers the item schema on the connection.
    /// </summary>
    /// <returns>A <see cref="Uri"/> to use to poll for registration status.</returns>
    private async Task<Uri?> RegisterSchemaAsync()
    {
        var nativeResponseHandler = new NativeResponseHandler();

        await graphClient.External
            .Connections[graphOptions.ConnectorId]
            .Schema
            .PatchAsync(IssuesSchema.Schema, requestConfig =>
            {
                requestConfig.Options.Add(new ResponseHandlerOption()
                {
                    ResponseHandler = nativeResponseHandler,
                });
            });

        if (nativeResponseHandler.Value is HttpResponseMessage response)
        {
            return response.Headers.Location;
        }

        return null;
    }
}

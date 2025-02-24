// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubIssueManager.Models;
using GitHubIssueManager.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GitHubIssueManager.Functions;

/// <summary>
/// Azure function to process queue messages created by the <see cref="Notify"/> function.
/// </summary>
/// <param name="issuesService">The <see cref="GitHubIssuesService"/> provided by dependency injection.</param>
/// <param name="connectorService">The <see cref="GraphConnectorService"/> provided by dependency injection.</param>
/// <param name="logger">The <see cref="ILogger"/> provided by dependency injection.</param>
public class ProcessNotification(
    GitHubIssuesService issuesService,
    GraphConnectorService connectorService,
    ILogger<ProcessNotification> logger)
{
    /// <summary>
    /// The function invoked when a message is added to the queue.
    /// </summary>
    /// <param name="queueEntry">The <see cref="GitHubIssueQueueEntry"/> from the queue.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    /// <exception cref="Exception">Thrown if the issue cannot be retrieved from the GitHub API.</exception>
    [Function(nameof(ProcessNotification))]
    public async Task Run([QueueTrigger("github-issues")] GitHubIssueQueueEntry queueEntry)
    {
        logger.LogInformation("Processing queue item for issue #{number}", queueEntry.IssueNumber);

        // Get the issue from the GitHub API
        var gitHubIssue = await issuesService.GetIssueWithRetryAsync(queueEntry.IssueNumber, 3) ??
            throw new Exception($"Could not get issue #{queueEntry.IssueNumber}");

        var externalItem = await issuesService.CreateExternalItemFromIssueAsync(gitHubIssue);

        await connectorService.AddOrUpdateItemAsync(externalItem);
    }
}

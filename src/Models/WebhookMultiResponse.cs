// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubIssueManager.Functions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace GitHubIssueManager.Models;

/// <summary>
/// Return type for the <see cref="Notify"/> function.
/// Allows for returning an HTTP status to the GitHub webhook
/// and outputting a queue entry into the Azure storage queue.
/// See https://learn.microsoft.com/azure/azure-functions/functions-add-output-binding-storage-queue-vs-code.
/// </summary>
public class WebhookMultiResponse(IActionResult httpResponse, GitHubIssueQueueEntry? queueEntry = null)
{
    /// <summary>
    /// Gets the queue entry to add to the queue.
    /// </summary>
    [QueueOutput("github-issues", Connection = "AzureWebJobsStorage")]
    public GitHubIssueQueueEntry? QueueEntry => queueEntry;

    /// <summary>
    /// Gets the HTTP response.
    /// </summary>
    public IActionResult HttpResponse => httpResponse;
}

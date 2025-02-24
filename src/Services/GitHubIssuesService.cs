// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using GitHubIssueManager.Extensions;
using GitHubIssueManager.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models.ExternalConnectors;
using Octokit;

namespace GitHubIssueManager.Services;

/// <summary>
/// Service that manages interactions with GitHub REST API.
/// </summary>
public class GitHubIssuesService
{
    private readonly GitHubOptions options;
    private readonly GitHubClient gitHubClient;
    private readonly ILogger<GitHubIssuesService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubIssuesService"/> class.
    /// </summary>
    /// <param name="gitHubOptions">The <see cref="GitHubOptions"/> loaded from local.settings.json.</param>
    /// <param name="logger">The <see cref="ILogger"/> provided by dependency injection.</param>
    public GitHubIssuesService(
        IOptions<GitHubOptions> gitHubOptions,
        ILogger<GitHubIssuesService> logger)
    {
        options = gitHubOptions.Value;
        this.logger = logger;

        ArgumentException.ThrowIfNullOrEmpty(
            options.PersonalAccessToken,
            "GitHubOptions:PersonalAccessToken");

        gitHubClient = new GitHubClient(new ProductHeaderValue("GitHubIssueManager", "1.0"))
        {
            Credentials = new Credentials(options.PersonalAccessToken),
        };
    }

    /// <summary>
    /// Gets all issues for the repository.
    /// </summary>
    /// <returns>The list of issues.</returns>
    public async Task<List<Issue>?> GetAllIssuesAsync()
    {
        var issueRequest = new RepositoryIssueRequest
        {
            State = ItemStateFilter.All,
        };

        // GitHub issues endpoint returns issues and pull requests
        var issuesAndPulls = await gitHubClient.Issue
            .GetAllForRepository(options.RepoOwner, options.RepoName, issueRequest);

        // Filter out pull requests
        return [.. issuesAndPulls.Where(issue => issue.PullRequest == null)];
    }

    /// <summary>
    /// Gets an issue by number.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue to get.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>The issue.</returns>
    public async Task<Issue?> GetIssueWithRetryAsync(int issueNumber, int retries)
    {
        try
        {
            return await gitHubClient.Issue
                .Get(options.RepoOwner, options.RepoName, issueNumber);
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                return await GetIssueWithRetryAsync(issueNumber, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Gets comments for an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>The list of comments.</returns>
    public async Task<IReadOnlyList<IssueComment>> GetCommentsForIssueWithRetryAsync(int issueNumber, int retries)
    {
        try
        {
            return await gitHubClient.Issue
                .Comment
                .GetAllForIssue(options.RepoOwner, options.RepoName, issueNumber);
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                return await GetCommentsForIssueWithRetryAsync(issueNumber, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Gets timeline events for an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>The list of events.</returns>
    public async Task<IReadOnlyList<TimelineEventInfo>> GetEventsForIssueWithRetryAsync(int issueNumber, int retries)
    {
        try
        {
            return await gitHubClient.Issue
                .Timeline
                .GetAllForIssue(options.RepoOwner, options.RepoName, issueNumber);
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                return await GetEventsForIssueWithRetryAsync(issueNumber, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Creates an <see cref="ExternalItem"/> from an <see cref="Issue"/>.
    /// </summary>
    /// <param name="issue">The issue to create from.</param>
    /// <returns>The external item.</returns>
    public async Task<ExternalItem> CreateExternalItemFromIssueAsync(Issue issue)
    {
        // Get timeline events
        var timelineEvents = await GetEventsForIssueWithRetryAsync(issue.Number, 3);
        var externalItem = issue.ToExternalItem(timelineEvents);

        // Content for the ingested item will consist of the
        // body of the issue + all comments, converted to plain text.
        var contentBuilder = new StringBuilder(Markdig.Markdown.ToPlainText(issue.Body));

        var comments = await GetCommentsForIssueWithRetryAsync(issue.Number, 3);

        foreach (var comment in comments)
        {
            contentBuilder.Append(Environment.NewLine);
            contentBuilder.Append(Markdig.Markdown.ToPlainText(comment.Body));
        }

        externalItem.Content = new()
        {
            Type = ExternalItemContentType.Text,
            Value = contentBuilder.ToString(),
        };

        return externalItem;
    }

    /// <summary>
    /// Adds labels to an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue to label.</param>
    /// <param name="labels">A list of labels to add to the issue.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    public async Task AddLabelsToIssueAsyncWithRetry(int issueNumber, List<string> labels, int retries)
    {
        try
        {
            await gitHubClient.Issue.Labels.AddToIssue(options.RepoOwner, options.RepoName, issueNumber, [.. labels]);
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                await AddLabelsToIssueAsyncWithRetry(issueNumber, labels, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Removes labels from an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue to unlabel.</param>
    /// <param name="labels">A list of labels to remove from the issue.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    public async Task RemoveLabelsFromIssueAsyncWithRetry(int issueNumber, List<string> labels, int retries)
    {
        try
        {
            foreach (var label in labels)
            {
                await gitHubClient.Issue.Labels.RemoveFromIssue(options.RepoOwner, options.RepoName, issueNumber, label);
            }
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                await RemoveLabelsFromIssueAsyncWithRetry(issueNumber, labels, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Assigns users to an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue to assign.</param>
    /// <param name="users">A list of GitHub usernames to assign to the issue.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    public async Task AssignUsersToIssueAsyncWithRetry(int issueNumber, List<string> users, int retries)
    {
        try
        {
            await gitHubClient.Issue.Assignee.AddAssignees(options.RepoOwner, options.RepoName, issueNumber, new(users));
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                await AssignUsersToIssueAsyncWithRetry(issueNumber, users, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Unassigns users from an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue to unassign.</param>
    /// <param name="users">A list of GitHub usernames to unassign from the issue.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    public async Task UnassignUsersFromIssueAsyncWithRetry(int issueNumber, List<string> users, int retries)
    {
        try
        {
            await gitHubClient.Issue.Assignee.RemoveAssignees(options.RepoOwner, options.RepoName, issueNumber, new(users));
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                await UnassignUsersFromIssueAsyncWithRetry(issueNumber, users, retries);
            }

            throw;
        }
    }

    /// <summary>
    /// Closes an issue.
    /// </summary>
    /// <param name="issueNumber">The issue number of the issue to close.</param>
    /// <param name="retries">The number of times to retry if a rate limit error is received.</param>
    /// <returns>A <see cref="Task"/> indicating the status of the asynchronous operation.</returns>
    public async Task CloseIssueAsyncWithRetry(int issueNumber, int retries)
    {
        try
        {
            await gitHubClient.Issue.Update(options.RepoOwner, options.RepoName, issueNumber, new()
            {
                State = ItemState.Closed,
            });
        }
        catch (RateLimitExceededException ex)
        {
            if (retries > 0)
            {
                logger.LogWarning(
                    "Rate limit exceeded - waiting for {seconds} seconds. {retries} retries remaining.",
                    ex.GetRetryAfterTimeSpan().TotalSeconds,
                    retries--);
                await Task.Delay(ex.GetRetryAfterTimeSpan());
                await CloseIssueAsyncWithRetry(issueNumber, retries);
            }

            throw;
        }
    }
}

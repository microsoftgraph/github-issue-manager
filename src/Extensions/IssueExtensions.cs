// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models.ExternalConnectors;
using Octokit;

namespace GitHubIssueManager.Extensions;

/// <summary>
/// Static class providing extensions to the <see cref="Issue"/> class.
/// </summary>
public static class IssueExtensions
{
    /// <summary>
    /// Creates an <see cref="ExternalItem"/> from properties of the <see cref="Issue"/>.
    /// </summary>
    /// <param name="issue">The <see cref="Issue"/> to create from.</param>
    /// <param name="events">A list of time line events for the issue.</param>
    /// <returns>An instance of <see cref="ExternalItem"/>.</returns>
    public static ExternalItem ToExternalItem(
        this Issue issue,
        IReadOnlyList<TimelineEventInfo>? events)
    {
        return new ExternalItem
        {
            Id = issue.Number.ToString(),
            Acl =
            [
                new()
                {
                    Type = AclType.Everyone,
                    Value = "everyone",
                    AccessType = AccessType.Grant,
                },
            ],
            Properties = issue.ToProperties(events),
        };
    }

    /// <summary>
    /// Creates a <see cref="Properties"/> from properties of the <see cref="Issue"/>.
    /// </summary>
    /// <param name="issue">The <see cref="Issue"/> to create from.</param>
    /// <param name="events">A list of time line events for the issue.</param>
    /// <returns>An instance of <see cref="Properties"/>.</returns>
    public static Properties ToProperties(this Issue issue, IReadOnlyList<TimelineEventInfo>? events)
    {
        string lastModifiedBy = events is not null && events.Count > 0 ?
            events[events.Count - 1].Actor?.Login ?? issue.User.Login : issue.User.Login;

        // Add the author to an array
        // This is because the author property in our schema
        // has the semantic label "authors", which requires an array
        string[] author = [issue.User.Login];
        return new()
        {
            AdditionalData = new Dictionary<string, object>
            {
                { "title", issue.Title },
                { "issueNumber", issue.Number },
                { "repo", issue.Url.ExtractRepoNameFromUrl() ?? string.Empty },
                { "body", issue.Body },
                { "assignees", AssigneesToString(issue.Assignees) },
                { "labels", LabelsToString(issue.Labels) },
                { "state", issue.State.ToString() },
                { "issueUrl", issue.HtmlUrl },
                { "icon", issue.User.AvatarUrl },
                { "updatedAt", issue.UpdatedAt ?? DateTimeOffset.MinValue },
                { "lastModifiedBy", lastModifiedBy },
                { "author@odata.type", "Collection(String)" },
                { "author", author },
                { "authorUrl", issue.User.HtmlUrl },
                { "statusIcon", IconFromState(issue.State.Value) },
            },
        };
    }

    private static string LabelsToString(IReadOnlyList<Octokit.Label> labels)
    {
        if (labels.Count <= 0)
        {
            return "None";
        }

        var labelNames = labels.Select(l => l.Name);
        return string.Join(",", labelNames);
    }

    private static string AssigneesToString(IReadOnlyList<User> users)
    {
        if (users.Count <= 0)
        {
            return "None";
        }

        var userNames = users.Select(u => u.Login);
        return string.Join(",", userNames);
    }

    private static string IconFromState(ItemState state) => state
    switch
    {
        ItemState.Open => "https://img.shields.io/badge/Open-brightgreen?logo=github",
        ItemState.Closed => "https://img.shields.io/badge/Closed-purple?logo=github",
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}

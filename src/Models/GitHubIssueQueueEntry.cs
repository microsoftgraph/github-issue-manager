// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents an Azure storage queue entry for a GitHub issue event.
/// </summary>
public class GitHubIssueQueueEntry
{
    /// <summary>
    /// Gets or sets the owner of the repository.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the name of the repository.
    /// </summary>
    public string? Repo { get; set; }

    /// <summary>
    /// Gets or sets the issue number.
    /// </summary>
    public int IssueNumber { get; set; }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubIssueManager.Options;

/// <summary>
/// GitHub options loaded from local.settings.json.
/// </summary>
public class GitHubOptions
{
    /// <summary>
    /// Gets or sets the webhook secret.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to log webhook payloads.
    /// </summary>
    public bool LogWebhookPayloads { get; set; } = false;

    /// <summary>
    /// Gets or sets the repository owner.
    /// </summary>
    public string? RepoOwner { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string? RepoName { get; set; }

    /// <summary>
    /// Gets or sets the personal access token (PAT).
    /// </summary>
    public string? PersonalAccessToken { get; set; }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents a GitHub webhook event payload.
/// </summary>
public class GitHubEvent
{
    /// <summary>
    /// Gets or sets the action that triggered the event.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the issue associated with the event.
    /// </summary>
    [JsonPropertyName("issue")]
    public GitHubIssue? Issue { get; set; }

    /// <summary>
    /// Gets or sets the repository associated with the event.
    /// </summary>
    [JsonPropertyName("repository")]
    public GitHubRepo? Repository { get; set; }
}

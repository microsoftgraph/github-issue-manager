// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents a GitHub issue in a webhook event payload.
/// </summary>
public class GitHubIssue
{
    /// <summary>
    /// Gets or sets the issue number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }
}

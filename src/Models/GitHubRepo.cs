// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents a GitHub repository in a webhook event payload.
/// </summary>
public class GitHubRepo
{
    /// <summary>
    /// Gets or sets the full name of the repository.
    /// </summary>
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets the repository's owner.
    /// </summary>
    /// <returns>The repository's owner.</returns>
    public string? GetOwner()
    {
        return FullName?.Split('/')[0];
    }

    /// <summary>
    /// Gets the repository's name.
    /// </summary>
    /// <returns>The repository's name.</returns>
    public string? GetName()
    {
        return FullName?.Split('/')[1];
    }
}

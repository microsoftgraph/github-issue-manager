// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents the request body for the /api/LabelIssue and /api/UnlabelIssue endpoints.
/// </summary>
public class UpdateLabelsRequest
{
    /// <summary>
    /// Gets or sets a list of labels to add to or remove from the issue.
    /// </summary>
    [OpenApiProperty(Description = "An array of labels to add to or remove from the issue")]
    public List<string> Labels { get; set; } = [];
}

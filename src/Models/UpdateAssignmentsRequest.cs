// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents the request body for the /api/AssignIssue and /api/UnassignIssue endpoints.
/// </summary>
public class UpdateAssignmentsRequest
{
    /// <summary>
    /// Gets or sets a list of GitHub usernames to assign to or unassign from the issue.
    /// </summary>
    [OpenApiProperty(Description = "An array of GitHub usernames to assign to or unassign from the issue")]
    public List<string> Users { get; set; } = [];
}

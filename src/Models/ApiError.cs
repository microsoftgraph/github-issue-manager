// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

namespace GitHubIssueManager.Models;

/// <summary>
/// Represents the payload returned by the API when an error occurs.
/// </summary>
public class ApiError(string code, string message)
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    [OpenApiProperty(Description = "The error code")]
    public string Code => code;

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [OpenApiProperty(Description = "The error message to display to the user")]
    public string Message => message;
}

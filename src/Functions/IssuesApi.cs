// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubIssueManager.Models;
using GitHubIssueManager.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace GitHubIssueManager.Functions;

/// <summary>
/// Contains functions that serve as REST APIs that the API
/// plugin will call from the declarative agent.
/// </summary>
/// <param name="issuesService">The <see cref="GitHubIssuesService"/> provided by dependency injection.</param>
/// <param name="logger">The <see cref="ILogger"/> provided by dependency injection.</param>
public class IssuesApi(
    GitHubIssuesService issuesService,
    ILogger<IssuesApi> logger)
{
    /// <summary>
    /// Adds labels to an issue.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="issueNumber">The issue number of the issue to label.</param>
    /// <param name="labelRequest">The label request body.</param>
    /// <returns>202 Accepted.</returns>
    [Function(nameof(LabelIssue))]
    [OpenApiOperation(
        operationId: nameof(LabelIssue),
        Description = "Adds labels to an issue")]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(
        "issueNumber",
        In = ParameterLocation.Path,
        Type = typeof(int),
        Description = "The issue number of the issue to label",
        Required = true)]
    [OpenApiRequestBody("application/json", typeof(UpdateLabelsRequest))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.Accepted,
        "text/plain",
        typeof(string))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.BadRequest,
        "application/json",
        typeof(ApiError))]
    public async Task<IActionResult> LabelIssue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{nameof(LabelIssue)}/{{issueNumber}}")] HttpRequest req,
        [FromRoute] int issueNumber,
        [FromBody] UpdateLabelsRequest labelRequest)
    {
        logger.LogInformation(
            "Received request to add {labels} to issue #{number}",
            string.Join(',', labelRequest.Labels),
            issueNumber);

        try
        {
            await issuesService.AddLabelsToIssueAsyncWithRetry(issueNumber, labelRequest.Labels, 3);
            return new AcceptedResult(string.Empty, "Success");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ApiError("LabelIssueError", ex.Message));
        }
    }

    /// <summary>
    /// Removes labels from an issue.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="issueNumber">The issue number of the issue to unlabel.</param>
    /// <param name="labelRequest">The label request body.</param>
    /// <returns>202 Accepted.</returns>
    [Function(nameof(UnlabelIssue))]
    [OpenApiOperation(
        operationId: nameof(UnlabelIssue),
        Description = "Removes labels from an issue")]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(
        "issueNumber",
        In = ParameterLocation.Path,
        Type = typeof(int),
        Description = "The issue number of the issue to unlabel",
        Required = true)]
    [OpenApiRequestBody("application/json", typeof(UpdateLabelsRequest))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.Accepted,
        "text/plain",
        typeof(string))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.BadRequest,
        "application/json",
        typeof(ApiError))]
    public async Task<IActionResult> UnlabelIssue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{nameof(UnlabelIssue)}/{{issueNumber}}")] HttpRequest req,
        [FromRoute] int issueNumber,
        [FromBody] UpdateLabelsRequest labelRequest)
    {
        logger.LogInformation(
            "Received request to remove {labels} to issue #{number}",
            string.Join(',', labelRequest.Labels),
            issueNumber);

        try
        {
            await issuesService.RemoveLabelsFromIssueAsyncWithRetry(issueNumber, labelRequest.Labels, 3);
            return new AcceptedResult(string.Empty, "Success");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ApiError("UnlabelIssueError", ex.Message));
        }
    }

    /// <summary>
    /// Assigns users to an issue.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="issueNumber">The issue number of the issue to assign.</param>
    /// <param name="assignmentRequest">The assignment request body.</param>
    /// <returns>202 Accepted.</returns>
    [Function(nameof(AssignIssue))]
    [OpenApiOperation(
        operationId: nameof(AssignIssue),
        Description = "Assigns users to an issue")]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(
        "issueNumber",
        In = ParameterLocation.Path,
        Type = typeof(int),
        Description = "The issue number of the issue to assign",
        Required = true)]
    [OpenApiRequestBody("application/json", typeof(UpdateAssignmentsRequest))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.Accepted,
        "text/plain",
        typeof(string))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.BadRequest,
        "application/json",
        typeof(ApiError))]
    public async Task<IActionResult> AssignIssue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{nameof(AssignIssue)}/{{issueNumber}}")] HttpRequest req,
        [FromRoute] int issueNumber,
        [FromBody] UpdateAssignmentsRequest assignmentRequest)
    {
        logger.LogInformation(
            "Received request to assign issue #{number} to {users}",
            issueNumber,
            string.Join(',', assignmentRequest.Users));

        try
        {
            await issuesService.AssignUsersToIssueAsyncWithRetry(issueNumber, assignmentRequest.Users, 3);
            return new AcceptedResult(string.Empty, "Success");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ApiError("AssignIssueError", ex.Message));
        }
    }

    /// <summary>
    /// Unassigns users from an issue.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="issueNumber">The issue number of the issue to unassign.</param>
    /// <param name="assignmentRequest">The assignment request body.</param>
    /// <returns>202 Accepted.</returns>
    [Function(nameof(UnassignIssue))]
    [OpenApiOperation(
        operationId: nameof(UnassignIssue),
        Description = "Unassigns users from an issue")]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(
        "issueNumber",
        In = ParameterLocation.Path,
        Type = typeof(int),
        Description = "The issue number of the issue to unassign",
        Required = true)]
    [OpenApiRequestBody("application/json", typeof(UpdateAssignmentsRequest))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.Accepted,
        "text/plain",
        typeof(string))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.BadRequest,
        "application/json",
        typeof(ApiError))]
    public async Task<IActionResult> UnassignIssue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{nameof(UnassignIssue)}/{{issueNumber}}")] HttpRequest req,
        [FromRoute] int issueNumber,
        [FromBody] UpdateAssignmentsRequest assignmentRequest)
    {
        logger.LogInformation(
            "Received request to unassign issue #{number} from {users}",
            issueNumber,
            string.Join(',', assignmentRequest.Users));

        try
        {
            await issuesService.UnassignUsersFromIssueAsyncWithRetry(issueNumber, assignmentRequest.Users, 3);
            return new AcceptedResult(string.Empty, "Success");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ApiError("UnassignIssueError", ex.Message));
        }
    }

    /// <summary>
    /// Closes an issue.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="issueNumber">The issue number of the issue to close.</param>
    /// <returns>202 Accepted.</returns>
    [Function(nameof(CloseIssue))]
    [OpenApiOperation(
        operationId: nameof(CloseIssue),
        Description = "Closes an issue")]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query)]
    [OpenApiParameter(
        "issueNumber",
        In = ParameterLocation.Path,
        Type = typeof(int),
        Description = "The issue number of the issue to close",
        Required = true)]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.Accepted,
        "text/plain",
        typeof(string))]
    [OpenApiResponseWithBody(
        System.Net.HttpStatusCode.BadRequest,
        "application/json",
        typeof(ApiError))]
    public async Task<IActionResult> CloseIssue(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{nameof(CloseIssue)}/{{issueNumber}}")] HttpRequest req,
        [FromRoute] int issueNumber)
    {
        logger.LogInformation(
            "Received request to close issue #{number}",
            issueNumber);

        try
        {
            await issuesService.CloseIssueAsyncWithRetry(issueNumber, 3);
            return new AcceptedResult(string.Empty, "Success");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(new ApiError("CloseIssueError", ex.Message));
        }
    }
}

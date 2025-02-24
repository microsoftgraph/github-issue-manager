// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubIssueManager.Options;

/// <summary>
/// Microsoft Graph options loaded from local.settings.json.
/// </summary>
public class GraphOptions
{
    /// <summary>
    /// Gets or sets the client ID from Entra app registration.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret from Entra app registration.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the Microsoft Graph connector ID.
    /// </summary>
    public string? ConnectorId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID from Entra app registration.
    /// </summary>
    public string? TenantId { get; set; }
}

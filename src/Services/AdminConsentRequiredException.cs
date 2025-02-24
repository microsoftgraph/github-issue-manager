// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace GitHubIssueManager.Services;

/// <summary>
/// Exception indicating that admin consent is required.
/// </summary>
/// <param name="clientId">The client ID of the app registration.</param>
/// <param name="tenantId">The tenant ID of the app registration.</param>
public class AdminConsentRequiredException(string? clientId, string? tenantId) : Exception(GetMessage(clientId, tenantId))
{
    private static readonly string MessageTemplate =
        "You need to grant tenant-wide admin consent to the application in Entra ID\nUse this link to provide the consent\nhttps://login.microsoftonline.com/{0}/adminconsent?client_id={1}";

    private static string GetMessage(string? clientId, string? tenantId)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            return string.Format(
                MessageTemplate,
                tenantId ?? "organizations",
                clientId);
        }
        else
        {
            return "Admin consent is required.";
        }
    }
}

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Graph.Models.ExternalConnectors;

namespace GitHubIssueManager.Schemas;

/// <summary>
/// Contains the schema for ingesting GitHub issues.
/// </summary>
public static class IssuesSchema
{
    /// <summary>
    /// Schema for ingesting GitHub issues.
    /// </summary>
    public static readonly Schema Schema = new()
    {
        BaseType = "microsoft.graph.externalItem",
        Properties =
        [
            new()
            {
                Aliases = ["issueTitle"],
                Name = "title",
                Type = PropertyType.String,
                IsSearchable = true,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
                Labels = [Label.Title],
            },
            new()
            {
                Name = "issueNumber",
                Type = PropertyType.Int64,
                IsSearchable = false,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
            },
            new()
            {
                Name = "repo",
                Type = PropertyType.String,
                IsSearchable = true,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
            },
            new()
            {
                Aliases = ["message"],
                Name = "body",
                Type = PropertyType.String,
                IsSearchable = true,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
            },
            new()
            {
                Name = "assignees",
                Type = PropertyType.String,
                IsSearchable = true,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
            },
            new()
            {
                Name = "labels",
                Type = PropertyType.String,
                IsSearchable = true,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
            },
            new()
            {
                Name = "state",
                Type = PropertyType.String,
                IsSearchable = false,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = true,
            },
            new()
            {
                Name = "issueUrl",
                Type = PropertyType.String,
                IsSearchable = false,
                IsQueryable = false,
                IsRetrievable = true,
                IsRefinable = false,
                Labels = [Label.Url],
            },
            new()
            {
                Name = "icon",
                Type = PropertyType.String,
                IsSearchable = false,
                IsQueryable = false,
                IsRetrievable = true,
                IsRefinable = false,
                Labels = [Label.IconUrl],
            },
            new()
            {
                Name = "updatedAt",
                Type = PropertyType.DateTime,
                IsSearchable = false,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = true,
                Labels = [Label.LastModifiedDateTime],
            },
            new()
            {
                Name = "lastModifiedBy",
                Type = PropertyType.String,
                IsSearchable = true,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = false,
                Labels = [Label.LastModifiedBy],
            },
            new()
            {
                Name = "author",
                Type = PropertyType.StringCollection,
                IsSearchable = false,
                IsQueryable = true,
                IsRetrievable = true,
                IsRefinable = true,
                Labels = [Label.Authors],
            },
            new()
            {
                Name = "authorUrl",
                Type = PropertyType.String,
                IsSearchable = false,
                IsQueryable = false,
                IsRetrievable = true,
                IsRefinable = false,
            },
            new()
            {
                Name = "statusIcon",
                Type = PropertyType.String,
                IsSearchable = false,
                IsQueryable = false,
                IsRetrievable = true,
                IsRefinable = false,
            },
        ],
    };
}

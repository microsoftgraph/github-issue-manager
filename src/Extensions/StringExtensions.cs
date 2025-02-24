// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace GitHubIssueManager.Extensions;

/// <summary>
/// Contains extensions to the <see cref="string"/> class.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Compares two strings for equality, ignoring case.
    /// </summary>
    /// <param name="value">The string this method is called on.</param>
    /// <param name="compare">The string to compare with.</param>
    /// <returns>True if the strings are equal, false if not.</returns>
    public static bool IsEqualIgnoringCase(this string value, string compare)
    {
        return string.Compare(value, compare, StringComparison.OrdinalIgnoreCase) == 0;
    }

    /// <summary>
    /// Extracts the repo name in the form 'owner/repo' from a GitHub API URL.
    /// </summary>
    /// <param name="url">The URL to extract from.</param>
    /// <returns>The extracted repo name.</returns>
    public static string? ExtractRepoNameFromUrl(this string url)
    {
        var matches = IssueUrlRegex().Matches(url);
        if (matches.Count <= 0)
        {
            return null;
        }

        return matches[0].Groups["repo"].Value;
    }

    [GeneratedRegex("https://api.github.com/repos/(?'repo'[^/]+/[^/]+)")]
    private static partial Regex IssueUrlRegex();
}

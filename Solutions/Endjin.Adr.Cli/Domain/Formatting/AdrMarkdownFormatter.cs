using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Endjin.Adr.Cli.Domain.Formatting;

/// <summary>
/// Provides helpers to extract and update markdown sections within ADR documents.
/// </summary>
internal static partial class AdrMarkdownFormatter
{
    /// <summary>
    /// Extracts the entries within the <c>## Status</c> section of an ADR document.
    /// </summary>
    public static IReadOnlyList<string> ExtractStatusEntries(string content)
    {
        Match match = StatusSectionRegex().Match(content ?? string.Empty);

        if (!match.Success)
        {
            return Array.Empty<string>();
        }

        string normalized = match.Groups["body"].Value.Replace("\r\n", "\n").Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        return normalized
            .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrEmpty(entry))
            .ToList();
    }

    /// <summary>
    /// Replaces the contents of the <c>## Status</c> section with the provided entries.
    /// </summary>
    public static string WithStatusEntries(string content, IReadOnlyList<string> entries)
    {
        string original = content ?? string.Empty;
        string lineEnding = DetectLineEnding(original);
        string replacement = BuildSectionBody(entries, lineEnding);

        Match match = StatusSectionRegex().Match(original);

        if (match.Success)
        {
            return StatusSectionRegex().Replace(original, match.Groups["header"].Value + replacement, 1);
        }

        string separator = original.Length == 0 || original.EndsWith(lineEnding + lineEnding, StringComparison.Ordinal)
            ? string.Empty
            : lineEnding + lineEnding;

        return string.Concat(original.TrimEnd(), separator, "## Status", replacement, lineEnding);
    }

    private static string BuildSectionBody(IReadOnlyList<string> entries, string lineEnding)
    {
        if (entries is null || entries.Count == 0)
        {
            return lineEnding;
        }

        IEnumerable<string> distinct = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0);

        string[] values = distinct.ToArray();

        if (values.Length == 0)
        {
            return lineEnding;
        }

        string doubleLineEnding = lineEnding + lineEnding;
        return lineEnding + string.Join(doubleLineEnding, values) + doubleLineEnding;
    }

    private static string DetectLineEnding(string content)
    {
        return content is not null && content.Contains("\r\n", StringComparison.Ordinal)
            ? "\r\n"
            : "\n";
    }

    [GeneratedRegex(@"(?<header>^##\s+Status\s*\r?\n)(?<body>.*?)(?=^\s*##\s+|\Z)", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex StatusSectionRegex();
}


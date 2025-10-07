// <copyright file="AdrCommandUtilities.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Commands.Shared;

/// <summary>
/// Provides helper methods shared across ADR commands.
/// </summary>
internal static class AdrCommandUtilities
{
    private static readonly Regex NumberedTitleRegex = new("^\\d+\\.\\s", RegexOptions.Compiled);

    /// <summary>
    /// Resolves a textual reference (number, slug or title fragment) to an ADR document.
    /// </summary>
    public static Adr ResolveAdrReference(string reference, IReadOnlyList<Adr> documents)
    {
        if (string.IsNullOrWhiteSpace(reference) || documents is null)
        {
            return null;
        }

        string candidate = reference.Trim();

        if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int recordNumber))
        {
            return documents.FirstOrDefault(doc => doc.RecordNumber == recordNumber);
        }

        Adr byFileName = documents.FirstOrDefault(doc =>
            !string.IsNullOrEmpty(doc.Path) &&
            Path.GetFileName(doc.Path).Contains(candidate, StringComparison.OrdinalIgnoreCase));

        if (byFileName is not null)
        {
            return byFileName;
        }

        return documents.FirstOrDefault(doc =>
            !string.IsNullOrWhiteSpace(doc.Title) &&
            doc.Title.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Calculates a Markdown-friendly relative path between ADR files.
    /// </summary>
    public static string GetMarkdownRelativePath(string fromDirectory, string toPath)
    {
        string baseDirectory = string.IsNullOrEmpty(fromDirectory)
            ? Environment.CurrentDirectory
            : fromDirectory;

        string from = Path.GetFullPath(baseDirectory);
        string to = Path.GetFullPath(toPath);
        string relative = Path.GetRelativePath(from, to);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Formats a display title reusing adr-tools conventions.
    /// </summary>
    public static string FormatDisplayTitle(int recordNumber, string title)
    {
        string trimmedTitle = title?.Trim() ?? string.Empty;

        if (NumberedTitleRegex.IsMatch(trimmedTitle))
        {
            return trimmedTitle;
        }

        if (trimmedTitle.Length == 0)
        {
            return $"ADR-{recordNumber:D4}";
        }

        return $"ADR-{recordNumber:D4}: {trimmedTitle}";
    }

    /// <summary>
    /// Produces a short textual representation of the ADR status for table output.
    /// </summary>
    public static string FormatStatusSummary(AdrStatus status)
    {
        if (status is null || status.IsEmpty)
        {
            return string.Empty;
        }

        if (status.Reference is null)
        {
            return status.Value?.Trim() ?? string.Empty;
        }

        string label = string.IsNullOrWhiteSpace(status.Value)
            ? status.Reference.Relationship
            : status.Value;

        string target = !string.IsNullOrWhiteSpace(status.Reference.Text)
            ? status.Reference.Text.Trim()
            : status.Reference.RecordNumber.HasValue
                ? $"ADR-{status.Reference.RecordNumber.Value:D4}"
                : status.Reference.Target;

        return string.IsNullOrWhiteSpace(label)
            ? target
            : $"{label.Trim()} → {target}";
    }
}

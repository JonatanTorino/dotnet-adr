// <copyright file="Adr.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Endjin.Adr.Cli.Domain.Models;

/// <summary>
/// Represents a parsed Architectural Decision Record.
/// </summary>
public class Adr
{
    private static readonly Regex SlugSanitizer = new("[^a-z0-9]+", RegexOptions.Compiled);

    /// <summary>
    /// Gets or sets the ADR identifier.
    /// </summary>
    public int RecordNumber { get; init; }

    /// <summary>
    /// Gets or sets the full path for the ADR file.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Gets or sets the title of the ADR.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Gets or sets the decision date when available.
    /// </summary>
    public DateTime? Date { get; init; }

    /// <summary>
    /// Gets or sets the raw Markdown content of the ADR.
    /// </summary>
    public string Content { get; init; }

    /// <summary>
    /// Gets or sets the parsed sections of the ADR.
    /// </summary>
    public IReadOnlyList<AdrSection> Sections { get; init; } = Array.Empty<AdrSection>();

    /// <summary>
    /// Gets or sets the parsed status of the ADR.
    /// </summary>
    public AdrStatus Status { get; init; } = AdrStatus.Empty;

    /// <summary>
    /// Gets or sets the parsed links declared by the ADR.
    /// </summary>
    public IReadOnlyList<AdrLink> Links { get; init; } = Array.Empty<AdrLink>();

    /// <summary>
    /// Gets or sets the metadata extracted from the optional YAML front matter.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a normalized file name for the ADR.
    /// </summary>
    /// <returns>A normalized file name including the record number.</returns>
    public string SafeFileName()
    {
        string title = this.Title ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(title))
        {
            title = RemoveDiacritics(title).ToLowerInvariant();
        }

        string slug = SlugSanitizer
            .Replace(title, "-")
            .Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "adr";
        }

        return $"{this.RecordNumber:D4}-{slug}.md";
    }

    private static string RemoveDiacritics(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(text.Length);

        foreach (char c in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

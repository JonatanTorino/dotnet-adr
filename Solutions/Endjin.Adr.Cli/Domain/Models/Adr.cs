// <copyright file="Adr.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Endjin.Adr.Cli.Domain.Models;

/// <summary>
/// Represents a parsed Architectural Decision Record.
/// </summary>
public class Adr
{
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
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets a normalized file name for the ADR.
    /// </summary>
    /// <returns>A normalized file name including the record number.</returns>
    public string SafeFileName()
    {
        string slug = this.Title?.ToLowerInvariant().Replace(" ", "-") ?? string.Empty;
        return $"{this.RecordNumber:D4}-{slug}.md";
    }
}

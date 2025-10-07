// <copyright file="AdrSection.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Endjin.Adr.Cli.Domain.Models;

/// <summary>
/// Represents a logical section within an ADR document.
/// </summary>
public class AdrSection
{
    public AdrSection(string name, string content, int level)
    {
        this.Name = name;
        this.Content = content;
        this.Level = level;
    }

    /// <summary>
    /// Gets the section heading text.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the Markdown content contained within the section.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the heading level used in the Markdown document.
    /// </summary>
    public int Level { get; }
}

// <copyright file="AdrLink.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System;
using System.Text.RegularExpressions;

namespace Endjin.Adr.Cli.Domain.Models;

/// <summary>
/// Represents a relationship declared between ADRs.
/// </summary>
public class AdrLink
{
    private static readonly Regex RecordNumberFromText = new("(?<number>\\d{4})", RegexOptions.Compiled);
    private static readonly Regex RecordNumberFromAdrCode = new("ADR-?(?<number>\\d{4})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AdrLink(string relationship, string text, string target)
    {
        this.Relationship = relationship?.Trim() ?? string.Empty;
        this.Text = text;
        this.Target = target;
        this.RecordNumber = TryParseRecordNumber(text) ?? TryParseRecordNumber(target);
    }

    /// <summary>
    /// Gets the semantic relationship (e.g. "Supersedes").
    /// </summary>
    public string Relationship { get; }

    /// <summary>
    /// Gets the linked ADR label (e.g. ADR-0005).
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the Markdown target, typically a relative path.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the record number parsed from the link when available.
    /// </summary>
    public int? RecordNumber { get; }

    private static int? TryParseRecordNumber(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        Match adrMatch = RecordNumberFromAdrCode.Match(candidate);
        if (adrMatch.Success && int.TryParse(adrMatch.Groups["number"].Value, out int adrNumber))
        {
            return adrNumber;
        }

        Match textMatch = RecordNumberFromText.Match(candidate);
        if (textMatch.Success && int.TryParse(textMatch.Groups["number"].Value, out int textNumber))
        {
            return textNumber;
        }

        return null;
    }
}

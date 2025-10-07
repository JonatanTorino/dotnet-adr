// <copyright file="AdrStatus.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Endjin.Adr.Cli.Domain.Models;

/// <summary>
/// Represents the lifecycle status of an ADR.
/// </summary>
public class AdrStatus
{
    public static AdrStatus Empty { get; } = new(string.Empty, null);

    public AdrStatus(string value, AdrLink reference)
    {
        this.Value = value;
        this.Reference = reference;
    }

    /// <summary>
    /// Gets the textual status value, e.g. "Accepted".
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets a reference to another ADR when the status links to one (e.g. superseded).
    /// </summary>
    public AdrLink Reference { get; }

    /// <summary>
    /// Gets a value indicating whether the status contains any information.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(this.Value) && this.Reference is null;
}

using System;
using System.IO;

namespace Endjin.Adr.Cli.Domain.Contracts;

/// <summary>
/// Options that control how ADR files are discovered and loaded.
/// </summary>
public class AdrRepositoryOptions
{
    public AdrRepositoryOptions(string rootPath, SearchOption searchScope = SearchOption.TopDirectoryOnly)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path must be provided", nameof(rootPath));
        }

        this.RootPath = rootPath;
        this.SearchScope = searchScope;
    }

    /// <summary>
    /// Gets the directory that contains ADR markdown files.
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// Gets the search scope used when discovering ADR files.
    /// </summary>
    public SearchOption SearchScope { get; }
}

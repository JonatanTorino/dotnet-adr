using System;

namespace Endjin.Adr.Cli.Infrastructure.Workspace;

/// <summary>
/// Represents the resolved workspace information required by ADR commands.
/// </summary>
public class AdrWorkspaceContext
{
    public AdrWorkspaceContext(string repositoryPath, string templatePath, string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("Repository path must be provided", nameof(repositoryPath));
        }

        this.RepositoryPath = repositoryPath;
        this.TemplatePath = templatePath;
        this.ConfigurationPath = configurationPath;
    }

    /// <summary>
    /// Gets the absolute path to the ADR repository directory.
    /// </summary>
    public string RepositoryPath { get; }

    /// <summary>
    /// Gets the absolute path to the configured template file when available.
    /// </summary>
    public string TemplatePath { get; }

    /// <summary>
    /// Gets the configuration file that produced this workspace, if any.
    /// </summary>
    public string ConfigurationPath { get; }
}

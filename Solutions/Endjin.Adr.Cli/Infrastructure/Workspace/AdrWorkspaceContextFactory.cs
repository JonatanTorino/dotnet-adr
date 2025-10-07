using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Configuration;
using Endjin.Adr.Cli.Configuration.Contracts;

namespace Endjin.Adr.Cli.Infrastructure.Workspace;

/// <summary>
/// Resolves ADR repository information from CLI options and configuration files.
/// </summary>
public class AdrWorkspaceContextFactory : IAdrWorkspaceContextFactory
{
    private readonly IConfigurationLocator configurationLocator;

    public AdrWorkspaceContextFactory(IConfigurationLocator configurationLocator)
    {
        this.configurationLocator = configurationLocator;
    }

    public async Task<AdrWorkspaceContext> CreateAsync(string pathOption, CancellationToken cancellationToken = default)
    {
        string repositoryPath = pathOption;
        string templatePath = null;
        string configurationPath = null;

        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            configurationPath = this.configurationLocator.LocatedRootConfiguration();

            if (!string.IsNullOrEmpty(configurationPath))
            {
                string configText = await File.ReadAllTextAsync(configurationPath, cancellationToken).ConfigureAwait(false);

                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true,
                };

                AdrConfig config = JsonSerializer.Deserialize<AdrConfig>(configText, options) ?? new AdrConfig();

                FileInfo rootConfiguration = new(configurationPath);
                string configurationDirectory = rootConfiguration.Directory?.FullName ?? Environment.CurrentDirectory;

                if (!string.IsNullOrWhiteSpace(config.Path))
                {
                    repositoryPath = Path.GetFullPath(Path.Combine(configurationDirectory, config.Path));
                }

                if (!string.IsNullOrWhiteSpace(config.TemplatePath))
                {
                    templatePath = Path.GetFullPath(Path.Combine(configurationDirectory, config.TemplatePath));
                }
            }
        }

        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            repositoryPath = Environment.CurrentDirectory;
        }

        repositoryPath = Path.GetFullPath(repositoryPath);

        return new AdrWorkspaceContext(repositoryPath, templatePath, configurationPath);
    }
}

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Abstractions;
using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;
using Endjin.Adr.Cli.Domain.Reporting;
using Endjin.Adr.Cli.Infrastructure.Workspace;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Endjin.Adr.Cli.Commands.Generate.Graph;

public class GenerateGraphCommand : AsyncCommand<GenerateGraphCommand.Settings>
{
    private readonly IAdrRepository repository;
    private readonly IAdrWorkspaceContextFactory workspaceFactory;

    public GenerateGraphCommand(IAdrRepository repository, IAdrWorkspaceContextFactory workspaceFactory)
    {
        this.repository = repository;
        this.workspaceFactory = workspaceFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AdrWorkspaceContext workspace = await this.workspaceFactory
            .CreateAsync(settings.Path)
            .ConfigureAwait(false);

        if (!Directory.Exists(workspace.RepositoryPath))
        {
            AnsiConsole.MarkupLine($"[red]The directory '{Markup.Escape(workspace.RepositoryPath)}' does not exist.[/]");
            return ReturnCodes.Error;
        }

        AdrRepositoryOptions options = new(workspace.RepositoryPath, SearchOption.TopDirectoryOnly);
        IReadOnlyList<Adr> documents = await this.repository.GetAllAsync(options).ConfigureAwait(false);

        string graph = AdrReportGenerator.BuildGraph(documents, settings.LinkPrefix, settings.LinkExtension);
        AnsiConsole.Write(new Text(graph ?? string.Empty));

        return ReturnCodes.Ok;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("--path <PATH>")]
        [Description("ADR repository path. Defaults to adr.config.json or current directory.")]
        public string Path { get; init; }

        [CommandOption("-p|--prefix <PREFIX>")]
        [Description("Link prefix to add before each ADR reference in the graph.")]
        public string LinkPrefix { get; init; }

        [CommandOption("-e|--extension <EXTENSION>")]
        [Description("File extension for generated links. Defaults to .html.")]
        public string LinkExtension { get; init; } = ".html";
    }
}

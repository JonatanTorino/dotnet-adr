using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Abstractions;
using Endjin.Adr.Cli.Commands.Shared;
using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;
using Endjin.Adr.Cli.Infrastructure.Workspace;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Endjin.Adr.Cli.Commands.List;

public class ListAdrCommand : AsyncCommand<ListAdrCommand.Settings>
{
    private readonly IAdrRepository repository;
    private readonly IAdrWorkspaceContextFactory workspaceFactory;

    public ListAdrCommand(IAdrRepository repository, IAdrWorkspaceContextFactory workspaceFactory)
    {
        this.repository = repository;
        this.workspaceFactory = workspaceFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Write(new FigletText("dotnet-adr").Color(Color.Green));

        AdrWorkspaceContext workspace = await this.workspaceFactory
            .CreateAsync(settings.Path)
            .ConfigureAwait(false);

        if (!Directory.Exists(workspace.RepositoryPath))
        {
            AnsiConsole.MarkupLine($"[red]The directory '{Markup.Escape(workspace.RepositoryPath)}' does not exist.[/]");
            return ReturnCodes.Error;
        }

        AdrRepositoryOptions options = new(workspace.RepositoryPath, settings.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        IReadOnlyList<Adr> documents = await this.repository.GetAllAsync(options).ConfigureAwait(false);

        if (documents.Count == 0)
        {
            AnsiConsole.MarkupLine($"No ADRs were found in [yellow]{Markup.Escape(workspace.RepositoryPath)}[/].");
            return ReturnCodes.Ok;
        }

        Table table = new Table().Border(TableBorder.Rounded).Title("[bold]Architecture Decision Records[/]");
        table.AddColumn("[grey]Record[/]");
        table.AddColumn("[grey]Title[/]");
        table.AddColumn("[grey]Status[/]");
        table.AddColumn("[grey]File[/]");

        foreach (Adr document in documents.OrderBy(doc => doc.RecordNumber).ThenBy(doc => doc.Path, System.StringComparer.OrdinalIgnoreCase))
        {
            string record = $"[aqua]{document.RecordNumber:D4}[/]";
            string title = Markup.Escape(document.Title ?? string.Empty);
            string status = Markup.Escape(AdrCommandUtilities.FormatStatusSummary(document.Status));
            string relativePath = Markup.Escape(Path.GetRelativePath(workspace.RepositoryPath, document.Path));

            table.AddRow(record, title, status, $"[grey]{relativePath}[/]");
        }

        AnsiConsole.Write(table);

        return ReturnCodes.Ok;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path <PATH>")]
        [Description("ADR repository path. Defaults to adr.config.json or current directory.")]
        public string Path { get; init; }

        [CommandOption("-r|--recursive")]
        [Description("Scan subdirectories for ADR files.")]
        public bool Recursive { get; init; }
    }
}

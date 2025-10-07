using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Abstractions;
using Endjin.Adr.Cli.Commands.Shared;
using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Formatting;
using Endjin.Adr.Cli.Domain.Models;
using Endjin.Adr.Cli.Infrastructure.Workspace;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Endjin.Adr.Cli.Commands.Link;

public class LinkAdrCommand : AsyncCommand<LinkAdrCommand.Settings>
{
    private readonly IAdrRepository repository;
    private readonly IAdrWorkspaceContextFactory workspaceFactory;

    public LinkAdrCommand(IAdrRepository repository, IAdrWorkspaceContextFactory workspaceFactory)
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

        IReadOnlyList<Adr> documents = await this.repository
            .GetAllAsync(new AdrRepositoryOptions(workspace.RepositoryPath))
            .ConfigureAwait(false);

        Adr source = AdrCommandUtilities.ResolveAdrReference(settings.Source, documents)
            ?? throw new CommandRuntimeException($"Unable to locate ADR '{settings.Source}'.");
        Adr target = AdrCommandUtilities.ResolveAdrReference(settings.Target, documents)
            ?? throw new CommandRuntimeException($"Unable to locate ADR '{settings.Target}'.");

        await UpdateStatusSectionAsync(source, target, settings.Link, workspace.RepositoryPath).ConfigureAwait(false);
        await UpdateStatusSectionAsync(target, source, settings.Reverse, workspace.RepositoryPath).ConfigureAwait(false);

        AnsiConsole.MarkupLine($"Linked [aqua]{source.RecordNumber:D4}[/] ⇄ [aqua]{target.RecordNumber:D4}[/] using " +
            $"[green]{Markup.Escape(settings.Link)}[/] / [green]{Markup.Escape(settings.Reverse)}[/].");

        return ReturnCodes.Ok;
    }

    private static async Task UpdateStatusSectionAsync(Adr origin, Adr target, string relationship, string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(relationship))
        {
            throw new CommandRuntimeException("Relationship descriptions must not be empty.");
        }

        string content = await File.ReadAllTextAsync(origin.Path).ConfigureAwait(false);
        List<string> entries = AdrMarkdownFormatter.ExtractStatusEntries(content).ToList();

        string relativePath = AdrCommandUtilities.GetMarkdownRelativePath(Path.GetDirectoryName(origin.Path) ?? repositoryPath, target.Path);
        string displayTitle = AdrCommandUtilities.FormatDisplayTitle(target.RecordNumber, target.Title);
        string entry = $"{relationship.Trim()} [{displayTitle}]({relativePath})";

        if (!entries.Any(existing => string.Equals(existing, entry, System.StringComparison.Ordinal)))
        {
            entries.Add(entry);
        }

        string updated = AdrMarkdownFormatter.WithStatusEntries(content, entries);
        await File.WriteAllTextAsync(origin.Path, updated).ConfigureAwait(false);
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path <PATH>")]
        [Description("ADR repository path. Defaults to adr.config.json or current directory.")]
        public string Path { get; init; }

        [CommandArgument(0, "<SOURCE>")]
        [Description("Record number, slug or title fragment of the origin ADR.")]
        public string Source { get; init; }

        [CommandArgument(1, "<LINK>")]
        [Description("Forward relationship description (e.g. 'Amends').")]
        public string Link { get; init; }

        [CommandArgument(2, "<TARGET>")]
        [Description("Record number, slug or title fragment of the target ADR.")]
        public string Target { get; init; }

        [CommandArgument(3, "<REVERSE>")]
        [Description("Reverse relationship description to add to the target ADR.")]
        public string Reverse { get; init; }
    }
}

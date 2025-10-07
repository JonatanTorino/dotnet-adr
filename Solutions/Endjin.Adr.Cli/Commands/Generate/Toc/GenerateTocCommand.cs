using System;
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

namespace Endjin.Adr.Cli.Commands.Generate.Toc;

public class GenerateTocCommand : AsyncCommand<GenerateTocCommand.Settings>
{
    private readonly IAdrRepository repository;
    private readonly IAdrWorkspaceContextFactory workspaceFactory;

    public GenerateTocCommand(IAdrRepository repository, IAdrWorkspaceContextFactory workspaceFactory)
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

        string intro = await LoadOptionalContentAsync(settings.Intro, workspace.RepositoryPath).ConfigureAwait(false);
        string outro = await LoadOptionalContentAsync(settings.Outro, workspace.RepositoryPath).ConfigureAwait(false);

        string toc = AdrReportGenerator.BuildTableOfContents(documents, intro, outro, settings.LinkPrefix);
        AnsiConsole.Write(new Text(toc ?? string.Empty));

        return ReturnCodes.Ok;
    }

    private static async Task<string> LoadOptionalContentAsync(string candidate, string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        string resolved = ResolveExistingPath(candidate, workspaceRoot);

        try
        {
            return await File.ReadAllTextAsync(resolved).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new CommandRuntimeException($"Unable to read '{candidate}'.", ex);
        }
    }

    private static string ResolveExistingPath(string candidate, string workspaceRoot)
    {
        if (Path.IsPathRooted(candidate))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }

            throw new CommandRuntimeException($"The file '{candidate}' could not be found.");
        }

        string currentDirectoryPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, candidate));

        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            string workspacePath = Path.GetFullPath(Path.Combine(workspaceRoot, candidate));

            if (File.Exists(workspacePath))
            {
                return workspacePath;
            }
        }

        throw new CommandRuntimeException($"The file '{candidate}' could not be found.");
    }

    public class Settings : CommandSettings
    {
        [CommandOption("--path <PATH>")]
        [Description("ADR repository path. Defaults to adr.config.json or current directory.")]
        public string Path { get; init; }

        [CommandOption("-i|--intro <INTRO>")]
        [Description("Path to a Markdown snippet to prepend before the table of contents.")]
        public string Intro { get; init; }

        [CommandOption("-o|--outro <OUTRO>")]
        [Description("Path to a Markdown snippet to append after the table of contents.")]
        public string Outro { get; init; }

        [CommandOption("-p|--prefix <PREFIX>")]
        [Description("Link prefix to add before each ADR file name.")]
        public string LinkPrefix { get; init; }
    }
}

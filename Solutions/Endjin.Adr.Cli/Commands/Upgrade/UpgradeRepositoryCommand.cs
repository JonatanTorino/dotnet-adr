using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Abstractions;
using Endjin.Adr.Cli.Commands.Shared;
using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;
using Endjin.Adr.Cli.Infrastructure.Workspace;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Endjin.Adr.Cli.Commands.Upgrade;

public class UpgradeRepositoryCommand : AsyncCommand<UpgradeRepositoryCommand.Settings>
{
    private static readonly Regex DateLineRegex = new("^Date:\\s*(?<value>.+)$", RegexOptions.Multiline);

    private static readonly string[] SupportedFormats =
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "dd-MM-yyyy",
        "MM-dd-yyyy",
    };

    private readonly IAdrRepository repository;
    private readonly IAdrWorkspaceContextFactory workspaceFactory;

    public UpgradeRepositoryCommand(IAdrRepository repository, IAdrWorkspaceContextFactory workspaceFactory)
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
            .GetAllAsync(new AdrRepositoryOptions(workspace.RepositoryPath, SearchOption.AllDirectories))
            .ConfigureAwait(false);

        if (documents.Count == 0)
        {
            AnsiConsole.MarkupLine($"No ADRs were found in [yellow]{Markup.Escape(workspace.RepositoryPath)}[/].");
            return ReturnCodes.Ok;
        }

        int updated = 0;

        foreach (Adr document in documents)
        {
            string content = await File.ReadAllTextAsync(document.Path).ConfigureAwait(false);
            string normalized = NormalizeDateLines(content);

            if (!string.Equals(content, normalized, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(document.Path, normalized).ConfigureAwait(false);
                updated++;

                string relativePath = AdrCommandUtilities.GetMarkdownRelativePath(workspace.RepositoryPath, document.Path);
                AnsiConsole.MarkupLine($"Updated [aqua]{document.RecordNumber:D4}[/] ([grey]{Markup.Escape(relativePath)}[/]).");
            }
        }

        if (updated == 0)
        {
            AnsiConsole.MarkupLine("All ADRs already use ISO-8601 dates.");
        }
        else
        {
            AnsiConsole.MarkupLine($"Normalised dates in [green]{updated}[/] ADR(s).");
        }

        return ReturnCodes.Ok;
    }

    internal static string NormalizeDateLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content ?? string.Empty;
        }

        return DateLineRegex.Replace(content, match =>
        {
            string value = match.Groups["value"].Value.Trim();
            if (TryParseDate(value, out DateTime parsed))
            {
                return $"Date: {parsed:yyyy-MM-dd}";
            }

            return match.Value;
        });
    }

    private static bool TryParseDate(string candidate, out DateTime date)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            date = default;
            return false;
        }

        if (DateTime.TryParseExact(candidate, SupportedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-p|--path <PATH>")]
        [Description("ADR repository path. Defaults to adr.config.json or current directory.")]
        public string Path { get; init; }
    }
}

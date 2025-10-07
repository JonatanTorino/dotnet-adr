// <copyright file="NewAdrCommand.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Abstractions;
using Endjin.Adr.Cli.Configuration;
using Endjin.Adr.Cli.Configuration.Contracts;
using Endjin.Adr.Cli.Templates;
using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;
using Endjin.Adr.Cli.Domain.Formatting;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Endjin.Adr.Cli.Commands.New;

public partial class NewAdrCommand : AsyncCommand<NewAdrCommand.Settings>
{
    private readonly ITemplateSettingsManager templateSettingsManager;
    private readonly IAppEnvironmentManager appEnvironmentManager;
    private readonly IConfigurationLocator configurationLocator;
    private readonly IAdrRepository adrRepository;

    public NewAdrCommand(
        ITemplateSettingsManager templateSettingsManager,
        IAppEnvironmentManager appEnvironmentManager,
        IConfigurationLocator configurationLocator,
        IAdrRepository adrRepository)
    {
        this.templateSettingsManager = templateSettingsManager;
        this.appEnvironmentManager = appEnvironmentManager;
        this.configurationLocator = configurationLocator;
        this.adrRepository = adrRepository;
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        AnsiConsole.Write(new FigletText("dotnet-adr").Color(Color.Green));

        try
        {
            string targetPath = string.Empty;
            string templatePath = null;

            // If the user hasn't specified the path to create the ADR
            if (!string.IsNullOrEmpty(settings.Path))
            {
                targetPath = settings.Path;
            }
            else
            {
                // We'll attempt to see if there's a configuration file in the root of the "project".
                // We'll make the assumption that the root of the "project" is defined by the presence of
                // a ".git" directory, otherwise we'll just use the current location the ADR tool was launched from.
                string rootConfiguration = this.configurationLocator.LocatedRootConfiguration();

                if (!string.IsNullOrEmpty(rootConfiguration))
                {
                    string configText = await File.ReadAllTextAsync(rootConfiguration).ConfigureAwait(false);

                    JsonSerializerOptions options = new()
                    {
                        PropertyNameCaseInsensitive = true,
                    };

                    AdrConfig config = JsonSerializer.Deserialize<AdrConfig>(configText, options);
                    FileInfo rootConfigurationFileInfo = new(rootConfiguration);

                    // The configuration path is relative to config file.
                    targetPath = Path.GetFullPath(Path.Combine(rootConfigurationFileInfo.Directory.FullName, config.Path));

                    if (!rootConfigurationFileInfo.Directory.Exists)
                    {
                        rootConfigurationFileInfo.Directory.Create();
                    }

                    templatePath = config.TemplatePath;
                }

                if (string.IsNullOrEmpty(targetPath))
                {
                    targetPath = Environment.CurrentDirectory;
                }
            }

            targetPath = Path.GetFullPath(targetPath);

            Directory.CreateDirectory(targetPath);

            IReadOnlyList<Adr> documents = await this.adrRepository
                .GetAllAsync(new AdrRepositoryOptions(targetPath))
                .ConfigureAwait(false);

            int recordNumber = documents.Count == 0 ? 1 : documents.Max(x => x.RecordNumber) + 1;
            DateTime recordDate = ResolveRecordDate(settings);
            string newContent = CreateNewDefaultTemplate(settings.Title, recordNumber, recordDate, this.templateSettingsManager, templatePath);

            Adr adr = new()
            {
                Content = newContent,
                RecordNumber = recordNumber,
                Title = settings.Title,
                Date = recordDate,
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Sections = Array.Empty<AdrSection>(),
                Links = Array.Empty<AdrLink>(),
                Status = AdrStatus.Empty,
            };

            string newFileName = adr.SafeFileName();
            string newFilePath = Path.Combine(targetPath, newFileName);
            string newDisplayTitle = FormatDisplayTitle(recordNumber, settings.Title);

            List<string> supersedeReferences = CollectSupersedeReferences(settings);
            List<Adr> supersededDocuments = ResolveAdrReferences(supersedeReferences, documents);

            IReadOnlyList<LinkSpecification> linkSpecifications = ParseLinkSpecifications(settings, documents);

            Dictionary<string, string> pendingUpdates = new(StringComparer.OrdinalIgnoreCase);

            List<string> newStatusEntries = new() { "Accepted" };

            foreach (Adr superseded in supersededDocuments)
            {
                string relativePathFromNew = GetMarkdownRelativePath(targetPath, superseded.Path);
                string targetDisplayTitle = FormatDisplayTitle(superseded.RecordNumber, superseded.Title);
                string supersedeLine = $"Supercedes [{targetDisplayTitle}]({relativePathFromNew})";

                if (!newStatusEntries.Any(entry => string.Equals(entry, supersedeLine, StringComparison.Ordinal)))
                {
                    newStatusEntries.Add(supersedeLine);
                }

                string supersededContent = await LoadDocumentContentAsync(superseded, pendingUpdates).ConfigureAwait(false);
                List<string> supersededStatusEntries = AdrMarkdownFormatter.ExtractStatusEntries(supersededContent).ToList();

                supersededStatusEntries.RemoveAll(entry => string.Equals(entry, "Accepted", StringComparison.Ordinal));

                string relativePathFromSuperseded = GetMarkdownRelativePath(Path.GetDirectoryName(superseded.Path) ?? targetPath, newFilePath);
                string supersededByLine = $"Superceded by [{newDisplayTitle}]({relativePathFromSuperseded})";

                if (!supersededStatusEntries.Any(entry => string.Equals(entry, supersededByLine, StringComparison.Ordinal)))
                {
                    supersededStatusEntries.Add(supersededByLine);
                }

                supersededContent = AdrMarkdownFormatter.WithStatusEntries(supersededContent, supersededStatusEntries);
                pendingUpdates[superseded.Path] = supersededContent;

                AnsiConsole.MarkupLine($"Superseded ADR Record: [aqua]{superseded.RecordNumber:D4}[/]");
            }

            foreach (LinkSpecification specification in linkSpecifications)
            {
                string relativePathFromNew = GetMarkdownRelativePath(targetPath, specification.Target.Path);
                string targetDisplayTitle = FormatDisplayTitle(specification.Target.RecordNumber, specification.Target.Title);
                string forwardLine = $"{specification.ForwardRelationship} [{targetDisplayTitle}]({relativePathFromNew})";

                if (!string.IsNullOrWhiteSpace(specification.ForwardRelationship) &&
                    !newStatusEntries.Any(entry => string.Equals(entry, forwardLine, StringComparison.Ordinal)))
                {
                    newStatusEntries.Add(forwardLine);
                }

                string targetContent = await LoadDocumentContentAsync(specification.Target, pendingUpdates).ConfigureAwait(false);
                List<string> targetStatusEntries = AdrMarkdownFormatter.ExtractStatusEntries(targetContent).ToList();

                string relativePathFromTarget = GetMarkdownRelativePath(Path.GetDirectoryName(specification.Target.Path) ?? targetPath, newFilePath);
                string reverseLine = $"{specification.ReverseRelationship} [{newDisplayTitle}]({relativePathFromTarget})";

                if (!targetStatusEntries.Any(entry => string.Equals(entry, reverseLine, StringComparison.Ordinal)))
                {
                    targetStatusEntries.Add(reverseLine);
                }

                targetContent = AdrMarkdownFormatter.WithStatusEntries(targetContent, targetStatusEntries);
                pendingUpdates[specification.Target.Path] = targetContent;

                AnsiConsole.MarkupLine($"Linked ADR Record: [aqua]{specification.Target.RecordNumber:D4}[/] ([green]{specification.ForwardRelationship}[/] ↔ [green]{specification.ReverseRelationship}[/])");
            }

            adr.Content = AdrMarkdownFormatter.WithStatusEntries(newContent, newStatusEntries);

            await File.WriteAllTextAsync(newFilePath, adr.Content).ConfigureAwait(false);

            foreach ((string path, string content) in pendingUpdates)
            {
                await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
            }

            AnsiConsole.MarkupLine($"""Created ADR Record: [aqua]"{settings.Title}"[/] in [yellow]{targetPath}[/]""");
        }
        catch (InvalidOperationException)
        {
            await this.appEnvironmentManager.SetFirstRunDesiredStateAsync().ConfigureAwait(false);
            await this.ExecuteAsync(context, settings).ConfigureAwait(false);
        }

        return ReturnCodes.Ok;
    }

    private static string CreateNewDefaultTemplate(string title, int recordNumber, DateTime date, ITemplateSettingsManager templateSettingsManager, string templatePath)
    {
        TemplateSettings templateSettings = templateSettingsManager.LoadSettings(nameof(TemplateSettings));

        if (templateSettings is null)
        {
            throw new InvalidOperationException("Couldn't load the template settings. Environment may not be initialised");
        }

        TemplatePackageDetail defaultTemplate = templateSettings.MetaData.Details.Find(x => x.FullPath == templateSettings.DefaultTemplate);

        string templateContents = File.ReadAllText(templatePath ?? defaultTemplate.FullPath);

        Regex yamlHeaderRegExp = YamlHeaderRegex();

        templateContents = NumberedTitleRegex().Replace(templateContents, $"# {recordNumber}. {title}");
        templateContents = yamlHeaderRegExp.Replace(templateContents, $"# {title}");

        string formattedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        templateContents = DateLineRegex().Replace(templateContents, $"Date: {formattedDate}", 1);
        templateContents = templateContents.Replace("{DATE}", formattedDate, StringComparison.Ordinal);

        return templateContents;
    }

    [GeneratedRegex(@"((?:^-{3})(?:.*\n)*(?:^-{3})\n# Title)", RegexOptions.Multiline)]
    private static partial Regex YamlHeaderRegex();

    [GeneratedRegex(@"^#\s*NUMBER\.\s*TITLE\s*$", RegexOptions.Multiline)]
    private static partial Regex NumberedTitleRegex();

    [GeneratedRegex(@"^Date:.*$", RegexOptions.Multiline)]
    private static partial Regex DateLineRegex();

    private static DateTime ResolveRecordDate(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Date))
        {
            if (DateTime.TryParse(settings.Date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime overrideDate))
            {
                return overrideDate.Date;
            }

            throw new CommandRuntimeException($"Unable to parse date '{settings.Date}'. Use ISO-8601 format (yyyy-MM-dd).");
        }

        string environmentDate = Environment.GetEnvironmentVariable("ADR_DATE");

        if (!string.IsNullOrWhiteSpace(environmentDate) &&
            DateTime.TryParse(environmentDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime envDate))
        {
            return envDate.Date;
        }

        return DateTime.Today;
    }

    private static List<string> CollectSupersedeReferences(Settings settings)
    {
        List<string> references = new();

        if (settings.Supersedes is not null)
        {
            references.AddRange(settings.Supersedes.Where(reference => !string.IsNullOrWhiteSpace(reference)));
        }

        if (settings.Id.HasValue)
        {
            references.Add(settings.Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        return references;
    }

    private static List<Adr> ResolveAdrReferences(IEnumerable<string> references, IReadOnlyList<Adr> documents)
    {
        List<Adr> resolved = new();

        foreach (string reference in references)
        {
            Adr match = ResolveAdrReference(reference, documents)
                ?? throw new CommandRuntimeException($"Unable to locate ADR '{reference}'.");

            if (!resolved.Any(candidate => candidate.Path == match.Path))
            {
                resolved.Add(match);
            }
        }

        return resolved;
    }

    private static IReadOnlyList<LinkSpecification> ParseLinkSpecifications(Settings settings, IReadOnlyList<Adr> documents)
    {
        if (settings.Links is null || settings.Links.Length == 0)
        {
            return Array.Empty<LinkSpecification>();
        }

        List<LinkSpecification> specifications = new();

        foreach (string specification in settings.Links)
        {
            if (string.IsNullOrWhiteSpace(specification))
            {
                continue;
            }

            string[] parts = specification.Split(':', 3, StringSplitOptions.TrimEntries);

            if (parts.Length < 3)
            {
                throw new CommandRuntimeException($"Link specification '{specification}' is invalid. Use TARGET:LINK:REVERSE format.");
            }

            Adr target = ResolveAdrReference(parts[0], documents)
                ?? throw new CommandRuntimeException($"Unable to locate ADR '{parts[0]}' for link specification '{specification}'.");

            string forwardRelationship = parts[1].Trim();
            string reverseRelationship = parts[2].Trim();

            if (string.IsNullOrWhiteSpace(forwardRelationship) || string.IsNullOrWhiteSpace(reverseRelationship))
            {
                throw new CommandRuntimeException($"Link specification '{specification}' must provide non-empty forward and reverse descriptions.");
            }

            specifications.Add(new LinkSpecification(target, forwardRelationship, reverseRelationship));
        }

        return specifications;
    }

    private static Adr ResolveAdrReference(string reference, IReadOnlyList<Adr> documents)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        string candidate = reference.Trim();

        if (int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out int recordNumber))
        {
            return documents.FirstOrDefault(doc => doc.RecordNumber == recordNumber);
        }

        Adr byFileName = documents.FirstOrDefault(doc =>
            !string.IsNullOrEmpty(doc.Path) &&
            Path.GetFileName(doc.Path).Contains(candidate, StringComparison.OrdinalIgnoreCase));

        if (byFileName is not null)
        {
            return byFileName;
        }

        return documents.FirstOrDefault(doc =>
            !string.IsNullOrWhiteSpace(doc.Title) &&
            doc.Title.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> LoadDocumentContentAsync(Adr document, Dictionary<string, string> pendingUpdates)
    {
        if (pendingUpdates.TryGetValue(document.Path, out string content))
        {
            return content;
        }

        string loaded = await File.ReadAllTextAsync(document.Path).ConfigureAwait(false);
        pendingUpdates[document.Path] = loaded;
        return loaded;
    }

    private static string GetMarkdownRelativePath(string fromDirectory, string toPath)
    {
        string from = Path.GetFullPath(fromDirectory);
        string to = Path.GetFullPath(toPath);
        string relative = Path.GetRelativePath(from, to);
        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string FormatDisplayTitle(int recordNumber, string title)
    {
        string trimmedTitle = title?.Trim() ?? string.Empty;

        if (Regex.IsMatch(trimmedTitle, @"^\d+\.\s"))
        {
            return trimmedTitle;
        }

        return string.IsNullOrWhiteSpace(trimmedTitle)
            ? recordNumber.ToString(CultureInfo.InvariantCulture)
            : $"{recordNumber}. {trimmedTitle}";
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<TITLE>")]
        [Description("Title of the ADR")]
        public string Title { get; set; }

        [CommandOption("-i|--id <RECORDNUMBER>")]
        [Description("Id of ADR to supersede.")]
        public int? Id { get; set; }

        [CommandOption("-s|--supersede <REFERENCE>")]
        [Description("Reference to an ADR to supersede. Can be repeated.")]
        public string[] Supersedes { get; set; } = Array.Empty<string>();

        [CommandOption("-l|--link <TARGET:LINK:REVERSE>")]
        [Description("Create reciprocal links to an existing ADR. Can be repeated.")]
        public string[] Links { get; set; } = Array.Empty<string>();

        [CommandOption("--date <DATE>")]
        [Description("Override the ADR date (ISO-8601). Defaults to today or ADR_DATE environment variable.")]
        public string Date { get; set; }

        [CommandOption("-p|--path <PATH>")]
        [Description("Path to create the ADR in")]
        public string Path { get; set; }
    }

    private record LinkSpecification(Adr Target, string ForwardRelationship, string ReverseRelationship);
}

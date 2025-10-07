using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Models;

using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using YamlDotNet.RepresentationModel;

namespace Endjin.Adr.Cli.Domain.Parsing;

/// <summary>
/// Parses ADR markdown using Markdig and YamlDotNet to extract structured information.
/// </summary>
public class MarkdigAdrParser : IAdrDocumentParser
{
    private static readonly Regex StatusLineRegex = new("^\\*\\s*Status:\\s*(?<value>.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LinkLineRegex = new("^\\*\\s*(?<relationship>[^\\[]+?)?\\s*\\[(?<text>[^\\]]+)\\]\\((?<target>[^\\)]+)\\)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex MarkdownLinkRegex = new("\\[(?<text>[^\\]]+)\\]\\((?<target>[^\\)]+)\\)", RegexOptions.Compiled);
    private static readonly Regex DateLineRegex = new("^Date:\\s*(?<value>.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] SupportedDateFormats =
    {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "MM/dd/yyyy",
    };
    private readonly MarkdownPipeline pipeline;

    public MarkdigAdrParser()
    {
        this.pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
    }

    public Adr Parse(AdrFileReference reference, string content)
    {
        MarkdownDocument document = Markdown.Parse(content ?? string.Empty, this.pipeline);

        IReadOnlyDictionary<string, string> metadata = ExtractMetadata(document);
        (string title, List<AdrSection> sections) = ExtractSections(document, content);

        AdrStatus status = ExtractStatus(sections);
        IReadOnlyList<AdrLink> links = ExtractLinks(sections);
        DateTime? date = ExtractDate(content, metadata);

        return new Adr
        {
            RecordNumber = reference.RecordNumber,
            Path = reference.FullPath,
            Title = !string.IsNullOrWhiteSpace(title) ? title : InferTitle(reference, metadata),
            Content = content,
            Sections = sections,
            Status = status,
            Links = links,
            Metadata = metadata,
            Date = date,
        };
    }

    private static string InferTitle(AdrFileReference reference, IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("Title", out string metadataTitle) && !string.IsNullOrWhiteSpace(metadataTitle))
        {
            return metadataTitle;
        }

        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(reference.FileName);
        int separatorIndex = fileNameWithoutExtension.IndexOf('-', StringComparison.Ordinal);

        return separatorIndex >= 0 && separatorIndex + 1 < fileNameWithoutExtension.Length
            ? fileNameWithoutExtension[(separatorIndex + 1)..].Replace('-', ' ')
            : fileNameWithoutExtension;
    }

    private static IReadOnlyDictionary<string, string> ExtractMetadata(MarkdownDocument document)
    {
        YamlFrontMatterBlock yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

        if (yamlBlock is null)
        {
            return new Dictionary<string, string>();
        }

        using StringReader reader = new(yamlBlock.Lines.ToString());
        YamlStream stream = new();
        stream.Load(reader);

        if (stream.Documents.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        YamlDocument yamlDocument = stream.Documents[0];

        if (yamlDocument.RootNode is not YamlMappingNode mapping)
        {
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
        {
            string key = entry.Key.ToString();
            string value = entry.Value.ToString();
            metadata[key] = value;
        }

        return metadata;
    }

    private static (string Title, List<AdrSection> Sections) ExtractSections(MarkdownDocument document, string content)
    {
        List<HeadingBlock> headings = document
            .Descendants<HeadingBlock>()
            .OrderBy(h => h.Span.Start)
            .ToList();

        string title = headings.FirstOrDefault(h => h.Level == 1) is HeadingBlock titleHeading
            ? ExtractInlineText(titleHeading.Inline)
            : null;

        List<AdrSection> sections = new();

        for (int index = 0; index < headings.Count; index++)
        {
            HeadingBlock heading = headings[index];

            if (heading.Level < 2)
            {
                continue;
            }

            int sectionStart = heading.Span.End + 1;
            int sectionEnd = content.Length;

            for (int nextIndex = index + 1; nextIndex < headings.Count; nextIndex++)
            {
                HeadingBlock next = headings[nextIndex];
                if (next.Level <= heading.Level)
                {
                    sectionEnd = next.Span.Start;
                    break;
                }
            }

            sectionStart = Math.Clamp(sectionStart, 0, content.Length);
            sectionEnd = Math.Clamp(sectionEnd, 0, content.Length);

            string sectionContent = sectionEnd > sectionStart
                ? content[sectionStart..sectionEnd].Trim()
                : string.Empty;

            sections.Add(new AdrSection(ExtractInlineText(heading.Inline), sectionContent, heading.Level));
        }

        return (title, sections);
    }

    private static AdrStatus ExtractStatus(IEnumerable<AdrSection> sections)
    {
        AdrSection statusSection = sections.FirstOrDefault(section => string.Equals(section.Name, "Status", StringComparison.OrdinalIgnoreCase));

        if (statusSection is null)
        {
            return AdrStatus.Empty;
        }

        Match match = StatusLineRegex.Match(statusSection.Content);
        if (!match.Success)
        {
            return new AdrStatus(statusSection.Content.Trim(), null);
        }

        string value = match.Groups["value"].Value.Trim();
        AdrLink reference = null;

        Match linkMatch = LinkLineRegex.Match(match.Value);
        if (linkMatch.Success)
        {
            string relationship = NormalizeRelationship(linkMatch.Groups["relationship"].Value, "Status");
            reference = new AdrLink(relationship, linkMatch.Groups["text"].Value, linkMatch.Groups["target"].Value);
        }

        string normalizedValue = MarkdownLinkRegex.Replace(value, string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue) && reference is not null)
        {
            normalizedValue = NormalizeRelationship(reference.Relationship, "Related");
        }

        return new AdrStatus(normalizedValue, reference);
    }

    private static DateTime? ExtractDate(string content, IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("Date", out string metadataValue) && TryParseDate(metadataValue, out DateTime metadataDate))
        {
            return metadataDate;
        }

        Match match = DateLineRegex.Match(content ?? string.Empty);

        if (match.Success && TryParseDate(match.Groups["value"].Value, out DateTime date))
        {
            return date;
        }

        return null;
    }

    private static bool TryParseDate(string candidate, out DateTime date)
    {
        string value = candidate?.Trim();

        if (string.IsNullOrEmpty(value))
        {
            date = default;
            return false;
        }

        if (DateTime.TryParseExact(value, SupportedDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
    }

    private static IReadOnlyList<AdrLink> ExtractLinks(IEnumerable<AdrSection> sections)
    {
        AdrSection linksSection = sections.FirstOrDefault(section => string.Equals(section.Name, "Links", StringComparison.OrdinalIgnoreCase));

        if (linksSection is null)
        {
            return Array.Empty<AdrLink>();
        }

        List<AdrLink> links = new();

        foreach (Match linkMatch in LinkLineRegex.Matches(linksSection.Content))
        {
            string relationship = NormalizeRelationship(linkMatch.Groups["relationship"].Value, "Related");

            string text = linkMatch.Groups["text"].Value.Trim();
            string target = linkMatch.Groups["target"].Value.Trim();

            links.Add(new AdrLink(relationship, text, target));
        }

        return links;
    }

    private static string ExtractInlineText(ContainerInline inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        AppendInlineText(inline, builder);
        return builder.ToString().Trim();
    }

    private static void AppendInlineText(Inline inline, StringBuilder builder)
    {
        for (Inline current = inline; current is not null; current = current.NextSibling)
        {
            switch (current)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case LinkInline link when link.IsImage:
                    builder.Append(link.Title ?? link.Url);
                    break;
                case LinkInline link:
                    if (link.FirstChild is not null)
                    {
                        AppendInlineText(link.FirstChild, builder);
                    }
                    else if (!string.IsNullOrEmpty(link.Title))
                    {
                        builder.Append(link.Title);
                    }
                    break;
                case EmphasisInline emphasis when emphasis.FirstChild is not null:
                    AppendInlineText(emphasis.FirstChild, builder);
                    break;
            }
        }
    }

    private static string NormalizeRelationship(string relationship, string fallback)
    {
        if (string.IsNullOrWhiteSpace(relationship))
        {
            return fallback;
        }

        string cleaned = relationship
            .Replace("Status:", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Status", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        cleaned = cleaned.Trim(':');

        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }
}

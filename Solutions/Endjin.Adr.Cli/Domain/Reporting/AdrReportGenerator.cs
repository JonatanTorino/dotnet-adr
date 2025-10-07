using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Endjin.Adr.Cli.Commands.Shared;
using Endjin.Adr.Cli.Domain.Formatting;
using Endjin.Adr.Cli.Domain.Models;

namespace Endjin.Adr.Cli.Domain.Reporting;

/// <summary>
/// Produces aggregate ADR reports such as tables of contents and graph visualisations.
/// </summary>
internal static partial class AdrReportGenerator
{
    private static readonly Regex MarkdownLinkRegex = MarkdownLinkRegexFactory();

    public static string BuildTableOfContents(IEnumerable<Adr> documents, string introContent, string outroContent, string linkPrefix)
    {
        string prefix = linkPrefix ?? string.Empty;
        IEnumerable<Adr> ordered = OrderDocuments(documents);

        StringBuilder builder = new();
        builder.AppendLine("# Architecture Decision Records");
        builder.AppendLine();

        AppendSection(builder, introContent, ensureTrailingBlankLine: true);

        foreach (Adr document in ordered)
        {
            string title = ResolveTitle(document);
            string linkTarget = ResolveLinkTarget(document, prefix);

            builder.Append("* [");
            builder.Append(title);
            builder.Append("](");
            builder.Append(linkTarget);
            builder.AppendLine(")");
        }

        AppendSection(builder, outroContent, ensureTrailingBlankLine: false, leadingBlankLine: true);

        return builder.ToString();
    }

    public static string BuildGraph(IEnumerable<Adr> documents, string linkPrefix, string linkExtension)
    {
        string prefix = linkPrefix ?? string.Empty;
        string extension = string.IsNullOrEmpty(linkExtension) ? ".html" : linkExtension;
        Adr[] ordered = OrderDocuments(documents).ToArray();

        StringBuilder builder = new();
        builder.AppendLine("digraph {");
        builder.AppendLine("  node [shape=plaintext];");
        builder.AppendLine();
        builder.AppendLine("  subgraph {");

        foreach (Adr document in ordered)
        {
            int number = Math.Max(0, document?.RecordNumber ?? 0);
            string title = EscapeDotLiteral(ResolveTitle(document));
            string linkTarget = EscapeDotLiteral(prefix + ResolveLinkBase(document) + extension);

            builder.Append("    _");
            builder.Append(number.ToString(CultureInfo.InvariantCulture));
            builder.Append(" [label=\"");
            builder.Append(title);
            builder.Append("\"; URL=\"");
            builder.Append(linkTarget);
            builder.AppendLine(""];");

            if (number > 1)
            {
                builder.Append("    _");
                builder.Append((number - 1).ToString(CultureInfo.InvariantCulture));
                builder.Append(" -> _");
                builder.Append(number.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine(" [style=\"dotted\", weight=1];");
            }
        }

        builder.AppendLine("  }");
        builder.AppendLine();

        HashSet<string> emitted = new(StringComparer.Ordinal);

        foreach (Adr document in ordered)
        {
            if (document is null)
            {
                continue;
            }

            int source = document.RecordNumber;

            foreach (AdrLink link in ExtractStatusLinks(document))
            {
                if (link.RecordNumber is null)
                {
                    continue;
                }

                string label = link.Relationship?.Trim();

                if (string.IsNullOrEmpty(label) || label.EndsWith(" by", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int target = link.RecordNumber.Value;
                string key = $"{source}->{target}:{label}";

                if (!emitted.Add(key))
                {
                    continue;
                }

                builder.Append("  _");
                builder.Append(source.ToString(CultureInfo.InvariantCulture));
                builder.Append(" -> _");
                builder.Append(target.ToString(CultureInfo.InvariantCulture));
                builder.Append(" [label=\"");
                builder.Append(EscapeDotLiteral(label));
                builder.AppendLine("\", weight=0];");
            }
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    private static IEnumerable<Adr> OrderDocuments(IEnumerable<Adr> documents)
    {
        return documents?
            .Where(doc => doc is not null)
            .OrderBy(doc => doc.RecordNumber)
            .ThenBy(doc => doc.Path, StringComparer.OrdinalIgnoreCase)
            ?? Array.Empty<Adr>();
    }

    private static string ResolveTitle(Adr document)
    {
        if (document is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(document.Title))
        {
            return document.Title.Trim();
        }

        return AdrCommandUtilities.FormatDisplayTitle(document.RecordNumber, document.Title);
    }

    private static string ResolveLinkTarget(Adr document, string linkPrefix)
    {
        string fileName = null;

        if (!string.IsNullOrWhiteSpace(document?.Path))
        {
            fileName = Path.GetFileName(document.Path);
        }

        if (string.IsNullOrEmpty(fileName))
        {
            fileName = document?.SafeFileName() ?? string.Empty;
        }

        return linkPrefix + fileName;
    }

    private static string ResolveLinkBase(Adr document)
    {
        if (!string.IsNullOrWhiteSpace(document?.Path))
        {
            return Path.GetFileNameWithoutExtension(document.Path);
        }

        string fileName = document?.SafeFileName() ?? string.Empty;
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static void AppendSection(StringBuilder builder, string content, bool ensureTrailingBlankLine, bool leadingBlankLine = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        string normalized = NormalizeLineEndings(content).TrimEnd('\n');

        if (leadingBlankLine)
        {
            builder.AppendLine();
        }

        builder.AppendLine(normalized);

        if (ensureTrailingBlankLine)
        {
            builder.AppendLine();
        }
    }

    private static IEnumerable<AdrLink> ExtractStatusLinks(Adr document)
    {
        if (document is null)
        {
            yield break;
        }

        IReadOnlyList<string> entries = AdrMarkdownFormatter.ExtractStatusEntries(document.Content);

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            Match match = MarkdownLinkRegex.Match(entry);

            if (!match.Success)
            {
                continue;
            }

            string prefix = entry[..match.Index]
                .Replace("*", string.Empty, StringComparison.Ordinal)
                .Trim();

            prefix = prefix.TrimEnd(':').Trim();

            string relationship = prefix.Length > 0 ? prefix : "Related";
            string text = match.Groups["text"].Value;
            string target = match.Groups["target"].Value;

            yield return new AdrLink(relationship, text, target);
        }
    }

    private static string EscapeDotLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private static string NormalizeLineEndings(string value)
    {
        return value?
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            ?? string.Empty;
    }

    [GeneratedRegex("\\[(?<text>[^\\]]+)\\]\\((?<target>[^\\)]+)\\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegexFactory();
}

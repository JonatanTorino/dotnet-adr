using System;

using Endjin.Adr.Cli.Domain.Models;
using Endjin.Adr.Cli.Domain.Reporting;

using NUnit.Framework;

namespace Endjin.AdrCli.Specs.Unit;

[TestFixture]
public class AdrReportGeneratorTests
{
    [Test]
    public void BuildTableOfContents_ProducesMarkdownWithIntroAndOutro()
    {
        Adr first = new()
        {
            RecordNumber = 1,
            Title = "First decision",
            Path = "/repo/docs/adr/0001-first.md",
        };

        Adr second = new()
        {
            RecordNumber = 2,
            Title = "Second decision",
            Path = "/repo/docs/adr/0002-second.md",
        };

        string intro = "Intro text";
        string outro = "Outro text";

        string toc = AdrReportGenerator.BuildTableOfContents(new[] { second, first }, intro, outro, "../");

        string expected = string.Join(Environment.NewLine, new[]
        {
            "# Architecture Decision Records",
            string.Empty,
            "Intro text",
            string.Empty,
            "* [First decision](../0001-first.md)",
            "* [Second decision](../0002-second.md)",
            string.Empty,
            "Outro text",
            string.Empty,
        });

        Assert.That(toc, Is.EqualTo(expected));
    }

    [Test]
    public void BuildGraph_ProducesDotOutputWithStatusLinks()
    {
        Adr first = new()
        {
            RecordNumber = 1,
            Title = "First decision",
            Path = "/repo/docs/adr/0001-first.md",
            Content = "## Status\nAccepted\n\n## Context\n",
        };

        Adr second = new()
        {
            RecordNumber = 2,
            Title = "Second decision",
            Path = "/repo/docs/adr/0002-second.md",
            Content = "## Status\nAccepted\n\nSupercedes [ADR-0001](0001-first.md)\n\n## Context\n",
        };

        string graph = AdrReportGenerator.BuildGraph(new[] { first, second }, "./", ".html");

        string expected = string.Join(Environment.NewLine, new[]
        {
            "digraph {",
            "  node [shape=plaintext];",
            string.Empty,
            "  subgraph {",
            "    _1 [label=\"First decision\"; URL=\"./0001-first.html\"];",
            "    _2 [label=\"Second decision\"; URL=\"./0002-second.html\"];",
            "    _1 -> _2 [style=\"dotted\", weight=1];",
            "  }",
            string.Empty,
            "  _2 -> _1 [label=\"Supercedes\", weight=0];",
            "}",
            string.Empty,
        });

        Assert.That(graph, Is.EqualTo(expected));
    }
}

using Endjin.Adr.Cli.Commands.Shared;
using Endjin.Adr.Cli.Domain.Models;

using NUnit.Framework;

namespace Endjin.AdrCli.Specs.Unit;

[TestFixture]
public class AdrCommandUtilitiesTests
{
    [Test]
    public void FormatStatusSummary_ReturnsEmpty_WhenStatusIsNull()
    {
        string summary = AdrCommandUtilities.FormatStatusSummary(null);
        Assert.That(summary, Is.Empty);
    }

    [Test]
    public void FormatStatusSummary_ReturnsValue_WhenStatusHasNoReference()
    {
        AdrStatus status = new("Accepted", null);
        string summary = AdrCommandUtilities.FormatStatusSummary(status);
        Assert.That(summary, Is.EqualTo("Accepted"));
    }

    [Test]
    public void FormatStatusSummary_CombinesRelationshipAndTarget_WhenReferenceExists()
    {
        AdrLink link = new("Superseded by", "ADR-0002", "0002-example.md");
        AdrStatus status = new(string.Empty, link);

        string summary = AdrCommandUtilities.FormatStatusSummary(status);

        Assert.That(summary, Is.EqualTo("Superseded by → ADR-0002"));
    }
}

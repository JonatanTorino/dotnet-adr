using Endjin.Adr.Cli.Commands.Upgrade;

using NUnit.Framework;

namespace Endjin.AdrCli.Specs.Unit;

[TestFixture]
public class UpgradeRepositoryCommandTests
{
    [Test]
    public void NormalizeDateLines_RewritesSupportedFormats()
    {
        string content = """# Title

Date: 12/05/2023

## Status
Accepted
""";

        string normalized = UpgradeRepositoryCommand.NormalizeDateLines(content);

        StringAssert.Contains("Date: 2023-05-12", normalized);
    }

    [Test]
    public void NormalizeDateLines_IgnoresInvalidDates()
    {
        string content = "Date: not-a-date";

        string normalized = UpgradeRepositoryCommand.NormalizeDateLines(content);

        Assert.That(normalized, Is.EqualTo(content));
    }
}

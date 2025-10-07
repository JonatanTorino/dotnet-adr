using System;
using System.IO;
using System.Threading.Tasks;

using Endjin.Adr.Cli.Configuration.Contracts;
using Endjin.Adr.Cli.Infrastructure.Workspace;

using NUnit.Framework;

namespace Endjin.AdrCli.Specs.Unit;

[TestFixture]
public class AdrWorkspaceContextFactoryTests
{
    [Test]
    public async Task UsesConfigurationPathsWhenPresent()
    {
        string tempRoot = CreateTemporaryDirectory();
        try
        {
            string repositoryRelative = Path.Combine("docs", "adr");
            string templateRelative = Path.Combine("docs", "templates", "adr.md");

            string repositoryFullPath = Path.Combine(tempRoot, repositoryRelative);
            string templateFullPath = Path.Combine(tempRoot, templateRelative);

            Directory.CreateDirectory(repositoryFullPath);
            Directory.CreateDirectory(Path.GetDirectoryName(templateFullPath)!);
            File.WriteAllText(templateFullPath, "# template");

            string configPath = Path.Combine(tempRoot, "adr.config.json");
            string configContent = "{\n"
                + $"  \"path\": \"./{repositoryRelative.Replace("\\", "/")}\",\n"
                + $"  \"templatePath\": \"./{templateRelative.Replace("\\", "/")}\"\n"
                + "}\n";

            File.WriteAllText(configPath, configContent);

            StubConfigurationLocator locator = new(configPath);
            AdrWorkspaceContextFactory factory = new(locator);

            AdrWorkspaceContext context = await factory.CreateAsync(null).ConfigureAwait(false);

            Assert.That(context.RepositoryPath, Is.EqualTo(Path.GetFullPath(repositoryFullPath)));
            Assert.That(context.TemplatePath, Is.EqualTo(Path.GetFullPath(templateFullPath)));
            Assert.That(context.ConfigurationPath, Is.EqualTo(configPath));
            Assert.That(locator.CallCount, Is.EqualTo(1));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Test]
    public async Task PathOptionShortCircuitsConfigurationLookup()
    {
        StubConfigurationLocator locator = new(Path.Combine(Path.GetTempPath(), "adr.config.json"));
        AdrWorkspaceContextFactory factory = new(locator);

        string customPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(customPath);

        try
        {
            AdrWorkspaceContext context = await factory.CreateAsync(customPath).ConfigureAwait(false);

            Assert.That(context.RepositoryPath, Is.EqualTo(Path.GetFullPath(customPath)));
            Assert.That(context.TemplatePath, Is.Null);
            Assert.That(locator.CallCount, Is.EqualTo(0));
        }
        finally
        {
            Directory.Delete(customPath, true);
        }
    }

    [Test]
    public async Task FallsBackToCurrentDirectoryWhenNoConfigurationExists()
    {
        string originalDirectory = Environment.CurrentDirectory;
        string tempRoot = CreateTemporaryDirectory();

        try
        {
            Environment.CurrentDirectory = tempRoot;

            StubConfigurationLocator locator = new(null);
            AdrWorkspaceContextFactory factory = new(locator);

            AdrWorkspaceContext context = await factory.CreateAsync(null).ConfigureAwait(false);

            Assert.That(context.RepositoryPath, Is.EqualTo(Path.GetFullPath(tempRoot)));
            Assert.That(context.TemplatePath, Is.Null);
            Assert.That(context.ConfigurationPath, Is.Null);
            Assert.That(locator.CallCount, Is.EqualTo(1));
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            Directory.Delete(tempRoot, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class StubConfigurationLocator : IConfigurationLocator
    {
        public StubConfigurationLocator(string configurationPath)
        {
            this.ConfigurationPath = configurationPath;
        }

        public string ConfigurationPath { get; }

        public int CallCount { get; private set; }

        public string LocatedRootConfiguration()
        {
            this.CallCount++;
            return this.ConfigurationPath;
        }
    }
}

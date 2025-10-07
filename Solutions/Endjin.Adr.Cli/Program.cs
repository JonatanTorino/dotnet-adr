// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using System.Threading.Tasks;

using Endjin.Adr.Cli.Commands.Init;
using Endjin.Adr.Cli.Commands.Link;
using Endjin.Adr.Cli.Commands.List;
using Endjin.Adr.Cli.Commands.New;
using Endjin.Adr.Cli.Commands.Templates.Default;
using Endjin.Adr.Cli.Commands.Templates.List;
using Endjin.Adr.Cli.Commands.Templates.Package;
using Endjin.Adr.Cli.Commands.Upgrade;
using Endjin.Adr.Cli.Extensions;
using Endjin.Adr.Cli.Infrastructure.Injection;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;

namespace Endjin.Adr.Cli;

/// <summary>
/// A CLI tool for creating and managing Architectural Decision Records.
/// </summary>
public static class Program
{
    /// <summary>
    /// Architectural Decision Records .NET Global Tool.
    /// </summary>
    /// <param name="args">Command Line Switches.</param>
    /// <returns>Exit Code.</returns>
    public static Task<int> Main(string[] args)
    {
        ServiceCollection registrations = new();
        registrations.ConfigureDependencies();

        TypeRegistrar registrar = new(registrations);
        CommandApp app = new(registrar);

        app.Configure(config =>
        {
            config.Settings.PropagateExceptions = false;
            config.CaseSensitivity(CaseSensitivity.None);
            config.SetApplicationName("adr");

            config.AddExample("templates", "package", "set", "adr.templates");
            config.AddExample("templates", "package", "install");

            config.AddExample("new", "\"Integration of an Event Store\"");
            config.AddExample("new", "\"Integration of an Event Store\"", "-i", "1");
            config.AddExample("new", "\"Integration of an Event Store\"", "-p", @"c:\repo\my-project\docs\adr");
            config.AddExample("new", "\"Integration of an Event Store\"", "-i", "1", "-p", @"c:\repo\my-project\docs\adr");

            config.AddExample("templates", "package", "set", "thirdparty.adr.templates");

            config.AddExample("templates", "list");
            config.AddExample("templates", "set");
            config.AddExample("templates", "show");

            config.AddExample("templates", "package", "set");
            config.AddExample("templates", "package", "show");
            config.AddExample("templates", "package", "install");
            config.AddExample("templates", "package", "update");

            config.AddExample("environment", "init");
            config.AddExample("environment", "reset");

            config.AddExample("list");
            config.AddExample("list", "--recursive");
            config.AddExample("link", "12", "Amends", "10", "Amended by");
            config.AddExample("upgrade", "repository");

            config.AddCommand<NewAdrCommand>("new")
                  .WithDescription("Creates a new Architectural Decision Record, from the default ADR Template.");

            config.AddCommand<ListAdrCommand>("list")
                  .WithDescription("Lists the Architecture Decision Records discovered in the repository.");

            config.AddCommand<LinkAdrCommand>("link")
                  .WithDescription("Creates reciprocal links between existing Architectural Decision Records.");

            config.AddBranch("environment", environment =>
            {
                environment.SetDescription("Initialize and reset the local environment");
                environment.AddCommand<EnvironmentInitCommand>("init")
                           .WithDescription("Initializes a new ADR repository.");
                environment.AddCommand<EnvironmentResetCommand>("reset")
                           .WithDescription("Resets the state of the ADR repository.");
            });

            config.AddBranch("upgrade", upgrade =>
            {
                upgrade.SetDescription("Upgrade ADR repository assets to the latest format");
                upgrade.AddCommand<UpgradeRepositoryCommand>("repository")
                       .WithDescription("Normalises ADR documents (dates, metadata) to the latest supported conventions.");
            });

            config.AddBranch("templates", templates =>
            {
                templates.SetDescription("list and select local templates, and install, & update template packages from nuget.org");
                templates.AddCommand<TemplatesDefaultShowCommand>("show").WithDescription("Show the active ADR template");
                templates.AddCommand<TemplatesSetCommand>("set").WithDescription("Set the active ADR template");
                templates.AddCommand<TemplatesListCommand>("list").WithDescription("List the installed ADR templates");

                templates.AddBranch("package", package =>
                {
                    package.SetDescription("Show, set, update, template packages");
                    package.AddCommand<TemplatesPackageSetCommand>("set").WithDescription("set the active ADR template package");
                    package.AddCommand<TemplatesPackageShowCommand>("show").WithDescription("show all available ADR template packages");
                    package.AddCommand<TemplatesPackageUpdateCommand>("update").WithDescription("update the specified ADR template package");
                    package.AddCommand<TemplatesPackageUpdateCommand>("install").WithDescription("install the specified ADR template package");
                });
            });
            #if DEBUG
            config.PropagateExceptions();
            #endif
            config.ValidateExamples();
        });

        return app.RunAsync(args);
    }
}
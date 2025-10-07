// <copyright file="ServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Endjin.Adr.Cli.Configuration;
using Endjin.Adr.Cli.Configuration.Contracts;
using Endjin.Adr.Cli.Domain.Contracts;
using Endjin.Adr.Cli.Domain.Parsing;
using Endjin.Adr.Cli.Domain.Storage;
using Endjin.Adr.Cli.Templates;

using Microsoft.Extensions.DependencyInjection;

namespace Endjin.Adr.Cli.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureDependencies(this ServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IAppEnvironment, FileSystemRoamingProfileAppEnvironment>();
        serviceCollection.AddTransient<IAppEnvironmentManager, AppEnvironmentManager>();
        serviceCollection.AddTransient<ITemplatePackageManager, NuGetTemplatePackageManager>();
        serviceCollection.AddTransient<ITemplateSettingsManager, TemplateSettingsManager>();
        serviceCollection.AddTransient<IConfigurationLocator, FileSystemConfigurationLocator>();
        serviceCollection.AddSingleton<IAdrDocumentParser, MarkdigAdrParser>();
        serviceCollection.AddTransient<IAdrFileLocator, FileSystemAdrLocator>();
        serviceCollection.AddTransient<IAdrDocumentReader, FileSystemAdrDocumentReader>();
        serviceCollection.AddTransient<IAdrRepository, AdrRepository>();
    }
}

using Ciir.Application.InputResolution;
using Ciir.Application.Ports;
using Ciir.Application.ProjectDiscovery;
using Ciir.Application.Reporting;
using Ciir.Application.UseCases;
using Ciir.Cli.Presentation;
using Ciir.CSharp.Workspace;
using Ciir.Serialization.Writing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ciir.Cli.Composition;

/// <summary>The composition root: wires the C# analyzer and serialization adapters into the application core.</summary>
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCiir(this IServiceCollection services, bool verbose, bool noProgress)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Warning);
        });

        services.AddSingleton<IInputResolver, InputResolver>();
        services.AddSingleton<ISolutionProjectLister, MsBuildSolutionProjectLister>();
        services.AddSingleton<ProjectDiscoveryService>();
        services.AddSingleton<ICodeAnalyzer, CSharpCodeAnalyzer>();
        services.AddSingleton<ICiirWriterFactory, JsonlCiirWriterFactory>();
        services.AddSingleton<IAnalysisArtifactWriter, AnalysisArtifactWriter>();
        services.AddSingleton<IAnalysisReporter, AnalysisReporter>();
        services.AddSingleton<IAnalysisProgressReporter>(
            noProgress ? NullAnalysisProgressReporter.Instance : new ConsoleProgressReporter());
        services.AddSingleton<AnalyzeInputHandler>();

        return services;
    }
}

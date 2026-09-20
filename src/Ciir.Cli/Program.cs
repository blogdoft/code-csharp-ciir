using Ciir.Application.Model;
using Ciir.Application.UseCases;
using Ciir.Cli.Composition;
using Ciir.Cli.Presentation;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

var pathArgument = new Argument<string>("path")
{
    Description = "A solution (.sln/.slnx), a project (.csproj), or a directory to scan.",
};

var outputOption = new Option<string>("--output")
{
    Description = "The directory CIIR output artifacts are written to.",
    DefaultValueFactory = _ => "./ciir-output",
};

var verboseOption = new Option<bool>("--verbose") { Description = "Emit verbose diagnostic logging." };
var noProgressOption = new Option<bool>("--no-progress") { Description = "Suppress progress reporting." };
var noBannerOption = new Option<bool>("--no-banner") { Description = $"Suppress the splash screen (also suppressed when {SplashScreen.NoLogoEnvironmentVariable}=1 or true)." };
var includeSourceOption = new Option<bool>("--include-source") { Description = "Embed literal source text in the output." };
var failOnErrorOption = new Option<bool>("--fail-on-error") { Description = "Exit with a non-zero code if any project fails to analyze." };

var rootCommand = new RootCommand("Statically analyzes C# source code and produces a CIIR (Code Intelligence Intermediate Representation) artifact.")
{
    pathArgument,
    outputOption,
    verboseOption,
    noProgressOption,
    noBannerOption,
    includeSourceOption,
    failOnErrorOption,
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    if (!SplashScreen.IsSuppressed(parseResult.GetValue(noBannerOption), Environment.GetEnvironmentVariable(SplashScreen.NoLogoEnvironmentVariable)))
    {
        SplashScreen.Write(Console.Out, GeneratorVersion.Current);
    }

    var verbose = parseResult.GetValue(verboseOption);
    var noProgress = parseResult.GetValue(noProgressOption);

    var services = new ServiceCollection().AddCiir(verbose, noProgress);
    await using var provider = services.BuildServiceProvider();

    var command = new AnalyzeInputCommand
    {
        Path = parseResult.GetRequiredValue(pathArgument),
        Options = new AnalysisOptions
        {
            OutputPath = Path.GetFullPath(parseResult.GetRequiredValue(outputOption)),
            IncludeSource = parseResult.GetValue(includeSourceOption),
            FailOnError = parseResult.GetValue(failOnErrorOption),
            Verbose = verbose,
            NoProgress = noProgress,
        },
    };

    var handler = provider.GetRequiredService<AnalyzeInputHandler>();
    var result = await handler.ExecuteAsync(command, cancellationToken);

    if (result.ErrorMessage is { } errorMessage)
    {
        Console.Error.WriteLine(errorMessage);
    }

    if (result.Report is { Success: false } report)
    {
        foreach (var error in report.Errors)
        {
            Console.Error.WriteLine($"{error.Project}: {error.Message} ({error.Category})");
        }
    }

    return (int)result.ExitCode;
});

var parsed = rootCommand.Parse(args);
if (parsed.Errors.Count > 0)
{
    foreach (var error in parsed.Errors)
    {
        Console.Error.WriteLine(error.Message);
    }

    return (int)AnalysisExitCode.InvalidInput;
}

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

return await parsed.InvokeAsync(new InvocationConfiguration(), cancellationTokenSource.Token);

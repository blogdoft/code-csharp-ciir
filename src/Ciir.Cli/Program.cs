using BlogDoFT.Libs.ResultPattern;
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

var sendOption = new Option<string?>("--send", "-s")
{
    Description = $"Send the generated ciir.jsonl to the code-ciir-indexer. The value is the indexer's base URL; when omitted, {SendOptionsFactory.BaseUrlEnvironmentVariable} is used. Put <path> before -s.",
    Arity = ArgumentArity.ZeroOrOne,
};
var projectIdOption = new Option<string?>("--projectId", "-pi") { Description = "With --send: the id (GUID) of the project registered in the indexer. Required." };
var clientIdOption = new Option<string?>("--clientId", "-ci") { Description = "With --send: the Keycloak client id, used with --clientSecret to negotiate a token." };
var clientSecretOption = new Option<string?>("--clientSecret", "-cs") { Description = "With --send: the Keycloak client secret. Only used together with --clientId." };
var tokenOption = new Option<string?>("--token", "-t") { Description = "With --send: an access token sent as the Bearer token (takes precedence over --clientId/--clientSecret)." };

var rootCommand = new RootCommand("Statically analyzes C# source code and produces a CIIR (Code Intelligence Intermediate Representation) artifact.")
{
    pathArgument,
    outputOption,
    verboseOption,
    noProgressOption,
    noBannerOption,
    includeSourceOption,
    failOnErrorOption,
    sendOption,
    projectIdOption,
    clientIdOption,
    clientSecretOption,
    tokenOption,
};

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    if (!SplashScreen.IsSuppressed(parseResult.GetValue(noBannerOption), Environment.GetEnvironmentVariable(SplashScreen.NoLogoEnvironmentVariable)))
    {
        SplashScreen.Write(Console.Out, GeneratorVersion.Current);
    }

    var sendResult = ResolveSendOptions(parseResult);
    if (sendResult is { IsFailure: true })
    {
        Console.Error.WriteLine(sendResult.Failure.Message);
        return (int)AnalysisExitCode.InvalidInput;
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
            Send = sendResult?.Value,
        },
    };

    var handler = provider.GetRequiredService<AnalyzeInputHandler>();
    var result = await handler.ExecuteAsync(command, cancellationToken);

    if (result.ErrorMessage is { } errorMessage)
    {
        Console.Error.WriteLine(errorMessage);
    }

    if (result.Upload is { } upload)
    {
        Console.Out.WriteLine($"Sent ciir.jsonl to the indexer (uploadId: {upload.UploadId}, status: {upload.Status}).");
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

Result<SendOptions>? ResolveSendOptions(ParseResult parseResult)
{
    if (parseResult.GetResult(sendOption) is null)
    {
        return null;
    }

    var token = parseResult.GetValue(tokenOption);
    var clientId = parseResult.GetValue(clientIdOption);
    var clientSecret = parseResult.GetValue(clientSecretOption);
    if (SendOptionsFactory.HasIncompleteClientCredentials(token, clientId, clientSecret))
    {
        Console.Error.WriteLine("Warning: --clientId and --clientSecret are both required to negotiate a token; sending without authentication.");
    }

    return SendOptionsFactory.Create(
        parseResult.GetValue(sendOption),
        Environment.GetEnvironmentVariable(SendOptionsFactory.BaseUrlEnvironmentVariable),
        parseResult.GetValue(projectIdOption),
        token,
        clientId,
        clientSecret);
}

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

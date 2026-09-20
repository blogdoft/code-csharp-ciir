using System.Text;

namespace Ciir.Cli.Presentation;

/// <summary>
/// The terminal splash screen shown before an analysis run: the tool name and "BlogDoFT" in ASCII
/// art, a link to the blog, and the prerequisite notices. Only ASCII characters and no ANSI
/// escapes, so it renders the same on any console and in CI logs.
/// </summary>
internal static class SplashScreen
{
    /// <summary>The name of the environment variable that suppresses the splash screen.</summary>
    public const string NoLogoEnvironmentVariable = "CIIR_NOLOGO";

    /// <summary>The blog link shown below the banner.</summary>
    public const string BlogUrl = "https://www.blogdoft.com.br/";

    private static readonly string[] ToolNameArt =
    [
        @"  ____  ___  ___  ____",
        @" / ___||_ _||_ _||  _ \",
        @"| |     | |  | | | |_) |",
        @"| |___  | |  | | |  _ <",
        @" \____||___||___||_| \_\",
    ];

    private static readonly string[] BlogDoFtArt =
    [
        @" ____   _                 ____          _____  _____",
        @"| __ ) | |  ___    __ _  |  _ \   ___  |  ___||_   _|",
        @"|  _ \ | | / _ \  / _` | | | | | / _ \ | |_     | |",
        @"| |_) || || (_) || (_| | | |_| || (_) ||  _|    | |",
        @"|____/ |_| \___/  \__, | |____/  \___/ |_|      |_|",
        @"                  |___/",
    ];

    private static readonly string[] Notices =
    [
        "Requires the .NET 10 SDK (the runtime alone is not enough).",
        "Analyzed projects must be restored first (dotnet restore); ciir will not.",
        "A global.json in the analyzed repository may select a different SDK.",
    ];

    /// <summary>Whether the splash screen should be suppressed (<c>--no-banner</c> or <c>CIIR_NOLOGO=1|true</c>).</summary>
    /// <param name="noBannerOption">Whether <c>--no-banner</c> was passed.</param>
    /// <param name="noLogoEnvironmentValue">The value of <see cref="NoLogoEnvironmentVariable"/>, if set.</param>
    public static bool IsSuppressed(bool noBannerOption, string? noLogoEnvironmentValue) =>
        noBannerOption
        || string.Equals(noLogoEnvironmentValue?.Trim(), "1", StringComparison.Ordinal)
        || string.Equals(noLogoEnvironmentValue?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Renders the splash screen: banner, blog link, then notices, in that order, followed by a
    /// blank line that separates it from the tool's own output.
    /// </summary>
    /// <param name="version">The tool version shown under the banner.</param>
    public static string Render(string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        var builder = new StringBuilder();

        AppendLines(builder, ToolNameArt);
        builder.Append("  Code Intelligence IR - v").AppendLine(version);
        builder.AppendLine();

        AppendLines(builder, BlogDoFtArt);
        builder.AppendLine();

        builder.Append("  ").AppendLine(BlogUrl);
        builder.AppendLine();

        builder.AppendLine("  Before you start:");
        foreach (var notice in Notices)
        {
            builder.Append("   - ").AppendLine(notice);
        }

        builder.AppendLine();
        return builder.ToString();
    }

    /// <summary>Writes the splash screen to <paramref name="writer"/>.</summary>
    /// <param name="writer">The destination writer (the console's standard output).</param>
    /// <param name="version">The tool version shown under the banner.</param>
    public static void Write(TextWriter writer, string version)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.Write(Render(version));
    }

    private static void AppendLines(StringBuilder builder, string[] lines)
    {
        foreach (var line in lines)
        {
            builder.AppendLine(line);
        }
    }
}

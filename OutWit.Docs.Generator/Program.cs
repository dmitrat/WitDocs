using CommandLine;
using OutWit.Docs.Generator.Commands;

namespace OutWit.Docs.Generator;

/// <summary>
/// Entry point for the OutWit.Docs content generator CLI.
/// </summary>
internal static class Program
{
    #region Functions

    public static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<GeneratorOptions>(args)
            .MapResult(
                async (GeneratorOptions options) => await RunGeneratorAsync(options),
                _ => Task.FromResult(1));
    }

    private static async Task<int> RunGeneratorAsync(GeneratorOptions options)
    {
        Console.WriteLine("OutWit.Docs Content Generator");
        Console.WriteLine($"  Site Path: {options.SitePath}");
        Console.WriteLine($"  Output Path: {options.GetOutputPath()}");
        Console.WriteLine();

        var config = new GeneratorConfig
        {
            SitePath = options.SitePath,
            OutputPath = options.GetOutputPath(),
            SiteUrl = options.SiteUrl,
            GenerateSitemap = !options.SkipSitemap,
            GenerateSearchIndex = !options.SkipSearch,
            GenerateRssFeed = !options.SkipRss,
            GenerateStaticPages = !options.SkipStatic,
            GenerateOgImages = options.GenerateOgImages,
            ForceOgImages = options.ForceOgImages,
            SearchContentMaxLength = options.SearchContentMaxLength,
            HostingProvider = options.HostingProvider
        };

        var generator = new ContentGenerator(config);
        try
        {
            await generator.GenerateAllAsync();
        }
        catch (Exception ex)
        {
            // Fail with a clean, actionable message and a non-zero exit code so the
            // MSBuild target stops the build instead of surfacing a raw stack trace.
            Console.Error.WriteLine();
            Console.Error.WriteLine($"ERROR: Content generation failed: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }

        return 0;
    }

    #endregion
}

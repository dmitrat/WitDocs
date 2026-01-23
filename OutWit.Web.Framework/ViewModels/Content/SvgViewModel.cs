using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using OutWit.Common.MVVM.Blazor.ViewModels;

namespace OutWit.Web.Framework.ViewModels.Content;

public partial class SvgViewModel : ViewModelBase
{
    #region Fields

    private string? m_loadedSvgContent;
    private string? m_errorMessage;
    private bool m_isLoading;

    #endregion

    #region Lifecycle

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await LoadSvgAsync();
    }

    #endregion

    #region Functions

    private async Task LoadSvgAsync()
    {
        if (string.IsNullOrEmpty(Src))
        {
            m_errorMessage = "SVG source not specified";
            return;
        }

        try
        {
            m_isLoading = true;
            StateHasChanged();

            var resolvedPath = GetResolvedSrc();
            var rawSvg = await Http.GetStringAsync(resolvedPath);

            m_loadedSvgContent = ProcessSvg(rawSvg);
            m_errorMessage = null;
        }
        catch (HttpRequestException ex)
        {
            m_errorMessage = $"Failed to load SVG: {ex.Message}";
            m_loadedSvgContent = null;
            Console.WriteLine($"Error loading SVG {Src}: {ex.Message}");
        }
        catch (Exception ex)
        {
            m_errorMessage = "Error processing SVG";
            m_loadedSvgContent = null;
            Console.WriteLine($"Error processing SVG {Src}: {ex.Message}");
        }
        finally
        {
            m_isLoading = false;
            StateHasChanged();
        }
    }

    protected string GetResolvedSrc()
    {
        if (Src.StartsWith("./") && !string.IsNullOrEmpty(BasePath))
        {
            return $"{BasePath.TrimStart('/')}/{Src[2..]}";
        }
        return Src;
    }

    private string ProcessSvg(string rawSvg)
    {
        var sanitized = SanitizeSvg(rawSvg);
        var modified = ModifySvgRoot(sanitized);
        return modified;
    }

    private string SanitizeSvg(string svg)
    {
        // Remove <script> tags and their content
        svg = ScriptTagRegex().Replace(svg, "");

        // Remove on* event handlers (onclick, onload, onerror, onmouseover, etc.)
        svg = EventHandlerRegex().Replace(svg, "");

        // Remove <foreignObject> elements (can contain arbitrary HTML/JS)
        svg = ForeignObjectRegex().Replace(svg, "");

        // Remove javascript: URLs in href/xlink:href attributes
        svg = JavascriptHrefRegex().Replace(svg, "$1=\"\"");

        // Remove data: URLs that could contain scripts
        svg = DataScriptHrefRegex().Replace(svg, "$1=\"\"");

        return svg;
    }

    private string ModifySvgRoot(string svg)
    {
        var match = SvgOpenTagRegex().Match(svg);
        if (!match.Success)
            return svg;

        var originalTag = match.Value;
        var tagContent = match.Groups["attrs"].Value;

        var newAttributes = new List<string>();

        // Preserve viewBox if present
        var viewBoxMatch = ViewBoxRegex().Match(tagContent);
        if (viewBoxMatch.Success)
        {
            newAttributes.Add(viewBoxMatch.Value);
        }

        // Preserve xmlns attributes
        var xmlnsMatches = XmlnsRegex().Matches(tagContent);
        foreach (Match xmlnsMatch in xmlnsMatches)
        {
            newAttributes.Add(xmlnsMatch.Value);
        }

        // Add accessibility attributes
        newAttributes.Add("role=\"img\"");

        if (!string.IsNullOrEmpty(Alt))
        {
            newAttributes.Add($"aria-label=\"{EscapeAttributeValue(Alt)}\"");
        }
        else
        {
            newAttributes.Add("aria-hidden=\"true\"");
        }

        // Build class attribute
        var classes = new List<string> { "svg-inline" };
        if (!string.IsNullOrEmpty(Class))
        {
            classes.Add(Class);
        }
        newAttributes.Add($"class=\"{string.Join(" ", classes)}\"");

        // Build style attribute if dimensions specified
        var styles = new List<string>();
        if (!string.IsNullOrEmpty(Width))
        {
            styles.Add($"width: {NormalizeDimension(Width)}");
        }
        if (!string.IsNullOrEmpty(Height))
        {
            styles.Add($"height: {NormalizeDimension(Height)}");
        }
        if (!string.IsNullOrEmpty(MaxWidth))
        {
            styles.Add($"max-width: {NormalizeDimension(MaxWidth)}");
        }

        if (styles.Count > 0)
        {
            newAttributes.Add($"style=\"{string.Join("; ", styles)}\"");
        }

        var newTag = $"<svg {string.Join(" ", newAttributes)}>";

        return svg.Replace(originalTag, newTag);
    }

    private static string NormalizeDimension(string value)
    {
        if (int.TryParse(value, out _))
        {
            return $"{value}px";
        }
        return value;
    }

    private static string EscapeAttributeValue(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    protected string GetRenderedContent()
    {
        if (m_isLoading)
        {
            return "<div class=\"svg-inline svg-inline--loading\"><span>Loading...</span></div>";
        }

        if (!string.IsNullOrEmpty(m_errorMessage))
        {
            return $"<div class=\"svg-inline svg-inline--error\" title=\"{EscapeAttributeValue(m_errorMessage)}\"><span>SVG failed to load</span></div>";
        }

        return m_loadedSvgContent ?? "";
    }

    #endregion

    #region Regex Patterns

    [GeneratedRegex(@"<script[\s\S]*?</script>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"\s+on\w+\s*=\s*(?:""[^""]*""|'[^']*')", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex(@"<foreignObject[\s\S]*?</foreignObject>", RegexOptions.IgnoreCase)]
    private static partial Regex ForeignObjectRegex();

    [GeneratedRegex(@"((?:xlink:)?href)\s*=\s*[""']javascript:[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex JavascriptHrefRegex();

    [GeneratedRegex(@"((?:xlink:)?href)\s*=\s*[""']data:(?:text/html|application/javascript)[^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex DataScriptHrefRegex();

    [GeneratedRegex(@"<svg(?<attrs>[^>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex SvgOpenTagRegex();

    [GeneratedRegex(@"viewBox\s*=\s*""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex ViewBoxRegex();

    [GeneratedRegex(@"xmlns(?::\w+)?\s*=\s*""[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex XmlnsRegex();

    #endregion

    #region Parameters

    [Parameter]
    public string Src { get; set; } = string.Empty;

    [Parameter]
    public string Alt { get; set; } = string.Empty;

    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Parameter]
    public string Width { get; set; } = string.Empty;

    [Parameter]
    public string Height { get; set; } = string.Empty;

    [Parameter]
    public string MaxWidth { get; set; } = string.Empty;

    [Parameter]
    public string BasePath { get; set; } = string.Empty;

    #endregion

    #region Injected Dependencies

    [Inject]
    public HttpClient Http { get; set; } = null!;

    #endregion
}

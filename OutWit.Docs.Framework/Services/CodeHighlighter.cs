using System.Net;
using System.Text;
using ColorCode;
using ColorCode.Common;
using ColorCode.Parsing;

namespace OutWit.Docs.Framework.Services;

/// <summary>
/// Pure-C# syntax highlighter (ColorCode). Runs at render time inside
/// <see cref="MarkdownService"/>, so highlighting appears in both the generator's
/// static HTML and the live Blazor app — no client-side highlighter, no flash.
/// Emits CSS classes per token (theme-able via the framework CSS), not inline styles.
/// </summary>
public static class CodeHighlighter
{
    #region Functions

    /// <summary>
    /// Highlight code to HTML span markup with token classes. Returns null when the
    /// language is unknown so the caller can fall back to plain (escaped) code.
    /// </summary>
    public static string? Highlight(string code, string? languageId)
    {
        var language = ResolveLanguage(languageId);
        if (language == null)
            return null;

        try
        {
            return new HtmlClassFormatter().GetHtml(code, language);
        }
        catch
        {
            // Never let a highlighter hiccup break rendering — fall back to plain.
            return null;
        }
    }

    #endregion

    #region Tools

    private static ILanguage? ResolveLanguage(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return id.Trim().ToLowerInvariant() switch
        {
            "cs" or "c#" or "csharp" => Languages.CSharp,
            "js" or "javascript" => Languages.JavaScript,
            "ts" or "typescript" => Languages.Typescript,
            "json" => Languages.FindById("json"),
            "html" or "htm" => Languages.Html,
            "xml" or "xaml" or "csproj" or "axml" => Languages.Xml,
            "css" => Languages.Css,
            "sql" => Languages.Sql,
            "ps" or "ps1" or "powershell" => Languages.PowerShell,
            "py" or "python" => Languages.Python,
            "cpp" or "c++" or "c" => Languages.Cpp,
            "java" => Languages.Java,
            "fs" or "fsharp" => Languages.FSharp,
            "vb" or "vbnet" => Languages.VbDotNet,
            "php" => Languages.Php,
            _ => null
        };
    }

    #endregion
}

/// <summary>
/// A ColorCode formatter that wraps tokens in <c>&lt;span class="tok-…"&gt;</c> so
/// the palette can be themed via CSS, instead of ColorCode's default inline styles.
/// </summary>
internal sealed class HtmlClassFormatter : CodeColorizerBase
{
    #region Fields

    private static readonly Dictionary<string, string> SCOPE_CLASSES = new(StringComparer.Ordinal)
    {
        [ScopeName.Comment] = "tok-comment",
        [ScopeName.Keyword] = "tok-keyword",
        [ScopeName.PreprocessorKeyword] = "tok-meta",
        [ScopeName.String] = "tok-string",
        [ScopeName.StringCSharpVerbatim] = "tok-string",
        [ScopeName.Number] = "tok-number",
        [ScopeName.ClassName] = "tok-type",
        [ScopeName.JsonKey] = "tok-property",
        [ScopeName.JsonString] = "tok-string",
        [ScopeName.JsonNumber] = "tok-number",
        [ScopeName.JsonConst] = "tok-keyword",
        [ScopeName.HtmlElementName] = "tok-tag",
        [ScopeName.HtmlTagDelimiter] = "tok-punctuation",
        [ScopeName.HtmlAttributeName] = "tok-attr",
        [ScopeName.HtmlAttributeValue] = "tok-string",
        [ScopeName.HtmlComment] = "tok-comment",
        [ScopeName.XmlName] = "tok-tag",
        [ScopeName.XmlAttribute] = "tok-attr",
        [ScopeName.XmlAttributeValue] = "tok-string",
        [ScopeName.XmlDelimiter] = "tok-punctuation",
        [ScopeName.XmlComment] = "tok-comment",
        [ScopeName.CssSelector] = "tok-selector",
        [ScopeName.CssPropertyName] = "tok-property",
        [ScopeName.CssPropertyValue] = "tok-string",
        [ScopeName.PowerShellVariable] = "tok-variable",
        [ScopeName.PowerShellCommand] = "tok-function",
        [ScopeName.PowerShellParameter] = "tok-attr",
        [ScopeName.PowerShellOperator] = "tok-punctuation",
        [ScopeName.PowerShellType] = "tok-type",
    };

    #endregion

    #region Constructors

    public HtmlClassFormatter() : base(null, null)
    {
    }

    #endregion

    #region Functions

    public string GetHtml(string code, ILanguage language)
    {
        var sb = new StringBuilder(code.Length * 2);
        using var writer = new StringWriter(sb);
        Writer = writer;
        languageParser.Parse(code, language, (parsed, captures) => Write(parsed, captures));
        writer.Flush();
        return sb.ToString();
    }

    #endregion

    #region CodeColorizerBase

    protected override void Write(string parsedSourceCode, IList<Scope> scopes)
    {
        var insertions = new List<TextInsertion>();
        foreach (var scope in scopes)
            GetScopeInsertions(scope, insertions);

        // OrderBy is stable, so open/close insertions at the same index keep order.
        var ordered = insertions.OrderBy(i => i.Index).ToList();

        var offset = 0;
        foreach (var insertion in ordered)
        {
            Writer.Write(WebUtility.HtmlEncode(parsedSourceCode.Substring(offset, insertion.Index - offset)));
            if (string.IsNullOrEmpty(insertion.Text))
                WriteOpenSpan(insertion.Scope);
            else
                Writer.Write(insertion.Text);
            offset = insertion.Index;
        }

        Writer.Write(WebUtility.HtmlEncode(parsedSourceCode.Substring(offset)));
    }

    #endregion

    #region Tools

    private TextWriter Writer { get; set; } = TextWriter.Null;

    private static void GetScopeInsertions(Scope scope, ICollection<TextInsertion> insertions)
    {
        insertions.Add(new TextInsertion { Index = scope.Index, Scope = scope });
        foreach (var child in scope.Children)
            GetScopeInsertions(child, insertions);
        insertions.Add(new TextInsertion { Index = scope.Index + scope.Length, Text = "</span>" });
    }

    private void WriteOpenSpan(Scope scope)
    {
        if (SCOPE_CLASSES.TryGetValue(scope.Name, out var cls))
            Writer.Write($"<span class=\"{cls}\">");
        else
            Writer.Write("<span>");
    }

    #endregion
}

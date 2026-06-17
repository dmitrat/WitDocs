using System.Net;
using System.Text;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace OutWit.Docs.Framework.Services;

/// <summary>
/// Markdig extension that replaces the default code-block renderer with one that
/// syntax-highlights fenced code (via <see cref="CodeHighlighter"/>) and wraps it
/// in a styled container with a language label and a copy button.
/// </summary>
internal sealed class CodeHighlightExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html)
            html.ObjectRenderers.Replace<CodeBlockRenderer>(new HighlightedCodeBlockRenderer());
    }
}

/// <summary>
/// Renders code blocks as:
/// <code>&lt;div class="code-block" data-lang="csharp"&gt;&lt;div class="code-block__bar"&gt;…
/// &lt;button class="code-copy"&gt;&lt;/div&gt;&lt;pre&gt;&lt;code&gt;…highlighted…&lt;/code&gt;&lt;/pre&gt;&lt;/div&gt;</code>
/// The markup is static (works in SSG and runtime); the copy button is wired by a
/// single delegated listener in framework.js.
/// </summary>
internal sealed class HighlightedCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        var language = (obj as FencedCodeBlock)?.Info?.Trim() ?? string.Empty;
        var code = ExtractCode(obj);

        // Highlighted spans if the language is known, else plain escaped code.
        var inner = CodeHighlighter.Highlight(code, language) ?? WebUtility.HtmlEncode(code);

        renderer.Write("<div class=\"code-block\"");
        if (!string.IsNullOrEmpty(language))
        {
            renderer.Write(" data-lang=\"");
            renderer.Write(WebUtility.HtmlEncode(language));
            renderer.Write("\"");
        }
        renderer.Write("><div class=\"code-block__bar\">");
        if (!string.IsNullOrEmpty(language))
        {
            renderer.Write("<span class=\"code-block__lang\">");
            renderer.Write(WebUtility.HtmlEncode(language));
            renderer.Write("</span>");
        }
        renderer.Write("<button class=\"code-copy\" type=\"button\" aria-label=\"Copy code\">Copy</button>");
        renderer.Write("</div><pre><code>");
        renderer.Write(inner);
        renderer.Write("</code></pre></div>\n");
    }

    private static string ExtractCode(LeafBlock obj)
    {
        var sb = new StringBuilder();
        var lines = obj.Lines.Lines;
        var count = obj.Lines.Count;
        for (var i = 0; i < count; i++)
        {
            sb.Append(lines[i].Slice.ToString());
            sb.Append('\n');
        }

        if (sb.Length > 0 && sb[^1] == '\n')
            sb.Length--;

        return sb.ToString();
    }
}

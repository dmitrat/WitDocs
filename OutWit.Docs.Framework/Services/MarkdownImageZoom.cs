using System.Text;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace OutWit.Docs.Framework.Services;

/// <summary>
/// Markdig extension that turns a paragraph holding nothing but an image into a
/// <c>&lt;figure&gt;</c> whose image can be opened at full size, with an optional
/// caption taken from the markdown title.
///
/// A documentation screenshot is usually wider than the column it is shown in, so
/// the picture on the page is a thumbnail whether or not it was meant to be one.
/// This gives every one of them a way back to its own resolution without the author
/// writing anything but <c>![alt](src "caption")</c>.
/// </summary>
internal sealed class ImageZoomExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer html)
            html.ObjectRenderers.Replace<ParagraphRenderer>(new FigureParagraphRenderer());
    }
}

/// <summary>
/// Renders a paragraph that holds a single image as:
/// <code>&lt;figure class="ow-figure"&gt;&lt;button class="ow-figure__zoom"&gt;&lt;img …&gt;&lt;/button&gt;
/// &lt;figcaption&gt;…&lt;/figcaption&gt;&lt;/figure&gt;</code>
/// Every other paragraph is left to the base renderer, so an image used inline in a
/// sentence - a badge, an icon - keeps flowing with the text.
///
/// The markup is static, so it is identical in the generated pages and at runtime;
/// the button is wired by one delegated listener in framework.js, the same way the
/// copy button on a code block is.
/// </summary>
internal sealed class FigureParagraphRenderer : ParagraphRenderer
{
    #region Constants

    /// <summary>Prefix of the button's accessible name, before the image's own alt text.</summary>
    private const string ZOOM_LABEL = "Open the image at full size";

    #endregion

    #region Functions

    protected override void Write(HtmlRenderer renderer, ParagraphBlock obj)
    {
        // A tight list item drops the <p> altogether, and a figure inside one would
        // be a block where the author wrote a list. Plain text rendering has no use
        // for the wrapper either.
        if (renderer.ImplicitParagraph || !renderer.EnableHtmlForBlock)
        {
            base.Write(renderer, obj);
            return;
        }

        var image = SoleImageOf(obj);
        if (image == null)
        {
            base.Write(renderer, obj);
            return;
        }

        WriteFigure(renderer, image);
    }

    #endregion

    #region Tools

    /// <summary>
    /// The one image this paragraph consists of, or null if it holds anything else.
    /// Whitespace and line breaks around the image do not count as content; an image
    /// inside a link does not qualify either, since a button inside an anchor is not
    /// markup a browser can be asked to make sense of.
    /// </summary>
    private static LinkInline? SoleImageOf(ParagraphBlock paragraph)
    {
        if (paragraph.Inline == null)
            return null;

        LinkInline? found = null;

        foreach (var inline in paragraph.Inline)
        {
            switch (inline)
            {
                case LinkInline { IsImage: true } image:
                    if (found != null)
                        return null;

                    found = image;
                    break;

                case LiteralInline literal when literal.Content.ToString().Trim().Length == 0:
                case LineBreakInline:
                    break;

                default:
                    return null;
            }
        }

        return found;
    }

    private static void WriteFigure(HtmlRenderer renderer, LinkInline image)
    {
        var alt = PlainTextOf(image);
        var caption = image.Title;

        renderer.EnsureLine();
        renderer.Write("<figure class=\"ow-figure\">");

        renderer.Write("<button type=\"button\" class=\"ow-figure__zoom\" aria-label=\"");
        renderer.WriteEscape(string.IsNullOrWhiteSpace(alt) ? ZOOM_LABEL : $"{ZOOM_LABEL}: {alt}");
        renderer.Write("\">");

        renderer.Write("<img src=\"");
        renderer.WriteEscapeUrl(image.GetDynamicUrl?.Invoke() ?? image.Url);
        renderer.Write("\" alt=\"");
        renderer.WriteEscape(alt);
        renderer.Write("\" loading=\"lazy\" decoding=\"async\"");
        renderer.WriteAttributes(image);
        renderer.Write(" />");

        renderer.Write("</button>");

        if (!string.IsNullOrWhiteSpace(caption))
        {
            renderer.Write("<figcaption class=\"ow-figure__caption\">");
            renderer.WriteEscape(caption);
            renderer.Write("</figcaption>");
        }

        renderer.Write("</figure>");
        renderer.WriteLine();
    }

    /// <summary>
    /// The text of an image's label, flattened. Markdown allows emphasis inside the
    /// square brackets; an alt attribute cannot carry it, so the markup is dropped
    /// and the words are kept.
    /// </summary>
    private static string PlainTextOf(ContainerInline container)
    {
        var text = new StringBuilder();
        Append(container, text);
        return text.ToString().Trim();
    }

    private static void Append(ContainerInline container, StringBuilder text)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    text.Append(literal.Content.ToString());
                    break;

                case CodeInline code:
                    text.Append(code.Content);
                    break;

                case LineBreakInline:
                    text.Append(' ');
                    break;

                case ContainerInline nested:
                    Append(nested, text);
                    break;
            }
        }
    }

    #endregion
}

using System.Text.Encodings.Web;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.AspNetCore.Html;

namespace Kentico.Community.Portal.Web.Rendering;

public class MarkdownRenderer
{
    private readonly MarkdownPipeline defaultPipeline;
    private readonly MarkdownPipeline unsafePipeline;

    public MarkdownRenderer()
    {
        defaultPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .DisableHtml()
            .Build();

        unsafePipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .Build();
    }

    /// <summary>
    /// Renders the given markdown to HTML using the default pipeline which encodes any HTML to prevent XSS
    /// </summary>
    /// <param name="markdownContent"></param>
    /// <returns></returns>
    public HtmlSanitizedHtmlString Render(string markdownContent)
    {
        string contentHTML = Markdown.ToHtml(markdownContent, defaultPipeline);

        return new(contentHTML);
    }

    /// <summary>
    /// Renders the given markdown to HTML using the unsafe pipeline which does not encode HTML
    /// and could be vulnerable to XSS.
    /// Only use this method with sources of markdown that are known to be safe (ex: content authored within the Xperience administration)
    /// </summary>
    /// <param name="contentWithHTML"></param>
    /// <returns></returns>
    public HtmlString RenderUnsafe(string contentWithHTML)
    {
        string contentHTML = Markdown.ToHtml(contentWithHTML, unsafePipeline);

        return new(contentHTML);
    }

    /// <summary>
    /// Like <see cref="RenderUnsafe(string)"/>, but paragraphs render without <c>&lt;p&gt;</c> tags,
    /// so a one-line value can go inside phrasing content (ex: a <c>&lt;label&gt;</c>).
    /// Same XSS caveat as <see cref="RenderUnsafe(string)"/>.
    /// </summary>
    /// <param name="contentWithHTML"></param>
    /// <returns></returns>
    public HtmlString RenderInlineUnsafe(string contentWithHTML)
    {
        var document = Markdown.Parse(contentWithHTML, unsafePipeline);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer) { ImplicitParagraph = true };
        unsafePipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        return new(writer.ToString().Trim());
    }

    public MarkdownDocument Parse(string contentWithHTML) => Markdown.Parse(contentWithHTML, unsafePipeline);

    /// <summary>
    /// Full plain text of the markdown, without formatting. Ex: structured data text of rendered content.
    /// </summary>
    /// <param name="markdownContent"></param>
    /// <returns></returns>
    public string ToPlainText(string markdownContent) =>
        Markdown.ToPlainText(markdownContent ?? "", defaultPipeline).Trim();

    /// <summary>
    /// Plain text of the markdown on one line, cut at a word boundary to at most
    /// <paramref name="maxLength"/> characters (ellipsis included). Ex: a meta description.
    /// </summary>
    /// <param name="markdownContent"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public string ToPlainTextExcerpt(string markdownContent, int maxLength = 160)
    {
        string text = string.Join(' ', Markdown.ToPlainText(markdownContent ?? "", defaultPipeline)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (text.Length <= maxLength)
        {
            return text;
        }

        int cut = text.LastIndexOf(' ', maxLength - 1);

        return $"{text[..(cut > 0 ? cut : maxLength - 1)].TrimEnd(' ', ',', '.', ';', ':')}…";
    }
}

/// <summary>
/// An explicit type to represent sanitized HTML, likely produced by user input.
/// Should be generated with <see cref="MarkdownRenderer.Render(string)"/> 
/// </summary>
public class HtmlSanitizedHtmlString(string rawStr) : IHtmlContent
{
    private readonly HtmlString html = new(rawStr);
    public static HtmlSanitizedHtmlString Empty { get; } = new("");

    public void WriteTo(TextWriter writer, HtmlEncoder encoder) => html.WriteTo(writer, encoder);
}

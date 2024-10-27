using System.Text.Encodings.Web;
using Markdig;
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

    public MarkdownDocument Parse(string contentWithHTML) => Markdown.Parse(contentWithHTML, unsafePipeline);
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

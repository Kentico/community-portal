using System.Text.Encodings.Web;
using Markdig;
using Microsoft.AspNetCore.Html;

namespace Kentico.Community.Portal.Web.Rendering;

public class MarkdownRenderer
{
    private readonly MarkdownPipeline defaultPipeline;

    public MarkdownRenderer() =>
        defaultPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseBootstrap()
            .DisableHtml()
            .Build();

    public HtmlSanitizedHtmlString Render(string content)
    {
        string contentHTML = Markdown.ToHtml(content, defaultPipeline);

        return new(contentHTML);
    }
}

/// <summary>
/// An explicit type to represent sanitized HTML, likely produced by user input.
/// Should be generated with <see cref="MarkdownRenderer.Render(string)"/> 
/// </summary>
public class HtmlSanitizedHtmlString : IHtmlContent
{
    private readonly HtmlString html;
    public static HtmlSanitizedHtmlString Empty { get; } = new("");

    public HtmlSanitizedHtmlString(string rawStr) => html = new(rawStr);

    public void WriteTo(TextWriter writer, HtmlEncoder encoder) => html.WriteTo(writer, encoder);
}

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CMS.Helpers;

namespace Kentico.Community.Portal.Web.Infrastructure.Search;

public class WebScraperHtmlSanitizer
{
    public virtual string SanitizeHtmlFragment(string htmlContent)
    {

        var parser = new HtmlParser();

        // null is relevant parameter
        var nodes = parser.ParseFragment(htmlContent, null);


        // Removes script tags
        foreach (var element in nodes.QuerySelectorAll("script"))
        {
            element.Remove();
        }

        // Removes script tags
        foreach (var element in nodes.QuerySelectorAll("style"))
        {
            element.Remove();
        }

        // Removes elements marked with the default Xperience exclusion attribute
        foreach (var element in nodes.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
        {
            element.Remove();
        }

        // Gets the text content of the body element
        string textContent = string.Join(" ", nodes.Select(n => n.TextContent));

        // Normalizes and trims whitespace characters
        textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
        textContent = textContent.Trim();

        return textContent;
    }

    public virtual string SanitizeHtmlDocument(string htmlContent)
    {
        if (!string.IsNullOrWhiteSpace(htmlContent))
        {
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(htmlContent);
            var body = doc.Body;
            if (body != null)
            {
                foreach (var element in body.QuerySelectorAll("noscript"))
                {
                    element.Remove();
                }

                // Removes script tags
                foreach (var element in body.QuerySelectorAll("script"))
                {
                    element.Remove();
                }

                // Removes script tags
                foreach (var element in body.QuerySelectorAll("style"))
                {
                    element.Remove();
                }

                // Removes elements marked with the default Xperience exclusion attribute
                foreach (var element in body.QuerySelectorAll($"*[{"data-ktc-search-exclude"}]"))
                {
                    element.Remove();
                }

                // Removes header
                foreach (var element in body.QuerySelectorAll("header"))
                {
                    element.Remove();
                }

                // Removes breadcrumbs
                foreach (var element in body.QuerySelectorAll(".breadcrumb"))
                {
                    element.Remove();
                }

                // Removes footer
                foreach (var element in body.QuerySelectorAll("footer"))
                {
                    element.Remove();
                }

                // Gets the text content of the body element
                string textContent = body.TextContent;

                // Normalizes and trims whitespace characters
                textContent = HTMLHelper.RegexHtmlToTextWhiteSpace.Replace(textContent, " ");
                textContent = textContent.Trim();

                string title = doc.Head.QuerySelector("title")?.TextContent;
                string description = doc.Head.QuerySelector("meta[name='description']")?.GetAttribute("content");

                return string.Join(" ",
                    new string[] { title, description, textContent }.Where(i => !string.IsNullOrWhiteSpace(i))
                    );
            }
        }

        return string.Empty;
    }
}

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Kentico.Community.Portal.Web.Rendering;

/// <summary>
/// Service for rendering Blazor components to HTML strings for use in emails
/// </summary>
public interface IRazorComponentRenderer
{
    /// <summary>
    /// Renders a Blazor component to an HTML string
    /// </summary>
    /// <typeparam name="TComponent">The component type to render</typeparam>
    /// <param name="parameters">Parameters to pass to the component</param>
    /// <returns>The rendered HTML as a string</returns>
    public Task<string> RenderToStringAsync<TComponent>(Dictionary<string, object?>? parameters = null) where TComponent : IComponent;
}

/// <summary>
/// Implementation of IRazorComponentRenderer using HtmlRenderer
/// </summary>
public class RazorComponentRenderer : IRazorComponentRenderer, IDisposable
{
    private readonly HtmlRenderer htmlRenderer;

    public RazorComponentRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory) => htmlRenderer = new HtmlRenderer(serviceProvider, loggerFactory);

    public async Task<string> RenderToStringAsync<TComponent>(Dictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        var parameterView = parameters != null ? ParameterView.FromDictionary(parameters) : ParameterView.Empty;
        string html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await htmlRenderer.RenderComponentAsync<TComponent>(parameterView);
            return result.ToHtmlString();
        });

        return html;
    }

    public void Dispose()
    {
        htmlRenderer?.Dispose();
        GC.SuppressFinalize(this);
    }
}

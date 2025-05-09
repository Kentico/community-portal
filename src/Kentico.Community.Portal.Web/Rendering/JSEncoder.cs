using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kentico.Community.Portal.Web.Rendering;

/// <summary>
/// Provides convenient encoding and serlization methods
/// used when passing values from Razor to JavaScript
/// </summary>
public interface IJSEncoder
{
    /// <summary>
    /// Encodes the value into valid JavaScript.
    /// </summary>
    /// <remarks>
    /// This will not generate a string and returned value must be placed in quotes when used in JavaScript
    /// </remarks>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <example>
    /// window.data = {
    ///     item: '@JSEncoder.Encode("item's")'
    /// };
    /// </example>
    public string Encode(string value);

    /// <summary>
    /// Encodes and serializes the given value to a valid JSON value
    /// </summary>
    /// <remarks>
    /// The returned value will already be a valid JavaScript string and should not be quoted.
    /// If the value will be enclosed in quotes (HTML attribute) ensure they are single quotes.
    /// </remarks>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <example>
    /// window.data = {
    ///     item: @JSEncoder.EncodeToJson("item's")
    /// };
    /// 
    /// &lt;div class='@JSEncoder.LocalizeToJson("CLASS")'&gt;...&lt;/div&gt;
    /// </example>
    public IHtmlContent EncodeToJson(object value);
}

public class JSEncoder(IJsonHelper helper) : IJSEncoder
{
    private readonly IJsonHelper helper = helper;

    public string Encode(string value) =>
        JavaScriptEncoder.Default.Encode(value);

    public IHtmlContent EncodeToJson(object value) =>
        helper.Serialize(value);
}

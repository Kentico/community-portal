using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.DataProtection;

namespace Kentico.Community.Portal.Web.Components;

public interface IWidgetPropertiesSerializer
{
    /// <summary>
    /// Protects the serialized widget properties.
    /// </summary>
    /// <param name="properties">Widget properties.</param>
    public string Protect<T>(T properties) where T : IWidgetProperties;

    /// <summary>
    /// Unprotects the widget properties.
    /// </summary>
    /// <param name="protectedProperties">Protected serialized widget properties.</param>
    public T? Unprotect<T>(string protectedProperties) where T : IWidgetProperties;
}

public class WidgetPropertiesSerializer(IDataProtectionProvider dataProtectionProvider) : IWidgetPropertiesSerializer
{
    private readonly string[] purpose = ["Widget Properties"];

    private readonly IDataProtectionProvider dataProtectionProvider = dataProtectionProvider;

    private static readonly JsonSerializerOptions defaultSerializationOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Serializes component properties to a JSON string.
    /// </summary>
    /// <param name="properties">The properties to serialize.</param>
    /// <returns>JSON string representation of the properties.</returns>
    private static string Serialize(object? properties) =>
        properties == null ? "{}" : JsonSerializer.Serialize(properties, defaultSerializationOptions);

    /// <summary>
    /// Deserializes a JSON string to component properties of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object of type T, or null if deserialization fails.</returns>
    private static T? Deserialize<T>(string json) where T : IWidgetProperties =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, defaultSerializationOptions);

    /// <summary>
    /// Protects the serialized widget properties.
    /// </summary>
    /// <param name="properties">Widget properties.</param>
    public string Protect<T>(T properties) where T : IWidgetProperties
    {
        string propsVal = Serialize(properties);
        var protector = dataProtectionProvider.CreateProtector(purpose);
        byte[] unprotectedBytes = Encoding.UTF8.GetBytes(propsVal);
        byte[] protectedBytes = protector.Protect(unprotectedBytes);

        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>
    /// Unprotects the widget properties.
    /// </summary>
    /// <param name="protectedProperties">Protected serialized widget properties.</param>
    public T? Unprotect<T>(string protectedProperties) where T : IWidgetProperties
    {
        var protector = dataProtectionProvider.CreateProtector(purpose);
        byte[] protectedBytes = Convert.FromBase64String(protectedProperties);
        byte[] unprotectedBytes = protector.Unprotect(protectedBytes);

        string propsVal = Encoding.UTF8.GetString(unprotectedBytes);

        return Deserialize<T>(propsVal);
    }
}

using System.Web;
using CMS.Base;
using CMS.Helpers;
using CMS.MacroEngine;
using Kentico.Community.Portal.Admin;
using Kentico.Community.Portal.Core.Content;
using Newtonsoft.Json;

[assembly: RegisterMacroNamespace(typeof(EmailNamespace), AllowAnonymous = true)]

namespace Kentico.Community.Portal.Admin;

[Extension(typeof(EmailMacroMethods))]
public class EmailNamespace : MacroNamespace<EmailNamespace> { }

public class EmailMacroMethods : MacroMethodContainer
{
    [MacroMethod(typeof(string), "Replaces a utm parameter in the given URL's query string.", 3)]
    [MacroMethodParam(0, "inputURL", typeof(string), "Original URL.")]
    [MacroMethodParam(1, "utmParameter", typeof(string), "UTM parameter.")]
    [MacroMethodParam(2, "newUtmValue", typeof(string), "UTM value.")]
    public static string ReplaceUtmParameter(EvaluationContext _, params object[] parameters)
    {
        switch (parameters.Length)
        {
            case 3:
                string inputUrl = ValidationHelper.GetString(parameters[0], "");
                string utmParameter = ValidationHelper.GetString(parameters[1], "");
                string newUtmValue = ValidationHelper.GetString(parameters[2], "");
                return ReplaceUtmParametersInternal(inputUrl, utmParameter, newUtmValue);
            default:
                throw new NotSupportedException();
        };
    }

    [MacroMethod(typeof(string), "Replaces utm parameters in the given URL's query string. Can accept 2 sets of UTM parameters with the second set overwriting values from the first.", 3)]
    [MacroMethodParam(0, "inputURL", typeof(string), "Original URL.")]
    [MacroMethodParam(1, "utmParametersJSON1", typeof(string), "UTM parameter 1 JSON.")]
    [MacroMethodParam(1, "utmParametersJSON2", typeof(string), "UTM parameter 2 JSON.")]
    public static string ReplaceUtmParameters(EvaluationContext _, params object[] parameters)
    {
        switch (parameters.Length)
        {
            case 2:
            case 3:
                string inputUrl = ValidationHelper.GetString(parameters[0], "");
                string utmParametersJSON1 = ValidationHelper.GetString(parameters[1], "");
                string utmParametersJSON2 = parameters.Length == 3 ? ValidationHelper.GetString(parameters[2], "") : "{ }";
                var utmParameters1 = JsonConvert.DeserializeObject<UTMParametersDataType>(utmParametersJSON1) ?? new();
                var utmParameters2 = JsonConvert.DeserializeObject<UTMParametersDataType>(utmParametersJSON2) ?? new();
                return ReplaceUtmParametersInternal(inputUrl, utmParameters1, utmParameters2);
            default:
                throw new NotSupportedException();
        };
    }

    private static string ReplaceUtmParametersInternal(string inputUrl, UTMParametersDataType parameters1, UTMParametersDataType parameters2)
    {
        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            return inputUrl;
        }

        // Validate if the input is a well-formed URI
        if (!Uri.TryCreate(inputUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid URL", nameof(inputUrl));
        }

        var queryParts = HttpUtility.ParseQueryString(uriResult.Query);
        ReplacePart(parameters1, "utm_source", p => p.Source);
        ReplacePart(parameters1, "utm_medium", p => p.Medium);
        ReplacePart(parameters1, "utm_campaign", p => p.Campaign);
        ReplacePart(parameters1, "utm_content", p => p.Content);
        ReplacePart(parameters1, "utm_term", p => p.Term);
        ReplacePart(parameters2, "utm_source", p => p.Source);
        ReplacePart(parameters2, "utm_medium", p => p.Medium);
        ReplacePart(parameters2, "utm_campaign", p => p.Campaign);
        ReplacePart(parameters2, "utm_content", p => p.Content);
        ReplacePart(parameters2, "utm_term", p => p.Term);

        var builder = new UriBuilder(uriResult)
        {
            Query = queryParts.ToString()
        };

        return builder.Uri.ToString();

        void ReplacePart(UTMParametersDataType parameters, string parameterName, Func<UTMParametersDataType, string> getValue)
        {
            string value = getValue(parameters);

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (queryParts.AllKeys.Length == 0 || !queryParts.AllKeys.Any(key => key?.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                queryParts.Add(parameterName, value);
            }

            foreach (string? key in queryParts.AllKeys)
            {
                if (key is not null && key.StartsWith(parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    queryParts[key] = value;
                }
            }

        }
    }

    private static string ReplaceUtmParametersInternal(string inputUrl, string utmParameter, string newUtmValue)
    {
        if (string.IsNullOrWhiteSpace(inputUrl) ||
            string.IsNullOrWhiteSpace(newUtmValue))
        {
            return inputUrl;
        }

        if (string.IsNullOrWhiteSpace(utmParameter))
        {
            throw new ArgumentException("Invalid utm parameter", nameof(utmParameter));
        }

        // Validate if the input is a well-formed URI
        if (!Uri.TryCreate(inputUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid URL", nameof(inputUrl));
        }

        string utmKey = $"utm_{utmParameter}";

        var queryParts = HttpUtility.ParseQueryString(uriResult.Query);

        if (queryParts.AllKeys.Length == 0 || !queryParts.AllKeys.Any(key => key?.StartsWith(utmKey, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            queryParts.Add(utmKey, newUtmValue);
        }

        foreach (string? key in queryParts.AllKeys)
        {
            if (key is not null && key.StartsWith(utmKey, StringComparison.OrdinalIgnoreCase))
            {
                queryParts[key] = newUtmValue;
            }
        }

        var builder = new UriBuilder(uriResult)
        {
            Query = queryParts.ToString()
        };

        return builder.Uri.ToString();
    }
}

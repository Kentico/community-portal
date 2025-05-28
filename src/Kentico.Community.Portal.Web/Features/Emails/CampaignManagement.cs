using System.Web;

namespace Kentico.Community.Portal.Web.Features.Emails;

public static class CampaignManagement
{
    public static string ReplaceUtmParameters(string inputUrl, UTMParametersDataType parameters1) =>
        ReplaceUtmParameters(inputUrl, parameters1, null);

    public static string ReplaceUtmParameters(string inputUrl, UTMParametersDataType parameters1, UTMParametersDataType? parameters2)
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
        if (parameters2 is not null)
        {
            ReplacePart(parameters2, "utm_source", p => p.Source);
            ReplacePart(parameters2, "utm_medium", p => p.Medium);
            ReplacePart(parameters2, "utm_campaign", p => p.Campaign);
            ReplacePart(parameters2, "utm_content", p => p.Content);
            ReplacePart(parameters2, "utm_term", p => p.Term);
        }

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

    public static string ReplaceUtmParameters(string inputUrl, string utmParameter, string newUtmValue)
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

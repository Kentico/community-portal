using CMS.Core;

namespace Kentico.Community.Portal.Web.Infrastructure;

/// <summary>
/// Used to customize validation messages returned with the Form Builder
/// See: https://community.kentico.com/q-and-a/q/updating-default-validation-messages-on-xbyk-forms-f2bc9075
/// </summary>
/// <param name="localizationService"></param>
public class ApplicationLocalizationService(ILocalizationService localizationService) : ILocalizationService
{
    private readonly ILocalizationService localizationService = localizationService;

    public string GetString(string resourceKey, string? culture = null, bool useDefaultCulture = true)
    {
        if (string.Equals("general.requiresvalue", resourceKey))
        {
            return "This field is required";
        }

        return localizationService.GetString(resourceKey, culture, useDefaultCulture);
    }

    public string LocalizeExpression(string expression, string? culture = null, bool encode = false, Func<string, string, bool, string>? getStringMethod = null, bool useDefaultCulture = true) =>
        localizationService.LocalizeExpression(expression, culture, encode, getStringMethod, useDefaultCulture);
    public string LocalizeString(string inputText, string? culture = null, bool encode = false, bool useDefaultCulture = true) =>
        localizationService.LocalizeString(inputText, culture, encode, useDefaultCulture);
}

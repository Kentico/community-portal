using CMS.Base;
using CMS.Helpers;
using CMS.MacroEngine;
using Kentico.Community.Portal.Web.Features.Emails;
using Newtonsoft.Json;

[assembly: RegisterMacroNamespace(typeof(EmailNamespace), AllowAnonymous = true)]

namespace Kentico.Community.Portal.Web.Features.Emails;

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
                return CampaignManagement.ReplaceUtmParameters(inputUrl, utmParameter, newUtmValue);
            default:
                throw new NotSupportedException();
        }
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
                return CampaignManagement.ReplaceUtmParameters(inputUrl, utmParameters1, utmParameters2);
            default:
                throw new NotSupportedException();
        }
    }


}

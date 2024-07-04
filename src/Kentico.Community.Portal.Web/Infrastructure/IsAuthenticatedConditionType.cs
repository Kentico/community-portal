using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;

[assembly: RegisterPersonalizationConditionType(
    identifier: IsAuthenticatedConditionType.IDENTIFIER,
    type: typeof(IsAuthenticatedConditionType),
    name: "Is authenticated",
    Description = "Evaluates based on the visitor's authentication status.",
    IconClass = "icon-app-membership",
    Hint = "Display personalized experiences to visitors who are authenticated")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class IsAuthenticatedConditionType(IHttpContextAccessor contextAccessor) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.IsAuthenticated";

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;

    /// <summary>
    /// Unused property to resolve bug where the ConditionType does not work if it has no custom properties
    /// </summary>
    [TextWithLabelComponent]
    public string Placeholder { get; set; } = "";

    public override bool Evaluate()
    {
        var context = contextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        var identity = context.User.Identities.FirstOrDefault();

        return identity is not null && identity.IsAuthenticated;
    }
}

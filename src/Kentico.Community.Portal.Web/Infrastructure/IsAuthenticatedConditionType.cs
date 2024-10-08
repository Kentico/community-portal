using Kentico.PageBuilder.Web.Mvc.Personalization;

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

    public override bool Evaluate()
    {
        var context = contextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        var identity = context.User.Identity;

        return identity is not null && identity.IsAuthenticated;
    }
}

using Kentico.Community.Portal.Web.Membership;
using Kentico.PageBuilder.Web.Mvc.Personalization;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Identity;

[assembly: RegisterPersonalizationConditionType(
    identifier: IsMVPConditionType.IDENTIFIER,
    type: typeof(IsMVPConditionType),
    name: "Is MVP",
    Description = "Evaluates based on the visitor's membership status.",
    IconClass = "icon-app-membership",
    Hint = "Display personalized experiences to visitors who are MVPs")]

namespace Kentico.PageBuilder.Web.Mvc.Personalization;

public class IsMVPConditionType(IHttpContextAccessor contextAccessor, UserManager<CommunityMember> userManager) : ConditionType
{
    public const string IDENTIFIER = "Kentico.Community.Portal.Personalization.IsMVP";

    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly UserManager<CommunityMember> userManager = userManager;

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

        if (identity is null || !identity.IsAuthenticated)
        {
            return false;
        }

        var user = userManager.GetUserAsync(context.User).GetAwaiter().GetResult();

        return user is not null && user.IsMVP;
    }
}

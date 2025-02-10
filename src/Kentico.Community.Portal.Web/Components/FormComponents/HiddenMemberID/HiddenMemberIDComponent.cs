using System.Security.Claims;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.FormComponents;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Forms.Web.Mvc;
using Microsoft.AspNetCore.Identity;

[assembly: RegisterFormComponent(
    HiddenMemberIDComponent.IDENTIFIER,
    typeof(HiddenMemberIDComponent),
    "Hidden member ID",
    Description = "Stores the current member ID value as a hidden field",
    IconClass = KenticoIcons.MASK,
    ViewName = "~/Components/FormComponents/HiddenMemberID/HiddenMemberID.cshtml")]

namespace Kentico.Community.Portal.Web.Components.FormComponents;

public class HiddenMemberIDComponent(
    UserManager<CommunityMember> userManager,
    IHttpContextAccessor contextAccessor,
    IFormBuilderContext formBuilderContext) : FormComponent<IntInputProperties, int?>, IHiddenInputComponent
{
    public const string IDENTIFIER = "CommunityPortal.FormComponent.HiddenMemberID";

    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IFormBuilderContext formBuilderContext = formBuilderContext;

    [BindableProperty]
    public string? Value { get; set; } = "";

    public FormBuilderMode FormBuilderMode => formBuilderContext.Mode;

    public override string LabelForPropertyName => nameof(Value);

    public override int? GetValue()
    {
        /*
         * We only allow the value to be retrieved for a db update
         * when an authenticated user is submitting the form on the live website.
         */
        if (contextAccessor.HttpContext is not { } ctx
            || ctx.User.Identity is not ClaimsIdentity identity
            || !identity.IsAuthenticated
            || formBuilderContext.Mode != FormBuilderMode.Live)
        {
            return 0;
        }

        string userId = userManager.GetUserId(ctx.User) ?? "";
        return int.TryParse(userId, out int value) ? value : null;
    }

    public override void SetValue(int? value) =>
        Value = value.HasValue
            ? value.ToString() ?? ""
            : "";

    public async Task<Maybe<string>> GetMemberName()
    {
        if (formBuilderContext.Mode != FormBuilderMode.ValueEditor)
        {
            return "";
        }

        if (string.IsNullOrWhiteSpace(Value))
        {
            return "";
        }

        var user = await userManager.FindByIdAsync(Value);

        return user is null
            ? "Unknown"
            : $"{user.DisplayName} ({user.Email})";
    }
}

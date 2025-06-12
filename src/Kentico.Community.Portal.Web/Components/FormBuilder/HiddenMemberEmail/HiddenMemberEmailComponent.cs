using System.Security.Claims;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.FormBuilder;
using Kentico.Community.Portal.Web.Infrastructure;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Forms.Web.Mvc;
using Microsoft.AspNetCore.Identity;

[assembly: RegisterFormComponent(
    HiddenMemberEmailComponent.IDENTIFIER,
    typeof(HiddenMemberEmailComponent),
    "Hidden member email",
    Description = "Stores the current member email value as a hidden field",
    IconClass = KenticoIcons.MASK,
    ViewName = "~/Components/FormBuilder/HiddenMemberEmail/HiddenMemberEmail.cshtml")]

namespace Kentico.Community.Portal.Web.Components.FormBuilder;

public class HiddenMemberEmailComponent(
    UserManager<CommunityMember> userManager,
    IHttpContextAccessor contextAccessor,
    IFormBuilderContext formBuilderContext) : FormComponent<TextInputProperties, string?>, IHiddenInputComponent
{
    public const string IDENTIFIER = "CommunityPortal.FormComponent.HiddenMemberEmail";

    private readonly UserManager<CommunityMember> userManager = userManager;
    private readonly IHttpContextAccessor contextAccessor = contextAccessor;
    private readonly IFormBuilderContext formBuilderContext = formBuilderContext;

    [BindableProperty]
    public string? Value { get; set; } = "";

    public FormBuilderMode FormBuilderMode => formBuilderContext.Mode;

    public override string LabelForPropertyName => nameof(Value);

    public override string? GetValue()
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
            return "";
        }

        var user = userManager.GetUserAsync(ctx.User).GetAwaiter().GetResult();
        return user?.Email ?? "";
    }

    public override void SetValue(string? value) =>
        Value = value ?? "";
}

using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.IntegrationCard;
using Kentico.Community.Portal.Web.Features.Members;
using Kentico.Community.Portal.Web.Membership;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using XperienceCommunity.KenticoComponentIcons;

[assembly: RegisterWidget(
    identifier: IntegrationCardWidget.IDENTIFIER,
    name: IntegrationCardWidget.NAME,
    viewComponentType: typeof(IntegrationCardWidget),
    propertiesType: typeof(IntegrationCardWidgetProperties),
    Description = "Card displaying a single integration content item selected from the Content hub",
    IconClass = KenticoIcons.COGWHEEL_SQUARE)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.IntegrationCard;

public class IntegrationCardWidget(
    IContentRetriever contentRetriever,
    IMediator mediator,
    LinkGenerator linkGenerator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.IntegrationCard";
    public const string NAME = "Integration Card";

    private const string ERROR_NO_SELECTION = "Select one integration item.";
    private const string ERROR_MISSING_CONTENT = "The selected integration no longer exists.";
    private const string ERROR_MISSING_MEMBER = "The selected integration member no longer exists.";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly IMediator mediator = mediator;
    private readonly LinkGenerator linkGenerator = linkGenerator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<IntegrationCardWidgetProperties> cvm)
    {
        var props = cvm.Properties;
        var selectedIntegration = props.Integration.FirstOrDefault();

        if (selectedIntegration is null)
        {
            return View("~/Components/ComponentError.cshtml", new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_NO_SELECTION));
        }

        var integration = (await contentRetriever.RetrieveContentByGuids<IntegrationContent>(
            [selectedIntegration.Identifier],
            new RetrieveContentParameters
            {
                LinkedItemsMaxLevel = 1,
            }))
            .FirstOrDefault();

        var member = await GetMember(integration);

        return Validate(props, integration, member)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/IntegrationCard/IntegrationCard.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm));
    }

    private async Task<Maybe<CommunityMember>> GetMember(IntegrationContent? integration)
    {
        if (integration is null ||
            !integration.IntegrationContentHasMemberAuthor ||
            integration.IntegrationContentAuthorMemberID <= 0)
        {
            return Maybe<CommunityMember>.None;
        }

        var member = await mediator.Send(new MemberByIDQuery(integration.IntegrationContentAuthorMemberID));

        return member is null
            ? Maybe<CommunityMember>.None
            : member.AsCommunityMember();
    }

    private Result<IntegrationCardWidgetViewModel, ComponentErrorViewModel> Validate(
        IntegrationCardWidgetProperties props,
        IntegrationContent? integration,
        Maybe<CommunityMember> member)
    {
        if (integration is null)
        {
            return Result.Failure<IntegrationCardWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_MISSING_CONTENT));
        }

        if (integration.IntegrationContentHasMemberAuthor &&
            !member.TryGetValue(out _))
        {
            return Result.Failure<IntegrationCardWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget, ERROR_MISSING_MEMBER));
        }

        return new IntegrationCardWidgetViewModel(props, integration, member, linkGenerator);
    }
}

[FormCategory(Label = "Content", Order = 1)]
[FormCategory(Label = "Display", Order = 3)]
public class IntegrationCardWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        contentTypeName: IntegrationContent.CONTENT_TYPE_NAME,
        Label = "Integration",
        ExplanationText = "Select one integration content item to display.",
        MinimumItems = 1,
        MaximumItems = 1,
        Order = 1)]
    public IEnumerable<ContentItemReference> Integration { get; set; } = [];

    [CheckBoxComponent(
        Label = "Show logo",
        ExplanationText = "Show integration logo when available.",
        Order = 2)]
    public bool ShowLogo { get; set; } = true;

    [CheckBoxComponent(
        Label = "Show author",
        ExplanationText = "Show integration author when available.",
        Order = 3)]
    public bool ShowAuthor { get; set; } = true;

    [DropDownComponent(
        Label = "Link style",
        ExplanationText = "Choose how repository and library links are displayed.",
        DataProviderType = typeof(EnumDropDownOptionsProvider<IntegrationCardLinkStyles>),
        Order = 4)]
    public string LinkStyle { get; set; } = nameof(IntegrationCardLinkStyles.Buttons);
    public IntegrationCardLinkStyles LinkStyleParsed => EnumDropDownOptionsProvider<IntegrationCardLinkStyles>.Parse(LinkStyle, IntegrationCardLinkStyles.Buttons);
}

public enum IntegrationCardLinkStyles
{
    TextLinks,
    Buttons,
}

public class IntegrationCardWidgetViewModel : BaseWidgetViewModel
{
    protected override string WidgetName => IntegrationCardWidget.NAME;

    public bool ShowLogo { get; }
    public bool ShowAuthor { get; }
    public IntegrationCardLinkStyles LinkStyle { get; }
    public string Title { get; }
    public Maybe<ImageViewModel> Logo { get; }
    public string ShortDescription { get; }
    public Maybe<string> RepositoryURL { get; }
    public Maybe<string> LibraryURL { get; }
    public Maybe<IntegrationAuthorLink> AuthorLink { get; }

    public IntegrationCardWidgetViewModel(IntegrationCardWidgetProperties props, IntegrationContent content, Maybe<CommunityMember> member, LinkGenerator linkGenerator)
    {
        ShowLogo = props.ShowLogo;
        ShowAuthor = props.ShowAuthor;
        LinkStyle = props.LinkStyleParsed;
        Title = content.BasicItemTitle;
        Logo = content.ToImageViewModel();
        ShortDescription = content.BasicItemShortDescription;
        RepositoryURL = Maybe.From(content.IntegrationContentRepositoryLinkURL).MapNullOrWhiteSpaceAsNone();
        LibraryURL = Maybe.From(content.IntegrationContentLibraryLinkURL).MapNullOrWhiteSpaceAsNone();

        AuthorLink = GetMemberLink();

        Maybe<IntegrationAuthorLink> GetMemberLink()
        {
            if (!member.TryGetValue(out var m))
            {
                return Maybe<IntegrationAuthorLink>.None;
            }

            string memberURL = linkGenerator.GetPathByAction(nameof(MemberController.MemberDetail), "Member", new { memberID = m.Id }) ?? "";

            return new IntegrationAuthorLink(m.DisplayName, memberURL);
        }
    }
}

public record IntegrationAuthorLink(string Label, string URL);

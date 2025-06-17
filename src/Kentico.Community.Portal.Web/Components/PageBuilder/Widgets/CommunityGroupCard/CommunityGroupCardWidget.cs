using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityGroupCard;
using Kentico.Community.Portal.Web.Rendering;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: CommunityGroupCardWidget.IDENTIFIER,
    name: CommunityGroupCardWidget.NAME,
    viewComponentType: typeof(CommunityGroupCardWidget),
    propertiesType: typeof(CommunityGroupCardWidgetProperties),
    Description = "Card sourcing Community Group content from the Content hub",
    IconClass = KenticoIcons.ID_CARD)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityGroupCard;

public class CommunityGroupCardWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.CommunityGroupCard";
    public const string NAME = "Community Group Card";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<CommunityGroupCardWidgetProperties> vm)
    {
        var contentItemGUID = vm.Properties.CommunityGroups.Select(r => r.Identifier).FirstOrDefault();

        var resp = await mediator.Send(new CommunityGroupContentByGUIDQuery(contentItemGUID));

        return Validate(resp)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/CommunityGroupCard/CommunityGroupCard.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private static Result<CommunityGroupViewModel, ComponentErrorViewModel> Validate(Maybe<CommunityGroupContent> resp)
    {
        if (!resp.TryGetValue(out var content))
        {
            return Result.Failure<CommunityGroupViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "Please select a Community Group."));
        }

        return new CommunityGroupViewModel(content);
    }
}

public class CommunityGroupCardWidgetProperties : BaseWidgetProperties
{
    [ContentItemSelectorComponent(
        contentTypeName: CommunityGroupContent.CONTENT_TYPE_NAME,
        Label = "Community Group",
        AllowContentItemCreation = true,
        DefaultViewMode = ContentItemSelectorViewMode.List,
        MinimumItems = 1,
        MaximumItems = 1,
        ExplanationText = "Only the first selected item will be used.",
        Order = 1)]
    public IEnumerable<ContentItemReference> CommunityGroups { get; set; } = [];
}

public class CommunityGroupViewModel
{
    public string Title { get; }
    public string Description { get; }
    public Maybe<string> URL { get; }
    public Maybe<ImageViewModel> Banner { get; }
    public CommunityGroupAddressViewModel Address { get; }

    public CommunityGroupViewModel(CommunityGroupContent content)
    {
        Title = content.BasicItemTitle;
        Description = content.BasicItemShortDescription;
        URL = Maybe.From(content.CommunityGroupContentWebsiteURL).MapNullOrWhiteSpaceAsNone();
        Banner = content.ToImageViewModel();
        Address = new CommunityGroupAddressViewModel(content);
    }
}

public class CommunityGroupAddressViewModel
{
    public string City { get; }
    public Maybe<string> StateOrProvince { get; }
    public string Country { get; }
    public CommunityGroupAddressViewModel(CommunityGroupContent content)
    {
        City = content.CommunityGroupContentAddressCity;
        StateOrProvince = Maybe.From(content.CommunityGroupContentAddressStateOrProvince).MapNullOrWhiteSpaceAsNone();
        Country = content.CommunityGroupContentAddressCountry;
    }
}

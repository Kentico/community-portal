using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.Widgets.ContactDetailsCard;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: ContactDetailsCardWidget.IDENTIFIER,
    name: ContactDetailsCardWidget.NAME,
    viewComponentType: typeof(ContactDetailsCardWidget),
    propertiesType: typeof(ContactDetailsCardWidgetProperties),
    Description = "Card sourcing contact details from the Content hub",
    IconClass = KenticoIcons.MAP_MARKER)]

namespace Kentico.Community.Portal.Web.Components.Widgets.ContactDetailsCard;

public class ContactDetailsCardWidget(IMediator mediator) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.ContactDetailsCard";
    public const string NAME = "Contact Details Card";

    private readonly IMediator mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ContactDetailsCardWidgetProperties> vm)
    {
        var contentItemGUIDs = vm.Properties.ContactDetails.Select(r => r.Identifier).ToArray();

        var resp = await mediator.Send(new ContactDetailsContentsQuery(contentItemGUIDs));

        return Validate(resp)
            .Match(
                vm => View("~/Components/Widgets/ContactDetailsCard/ContactDetailsCard.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private static Result<ContactDetailsCardWidgetViewModel, ComponentErrorViewModel> Validate(ContactDetailsContentsQueryResponse resp)
    {
        if (resp.Items is not [var item])
        {
            return Result.Failure<ContactDetailsCardWidgetViewModel, ComponentErrorViewModel>(new ComponentErrorViewModel(NAME, ComponentType.Widget, "Please select a valid number of Contact Details."));
        }

        return new ContactDetailsCardWidgetViewModel(item);
    }
}

public class ContactDetailsCardWidgetProperties : BaseWidgetProperties
{
    /// <summary>
    /// Button text.
    /// </summary>
    [ContentItemSelectorComponent(
        contentTypeName: ContactDetailsContent.CONTENT_TYPE_NAME,
        Label = "Contact Details",
        AllowContentItemCreation = true,
        DefaultViewMode = ContentItemSelectorViewMode.List,
        ExplanationText = "Only the first selected item will be used.",
        Order = 1)]
    public IEnumerable<ContentItemReference> ContactDetails { get; set; } = [];
}

public class ContactDetailsCardWidgetViewModel(ContactDetailsContent item) : BaseWidgetViewModel
{
    protected override string WidgetName { get; } = ContactDetailsCardWidget.NAME;

    public string Title { get; set; } = item.ContactDetailsContentTitle;
    public string? PhoneNumber { get; set; } = item.ContactDetailsContentPhoneNumber;
    public string? EmailAddress { get; set; } = item.ContactDetailsContentEmailAddress;
}

public record ContactDetailsContentsQuery(Guid[] ContentItemGUIDs) : IQuery<ContactDetailsContentsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", ContentItemGUIDs);
}

public record ContactDetailsContentsQueryResponse(IReadOnlyList<ContactDetailsContent> Items);
public class ContactDetailsContentQueryHandler(ContentItemQueryTools tools) : ContentItemQueryHandler<ContactDetailsContentsQuery, ContactDetailsContentsQueryResponse>(tools)
{
    public override async Task<ContactDetailsContentsQueryResponse> Handle(ContactDetailsContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(ContactDetailsContent.CONTENT_TYPE_NAME, c =>
        {
            _ = c.Where(w => w.WhereIn(nameof(ContentItemFields.ContentItemGUID), request.ContentItemGUIDs.ToArray()));
        });

        var contents = await Executor.GetMappedResult<ContactDetailsContent>(b, DefaultQueryOptions, cancellationToken);

        return new([.. contents]);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(ContactDetailsContentsQuery query, ContactDetailsContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(result.Items, (l, b) => b.ContentItem(l.SystemFields.ContentItemID));
}

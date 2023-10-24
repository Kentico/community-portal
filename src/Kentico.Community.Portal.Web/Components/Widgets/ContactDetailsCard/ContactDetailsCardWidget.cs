using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Components.Widgets.ContactDetailsCard;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWidget(
    identifier: ContactDetailsCardWidget.IDENTIFIER,
    name: "Contact Details Card",
    viewComponentType: typeof(ContactDetailsCardWidget),
    propertiesType: typeof(ContactDetailsCardWidgetProperties),
    Description = "Card sourcing contact details from the Content Hub.",
    IconClass = "icon-rectangle-a",
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.Widgets.ContactDetailsCard;

public class ContactDetailsCardWidget : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Widget.ContactDetailsCard";

    private readonly IMediator mediator;

    public ContactDetailsCardWidget(IMediator mediator) => this.mediator = mediator;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<ContactDetailsCardWidgetProperties> vm)
    {
        var contentItemGUIDs = vm.Properties.ContactDetails.Select(r => r.Identifier).ToArray();

        var res = await mediator.Send(new ContactDetailsContentsQuery(contentItemGUIDs));

        if (res.Items is not [var item])
        {
            ModelState.AddModelError("", "Please select a valid number of Contact Details.");

            return View("~/Components/ComponentError.cshtml");
        }


        var model = new ContactDetailsCardWidgetViewModel(item);

        return View("~/Components/Widgets/ContactDetailsCard/ContactDetailsCard.cshtml", model);
    }
}

public class ContactDetailsCardWidgetProperties : IWidgetProperties
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
    public IEnumerable<ContentItemReference> ContactDetails { get; set; } = Enumerable.Empty<ContentItemReference>();
}

public class ContactDetailsCardWidgetViewModel
{
    public ContactDetailsCardWidgetViewModel(ContactDetailsContent item)
    {
        Title = item.ContactDetailsContentTitle;
        PhoneNumber = item.ContactDetailsContentPhoneNumber;
        EmailAddress = item.ContactDetailsContentEmailAddress;
    }

    public string Title { get; set; } = "";
    public string? PhoneNumber { get; set; } = null;
    public string? EmailAddress { get; set; } = null;
}

public record ContactDetailsContentsQuery(Guid[] ContentItemGUIDs) : IQuery<ContactDetailsContentsQueryResponse>, ICacheByValueQuery
{
    public string CacheValueKey => string.Join(",", ContentItemGUIDs);
}

public record ContactDetailsContentsQueryResponse(IReadOnlyList<ContactDetailsContent> Items);
public class ContactDetailsContentQueryHandler : ContentItemQueryHandler<ContactDetailsContentsQuery, ContactDetailsContentsQueryResponse>
{
    public ContactDetailsContentQueryHandler(ContentItemQueryTools tools) : base(tools) { }

    public override async Task<ContactDetailsContentsQueryResponse> Handle(ContactDetailsContentsQuery request, CancellationToken cancellationToken = default)
    {
        var b = new ContentItemQueryBuilder().ForContentType(ContactDetailsContent.CONTENT_TYPE_NAME, c =>
        {
            _ = c.Where(w => w.WhereIn(nameof(ContentItemFields.ContentItemGUID), request.ContentItemGUIDs.ToArray()));
        });

        var contents = await Executor.GetResult(b, ContentItemMapper.Map<ContactDetailsContent>, DefaultQueryOptions, cancellationToken);

        return new(contents.ToList());
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(ContactDetailsContentsQuery query, ContactDetailsContentsQueryResponse result, ICacheDependencyKeysBuilder builder) =>
        builder.Collection(result.Items, (l, b) => b.ContentItem(l.SystemFields.ContentItemID));
}

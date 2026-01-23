using CMS.ContentEngine;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.Image;
using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: ImageWidget.IDENTIFIER,
    name: "Image",
    componentType: typeof(ImageWidget),
    IconClass = KenticoIcons.CAMERA,
    PropertiesType = typeof(ImageWidgetProperties),
    Description = "Displays an image from the Content hub.")]

namespace Kentico.Community.Portal.Web.Components.EmailBuilder.Widgets.Image;

public partial class ImageWidget : ComponentBase
{
    public const string IDENTIFIER = "KenticoCommunity.EmailBuilder.Widgets.Image";

    [Inject] public required IEmailContextAccessor EmailContextAccessor { get; set; }
    [Inject] public required IContentRetriever ContentRetriever { get; set; }

    [Parameter] public required ImageWidgetProperties Properties { get; set; }

    public Maybe<ImageContent> Model { get; set; }
    public EmailContext EmailContext => field ??= EmailContextAccessor.GetContext();

    protected override async Task OnInitializedAsync()
    {
        if (!Properties.SelectedImages.Any())
        {
            return;
        }

        var emailContext = EmailContextAccessor.GetContext();

        var pages = await ContentRetriever.RetrieveContentByGuids<ImageContent>(
            Properties.SelectedImages.Select(i => i.Identifier),
            new RetrieveContentParameters { IsForPreview = emailContext.BuilderMode != EmailBuilderMode.Off, LanguageName = emailContext.LanguageName, LinkedItemsMaxLevel = 1 },
            RetrieveContentQueryParameters.Default,
            RetrievalCacheSettings.CacheDisabled);

        Model = pages.TryFirst();
    }
}

public class ImageWidgetProperties : IEmailWidgetProperties
{
    [ContentItemSelectorComponent(
        ImageContent.CONTENT_TYPE_NAME,
        Label = "Selected image",
        AllowContentItemCreation = false,
        MinimumItems = 1,
        MaximumItems = 1,
        Order = 1)]
    public IEnumerable<ContentItemReference> SelectedImages { get; set; } = [];
}

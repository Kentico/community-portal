# Example widgets

## Card Widget

CardWidgetViewComponent.cs

```csharp
using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(CardWidgetViewComponent.IDENTIFIER, typeof(CardWidgetViewComponent), "{$dancinggoat.cardwidget.title$}", typeof(CardWidgetProperties), Description = "{$dancinggoat.cardwidget.description$}", IconClass = "icon-rectangle-paragraph")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Controller for card widget.
    /// </summary>
    public class CardWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.LandingPage.CardWidget";


        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of <see cref="CardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public CardWidgetViewComponent(IContentRetriever contentRetriever) => this.contentRetriever = contentRetriever;


        public async Task<ViewViewComponentResult> InvokeAsync(CardWidgetProperties properties)
        {
            var image = await GetImage(properties);

            return View("~/Components/Widgets/CardWidget/_CardWidget.cshtml", new CardWidgetViewModel
            {
                ImagePath = image?.ImageFile.Url,
                Text = properties.Text
            });
        }


        private async Task<Image> GetImage(CardWidgetProperties properties)
        {
            var image = properties?.Image?.FirstOrDefault();

            if (image == null)
            {
                return null;
            }

            var result = await contentRetriever.RetrieveContentByGuids<Image>(
                [image.Identifier],
                HttpContext.RequestAborted
            );

            return result.FirstOrDefault();
        }
    }
}

```

CardWidgetProperties.cs

```csharp
using CMS.ContentEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Card widget properties.
    /// </summary>
    public class CardWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Image to be displayed.
        /// </summary>
        [ContentItemSelectorComponent(Models.Image.CONTENT_TYPE_NAME, Label = "{$dancinggoat.cardwidget.image.label$}", Order = 1)]
        public IEnumerable<ContentItemReference> Image { get; set; } = [];

        /// <summary>
        /// Text to be displayed.
        /// </summary>
        public string Text { get; set; }
    }
}
```

CardWidgetViewModel.cs

```csharp
namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Card widget.
    /// </summary>
    public class CardWidgetViewModel
    {
        /// <summary>
        /// Card background image path.
        /// </summary>
        public string ImagePath { get; set; }


        /// <summary>
        /// Card text.
        /// </summary>
        public string Text { get; set; }
    }
}
```

\_CardWidget.cshtml

```cshtml
@using DancingGoat.InlineEditors
@using DancingGoat.Widgets

@model DancingGoat.Widgets.CardWidgetViewModel

@{
    string styleAttribute = null;
    if (!string.IsNullOrEmpty(Model.ImagePath))
    {
        styleAttribute = $"style=\"background-image: url('{Url.Content(HTMLHelper.HTMLEncode(Model.ImagePath))}');\"";
    }
}

<div class="card-body">
    <section class="card-section" @Html.Raw(styleAttribute)>
        <div class="card-text">
            @if (Context.Kentico().PageBuilder().EditMode)
            {
                <partial name="~/Components/InlineEditors/TextEditor/_TextEditor.cshtml"
                         model="new TextEditorViewModel
                                {
                                    PropertyName = nameof(CardWidgetProperties.Text),
                                    Text = Model.Text,
                                }" />
            }
            else
            {
                @Model.Text
            }
        </div>
    </section>
</div>
```

## Simple Call To Action Widget

CallToActionWidgetViewComponent.cs

```csharp
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using TrainingGuides.Web.Features.LandingPages.Widgets.CallToAction;
using TrainingGuides.Web.Features.LandingPages.Widgets.SimpleCallToAction;
using TrainingGuides.Web.Features.Shared.Services;

[assembly:
    RegisterWidget(
        identifier: SimpleCallToActionWidgetViewComponent.IDENTIFIER,
        viewComponentType: typeof(SimpleCallToActionWidgetViewComponent),
        name: "Simple call to action",
        propertiesType: typeof(SimpleCallToActionWidgetProperties),
        Description = $"Displays a call to action button. Simpler configuration options than {CallToActionWidgetViewComponent.NAME} widget.",
        IconClass = "icon-bubble")]

namespace TrainingGuides.Web.Features.LandingPages.Widgets.SimpleCallToAction;

public class SimpleCallToActionWidgetViewComponent : ViewComponent
{
    public const string IDENTIFIER = "TrainingGuides.SimpleCallToActionWidget";

    private readonly IContentRetriever contentRetriever;

    public SimpleCallToActionWidgetViewComponent(IContentRetriever contentRetriever)
    {
        this.contentRetriever = contentRetriever;
    }

    public async Task<ViewViewComponentResult> InvokeAsync(SimpleCallToActionWidgetProperties properties)
    {

        string targetUrl = properties.TargetContent switch
        {
            nameof(TargetContentOption.Page) => await GetWebPageUrl(properties.TargetContentPage?.FirstOrDefault()) ?? string.Empty,
            nameof(TargetContentOption.AbsoluteUrl) => properties.TargetContentAbsoluteUrl,
            _ => string.Empty
        };

        var model = new SimpleCallToActionWidgetViewModel()
        {
            Text = properties.Text,
            Url = targetUrl,
            OpenInNewTab = properties?.OpenInNewTab ?? false,
        };

        return View("~/Features/LandingPages/Widgets/SimpleCallToAction/SimpleCallToACtionWidget.cshtml", model);
    }

    private async Task<string?> GetWebPageUrl(ContentItemReference? webPage)
    {
        if (webPage is not null)
        {
            var page = await contentRetriever.RetrieveContentByGuids([webPage.Identifier]).FirstOrDefault();
            return page?.GetUrl()?.RelativePath ?? string.Empty;
        }
        return string.Empty;
    }
}
```

SimpleCallToActionWidgetProperties.cs

```csharp
using System.ComponentModel;
using CMS.ContentEngine;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using TrainingGuides.Web.Features.Shared.OptionProviders;

namespace TrainingGuides.Web.Features.LandingPages.Widgets.SimpleCallToAction;

public class SimpleCallToActionWidgetProperties : IWidgetProperties
{
    [TextInputComponent(
        Label = "Call to action text",
        ExplanationText = "Add your call to action. Keep it under 30 characters.",
        Order = 10)]
    public string Text { get; set; } = string.Empty;

    // See the same property implemented using RadioGroupComponent instead in CallToAction widget.
    [DropDownComponent(
        Label = "Target content",
        ExplanationText = "Select what happens when a visitor clicks your button.",
        DataProviderType = typeof(DropdownEnumOptionProvider<TargetContentOption>),
        Order = 20)]
    public string TargetContent { get; set; } = nameof(TargetContentOption.Page);

    [ContentItemSelectorComponent(
        [
            ArticlePage.CONTENT_TYPE_NAME,
            DownloadsPage.CONTENT_TYPE_NAME,
            EmptyPage.CONTENT_TYPE_NAME,
            LandingPage.CONTENT_TYPE_NAME,
            ProductPage.CONTENT_TYPE_NAME,
            ProfilePage.CONTENT_TYPE_NAME
        ],
        Label = "Target page",
        ExplanationText = "Select the page in the tree.",
        MaximumItems = 1,
        Order = 30)]
    [VisibleIfEqualTo(nameof(TargetContent), nameof(TargetContentOption.Page), StringComparison.OrdinalIgnoreCase)]
    public IEnumerable<ContentItemReference> TargetContentPage { get; set; } = [];

    [TextInputComponent(
        Label = "Absolute URL",
        ExplanationText = "Add a hyperlink to an external site, or use the product's URL + anchor tag # for referencing an anchor on the page.",
        Order = 40)]
    [VisibleIfEqualTo(nameof(TargetContent), nameof(TargetContentOption.AbsoluteUrl), StringComparison.OrdinalIgnoreCase)]
    public string TargetContentAbsoluteUrl { get; set; } = string.Empty;

    [CheckBoxComponent(
        Label = "Open in new tab",
        Order = 50)]
    public bool OpenInNewTab { get; set; } = false;
}

public enum TargetContentOption
{
    [Description("Page")]
    Page,
    [Description("Absolute URL")]
    AbsoluteUrl
}
```

SimpleCallToActionWidgetViewModel.cs

```csharp
namespace TrainingGuides.Web.Features.LandingPages.Widgets.SimpleCallToAction;

public class SimpleCallToActionWidgetViewModel
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; } = false;
}
```

\_SimpleCallToACtionWidget.cshtml

```cshtml
@using TrainingGuides.Web.Features.LandingPages.Widgets.SimpleCallToAction
@model SimpleCallToActionWidgetViewModel

@if (Model == null || string.IsNullOrWhiteSpace(Model.Text) || string.IsNullOrWhiteSpace(Model.Url))
{
    <tg-page-builder-content>
        <tg-configure-widget-instructions />
    </tg-page-builder-content>

    return;
}

@if (Model != null)
{
    <div class="text-center">
        <a href="@Model.Url" class="btn tg-btn-secondary text-uppercase my-4" target="@(Model.OpenInNewTab ? "_blank" : "")">
            @Model.Text
        </a>
    </div>
}
```

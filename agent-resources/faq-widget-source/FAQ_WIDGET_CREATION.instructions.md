# Page Builder Widget Requirements

## Widget Overview

### Widget Identification

- **Widget Name**: FAQ
- **Widget Identifier**: CommunityPortal.FAQWidget
  - _Format: `CompanyName.WidgetName`_
- **Description**: Displays FAQ items in an expandable accordion format with
  optional expand/collapse all controls
- **Icon Class**: KenticoIcons.SPEECH_BUBBLE_COMMENT

### Purpose

Display FAQ content items in an accessible accordion interface using Bootstrap
5.3 components. Supports two data source modes: selecting multiple individual
FAQ items or a single FAQ group. Multiple widget instances can coexist on the
same page with independent functionality.

### Core Functionality

- Select either multiple `FAQItemContent` items OR a single `FAQGroupContent`
  item as data source
- Display FAQs in Bootstrap 5.3 accordion format with expand/collapse
  functionality
- Optional expand all/collapse all button group
- Support multiple widget instances per page with unique IDs
- Use ES Module pattern for JavaScript (not IIFE)
- Render markdown content in FAQ answers

## Widget Properties

| Property Name              | Type                                    | Form Component               | Required    | Default Value    | Description                                                                                |
| -------------------------- | --------------------------------------- | ---------------------------- | ----------- | ---------------- | ------------------------------------------------------------------------------------------ |
| Label                      | string                                  | TextInputComponent           | No          | ""               | Optional heading displayed above the FAQ accordion                                         |
| DataSource                 | string (enum)                           | DropDownComponent            | Yes         | Individual_Items | Choose between "Individual Items" or "FAQ Group"                                           |
| FAQItems                   | IEnumerable&lt;ContentItemReference&gt; | ContentItemSelectorComponent | Conditional | []               | Select multiple FAQItemContent items (visible when DataSource = Individual_Items)          |
| FAQGroup                   | IEnumerable&lt;ContentItemReference&gt; | ContentItemSelectorComponent | Conditional | []               | Select single FAQGroupContent item (visible when DataSource = FAQ_Group, MaximumItems = 1) |
| ShowExpandCollapseControls | bool                                    | CheckBoxComponent            | No          | true             | Show/hide the expand all/collapse all button group                                         |

## Data Requirements

### External Data Sources

| Content Type Name               | Property Name                | Description                   |
| ------------------------------- | ---------------------------- | ----------------------------- |
| KenticoCommunity.FAQItemContent | FAQItemContentQuestion       | The question text             |
| KenticoCommunity.FAQItemContent | FAQItemContentAnswerMarkdown | The answer in markdown format |

**Note**: `FAQGroupContent` does not exist yet in the codebase. The widget
should be implemented to support individual FAQ items first. The FAQ Group
functionality can be added later.

### Data Retrieval Logic

1. Use `IContentRetriever` service to retrieve FAQ items by GUIDs
2. Use `RetrieveContentByGuids<FAQItemContent>()` method with:
   - Array of ContentItemGUIDs from widget properties
   - `RetrieveContentParameters` for query configuration
   - Custom query filtering with `WhereIn` to match GUID order
3. Configure caching with `RetrievalCacheSettings` including suffix for cache
   key uniqueness
4. Order results to match input GUID order for consistent display

### Dependencies

- `IContentRetriever` - for content item retrieval
- `ISlugHelper` - for generating unique IDs from question text
- `IMarkdownParser` - for rendering markdown answers (if available, otherwise
  use plain text)

## View/Presentation

### View Model Structure

| Property Name              | Type                                | Source            | Description                                 |
| -------------------------- | ----------------------------------- | ----------------- | ------------------------------------------- |
| Label                      | string                              | Properties        | Optional heading for the FAQ section        |
| FAQItems                   | IReadOnlyList&lt;FAQItemContent&gt; | IContentRetriever | Collection of FAQ items to display          |
| ShowExpandCollapseControls | bool                                | Properties        | Whether to show expand/collapse all buttons |
| WidgetInstanceID           | Guid                                | Properties.ID     | Unique ID for this widget instance          |

### HTML Structure

```html
<div xpc-preview-outline="FAQ Widget" xpc-preview-outline-remove-element="true">
  <!-- Optional Label as h2 with generated ID -->
  @if (!string.IsNullOrWhiteSpace(Model.Label))
  {
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
      <h2 id="faq-[slug]" class="h4 mb-0">
        @Model.Label
        <a title="Navigate to this heading" aria-label="Navigate to this heading"
           class="heading-link" href="#faq-[slug]"></a>
      </h2>

      <!-- Expand/Collapse controls (only if ShowExpandCollapseControls && FAQItems.Count > 1) -->
      @if (Model.ShowExpandCollapseControls && Model.FAQItems.Count > 1)
      {
        <div class="btn-group" role="group" aria-label="FAQ controls">
          <button type="button" class="btn btn-outline-secondary btn-sm"
                  data-faq-action="expand" data-faq-target="faq-accordion-[WidgetInstanceID]">
            Expand all
          </button>
          <button type="button" class="btn btn-outline-secondary btn-sm"
                  data-faq-action="collapse" data-faq-target="faq-accordion-[WidgetInstanceID]">
            Collapse all
          </button>
        </div>
      }
    </div>
  }
  @else if (Model.ShowExpandCollapseControls && Model.FAQItems.Count > 1)
  {
    <!-- Show controls without label -->
    <div class="d-flex justify-content-end mb-3">
      <div class="btn-group" role="group" aria-label="FAQ controls">
        <button type="button" class="btn btn-outline-secondary btn-sm"
                data-faq-action="expand" data-faq-target="faq-accordion-[WidgetInstanceID]">
          Expand all
        </button>
        <button type="button" class="btn btn-outline-secondary btn-sm"
                data-faq-action="collapse" data-faq-target="faq-accordion-[WidgetInstanceID]">
          Collapse all
        </button>
      </div>
    </div>
  }

  <!-- Accordion (no data-bs-parent to allow multiple items open) -->
  <div class="accordion" id="faq-accordion-[WidgetInstanceID]">
    @foreach (var (item, index) in Model.FAQItems.Select((item, index) => (item, index)))
    {
      var itemId = $"faq-item-{Model.WidgetInstanceID}-{index}";
      var headerId = $"{itemId}-header";
      var collapseId = $"{itemId}-collapse";

      <div class="accordion-item">
        <h3 class="accordion-header" id="@headerId">
          <button class="accordion-button collapsed" type="button"
                  data-bs-toggle="collapse" data-bs-target="#@collapseId"
                  aria-expanded="false" aria-controls="@collapseId">
            @item.FAQItemContentQuestion
          </button>
        </h3>
        <div id="@collapseId" class="accordion-collapse collapse" aria-labelledby="@headerId">
          <div class="accordion-body">
            @* Render markdown or plain text *@
            @Html.Raw(RenderAnswer(item.FAQItemContentAnswerMarkdown))
          </div>
        </div>
      </div>
    }
  </div>
</div>
```

### Styling Requirements

- Use Bootstrap 5.3 accordion classes (`.accordion`, `.accordion-item`,
  `.accordion-header`, `.accordion-button`, `.accordion-collapse`,
  `.accordion-body`)
- Use Bootstrap button group for expand/collapse controls (`.btn-group`, `.btn`,
  `.btn-outline-secondary`, `.btn-sm`)
- Use Bootstrap flex utilities for layout (`.d-flex`,
  `.justify-content-between`, `.align-items-center`, `.gap-2`, `.mb-3`)
- Follow existing heading link pattern with `.heading-link` class
- Ensure `xpc-preview-outline` attribute for Page Builder preview mode

### Responsive Behavior

- Button group should wrap on small screens using `.flex-wrap`
- Accordion is responsive by default via Bootstrap
- No custom breakpoints needed

## JavaScript & Client-side Requirements

Place JavaScript inline in the view using ES Module pattern in a
`<script type="module">` block at the end of the view:

```html
<script type="module">
  // Use unique IDs from WidgetInstanceID for all selectors
  const accordionId = "faq-accordion-@Model.WidgetInstanceID";
  const accordion = document.getElementById(accordionId);

  if (!accordion) return;

  const collapseElements = accordion.querySelectorAll(".accordion-collapse");
  const expandBtn = document.querySelector(`[data-faq-action="expand"][data-faq-target="${accordionId}"]`);
  const collapseBtn = document.querySelector(`[data-faq-action="collapse"][data-faq-target="${accordionId}"]`);

  function setAllAccordions(shouldExpand) {
    collapseElements.forEach(el => {
      const instance = bootstrap.Collapse.getOrCreateInstance(el, { toggle: false });
      shouldExpand ? instance.show() : instance.hide();
    });
  }

  expandBtn?.addEventListener("click", () => setAllAccordions(true));
  collapseBtn?.addEventListener("addEventListener("click", () => setAllAccordions(false));
</script>
```

**Key Points:**

- Use `type="module"` on script tag
- Use data attributes for targeting: `data-faq-action` and `data-faq-target`
- Reference Bootstrap's `bootstrap.Collapse` API
- Use unique accordion ID from `WidgetInstanceID` for isolation
- No IIFE - ES Module scope provides isolation

## Registration Details

Use `RegisterWidget` assembly attribute at the top of the widget view component
file:

```csharp
[assembly: RegisterWidget(
    identifier: FAQWidget.IDENTIFIER,
    viewComponentType: typeof(FAQWidget),
    name: FAQWidget.NAME,
    propertiesType: typeof(FAQWidgetProperties),
    Description = "Displays FAQ items in an expandable accordion format",
    IconClass = KenticoIcons.SPEECH_BUBBLE_COMMENT,
    AllowCache = true)]
```

Add constant to `WidgetIdentifiers.cs`:

```csharp
public const string FAQ = FAQWidget.IDENTIFIER;
```

## Widget File Structure

### 1. View Component: `FAQWidget.cs`

**Location**:
`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/FAQ/FAQWidget.cs`

**Purpose**: Widget controller logic, validation, and view model creation

**Key Methods**:

- `InvokeAsync`: Main entry point, retrieves FAQ items, validates, returns view
- `Validate`: Checks for empty FAQ items, returns Result type
- `RenderAnswer`: Helper to convert markdown to HTML (or return plain text)

**Structure**:

```csharp
using CMS.ContentEngine;
using Kentico.Community.Portal.Core.Components;
using Kentico.Community.Portal.Core.Content;
using Kentico.Community.Portal.Web.Components;
using Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.FAQ;
using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.AspNetCore.Mvc;
using Slugify;

[assembly: RegisterWidget(
    identifier: FAQWidget.IDENTIFIER,
    viewComponentType: typeof(FAQWidget),
    name: FAQWidget.NAME,
    propertiesType: typeof(FAQWidgetProperties),
    Description = "Displays FAQ items in an expandable accordion format",
    IconClass = KenticoIcons.SPEECH_BUBBLE_COMMENT,
    AllowCache = true)]

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.FAQ;

public class FAQWidget(IContentRetriever contentRetriever, ISlugHelper slugHelper) : ViewComponent
{
    public const string IDENTIFIER = "CommunityPortal.Components.Widgets.FAQ";
    public const string NAME = "FAQ";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly ISlugHelper slugHelper = slugHelper;

    public async Task<IViewComponentResult> InvokeAsync(ComponentViewModel<FAQWidgetProperties> cvm)
    {
        var props = cvm.Properties;

        // Get FAQ item GUIDs based on data source
        var faqItemGUIDs = (props.FAQItems ?? []).Select(i => i.Identifier).ToList();

        if (faqItemGUIDs.Count == 0)
        {
            return View("~/Components/ComponentError.cshtml",
                new ComponentErrorViewModel(NAME, ComponentType.Widget,
                    "Select at least 1 FAQ Item."));
        }

        // Retrieve FAQ items using IContentRetriever
        var faqItems = await contentRetriever.RetrieveContentByGuids<FAQItemContent>(
            faqItemGUIDs,
            new RetrieveContentParameters(),
            query => query.Where(w => w.WhereIn(
                nameof(ContentItemFields.ContentItemGUID),
                faqItemGUIDs)),
            new RetrievalCacheSettings($"{nameof(FAQWidget)}|{string.Join(",", faqItemGUIDs)}")
        );

        // Order by original GUID order
        var orderedItems = faqItems
            .OrderBy(item => faqItemGUIDs.IndexOf(item.SystemFields.ContentItemGUID))
            .ToList();

        return Validate(orderedItems, props)
            .Match(
                vm => View("~/Components/PageBuilder/Widgets/FAQ/FAQ.cshtml", vm),
                vm => View("~/Components/ComponentError.cshtml", vm)
            );
    }

    private Result<FAQWidgetViewModel, ComponentErrorViewModel> Validate(
        IReadOnlyList<FAQItemContent> items,
        FAQWidgetProperties props)
    {
        if (items.Count == 0)
        {
            return Result.Failure<FAQWidgetViewModel, ComponentErrorViewModel>(
                new ComponentErrorViewModel(NAME, ComponentType.Widget,
                    "No FAQ items found."));
        }

        return new FAQWidgetViewModel(props, items, slugHelper);
    }
}

public class FAQWidgetProperties : BaseWidgetProperties
{
    [TextInputComponent(
        Label = "Label",
        ExplanationText = "Optional heading displayed above the FAQ section",
        Order = 1
    )]
    public string Label { get; set; } = "";

    [ContentItemSelectorComponent(
        contentTypeName: FAQItemContent.CONTENT_TYPE_NAME,
        Label = "FAQ Items",
        ExplanationText = "Select FAQ items to display",
        AllowContentItemCreation = true,
        DefaultViewMode = ContentItemSelectorViewMode.List,
        Order = 2
    )]
    public IEnumerable<ContentItemReference> FAQItems { get; set; } = [];

    [CheckBoxComponent(
        Label = "Show Expand/Collapse All Controls",
        ExplanationText = "Display buttons to expand or collapse all FAQ items at once",
        Order = 3
    )]
    public bool ShowExpandCollapseControls { get; set; } = true;
}

public class FAQWidgetViewModel
{
    public string Label { get; }
    public IReadOnlyList<FAQItemContent> FAQItems { get; }
    public bool ShowExpandCollapseControls { get; }
    public Guid WidgetInstanceID { get; }

    public FAQWidgetViewModel(
        FAQWidgetProperties props,
        IReadOnlyList<FAQItemContent> items,
        ISlugHelper slugHelper)
    {
        Label = props.Label;
        FAQItems = items;
        ShowExpandCollapseControls = props.ShowExpandCollapseControls;
        WidgetInstanceID = props.ID;
    }

    public string GenerateSlug(string text) =>
        string.IsNullOrWhiteSpace(text) ? "" : slugHelper.GenerateSlug(text);
}
```

### 2. View: `FAQ.cshtml`

**Location**:
`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/FAQ/FAQ.cshtml`

**Purpose**: Razor view template rendering HTML and inline JavaScript

**Key Features**:

- Render optional label with heading link
- Render expand/collapse controls conditionally
- Loop through FAQ items in accordion structure
- Generate unique IDs using WidgetInstanceID
- Inline ES Module script for functionality
- Use `@Html.Raw()` for markdown rendering (ensure content is sanitized)

### 3. Update WidgetIdentifiers.cs

**Location**:
`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/WidgetIdentifiers.cs`

Add constant:

```csharp
public const string FAQ = FAQWidget.IDENTIFIER;
```

## Implementation Checklist

- [ ] Create `FAQWidget.cs` view component in
      `/Components/PageBuilder/Widgets/FAQ/`
- [ ] Inject `IContentRetriever` and `ISlugHelper` dependencies
- [ ] Implement widget registration attribute
- [ ] Create `FAQWidgetProperties` with all required properties
- [ ] Create `FAQWidgetViewModel` with data transformation
- [ ] Implement content retrieval using
      `IContentRetriever.RetrieveContentByGuids`
- [ ] Configure caching with `RetrievalCacheSettings`
- [ ] Order retrieved items to match input GUID order
- [ ] Implement validation logic for empty items
- [ ] Create `FAQ.cshtml` view with Bootstrap accordion markup
- [ ] Add unique ID generation using `WidgetInstanceID`
- [ ] Implement conditional rendering for label and controls
- [ ] Add inline ES Module script for expand/collapse functionality
- [ ] Use data attributes for JavaScript targeting
- [ ] Test markdown rendering (or plain text fallback)
- [ ] Add widget identifier to `WidgetIdentifiers.cs`
- [ ] Test multiple widget instances on same page
- [ ] Verify accessibility (ARIA attributes, semantic HTML)
- [ ] Test expand/collapse all functionality
- [ ] Verify Bootstrap 5.3 compatibility

## Additional Notes

### Markdown Rendering

- Check if `IMarkdownParser` or similar service exists in the project
- If not available, use plain text rendering initially
- Ensure any HTML output is properly sanitized to prevent XSS

### Future Enhancement: FAQ Group Support

The requirements mention `FAQGroupContent` but this content type doesn't exist
yet. The initial implementation should focus on individual FAQ items. When FAQ
groups are added:

1. Add `DataSource` enum property with "Individual Items" and "FAQ Group"
   options
2. Add `FAQGroup` property with single-item selector
3. Add conditional visibility logic in properties
4. Create separate query handler for FAQ groups
5. Update widget logic to handle both data source types

### Bootstrap Dependency

- Widget assumes Bootstrap 5.3 JS is loaded globally
- Widget uses Bootstrap Collapse component
- No additional CSS/JS files needed

### Accessibility

- Use semantic HTML (`<h2>`, `<h3>` for headings)
- Include proper ARIA attributes (`aria-expanded`, `aria-controls`,
  `aria-labelledby`)
- Ensure keyboard navigation works
- Provide descriptive button labels

### Performance

- Widget is marked with `AllowCache = true`
- Uses `RetrievalCacheSettings` with unique cache key suffix
- `IContentRetriever` automatically handles cache dependencies for content items

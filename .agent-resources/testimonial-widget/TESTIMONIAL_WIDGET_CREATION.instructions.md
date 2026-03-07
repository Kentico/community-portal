# Page Builder Widget Requirements

## Widget Overview

### Widget Identification

- **Widget Name**: Testimonial
- **Widget Identifier**: CommunityPortal.Components.Widgets.Testimonial
  - _Format: `CompanyName.WidgetName` (e.g., `DancingGoat.CardWidget`)_
- **Description**: Displays one testimonial content item in either Featured or
  Simple layout with configurable visual theme and optional profile details
- **Icon Class**: KenticoIcons.SPEECH_BUBBLE_COMMENT

### Purpose

Render a single `TestimonialContent` item from Content Hub as a reusable Page
Builder widget. Editors can pick one testimonial and control presentation using
layout/theme dropdowns and visibility toggles for title, photo, and employment
information.

### Core Functionality

- Select exactly one linked `TestimonialContent` item via Combined Content
  Selector
- Provide two presentation layouts: `Featured` and `Simple`
- Provide theme selection: `Primary` and `Secondary`
- Provide visibility toggles for title, photo, and employment fields
- Map properties and retrieved content into a dedicated widget view model via
  constructor
- Select view path by layout in widget view component
- Use switch expressions in Razor views for layout/theme-driven CSS classes and
  attributes
- Reuse Bootstrap 5.3 utilities and existing SCSS/CSS variable approach in
  `Client/styles`

## Widget Properties

| Property Name  | Type                              | Form Component               | Required | Default Value | Description                                                                                                                        |
| -------------- | --------------------------------- | ---------------------------- | -------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Testimonial    | IEnumerable<ContentItemReference> | ContentItemSelectorComponent | Yes      | []            | Required single testimonial content item (`MaximumItems = 1`, `MinimumItems = 1`) scoped to `TestimonialContent.CONTENT_TYPE_NAME` |
| Layout         | string (enum)                     | DropDownComponent            | Yes      | Featured      | Visual layout variant (`Featured` or `Simple`)                                                                                     |
| Theme          | string (enum)                     | DropDownComponent            | Yes      | Primary       | Visual theme variant (`Primary` or `Secondary`)                                                                                    |
| ShowTitle      | bool                              | CheckBoxComponent            | No       | true          | Controls rendering of `TestimonialContentTitle`                                                                                    |
| ShowPhoto      | bool                              | CheckBoxComponent            | No       | true          | Controls rendering of testimonial photo                                                                                            |
| ShowEmployment | bool                              | CheckBoxComponent            | No       | true          | Controls rendering of job title + employer                                                                                         |

## Data Requirements

### External Data Sources

| Content Type Name                   | Property Name              | Description                        |
| ----------------------------------- | -------------------------- | ---------------------------------- |
| KenticoCommunity.TestimonialContent | TestimonialContentTitle    | Optional testimonial heading/title |
| KenticoCommunity.TestimonialContent | TestimonialContentMessage  | Main testimonial quote/message     |
| KenticoCommunity.TestimonialContent | TestimonialContentName     | Person name                        |
| KenticoCommunity.TestimonialContent | TestimonialContentPhoto    | Linked photo content               |
| KenticoCommunity.TestimonialContent | TestimonialContentJobTitle | Person job title                   |
| KenticoCommunity.TestimonialContent | TestimonialContentEmployer | Person employer                    |

### Data Retrieval Logic

Use `IContentRetriever` and `RetrieveContentByGuids<TestimonialContent>()` with
GUIDs from the selected content item reference. Include
`LinkedItemsMaxLevel = 1` so photo data is available. Validate null/empty
selection and missing/deleted content item; when invalid, return
`~/Components/ComponentError.cshtml` with a clear widget-specific message.

### Dependencies

- `IContentRetriever` for testimonial content retrieval
- `Kentico.Community.Portal.Core.Content.TestimonialContent` generated model
- `ImageAssetViewModel` (or equivalent existing image VM utility) for photo
  rendering if image is displayed

## View/Presentation

### View Model Structure

| Property Name  | Type                 | Source                  | Description                                                  |
| -------------- | -------------------- | ----------------------- | ------------------------------------------------------------ |
| Layout         | TestimonialLayout    | Widget properties       | Parsed layout enum used for view selection and class mapping |
| Theme          | TestimonialTheme     | Widget properties       | Parsed theme enum used for class/attribute mapping           |
| ShowTitle      | bool                 | Widget properties       | Whether title is rendered                                    |
| ShowPhoto      | bool                 | Widget properties       | Whether photo is rendered                                    |
| ShowEmployment | bool                 | Widget properties       | Whether employment block is rendered                         |
| Title          | string               | TestimonialContent      | Testimonial heading                                          |
| Message        | string               | TestimonialContent      | Testimonial quote/message                                    |
| Name           | string               | TestimonialContent      | Person name                                                  |
| JobTitle       | string               | TestimonialContent      | Employment title                                             |
| Employer       | string               | TestimonialContent      | Employment company                                           |
| Photo          | ImageAssetViewModel? | TestimonialContentPhoto | Optional mapped image asset                                  |
| ComponentName  | string               | BaseWidgetViewModel     | Preview outline label                                        |

### HTML Structure

Two separate views are required, one per layout:

- `Testimonial_Featured.cshtml`
- `Testimonial_Simple.cshtml`

Each view should:

- Wrap output with `xpc-preview-outline="@Model.ComponentName"`
- Render quote message and person name
- Conditionally render title, photo, and employment block based on booleans
- Use switch expressions in Razor to map enum values to CSS classes/attributes,
  e.g.:
  - theme wrapper class
  - data/theme attribute
  - layout-specific utility class composition

### Styling Requirements

- Use Bootstrap 5.3 utility classes and existing component style conventions
- Add widget-specific SCSS in
  `src/Kentico.Community.Portal.Web/Client/styles/01_component/_testimonial.scss`
- Import new stylesheet from
  `src/Kentico.Community.Portal.Web/Client/styles/style.scss`
- Use CSS custom properties and existing theme tokens; avoid hard-coded new
  colors/fonts/shadows unless already defined by current design tokens
- Keep class naming consistent with existing component naming style

### Responsive Behavior

- Featured layout should support stacked mobile rendering and enhanced spacing
  at larger breakpoints
- Simple layout should remain compact and readable on all sizes
- Photo should scale responsively and avoid layout shifts

## JavaScript & Client-side Requirements

No JavaScript is required for this widget. Keep rendering fully server-side.

## Registration Details

Ensure that newly created widget is registered using the RegisterWidget
attribute.

Also update
`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/WidgetIdentifiers.cs`
with a new `Testimonial` constant mapped to `TestimonialWidget.IDENTIFIER`.

## Widget File Structure

`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/Testimonial/TestimonialWidget.cs`

- Register widget via assembly attribute
- Implement `TestimonialWidget : ViewComponent`
- Include properties class, view model class, and enums (`TestimonialLayout`,
  `TestimonialTheme`) in same file, matching existing widget pattern
- Retrieve data, validate, create view model, and select one of two views by
  `Layout` switch

`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/Testimonial/Testimonial_Featured.cshtml`

- Featured testimonial markup with optional photo/employment sections
- Use switch expressions for layout/theme-based class and attribute mappings

`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/Testimonial/Testimonial_Simple.cshtml`

- Simple testimonial markup variant
- Use switch expressions for layout/theme-based class and attribute mappings

`src/Kentico.Community.Portal.Web/Client/styles/01_component/_testimonial.scss`

- Component-level styling for featured/simple variants and theme states
- Reuse existing CSS variable conventions from other component styles

`src/Kentico.Community.Portal.Web/Client/styles/style.scss`

- Add `@import "01_component/testimonial";`

`src/Kentico.Community.Portal.Web/Components/PageBuilder/Widgets/WidgetIdentifiers.cs`

- Add `public const string Testimonial = TestimonialWidget.IDENTIFIER;`

## Implementation Checklist

- Create `Testimonial` widget folder and `TestimonialWidget.cs`
- Register widget with identifier, name, description, icon, and properties type
- Add required content selector property (single testimonial item)
- Add layout/theme dropdown properties using `EnumDropDownOptionsProvider<T>`
- Add `ShowTitle`, `ShowPhoto`, `ShowEmployment` checkbox properties
- Add parsed enum helpers for layout/theme
- Retrieve `TestimonialContent` via `IContentRetriever`
- Validate empty/missing content and return `ComponentError` view on failure
- Build view model via constructor mapping properties + content fields
- Add `Testimonial_Featured.cshtml` and `Testimonial_Simple.cshtml`
- Use switch expressions in Razor to derive classes/attributes
- Add `_testimonial.scss` and import in `style.scss`
- Add constant to `WidgetIdentifiers.cs`
- Verify widget renders in both layouts and both themes with all toggle
  combinations

## Additional Notes

- Follow `base-pagebuilder.instructions.md`: use Combined Content Selector and
  validate null/empty data
- Keep code aligned with existing widget conventions in
  `Components/PageBuilder/Widgets` (single-file properties/viewmodel/enums where
  already used)
- Keep implementation minimal and focused on required UX only: one testimonial,
  two layouts, two themes, and three visibility toggles
- Prefer async APIs and `IContentRetriever` per Xperience conventions

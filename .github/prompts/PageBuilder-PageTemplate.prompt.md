---
mode: "agent"
tools: ["codebase", "findUsages"]
description: "Instructions to create a Page Builder page template"
---

A Page Builder page template...

- Is a class that inherits from `Controller` and has a class name suffixed with
  "TemplateController"
- Renders a specific web page content type
- Has a properties class implementing `IPageTemplateProperties`, matching the
  name of the page template suffixed with "Properties"
- Has a view model class matching the name of the page template, suffixed with
  "ViewModel"
- Has an `Index()` method with the signature
  `public async Task<ActionResult> Index()`
- Returns its view model in a `TemplateResult` from the `Index()` method
- Uses C# primary constructors
- The page template, properties, and view model classes should all be in the
  same file
- Has a Razor view named after the web page content type and the page template
  type. For example a "Default" page template Razor view for the `BlogPostPage`
  content type would be named `BlogPostPage_Default.cshtml`
- Both the page template C# class and Razor view are located in a top level
  feature folder for the template, and the folder should be in the
  ./src/Kentico.Community.Portal.Web/Features folder and not include the
  "PageTemplate" suffix because the folder name represents the feature or
  customer experience, not the technology
- Registers its properties, view, and content type as a page template using the
  `[assembly: RegisterPageTemplate()]` attribute and registers itself as the
  controller to handle a specific content type using the
  `[assembly: RegisterWebPageRoute()]` attribute

Examples can be found the
[project's feature folders](../../src/Kentico.Community.Portal.Web/Features).

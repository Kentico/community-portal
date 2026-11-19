---
applyTo: "**/*.cs;**/*.cshtml"
---

These are conventions to follow when working with Xperience by Kentico.

If you are not 100% confident how an Xperience feature or API works, use the
kentico.docs.mcp MCP server for details on the topic.

# Querying

Use the `Kentico.Content.Web.Mvc.IContentRetriever` interface when querying for
content.

- **Content Retrieval Guide**:
  https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval
- **ContentRetriever API Reference**:
  https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/reference-content-retriever-api

## System Fields of Content Items

For these, use just web search, not MCP!

- **ContentItemFields**:
  https://api-reference.kentico.com/api/CMS.ContentEngine.ContentItemFields.html
- **WebPageFields**:
  https://api-reference.kentico.com/api/CMS.Websites.WebPageFields.html

# Async

Use async variations of methods when both sync and async (`Task` and `Task<T>`
returning) are available.

# Page Templates

Register page templates and define page template controllers for a single web
page content type in the same `.cs` file, naming them
`<ContentTypeName>Templates`.

# Components

When selecting icons for registration attributes for the following components,
always select an icon from `Kentico.Xperience.ComponentIcons.KenticoIcons`.

- Page Builder widgets, sections
- Email Builder widgets, sections
- Form Builder components, sections
- Personalization condition types

For the following components always select an icon from
`Kentico.Xperience.Admin.Base.Icons`

- Page Builder templates
- Email Builder templates
- Admin UI components
- Admin UI categories, page templates, pages and Page Extenders

# Logging

Use the .NET `ILogger<T>` API for all logging operations. Replace any use of
`IEventLogService` with `ILogger<T>` as the perferred API.

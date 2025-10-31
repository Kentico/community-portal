---
applyTo: "**/*.cs;**/*.cshtml"
---

These are conventions to follow when working with Xperience by Kentico.

If you are not 100% confident how an Xperience feature or API works, search the
Kentico docs for details on the topic.

# Querying

Use the `Kentico.Content.Web.Mvc.IContentRetriever` interface when querying for
content.

# Async

Use async variations of methods when both sync and async (`Task` and `Task<T>`
returning) are available.

# Page Templates

Register page templates and define page template controllers for a single web
page content type in the same `.cs` file, naming them
`<ContentTypeName>Templates`.

# Logging

Use the .NET `ILogger<T>` API for all logging operations. Replace any use of
`IEventLogService` with `ILogger<T>` as the perferred API.

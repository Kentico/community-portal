---
applyTo: "**/*.cs;**/*.cshtml"
---

These are conventions to follow when working with Xperience by Kentico

# Querying

Use the `Kentico.Content.Web.Mvc.IContentRetriever` interface when querying for
content.

Information about the available methods, parameters, and uses for this API can
be found at the following URLs

- <https://docs.kentico.com/x/content_retriever_api_xp>
- <https://docs.kentico.com/x/reference_content_retriever_api_xp>

# Async

Use async variations of methods when both sync and async (`Task` and `Task<T>`
returning) are available.

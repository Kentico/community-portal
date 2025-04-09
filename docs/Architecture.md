# Architecture

## Projects

| Name                                   | Purpose                                                                                                     |
| -------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Kentico.Community.Portal.Core          | Xperience by Kentico generated content/data types and types shared across live site and administration      |
| Kentico.Community.Portal.Admin         | Admin-only customizations to the Xperience by Kentico app                                                   |
| Kentico.Community.Portal.Web           | Xperience by Kentico Community Portal ASP.NET Core app                                                      |
| Kentico.Community.Portal.Web.Tests     | Unit and integration tests for Kentico.Community.Portal.Web                                                 |
| Kentico.Community.Portal.Web.E2E.Tests | End-to-end tests in Playwright.NET for Kentico.Community.Portal.Web                                         |
| Kentico.Community.DataCleaner.App      | Console app that processes CI files to clear or replace invalid data imported from a production environment |

## Technologies and Frameworks

- `Kentico.Community.Portal.Admin`
  - [Webpack](https://github.com/webpack/webpack)
  - [React](https://github.com/facebook/react)
  - [Milkdown](https://github.com/Milkdown/milkdown)
- `Kentico.Community.Portal.Web`
  - [Vite.js](https://github.com/vitejs/vite)
  - [HTMX](https://github.com/bigskysoftware/htmx)
  - [Alpine.js](https://github.com/alpinejs/alpine)
  - [Bootstrap 5](https://github.com/twbs/bootstrap)
  - [Milkdown](https://github.com/Milkdown/milkdown)
  - [Prism.js](https://github.com/PrismJS/prism)

## Integrations

### Azure Storage

The solution uses
[Azure Queue Storage](https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction)
and
[Azure Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-overview)
to process support requests in an ASP.NET Core background service.

### ReCaptcha

The solution uses
[Google ReCaptcha v3](https://developers.google.com/recaptcha/docs/v3) to
prevent spam through website forms that do not require authentication.

### Lucene Search

The solution uses
[Kentico.Xperience.Lucene](https://github.com/Kentico/xperience-by-kentico-lucene/)
to provide a web based search experience for blogs and Q&A discussions.

### Google Tag Manager

The solution uses
[Kentico.Xperience.TagManager](https://github.com/Kentico/xperience-by-kentico-tag-manager)
to manage and load each environment's Google Tag Manager tag.

# Architecture

## Projects

| Name                                   | Purpose                                                                                                |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| Kentico.Community.Portal.Core          | Xperience by Kentico generated content/data types and types shared across live site and administration |
| Kentico.Community.Portal.Admin         | Admin-only customizations to the Xperience by Kentico app                                              |
| Kentico.Community.Portal.Web           | Xperience by Kentico Community Portal ASP.NET Core app                                                 |
| Kentico.Community.Portal.Web.Tests     | Unit and integration tests for Kentico.Community.Portal.Web                                            |
| Kentico.Community.Portal.Web.E2E.Tests | End-to-end tests in Playwright.NET for Kentico.Community.Portal.Web                                    |

## Technologies and Frameworks

- `Kentico.Community.Portal.Admin`
  - Webpack, React
- `Kentico.Community.Portal.Web`
  - Vite.js, HTMX, Bootstrap 5, [Editormd](https://pandao.github.io/editor.md/), Prism.js

## Integrations

### ReCaptcha

### Google Tag Manager

### Authentication

The site uses a custom Azure AD App Registration in the Kentico tenant to provide SSO for Kentico employees to the Community Portal. This integration is set up according to [the Xperience documentation](https://docs.xperience.io/xp/developers-and-admins/configuration/users/administration-registration-and-authentication/administration-external-authentication#AdministrationExternalauthentication-MicrosoftAzureActiveDirectory) on the topic.

To assign Azure AD Users and Groups to specific roles within Xperience, manage the App Role assignments for the Enterprise Application.

Traditional Forms Authentication for the Admin is still enabled.

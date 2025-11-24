---
name: Read-Only-Mode-Implementation
description: Focus read-only safeguards on controller actions and form submissions across the Community Portal web and admin apps.
---

## Objective

Design and implement outer-boundary protections (HTTP controllers and form
pipelines) so the portal can run in Xperience read-only mode without triggering
`ReadOnlyModeException` or user-facing errors.

## Key References

- Kentico docs:
  https://docs.kentico.com/documentation/developers-and-admins/deployment/read-only-deployments#required-project-changes
- Web project root: `src/Kentico.Community.Portal.Web`
- Core shared utilities: `src/Kentico.Community.Portal.Core`
- Xperience configuration:
  `src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs`

## Xperience Read-only Mode Essentials

- **Core behavior**: Read-only mode runs the site against a cloned database
  snapshot and Azure Blob versions (identified by
  `ReadOnlyModeOptions.VersioningTimestamp`). Xperience blocks all data provider
  writes (insert, update, delete, bulk), prevents blob mutations, and suspends
  scheduled/background workers so the frozen content stays consistent.
- **Provider usage**: Inject `IReadOnlyModeProvider` (namespace
  `using CMS.Base;`) via DI wherever writes or enqueues might happen. Use
  `readOnlyProvider.IsReadOnly` to branch before work, or
  `readOnlyProvider.ThrowIfReadOnly()` as a guard clause before persistence
  logic. This project is deployed to Xperience by Kentico SaaS, which handles
  the `appsettings` configuration for `ReadOnlyModeOptions`. Add the
  configuration call to `ServiceCollectionXperienceExtensions.AddAppXperience()`
  method in
  `src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs`
  using
  `services.Configure<ReadOnlyModeOptions>(config.GetSection("ReadOnlyModeOptions"))`.
- **Xperience guarantees**: Built-in forms auto-disable inputs and submit
  buttons, administration UI shows a read-only splash, digital marketing
  endpoints (activities, contacts, email subscriptions) return HTTP 503 or
  redirect to configured fallback pages, and membership APIs stop creating
  contacts. Azure blob access automatically resolves the newest version created
  before the configured timestamp.
- **Application responsibilities**: Guard every custom write path (controllers,
  Razor handlers, services, background jobs, storage helpers) to avoid
  `ReadOnlyModeException`. Surface maintenance messaging or graceful fallbacks
  for forms, identity workflows, consent changes, and support submissions. Skip
  enqueueing work that would write later, avoid deleting or replacing content
  files while read-only, and validate styling so disabled forms communicate
  maintenance state clearly.
- **Custom HTML forms**: All custom HTML forms across the application must use
  the `xpc-readonly-disabled` tag helper attribute on the form's `<fieldset>`
  element. This tag helper automatically applies the `disabled` attribute when
  read-only mode is enabled. Additionally, invoke the
  `ReadOnlyModeNotificationViewComponent` at the bottom of each form's fieldset
  using `<vc:read-only-mode-notification />`. This view component encapsulates
  both the conditional display logic (checking
  `IReadOnlyModeProvider.IsReadOnly`) and the maintenance message markup,
  eliminating the need for inline `@if` blocks in form views.

Source documentation
<https://docs.kentico.com/documentation/developers-and-admins/deployment/read-only-deployments#required-project-changes>

## Required Outcomes

1. Global configuration wires `ReadOnlyModeOptions` and ensures
   `IReadOnlyModeProvider` is available for controller and form entry points.
2. Every HTTP endpoint (controllers, Razor components invoked via forms, HTMX
   handlers) guards writes before DB or blob updates and returns graceful UX
   alternatives when read-only.
3. Form pipelines (MVC forms, Page Builder widgets, support submissions) detect
   read-only early, suppress writes, and inform users of maintenance state. All
   custom HTML forms must use the `xpc-readonly-disabled` tag helper attribute
   on their `<fieldset>` elements (which automatically applies the `disabled`
   attribute when read-only mode is active) and invoke the
   `ReadOnlyModeNotificationViewComponent` at the bottom of the fieldset using
   `<vc:read-only-mode-notification />`. The view component encapsulates both
   the conditional display logic and the maintenance message markup, eliminating
   the need for inline `@if` blocks in form views.
4. Automated tests (or manual validation steps) cover success + read-only
   scenarios for critical outer-boundary flows.
5. Documentation / inline comments note intentional read-only bypasses or
   downstream responsibilities.

## Work Plan

1. **Bootstrap read-only services**

   - Update `ServiceCollectionXperienceExtensions.AddAppXperience()` to
     `Configure<ReadOnlyModeOptions>` using the configuration section provided
     by Xperience SaaS.
   - Make `IReadOnlyModeProvider` easy to consume via DI and shared utilities
     where helpful.
   - Create a custom ASP.NET Core tag helper named `ReadOnlyDisabledTagHelper`
     in `Components/TagHelpers/` that targets `<fieldset>` elements with the
     `xpc-readonly-disabled` attribute. The tag helper should inject
     `IReadOnlyModeProvider` and conditionally add the `disabled` attribute when
     read-only mode is active.
   - Create a view component named `ReadOnlyModeNotificationViewComponent` in
     `Components/ViewComponents/` that encapsulates the conditional display
     logic (checking `IReadOnlyModeProvider.IsReadOnly`) and the maintenance
     message markup. The view component should inject `IReadOnlyModeProvider`
     and return empty content when not in read-only mode, or render the
     notification message view when read-only mode is active. This centralizes
     both the condition check and the message markup, eliminating the need for
     inline `@if` blocks in form views.

2. **Map controller and form entry points**

   - Inventory POST/PUT/DELETE actions in `Features/**` and admin areas.
   - Identify form components (MVC, HTMX partials, Page Builder) that submit
     writes.
   - Flag any routes that rely on background handlers so they can surface
     read-only messages at the boundary.

3. **Membership & Identity controllers**

   - Controllers: `Features/Authentication`, `Features/Registration`,
     `Features/Accounts`, `Features/PasswordRecovery`.
   - Services: `Membership/MemberContactManager.cs`,
     `Features/Members/Badges/MemberBadgeService.cs`,
     `Rendering/AvatarImageService.cs`.
   - Guard `UserManager` writes, consent updates, avatar storage writes
   - Return original partials where appropriate, 503 status code otherwise

4. **Q&A + Blog controllers**

   - Controllers and view components under `Features/QAndA/Components/`.
   - Command handlers in `Features/QAndA/Operations/*.cs` and
     `Features/Blog/Operations/BlogPostPageSetQuestionCommand.cs`.
   - Ensure all `SetAsync`, `BulkDelete`, `WebPageManager` draft/publish calls
     are gated at the controller level; surface read-only messaging when
     submissions are blocked.

5. **Consent & cookie endpoints**

   - `Features/DataCollection/CookieConsentManager.cs`,
     `Infrastructure/ConsentManager.cs`.
   - Prevent agree/revoke/level changes during read-only and present fallback
     messaging directly in the responding controllers/components.

6. **Forms & Support submission pipelines**

   - Page Builder widgets (`Components/PageBuilder/Widgets/Forms`),
     `Support/Components/SupportFormController.cs`,
     `SupportRequestProcessorBackgroundService.cs`.
   - Ensure initial submission handlers short-circuit cleanly; enqueue
     operations only when read-only is false.
   - Apply the `xpc-readonly-disabled` tag helper attribute to all custom form
     `<fieldset>` elements.
   - Invoke the `ReadOnlyModeNotificationViewComponent` at the bottom of each
     form's fieldset using `<vc:read-only-mode-notification />`. The view
     component encapsulates both the conditional display logic (checking
     `IReadOnlyModeProvider.IsReadOnly`) and the maintenance message markup,
     eliminating the need for inline `@if` blocks in form views.

7. **Admin controllers and forms**

   - Info edit pages and the administration UI in general do not need modified.
     Xperience ensures the administration UI is disabled during read-only mode.

8. **Testing & verification**

   - Add targeted unit/integration tests toggling read-only flag.
   - Document manual validation checklist (toggle config, ensure forms disable,
     controllers return 503/partial, queued work is not enqueued).

9. **Polish**

- Update localized resources / UX messaging to explain maintenance state.
- Add guidance to deployment docs about switching read-only mode.

## Deliverables

- Code changes implementing safeguards + UX fallbacks.

## Execution Tips

- When read-only, return `StatusCode(503)` plus HTMX redirects or partials
  explaining maintenance window.
- Keep logging concise; use structured events with `EventId` for monitoring.
- Avoid duplicating checks by centralizing them in shared helpers and provider
  extensions.
- Validate downstream processors respect read-only by refusing submissions when
  the boundary detects the mode.

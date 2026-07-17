# Admin E2E Testing Research

Date: 2026-07-15

## Goal

Expand the existing Playwright E2E suite from member-only scenarios to include
Xperience administration editing and UX scenarios, while keeping tests runnable
both locally and in CI.

## Primary findings from Kentico docs

1. Kentico explicitly supports automated UI testing for the administration
   interface and recommends Cypress or Playwright for that purpose
   (https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/administration-interface-ui-tests.html).
2. Administration authentication is separate from live-site membership
   authentication and can use forms and external OAuth/OIDC authentication
   together; for this project, admin E2E should use forms authentication with
   one default account supplied by configuration
   (https://docs.kentico.com/documentation/developers-and-admins/configuration/users/administration-registration-and-authentication/administration-forms-authentication.html,
   https://docs.kentico.com/documentation/developers-and-admins/configuration/users/administration-registration-and-authentication/administration-external-authentication.html).
3. The admin UI exposes data-testid attributes to help test selection, but
   Kentico does not guarantee those attributes are stable across releases, so
   tests should not rely on fragile selectors alone
   (https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/administration-interface-ui-tests.html).
4. The administration sign-in entry point is domain/admin, and administration
   routes are under the admin URL space
   (https://docs.kentico.com/documentation/administration-interface-basics.html).
5. External administration authentication supports OAuth/OIDC providers
   (including Entra ID) and callback routes such as admin-oidc; only one
   external provider can be enabled at a time
   (https://docs.kentico.com/documentation/developers-and-admins/configuration/users/administration-registration-and-authentication/administration-external-authentication.html).
6. Kentico architecture guidance calls out automated E2E coverage of use-case
   scenarios as a recommended practice
   (https://docs.kentico.com/guides/architecture/xperience-implementation-handbook/test-your-solution.html).

## What exists in this repository today

1. TypeScript Playwright suite is already wired for local and CI execution,
   including retries and web server startup.
   - test/kentico-community-portal-web-e2e-tests/playwright.config.ts
2. Existing tests are member/live-site focused.
   - test/kentico-community-portal-web-e2e-tests/tests/membership/register-and-confirm.spec.ts
   - test/kentico-community-portal-web-e2e-tests/tests/membership/password-recovery.spec.ts
   - test/kentico-community-portal-web-e2e-tests/tests/membership/login-moderation.spec.ts
3. Deterministic E2E setup currently exists only for member moderation state via
   a test endpoint.
   - src/Kentico.Community.Portal.Web/Features/Testing/E2ETestingEndpoints.cs
4. CI already runs Playwright against the published app on fixed HTTPS URL/port
   and sets Playwright env values.
   - .github/workflows/ci.yml
5. Admin auth in app code is configured to external sign-in (Entra ID style
   OIDC + cookies), with long-lived admin auth cookie options.
   - src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionAdminAuthenticationExtensions.cs

## Gap analysis

1. No admin-focused E2E test folder, fixtures, or page objects currently exist.
2. Admin E2E authentication flow is not yet standardized to forms auth with a
   single configured default account.
3. No CI secrets/env contract currently defines admin test credentials.
4. Selector policy for admin tests is not documented yet (important due to
   possible data-testid changes).

## Recommended approach for this codebase

### 1) Add admin test infrastructure beside existing Playwright support

Add:

- test/kentico-community-portal-web-e2e-tests/tests/admin/
- test/kentico-community-portal-web-e2e-tests/support/admin/auth.ts
- test/kentico-community-portal-web-e2e-tests/support/admin/pageObjects/
- test/kentico-community-portal-web-e2e-tests/tests/admin/auth.setup.ts
  (optional dedicated setup project)

Reasoning: this keeps the current member suite untouched while introducing an
explicit admin lane.

### 2) Standardize all admin E2E on forms auth with configured default account

Use one deterministic forms-based admin account for all administration E2E runs
(local and CI), provided via configuration.

Implementation points:

- Ensure forms auth is enabled for admin sign-in in E2E environments.
- Provide default account credentials via env/config.
- Reuse the existing local admin account; do not create a new admin user for
  local development E2E.

Reasoning:

- Keeps admin E2E deterministic and fast across local and CI.
- Avoids brittle automation dependencies on external OAuth/OIDC redirects.

### 3) Update Playwright config for an admin project

Add Playwright projects:

- admin-setup (captures storage state after sign-in)
- admin-tests (depends on admin-setup and reuses storage state)

Keep base URL as existing portal URL, and derive admin URL via env variable.

### 4) Introduce admin env contract for local and CI

Suggested variables:

- ADMIN_BASE_URL (default https://localhost:45039/admin)
- ADMIN_DEFAULT_USERNAME
- ADMIN_DEFAULT_PASSWORD

CI should set these in .github/workflows/ci.yml, with password from GitHub
Secrets.

### 5) Selector and reliability policy for admin UI

Because the built-in Xperience by Kentico administration UI is not owned by this
codebase, selector options can be limited and may change across upgrades. When
testing custom administration pages/components implemented in this repository,
add explicit test attributes to those components to improve selector stability.

Use this priority:

1. ARIA role/name selectors (Playwright getByRole)
2. Label-based selectors (getByLabel)
3. Stable text selectors
4. data-testid as fallback for built-in admin UI, wrapped in page-object helpers
   due to Kentico stability caveat
5. For custom admin UI components in this repository, prefer intentional
   test-specific attributes (for example data-testid) designed for E2E

Also require:

- explicit wait for page landmarks after route transitions
- trace/screenshot retention on failure (already configured)
- unique test data keys per worker/test to avoid collisions

## Proposed initial scenarios (high value)

1. Admin sign-in smoke: sign in with configured default forms account, open
   dashboard, assert main navigation presence.
2. Members administration smoke: navigate to members list, search for known
   existing member, open detail/edit page.
3. Content editing smoke: open one editable content item/page, update a
   non-destructive field, save, verify success notification.
4. Permission UX smoke: sign in as lower-privilege admin fixture and verify
   restricted action visibility/denial behavior.

## Local and CI execution model

Local:

1. Reuse current pnpm test workflow.
2. Run all E2E: pnpm test.
3. Run admin-only: pnpm test tests/admin.

CI:

1. Keep current publish/start model in workflow.
2. Configure admin default credentials via CI secrets/env and use that account
   for sign-in.
3. Run admin suite as a separate step so failures are isolated and diagnosable.
4. Upload Playwright artifacts for admin failures (already aligned with current
   artifact strategy).

## Risks and mitigations

1. Risk: data-testid drift in Kentico upgrades.
   - Mitigation: for built-in UI prefer role/label selectors and centralize
     test-id usage in page objects; for custom UI add intentional test-specific
     attributes.
2. Risk: default admin account credentials drift or missing config in CI.
   - Mitigation: enforce required env vars and add pre-test validation with
     clear failure messages.
3. Risk: stateful admin data causing flaky tests.
   - Mitigation: keep scenarios non-destructive where possible and use unique
     test data keys per worker/test to avoid collisions.
4. Risk: admin test suite growth increases CI time.
   - Mitigation: keep a smoke subset required on PRs, full regression nightly.

## Proposed repo changes checklist

- [ ] Add admin E2E folder and first smoke specs under
      test/kentico-community-portal-web-e2e-tests/tests/admin/
- [ ] Add support/admin helpers and page objects
- [ ] Add admin env variables to
      test/kentico-community-portal-web-e2e-tests/support/config.ts
- [ ] Extend playwright.config.ts with admin setup/test projects and
      storage-state flow
- [ ] Add CI secrets and env wiring for admin test credentials in
      .github/workflows/ci.yml
- [ ] Document admin E2E run commands and constraints in docs/Run-Tests.md

## Source list

- https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/administration-interface-ui-tests.html
- https://docs.kentico.com/documentation/administration-interface-basics.html
- https://docs.kentico.com/documentation/developers-and-admins/configuration/users/administration-registration-and-authentication/administration-forms-authentication.html
- https://docs.kentico.com/documentation/developers-and-admins/configuration/users/administration-registration-and-authentication/administration-external-authentication.html
- https://docs.kentico.com/guides/architecture/xperience-implementation-handbook/test-your-solution.html
- test/kentico-community-portal-web-e2e-tests/playwright.config.ts
- test/kentico-community-portal-web-e2e-tests/package.json
- test/kentico-community-portal-web-e2e-tests/support/config.ts
- docs/Run-Tests.md
- .github/workflows/ci.yml
- src/Kentico.Community.Portal.Web/Features/Testing/E2ETestingEndpoints.cs
- src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionAdminAuthenticationExtensions.cs
- src/Kentico.Community.Portal.Web/Configuration/ApplicationBuilderUtilityExtensions.cs

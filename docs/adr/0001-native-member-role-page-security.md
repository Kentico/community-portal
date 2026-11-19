# Retire the custom content-authorization filter in favor of native Member Role page security

`IContentAuthorizationFields` + `ContentAuthorizationFilter` were built to gate web page access by taxonomy tag before Xperience had a native membership-roles feature. We're adopting Xperience's built-in page security (Requires authentication / Membership roles), which content-tree routing already enforces automatically, and deleting `ContentAuthorizationFilter` once E2E tests confirm the native behavior matches the old one — rather than rewriting the filter to check roles itself. The native router already does this job; keeping a custom filter around would just be redundant code to maintain.

## Consequences

- `IContentAuthorizationFields` and its tag data stay in place on `LandingPage` (unused) through the expand period; removed in the contract phase.
- Scope is limited to `LandingPage` (the `/MyAccount` and `/communityprogramsdashboard2026` pages) — no other content type used the old schema.

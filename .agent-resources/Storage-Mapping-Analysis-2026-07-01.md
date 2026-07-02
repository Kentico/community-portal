# Storage Mapping Analysis (2026-07-01)

## Scope

- Reviewed current storage mapping and storage access in application code.
- Reviewed Xperience storage path mapping guidance for 31.6.0.
- Factored in that member avatars are fully migrated to Content Hub assets in
  all environments.
- Factored in Aspire local development options: Azurite emulation vs local
  filesystem behavior.

## Executive Summary

The project still uses a legacy custom `StorageInitializationModule` approach
for storage mapping, including a custom `member-assets` path used by legacy
avatar files. This is now out of alignment with Xperience 31.6.0 guidance, which
recommends storage path registry + environment-specific mapping methods (for
SaaS: `AddXperienceCloudStoragePathMapping()`).

Given that avatar migration is complete, the highest-value simplification is to
remove legacy member-asset storage reads/writes and rely on Content Hub asset
URLs only. That change significantly reduces complexity and unlocks migration
from manual path mapping to the built-in storage path mapping model.

## Current Implementation (Code Review)

### Storage mapping core

- Legacy mapping module is still active:
  - `StorageInitializationModule` is registered via assembly attribute in
    `src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs`.
  - Manual calls to `StorageHelper.MapStoragePath(...)` in
    `src/Kentico.Community.Portal.Web/Infrastructure/Storage/StorageInitializationModule.cs`.
- Mapped prefixes today:
  - Xperience assets: `~/assets/`
  - Member assets: `~/member-assets/`
  - Lucene: `~/App_Data/LuceneSearch/`
- Environment switch today:
  - Azure mapping enabled in QA/UAT/Production (`ShouldMapAzureStorage`).
  - Local development maps to local filesystem directories (`$StorageAssets`,
    `$StorageMemberAssets`).

### Avatar-related storage access (legacy + Content Hub dual path)

- `AvatarImageService` currently:
  - Reads Content Hub avatar first
    (`MemberAssetContentService.GetContentHubAvatarUrl`), then falls back to
    legacy file path lookup.
  - On upload: writes to legacy storage path and updates Content Hub asset.
- Legacy path dependency is through
  `IStoragePathService.GetStorageFilePath(..., StorageAssetType.Member)`.
- `MemberController` still serves legacy file stream path if no Content Hub URL
  is found.
- Admin migration/reporting pages still scan the legacy avatar filesystem and
  expose migration actions.

### Aspire and local storage context

- AppHost provisions Azurite on standard ports (`10000/10001/10002`) in
  `src/Kentico.Community.Portal.AppHost/AppHost.cs`.
- Web local appsettings include Azurite credentials/endpoints in
  `src/Kentico.Community.Portal.Web/appsettings.Development.json` and
  `src/Kentico.Community.Portal.Web/appsettings.CI.json`.
- Current mapping behavior is custom-module-driven, not
  storage-path-registry-driven.

## Xperience 31.6.0 Storage Mapping Guidance (Docs Review)

### Key guidance

- Xperience now uses a storage path registry (`IStoragePathRegistry`) and path
  typing (`SharedPersistent`, `SharedTemp`, `LocalOnly`).
- For SaaS projects, recommended approach is
  `AddXperienceCloudStoragePathMapping()`.
- Legacy `StorageInitializationModule` pattern is explicitly described as
  pre-31.6.0 and recommended for migration.
- System paths (content item assets, form files, AIRA, etc.) are registered
  automatically by platform modules.
- Custom paths should be registered only if your app truly stores custom files
  outside system-managed paths.

### Important implications

- Avoid direct path-string assumptions where possible; prefer system path
  registration/identification.
- Container-name changes do not migrate existing files automatically.
- Blob storage is case-sensitive; Xperience lowercases paths on Azure storage.

## Fit/Gap Analysis

### What aligns

- Project already uses Azure-compatible config and Azurite in local
  environments.
- Content Hub is already used for member avatars and URL serving is implemented.

### What does not align

- Still using legacy manual mapping module instead of 31.6.0 storage path
  mapping API.
- Still carrying a custom `member-assets` mapping that was for old avatar
  storage.
- Avatar runtime remains dual-path (Content Hub + legacy file fallback/write),
  increasing complexity and operational risk.

## Findings Specific to “Avatars Fully Migrated” Constraint

If avatar migration is truly complete across all environments, these legacy
behaviors are now technical debt:

1. Upload-time write to legacy filesystem for avatars.
2. Read-time fallback to legacy avatar filesystem.
3. Admin migration workflow and inventory built around legacy avatar files.
4. `member-assets` custom mapping path (and related service/test expectations).
5. Continued reliance on `MemberAvatarFileExtension` for physical file lookup
   semantics.

## Aspire Local Strategy: Azurite vs Local Filesystem

Both are valid, but they serve different goals.

### Option A: SaaS-fidelity local behavior (recommended default for this repo)

- Keep Azurite available via AppHost (already done).
- Move to `AddXperienceCloudStoragePathMapping()` so behavior follows Xperience
  cloud mapping conventions.
- Benefit: closer parity with QA/UAT/Prod storage behavior and fewer
  environment-specific surprises.

### Option B: Local filesystem-first behavior for dev ergonomics

- Intentionally keep mapping disabled locally and use local disk for quicker
  inspection/debugging.
- Benefit: simple local debugging and no emulator dependency for all workflows.
- Cost: reduced fidelity to SaaS cloud behavior.

Given this project’s SaaS target and existing Aspire Azurite setup, Option A is
the better long-term default.

## Recommended Migration Plan (Phased)

### Phase 1: Remove legacy avatar storage usage

1. Stop writing avatar files to legacy member-assets storage in
   `AvatarImageService.UpdateAvatarImage`.
2. Stop fallback reads from legacy avatar storage in
   `AvatarImageService.GetAvatarImage` and `MemberController.GetAvatarImage`.
3. Retain default-avatar behavior for missing content.
4. Remove/retire admin migration UI/actions tied to legacy avatar files.

### Phase 2: Move mapping to Xperience 31.6 model

1. Add `AddXperienceCloudStoragePathMapping()` in startup configuration.
2. Remove `StorageInitializationModule` registration and class when no longer
   needed.
3. Remove `StorageAssetType.Member` path concepts unless another real custom
   file store still requires it.
4. Keep/validate Lucene behavior according to current deployment assumptions.

### Phase 3: Cleanup + verification

1. Simplify `IStoragePathService` interface and tests to match new reality.
2. Validate in Development, QA, UAT, Production:
   - Avatar display and upload
   - Content item asset URLs
   - Deployment package behavior
   - Any file-backed features (licenses/form uploads/content assets)
3. Ensure no residual references to `$StorageMemberAssets` / `member-assets`
   remain.

## Risks and Checks

- Risk: Hidden dependencies on legacy avatar files in old data or utilities.
  - Mitigation: run a one-time DB/content audit for members lacking Content Hub
    avatar references.
- Risk: Behavioral change in avatar endpoint caching/etag logic if legacy
  streaming is removed.
  - Mitigation: test cache headers for redirected Content Hub URLs in all
    environments.
- Risk: Mapping changes can affect deployment package expectations.
  - Mitigation: validate `Export-DeploymentPackage.ps1` inputs/outputs after
    mapping migration.

## Suggested Decision

Adopt a two-step modernization:

1. First remove legacy avatar file usage now (low ambiguity because migration is
   complete).
2. Then migrate from `StorageInitializationModule` to Xperience storage path
   mapping API with Aspire/Azurite-aligned local behavior.

This sequence minimizes risk while aligning to Xperience 31.6.0 best practices.

## Implementation Task List (Handoff)

Use this as the execution checklist for a fresh-context agent.

### Task 0: Baseline safety checks

- Capture baseline behavior before code changes:
  - Verify avatar rendering for members with and without avatar assets.
  - Verify avatar upload flow from account management.
  - Verify default avatar behavior for spam/flagged members.
- Record baseline commands and outputs in the PR description.

### Task 1: Remove legacy avatar file fallback on read path

- Update `AvatarImageService.GetAvatarImage` to return:
  - Content Hub URL when available.
  - Default avatar file when not available.
- Remove `GetLegacyAvatarFile` usage and related legacy file-extension map
  lookups.
- Keep current logging intent, but update source labels to reflect Content
  Hub/default-only behavior.
- Acceptance criteria:
  - No runtime reads from `member-assets` in avatar path resolution.
  - Member avatar endpoints still resolve correctly for migrated users.

### Task 2: Remove legacy avatar file writes on upload path

- Update `AvatarImageService.UpdateAvatarImage` to stop writing to legacy
  storage.
- Keep Content Hub asset update/create logic intact.
- Validate `member.AvatarFileExtension` usage and remove it from write-path
  requirements if no longer needed.
- Acceptance criteria:
  - Upload writes only to Content Hub asset pipeline.
  - Avatar update succeeds and new image is served after upload.

### Task 3: Simplify controller behavior

- Update `MemberController.GetAvatarImage`:
  - If Content Hub URL exists, redirect as before.
  - If not, return default avatar directly (no legacy file stream fallback).
- Reassess/remove ETag logic if it only applied to legacy file streaming.
- Acceptance criteria:
  - Controller no longer depends on legacy avatar file presence.
  - Response behavior remains stable for consumers and views.

### Task 4: Retire admin migration/inventory features tied to legacy files

- Remove or deprecate `MemberAvatarToMemberAssetContentMigrator` and related
  management page commands/UI actions.
- Remove legacy avatar filesystem inventory logic from `MemberManagementPage`.
- If deprecation is preferred, clearly mark actions as disabled and no-op with
  explicit messaging.
- Acceptance criteria:
  - No active admin workflow expects `member-assets/avatars` files.
  - Admin UX clearly reflects post-migration state.

### Task 5: Migrate storage mapping to Xperience 31.6 API

- Add `AddXperienceCloudStoragePathMapping()` in startup configuration.
- Remove legacy module registration:
  - `[assembly: RegisterModule(typeof(StorageInitializationModule))]`
  - `StorageInitializationModule` implementation when no longer needed.
- Ensure SaaS/cloud environment behavior remains aligned with existing
  deployment assumptions.
- Acceptance criteria:
  - No manual `StorageHelper.MapStoragePath(...)` for standard system paths.
  - Storage path mapping managed through Xperience-recommended API.

### Task 6: Remove obsolete member storage abstractions

- Refactor `IStoragePathService` and `StoragePathService`:
  - Remove `StorageAssetType.Member` if no valid remaining use case.
  - Keep only truly needed custom path logic (if any).
- Update tests (`StoragePathServiceTests`) to match new contract.
- Acceptance criteria:
  - No production code references `StorageAssetType.Member`.
  - Tests pass and reflect final intended storage architecture.

### Task 7: Cleanup references and docs

- Remove stale references to:
  - `member-assets`
  - `$StorageMemberAssets`
  - avatar migration operational steps that no longer apply
- Update architecture/developer docs to explain the new
  avatar/content-asset-only model.
- Acceptance criteria:
  - Repository search has no unintended legacy storage references.
  - Documentation matches implemented behavior.

### Task 8: Validate across environments

- Development (Aspire + Azurite):
  - Avatar display/upload
  - Content item asset URL behavior
- QA/UAT/Production:
  - Avatar rendering and caching behavior
  - No regressions for other asset-backed features (licenses, form uploads,
    content assets)
- Acceptance criteria:
  - All checks green in each environment.
  - No operational dependency on legacy avatar filesystem.

### Task 9: PR and rollout checklist

- Include in PR:
  - Before/after architecture note.
  - Explicit mention that legacy avatar file storage is removed.
  - Migration/rollback notes.
- Rollout:
  - Deploy to lower environment first.
  - Verify avatar behavior with a focused smoke test script.
  - Promote after sign-off.

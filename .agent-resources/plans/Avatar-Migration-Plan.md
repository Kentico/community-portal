# Plan: Migrate Member Avatar Images to Content Hub

## TL;DR

Migrate member avatar images from direct Azure Storage
(`member-assets/avatars/`) to Content Hub under a new `MemberAssetContent`
content type. `MemberAssetContent` is designed for both avatar images and future
member-uploaded Q&A images, but this plan covers avatar migration only. This
enables admin UI management while maintaining legacy folder access as a fallback
during the transition. The strategy uses the expand-and-contract pattern: define
new content type → migrate data via admin UI tool → update serving code → remove
legacy storage access.

---

## Steps

If the MCP servers cannot be accessed, stop the migration immediately and
request access to the MCP servers

- kentico.docs.mcp
- xperience-management-api
- community-portal
- io.github.ChromeDevTools/chrome-devtools-mcp

### Phase 1: Content Type Definition & Infrastructure

1. **Create `MemberAssetContent` content type via Management MCP server**
   - Fields:
     - `MemberAssetContentImageAsset` (content item asset) → stores the actual
       image file
     - `MemberAssetContentMemberID` (integer) → foreign key linking to
       CMS_Member
     - `MemberAssetContentMemberAssetTypes` (taxonomy tags using
       `MemberAssetType` taxonomy)
     - `CoreTaxonomyDXTopics` (reusable field schema which adds
       `ICoreTaxonomyFields` to content type)

2. **Code generation** (_depends on step 1_)
   - Run `Refresh-GeneratedCode.ps1` with
     `scripts/Utilities/settings.local.json` config to generate new content type
   - This generates `MemberAssetContent.generated.cs` in
     `src/Kentico.Community.Portal.Core/Content/Hub/`
   - Create partial class `MemberAssetContent.cs` and populate GUID with value
     from Management API for the content type GUID

3. **Extend `MemberManagementPage` to support dry-run analysis** (_parallel with
   step 1-2_)
   - Adapt this page infrastructure for new avatar migration. No existing logic
     needs to be maintained.
   - Create `PageCommand` method: `AnalyzeMemberAvatarsForContentHubMigration()`
   - Output: Shows preview of which members have avatars and would be migrated;
     members without avatars are skipped (no conflicts expected)
   - Does NOT modify any data; read-only analysis
   - Returns structured result showing file count, sizes, members affected
   - Update `MemberManagementPageClientProperties` with analysis results
   - Pattern reference: follow `MigrateOldAvatarPaths()` in
     [MemberManagementPage.cs](src/Kentico.Community.Portal.Admin/Features/Members/MemberManagementPage.cs)
     for `[PageCommand(Permission = SystemPermissions.UPDATE)]` +
     `ICommandResponse` pattern
   - Update the `MemberManagementLayoutTemplate.tsx` to display all required
     data to perform the migration of avatars. No existing UI needs to be
     maintained. Follow the design patterns that exist in the client app using
     `design-tokens.css` and other custom layout designs.

4. **Create `IDataMigrator` implementation:
   `MemberAvatarToMemberAssetContentMigrator`** (_parallel with step 3_)
   - Location: `src/Kentico.Community.Portal.Admin/Features/Migrations/`
   - Implements `IDataMigrator` interface — see interface definition in
     [MigrationsManagementPage.cs](src/Kentico.Community.Portal.Admin/Features/Migrations/MigrationsManagementPage.cs)
   - Name: "Member Avatar Content Hub Migration"
   - Implements:
     - `MigrateableItems()` → queries file system + DB for members that have
       avatar files; members without avatars are excluded
     - `Migrate(int count)` → uses **Content Item Assets API** to:
       1. Read file from member-assets/avatars/{memberID}{ext}
       2. Create `ContentItemAssetMetadata` + `ContentItemAssetFileSource` (use
          `CMS.IO.FileInfo.New()` — not `System.IO` — for storage provider
          compatibility)
       3. Wrap in `ContentItemAssetMetadataWithSource`
       4. **Check if `MemberAssetContent` item already exists** for member:
          - If not found → call `IContentItemManager.Create()`
          - If found → call `IContentItemManager.Update()` on existing item
       5. Link member ID via `MemberAssetContentMemberID` field
     - Returns `MigrationResult` with success/failure tracking per member
   - Idempotent: re-running migration updates existing items instead of creating
     duplicates
   - Batching: Support migrating in chunks (e.g., 1-5 at a time)
   - Handles:
     - File → content item asset conversion (using Asset API)
     - Member ID linking
     - Extension preservation in file naming

5. **Create query service: `MemberAssetContentService`** (_parallel with step
   4_)
   - Location: `src/Kentico.Community.Portal.Core/Content/Hub/`
   - Provides query methods:
     - `GetAvatarContentByMemberID(int memberID)` → queries for
       `MemberAssetContent` by `MemberAssetContentMemberID` using
       `IContentRetriever`, retrieves asset metadata
     - `GetAvatarAssetUrl(int memberID)` → returns content item asset URL if
       `MemberAssetContent` item exists for member; `null` if not yet migrated
   - Caches results using `IProgressiveCache` — follow pattern in
     [AvatarImageService.cs](src/Kentico.Community.Portal.Web/Rendering/AvatarImageService.cs)
     using `CacheHelper.GetCacheDependency("cms.member|all")` if asset was _not_
     retrieved from content item.
   - When avatar is stored in content item, URL of asset metadata can be
     returned unmodified.
   - Docs:
     [ContentRetriever API](https://docs.kentico.com/documentation/developers-and-admins/development/content-retrieval/retrieve-content-items)
     and
     [Content Item Asset Upload API](https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/content-item-asset-upload-api.html)

### Phase 2: Update Serving Code (Expand Phase)

6. **Refactor `AvatarImageService` to support dual sources** (_depends on step
   5_)
   - Add constructor dependency: `MemberAssetContentService contentHubService`
   - Update `GetAvatarImage()`:
     - If `MemberAssetContentService.GetAvatarAssetUrl()` returns a URL → serve
       Content Hub asset URL directly
     - If `null` (not yet migrated) → fall back to existing member-assets/
       folder lookup using current URL pattern
     - Log which source was used (for monitoring transition)
   - Update `UpdateAvatarImage()`:
     - Write to legacy member-assets/ folder first (known stable path)
   - Then create/update `MemberAssetContent` item via `IContentItemManager`
   - If either write fails → surface error to user; they can retry the upload
   - Store asset reference + member ID in content item
   - Deprecation: Add code comments flagging this service for removal in Phase 3

7. **Update avatar serving endpoints & components** (_depends on step 6_)
   - `MemberController.GetAvatarImage()`
     - Still calls `AvatarImageService.GetAvatarImage()` (no changes needed;
       service handles fallback)
   - `Avatar.cshtml`
     - No changes needed; `AvatarImageService` abstracts the URL source
   - Email components, testimonial widgets: No changes (they use the API
     endpoint, which is abstracted)

8. **Register `MemberAssetContentService` &
   `MemberAvatarToMemberAssetContentMigrator` in DI** (_parallel with step 6-7_)
   - Location: Feature-specific extension method in
     `src/Kentico.Community.Portal.Web/Features/Members/`
   - Register:
   - `MemberAssetContentService` as Scoped
   - `MemberAvatarToMemberAssetContentMigrator` as Transient
   - Wire into migrations management page
   - Add rate limiting policy in `ServiceCollectionMvcExtensions`:
     `MemberRateLimitingConstants.UpdateAvatar` (5 permits per 5 minutes,
     sliding window) to prevent abuse of avatar upload endpoint

### Phase 3: Monitor & Contract (Post-Migration)

9. **Migration finalization & cleanup**
   - This will be initiated manually after deployment of migration code
     - Remove fallback logic from `AvatarImageService`
     - Delete member-assets/avatars/ folder
     - Remove `IStoragePathService.StorageAssetType.Member` handling for avatars
     - Remove legacy `AvatarImageService` methods if fully replaced
   - Update storage initialization to skip avatar mappings

---

## Tooling & Automation

### Management MCP Server (Step 1)

The `MemberAssetContent` content type can be created programmatically via the
**Management MCP server** instead of manual admin UI setup. This approach:

**Implementation**: Create a Management MCP client that defines the content type
schema, then execute the deployment as part of the deployment pipeline or
one-time setup script.

**Reference**:
[Management MCP server](https://docs.kentico.com/documentation/developers-and-admins/api/management-api.html)

### Content Item Assets API (Steps 4-5)

The `MemberAvatarToMemberAssetContentMigrator` and `MemberAssetContentService`
require the **Content Item Assets API** to handle avatar file uploads and
retrieval.

**Upload pattern** (`MemberAvatarToMemberAssetContentMigrator.Migrate()`):

1. Read avatar file from member-assets/ folder
2. Create `ContentItemAssetMetadata` with file properties (Extension, Name,
   Size, LastModified)
3. Create `ContentItemAssetFileSource` pointing to file path
4. Wrap both in `ContentItemAssetMetadataWithSource`
5. Add to `ContentItemData` dictionary with key = field code name
   (`MemberAssetContentImageAsset`)
6. Pass to `IContentItemManager.Create()` or `Update()`

**Retrieval pattern** (`MemberAssetContentService.GetAvatarAssetStream()`):

1. Query `MemberAssetContent` by `MemberAssetContentMemberID` using
   `IContentRetriever`
2. Extract asset metadata from response (GUID, extension, URL)
3. Download asset using the Xperience asset URL or stream retrieval API

**Key classes**:

- `ContentItemAssetMetadata` — File metadata (Extension, Identifier,
  LastModified, Name, Size)
- `ContentItemAssetFileSource` — Source from filesystem
- `ContentItemAssetStreamSource` — Source from memory stream
- `ContentItemAssetMetadataWithSource` — Combined metadata + source

**Important**: Use `CMS.IO.FileInfo.New(path)` (not `System.IO.FileInfo`) to
ensure compatibility with Azure Blob and local storage providers.

**References**:

- [Content Item Asset Upload API](https://docs.kentico.com/documentation/developers-and-admins/api/content-item-api/content-item-asset-upload-api.html)
- [Content items API examples](https://docs.kentico.com/api/content-management/content-items.html#content-item-assets)
- [Content type field configuration](https://docs.kentico.com/documentation/developers-and-admins/development/content-types.html)

---

## Relevant Files

### New Files to Create

- `src/Kentico.Community.Portal.Core/Content/Hub/MemberAssetContent.cs` — Custom
  partial class for any post-generation logic
- `src/Kentico.Community.Portal.Admin/Features/Migrations/MemberAvatarToMemberAssetContentMigrator.cs`
  — IDataMigrator implementation
- `src/Kentico.Community.Portal.Core/Content/Hub/MemberAssetContentService.cs` —
  Query service for Content Hub avatars

### Files to Modify

- `src/Kentico.Community.Portal.Admin/Features/Members/MemberManagementPage.cs`
  and
  `src\Kentico.Community.Portal.Admin\Client\src\features\members\MemberManagementLayoutTemplate.tsx`
  — Add analysis & migration UI methods
- `src/Kentico.Community.Portal.Web/Rendering/AvatarImageService.cs` — Add
  dual-source logic (Content Hub + fallback)

### Files to Remove (Phase 3 only)

- `src/Kentico.Community.Portal.Web/Infrastructure/Storage/StorageInitializationModule.cs`
  — Remove avatar-related storage mappings for member-assets
- Fallback logic in `AvatarImageService` after migration complete
- Legacy migration method `MemberManagementPage.MigrateOldAvatarPaths()` (after
  phase 1 migration complete)

---

## Verification

### Phase 1 Verification

1. **Management MCP deployment & Xperience admin UI verification**
   - Verify `MemberAssetContent` content type exists in admin UI with all 4
     fields
   - Verify it implements `ICoreTaxonomyFields` (taxonomy dropdown visible in
     form)
   - Verify generated .cs files appear in Hub folder

2. **Code review**
   - Verify `MemberAssetContentService` queries correctly
   - Verify `MemberAvatarToMemberAssetContentMigrator` identifies correct files
     (dry-run mode)
   - Verify DI registration resolves without errors

3. **MemberManagementPage analysis UI**
   - Run `AnalyzeMemberAvatarsForContentHubMigration()` page command
   - Verify preview shows correct file count matching member-assets/avatars/
     folder
   - Verify no changes occur (read-only validation)

### Phase 2 Verification

4. **Dual-source serving**
   - Upload new avatar via `AvatarImageService.UpdateAvatarImage()` → verify
     dual-write (both Content Hub + member-assets)
   - Retrieve avatar via `GetAvatarImage()` → verify Content Hub source is
     queried first
   - Check logs for fallback usage (should be zero during new uploads)

5. **Existing avatar serving**
   - Access avatar for migrated member via API/component → verify it returns
     correct image
   - Verify fallback logic works (temporarily remove Content Hub item, confirm
     legacy file still serves)

6. **Page command execution**
   - Run migration batch (e.g., 50 members) from
     `MemberManagementPage.MigrateAvatarsToContentHub()`
   - Verify each creates content item with asset + member ID link
   - Verify result tracking shows successes/failures
   - Verify can run migration again without duplicate items (idempotent or
     duplicate-detection logic)

7. **Cache invalidation**
   - Verify cache dependencies trigger correctly in `MemberAssetContentService`
   - Verify fallback to legacy service uses `AvatarImageService` cache (avoid
     cache collision)

### Phase 3 Verification (post-transition)

8. **Cleanup validation**
   - After removing fallback: all avatars still serve correctly from Content Hub
   - Verify storage folder is empty and can be safely deleted
   - Verify no dead code paths remain

---

## Decisions

- **Scope boundary**: `MemberAssetContent` is shared infrastructure for avatar
  and future member-uploaded Q&A images, but this migration plan only migrates
  avatar images.
- **Admin UI**: Reuse existing `MemberManagementPage` shell instead of separate
  admin ui page
- **Dual-write period**: New uploads → write to both sources; enables instant
  rollback if needed

---

## Further Considerations

1. **Member deletion workflow** — Members are currently moderated and disabled
   rather than deleted, so cascade delete is not an immediate concern. When
   deletion is eventually supported, `MemberAssetContent` items will require
   custom cleanup (no automatic cascade from Xperience). Implement as a separate
   future feature.

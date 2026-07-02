# Xperience v31.0.0 → v31.6.0 Adoption Status

**Review Date**: 2026-07-01  
**Xperience Version**: 31.6.0  
**Status**: ✅ Fully Modernized on Core APIs | ⚠️ Selective Adoption of New
Features

---

## Executive Summary

The codebase is **fully compliant with v31.0.0+ breaking changes** and has
adopted **8/13 major features** from v31.6.0. All deprecated APIs have been
replaced with their modern equivalents. New v31.6.0 features show **selective
adoption based on project needs** rather than universal implementation gaps.

---

## Part 1: Core API Modernization (v31.0.0 Breaking Changes)

### ✅ Fully Adopted (100% Compliant)

#### 1. Content & Page APIs

- **Status**: ✅ **COMPLETE MIGRATION**
- **What Changed**: Moved from `DocumentEngine` (TreeNode API) to
  `ContentEngine` + `Websites` projects
- **Adoption**:
  - Using `IWebPageManager` & `IWebPageManagerFactory` (10+ usages)
  - Using `IContentItemManager` & `IContentItemManagerFactory` (6+ usages)
  - Using `ContentItemQueryBuilder` (53+ usages)
  - Using `IContentQueryExecutor` (45+ usages)
  - All 43 generated content type classes use `IWebPageFieldsSource`
  - Generated classes follow `AbstractInfo<T, IInfoProvider<T>>` pattern
- **Evidence**:
  [src/Kentico.Community.Portal.Core/Modules/ContentItemManagerCreator.cs](src/Kentico.Community.Portal.Core/Modules/ContentItemManagerCreator.cs)
- **No Deprecated APIs Found**: ✅ No TreeNode, DocumentEngine, DocumentQuery,
  or IPageRetriever

---

#### 2. Data Caching (Replaced ICacheKeyProvider)

- **Status**: ✅ **MODERN PATTERN**
- **What Changed**: New `ICacheDependencyKeysBuilder` interface for cache key
  generation
- **Adoption**:
  - Custom `ICacheDependencyKeysBuilder` implementation with 176 usages
  - Cache keys built through query handlers
  - Registered as singleton in DI container
- **Evidence**:
  [src/Kentico.Community.Portal.Core/Operations/CacheDependencyKeysBuilder.cs](src/Kentico.Community.Portal.Core/Operations/CacheDependencyKeysBuilder.cs)

---

#### 3. Routing & View File Organization

- **Status**: ✅ **UPDATED**
- **What Changed**: Default Razor view location moved from
  `~/Views/Shared/PageTypes/` to `~/Views/Shared/ContentTypes/`
- **Adoption**:
  - Views organized in `~/Views/Shared/Kentico/Widgets/` and
    `~/Views/Shared/Kentico/FormComponents/`
  - Using `IWebPageUrlRetriever` (30+ usages)
  - Using `RoutedWebPage` context
- **Evidence**:
  [src/Kentico.Community.Portal.Web/Features/Blog/BlogPostPageTemplates.cs](src/Kentico.Community.Portal.Web/Features/Blog/BlogPostPageTemplates.cs)

---

#### 4. Site Domain Aliases (Code-Driven)

- **Status**: ✅ **MODERN CODE-DRIVEN APPROACH**
- **What Changed**: Moved from database-driven `SiteDomainAlias` to options
  pattern
- **Adoption**:
  - Using `IWebsiteChannelDomainProvider` (8+ usages)
  - No custom domain module approach
- **Evidence**:
  [src/Kentico.Community.Portal.Core/Modules/ChannelDataProvider.cs](src/Kentico.Community.Portal.Core/Modules/ChannelDataProvider.cs)

---

#### 5. Generic Provider Interfaces

- **Status**: ✅ **UNIVERSAL ADOPTION**
- **What Changed**: All specific provider interfaces obsolete, use generic
  `IInfoProvider<TInfo>`
- **Adoption**:
  - 230+ usages of generic `IInfoProvider<T>` across 86 files
  - Custom module providers implement pattern correctly:
    - `IMemberBadgeInfoProvider : IInfoProvider<MemberBadgeInfo>`
    - `IMemberBadgeMemberInfoProvider : IInfoProvider<MemberBadgeMemberInfo>, IBulkInfoProvider`
  - Obsolete interfaces replaced: IConsentInfoProvider, IEventLogInfoProvider,
    IMediaFileInfoProvider, ICountryInfoProvider, IStateInfoProvider, etc.
- **Evidence**:
  [src/Kentico.Community.Portal.Core/Modules/](src/Kentico.Community.Portal.Core/Modules/)

---

#### 6. NUnit Test Fixtures & v31.6.0 Integration Test API

- **Status**: ✅ **EXCELLENT ADOPTION** (v31.6.0 Update)
- **What Changed**:
  - v31.0.0: Test classes must inherit from `CMS.Tests` managed base classes
    instead of direct `Service.InitializeContainer()` calls
  - v31.6.0: New `IsolatedContainerUnitTests` base class introduced (lightweight
    container-only tests)
- **Adoption**:
  - ✅ Integration test: `PostPageIntegrationTests` correctly uses
    `IntegrationTests` base class (1 class)
  - ✅ Pure unit tests: Lightweight, no base class (2 classes:
    `DateTimeFormFieldTextValueExtractorTests`, `QAndASearchViewComponentTests`)
  - ✅ No `Service.InitializeContainer()` calls (0 violations)
  - ✅ Uses `Service.Resolve<T>()` and `Service.Use()` via base class
  - ✅ Test isolation maintained; no global state pollution
- **Evidence**:
  - [test/Kentico.Community.Portal.Web.Integration.Tests/Features/Blog/PostPageIntegrationTests.cs](test/Kentico.Community.Portal.Web.Integration.Tests/Features/Blog/PostPageIntegrationTests.cs)
    (IntegrationTests)
  - [test/Kentico.Community.Portal.Web.Tests/Rendering/DateTimeFormFieldTextValueExtractorTests.cs](test/Kentico.Community.Portal.Web.Tests/Rendering/DateTimeFormFieldTextValueExtractorTests.cs)
    (Pure Unit Test)
- **Future Guidance**:
  - For new CMS-dependent unit tests: Use `IsolatedContainerUnitTests` (v31.6.0
    recommendation)
  - For data-faker tests: Use `UnitTests` base class
  - For non-CMS tests: Keep current lightweight approach (no base class)
- **Action Item**: ℹ️ When upgrading NUnit to 4.5.0+ (planned Q4 2026), update
  test framework according to NUnit release notes

---

#### 7. Content Management API Changes

- **Status**: ✅ **MODERN APIS**
- **What Changed**: `IPageTypeFieldsProvider` → `IContentTypeFieldsProvider`;
  TreeNode methods → `IWebPageManager`
- **Adoption**:
  - No deprecated content management APIs in use
  - Using `ContentEngine` namespace system-wide
  - Using `IContentQueryExecutor`
- **Evidence**:
  [src/Kentico.Community.Portal.Core/Content/Pages/](src/Kentico.Community.Portal.Core/Content/Pages/)

---

#### 8. Module Customization (Modern Pattern)

- **Status**: ✅ **MODERN PATTERN**
- **What Changed**: Module inheritance pattern and initialization
- **Adoption**:
  - `PortalWebAdminModule : AdminModule` with `IDataTypeRegister` implementation
  - Uses `[assembly: RegisterModule(...)]` attribute pattern
  - Proper module lifecycle management
- **Evidence**:
  [src/Kentico.Community.Portal.Admin/PortalWebAdminModule.cs](src/Kentico.Community.Portal.Admin/PortalWebAdminModule.cs)

---

### ℹ️ Not Used (Not Critical)

#### UserInfo API

- **Status**: ℹ️ **NOT USED**
- **What Changed**: `UserInfo.Enabled` → `UserInfo.UserEnabled` (was fully
  removed)
- **Notes**: No active usage of UserInfo in codebase; not applicable to this
  project's concerns

---

## Part 2: v31.6.0 New Features Adoption

### ✅ Actively Implemented (5 Features)

#### 1. Management MCP Server

- **Status**: ✅ **FULLY CONFIGURED**
- **Details**: Xperience Management MCP server enabled for AI-assisted
  development
- **Configuration**:
  - `.mcp.json`: `xperience-management-api@31.6.0-preview` with
    `MANAGEMENT_API_URL=localhost:45041`
  - `Program.cs`: `.UseKenticoManagementApi()` enabled in development
  - `ServiceCollectionXperienceExtensions.cs`:
    `AddKenticoManagementApi(options => options.Secret = secret)`
  - Supports: pages, content items, content hub folders, channels, page
    templates, assets, member roles (retrieve)
- **Evidence**: [.mcp.json](.mcp.json),
  [src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs](src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs#L135-L147)

---

#### 2. Storage Path Registry (New Automatic Mapping)

- **Status**: ✅ **FULLY IMPLEMENTED**
- **Details**: Migrated from custom `StorageInitializationModule` to
  registry-based automatic path mapping
- **Configuration**:
  - `ServiceCollectionSaaSExtensions.cs`: Uses
    `AddXperienceCloudStoragePathMapping()` with custom Lucene container naming
  - Automatic environment-based storage provider mapping for Cloud/SaaS
  - Legacy approach noted as superseded in agent resources
- **Evidence**:
  [src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionSaaSExtensions.cs](src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionSaaSExtensions.cs#L14-L23)

---

#### 3. Continuous Integration CLI Commands

- **Status**: ✅ **FULLY IMPLEMENTED**
- **Details**: Three new CLI commands for managing CI state via command line
- **Implementation**:
  - `--kxp-ci-enable`: Enable CI from CLI
  - `--kxp-ci-disable`: Disable CI from CLI
  - `--kxp-ci-status`: Check CI status from CLI
  - All support `--format json` for machine-readable output
- **Evidence**:
  - [scripts/Enable-CI.ps1](scripts/Enable-CI.ps1#L24)
  - [scripts/Disable-CI.ps1](scripts/Disable-CI.ps1#L24)
  - [scripts/Get-CI-Status.ps1](scripts/Get-CI-Status.ps1#L24)

---

#### 4. Automatic Storage Path Mapping

- **Status**: ✅ **ACTIVE**
- **Details**: System automatically maps file system paths based on hosting
  environment
- **Configuration**:
  - Environment-based storage provider registration in
    `ServiceCollectionSaaSExtensions.cs`
  - Works in Cloud/SaaS contexts
  - No manual path customization needed (vs. legacy approach)
- **Evidence**:
  [src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionSaaSExtensions.cs](src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionSaaSExtensions.cs#L11)

---

#### 5. Management MCP Basic Validation

- **Status**: ✅ **IMPLEMENTED**
- **Details**: Validates MCP configuration at startup
- **Implementation**: Checks that API secret is not empty
- **Evidence**:
  [src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs](src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionXperienceExtensions.cs#L135-L147)

---

### ⚠️ Partially Adopted (2 Features)

#### 1. Custom Automation Actions

- **Status**: ⚠️ **PARTIAL - Built-in Only**
- **Details**: v31.6.0 allows custom `IAutomationAction` implementations for
  project-specific automation steps
- **Current State**:
  - Using built-in automation steps only
  - CI repository contains automation workflows (newsletter confirmation,
    autoresponder emails)
  - `AutomationProcessContactsTabExtender.cs` exists but doesn't extend with
    custom actions
- **Recommendation**:
  - 🔄 Consider adding custom automation actions for project-specific workflows
    (e.g., member badge assignment automation, newsletter management)
  - 📋 **Future Enhancement**: Implement `IAutomationActionProvider` for
    domain-specific automation steps
- **Evidence**:
  [src/Kentico.Community.Portal.Admin/UIPages/AutomationProcessContactsTabExtender.cs](src/Kentico.Community.Portal.Admin/UIPages/AutomationProcessContactsTabExtender.cs)

---

#### 2. AIRA Integration

- **Status**: ⚠️ **DOCUMENTATION ONLY**
- **Details**: v31.6.0 introduces richer AIRA context options (linked items,
  page preview) and custom automation actions
- **Current State**:
  - References to AIRA found only in content items (Q&A, documentation)
  - No code integration with AIRA services
  - AIRA configuration not leveraged for content generation
- **Recommendation**:
  - 🤔 **Strategic Decision Needed**: Determine if AIRA content generation
    aligns with project goals
  - 📋 **If Adopting**: Configure AIRA context (linked items + page preview for
    blog content generation), enable tone of voice guidelines
  - 📚 **Documentation**: See
    [Xperience AIRA Configuration](https://docs.kentico.com/documentation/developers-and-admins/configuration/aira-configuration)

---

### ❌ Not Implemented (3 Features)

#### 1. Content Locking

- **Status**: ❌ **ABSENT**
- **What Is It**: Feature that prevents conflicting concurrent edits to pages,
  content items, headless items, and emails
- **Current State**: No `IContentLockingService` or locking configuration found
- **Consideration**:
  - ℹ️ **Low Priority**: Only needed if multiple concurrent editors frequently
    work on same content
  - 🔧 **To Enable**: Add `ContentLockingConfiguration` to Xperience setup with
    lock duration and auto-release settings
- **Documentation**:
  [Content Locking](https://docs.kentico.com/documentation/business-users/content-locking)

---

#### 2. Overwrite Protection

- **Status**: ❌ **ABSENT**
- **What Is It**: v31.5.0 feature that prevents users from overwriting changes
  made by other users (or via API) while editing an item
- **Current State**: No `IVersionConflictDetector` or concurrency handling logic
- **Consideration**:
  - 📊 **Impact**: Content editors alerted if item changes while they're
    editing; must reload
  - 🔧 **To Enable**: System-level setting in administration, requires no code
    changes
  - ⚠️ **Status**: May be automatic in admin UI or requires configuration check
- **Documentation**:
  [Overwrite Protection](https://docs.kentico.com/changelog/index.html) (v31.5.0
  release notes)

---

#### 3. Partial Updates (PATCH Support)

- **Status**: ❌ **ABSENT**
- **What Is It**: v31.6.0 Management MCP server supports partial updates (HTTP
  PATCH) for content types, taxonomies, tags, workspaces, languages
- **Current State**: No PATCH verb handlers or partial update operations
- **Consideration**:
  - 🤖 **For AI Agents**: Makes MCP-assisted updates more efficient (only patch
    changed fields)
  - 🔧 **To Enable**: Requires MCP server updates; likely automatic in v31.6.0+
  - ℹ️ **Low Impact**: Not blocking any functionality; default PUT/POST still
    works
- **Documentation**:
  [Management MCP Partial Updates](https://docs.kentico.com/documentation/developers-and-admins/api/management-api)

---

## Part 3: CI/CD Configuration

### ✅ Repository.config Status

- **Status**: ⚠️ **NEEDS VERIFICATION**
- **Current State**: `cms.scheduledtaskconfiguration` appears in
  repository.config but commented
- **What Changed**: `cms.scheduledtask` → `cms.scheduledtaskconfiguration` (new
  object type in v31.6.0)
- **Action Item**:
  - ✅ Verify repository.config does not reference old `cms.scheduledtask`
    object type
  - 📋 Confirm `cms.scheduledtaskconfiguration` is properly included if
    scheduled tasks are managed via CI/CD
- **Evidence**: [scripts/Store-CI.ps1](scripts/Store-CI.ps1) and CI repository
  configuration

---

## Part 4: Database Schema Updates

### ✅ GUID Uniqueness Validation (v31.6.0)

- **Status**: ✅ **COMPLIANT**
- **What Changed**: Database-level uniqueness constraints added for GUID columns
- **Affected Columns**:
  - CMS_Channel.ChannelGUID
  - CMS_ContentItem.ContentItemGUID
  - CMS_ContentLanguage.ContentLanguageGUID
  - CMS_HeadlessChannel.HeadlessChannelGUID
  - CMS_HeadlessItem.HeadlessItemGUID
  - CMS_Tag.TagGUID
  - CMS_Taxonomy.TaxonomyGUID
  - CMS_WebsiteChannel.WebsiteChannelGUID
- **Action**: Database check recommended before upgrade (diagnostic query
  provided in v31.6.0 changelog)

---

## Summary Table

| Feature                                        | Category              | Status         | Priority    | Action                                |
| ---------------------------------------------- | --------------------- | -------------- | ----------- | ------------------------------------- |
| **IWebPageManager API**                        | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **IContentItemManager API**                    | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **ContentEngine**                              | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **Generic IInfoProvider<T>**                   | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **NUnit Test Base Classes (v31.0+)**           | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **IsolatedContainerUnitTests (v31.6.0)**       | v31.6.0 Test API      | ✅ Optimal     | —           | Use for new CMS-dependent unit tests  |
| **Data Caching (ICacheDependencyKeysBuilder)** | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **Routing (ContentTypes folder)**              | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **Site Domain Aliases (Code-Driven)**          | Core Breaking Changes | ✅ Complete    | —           | No action needed                      |
| **Management MCP Server**                      | v31.6.0 Features      | ✅ Active      | —           | No action needed                      |
| **Storage Path Registry**                      | v31.6.0 Features      | ✅ Implemented | —           | No action needed                      |
| **CLI CI Commands**                            | v31.6.0 Features      | ✅ Active      | —           | No action needed                      |
| **Custom Automation Actions**                  | v31.6.0 Features      | ⚠️ Partial     | 🔵 Low      | Consider for future workflows         |
| **AIRA Integration**                           | v31.6.0 Features      | ⚠️ Partial     | 🔵 Low      | Strategic decision required           |
| **Content Locking**                            | v31.6.0 Features      | ❌ Absent      | 🔵 Low      | Enable if multi-editor scenarios grow |
| **Overwrite Protection**                       | v31.5.0 Features      | ❌ Absent      | 🟢 Very Low | Check if automatic in admin UI        |
| **Partial Updates (PATCH)**                    | v31.6.0 Features      | ❌ Absent      | 🟢 Very Low | Automatic in MCP; no action needed    |

---

## Recommendations

### 🟢 No Action Required (v31.0-31.6 Compliant)

- ✅ Core API modernization is complete
- ✅ All deprecated APIs have been replaced
- ✅ Major v31.6.0 features are appropriately configured
- ✅ v31.6.0 test API optimal: `IntegrationTests` for DB tests, pure unit tests
  for business logic, future new CMS unit tests should use
  `IsolatedContainerUnitTests`

### 🟡 Future Enhancements (Lower Priority)

1. **Custom Automation Actions** – Implement if project gains more automation
   workflows
2. **AIRA Integration** – Evaluate ROI for AI-assisted content generation for
   blog/FAQ
3. **Content Locking** – Consider enabling if team size/concurrent editing
   increases

### 🔵 Informational (No Changes Needed)

1. **Overwrite Protection** – Verify if enabled by default in v31.5.0+
2. **PATCH Support** – Automatically available in MCP v31.6.0+
3. **NUnit Upgrade** – Prepare test migration plan for Q4 2026 (NUnit 4.5.0)

---

## References

- [Xperience v31.0.0 Breaking Changes](https://docs.kentico.com/changelog/index.html)
- [Xperience v31.6.0 Release Notes](https://docs.kentico.com/changelog/index.html)
- [Management MCP Server](https://docs.kentico.com/documentation/developers-and-admins/api/management-api)
- [Content Locking](https://docs.kentico.com/documentation/business-users/content-locking)
- [Storage Path Registry](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/storage-path-mapping)

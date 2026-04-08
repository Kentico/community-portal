---
name: configure-cdrepository
description: Builds a scoped CDRepository repository.config from recent CIRepository changes in an Xperience by Kentico project. Use when preparing a deployment package and you need CD filters derived from PR/commit CI diffs while excluding Xperience-update-only noise.
argument-hint: PR number or commit range to analyze
---

# Configure CDRepository from CI Changes

Create or update CD filters in App_Data/CDRepository/repository.config using
recent CIRepository deltas.

## Inputs

Ask for:

- PR number (or commit range)
- CIRepository path (default:
  src/Kentico.Community.Portal.Web/App_Data/CIRepository)
- CD config path (default:
  src/Kentico.Community.Portal.Web/App_Data/CDRepository/repository.config)
- Rule for Xperience update commits (default: exclude from deployment filter
  changes)

## Workflow

1. Research Kentico CI/CD config semantics with Kentico docs MCP.
2. Confirm repository.config version and behavior (v2 expected).
3. Collect changed CIRepository files from PR via gh CLI.
4. If gh is unavailable, use local git commands (log/show/diff).
5. Classify changed CI files by source:

- Feature/business changes
- Xperience update/package update changes

6. Exclude Xperience update-only artifacts from proposed CD filter changes
   unless user explicitly says include.
7. Map remaining CI file paths to object types using CI folder names.
8. Build minimal IncludedObjectTypes allowlist needed for selected changes.
9. Add ObjectFilters IncludedCodeNames entries for precise scope.
10. Keep one IncludedCodeNames element per object type (semicolon-separated
    values).
11. Preserve existing RestoreMode unless user asks to change.
12. Validate XML and check for duplicate/contradicting filters.
13. Run `scripts/Export-DeploymentPackage.ps1` and validate
    `src/Kentico.Community.Portal.Web/$CDRepository` includes correct folders
    and files for deployment

## Decision Rules

- If IncludedObjectTypes is populated, it is an allowlist: include every needed
  main object type.
- Use only main object types in IncludedObjectTypes.
- Child/binding objects follow parent inclusion rules.
- Prefer explicit object types over IncludeAll for regular CD deployments.
- Only add IncludedContentItemsOfType/ContentItemFilters when content item
  deployment is intentionally included.
- For content item name/type filtering, keep parent content type inclusion
  consistent.

## Mapping Hints

Common CI paths to object types:

- @global/cms.contenttype -> cms.contenttype
- @global/cms.memberrole -> cms.memberrole
- @global/cms.class -> cms.class
- @global/emaillibrary.emailtemplate -> emaillibrary.emailtemplate
- @global/cms.settingskey -> cms.settingskey
- _/contentitemdata._ or _/cms.contentitemcommondata_ -> content-item related;
  decide if content items are in scope

## Quality Checks

- XML is well-formed.
- No repeated IncludedCodeNames for the same ObjectType.
- IncludedCodeNames values match actual object code names from CI XML files.
- Proposed filters are minimal and exclude known Xperience update-only changes.
- Final config change is diffed and explained.

## Output Format

Finish with a concise deployment summary:

- Reviewed source (PR/commit range)
- Selected object types for deployment
- Selected code names by object type
- Explicitly excluded update-only groups and why
- Exact impact on repository.config (what was added/changed)

## Example Prompts

- Build CD filters from PR 241 CIRepository changes; exclude Xperience update
  noise.
- Update repository.config from last 10 commits touching CIRepository and
  summarize deployment scope.
- Rebuild CD allowlist/object filters from this release branch CI changes and
  show final diff rationale.

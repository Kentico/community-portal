---
name: update-xperience
description: Update Kentico Xperience NuGet and pnpm packages to latest non-breaking version and commit changes.
---

## Purpose

Automate updating all Kentico Xperience dependencies (`Kentico.Xperience.*`
NuGet + `@kentico/*` pnpm) when a newer version than the one declared in
`README.md` is available, run required update tasks, and produce a proper
commit. Abort early if no newer version or if breaking changes are detected.

## Preconditions (MUST validate before proceeding)

1. Git working tree clean: `git status --porcelain` MUST return no output.
2. Current branch MUST NOT be `main`.
3. Confirm central package management file `Directory.Packages.props` is
   present.

If any precondition fails: output a clear message and stop.

## Determine Current and Latest Xperience Version

1. Run `dotnet tool restore` at the repository root
2. Run `dotnet list Kentico.Community.Portal.slnx package --outdated` and
   analyze the output to identify if Xperience by Kentico packages are outdated
3. If there are no out of date packages: report "No new Xperience version
   available" and STOP.

## Update NuGet Packages

1. Open `Directory.Packages.props`.
2. Identify all `<PackageVersion Include="Kentico.Xperience.*" Version="X" />`
   entries where `Version` equals the current version.
3. Replace their `Version` with the latest version reported by
   `dotnet-outdated`.
4. If a referenced package is a `-preview` or `-prerelease` version and the
   version number matches the other `Kentico.Xperience.*` packages, try to
   update it to a new `-preview` or `-prerelease` version as well.
5. Do NOT change packages whose versions are intentionally different (examples
   today: `Kentico.Xperience.Lucene`, `Kentico.Xperience.MiniProfiler`,
   `Kentico.Xperience.TagManager`). These are outside the scope of this task.
6. Run restore:
   ```pwsh
   dotnet restore
   ```

## Update pnpm Packages (@kentico/\*)

1. Locate `package.json` files under `src/` containing dependencies or
   devDependencies starting with `@kentico/`.
2. For each found package run:
   ```pwsh
   corepack pnpm add <package>@latest --save-exact
   ```
   or if dev dependency: `--save-dev --save-exact`.
3. After updates, execute VS Code task: `pnpm: install (Admin)` (ensures
   lockfile consistency for Admin client).
4. After installation, run `pnpm: audit fix (Admin)` (attempts to resolve
   package vulnerabilities) and do not block workflow unless critical
5. Also look for related Xperience packages in the `.mcp.json` file and ensure
   their version is updated to match

## Build & Breaking Change Detection

1. Run VS Code task: `.NET: build (Solution)`.
2. If build fails OR compile errors reference removed/renamed Kentico APIs,
   treat as breaking changes: output a summary and STOP (do NOT commit).
3. Optional deeper check: run `.NET: test (Solution)`; if widespread failures
   clearly due to API changes, STOP.

## Run Xperience Application Update

1. Execute VS Code task: `Xperience: Application Update` (must invoke
   `Update-Xperience.ps1 -AgentMode`) to update database schema/data to new
   package version without interactive confirmation prompts. The `-AgentMode`
   switch ensures `--skip-confirmation` is passed for non-interactive
   automation.
2. If script fails: report failure, revert changes if feasible (manual revert
   recommended), STOP.

## Update README

1. Replace existing version line with new version:
   `This project is using Xperience by Kentico vNEW_VERSION (changelog: https://docs.kentico.com/documentation/changelog#ANCHOR).`
2. Determine `ANCHOR`:
   - Review the RSS feed for product updates
     <https://docs.kentico.com/feeds/xbyk-releases.xml>.
   - Find the RSS item for the hotfix or Refresh applied to the project
   - Replace the README link with the full URL for this RSS item.

## Database backup

1. Run ./scripts/Backup-Database.ps1 script to generate a database backup
2. The script creates a new .zip in ./database
3. Delete the old .zip and keep the new one

## Final Validation

1. Run `git diff --name-only` and ensure expected files modified (props file,
   README, package.json / lock files, potentially generated code, database
   scripts ignored if unchanged).
2. Optionally re-run `.NET: build (Solution)` to ensure no transient issues.

## Commit Changes

1. Stage changes:
   ```pwsh
   git add Directory.Packages.props README.md **/package.json **/pnpm-lock.yaml
   ```
   Add any generated files updated by the application update (e.g., under
   `src/**` if present).
2. Commit using Conventional Commit format:
   ```pwsh
   git commit -m "build(sln): update to Xperience v${latestVersion}"
   ```
3. If README changelog link is provisional, append a body line explaining
   placeholder.

## Output

Provide a concise summary including:

1. Previous vs new version.
2. Count of NuGet packages updated.
3. List of @kentico pnpm packages updated.
4. Confirmation of application update success.
5. Commit hash.

If stopped early (no update or breaking changes), explain reason and suggest
manual follow-up steps.

## Error Handling Guidelines

| Scenario                        | Action                                              |
| ------------------------------- | --------------------------------------------------- |
| Dirty working tree              | Abort; ask user to commit/stash changes.            |
| On `main` branch                | Abort; instruct user to create feature branch.      |
| NuGet query fails               | Retry with fallback search; if still failing abort. |
| Build fails post-update         | Report potential breaking changes; abort.           |
| Application update script fails | Report; abort commit.                               |

## Pseudocode Flow

```
ValidateRepoState()
currentVersion = ExtractVersionFromReadme()
latestVersion = QueryLatestNuGet()
if latestVersion <= currentVersion: Stop("No new version")
UpdateCentralPackageVersions(latestVersion)
UpdateKenticoPnpmPackages()
RunTask(".NET: build (Solution)")
if BuildFailed(): Stop("Breaking changes detected")
RunTask("Xperience: Application Update")
UpdateReadmeVersion(latestVersion)
BackupDatabase()
StageAndCommit(latestVersion)
ReportSummary()
```

## Notes

## Keep changes minimal; do not bump unrelated dependencies. Never force push or modify history. Do not proceed past any failure point without explicit user override.

name: Update Xperience Packages description: A prompt to guide an agent to
update Xperience by Kentico packages in the repository. tasks:

- check_repository_state: description: Ensure the repository is in a clean state
  and not on the main branch.
- check_new_version: description: Check if a newer version of Xperience by
  Kentico is available than the one in use.
- update_nuget_packages: description: Update all Xperience by Kentico NuGet
  packages to the latest version.
- update_pnpm_packages: description: Update all Xperience by Kentico pnpm
  packages to the latest version.
- install_admin_pnpm: description: Use VS Code tasks to install the Admin
  project pnpm packages.
- build_solution: description: Use VS Code tasks to perform a solution build.
- check_breaking_changes: description: Check for breaking changes after the
  updates.
- run_update_script: description: Run `Update-Xperience.ps1 -AgentMode`
  (non-interactive update applying `--skip-confirmation`).
- update_readme: description: Update the Xperience version and link in the
  README to match the package updates.
- stage_and_commit: description: Stage the updates in Git and create a commit
  following the Git commit rules for this repository.

---

---
name: Update-Xperience
description: Update Kentico Xperience NuGet and npm packages to latest non-breaking version and commit changes.
---

## Purpose

Automate updating all Kentico Xperience dependencies (`Kentico.Xperience.*`
NuGet + `@kentico/*` npm) when a newer version than the one declared in
`README.md` is available, run required update tasks, and produce a proper
commit. Abort early if no newer version or if breaking changes are detected.

## Preconditions (MUST validate before proceeding)

1. Git working tree clean: `git status --porcelain` MUST return no output.
2. Current branch MUST NOT be `main`.
3. Confirm central package management file `Directory.Packages.props` is
   present.

If any precondition fails: output a clear message and stop.

## Determine Current and Latest Xperience Version

1. Read `README.md` and extract the project's Xperience by Kentico version
2. Store captured semantic version as `currentVersion`.
3. Query NuGet for latest published stable version of `Kentico.Xperience.Core`:
   PowerShell example:
   ```pwsh
   $latestVersion = (Find-Package -Name Kentico.Xperience.Core -Source https://api.nuget.org/v3/index.json | Sort-Object Version -Descending | Select-Object -First 1 -ExpandProperty Version)
   ```
4. Compare `latestVersion` > `currentVersion` (semantic comparison). If NOT
   greater: report "No new Xperience version available" and STOP.

## Update NuGet Packages

1. Open `Directory.Packages.props`.
2. Identify all `<PackageVersion Include="Kentico.Xperience.*" Version="X" />`
   entries where `Version` equals `currentVersion`.
3. Replace their `Version` with `latestVersion`.
4. Do NOT change packages whose versions are intentionally different (examples
   today: `Kentico.Xperience.Lucene`, `Kentico.Xperience.MiniProfiler`,
   `Kentico.Xperience.TagManager`). These are outside the scope of this task.
5. Run restore:
   ```pwsh
   dotnet restore
   ```

## Update npm Packages (@kentico/\*)

1. Locate `package.json` files under `src/` containing dependencies or
   devDependencies starting with `@kentico/`.
2. For each found package run:
   ```pwsh
   npm install <package>@latest --save
   ```
   or if dev dependency: `--save-dev`.
3. After updates, execute VS Code task: `npm: install (Admin)` (ensures lockfile
   consistency for Admin client).
4. After installation, run `npm: audit fix (Admin)` (attempts to resolve package
   vulnerabilities) and do not block workflow unless critical

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
   - Visit `https://docs.kentico.com/documentation/changelog` and find anchor
     for the release/hotfix containing `latestVersion`.
   - Use pattern `#hotfix-<month-lowercase>-<day>-<year>` or
     `#release-<month-lowercase>-<day>-<year>` depending on listing. Insert full
     URL.
3. If anchor not confidently derivable, temporarily link to base changelog URL
   and note TODO; still proceed with commit (user can adjust later).

## Final Validation

1. Run `git diff --name-only` and ensure expected files modified (props file,
   README, package.json / lock files, potentially generated code, database
   scripts ignored if unchanged).
2. Optionally re-run `.NET: build (Solution)` to ensure no transient issues.

## Commit Changes

1. Stage changes:
   ```pwsh
   git add Directory.Packages.props README.md **/package.json **/package-lock.json
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
3. List of @kentico npm packages updated.
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
UpdateKenticoNpmPackages()
RunTask(".NET: build (Solution)")
if BuildFailed(): Stop("Breaking changes detected")
RunTask("Xperience: Application Update")
UpdateReadmeVersion(latestVersion)
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
- update_npm_packages: description: Update all Xperience by Kentico npm packages
  to the latest version.
- install_admin_npm: description: Use VS Code tasks to install the Admin project
  npm packages.
- build_solution: description: Use VS Code tasks to perform a solution build.
- check_breaking_changes: description: Check for breaking changes after the
  updates.
- run_update_script: description: Use VS Code task to run
  `Update-Xperience.ps1 -AgentMode` (non-interactive update applying
  `--skip-confirmation`).
- update_readme: description: Update the Xperience version and link in the
  README to match the package updates.
- stage_and_commit: description: Stage the updates in Git and create a commit
  following the Git commit rules for this repository.

---

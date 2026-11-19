---
name: diagnose-ci-failure
description: "Diagnoses a failed 'CI: Build and Test' run from its artifacts instead of re-running the pipeline. Use when a PR's CI run is red, a GitHub Actions workflow failed, an E2E or Playwright spec passed locally but failed in CI, or you need the trace, screenshot, or application log from a CI run."
argument-hint: "Optional run id, run URL, or branch name"
---

# Diagnose a CI failure

A red `CI: Build and Test` run has already captured what is needed to explain itself.
Read its artifacts first. Pushing another commit to reproduce the failure costs ~10
minutes and rarely reveals anything the first run did not record.

## Procedure

1. Find the failed run and the step that failed.

   ```powershell
   gh run list --workflow=ci.yml --branch <branch> --limit 5

   gh api repos/{owner}/{repo}/actions/runs/<run-id>/jobs `
     --jq '.jobs[].steps[] | select(.conclusion=="failure") | .name'
   ```

2. Route on the failed step name using the sections below.

3. Report the cause and the file to change. Only re-run CI once a fix is in hand.

## E2E steps

`Run E2E tests (portal)` and `Run E2E tests (admin)` upload a Playwright trace for every
failed spec. A trace holds the DOM snapshot at each step, the network log, the console
output, and a screenshot timeline — usually enough to explain a CI-only failure without
reproducing it.

**These artifacts are retained for one day.** Download them in the same session the run fails.

The artifact name ends in the run *number*, which is not the run id and is not shown by
`gh run list`. Read it from the run:

```powershell
$runNumber = gh run view <run-id> --json number --jq .number

gh run download <run-id> -n "playwright-artifacts-$runNumber" -D ./ci-artifacts

pnpm exec playwright show-trace ./ci-artifacts/test-results/<spec-folder>/trace.zip
```

There are no retries in CI, so a failing spec failed once, on its first attempt.

The trace is the only artifact. For server-side detail, read the E2E step's own console
output — Playwright pipes the application's stdout and stderr into it.

## Infrastructure steps

`Setup pnpm`, `Install npm dependencies`, `Install Playwright dependencies`,
`Install Playwright browsers`, and `Restore NuGet packages` reach the network. Failures
here are tooling or network problems, not code problems. These steps run early so this
class of failure is cheap to hit. Check whether a pinned version was withdrawn before
assuming a transient error.

## Database steps

`Restore Database from Backup` runs `scripts/Restore-Database-Backup.ps1`. It fails with a
message naming the stage and the command that broke, and dumps container logs when a
readiness gate times out. Read the step output before digging further.

`Restore CI Repository` runs `scripts/Restore-CI.ps1`. Failures here usually mean the
commit's CI repository files are inconsistent, which can be reproduced locally.

## CI and local environments differ deliberately

CI runs on Linux; local development runs on Windows. That difference is intentional — it
catches path, casing, and line-ending problems. Do not try to make CI mirror a local
machine. When something passes locally and fails in CI, CI is usually right.

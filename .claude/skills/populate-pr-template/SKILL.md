---
name: populate-pr-template
description: Populate this repository's pull request template from branch and CI evidence with concise, human-scannable sections. Use when creating or updating PR descriptions, checklists, or review summaries.
---

## Purpose

Fill `.github/PULL_REQUEST_TEMPLATE.md` for a PR using verified data from git
diff, status checks, and workflow output. Optimize for reviewer scanability and
minimize noise.

## Preconditions

1. PR number is known or active PR can be resolved.
2. CI status checks are queryable.
3. Diff can be compared against `main`.

If any precondition fails, report the blocker and stop.

## Source of Truth (in order)

1. GitHub status checks for CI result.
2. `git diff --name-only main...HEAD` for changed files.
3. `.github/PULL_REQUEST_TEMPLATE.md` headings (authoritative section schema).
4. PR title/body context and linked issues.

Do not infer test pass/fail from local assumptions.

## Population Rules

1. Keep bullets single-line.
2. Use concrete verbs first (Added, Updated, Removed, Migrated, Fixed).
3. Prefer grouped summaries over file dumps.
4. Remove unpopulated optional sections/subsections entirely.
5. Write `None` only for required fields that must remain present.
6. Cap sections to high-signal bullets:
   - `TL;DR`: max 3 bullets
   - `Changes by Area`: max 6 bullets per area
7. Exclude noisy generated paths unless material to review:
   - `src/Kentico.Community.Portal.Web/assets/**`
   - `src/Kentico.Community.Portal.Web/App_Data/CIRepository/**`
8. If excluded generated content materially affects behavior, summarize impact
   in `Content/Data/CI Repository` without listing files.
9. Do not claim manual testing unless explicit manual steps/evidence exist.
10. Do not add placeholders like "fixes #issue number" when issue is unknown;
    use `None`.
11. Keep wording neutral and review-focused.
12. Discover available `Changes by Area` sections from the template at runtime;
    do not hardcode area names in generated PR bodies.

## Section Mapping

- `Summary`: 1 short paragraph with why + impact.
- `TL;DR`: top change, top risk, first review focus.
- `Status`: risk, breaking change, manual test need, data impact.
- `Linked Work`: only verified issue references, else `None`.
- `Review Map`: 3 ordered review entry points by area.
- `Changes by Area`: use only area headings defined in
  `.github/PULL_REQUEST_TEMPLATE.md`; include only changed areas and remove
  empty area headings.
- `Validation/Automated`: default to required GitHub checks, and include
  status-check names/links only when non-obvious or useful for context.
- `Validation/Manual`: include only when manual testing is required.
- `Risk and Rollback`: one concise line each.
- `Out of Scope`: include only when there are explicit non-goals.
- `Reviewer Notes`: include only when there are tradeoffs/open questions.

## Checklist Policy

- Do not include "Tests are passing" as a checkbox in this repo template.
- Reflect test state only in `Validation/Automated` from CI checks.

## Update Workflow

1. Resolve PR and fetch status checks.
2. Read `.github/PULL_REQUEST_TEMPLATE.md` and extract `Changes by Area`
   headings.
3. Build area summaries from diff against `main` using extracted headings.
4. Draft the full template content.
5. Validate rules above (length, evidence, noise filtering). If any rule fails,
   revise the draft and re-validate before proceeding.
6. Update PR body.
7. Re-read PR body and confirm formatting integrity (line breaks preserved).

## Output Requirements

When done, provide:

1. PR number and URL.
2. Validation/Automated summary used.
3. Any assumptions or unknowns.
4. A short note if generated content was intentionally summarized.

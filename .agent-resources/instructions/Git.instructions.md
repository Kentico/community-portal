# Git Commit Message Conventions

When authoring git commit messages, always follow the project's established
conventions based on
[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary).

## Commit Message Format

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

## Types

Use one of these conventional commit types:

- **feat**: A new feature
- **fix**: A bug fix
- **refactor**: A code change that neither fixes a bug nor adds a feature
- **build**: Changes that affect the build system, dependencies, or project
  configuration
- **docs**: Documentation only changes
- **test**: Adding missing tests or correcting existing tests
- **chore**: Other changes that don't modify src or test files

## Scopes

Use these project-specific scopes to indicate the area of change:

- **Web**: Changes to the main web application (`Kentico.Community.Portal.Web`)
- **Core**: Changes to the core library (`Kentico.Community.Portal.Core`)
- **Admin**: Changes to the admin application (`Kentico.Community.Portal.Admin`)
- **sln**: Solution-level changes (packages, dependencies, project
  configuration)
- **<Filename>**: Documentation markdown file updates
- **copilot**: Copilot tool updates (anything changes in `.github` or
  `.vscode/mcp.json`)
- **vscode**: VS Code workspace configuration changes

## Description Guidelines

- Use the imperative mood ("add feature" not "added feature")
- Don't capitalize the first letter
- Don't end with a period
- Keep it concise but descriptive
- Focus on what the change does, not how it does it

## Examples

Good commit messages from this project:

```
fix(Web): resolve null reference exceptions in email services
refactor(Web): improve LicenseFileService architecture and performance
build(sln): update to Xperience v30.8.1
docs(mermaid): add MCP generated content arch diagrams
fix(Web): taxonomies query cache dependencies
feat(Web): add new blog post validation logic
perf(Core): optimize content query execution
```

## Multi-line Commits

For complex changes, use the body to explain the motivation and implementation
details:

```
fix(Web): optimize BlogPostContentAutoPopulateHandler to prevent unnecessary updates

- Replace IInfoProvider call with IContentQueryExecutor for BlogPostContent retrieval
- Add proper ContentQueryExecutionOptions with ForPreview=true and IncludeSecuredItems=true
- Fix comparison logic to properly detect when taxonomy fields are unchanged
- Add AreTagReferencesEqual helper method for accurate TagReference collection comparison
- Remove redundant contentProvider.GetAsync call in Update handler

This prevents the handler from making updates when the BlogPostPage's taxonomy fields
haven't actually changed compared to the associated BlogPostContent item.
```

## Branch Naming

When creating branches, use these prefixes:

- `feat/` - for new functionality
- `refactor/` - for restructuring of existing features
- `fix/` - for bugfixes

## Breaking Changes

For breaking changes, add `!` after the type/scope and include details in the
footer:

```
feat(Web)!: remove deprecated API endpoints

BREAKING CHANGE: The legacy blog API endpoints have been removed.
Use the new ContentRetriever-based endpoints instead.
```

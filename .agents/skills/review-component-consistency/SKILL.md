---
name: review-component-consistency
description: Review a specific code component type throughout the codebase (e.g., Page Builder Widgets, Razor templates, scheduled tasks) and identify inconsistencies in naming, registration, properties, validation, querying, and structure. Use when analyzing component implementations for consistency and estimating AI code generation risk.
---

# Review Component Consistency

Systematically audit a code component type across the codebase, identify
inconsistency patterns, and estimate their impact on AI agent code generation.

## Quick Start

1. **Name the component type**: "Review Page Builder Widgets" or "Review custom
   scheduled tasks" or "Review admin UI pages"
2. **The skill will explore and report**:
   - Inconsistency patterns (naming, registration, properties, structure)
   - Impact on AI code generation (Low/Medium/High risk)
   - Recommended changes with total risk estimates

## Process

### 1. Clarify the Component Type

Ask:

- What component type should I review? (e.g., Page Builder Widgets, Razor
  templates, scheduled tasks, admin UI pages)
- Any specific project area? (or search entire codebase)
- Are there reference examples of "well-implemented" versions?

### 2. Discover All Implementations

Use the Explore agent to find all implementations of this component type:

- File patterns and locations
- Naming conventions currently in use
- Registration patterns (attributes, DI, configuration)
- Count of implementations

Output: A discovery matrix showing all found components with basic info.

### 3. Audit Consistency Dimensions

For each consistency dimension, analyze across all implementations:

**Naming Conventions**

- Class/file name patterns (e.g., `{Domain}{Type}Widget`, `{Feature}Provider`)
- Method, function, variable, and field names
- Naming of supporting files (interfaces, extensions, utilities, types)
- Namespaces
- Outliers: count of implementations that don't match majority pattern

**Registration & Configuration**

- How components are registered (attributes, DI container, configuration files,
  manual setup)
- Consistency of registration metadata
- Required vs optional configurations
- Services the component depends on
- Module exposure through type system (exported, public, internal, etc...)
- Outliers and their registration approaches

**Properties & Attributes**

- Property name patterns and casing conventions
- Use of validation attributes or decorators (required, range, length, etc.)
- Property type consistency (when semantically representing the same concept)
- Documentation/description patterns
- Outliers: properties that break patterns

**State & Invariance Validation**

- Constructor validation patterns
- Immutability expectations
- State initialization approaches
- Null/empty checking strategies
- Outliers: unusual validation or state handling

**Content Querying and Service calls**

- How components query or access data (repositories, APIs, caches, imports)
- Query patterns and abstractions used
- Outliers: components with unique querying strategies

**File & Folder Structure**

- File naming (PascalCase vs. kebab-case, file extensions)
- Directory organization relative to component type
- Colocation or existence of related files (tests, styles, config, supporting
  classes)
- Outliers: components in unexpected locations

### 4. Report Findings

For each consistency dimension, create a structured report:

```
## [Dimension Name]

**Majority Pattern** (N implementations, X%):
- [description]

**Outliers** (N implementations, X%):
- [outlier pattern 1]: [list of components]
- [outlier pattern 2]: [list of components]

**Impact if Not Standardized**:
- AI code generation risk: [Low/Medium/High]
- Specific risk: [how AI agents might generate code that doesn't match majority pattern or violates implicit expectations]
```

### 5. Estimate AI Code Generation Impact

For each inconsistency, estimate impact on AI agent code generation:

- **Low**: Pattern variance doesn't affect agent ability to generate code
  matching the codebase; agents infer rules reliably from existing code
- **Medium**: Agents may generate code matching majority pattern but could miss
  minority patterns; requires clarification to stay consistent
- **High**: Inconsistency likely causes agents to generate mismatched,
  conflicting, or suboptimal code; new code added to codebase would increase
  inconsistency

Consider:

- How clearly can AI infer the rules from existing code?
- Will an agent with no special context generate code matching the majority
  pattern?
- Are there edge cases where an agent would be confidently wrong?
- How much could this lead to new inconsistencies being added to the codebase?
- Would a developer need to significantly rework or clarify AI-generated code?

### 6. Recommend Changes

For each High-impact inconsistency, recommend changes:

```
**Recommendation**: [Summary of change]

**Justification**:
- Aligns with majority pattern (X% of implementations)
- Reduces risk of [specific AI code generation problem]
- Improves clarity for [specific scenarios where agents struggle]

**Total Risk Estimate**: [Low/Medium/High]
- Low: narrow, mechanical changes with low chance of regressions
- Medium: broader updates needing targeted validation
- High: cross-cutting changes with meaningful regression risk

**Affected Components** (N total):
- [list of components that need change]

**Migration Path**:
1. [step 1]
2. [step 2]
```

### 7. Prioritize & Present

Create a prioritized action plan:

1. **Critical** (High AI code generation risk, Low remediation risk)
2. **Important** (High AI code generation risk, Medium/High remediation risk)
3. **Nice-to-have** (Medium or Low AI code generation risk)

Present findings with:

- Executive summary of inconsistencies found
- AI code generation risk assessment for each dimension
- Prioritized recommendations with rationale
- Total risk breakdown by priority

## Advanced Features

For large-scale consistency reviews:

### Quantitative Analysis

- Generate a consistency score (0-100) for each dimension
- Overall component type consistency rating
- Trend analysis if historical baseline exists

### Refactor Automation

- Identify components that can be auto-refactored (renaming, reorganizing)
- Separate strategic decisions (must review) from mechanical changes (can
  automate)

### Reference Implementation

- Suggest which existing implementation best exemplifies the ideal pattern
- Create it as reference documentation for new implementations
- Use as a template that prompts can reference in instructions

## Example Prompts

- "Review all Page Builder Widgets for consistency in property naming and
  validation"
- "Audit custom scheduled tasks — check registration and how they handle
  configuration"
- "Is there consistency in how admin UI pages are structured and styled?"
- "Find inconsistencies in how Razor templates access and display data"
- "Do our service handlers all follow the same error handling and validation
  patterns?"

## 8. Format analysis as HTML report

- Format detailed data, statistics, rankings, and suggestions as a minimally
  designed, but very organized and readable HTML document
- Use tables, sections, tabs, accordions, graphs, cards, or any other helpful
  layout design elements

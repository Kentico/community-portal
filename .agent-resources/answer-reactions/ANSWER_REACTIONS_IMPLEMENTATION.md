# Implementation Plan: Answer Upvote Feature

## Overview

Add a member-facing upvote feature for Q&A answers. Members click a badge to
toggle upvotes on/off. Badge displays total count with tooltip showing all
members who upvoted.

## Architecture Summary

- **View Model**: `QAndAPostAnswerViewModel` + reaction metadata properties
- **UI**: Badge + HTMX form in `_QAndAAnswerDetail.cshtml`
- **Controller**: `ToggleAnswerReaction()` action in `QAndAAnswerController`
- **Database**: Existing `QAndAAnswerReactionInfo` binding table
- **Rate Limiting**: New `UpdateAnswerReaction` policy (10 per minute)

---

## Implementation Steps

### 1. Update Permissions Model

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAPermissionService.cs`

- Add `bool CanReact` property to `QAndAAnswerPermissions` record
- All authenticated members get `CanReact = true`
- Update `NoPermissions` static instance with `CanReact = false`

**Status**: ✅ COMPLETED

### 2. Extend QAndAPostAnswerViewModel

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs`

Add properties:

- `int ReactionCount { get; set; }` - total upvotes
- `bool CurrentMemberHasReacted { get; set; }` - current user upvoted?
- `List<string> MembersWhoReacted { get; set; } = []` - display names for
  tooltip

Data source: Query `QAndAAnswerReactionInfo` by `QAndAAnswerReactionAnswerID`
where `QAndAAnswerReactionType == "upvote"`

**Status**: ✅ COMPLETED

### 3. Update Answer Detail View - Add Upvote Badge

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/_QAndAAnswerDetail.cshtml`

Add badge section **before** action buttons (Edit, Delete, Mark as Answer):

**Requirements**:

- Show: `⬆️ {ReactionCount}`
- Only render if `Model.Permissions.CanReact == true`
- Apply active/highlighted state if `Model.CurrentMemberHasReacted == true`
  - Active state: `btn-primary` class
  - Inactive state: `btn-outline-primary` class
- Bootstrap tooltip with comma-separated member names (only if
  `ReactionCount > 0`)
- Wrapped in HTMX form

**HTMX Form Attributes**:

```
hx-post
hx-controller="QAndAAnswer"
hx-action="ToggleAnswerReaction"
hx-route-answerID="Model.ID"
hx-route-questionID="Model.ParentQuestionID"
```

**Tooltip Pattern**:

```html
data-bs-toggle="tooltip"
data-bs-placement="top"
data-bs-title="Upvoted by: {comma-separated member names}"
```

**Status**: ✅ COMPLETED

### 4. Add Rate Limiting Constant

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndARateLimitingConstants.cs`

Add constant:

```csharp
public const string UpdateAnswerReaction = "QAndA_UpdateAnswerReaction";
```

**Rate Limiting Policy Configuration**: (Needs to be added to Program.cs or
feature configuration)

```csharp
options.AddPolicy(
    QAndARateLimitingConstants.UpdateAnswerReaction,
    httpContext => RateLimitingUtilities.CreateSlidingWindowPartition(
        httpContext,
        permitLimit: 10,
        segmentsPerWindow: 5,
        window: TimeSpan.FromMinutes(1)
    )
)
```

**Status**: ✅ COMPLETED (constant added, policy needs registration in
configuration)

### 5. Create MediatR Commands & Handlers

#### Create Reaction Command

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAAnswerReactionCreateCommand.cs`

```csharp
public record QAndAAnswerReactionCreateCommand(
    int MemberId,
    int AnswerId,
    string ReactionType = "upvote")
    : ICommand<Result<int>>;
```

Handler: Creates `QAndAAnswerReactionInfo` record with member ID, answer ID,
type, and current timestamp

**Status**: ✅ COMPLETED

#### Delete Reaction Command

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAAnswerReactionDeleteCommand.cs`

```csharp
public record QAndAAnswerReactionDeleteCommand(
    int MemberId,
    int AnswerId,
    string ReactionType = "upvote")
    : ICommand<Result>;
```

Handler: Queries for existing reaction by (MemberId, AnswerId, Type) and deletes
if found

**Pattern**: Both use `Result<T>` monad from CSharpFunctionalExtensions

**Status**: ✅ COMPLETED

### 6. Create Query Handler for Reaction Data

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAAnswerReactionsQuery.cs`

```csharp
public record AnswerReactionsData(
    int TotalCount,
    bool CurrentMemberHasReacted,
    List<string> MembersWhoReacted);

public record QAndAAnswerReactionsQuery(
    int AnswerId,
    int? CurrentMemberId = null)
    : IQuery<AnswerReactionsData>;
```

Handler:

- Queries all upvote reactions for answer
- Counts total
- Checks if current member in list
- Fetches member display names via `UserManager.FindByIdAsync()`
- Returns `AnswerReactionsData`

**Status**: ✅ COMPLETED

### 7. Implement ToggleAnswerReaction Controller Action

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Components/Form/QAndAAnswerController.cs`

Action Signature:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[EnableRateLimiting(QAndARateLimitingConstants.UpdateAnswerReaction)]
public async Task<ActionResult> ToggleAnswerReaction(Guid questionID, int answerID)
```

**Logic Flow**:

1. Check `readOnlyProvider.IsReadOnly()` → return 503
2. Get current user via `userManager.CurrentUser(HttpContext)` → return
   Unauthorized if null
3. Validate question exists via
   `contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>`
4. Validate answer exists via
   `mediator.Send(new QAndAAnswerDataByIDQuery(answerID))`
5. Query existing reaction via
   `mediator.Send(new QAndAAnswerReactionsQuery(answerID, member.Id))`
6. **Toggle**:
   - If exists → delete it (send `QAndAAnswerReactionDeleteCommand`)
   - If not exists → create it (send `QAndAAnswerReactionCreateCommand`)
7. Redirect to question page to refresh all answers with updated reaction data

**No Notifications**: Per requirements, do NOT trigger notifications or
subscriptions on reaction

**Status**: ✅ COMPLETED

### 8. Update Question Page Controller/View Model Builder

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs`
(MapAnswer method)

When building answer list:

- For each answer, call `QAndAAnswerReactionsQuery` passing answer ID and
  current member ID (nullable)
- Populate reaction properties on `QAndAPostAnswerViewModel`:
  - `ReactionCount = reactionData.TotalCount`
  - `CurrentMemberHasReacted = reactionData.CurrentMemberHasReacted`
  - `MembersWhoReacted = reactionData.MembersWhoReacted`

**Status**: ✅ COMPLETED

### 9. Admin UI - List and Manage Reactions

**Files to Create**:

#### A. Admin Controller for Reactions

**File**:
`src/Kentico.Community.Portal.Admin/Features/QAndA/QAndAAnswerReactionAdminController.cs`

- Route: `/admin/qanda/answer-reactions`
- Actions:
  - `ListReactions()` - GET list all reactions with filters
  - `DeleteReaction()` - POST delete a reaction
  - `ExportReactions()` - GET export reaction data (optional)

**Responsibilities**:

- Query all reactions with pagination
- Support filtering by:
  - Answer ID / Question ID
  - Member ID / Member name
  - Reaction Type
  - Date range
- Load related data (question title, answer preview, member name)
- Enforce admin authorization

#### B. Admin View Model

**File**:
`src/Kentico.Community.Portal.Admin/Features/QAndA/ReactionAdminListViewModel.cs`

**Properties**:

```csharp
public record ReactionListItemViewModel(
    int ReactionID,
    Guid ReactionGUID,
    string ReactionType,
    string MemberDisplayName,
    int MemberID,
    string QuestionTitle,
    int QuestionID,
    string AnswerPreview,
    int AnswerID,
    DateTime DateCreated
);

public class ReactionAdminListViewModel
{
    public List<ReactionListItemViewModel> Reactions { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }

    // Filter properties
    public string? FilterQuestion { get; set; }
    public string? FilterMember { get; set; }
    public string? FilterReactionType { get; set; }
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }
}
```

#### C. Admin List View

**File**:
`src/Kentico.Community.Portal.Admin/Features/QAndA/ReactionsList.cshtml`

**Components**:

- Filter form (question, member, type, date range)
- Sortable table with columns:
  - Reaction Type (icon: ⬆️)
  - Member Name (link to member profile)
  - Answer Preview (truncated, link to answer)
  - Question Title (link to question)
  - Date Created
  - Actions (Delete button)
- Pagination controls
- Bulk actions (select multiple, delete all)

**Pattern**: Follow existing admin pages in Xperience Admin (e.g., content list
views, form field management)

**Styling**: Use existing admin CSS/Bootstrap patterns from portal admin UI

#### D. Admin Query Handler

**File**:
`src/Kentico.Community.Portal.Admin/Features/QAndA/ListReactionsAdminQuery.cs`

**Responsibilities**:

- Query `QAndAAnswerReactionInfo` with filters
- Join with `QAndAAnswerDataInfo` for answer data
- Join with question content using `IContentRetriever`
- Join with `CommunityMember` for member names
- Support pagination and sorting
- Use efficient query patterns (selected columns only)

```csharp
public record ListReactionsAdminQuery(
    int Page = 1,
    int PageSize = 20,
    string? QuestionFilter = null,
    string? MemberFilter = null,
    string? ReactionTypeFilter = null,
    DateTime? DateFromFilter = null,
    DateTime? DateToFilter = null,
    string OrderBy = "DateCreated DESC")
    : IQuery<ReactionAdminListViewModel>;
```

#### E. Delete Reaction Admin Command

**File**:
`src/Kentico.Community.Portal.Admin/Features/QAndA/DeleteReactionAdminCommand.cs`

- Admin-only action to delete reactions
- Accepts reaction GUID (safer than ID)
- Hard delete via `IInfoProvider<QAndAAnswerReactionInfo>.DeleteAsync()`
- Log deletion for audit trail (use IEventLogService if available)
- Return success/error with affected count

**Authorization**: Enforce `[Authorize]` with admin role check in controller

### Admin UI Patterns (Reference)

- **Filter Form**: Use ASP.NET Core model binding, form submits to same route
  with query parameters
- **Table**: Bootstrap table classes (`table`, `table-hover`, `table-striped`)
- **Pagination**: Implement custom pagination or use existing portal pagination
  component
- **Delete Confirmation**: Use JavaScript confirm modal or Bootstrap modal
- **Member Links**: Link to member detail in admin area
- **Answer Links**: Link to question page with anchor hash to specific answer
- **Date Formatting**: Use consistent date format (e.g.,
  `@item.DateCreated.ToString("g", culture)`)

### Admin Access & Permissions

- **Authorization**: Require `[Authorize(Roles = "Administrator")]` attribute on
  controller
- **Admin Navigation**: Add menu item to admin navigation if admin menu system
  exists
- **Audit Logging**: Log deletion events via `IEventLogService` (for
  compliance/moderation trail)
- **Safe Deletion**: Use GUID in URLs instead of ID for safety

### Performance Considerations

- **Database Indexes**: Ensure indexes on:
  - `QAndAAnswerReactionAnswerID` (for filtering by answer)
  - `QAndAAnswerReactionMemberID` (for filtering by member)
  - `QAndAAnswerReactionDateModified` (for date range filters)
  - Composite: `(QAndAAnswerReactionType, QAndAAnswerReactionDateModified)`
- **Query Optimization**: Use `.Columns()` to select only needed fields in query
- **Pagination**: Always paginate (default 20 per page, max 100)
- **N+1 Prevention**: Load all related data in single query (joins) not loop
- **Caching**: Cache question/member lookup data if querying many reactions (not
  critical for admin UI with low traffic)

**Status**: ⏳ NOT STARTED (future implementation)

---

## Architecture Decisions

### 1. Unique Constraint on (MemberID, AnswerID, Type)

- **Why**: Prevents duplicate reactions; allows future emoji reactions
- **Database**: Composite unique index
- **Application**: Query check before insert ensures idempotency
- **Graceful Handling**: Return existing reaction if duplicate attempt

### 2. Tooltip Member Names

- **Scale**: Current scale (~4000 reactions total) supports full name lists
- **Future**: If tooltip exceeds usability (50+ upvotes), add modal for full
  list
- **Current**: Comma-separated display names in tooltip

### 3. Query Pattern

- No reaction count denormalization in answer table
- Fresh query per answer load (acceptable at current scale)
- No separate caching layer needed (reactions are display-only, non-critical)

### 4. Toggle Response Strategy

- Redirect to question page after toggle (HTMX redirect)
- Simplifies UI update: browser navigates naturally
- Avoids partial view swap complexity
- Page refresh ensures all answer counts are current

---

## File Summary

**Modified Files**:

- `src/Kentico.Community.Portal.Web/Features/QAndA/QAndAPermissionService.cs` -
  Add `CanReact` property
- `src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs` -
  Add reaction properties + populate in MapAnswer
- `src/Kentico.Community.Portal.Web/Features/QAndA/_QAndAAnswerDetail.cshtml` -
  Add upvote badge + HTMX form + readonly fieldset
- `src/Kentico.Community.Portal.Web/Features/QAndA/QAndARateLimitingConstants.cs` -
  Add `UpdateAnswerReaction` constant
- `src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionMvcExtensions.cs` -
  Add rate limiting policy registration
- `src/Kentico.Community.Portal.Web/Features/QAndA/Components/Form/QAndAAnswerController.cs` -
  Add `ToggleAnswerReaction` action

**New Files (Frontend)**:

- `src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAAnswerReactionCreateCommand.cs`
- `src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAAnswerReactionDeleteCommand.cs`
- `src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAAnswerReactionsQuery.cs`

**New Files (Admin) - Future**:

- `src/Kentico.Community.Portal.Admin/Features/QAndA/QAndAAnswerReactionAdminController.cs`
- `src/Kentico.Community.Portal.Admin/Features/QAndA/ReactionAdminListViewModel.cs`
- `src/Kentico.Community.Portal.Admin/Features/QAndA/ReactionsList.cshtml`
- `src/Kentico.Community.Portal.Admin/Features/QAndA/ListReactionsAdminQuery.cs`
- `src/Kentico.Community.Portal.Admin/Features/QAndA/DeleteReactionAdminCommand.cs`

**Existing (No Changes)**:

- `src/Kentico.Community.Portal.Core/Modules/QAndAAnswerReaction/QAndAAnswerReactionInfo.generated.cs` -
  Already exists with correct schema

---

## TODO: Rate Limiting Configuration

The rate limiting constant has been added, but the policy registration needs to
be added to the application configuration (likely in `Program.cs`). This should
be added to the rate limiting policy registration section:

```csharp
options.AddPolicy(
    QAndARateLimitingConstants.UpdateAnswerReaction,
    httpContext => RateLimitingUtilities.CreateSlidingWindowPartition(
        httpContext,
        permitLimit: 10,
        segmentsPerWindow: 5,
        window: TimeSpan.FromMinutes(1)
    )
)
```

---

## Implementation Order (Completed)

1. ✅ Permissions & View Model
2. ✅ Rate Limiting Setup
3. ✅ MediatR Commands & Handlers
4. ✅ Query Handler
5. ✅ Controller Action
6. ✅ View & Data Population
7. ⏳ Admin UI (List, Filter, Delete)

---

## Considerations for Future Expansion

### Multiple Reaction Types (Emoji Reactions)

- Schema already supports via `ReactionType` string field
- Add new constants: `const string REACTION_TYPE_UPVOTE = "upvote";`
- UI could extend to show multiple emoji buttons
- Query handler easily filters by type
- No breaking changes needed

### Performance at Scale

- Current: ~500 reactions
- Projected (2-3 years): ~1500-2000 reactions
- Likely query performance: negligible (small table)
- If needed: Add index on `(AnswerID, Type)` for filtering

### Permission Granularity

- Current: All authenticated users can react
- Future: Add role-based restriction (e.g., only members with 50+ reputation)
- Implement via `IQAndAPermissionService.HasPermission()` extension

---

## Testing Checklist

- [ ] Toggle upvote as authenticated user → reaction created, redirects to
      question
- [ ] Click again → reaction deleted, redirects to question
- [ ] Tooltip shows correct member names
- [ ] Rate limiting enforced (>10 per minute blocked)
- [ ] Anonymous/unauthorized users → badge hidden
- [ ] Read-only mode → return 503 on attempt
- [ ] Deleting answer → cascades delete reactions (verify FK)
- [ ] Deleting member → cascades delete reactions (verify FK)
- [ ] Multiple answers with different reaction counts display correctly
- [ ] Member display names handle null/empty first/last names gracefully

---

## Notes

- **No Notifications**: Reactions do not trigger Q&A discussion notifications
  (per requirements)
- **No Auto-Subscription**: Reacting does not auto-subscribe member to
  discussion (per requirements)
- **Idempotent**: Toggle pattern ensures safe repeated clicks
- **Graceful Degradation**: Badge hidden if user not authorized to react
- **Future-Proof Schema**: Supports multiple reaction types for emoji reactions

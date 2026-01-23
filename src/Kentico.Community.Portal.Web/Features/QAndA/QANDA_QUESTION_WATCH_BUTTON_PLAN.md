# Q&A Question Watch/Unwatch Button - Implementation Plan

## Overview

Add watch/unwatch button functionality to individual Q&A question detail pages
to allow users to subscribe to discussion notifications directly from the
question page, using HTMX patterns consistent with existing Q&A features.

## Analysis of Existing Features

### Existing Backend Infrastructure

- `QAndAQuestionController.UpdateQuestionWatch` action already exists (line
  223-248)
- `QAndANotificationSettingsManager.SetQuestionWatch` and `SetQuestionUnWatch`
  methods exist
- Rate limiting already configured:
  `QAndARateLimitingConstants.UpdateQuestionWatch`
- Backend accepts `Guid questionID` and `bool isWatching`

### Existing Frontend Infrastructure

- **HTMX is used throughout the app** for all Q&A interactions
- `_QAndANotificationsForm.cshtml` uses HTMX for unwatch buttons
- `_QAndAQuestionDetail.cshtml` uses HTMX for Edit/Delete buttons
- `_QAndAAnswerDetail.cshtml` uses HTMX for answer operations
- All Q&A forms use HTMX with `hx-post`, `hx-swap`, `hx-target` patterns

### Why HTMX Over Plain JavaScript

1. **Consistency** - All Q&A features use HTMX
2. **Automatic CSRF** - HTMX handles anti-forgery tokens automatically
3. **Built-in loading** - `xpc-loading-button` attribute provides spinner
4. **Simpler** - No custom JavaScript needed
5. **Maintainable** - Follows established patterns in codebase

**Note:** `notifications.js` exists but is not currently used anywhere in the
app's views.

## Implementation Steps

### 1. Add IsWatching Property to ViewModel

**File:** `QAndAQuestionPageController.cs` (lines 195-234)

Add property to `QAndAPostQuestionViewModel`:

```csharp
public bool IsWatchingQuestion { get; set; }
```

### 2. Update ViewModel Mapping

**File:** `QAndAQuestionPageController.cs` (method `MapQuestion`, around
line 121)

Add parameter to constructor:

```csharp
private async Task<QAndAPostQuestionViewModel> MapQuestion(
    QAndAQuestionPage question,
    CommunityMember? currentMember,
    QAndATaxonomiesQueryResponse taxonomiesResp)
{
    // ... existing code ...

    bool isWatching = false;
    if (currentMember is not null)
    {
        var watchedWebPageItemIDs = await notificationSettingsManager.GetWatchedWebPageItemIDs(currentMember);
        isWatching = watchedWebPageItemIDs.Contains(question.SystemFields.WebPageItemID);
    }

    return new QAndAPostQuestionViewModel(
        question,
        permissions,
        currentMember,
        taxonomiesResp,
        author,
        markdownRenderer,
        isWatching  // Add this parameter
    );
}
```

Update `QAndAPostQuestionViewModel` constructor to accept and set `isWatching`:

```csharp
public QAndAPostQuestionViewModel(
    QAndAQuestionPage question,
    QAndAQuestionPermissions permissions,
    CommunityMember? currentMember,
    QAndATaxonomiesQueryResponse taxonomiesResp,
    QAndAAuthorViewModel author,
    MarkdownRenderer markdownRenderer,
    bool isWatching  // Add this parameter
)
{
    // ... existing code ...
    IsWatchingQuestion = isWatching;
}
```

### 3. Add GetIsWatching Method to QAndANotificationSettingsManager (Optional)

**File:** `QAndANotificationSettingsManager.cs`

The existing `GetWatchedWebPageItemIDs` method can be used, but optionally add a
direct check method:

```csharp
public async Task<bool> GetIsWatchingQuestion(int memberID, int webPageItemID)
{
    var notifications = await GetNotificationsByMemberID(memberID);
    return notifications
        .Select(n => n.QAndADiscussionMemberNotificationQuestionWebPageItemID)
        .Contains(webPageItemID);
}
```

### 4. Create \_QAndAWatchButton Partial View

**File:** `_QAndAWatchButton.cshtml`

Create reusable button component that swaps itself on toggle using HTMX:

```razor
@model (Guid QuestionID, bool IsWatching)

@{
    string buttonId = $"watchButton_{Model.QuestionID:N}";
    bool isWatching = Model.IsWatching;
}

<button id="@buttonId"
    type="button"
    class="btn btn-sm btn-outline-secondary d-flex align-items-center gap-1"
    hx-post
    hx-controller="QAndAQuestion"
    hx-action="UpdateQuestionWatch"
    hx-route-questionID="@Model.QuestionID"
    hx-vals='{"isWatching": @(!isWatching).ToString().ToLower()}'
    hx-swap="outerHTML"
    hx-target="#@buttonId"
    title="@(isWatching ? "Stop watching this discussion" : "Watch this discussion")"
    xpc-loading-button>
    @if (isWatching)
    {
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8 16a2 2 0 0 0 2-2H6a2 2 0 0 0 2 2"/>
            <path d="M8 1.918l-.797.161A4 4 0 0 0 4 6c0 .628-.134 2.197-.459 3.742-.16.767-.376 1.566-.663 2.258h10.244c-.287-.692-.502-1.49-.663-2.258C12.134 8.197 12 6.628 12 6a4 4 0 0 0-3.203-3.92L8 1.917zM14.22 12c.223.447.481.801.78 1H1c.299-.199.557-.553.78-1C2.68 10.2 3 6.88 3 6c0-2.42 1.72-4.44 4.005-4.901a1 1 0 1 1 1.99 0A5 5 0 0 1 13 6c0 .88.32 4.2 1.22 6"/>
        </svg>
        <span>Watching</span>
    }
    else
    {
        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8 16a2 2 0 0 0 2-2H6a2 2 0 0 0 2 2M8 1.918l-.797.161A4 4 0 0 0 4 6c0 .628-.134 2.197-.459 3.742-.16.767-.376 1.566-.663 2.258h10.244c-.287-.692-.502-1.49-.663-2.258C12.134 8.197 12 6.628 12 6a4 4 0 0 0-3.203-3.92zM14.22 12c.223.447.481.801.78 1H1c.299-.199.557-.553.78-1C2.68 10.2 3 6.88 3 6c0-2.42 1.72-4.44 4.005-4.901a1 1 0 1 1 1.99 0A5 5 0 0 1 13 6c0 .88.32 4.2 1.22 6"/>
        </svg>
        <span>Watch</span>
    }
</button>
```

**Benefits:**

- Button swaps itself on click (no page reload)
- `xpc-loading-button` shows spinner automatically
- HTMX handles CSRF tokens
- Self-contained component

### 5. Update \_QAndAQuestionDetail to Use Button Partial

**File:** `_QAndAQuestionDetail.cshtml`

Replace Edit/Delete section (after line 31) to include watch button:

```razor
<div class="d-flex gap-1 justify-content-end">
    @if (User.Identity?.IsAuthenticated == true)
    {
        <partial name="~/Features/QAndA/_QAndAWatchButton.cshtml" model="@((Model.ID, Model.IsWatchingQuestion))" />
    }
    @if (Model.Permissions.Edit)
    {
        <button type="button" class="btn btn-sm btn-outline-secondary" hx-get hx-controller="QAndAQuestion"
            hx-action="DisplayEditQuestionForm" hx-route-questionID="@Model.ID" hx-swap="outerHTML"
            hx-target="#@wrapperElId">
            Edit Question
        </button>
    }
    @if (Model.Permissions.Delete)
    {
        <form method="post" hx-post hx-controller="QAndAQuestion" hx-action="DeleteQuestion"
            hx-confirm="Do you want to delete this question?" hx-route-questionID="@Model.ID">
            <button type="submit" class="btn btn-sm btn-outline-danger">
                Delete
            </button>
        </form>
    }
</div>
```

**Benefits:**

- Button swaps itself on click (no page reload)
- `xpc-loading-button` shows spinner automatically
- HTMX handles CSRF tokens
- Self-contained component

### 6. Update QAndAQuestionPageController Dependencies

**File:** `QAndAQuestionPageController.cs`

Add `QAndANotificationSettingsManager` to constructor if not present:

```csharp
public class QAndAQuestionPageController(
    // ... existing dependencies ...
    QAndANotificationSettingsManager notificationSettingsManager) : Controller
{
    // ... existing fields ...
    private readonly QAndANotificationSettingsManager notificationSettingsManager = notificationSettingsManager;
```

### 7. Update UpdateQuestionWatch Action to Return Partial

**File:** `QAndAQuestionController.cs`

Modify existing action to return the watch button partial instead of just OK:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[EnableRateLimiting(QAndARateLimitingConstants.UpdateQuestionWatch)]
public async Task<IActionResult> UpdateQuestionWatch(Guid questionID, [FromBody] bool isWatching)
{
    var member = await userManager.CurrentUser(HttpContext);
    if (member is null)
    {
        return Unauthorized();
    }

    var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>(
        [questionID],
        new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });
    if (questionPages.FirstOrDefault() is not QAndAQuestionPage questionPage)
    {
        return NotFound();
    }

    await (isWatching
       ? notificationSettingsManager.SetQuestionWatch(member.Id, questionPage)
       : notificationSettingsManager.SetQuestionUnWatch(member.Id, questionPage));

    // Return updated button partial for HTMX swap
    return PartialView("~/Features/QAndA/_QAndAWatchButton.cshtml", (questionID, isWatching));
}
```

## Implementation Order

1. Add `IsWatchingQuestion` property to `QAndAPostQuestionViewModel`
2. Add `QAndANotificationSettingsManager` dependency to
   `QAndAQuestionPageController` if missing
3. Update `MapQuestion` method to populate `IsWatchingQuestion` using
   `GetWatchedWebPageItemIDs`
4. Update `QAndAPostQuestionViewModel` constructor to accept `isWatching`
   parameter
5. Create `_QAndAWatchButton.cshtml` partial view with HTMX
6. Update `UpdateQuestionWatch` action to return button partial view
7. Add watch button to `_QAndAQuestionDetail.cshtml` (render partial)
8. Test button interaction and swap behavior

## Testing Checklist

- [ ] Watch button appears on question detail page for authenticated users
- [ ] Watch button does not appear for anonymous users
- [ ] Clicking watch button enables watching (bell icon changes state)
- [ ] Clicking unwatch button disables watching
- [ ] Button shows loading spinner during API call
- [ ] Button title updates after successful toggle
- [ ] Watched question appears in MyAccount notifications list
- [ ] Unwatched question removed from MyAccount notifications list
- [ ] Rate limiting works correctly
- [ ] Button state persists on page reload
- [ ] CSRF token validation works
- [ ] Read-only mode doesn't affect display (only backend)

## Notes

- The existing JavaScript in `notifications.js` uses a non-standard route
  pattern (`/qanda/question/{guid}/notified`)
- The existing controller action uses standard MVC routing
  (`/QAndAQuestion/UpdateQuestionWatch`)
- Use standard MVC routing
- Bell icon SVG is Bootstrap Icons standard bell design
- Add aria-labels for accessibility
- Spinner uses Bootstrap spinner component

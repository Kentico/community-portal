# Q&A Notifications Management Form - Implementation Plan

## Overview

Add a form to MyAccount.cshtml for managing watched Q&A questions and
notification frequency settings, following existing patterns in
\_BadgesForm.cshtml and \_ProfileForm.cshtml.

## 1. Create ViewModel

**File:** `AccountController.cs` (add to existing view models)

```csharp
public class QAndANotificationsFormViewModel
{
    public QAndANotificationFrequencyType SelectedFrequency { get; set; }
    public IReadOnlyList<WatchedQuestionViewModel> WatchedQuestions { get; set; } = [];
}

public class WatchedQuestionViewModel
{
    public int WebPageItemID { get; set; }
    public string Title { get; set; } = "";
    public string QuestionURL { get; set; } = "";
}
```

## 2. Update MyAccountViewModel

**File:** `AccountController.cs` (modify existing class)

Add property:

```csharp
public QAndANotificationsFormViewModel NotificationsForm { get; set; } = new();
```

## 3. Update MyAccount Action

**File:** `AccountController.cs` (modify existing action)

In `MyAccount()` action, populate the form:

```csharp
var watchedWebPageItemIDs = await notificationsManager.GetWatchedWebPageItemIDs(member);
var watchedQuestions = await contentRetriever.RetrievePages<QAndAQuestionPage>(
    RetrievePagesParameters.Default,
    q => q.Where(w => w.WhereIn(
        nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
        watchedWebPageItemIDs.ToArray())),
    RetrievalCacheSettings.CacheDisabled);

var frequency = await notificationsManager.GetMemberFrequency(member.Id);

vm.NotificationsForm = new()
{
    SelectedFrequency = frequency,
    WatchedQuestions = watchedQuestions.Select(q => new WatchedQuestionViewModel
    {
        WebPageItemID = q.SystemFields.WebPageItemID,
        Title = q.BasicItemTitle,
        QuestionURL = linkGenerator.GetUriByAction(
            HttpContext,
            "Index",
            "QAndAQuestionPage",
            new { guid = q.SystemFields.WebPageItemGUID }) ?? ""
    }).ToList()
};
```

## 4. Create Partial View

**File:** `_QAndANotificationsForm.cshtml`

Structure:

- HTMX form with hx-post to Account/UpdateQAndANotifications
- Frequency dropdown using `<select>` with enum values
- List of watched questions with unwatch buttons
- Each unwatch button uses hx-post to Account/UnwatchQuestion with question ID
- Submit button to save frequency
- Success alert on HTMX request completion
- xpc-readonly-disabled on fieldset

Pattern similar to \_BadgesForm.cshtml:

```razor
@model QAndANotificationsFormViewModel

<form id="qandaNotificationsForm" method="post"
    hx-post hx-controller="Account" hx-action="UpdateQAndANotifications"
    hx-swap="outerHTML" hx-disabled-elt="find fieldset">

    <fieldset hx-indicator="this" xpc-readonly-disabled>
        <h5>Q&A Discussion Notifications</h5>

        <div class="form-group">
            <label for="notificationFrequency" class="control-label form-label mt-3">
                Notification Frequency
            </label>
            <select id="notificationFrequency" name="SelectedFrequency" class="form-select">
                @foreach (var freq in Enum.GetValues<QAndANotificationFrequencyType>())
                {
                    <option value="@freq" selected="@(freq == Model.SelectedFrequency)">
                        @freq.GetDescription()
                    </option>
                }
            </select>
            <small class="form-text text-muted">
                Choose how often you want to receive email notifications for watched questions.
            </small>
        </div>

        <div class="form-group mt-3">
            <h6>Watched Questions (@Model.WatchedQuestions.Count)</h6>

            @if (Model.WatchedQuestions.Any())
            {
                <ul class="list-group">
                    @foreach (var q in Model.WatchedQuestions)
                    {
                        <li class="list-group-item d-flex justify-content-between align-items-center">
                            <a href="@q.QuestionURL" target="_blank">@q.Title</a>
                            <button type="button"
                                class="btn btn-sm btn-outline-danger"
                                hx-post hx-controller="Account"
                                hx-action="UnwatchQuestion"
                                hx-vals='@Json.Serialize(new { webPageItemID = q.WebPageItemID })'
                                hx-target="#qandaNotificationsForm"
                                hx-swap="outerHTML">
                                Unwatch
                            </button>
                        </li>
                    }
                </ul>
            }
            else
            {
                <p class="text-muted">You are not watching any questions.</p>
            }
        </div>

        <button type="submit" class="btn btn-primary mt-3" xpc-loading-button>
            Update notification settings
        </button>

        <vc:read-only-mode-notification />
    </fieldset>

    @if (Context.Request.IsHtmx() && ViewContext.ModelState.IsValid)
    {
        <xpc-alert dismissable="true">
            Notification settings updated.
        </xpc-alert>
    }
</form>
```

## 5. Add Controller Actions

**File:** `AccountController.cs`

### UpdateQAndANotifications Action

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<ActionResult> UpdateQAndANotifications(QAndANotificationsFormViewModel model)
{
    if (readOnlyProvider.IsReadOnly)
    {
        return StatusCode(503);
    }

    var member = await userManager.GetUserAsync(User);
    if (member is null)
    {
        return Unauthorized();
    }

    await notificationsManager.SetMemberFrequency(member.Id, model.SelectedFrequency);

    // Reload the form with updated data
    var watchedWebPageItemIDs = await notificationsManager.GetWatchedWebPageItemIDs(member);
    var watchedQuestions = await contentRetriever.RetrievePages<QAndAQuestionPage>(
        RetrievePagesParameters.Default,
        q => q.Where(w => w.WhereIn(
            nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
            watchedWebPageItemIDs.ToArray())),
        RetrievalCacheSettings.CacheDisabled);

    var updatedModel = new QAndANotificationsFormViewModel
    {
        SelectedFrequency = model.SelectedFrequency,
        WatchedQuestions = watchedQuestions.Select(q => new WatchedQuestionViewModel
        {
            WebPageItemID = q.SystemFields.WebPageItemID,
            Title = q.BasicItemTitle,
            QuestionURL = linkGenerator.GetUriByAction(
                HttpContext,
                "Index",
                "QAndAQuestionPage",
                new { guid = q.SystemFields.WebPageItemGUID }) ?? ""
        }).ToList()
    };

    return PartialView("~/Features/Accounts/_QAndANotificationsForm.cshtml", updatedModel);
}
```

### UnwatchQuestion Action

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<ActionResult> UnwatchQuestion([FromBody] int webPageItemID)
{
    if (readOnlyProvider.IsReadOnly)
    {
        return StatusCode(503);
    }

    var member = await userManager.GetUserAsync(User);
    if (member is null)
    {
        return Unauthorized();
    }

    var questionPages = await contentRetriever.RetrievePages<QAndAQuestionPage>(
        RetrievePagesParameters.Default,
        q => q.Where(w => w.WhereEquals(
            nameof(QAndAQuestionPage.SystemFields.WebPageItemID),
            webPageItemID)),
        RetrievalCacheSettings.CacheDisabled);

    if (questionPages.FirstOrDefault() is QAndAQuestionPage questionPage)
    {
        await notificationsManager.SetQuestionUnWatch(member.Id, questionPage);
    }

    // Return updated form
    return await UpdateQAndANotifications(new QAndANotificationsFormViewModel
    {
        SelectedFrequency = await notificationsManager.GetMemberFrequency(member.Id)
    });
}
```

## 6. Update MyAccount.cshtml

**File:** `MyAccount.cshtml`

Add new grid column after badges or password sections:

```razor
<div class="g-col-12 g-col-lg-6">
    <partial name="~/Features/Accounts/_QAndANotificationsForm.cshtml" model="Model.NotificationsForm" />
</div>
```

## 7. Add Required Dependencies

**File:** `AccountController.cs` (constructor)

Already has:

- `QAndANotificationSettingsManager notificationsManager` ✓

Need to add:

- `IContentRetriever contentRetriever`

## 8. Helper Extension Method (Optional)

**File:** Create `QAndANotificationFrequencyTypeExtensions.cs` in
Features/QAndA/Notifications

```csharp
public static class QAndANotificationFrequencyTypeExtensions
{
    public static string GetDescription(this QAndANotificationFrequencyType frequency)
    {
        var field = frequency.GetType().GetField(frequency.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? frequency.ToString();
    }
}
```

## Implementation Order

1. Add view models to AccountController.cs
2. Add IContentRetriever to AccountController constructor
3. Update MyAccount() action to populate NotificationsForm
4. Create \_QAndANotificationsForm.cshtml partial view
5. Add UpdateQAndANotifications action to controller
6. Add UnwatchQuestion action to controller
7. Add partial to MyAccount.cshtml grid
8. Test form submission and unwatch functionality
9. Add extension method for enum descriptions if needed

## Testing Checklist

- [ ] Form loads with correct frequency selected
- [ ] Watched questions list displays correctly
- [ ] Frequency dropdown updates successfully
- [ ] Unwatch button removes question from list
- [ ] HTMX swaps work correctly
- [ ] Success alerts display properly
- [ ] Read-only mode disables form correctly
- [ ] Links to questions work
- [ ] No watched questions shows appropriate message

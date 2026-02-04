# Implementation Plan: Question Upvote Feature

## Overview

Add a member-facing upvote feature for Q&A questions. Members click a badge to
toggle upvotes on/off. Badge displays total count with tooltip showing all
members who upvoted.

## Architecture Summary

- **View Model**: `QAndAPostQuestionViewModel` + reaction metadata properties
- **UI**: Badge + HTMX form in question detail section of
  `QAndAQuestionPageDetail.cshtml`
- **Controller**: `ToggleQuestionReaction()` action in `QAndAQuestionController`
- **Database**: New `QAndAQuestionReactionInfo` binding table (auto-generated)
- **Rate Limiting**: New `UpdateQuestionReaction` policy (10 per minute)

---

## Key Differences from Answer Reactions

### Foreign Key Strategy

- **Answers**: `QAndAAnswerReactionAnswerID` →
  `QAndAAnswerDataInfo.QAndAAnswerDataID` (simple int)
- **Questions**: `QAndAQuestionReactionWebPageItemID` →
  `QAndAQuestionPage.SystemFields.WebPageItemID` (page item int)
- **Impact**: Question validation uses `IContentRetriever` instead of entity
  query

### Permission Service Additions

- **Answers**: `QAndAAnswerPermissions.CanReact` (can't react to own answer)
- **Questions**: `QAndAQuestionPermissions.CanReact` (can't react to own
  question)
- **Difference**: Question author check uses `QAndAQuestionPageAuthorMemberID`

### View Model Nesting

- **Answers**: Reaction data passed directly to answer detail partial
- **Questions**: Reaction data added to question VM in question detail section

---

## Implementation Steps

### 1. ✅ COMPLETED: Database Table Auto-Generation

**File**:
`src/Kentico.Community.Portal.Core/Modules/QAndAQuestionReaction/QAndAQuestionReactionInfo.generated.cs`

**Status**: ✅ Already generated and attached

**Verification**:

- ✅ `QAndAQuestionReactionID` (PK)
- ✅ `QAndAQuestionReactionGUID`
- ✅ `QAndAQuestionReactionWebPageItemID` (FK to page item)
- ✅ `QAndAQuestionReactionMemberID` (FK to member)
- ✅ `QAndAQuestionReactionType` (string for "upvote")
- ✅ `QAndAQuestionReactionDateModified` (timestamp)

**Next Step**: Create `.xml` CI repository files (see step 3)

---

### 2. Create Enum File (NEW)

**File**:
`src/Kentico.Community.Portal.Core/Modules/QAndAQuestionReaction/QAndAQuestionReactionInfo.cs`

**Content**:

```csharp
namespace Kentico.Community.Portal.Core.Modules;

public enum DiscussionReactionTypes
{
    Upvote
}
```

**Purpose**: Mirrors answer reactions enum; enables type-safe reaction constants

**Status**: ⏳ NOT STARTED

---

### 3. Create CI Repository Configuration Files (NEW)

#### A. Class Definition

**File**:
`src/Kentico.Community.Portal.Web/App_Data/CIRepository/@global/cms.class/kenticocommunity.qandaquestionreaction.xml`

**Content** (based on answer reaction class):

```xml
<?xml version="1.0" encoding="utf-8"?>
<cms.class>
  <ClassDisplayName>
    <![CDATA[Q&A Question Reactions]]>
  </ClassDisplayName>
  <ClassFormDefinition>
    <form>
      <field column="QAndAQuestionReactionID" columntype="integer" enabled="true" guid="97e40e9a-0d67-448c-a92a-2fd509e1620d" isPK="true" />
      <field column="QAndAQuestionReactionGUID" columnprecision="0" columntype="guid" enabled="true" guid="d40fa9c6-b461-4db1-b264-4f5f39c5f913" />
      <field column="QAndAQuestionReactionWebPageItemID" columnprecision="0" columntype="integer" enabled="true" guid="85163750-5b52-4865-a9f3-b17fe2e3e5f1">
        <properties>
          <defaultvalue>0</defaultvalue>
        </properties>
      </field>
      <field column="QAndAQuestionReactionMemberID" columnprecision="0" columntype="integer" enabled="true" guid="072cbc0c-b665-4b2e-90e7-9a54c5c7b442" refobjtype="cms.member" reftype="Binding" />
      <field column="QAndAQuestionReactionType" columnprecision="0" columnsize="20" columntype="text" enabled="true" guid="213094ca-6465-40c4-b8b5-4513a2dd20a0" />
      <field column="QAndAQuestionReactionDateModified" columnprecision="3" columntype="datetime" enabled="true" guid="724f81db-5a45-48e3-bda5-8a836c8b2917" />
    </form>
  </ClassFormDefinition>
  <ClassGUID>362ad51d-d743-4304-9428-677e0934d024</ClassGUID>
  <ClassHasUnmanagedDbSchema>False</ClassHasUnmanagedDbSchema>
  <ClassName>KenticoCommunity.QAndAQuestionReaction</ClassName>
  <ClassResourceID>
    <CodeName>KenticoCommunity</CodeName>
    <GUID>5e10e4ec-a038-4b97-a9f6-c17724896c89</GUID>
    <ObjectType>cms.resource</ObjectType>
  </ClassResourceID>
  <ClassTableName>KenticoCommunity_QAndAQuestionReaction</ClassTableName>
  <ClassType>Other</ClassType>
</cms.class>
```

**Status**: ⏳ NOT STARTED

#### B. Create Form (Alternative Form)

**File**:
`src/Kentico.Community.Portal.Web/App_Data/CIRepository/@global/cms.alternativeform/cms.class_kenticocommunity.qandaquestionreaction/create.xml`

**Content**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<cms.alternativeform>
  <FormClassID>
    <CodeName>KenticoCommunity.QAndAQuestionReaction</CodeName>
    <GUID>362ad51d-d743-4304-9428-677e0934d024</GUID>
    <ObjectType>cms.class</ObjectType>
  </FormClassID>
  <FormDefinition>
    <form>
      <field column="QAndAQuestionReactionID" guid="97e40e9a-0d67-448c-a92a-2fd509e1620d" enabled="" />
      <field column="QAndAQuestionReactionGUID" guid="d40fa9c6-b461-4db1-b264-4f5f39c5f913" enabled="" />
      <field column="QAndAQuestionReactionDateModified" guid="724f81db-5a45-48e3-bda5-8a836c8b2917" enabled="" order="2" />
      <field column="QAndAQuestionReactionWebPageItemID" enabled="true" guid="85163750-5b52-4865-a9f3-b17fe2e3e5f1" visible="true" order="3">
        <settings>
          <controlname>Kentico.Administration.SingleObjectIdSelector</controlname>
          <ObjectType>Kentico.Content.Web.Mvc.Routing.PageContentType</ObjectType>
        </settings>
        <properties>
          <explanationtextashtml>False</explanationtextashtml>
          <fieldcaption>Question</fieldcaption>
          <fielddescriptionashtml>False</fielddescriptionashtml>
        </properties>
      </field>
      <field column="QAndAQuestionReactionMemberID" enabled="true" guid="072cbc0c-b665-4b2e-90e7-9a54c5c7b442" visible="true" order="4">
        <settings>
          <controlname>Kentico.Administration.SingleObjectIdSelector</controlname>
          <ObjectType>cms.member</ObjectType>
        </settings>
        <properties>
          <defaultvalue>0</defaultvalue>
          <explanationtextashtml>False</explanationtextashtml>
          <fieldcaption>Member</fieldcaption>
          <fielddescriptionashtml>False</fielddescriptionashtml>
        </properties>
      </field>
      <field column="QAndAQuestionReactionType" enabled="true" guid="29fb31ea-5a95-45a0-9b15-4ae4b41c6545" visible="true" order="5">
        <settings>
          <controlname>Kentico.Administration.DropDownSelector</controlname>
          <Options>Upvote</Options>
          <OptionsValueSeparator>;</OptionsValueSeparator>
        </settings>
        <properties>
          <explanationtextashtml>False</explanationtextashtml>
          <fieldcaption>Type</fieldcaption>
          <fielddescriptionashtml>False</fielddescriptionashtml>
        </properties>
      </field>
    </form>
  </FormDefinition>
  <FormDisplayName>Create</FormDisplayName>
  <FormGUID>30788050-3c63-4fb8-80b9-ad4fa9b36f3c</FormGUID>
  <FormIsCustom>True</FormIsCustom>
  <FormName>Create</FormName>
</cms.alternativeform>
```

**Status**: ⏳ NOT STARTED

#### C. Edit Form (Alternative Form)

**File**:
`src/Kentico.Community.Portal.Web/App_Data/CIRepository/@global/cms.alternativeform/cms.class_kenticocommunity.qandaquestionreaction/edit.xml`

**Content**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<cms.alternativeform>
  <FormClassID>
    <CodeName>KenticoCommunity.QAndAQuestionReaction</CodeName>
    <GUID>362ad51d-d743-4304-9428-677e0934d024</GUID>
    <ObjectType>cms.class</ObjectType>
  </FormClassID>
  <FormDefinition>
    <form>
      <field column="QAndAQuestionReactionID" guid="97e40e9a-0d67-448c-a92a-2fd509e1620d" enabled="" />
      <field column="QAndAQuestionReactionGUID" guid="d40fa9c6-b461-4db1-b264-4f5f39c5f913" enabled="" />
      <field column="QAndAQuestionReactionWebPageItemID" guid="85163750-5b52-4865-a9f3-b17fe2e3e5f1" enabled="" />
      <field column="QAndAQuestionReactionMemberID" guid="072cbc0c-b665-4b2e-90e7-9a54c5c7b442" enabled="" />
      <field column="QAndAQuestionReactionDateModified" guid="724f81db-5a45-48e3-bda5-8a836c8b2917" enabled="" order="4" />
      <field column="QAndAQuestionReactionType" enabled="true" guid="8c261c45-6688-4098-8e1a-1e7fe3b2901f" visible="true" order="5">
        <settings>
          <controlname>Kentico.Administration.DropDownSelector</controlname>
          <Options>Upvote</Options>
          <OptionsValueSeparator>;</OptionsValueSeparator>
        </settings>
        <properties>
          <explanationtextashtml>False</explanationtextashtml>
          <fieldcaption>Type</fieldcaption>
          <fielddescriptionashtml>False</fielddescriptionashtml>
        </properties>
      </field>
    </form>
  </FormDefinition>
  <FormDisplayName>Edit</FormDisplayName>
  <FormGUID>3ef2ee06-ffd5-40e2-99ab-6898ee475b52</FormGUID>
  <FormIsCustom>True</FormIsCustom>
  <FormName>Edit</FormName>
</cms.alternativeform>
```

**Status**: ⏳ NOT STARTED

---

### 4. Update Permissions Model (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAPermissionService.cs`

**Changes**:

Add `bool CanReact` property to `QAndAQuestionPermissions` record:

```csharp
public record QAndAQuestionPermissions(bool Edit, bool Delete, bool Answer, bool CanReact)
{
    public static QAndAQuestionPermissions NoPermissions { get; } = new(false, false, false, false);
    public bool CanInteract => Edit || Delete || Answer || CanReact;
}
```

Update
`GetPermissions(CommunityMember? communityMember, QAndAQuestionPage question)`
method:

```csharp
public async Task<QAndAQuestionPermissions> GetPermissions(CommunityMember? communityMember, QAndAQuestionPage question)
{
    if (communityMember is null)
    {
        return QAndAQuestionPermissions.NoPermissions;
    }

    bool edit = await HasPermission(communityMember, question, QAndAQuestionPermissionType.Edit);
    bool delete = await HasPermission(communityMember, question, QAndAQuestionPermissionType.Delete);
    bool answer = await HasPermission(communityMember, question, QAndAQuestionPermissionType.SubmitAnswer);
    bool canReact = question.QAndAQuestionPageAuthorMemberID != communityMember.Id;  // Can't react to own question

    return new QAndAQuestionPermissions(edit, delete, answer, canReact);
}
```

**Status**: ⏳ NOT STARTED

---

### 5. Extend QAndAPostQuestionViewModel (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs`

Add properties to `QAndAPostQuestionViewModel`:

```csharp
public class QAndAPostQuestionViewModel
{
    // ... existing properties ...

    public int ReactionCount { get; set; }
    public bool CurrentMemberHasReacted { get; set; }
    public List<string> MembersWhoReacted { get; set; } = [];
}
```

**Status**: ⏳ NOT STARTED

---

### 6. Add Rate Limiting Constant (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndARateLimitingConstants.cs`

Add constant:

```csharp
/// <summary>
/// Rate limiting policy for toggling question reactions (upvotes)
/// </summary>
public const string UpdateQuestionReaction = "QAndA_UpdateQuestionReaction";
```

**Status**: ⏳ NOT STARTED

---

### 7. Register Rate Limiting Policy (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionMvcExtensions.cs`

Add policy to `AddRateLimiter()` configuration:

```csharp
.AddPolicy(
    QAndARateLimitingConstants.UpdateQuestionReaction,
    httpContext => RateLimitingUtilities.CreateSlidingWindowPartition(
        httpContext,
        permitLimit: 10,
        segmentsPerWindow: 10,
        window: TimeSpan.FromMinutes(1)
    )
)
```

**Location**: Near existing `UpdateAnswerReaction` policy registration

**Status**: ⏳ NOT STARTED

---

### 8. Create MediatR Commands & Handlers (NEW)

#### A. Create Reaction Command

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAQuestionReactionCreateCommand.cs`

```csharp
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionReactionCreateCommand(
    int MemberId,
    int QuestionWebPageItemId,
    string ReactionType = "upvote")
    : ICommand<Result<int>>;

public class QAndAQuestionReactionCreateCommandHandler(
    DataItemCommandTools tools,
    TimeProvider clock,
    IInfoProvider<QAndAQuestionReactionInfo> provider)
    : DataItemCommandHandler<QAndAQuestionReactionCreateCommand, Result<int>>(tools)
{
    private readonly TimeProvider clock = clock;
    private readonly IInfoProvider<QAndAQuestionReactionInfo> provider = provider;

    public override async Task<Result<int>> Handle(
        QAndAQuestionReactionCreateCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().DateTime;

        var reaction = new QAndAQuestionReactionInfo()
        {
            QAndAQuestionReactionGUID = Guid.NewGuid(),
            QAndAQuestionReactionMemberID = request.MemberId,
            QAndAQuestionReactionWebPageItemID = request.QuestionWebPageItemId,
            QAndAQuestionReactionType = request.ReactionType,
            QAndAQuestionReactionDateModified = now
        };

        try
        {
            await provider.SetAsync(reaction);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>(
                $"Could not create reaction for question [{request.QuestionWebPageItemId}]: {ex}");
        }

        return Result.Success(reaction.QAndAQuestionReactionID);
    }
}
```

**Status**: ⏳ NOT STARTED

#### B. Delete Reaction Command

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAQuestionReactionDeleteCommand.cs`

```csharp
using CMS.DataEngine;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QAndAQuestionReactionDeleteCommand(
    int MemberId,
    int QuestionWebPageItemId,
    string ReactionType = "upvote")
    : ICommand<Result>;

public class QAndAQuestionReactionDeleteCommandHandler(
    DataItemCommandTools tools,
    IInfoProvider<QAndAQuestionReactionInfo> provider)
    : DataItemCommandHandler<QAndAQuestionReactionDeleteCommand, Result>(tools)
{
    private readonly IInfoProvider<QAndAQuestionReactionInfo> provider = provider;

    public override async Task<Result> Handle(
        QAndAQuestionReactionDeleteCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var reaction = await provider.Get()
                .WhereEquals(
                    nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionMemberID),
                    request.MemberId)
                .WhereEquals(
                    nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionWebPageItemID),
                    request.QuestionWebPageItemId)
                .WhereEquals(
                    nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionType),
                    request.ReactionType)
                .FirstOrDefaultAsync(cancellationToken);

            if (reaction is not null)
            {
                await provider.DeleteAsync(reaction);
            }
        }
        catch (Exception ex)
        {
            return Result.Failure(
                $"Could not delete reaction for question [{request.QuestionWebPageItemId}] " +
                $"and member [{request.MemberId}]: {ex}");
        }

        return Result.Success();
    }
}
```

**Status**: ⏳ NOT STARTED

---

### 9. Create Query Handler for Reaction Data (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAQuestionReactionsQuery.cs`

```csharp
using CMS.DataEngine;
using CMS.Membership;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Community.Portal.Core.Operations;
using Kentico.Community.Portal.Web.Membership;

namespace Kentico.Community.Portal.Web.Features.QAndA;

public record QuestionReactionsData(
    int TotalCount,
    bool CurrentMemberHasReacted,
    List<string> MembersWhoReacted);

public record QAndAQuestionReactionsQuery(
    int QuestionWebPageItemId,
    int? CurrentMemberId = null)
    : IQuery<QuestionReactionsData>;

public class QAndAQuestionReactionsQueryHandler(
    DataItemQueryTools tools,
    IInfoProvider<QAndAQuestionReactionInfo> reactionProvider,
    IInfoProvider<MemberInfo> memberProvider)
    : DataItemQueryHandler<QAndAQuestionReactionsQuery, QuestionReactionsData>(tools)
{
    private readonly IInfoProvider<QAndAQuestionReactionInfo> reactionProvider = reactionProvider;
    private readonly IInfoProvider<MemberInfo> memberProvider = memberProvider;

    public override async Task<QuestionReactionsData> Handle(
        QAndAQuestionReactionsQuery request,
        CancellationToken cancellationToken = default)
    {
        // Get all upvote reactions for this question
        var reactions = await reactionProvider.Get()
            .WhereEquals(
                nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionWebPageItemID),
                request.QuestionWebPageItemId)
            .OrderBy(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionID))
            .GetEnumerableTypedResultAsync();

        int totalCount = reactions.Count();
        bool currentMemberHasReacted = request.CurrentMemberId.HasValue
            && reactions.Any(r => r.QAndAQuestionReactionMemberID == request.CurrentMemberId);

        var members = (await memberProvider.Get()
            .WhereIn(
                nameof(MemberInfo.MemberID),
                reactions.Select(r => r.QAndAQuestionReactionMemberID).Distinct())
            .GetEnumerableTypedResultAsync())
            .Select(CommunityMember.FromMemberInfo)
            .ToDictionary(m => m.Id);

        var memberDisplayNames = reactions
            .Select(r => r.QAndAQuestionReactionMemberID)
            .Distinct()
            .Select(mId => members.TryGetValue(mId, out var m) ? m.DisplayName : "")
            .ToList();

        return new QuestionReactionsData(
            totalCount,
            currentMemberHasReacted,
            memberDisplayNames);
    }

    protected override ICacheDependencyKeysBuilder AddDependencyKeys(
        QAndAQuestionReactionsQuery query,
        QuestionReactionsData result,
        ICacheDependencyKeysBuilder builder) =>
        builder.AllObjects(QAndAQuestionReactionInfo.OBJECT_TYPE);
}
```

**Status**: ⏳ NOT STARTED

---

### 10. Implement ToggleQuestionReaction Controller Action (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/Components/Form/QAndAQuestionController.cs`

**Action Signature**:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[EnableRateLimiting(QAndARateLimitingConstants.UpdateQuestionReaction)]
public async Task<ActionResult> ToggleQuestionReaction(Guid questionID)
{
    if (readOnlyProvider.IsReadOnly)
    {
        return StatusCode(503);
    }

    var member = await userManager.CurrentUser(HttpContext);
    if (member is null)
    {
        return Unauthorized();
    }

    var questionPages = await contentRetriever.RetrievePagesByGuids<QAndAQuestionPage>(
        [questionID],
        new RetrievePagesParameters { LinkedItemsMaxLevel = 1 });

    if (questionPages.FirstOrDefault() is not QAndAQuestionPage question)
    {
        return NotFound();
    }

    // Check if member has already reacted
    var reactionData = await mediator.Send(
        new QAndAQuestionReactionsQuery(question.SystemFields.WebPageItemID, member.Id));

    if (reactionData.CurrentMemberHasReacted)
    {
        // Delete the reaction (un-upvote)
        await mediator.Send(
            new QAndAQuestionReactionDeleteCommand(
                member.Id,
                question.SystemFields.WebPageItemID));
    }
    else
    {
        // Create the reaction (upvote)
        await mediator.Send(
            new QAndAQuestionReactionCreateCommand(
                member.Id,
                question.SystemFields.WebPageItemID));
    }

    // Redirect to question page to refresh with updated reaction data
    return Redirect(question.SystemFields.WebPageUrl);
}
```

**Location**: Add to existing `QAndAQuestionController` or create if needed

**Dependencies**:

- `IMediator mediator`
- `UserManager<CommunityMember> userManager`
- `IContentRetriever contentRetriever`
- `IReadOnlyModeProvider readOnlyProvider`

**Status**: ⏳ NOT STARTED

---

### 11. Update Question Page Controller/View Model Builder (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs`

**Changes in MapQuestion method**:

```csharp
private async Task<QAndAPostQuestionViewModel> MapQuestion(
    QAndAQuestionPage question,
    CommunityMember? currentMember,
    IQAndAPermissionService permissionService)
{
    var permissions = await permissionService.GetPermissions(currentMember, question);

    var author = await GetAuthor(question.QAndAQuestionPageAuthorMemberID);
    var taxonomiesResp = await mediator.Send(
        new GetPageTaxonomiesQuery(question.SystemFields.WebPageItemGUID));

    // NEW: Query reaction data for question
    var reactionData = await mediator.Send(
        new QAndAQuestionReactionsQuery(
            question.SystemFields.WebPageItemID,
            currentMember?.Id));

    var isSubscribed = currentMember is not null
        && await notificationSettingsManager.IsMemberSubscribedAsync(
            currentMember.Id,
            question.SystemFields.WebPageItemGUID);

    return new QAndAPostQuestionViewModel(
        question,
        permissions,
        currentMember,
        taxonomiesResp,
        author,
        markdownRenderer,
        isSubscribed)
    {
        // NEW: Populate reaction properties
        ReactionCount = reactionData.TotalCount,
        CurrentMemberHasReacted = reactionData.CurrentMemberHasReacted,
        MembersWhoReacted = reactionData.MembersWhoReacted
    };
}
```

**Status**: ⏳ NOT STARTED

---

### 12. Update Question Detail View - Add Upvote Badge (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/_QAndAQuestionDetail.cshtml`
(or similar question detail partial)

**Location**: Near question title/metadata, before action buttons (Edit, Delete)

**Add partial**:

```html
<partial name="~/Features/QAndA/_QAndAQuestionReaction.cshtml"
    model="@(new QAndAQuestionReactionViewModel(
        Model.ID,
        new(Model.ReactionCount, Model.CurrentMemberHasReacted, Model.MembersWhoReacted),
        Model.Permissions))" />
```

**Status**: ⏳ NOT STARTED

---

### 13. Create Question Reaction View Model (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs`

Add new class:

```csharp
public class QAndAQuestionReactionViewModel
{
    public Guid QuestionID { get; }
    public bool CurrentMemberHasReacted { get; }
    public int ReactionCount { get; }
    public IReadOnlyList<string> MembersWhoReacted { get; } = [];
    public QAndAQuestionPermissions Permissions { get; set; } = QAndAQuestionPermissions.NoPermissions;

    public static QAndAQuestionReactionViewModel Empty { get; } = new();

    public QAndAQuestionReactionViewModel(
        Guid questionID,
        QuestionReactionsData data,
        QAndAQuestionPermissions permissions)
    {
        QuestionID = questionID;
        CurrentMemberHasReacted = data.CurrentMemberHasReacted;
        ReactionCount = data.TotalCount;
        MembersWhoReacted = data.MembersWhoReacted;
        Permissions = permissions;
    }

    private QAndAQuestionReactionViewModel() { }
}
```

**Status**: ⏳ NOT STARTED

---

### 14. Create Question Reaction View Partial (NEW)

**File**:
`src/Kentico.Community.Portal.Web/Features/QAndA/_QAndAQuestionReaction.cshtml`

**Content**:

```html
@using Kentico.Community.Portal.Web.Features.QAndA

@model QAndAQuestionReactionViewModel

@{
    string reactionFormId = $"questionReactionForm_{Model.QuestionID:N}";
    var toolTipAttrs = Html.Raw(
        Model.ReactionCount > 0
            ? $"data-bs-toggle='tooltip' data-bs-delay='1000' data-bs-placement='top' " +
              $"data-bs-title='Upvoted by: {Html.Encode(string.Join(", ", Model.MembersWhoReacted))}'"
            : "");
}

@if (Model.Permissions.CanReact)
{
    <form id="@reactionFormId" method="post" hx-post hx-controller="QAndAQuestion"
        hx-action="ToggleQuestionReaction" hx-route-questionID="@Model.QuestionID"
        hx-swap="outerHTML" hx-target="#@reactionFormId">
        <fieldset xpc-readonly-disabled>
            <button type="submit"
                class="btn btn-sm @(Model.CurrentMemberHasReacted ? "btn-primary" : "btn-outline-primary")"
                @toolTipAttrs>
                <svg aria-hidden="true" height="16" viewBox="0 0 16 16" version="1.1" width="16" fill="currentColor">
                    <path d="M3.47 7.78a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0l4.25 4.25a.751.751 0 0 1-.018 1.042.751.751 0 0 1-1.042.018L9 4.81v7.44a.75.75 0 0 1-1.5 0V4.81L4.53 7.78a.75.75 0 0 1-1.06 0Z">
                    </path>
                </svg>
                @Model.ReactionCount
            </button>
        </fieldset>
    </form>
}
else
{
    <span class="badge rounded-pill text-bg-light" @(toolTipAttrs) disabled>
        <svg aria-hidden="true" height="16" viewBox="0 0 16 16" version="1.1" width="16">
            <path d="M3.47 7.78a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0l4.25 4.25a.751.751 0 0 1-.018 1.042.751.751 0 0 1-1.042.018L9 4.81v7.44a.75.75 0 0 1-1.5 0V4.81L4.53 7.78a.75.75 0 0 1-1.06 0Z">
            </path>
        </svg>
        <span class="px-1">@Model.ReactionCount</span>
    </span>
}
```

**Status**: ⏳ NOT STARTED

---

### 15. Optional: Admin UI - List and Manage Question Reactions (FUTURE)

**Status**: ⏳ NOT STARTED (can be implemented after basic feature is working)

**Scope**: Create admin pages for viewing, filtering, and deleting question
reactions

**Files to Create**:

- `QAndAQuestionReactionAdminController.cs`
- `ReactionAdminListViewModel.cs`
- `ReactionsList.cshtml`
- `ListReactionsAdminQuery.cs`
- `DeleteReactionAdminCommand.cs`

**Note**: Can be unified with answer reactions admin UI via UNION query

---

## File Summary

### Modified Files

- `src/Kentico.Community.Portal.Web/Features/QAndA/QAndAPermissionService.cs` -
  Add `CanReact` to `QAndAQuestionPermissions`
- `src/Kentico.Community.Portal.Web/Features/QAndA/QAndAQuestionPageController.cs` -
  Add reaction properties + query handler call
- `src/Kentico.Community.Portal.Web/Features/QAndA/QAndARateLimitingConstants.cs` -
  Add `UpdateQuestionReaction` constant
- `src/Kentico.Community.Portal.Web/Configuration/ServiceCollectionMvcExtensions.cs` -
  Register rate limiting policy
- `src/Kentico.Community.Portal.Web/Features/QAndA/_QAndAQuestionDetail.cshtml` -
  Add reaction partial reference

### New Files - Core

- `src/Kentico.Community.Portal.Core/Modules/QAndAQuestionReaction/QAndAQuestionReactionInfo.cs` -
  Enum file

### New Files - Web Operations

- `src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAQuestionReactionCreateCommand.cs`
- `src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAQuestionReactionDeleteCommand.cs`
- `src/Kentico.Community.Portal.Web/Features/QAndA/Operations/QAndAQuestionReactionsQuery.cs`

### New Files - Web Controller/Views

- `src/Kentico.Community.Portal.Web/Features/QAndA/Components/Form/QAndAQuestionController.cs` -
  Add action (or extend existing)
- `src/Kentico.Community.Portal.Web/Features/QAndA/_QAndAQuestionReaction.cshtml` -
  Reaction view partial

### New Files - CI Repository

- `src/Kentico.Community.Portal.Web/App_Data/CIRepository/@global/cms.class/kenticocommunity.qandaquestionreaction.xml`
- `src/Kentico.Community.Portal.Web/App_Data/CIRepository/@global/cms.alternativeform/cms.class_kenticocommunity.qandaquestionreaction/create.xml`
- `src/Kentico.Community.Portal.Web/App_Data/CIRepository/@global/cms.alternativeform/cms.class_kenticocommunity.qandaquestionreaction/edit.xml`

### Auto-Generated (Already Exists)

- `src/Kentico.Community.Portal.Core/Modules/QAndAQuestionReaction/QAndAQuestionReactionInfo.generated.cs`
  ✅ COMPLETED

---

## Implementation Order (Recommended Sequence)

1. ✅ Create enum file (step 2)
2. ✅ Create CI repository XML files (step 3)
3. ✅ Update permissions model (step 4)
4. ✅ Add rate limiting constant (step 6)
5. ✅ Register rate limiting policy (step 7)
6. ✅ Create MediatR commands & handlers (step 8)
7. ✅ Create query handler (step 9)
8. ✅ Create view models (steps 5, 13)
9. ✅ Implement controller action (step 10)
10. ✅ Update question page controller (step 11)
11. ✅ Create view partials (step 14)
12. ✅ Update question detail view (step 12)
13. ⏳ Admin UI (optional, step 15)

---

## Architecture Decisions

### 1. Foreign Key to Page Item vs. Separate FK

**Decision**: Use `QAndAQuestionReactionWebPageItemID` to `WebPageItemID`

- **Why**: Matches Xperience pattern for content pages
- **Trade-off**: Requires page item ID in queries (not as simple as answer ID)
- **Benefit**: Leverages existing page content infrastructure

### 2. Reaction Type Field

**Decision**: Keep `QAndAQuestionReactionType` as string for future emoji
reactions

- **Why**: Same pattern as answers; enables multiple reaction types
- **Current**: Only "upvote" supported
- **Future**: Easy to extend (like/love/celebrate emojis)

### 3. No Duplicate Reactions

**Decision**: Unique constraint on (MemberID, QuestionID, Type)

- **Why**: Prevents accidental duplicates; toggle pattern relies on this
- **Implementation**: Query check in delete handler; FK prevents data corruption

### 4. Cascading Deletes

**Decision**: Use explicit Foreign Key constraints in database

- **Why**: Automatic cleanup when question or member deleted
- **Benefit**: No orphaned reactions possible

### 5. Reaction Data Structure

**Decision**: Use `QuestionReactionsData` record matching answer pattern

- `TotalCount`: Number of upvotes
- `CurrentMemberHasReacted`: Boolean flag
- `MembersWhoReacted`: List of display names for tooltip

**Benefit**: Code parity with answer reactions; easy to unify later

---

## Testing Checklist

- [ ] Toggle upvote as authenticated user → reaction created, redirects to
      question
- [ ] Click again → reaction deleted, redirects to question
- [ ] Tooltip shows correct member names
- [ ] Rate limiting enforced (>10 per minute blocked)
- [ ] Anonymous/unauthorized users → badge hidden
- [ ] Read-only mode → return 503 on attempt
- [ ] Deleting question → cascades delete reactions (verify FK)
- [ ] Deleting member → cascades delete reactions (verify FK)
- [ ] Reaction count updates correctly after toggle
- [ ] Member display names handle null/empty first/last names gracefully
- [ ] Both questions and answers can be reacted to independently
- [ ] Can't react to own question (permission check)

---

## Performance Considerations

### Database Indexes

- Index on `QAndAQuestionReactionWebPageItemID` (for filtering by question)
- Index on `QAndAQuestionReactionMemberID` (for filtering by member)
- Index on `QAndAQuestionReactionDateModified` (for date range filters)
- Composite: `(QAndAQuestionReactionType, QAndAQuestionReactionDateModified)`

### Query Optimization

- Use `.Columns()` to select only needed fields
- Load member data in single query (no N+1)
- Cache dependency: Only invalidate on `QAndAQuestionReactionInfo` changes

### Caching

- Question reactions cached per page item ID
- Cache invalidated when:
  - New reaction created
  - Reaction deleted
  - Member info changed

---

## Considerations for Future Expansion

### Multiple Reaction Types (Emoji Reactions)

- Schema already supports via `ReactionType` string field
- Add enum constants: `REACTION_TYPE_UPVOTE = "upvote"`
- UI could extend to show multiple emoji buttons
- No breaking changes needed to existing schema

### Unified Reactions Admin

- Create UNION query combining answer + question reactions
- Single admin page showing all reactions with type filter
- Shared delete command for both types

### Permission Granularity

- Current: All authenticated users can react (except to own content)
- Future: Add role-based restriction (e.g., only members with 50+ reputation)
- Implement via permission service extension

---

## Notes

- **No Notifications**: Reactions do NOT trigger Q&A discussion notifications
  (same as answers)
- **No Auto-Subscription**: Reacting does NOT auto-subscribe member (same as
  answers)
- **Idempotent**: Toggle pattern ensures safe repeated clicks
- **Graceful Degradation**: Badge hidden if user not authorized to react
- **Independent from Answers**: Question reactions are separate from answer
  reactions
- **Author Restriction**: Authors cannot react to their own content (both
  answers and questions)

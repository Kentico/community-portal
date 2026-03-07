# Community Program Targets Report - Admin Member Emulation Feature

## Overview

Enhance the Community Program Targets Report widget to allow internal employees
(administrators) to view scorecard data for any member enrolled in the Community
Program (MVP or Community Leader status). This enables administrators to audit
member progress without manually switching accounts.

## User Stories

### Admin Member Selection

- **As An:** Administrator (internal employee with @kentico.com email)
- **I Want To:** View a dropdown of all members with MVP or CommunityLeader
  status
- **So That:** I can select and view their program scorecard without switching
  accounts

### Member Scorecard Emulation

- **As An:** Administrator viewing the widget
- **I Want To:** See the same scorecard data for the selected member as they
  would see
- **So That:** I can audit their progress and performance metrics

## Architecture Changes

### 1. State Management via Query Parameter

**Approach:** HTMX POST request handling with emulated member ID passed as query
parameter

The emulated member ID is transmitted via:

- **Query Parameter:** `?emulatedMemberId={id}` in HTMX POST request
- **No Widget Property Storage:** The emulation state is request-scoped, not
  persisted
- **Benefits:**
  - Stateless server-side rendering
  - No session state required
  - Can be refreshed via browser back/forward
  - Multiple admins can emulate different members without interference
  - Clean URL structure for bookmarking/sharing specific member scorecard

### 2. Backend Query Handler Enhancement

**File:** `CommunityProgramTargetsReportWidget.cs`

Modify `CommunityProgramTargetsReportQuery` record:

```csharp
public record CommunityProgramTargetsReportQuery(
    int MemberId,                // Either current user or emulated member
    string ChannelName,
    int Year,
    int? RequestingAdminId = null) // Store who made the request for audit trail
    : IQuery<CommunityProgramTargetsReportQueryResponse>, ...
```

Modify `CommunityProgramTargetsReportQueryHandler`:

- Add validation to ensure RequestingAdminId belongs to internal employee
- Determine which member ID to use (current auth user vs emulated member)
- Add optional audit logging for admin emulation requests
- Query handler validates that MemberId is either:
  - Current authenticated user's ID (non-emulated request), OR
  - Any program member (when RequestingAdminId is internal employee)

### 3. New Query Handler - List Members in programs

- Use `CommunityProgramMembersQuery`

### 4. Widget View Model Update

**File:** `CommunityProgramTargetsReportWidget.cs`

Modify `CommunityProgramTargetsReportWidgetViewModel`:

```csharp
public class CommunityProgramTargetsReportWidgetViewModel : BaseWidgetViewModel
{
    public bool IsInternalEmployee { get; }  // NEW
    public int CurrentAuthMemberId { get; }  // NEW - authenticated user's ID
    public int? EmulatedMemberId { get; }    // NEW - admin-selected member ID
    public IReadOnlyList<MemberSummary> AvailableMembers { get; } // NEW - dropdown options
    public bool IsEmulating => EmulatedMemberId is not null; // NEW - convenience property

    // ... existing properties
}
```

### 5. Widget UI/View Enhancement with HTMX

**File:** `CommunityProgramTargetsReport.cshtml`

Server-side rendered admin emulation section (conditional on
`IsInternalEmployee`). Uses HTMX to POST dropdown changes to controller action.

**Key Features:**

- Dropdown rendered server-side only for internal employees
- HTMX `hx-post` attribute submits selection to controller action
- HTMX `hx-target` swaps response HTML into same container
- Controller authorizes request and re-renders widget HTML
- No client-side JavaScript required for member selection

**Rendering Logic:**

```html
@if (Model.IsInternalEmployee)
{
    <div class="admin-emulation-section" id="emulation-container">
        <label for="memberSelect">Member Emulation (Admin Only)</label>
        <select id="memberSelect"
                name="emulatedMemberId"
                hx-post="@Url.Action("SelectMember", "CommunityProgramTargetsWidget")"
                hx-target="#emulation-container"
                hx-swap="outerHTML"
                class="form-select">
            <option value="">View your own scorecard</option>
            @foreach (var member in Model.AvailableMembers)
            {
                <option value="@member.MemberId"
                        selected="@(Model.EmulatedMemberId == member.MemberId)">
                    @member.DisplayName (@member.ProgramStatus)
                </option>
            }
        </select>
    </div>

    @if (Model.IsEmulating)
    {
        <div class="alert alert-info">
            Viewing scorecard for: <strong>@Model.AvailableMembers
            .FirstOrDefault(m => m.MemberId == Model.EmulatedMemberId)?.DisplayName</strong>
        </div>
    }
}
```

**HTMX Attribute Behavior:**

- `hx-post`: Submits form data (emulatedMemberId) via POST to controller
- `hx-target="#emulation-container"`: Target element for HTML replacement
- `hx-swap="outerHTML"`: Replace entire outer HTML of container
- Change event triggers automatically on `<select>` (HTMX default)

## Implementation Phases

### Phase 1: Backend Query Infrastructure (REQUIRED)

1. Create `ListMembersInProgramQuery` handler
2. Implement database query to fetch members by program status
3. Add unit tests for query handler
4. Verify query performance with large member lists

### Phase 2: Controller Action for HTMX (NEW)

1. Create `CommunityProgramTargetsWidgetController` with action:
   - `SelectMember(int? emulatedMemberId)` - POST action
2. Authorize request to internal employees only
3. Render partial HTML of emulation section + widget (or full component)
4. Return HTML for HTMX to swap

**Controller Implementation:**

```csharp
[ApiController]
[Route("api/community-program-targets-report")]
public class CommunityProgramTargetsReportController : ControllerBase
{
    [HttpPost("select-member")]
    public async Task<IActionResult> SelectMember(int? emulatedMemberId)
    {
        // Get current authenticated member
        var currentMemberId = CommunityMember.GetMemberIDFromClaim(HttpContext);

        // Authorize: must be internal employee
        if (!await IsInternalEmployee(currentMemberId))
        {
            return Unauthorized();
        }

        // Re-invoke widget with emulated member ID
        var component = new ComponentViewModel<CommunityProgramTargetsReportWidgetProperties>
        {
            // ... properties
        };

        // Render the widget component HTML with emulated data
        return PartialView("CommunityProgramTargetsReport",
            new { EmulatedMemberId = emulatedMemberId });
    }
}
```

### Phase 3: Widget Logic Enhancement

1. Update `CommunityProgramTargetsReportQuery` to support admin emulation
2. Modify `CommunityProgramTargetsReportQueryHandler` to:
   - Accept RequestingAdminId parameter
   - Validate admin is internal employee
   - Resolve correct member ID to query
3. Update `CommunityProgramTargetsReportWidget` InvokeAsync to:
   - Check if user is internal employee
   - Fetch available members list
   - Extract emulatedMemberId from query parameter (if present)
4. Update `CommunityProgramTargetsReportWidgetViewModel` with new properties

### Phase 4: UI Implementation

1. Update `CommunityProgramTargetsReport.cshtml` with:
   - Admin emulation dropdown (server-side conditional)
   - HTMX attributes for POST/swap behavior
   - Member selection display (conditional)
   - Emulation banner (conditional)
2. Add styling for emulation UI and banner
3. Ensure HTMX library is loaded on page

### Phase 5: Testing & Polish

1. Unit tests for admin validation logic
2. Integration tests for controller authorization
3. Unit tests for member emulation query
4. HTMX behavior tests (member selection triggers POST)
5. Performance testing with large member lists
6. Accessibility review of dropdown UI

## Missing Features & Dependencies

### 1. **Members Query Handler (CRITICAL)**

- **What:** Query to retrieve all members with specific program statuses
- **Where:** Create in `Queries/` folder within widget or shared location
- **Current Gap:** No existing infrastructure to fetch filtered member list
- **Consider:**
  - Pagination for potentially large lists (100+ members)
  - Caching strategy
  - Performance implications of full member scan
  - Whether to use IContentQueryExecutor or direct data layer

### 2. **Member Summary DTO (REQUIRED)**

- **What:** Lightweight DTO containing member ID, name, email, status
- **Why:** Avoid exposing full CommunityMember object in queries
- **Current Gap:** No existing lightweight member projection type

### 3. **Controller Action for HTMX (CRITICAL)**

- **What:** `CommunityProgramTargetsReportController` with `SelectMember` POST
  action
- **Why:** Handle HTMX requests for member selection with authorization check
- **Current Gap:** No existing controller for this widget
- **Key Logic:**
  - Validate authenticated user is internal employee
  - Accept `emulatedMemberId` form parameter
  - Return HTML fragment for HTMX to swap

### 4. **Admin Validation Extension (RECOMMENDED)**

- **What:** Extension method or utility to validate internal employee status
- **Current State:** `CommunityMember.IsInternalEmployee` exists
- **Enhancement:** Consider caching or adding to ClaimsPrincipal for perf

## Data Access Considerations

### Query Performance

- Large member lists (100+ members) could impact dropdown render time
- **Solutions:**
  1. Pagination on dropdown (load first 50, lazy-load on scroll)
  2. Searchable dropdown (filter by name/email)
  3. Cache all members list with cache dependency on MemberInfo

### Security & Permissions

- Only internal employees can see dropdown
- Admin selection logs which member admin is viewing (optional audit trail)
- Consider: Should there be explicit "emulation" permissions separate from
  "internal employee"?

### Cache Dependencies

- Add `MemberInfo.OBJECT_TYPE` to cache dependencies
- Dropdown members list should invalidate when:
  - Member program status changes
  - New member added to programs
  - Member removed from programs

## Implementation Order

1. **Step 1:** Create `ListMembersInProgramQuery` and handler
2. **Step 2:** Add `MemberSummary` record type
3. **Step 3:** Create `CommunityProgramTargetsReportController` with
   `SelectMember` POST action
   - Include authorization check for internal employees
   - Return partial HTML with emulation section
4. **Step 4:** Update widget model and view model with new properties
5. **Step 5:** Add HTMX attributes to dropdown in
   `CommunityProgramTargetsReport.cshtml`
   - `hx-post`, `hx-target`, `hx-swap` attributes
6. **Step 6:** Handle `emulatedMemberId` query parameter in widget's
   `InvokeAsync`
7. **Step 7:** Add unit/integration tests for controller authorization
8. **Step 8:** Add tests for member selection and query execution
9. **Step 9:** HTMX integration testing and UI polish

## Code Examples & Patterns

### Retrieve Members by Program Status (Pseudo-code)

```csharp
// In ListMembersInProgramQueryHandler
var members = await memberProvider.Get()
    .WhereEquals("MemberProgramStatus", ProgramStatuses.MVP)
    .Or()
    .WhereEquals("MemberProgramStatus", ProgramStatuses.CommunityLeader)
    .OrderBy("MemberDisplayName")
    .AsAsync();

return new ListMembersInProgramQueryResponse(
    members.Select(m => new MemberSummary(
        m.MemberId,
        m.GetValue("MemberFirstName") + " " + m.GetValue("MemberLastName"),
        m.MemberEmail,
        Enum.Parse<ProgramStatuses>(m.GetValue("MemberProgramStatus"))))
        .ToList());
```

### Widget Integration with HTMX Request Handling

```csharp
// In CommunityProgramTargetsReportWidget.InvokeAsync
var currentMemberId = CommunityMember.GetMemberIDFromClaim(HttpContext);
var isInternalEmployee = /* check IsInternalEmployee property */;

IReadOnlyList<MemberSummary> availableMembers = [];
int? emulatedMemberId = null;

if (isInternalEmployee)
{
    // Fetch all members in program for dropdown
    var membersResponse = await mediator.Send(
        new ListMembersInProgramQuery(
            channelContext.ChannelName,
            [ProgramStatuses.MVP, ProgramStatuses.CommunityLeader]));
    availableMembers = membersResponse.Members;

    // Extract emulated member ID from POST request query parameter
    // (set by HTMX when admin changes dropdown selection)
    if (int.TryParse(HttpContext.Request.Query["emulatedMemberId"], out int emulateId))
    {
        emulatedMemberId = emulateId;
    }
}

// Use emulated member ID if set by admin, otherwise use current authenticated member
int memberIdToQuery = emulatedMemberId ?? currentMemberId;

var query = new CommunityProgramTargetsReportQuery(
    memberIdToQuery,
    channelContext.ChannelName,
    year,
    RequestingAdminId: isInternalEmployee ? currentMemberId : null);

// ... execute query and build response
```

### Controller Action for Member Selection (HTMX Target)

```csharp
[Route("api/community-program-targets-report")]
public class CommunityProgramTargetsReportController : ControllerBase
{
    private readonly IMediator mediator;
    private readonly IWebsiteChannelContext channelContext;

    [HttpPost("select-member")]
    [Authorize]  // Requires authenticated user
    public async Task<IActionResult> SelectMember(
        [FromForm] int? emulatedMemberId,
        CancellationToken cancellationToken)
    {
        // Get current authenticated member
        var currentMemberId = CommunityMember.GetMemberIDFromClaim(HttpContext);
        if (currentMemberId.Value <= 0)
        {
            return Unauthorized();
        }

        // Get member info and validate internal employee status
        var memberInfo = await memberProvider.Get(currentMemberId).FirstOrDefaultAsync();
        var currentMember = memberInfo?.AsCommunityMember();

        if (currentMember?.IsInternalEmployee != true)
        {
            return Forbid("Only internal employees can emulate members");
        }

        // Fetch members list and build view model
        var membersResponse = await mediator.Send(
            new ListMembersInProgramQuery(
                channelContext.ChannelName,
                [ProgramStatuses.MVP, ProgramStatuses.CommunityLeader]),
            cancellationToken);

        var viewModel = new CommunityProgramTargetsReportAdminSectionViewModel(
            IsInternalEmployee: true,
            CurrentAuthMemberId: currentMemberId,
            EmulatedMemberId: emulatedMemberId,
            AvailableMembers: membersResponse.Members);

        // Render partial HTML of emulation section + widget
        // (HTMX will swap this into #emulation-container)
        return PartialView(
            "~/Components/PageBuilder/Widgets/CommunityProgramTargetsReport/CommunityProgramTargetsReport.cshtml",
            viewModel);
    }
}
```

## Testing Strategy

### Unit Tests

- Validate internal employee detection
- Test query filtering by program status
- Verify cache dependencies include MemberInfo

### Integration Tests

- End-to-end member emulation query
- Verify correct member data returned
- Test with multiple members in programs

### E2E Tests

- Admin selects member from dropdown
- Scorecard updates to show selected member's data
- Verify emulation banner displays
- Test switching between members

## Success Criteria

- ✅ Internal employees see member dropdown on widget
- ✅ Admin can select member and view their scorecard
- ✅ Scorecard displays selected member's data accurately
- ✅ Non-internal employees see no dropdown (existing behavior preserved)
- ✅ Performance acceptable with 100+ members
- ✅ All tests passing (unit, integration, E2E)
- ✅ Audit trail logged (if implemented)

## HTMX Implementation Details

### Request/Response Flow

1. **Admin changes dropdown selection** → HTMX listener triggers
2. **HTMX POST to `/api/community-program-targets-report/select-member`**
   - Form data: `emulatedMemberId={selectedValue}`
   - Headers: Standard HTMX headers (HX-Request, etc.)
3. **Server-side authorization check** in controller action
   - Verify user is authenticated
   - Verify user is internal employee (@kentico.com
   - Return 403 Forbidden if not authorized
4. **Server re-renders emulation section HTML**
   - Includes updated dropdown selection
   - Includes emulation banner (if emulating)
5. **HTMX swaps response into #emulation-container**
   - `hx-swap="outerHTML"` replaces entire container
   - DOM updated with new dropdown state and banner
6. **Widget displays updated scorecard data**
   - Already rendered server-side with emulated member data

### HTMX Configuration Required

- Must ensure HTMX library is loaded on page (script tag)
- Configure HTMX to include CSRF token (if using ASP.NET CSRF protection)
- Consider adding loading indicator during request: `hx-indicator=".spinner"`

```html
<!-- In layout or page head -->
<script src="https://unpkg.com/htmx.org@1.9.10"></script>

<!-- Configure CSRF for POST requests -->
<script>
    document.body.addEventListener('htmx:configRequest', function(evt) {
        evt.detail.headers['RequestVerificationToken'] =
            document.querySelector('input[name="__RequestVerificationToken"]').value;
    });
</script>
```

## Risks & Mitigation

| Risk                                         | Impact | Mitigation                                                       |
| -------------------------------------------- | ------ | ---------------------------------------------------------------- |
| Large member list impacts performance        | High   | Implement pagination/lazy-load or search                         |
| HTMX request blocked or fails silently       | Medium | Add error handling with `hx-on="htmx:responseError: alert(...)"` |
| New member added requires cache invalidation | Medium | Add MemberInfo cache dependency                                  |
| Security: Unauthorized emulation attempt     | High   | Controller action validates internal employee status             |
| CSRF token missing in POST request           | High   | Configure HTMX to include CSRF token (see config above)          |

---

**Last Updated:** 2026-02-06 **Status:** Ready for Implementation

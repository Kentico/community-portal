# Q&A Notification Pipeline — "Processed 0 notifications across 0 members" Analysis

**Date**: 2026-07-08 (analysis), documented 2026-07-10
**Repo**: `community-portal-internal`
**Branch**: `analysis/qanda-notification-zero-notifications` (created from `copilot/add-custom-automation-action`)
**Symptom**: Production Event Log shows `Processed 0 notifications across 0 members for DailyTwice`
from the `NOTIFICATION_EMAILS_SENT` info event, every run, despite Q&A interactions that should
generate notifications.

## Pipeline overview

`QAndADiscussionNotificationDailyTwiceScheduledTask` (runs every 12h per
`q_anotification-twicedaily.xml`) → `QAndANotificationsProcessor.Process(DailyTwice)`:

1. `GetAllSettingsAtFrequency` — settings rows where `FrequencyType = 'DailyTwice'`
2. `GetNotifiableEvents` — events where `DiscussionEventDateModified > now − 12h`
3. `GetQuestionsByWebPageItemID` — published question pages for those events
4. `GetSubscribedQuestionsGroupedByMember` — subscriptions intersecting those questions
5. `FilterEventsForMembers` / `IsEventNewForMember` — per-member ID or date gate
6. Render email → `SendEmail` per batch → advance `LatestDiscussionEventID` → cleanup old events

Events are written by `QAndANotificationLogger` from `QAndAAnswerController` on answer create
(`NotifyNewAnswer`) and answer accept (`NotifyAcceptedAnswer`).

## Ranked findings

### 1. Event query window == task interval with no overlap (regression in `25d9a80f3`)

`GetNotifiableEvents` (`QAndANotificationsProcessor.cs:316`) loads only events from the last 12
hours for DailyTwice. Before commit `25d9a80f3` ("copilot optimizations"), when any member had a
nonzero `LatestDiscussionEventID`, the query was ID-based
(`DiscussionEventID >= min(lastSeenIDs) + 1`) with no time bound.

Consequences:
- Scheduler drift, app restart/downtime (Xperience tasks only run while the app is up), or a
  failed/skipped run means events age past 12h and are **never loaded again** — permanently
  unnotified even though the per-member ID gate would accept them.
- If an email send fails on one run, the member's pointer correctly isn't advanced, but by the
  next run the events may be outside the window — the retry the pointer enables never happens.
- Same knife-edge applies to DailyOnce (24h), Weekly (7d), Monthly (1mo).

**Fix**: widen the query window (e.g., 2× frequency, or query since the oldest
`LatestDiscussionEventID` among members at this frequency); keep the per-member ID filter as the
source of truth.

### 2. `DiscussionEventDateModified` is the auto-managed timestamp column (server-local time)

`DiscussionEventInfo.TYPEINFO` (`DiscussionEventInfo.generated.cs:29`) declares
`DiscussionEventDateModified` as the ObjectTypeInfo *timestamp column*, which Xperience overwrites
on save with the server's **local** time — the `clock.GetUtcNow()` value set in
`QAndANotificationLogger.Log` is discarded. `limitDate` is derived from `GetUtcNow()`. If the
production host's timezone is behind UTC (e.g., US Eastern), the effective DailyTwice window
shrinks from 12h to ~7h; combined with finding #1, most events fall outside it.

> Note: inferred from the ObjectTypeInfo constructor signature — verify by comparing stored
> `DiscussionEventDateModified` values against actual (UTC) answer-post times.

### 3. Poisoned `LatestDiscussionEventID` mutes a member forever

`IsEventNewForMember` (`QAndANotificationsProcessor.cs:178`): once a member's
`LatestDiscussionEventID` is nonzero, only events with a **higher ID** qualify. If the event table
was ever cleared/reseeded (DB restore, manual cleanup, environment sync) while settings rows kept
old pointers, no new event ever qualifies — a **persistent** zero. Diagnostic SQL:

```sql
SELECT MAX(DiscussionEventID) FROM KenticoCommunity_DiscussionEvent;

SELECT DiscussionMemberNotificationSettingsMemberID,
       DiscussionMemberNotificationSettingsLatestDiscussionEventID
FROM KenticoCommunity_DiscussionMemberNotificationSettings
WHERE DiscussionMemberNotificationSettingsFrequencyType = 'DailyTwice';
```

If any pointer ≥ max event ID while recent events exist, this is the bug.

### 4. The "0 across 0" log is ambiguous — also fires when every email send fails

In `Process` (`QAndANotificationsProcessor.cs:101`), `successfulBatches.Count` is 0 both when no
batches were built **and** when batches existed but every `SendEmail` threw (caught per batch at
line 74). Check the Event Log for `NOTIFICATION_EMAIL_SEND_FAILURE` /
`NOTIFICATION_SETTINGS_UPDATE_FAILURE` at the same timestamps. A failure in
`RenderToStringAsync` is *not* caught and would fail the whole task instead.

## Legitimate-zero data-state causes (verify before assuming a code bug)

- **Auto-subscribe defaults to `false`**; settings rows only exist after a member explicitly saves
  the account form or clicks Subscribe. Posting a question/answer does **not** subscribe the
  author (`CreateAnswer` / `CreateQuestion` gate `SubscribeToDiscussion` on
  `GetAutoSubscribeEnabled`, which returns `false` with no settings row). Interactions can be
  plentiful while the subscriber set for those questions is empty.
- **Default frequency is Weekly** (`InitializeNewSettings`) — the DailyTwice cohort only contains
  members who explicitly picked "Twice Daily". If empty, `GetAllSettingsAtFrequency`
  short-circuits and produces exactly this log. Compare with what the Weekly task run reports.
- `SubscribeToDiscussion` initializes a brand-new settings row's `LatestDiscussionEventID` to the
  current **global** max event ID — intended to skip history, but it also skips concurrent events
  on other questions the member subscribes to moments later.

## Smaller issues

- Retention cleanup passes a `DateTimeOffset` (`GetUtcNow().AddMonths(-3)`) into a
  `WhereCondition` against a `datetime` column — SQL type-precedence conversion assumes UTC
  offset 0; consistent with the file's general UTC/local mixing.
- `CMSTransactionScope` in `QAndANotificationSettingsManager` spans `await` calls; Kentico
  connection scopes are ambient/context-sensitive and can break under async continuations.
- **Diagnostic gap**: `CollectNotificationContent` computes the whole funnel (settings → events →
  subscriptions → filtered members) but logs nothing. Add one info log with those four counts —
  recommended as the first change regardless of the fix.

## Verdict

Given a *consistent* "0 across 0": most likely either the DailyTwice cohort/subscription data
doesn't exist as expected (auto-subscribe false + Weekly default), or finding #3 (stale event-ID
pointers) — both produce persistent zeros. Findings #1/#2 produce intermittent misses. The SQL
checks above distinguish these quickly.

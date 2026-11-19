# Privacy, consent and data retention

How the portal handles consent, personal data requests and data retention, and where each part is configured.

## Configuration (`appsettings.json`)

| Setting | Default | Purpose |
| --- | --- | --- |
| `TermsOptions:CurrentVersion` | see `appsettings.json` | Terms of Use version stored on the member at registration (`MemberTermsAcceptedVersion`). `/terms` shows it through `{{TermsVersion}}`; E2E specs read it from `appsettings.json`. See [Changing the Terms of Use](#changing-the-terms-of-use). |
| `EmailMarketingOptions:AgreementCutoffDate` | `EmailMarketingOptions.CONSENT_GO_LIVE` (code) | Newsletter confirmations dated before this get no *Kentico Community news and updates* (`EmailMarketing`) consent agreement: those subscribers saw the earlier sign-up text. Deployed environments use the code default. Development/CI: `2026-01-01`, so E2E specs can seed confirmations on both sides. |
| `RetentionOptions:AnonymousContactInactiveDays` | `365` | Contacts without an email are deleted after this many days without activity. |
| `RetentionOptions:KnownContactInactiveDays` | `1095` | Contacts with an email that aren't members or active newsletter subscribers are deleted after this many days without activity. |
| `RetentionOptions:ActivityDays` | `1095` | Activities older than this are deleted, except `KeptActivityTypes`. |
| `RetentionOptions:KeptActivityTypes` | `memberregistration`, `bizformsubmit` | Activity types never deleted by the activity task. |
| `RetentionOptions:ActivityDeleteBatchSize` | `4000` | Rows per `DELETE` batch in the activity retention task. Kept under SQL Server's lock escalation threshold (~5000) so live activity logging isn't blocked. |
| `RetentionOptions:ShortLivedFormDefaultDays` | `1095` | Retention for Short-lived forms whose policy has no days of its own (0). |
| `EmailQueueOptions:ArchiveDuration` | `14` | Days Xperience keeps sent email copies (`CMS_Email`). After that, email statistics can't be linked to a recipient. |
| `TagManagerConsentOptions:AlwaysAllowedSnippetNames` | empty | Tag Manager snippet code names that render before the visitor allows Analytics. Empty: no snippet renders without Analytics. |
| `ReCaptcha:ScoreThreshold` | `0` (not in `appsettings.json`) | Minimum reCAPTCHA v3 score, set per environment (`appsettings.{Environment}.json`). CI: `0`. Scores are logged (event 1001). |

The periods are also stated on `/privacy` ("How long we keep data"). Change both together.

## Changing the Terms of Use

The version lives in one place: `TermsOptions:CurrentVersion`. Any Rich Text widget replaces `{{TermsVersion}}` with it when the page renders (`TermsVersionPlaceholder`), and `/terms` uses that placeholder instead of a typed version.

1. Edit the `/terms` text in admin. Keep `{{TermsVersion}}`; update the "effective" date by hand.
2. Change `TermsOptions:CurrentVersion` in `appsettings.json` (e.g. `2026-11` → `2027-03`). No spec or doc edits needed.
3. CI store, commit, deploy, then Content Sync `/terms`. Deploy the setting and the page together, so new members store the version the page shows.
4. Existing members aren't asked to accept the new version (sign-in acceptance isn't built). Announce significant changes by email or on the site, as `/terms` says.

## Scheduled tasks

All in admin → Scheduled tasks. All the retention tasks ship **disabled**. Enable them after the environment's forms are classified. The exception is `EmailMarketingAgreementReconciliation`, which ships **enabled** (it isn't a cleanup task).

| Task (code name) | Schedule | Default | What it does |
| --- | --- | --- | --- |
| `EmailMarketingAgreementReconciliation` | every 15 min | enabled | Agrees/revokes the *Kentico Community news and updates* consent to match newsletter subscriptions. Not a cleanup task. |
| `ContactRetention` | nightly | disabled | Deletes anonymous contacts inactive for `AnonymousContactInactiveDays`, and known contacts (not members, not active subscribers) inactive for `KnownContactInactiveDays`. "Inactive" = created before the cutoff and no activity since. Activities, consent agreements and list memberships go with the contact. |
| `ActivityRetention` | nightly | disabled | Deletes activities older than `ActivityDays`, except `KeptActivityTypes`. |
| `FormDataRetention` | nightly | disabled | Deletes submissions of Short-lived forms older than their retention days. Keeps History forms. Keeps unclassified forms and logs a warning (event 2004) for each. |

Xperience's built-in inactive contact deletion stays off (the "Delete inactive contacts" task disabled, the setting on "Do not delete contacts"). In 31.9.1 it can't exclude member and subscriber contacts, so it would also delete quiet members' and subscribers' contacts, with their subscriptions and agreements.

Logged results (event IDs): 2001 contacts, 2002 activities, 2003 forms. These are Information, so they go to the app's standard log output only, not the Xperience event log (`Logging:XperienceEventLog:LogLevel` keeps `Kentico.Community.Portal` at Warning). The 2004 unclassified-form warning is the only one that reaches the Xperience event log.

## Form retention policies

Each form has a policy, set in admin → Forms → *form* → **Retention** tab. The Forms list shows it in the **Retention** column.

| Policy | Kept | On an erasure request |
| --- | --- | --- |
| Unclassified (no policy, the default) | Until a policy is set (fail-safe: nothing is deleted by mistake). The form retention task logs a warning nightly, so set a policy on every form. | Email replaced, row kept |
| History | While the portal runs (polls, program activity) | Email replaced, row and member ID kept |
| Short-lived | Retention days on the tab, or `ShortLivedFormDefaultDays` when 0 | Row deleted |

Policies are data (`kenticocommunity.formretentionpolicy`), not settings, and are **not deployed with CD**: local and production forms differ. Set them in each environment's admin. The local CI repository has policies for the local forms (E2E specs rely on them).

Personal data in forms is found by field shape, not form name: a member ID field, a member email field, any *Email input* field, and the field mapped to the contact's email.

## Data protection requests

Admin → Data protection → enter the email.

- **Right to access / Data portability:** text or JSON with the member, roles, external logins, linked content items, Q&A answers and reactions, discussion settings, badges, form submissions, contacts, activities, consent agreements, subscriptions, sent emails and email statistics.
- **Right to be forgotten:** options (all on by default):
  - anonymize member: disabled and archived (the moderation status says why), password cleared, email `erased-<memberID>@erased.invalid`, user name `former-member-<memberID>`, name "Former member", profile fields, avatar and external logins cleared. The member is never deleted, so Q&A and badges stay attributed to "Former member". The Q&A search index is rebuilt.
  - delete contacts (with activities and agreements)
  - delete Short-lived form submissions
  - replace the email in kept form submissions
  - remove subscriptions (recipient lists, Q&A discussion subscriptions and notification settings)
  - unlink email statistics (delete sent email copies, blank the mailout GUID on their hits)

  Editorial content linked to the member (profile cards, author, testimonial, integration and contribution items) is not changed; it's listed in the event log for manual review.

## Consent cookies and tracking

See `/cookies-policy` for the cookie list. In short: tracking (contact, activities, email open/click linked to a contact) needs the **Kentico Community website & email tracking** (`KenticoCommunityWebsiteTracking`) consent and cookie level All. Without it the site runs at Essential. Analytics (GA4 via Google Consent Mode) is a separate choice in the banner.

### Tag Manager snippets

No Xperience Tag Manager snippet (a GTM snippet included) renders or loads until the visitor allows **Analytics**. This applies to every snippet, whatever its consent setting in admin. Only snippets listed in `TagManagerConsentOptions:AlwaysAllowedSnippetNames` render without it.

- Allowing Analytics in the banner or cookie settings loads the snippets on the same page, no reload.
- Withdrawing Analytics stops them from the next page load and deletes `_ga*` cookies set on the portal host. Sign-out does the same. Analytics cookies on the parent domain are shared with other Kentico websites and aren't touched.
- Google Consent Mode starts denied on every page, before any snippet, and is updated from the choice. `ad_storage`, `ad_user_data` and `ad_personalization` are always denied.

Code: `ConsentGatedCodeSnippetsService` (decorates Tag Manager's `IChannelCodeSnippetsService`) and `ConsentModeScript`.

## Testing

- E2E: `tests/privacy/data-protection.spec.ts`, `tests/privacy/retention.isolated.spec.ts` (isolated project: the tasks delete across the database), `tests/privacy/third-party.isolated.spec.ts` (Tag Manager gate, with a site-wide stub snippet).
- Testing endpoints: `/testing/e2e/data-protection/*`, `/testing/e2e/form-retention/*` (Development/CI only).

# Member email confirmation is its own field, separate from Enabled

Xperience's membership treats `EmailConfirmed` and `MemberEnabled` as the same value: `ApplicationUser.EmailConfirmed` is stored in `MemberEnabled`, and `UserManager.ConfirmEmailAsync` enables the member. `Enabled` was overloaded. It meant both "this member proved they own their email" and "an administrator allows this member to sign in".

That made a self re-enable hole. An administrator disables a member who confirmed their email long ago and isn't moderated. On sign-in, the member sees "Your email has not been verified" with a "Send verification email" button. They click it, confirm the new link, `ConfirmEmailAsync` sets `Enabled` back to true, and they're signed in again. Nothing distinguished "never confirmed" from "disabled by an administrator".

We add one custom column, `CMS_Member.MemberEmailConfirmedDate` (datetime, null = never confirmed). `Enabled` keeps meaning "can sign in". The date means "proved they own their account email", and disabling the member never clears it.

## Options considered

- **Policy: administrators always moderate instead of disabling.** No code change, but it depends on every administrator (and every script and integration) remembering. One plain "Enabled" uncheck reopens the hole, and nothing in the product prevents it.
- **Auto-set a moderation status when `Enabled` flips to false.** Moderation already blocks resend and confirmation. But it overloads moderation instead of `Enabled`: a status meant for spam, flags and offboarding would also mean "disabled", it needs a status value to pick, and it would have to be undone when the member is re-enabled. A member who never confirmed is also disabled, so the rule needs the same "were they ever confirmed?" fact anyway.
- **A `MemberEmailConfirmedDate` column (chosen).** One field, one meaning, set by the flows that prove email ownership. It answers the question directly, works no matter how the member got disabled (admin form, Management API, code), and keeps moderation and `Enabled` meaning what they say. Cost: a custom column on a system class, and a backfill for existing members.

## How it works now

`CommunityMember.SignInState`:

| State               | Rule                                    | Sign-in (right password)                | Resend / confirm registration email |
| ------------------- | --------------------------------------- | --------------------------------------- | ----------------------------------- |
| `UnderModeration`   | Moderation status other than `None`     | Moderation message                      | No                                  |
| `Active`            | Enabled                                 | Signs in                                | No (already confirmed)              |
| `EmailNotConfirmed` | Not enabled, confirmed date null        | Verify-email prompt with resend button  | Yes                                 |
| `Disabled`          | Not enabled, confirmed date set         | Generic "you can't sign in" message     | No                                  |

- **Disabled vs moderation, for administrators.** Unchecking Enabled on a member who confirmed before is now enough to block them; they can't undo it. Moderation stays for spam, flagged and archived (offboarding) members, with its own message. Administrators no longer need to also disable a member when moderating, or moderate a member they only want disabled.
- **Setting the date.** Registration confirmation and a confirmed self-service account email change set it when it's null, and keep it when it's set. Any member save that enables a member without a date (an administrator enabling them) sets it too: the administrator vouches for them, and a later disable must stick. Only anonymization clears it.
- **Resend and confirm.** `ResendConfirmationEmail` and the registration confirm only act for `EmailNotConfirmed`. Resend validates the antiforgery token, is rate limited (`EmailVerification` policy), and always returns the same response after the same minimum delay, whether or not anything was sent.
- **Password-first sign-in.** For every state other than `Active`, the password is checked first. A wrong password gets the same generic failure as an unknown account, so the account's state is only shown to someone who knows the password.
- **Confirmation links need a click.** Corporate email scanners pre-open links. Opening a registration, account email change or alternate email link (GET) changes nothing: the page shows a confirm button. The button htmx-POSTs the token in the body with the antiforgery token; the page then shows the result.
- **Backfill.** Migration `20261118_01_MemberEmailConfirmedDateBackfill` (CD and CI `@migrations`, listed in `After.txt`) sets the date to `MemberCreated` (or now) for enabled members with no date. It's safe to run more than once.

## Consequences

- Members who were already disabled when this shipped keep a null date, so they are treated as not yet confirmed. That matches production: administrators have always set a moderation status when locking a member out, so every disabled, non-moderated member is one who never confirmed their email. Moderation keeps blocking the rest.
- An administrator who disables a member who never confirmed hasn't blocked them for good: the member can still confirm their email, which enables them. Use moderation for that.
- The field depends on code (`ApplicationUser` mapping in `CommunityMember`) and on the Edit form showing it read-only. Changing it by hand in the database changes what a disabled member can do.
- `CMS.Member` changes (the column and the alternative forms) must be included in the CD configuration so the migration finds the column.
- Related: [ADR 0003](./0003-kenticoemployee-role-lifecycle.md) — the KenticoEmployee role's trust signal is email confirmation.

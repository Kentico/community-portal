# KenticoEmployee Member Role: assign on confirmation, revoke on offboarding, no live domain check

Internal-employee status was previously a live, unstored check on email domain (`@kentico.com`). Native page/content security needs an actual persisted role, not a runtime computation, so `KenticoEmployee` becomes a real Member Role with its own assignment lifecycle.

The role is assigned automatically when a member confirms their email, if that email ends in `@kentico.com` — with no dependency on a matching CMS admin User existing. Email confirmation is itself the trust signal: someone who has left the company can no longer receive or click a confirmation link at that address, so a "matching active User account" check was considered and rejected — it produces false negatives for genuinely active employees who never used SSO to log into admin, and false positives aren't possible via this trigger.

The role is revoked, and the member's `ModerationStatus` set to `Archived`, via a global event when the matching CMS admin User account (matched by email) is disabled or deleted — offboarding is a signal only available on the User side, since email delivery alone can't detect departure after the fact. Archiving is one-directional; only a human clears it.

## Consequences

- Members who registered before this shipped may not hold the role yet. A one-time SQL migration backfilled it (the script has since been removed from the CD repository) for confirmed, non-moderated members with an `@kentico.com` email — no User-account requirement, for the same false-negative reason above.
- The backfill does **not** retroactively archive existing members lacking a matching active User, since that signal doesn't reliably indicate departure (same reasoning as above).
- Self-service email change: verifying a new `@kentico.com` account email grants the role (same trust signal as registration). Changing away from an `@kentico.com` email revokes it, because offboarding matches the member by that email and could no longer find them.
- "Confirms their email" means the registration confirmation (or a confirmed email change) that sets `MemberEmailConfirmedDate`. Since [ADR 0005](./0005-separate-email-confirmation-from-enabled.md), a member an administrator disabled can't confirm again to re-enable themselves, so a confirmation is always a fresh proof of ownership.

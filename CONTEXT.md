# Community Portal

Xperience by Kentico website for the Kentico developer/customer community — blog, Q&A, resource hub, and member profiles.

## Language

**Member Role**:
A named group of Members granted by Xperience's native membership-roles feature (`MemberRoleInfo`), used to gate access to secured pages/content items and to drive in-app permission checks (e.g. Q&A moderation).
_Avoid_: "Role" alone — that term is reserved for the separate CMS admin Role (`RoleInfo`) assigned to Users (e.g. `CommunityManager`), which is unrelated and continues to serve admin-side authorization.

**Program Status** (Community Program membership):
Whether a Member currently participates in the Kentico Community Program (MVP or Community Leader) and has access to program resources. Access is granted the moment a member is admitted (early January), independent of and prior to the public recognition badge. Backed by Member Roles (`KenticoMVP`, `KenticoCommunityLeader`), assigned manually once a year; no longer a persisted enum field on the member record.
_Avoid_: conflating with **Member Badge** — the badge is a separate, later event.

**Member Badge**:
A profile-display achievement/recognition shown publicly on a member's profile (e.g. the MVP/Community Leader badge awarded late-Jan/early-Feb). Carries no access or permission implications — entirely decoupled from page/content access and site-action permissions, even though a prior code comment incorrectly implied otherwise.

**Content Authorization** (legacy, expand-period only):
The pre-Member-Role mechanism — the `IContentAuthorizationFields` reusable schema + `ContentAuthorizationFilter` — that gated web page access using taxonomy tags matched against Member data. Being replaced by native page security (Member Roles). Remains only on the `LandingPage` content type during the expand period; removed during contract.

**Account Email**:
A Member's sign-in and contact email (`MemberEmail`). Members change it themselves from My account; it only changes after they click a verification link sent to the new address, and the previous address is notified. The username never changes with it.

**Alternate Email**:
A second, optional Member email (e.g. business or agency) used to match the Member with company data in system and data connections. Only trusted once verified (`MemberAlternateEmailVerifiedDate` set); an unverified Alternate Email never matches a Member. Unique across Members' Account Emails and verified Alternate Emails.

**Email Confirmed**:
A Member proved they own their Account Email: registration confirmation, a confirmed Account Email change, or an administrator enabling them (`MemberEmailConfirmedDate` set). Separate from **Enabled**: disabling a Member keeps the date, so they can't re-enable themselves by confirming again. See docs/adr/0005-separate-email-confirmation-from-enabled.md.
_Avoid_: using `Enabled` / Xperience's `EmailConfirmed` to mean "confirmed" — they are the same stored value and mean "can sign in".

**Enabled** (Disabled):
Whether a Member can sign in (`MemberEnabled`). A Member who never confirmed their email is not enabled; a Member with Email Confirmed who is not enabled was **Disabled** by an administrator and sees a generic "can't sign in" message. Disabling is enough to block a Member; it isn't a Moderation Status.

**Moderation Status**:
A Member's standing (`None`/`Spam`/`Flagged`/`Archived`). Anything other than `None` blocks the member from all secured content, regardless of Member Role, and blocks sign-in, resending the confirmation email, and confirming it. `Archived` is also set automatically when the Member's matching CMS admin User account (matched by email) is disabled or deleted — an employee-offboarding safeguard. It never reverts automatically; only a human clears it.

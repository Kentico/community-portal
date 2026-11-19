# Model Community Program access as Member Roles, not a persisted ProgramStatus field

`CommunityMember.ProgramStatus` (MVP / Community Leader) drove both portal resource access and reporting, and was easy to conflate with the public `MemberBadge` awarded for the same tiers. We're retiring the persisted enum in favor of `KenticoMVP` / `KenticoCommunityLeader` Member Roles as the sole source of truth for program access — assigned manually once a year — and exposing their well-known role code names in code as a `SystemTaxonomies`-style registry instead of an enum.

Program resource access must be granted at admission (early January) while the badge is awarded weeks later for public recognition — collapsing both into one field made that distinction invisible in code. Manual annual assignment (via Xperience's built-in "assign roles to members" admin UI) also removes any need for sync automation between the two concepts.

## Consequences

- Reporting surfaces that read `ProgramStatus` (`MembershipStatsPage`, `CommunityProgramTargetsReport`, `ProgramMembersAllQuery`, `TargetsReportQuery`) must switch to querying role membership — done as part of the same body of work, not deferred, since the field stops being maintained immediately.
- The admin `MemberListFilter`'s `ProgramStatus` dropdown is replaced by a multi-select Member Role filter. Since Member Roles are a many-to-many relationship (`CMS_MemberRoleMember` / `CMS_MemberRole`), this reuses the same sub-query + `STRING_AGG` join pattern (`ModifyQueryForBadgeFilter`) already used for the existing Badges filter, just against the role tables instead of the badge tables. The role selector itself reuses `MemberRoleDataProvider`, already built for `MemberRoleConditionType`.

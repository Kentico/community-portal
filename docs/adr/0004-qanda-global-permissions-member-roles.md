# Q&A global edit/delete permissions move to Member Roles, dropping the admin-User email bridge

`QAndAPermissionService.CanEditGlobally` / `CanDeleteGlobally` matched a Member to a CMS admin User by email and checked the admin `CommunityManager` Role — an indirect, non-role connection to member data that silently granted no permission for members without a matching admin User.

Both checks now test membership in the `KenticoEmployee` Member Role instead (`CanDeleteGlobally` additionally accepts `KenticoMVP` / `KenticoCommunityLeader`, unchanged). The admin `CommunityManager` Role is untouched and stays in place, but purely for CMS admin/User authorization — it has no further connection to Q&A permissions or Members.

## Consequences

- `QAndAPermissionService.GetCmsUser` and its email-matching bridge to `UserInfo` are removed.
- This is a pragmatic simplification, not a permanent permission model — a dedicated Q&A-moderator Member Role can replace the `KenticoEmployee` check later if finer-grained scoping turns out to be needed.

-- Back filling dates for badges that have already been assigned
UPDATE KenticoCommunity_MemberBadgeMember
SET MemberBadgeMemberCreatedDate = GETUTCDATE()
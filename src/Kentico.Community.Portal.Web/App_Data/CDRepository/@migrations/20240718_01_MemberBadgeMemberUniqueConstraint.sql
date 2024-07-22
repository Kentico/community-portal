-- Helps ensure we don't end up with duplicate values from invalid data logic or application scale-out
ALTER TABLE KenticoCommunity_MemberBadgeMember
ADD CONSTRAINT UQ_MemberBadgeMemberMemberBadgeId_MemberBadgeMemberMemberId UNIQUE (MemberBadgeMemberMemberBadgeId, MemberBadgeMemberMemberId);
DELETE mbm
FROM KenticoCommunity_MemberBadgeMember mbm
    INNER JOIN KenticoCommunity_MemberBadge mb ON mbm.MemberBadgeMemberMemberBadgeId = mb.MemberBadgeID
    INNER JOIN CMS_Member m ON mbm.MemberBadgeMemberMemberId = m.MemberID
WHERE mb.MemberBadgeCodeName = 'KenticoEmployee'
    AND m.MemberEmail NOT LIKE '%@kentico.com'
export interface MemberBadgeAssigmentModel {
  memberBadgeID: number;
  memberBadgeDescription: string;
  memberBadgeDisplayName: string;
  memberBadgeCodeName: string;
  badgeImageRelativePath: string | null;
  isAssigned: boolean;
}

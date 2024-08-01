export interface MemberBadgeAssigmentModel {
  memberBadgeID: number;
  memberBadgeDescription: string;
  memberBadgeDisplayName: string;
  badgeImageRelativePath: string | null;
  isAssigned: boolean;
}

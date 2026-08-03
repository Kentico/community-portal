import { type FormComponentProps } from '@kentico/xperience-admin-base';
import React, { useEffect, useState } from 'react';
import { IoCheckmarkCircle } from 'react-icons/io5';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '../../../components/ui/card';
import { Badge } from '../../../components/ui/badge';
import { MemberBadgeAssigmentModel } from './MemberBadgeAssignmentModel';

export type MemberBadgesRuleAssignedListComponentClientProperties =
  FormComponentProps<MemberBadgeAssigmentModel[]>;

export const MemberBadgesRuleAssignedListFormComponent = (
  props: MemberBadgesRuleAssignedListComponentClientProperties,
) => {
  const [badges, setBadges] = useState<MemberBadgeAssigmentModel[]>([]);

  useEffect(() => {
    setBadges([...props.value]);
  }, [props.value]);

  const toggleBadge = (badgeId: number): void => {
    const updatedBadges = badges.map((badge) =>
      badge.memberBadgeID === badgeId
        ? { ...badge, isAssigned: !badge.isAssigned }
        : badge,
    );
    setBadges(updatedBadges);

    props.value.length = 0;
    props.value.push(...updatedBadges);

    if (props.onChange !== undefined) {
      props.onChange(props.value);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent, badgeId: number): void => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      toggleBadge(badgeId);
    }
  };

  const assigned = badges.filter((b) => b.isAssigned);
  const unassigned = badges.filter((b) => !b.isAssigned);

  return (
    <div className="member-badges-wrapper space-y-4">
      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-4">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Rule assigned badges
          </CardTitle>
          <p className="!text-sm !text-muted-foreground !mt-2">
            Click badges to assign or unassign
          </p>
          <p className="!text-sm !text-amber-600 !mt-1">
            Manually updated rule-assigned badges are not guaranteed to persist
            across badge assignment scheduled task runs.
          </p>
        </CardHeader>
        <CardContent className="!pt-0 space-y-6">
          <div className="space-y-3">
            <h3 className="!text-base !font-semibold !text-foreground !m-0">
              Assigned
            </h3>
            {assigned.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {assigned.map((b) => (
                  <Badge
                    key={b.memberBadgeID}
                    variant="default"
                    className="!px-3 !py-2 !text-sm gap-2 cursor-pointer relative hover:opacity-80 transition-opacity !bg-blue-600 !text-white !border-transparent"
                    title={b.memberBadgeDescription}
                    onClick={() => toggleBadge(b.memberBadgeID)}
                    onKeyDown={(e) => handleKeyDown(e, b.memberBadgeID)}
                    tabIndex={0}
                    role="button"
                    aria-pressed={true}
                  >
                    <IoCheckmarkCircle
                      className="absolute -top-1 -right-1"
                      style={{ color: '#22c55e', fontSize: '1rem' }}
                    />
                    {b.badgeImageRelativePath && (
                      <img
                        src={b.badgeImageRelativePath}
                        width={16}
                        height={16}
                        alt=""
                        className="inline-block"
                      />
                    )}
                    {b.memberBadgeDisplayName}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="!text-sm !text-muted-foreground !m-0">
                No badges assigned
              </p>
            )}
          </div>

          <div className="space-y-3">
            <h3 className="!text-base !font-semibold !text-foreground !m-0">
              Unassigned
            </h3>
            {unassigned.length > 0 ? (
              <div className="flex flex-wrap gap-2">
                {unassigned.map((b) => (
                  <Badge
                    key={b.memberBadgeID}
                    variant="outline"
                    className="!px-3 !py-2 !text-sm gap-2 cursor-pointer relative hover:opacity-80 transition-opacity !border-gray-300 !text-gray-700 !bg-gray-50"
                    title={b.memberBadgeDescription}
                    onClick={() => toggleBadge(b.memberBadgeID)}
                    onKeyDown={(e) => handleKeyDown(e, b.memberBadgeID)}
                    tabIndex={0}
                    role="button"
                    aria-pressed={false}
                  >
                    {b.badgeImageRelativePath && (
                      <img
                        src={b.badgeImageRelativePath}
                        width={16}
                        height={16}
                        alt=""
                        className="inline-block opacity-50"
                      />
                    )}
                    {b.memberBadgeDisplayName}
                  </Badge>
                ))}
              </div>
            ) : (
              <p className="!text-sm !text-muted-foreground !m-0">
                All badges are assigned
              </p>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

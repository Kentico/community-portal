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

export interface MemberBadgesAssignmentComponentClientProperties extends FormComponentProps {
  value: MemberBadgeAssigmentModel[];
}

export const MemberBadgesAssignmentFormComponent = (
  props: MemberBadgesAssignmentComponentClientProperties,
): JSX.Element => {
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

  return (
    <div className="member-badges-wrapper">
      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-4">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Manually assigned badges
          </CardTitle>
          <p className="!text-sm !text-muted-foreground !mt-2">
            Click badges to assign or unassign
          </p>
        </CardHeader>
        <CardContent className="!pt-0">
          <div className="flex flex-wrap gap-2">
            {badges.map((badge) => (
              <Badge
                key={badge.memberBadgeID}
                variant={badge.isAssigned ? 'default' : 'outline'}
                className="!px-3 !py-2 !text-sm gap-2 cursor-pointer relative hover:opacity-80 transition-opacity"
                style={
                  badge.isAssigned
                    ? {
                        backgroundColor: '#2563eb',
                        color: '#ffffff',
                        borderColor: 'transparent',
                      }
                    : undefined
                }
                onClick={() => toggleBadge(badge.memberBadgeID)}
                onKeyDown={(e) => handleKeyDown(e, badge.memberBadgeID)}
                title={badge.memberBadgeDescription}
                tabIndex={0}
                role="button"
                aria-pressed={badge.isAssigned}
              >
                {badge.isAssigned && (
                  <IoCheckmarkCircle
                    className="absolute -top-1 -right-1"
                    style={{ color: '#22c55e', fontSize: '1rem' }}
                  />
                )}
                {badge.badgeImageRelativePath && (
                  <img
                    src={badge.badgeImageRelativePath}
                    width={16}
                    height={16}
                    alt=""
                    className="inline-block"
                  />
                )}
                {badge.memberBadgeDisplayName}
              </Badge>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

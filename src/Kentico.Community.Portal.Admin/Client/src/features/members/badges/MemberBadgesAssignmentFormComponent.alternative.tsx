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
import { Checkbox } from '../../../components/ui/checkbox';
import { MemberBadgeAssigmentModel } from './MemberBadgeAssignmentModel';

export interface MemberBadgesAssignmentComponentClientProperties extends FormComponentProps {
  value: MemberBadgeAssigmentModel[];
}

/**
 * Alternative implementation using shadcn/ui components
 * Displays badges in a grid with direct toggle interaction
 *
 * Benefits:
 * - All badges visible at once (no dropdown)
 * - Direct manipulation (click to toggle)
 * - Consistent with shadcn/ui usage elsewhere
 * - Visual icons are prominent
 * - No external select library needed
 */
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

    // Update parent component
    props.value.length = 0;
    props.value.push(...updatedBadges);

    if (props.onChange !== undefined) {
      props.onChange(props.value);
    }
  };

  return (
    <div className="member-badges-wrapper">
      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-4">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Manually assigned badges
          </CardTitle>
        </CardHeader>
        <CardContent className="!pt-0">
          <div className="flex flex-wrap gap-3">
            {badges.map((badge) => (
              <div
                key={badge.memberBadgeID}
                className="flex items-center gap-2 group"
              >
                {/* Option 1: Checkbox + Badge */}
                <Checkbox
                  id={`badge-${badge.memberBadgeID}`}
                  checked={badge.isAssigned}
                  onCheckedChange={() => toggleBadge(badge.memberBadgeID)}
                  className="!cursor-pointer"
                />
                <label
                  htmlFor={`badge-${badge.memberBadgeID}`}
                  className="cursor-pointer"
                >
                  <Badge
                    variant={badge.isAssigned ? 'default' : 'outline'}
                    className={`!px-3 !py-2 !text-sm gap-2 transition-all ${
                      badge.isAssigned
                        ? '!bg-blue-600 !text-white !border-transparent'
                        : '!border-gray-300 !text-gray-700 !bg-gray-50 hover:!bg-gray-100'
                    }`}
                    title={badge.memberBadgeDescription}
                  >
                    {badge.badgeImageRelativePath && (
                      <img
                        src={badge.badgeImageRelativePath}
                        width={16}
                        height={16}
                        alt=""
                        className={`inline-block ${!badge.isAssigned ? 'opacity-50' : ''}`}
                      />
                    )}
                    {badge.memberBadgeDisplayName}
                  </Badge>
                </label>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

/**
 * Alternative: Clickable badges without separate checkbox
 * More compact, direct click-to-toggle
 */
export const MemberBadgesAssignmentFormComponentClickable = (
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

  return (
    <div className="member-badges-wrapper">
      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-4">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Manually assigned badges
          </CardTitle>
          <p className="!text-sm !text-muted-foreground !mt-1 !mb-0">
            Click badges to assign or unassign
          </p>
        </CardHeader>
        <CardContent className="!pt-0">
          <div className="flex flex-wrap gap-2">
            {badges.map((badge) => (
              <Badge
                key={badge.memberBadgeID}
                variant={badge.isAssigned ? 'default' : 'outline'}
                className={`!px-3 !py-2 !text-sm gap-2 cursor-pointer transition-all relative ${
                  badge.isAssigned
                    ? '!bg-blue-600 !text-white !border-transparent hover:!bg-blue-700'
                    : '!border-gray-300 !text-gray-700 !bg-gray-50 hover:!bg-gray-100 hover:!border-gray-400'
                }`}
                title={badge.memberBadgeDescription}
                onClick={() => toggleBadge(badge.memberBadgeID)}
              >
                {badge.isAssigned && (
                  <IoCheckmarkCircle
                    className="absolute -top-1 -right-1 !text-green-500 !bg-white rounded-full"
                    size={16}
                  />
                )}
                {badge.badgeImageRelativePath && (
                  <img
                    src={badge.badgeImageRelativePath}
                    width={16}
                    height={16}
                    alt=""
                    className={`inline-block ${!badge.isAssigned ? 'opacity-50' : ''}`}
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

/**
 * Alternative: Using Switch components
 * More explicit on/off toggle affordance
 */
export const MemberBadgesAssignmentFormComponentSwitch = (
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

  return (
    <div className="member-badges-wrapper">
      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-4">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Manually assigned badges
          </CardTitle>
        </CardHeader>
        <CardContent className="!pt-0">
          <div className="space-y-3">
            {badges.map((badge) => (
              <div
                key={badge.memberBadgeID}
                className="flex items-center justify-between p-3 rounded-lg border border-gray-200 hover:bg-gray-50 transition-colors"
              >
                <div className="flex items-center gap-3">
                  {badge.badgeImageRelativePath && (
                    <img
                      src={badge.badgeImageRelativePath}
                      width={24}
                      height={24}
                      alt=""
                      className={!badge.isAssigned ? 'opacity-50' : ''}
                    />
                  )}
                  <div>
                    <div className="font-medium text-sm">
                      {badge.memberBadgeDisplayName}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {badge.memberBadgeDescription}
                    </div>
                  </div>
                </div>
                {/* Note: You'll need to add Switch component from shadcn */}
                <input
                  type="checkbox"
                  checked={badge.isAssigned}
                  onChange={() => toggleBadge(badge.memberBadgeID)}
                  className="cursor-pointer"
                />
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

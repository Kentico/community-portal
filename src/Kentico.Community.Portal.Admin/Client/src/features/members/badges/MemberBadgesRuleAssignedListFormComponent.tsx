import { type FormComponentProps } from '@kentico/xperience-admin-base';
import React from 'react';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '../../../components/ui/card';
import { Badge } from '../../../components/ui/badge';
import { MemberBadgeAssigmentModel } from './MemberBadgeAssignmentModel';

export interface MemberBadgesRuleAssignedListComponentClientProperties extends FormComponentProps {
  value: MemberBadgeAssigmentModel[];
}

export const MemberBadgesRuleAssignedListFormComponent = (
  props: MemberBadgesRuleAssignedListComponentClientProperties,
) => {
  const assigned = props.value.filter((b) => b.isAssigned);
  const unassigned = props.value.filter((b) => !b.isAssigned);

  return (
    <div className="member-badges-wrapper space-y-4">
      <Card className="!bg-card !border-border">
        <CardHeader className="!pb-4">
          <CardTitle className="!text-xl !font-semibold !text-card-foreground !m-0">
            Rule assigned badges
          </CardTitle>
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
                    className="!px-3 !py-2 !text-sm gap-2 cursor-help !bg-blue-600 !text-white !border-transparent"
                    title={b.memberBadgeDescription}
                  >
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
                    className="!px-3 !py-2 !text-sm gap-2 cursor-help !border-gray-300 !text-gray-700 !bg-gray-50"
                    title={b.memberBadgeDescription}
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

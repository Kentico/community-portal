import {
  Colors,
  Headline,
  HeadlineSize,
  LabelWithTooltip,
} from '@kentico/xperience-admin-components';
import React from 'react';
import { MemberBadgeAssigmentModel } from './MemberBadgeAssignmentModel';

export interface MemberBadgesRuleAssignedListComponentClientProperties
  extends FormComponentProps {
  value: MemberBadgeAssigmentModel[];
}

export const MemberBadgesRuleAssignedListFormComponent = (
  props: MemberBadgesRuleAssignedListComponentClientProperties,
) => {
  const assigned = props.value.filter((b) => b.isAssigned);
  const unassigned = props.value.filter((b) => !b.isAssigned);

  return (
    <div>
      <h1>
        <Headline size={HeadlineSize.L} labelColor={Colors.TextDefaultOnLight}>
          Rule assigned badges
        </Headline>
      </h1>

      <h3>
        <Headline size={HeadlineSize.M} labelColor={Colors.TextDefaultOnLight}>
          Assigned
        </Headline>
      </h3>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
        {assigned.map((b) => (
          <div key={b.memberBadgeID}>
            <LabelWithTooltip
              label={b.memberBadgeDisplayName}
              tooltipText={b.memberBadgeDescription}
            />
            <img src={b.badgeImageRelativePath} width={30} height={30} />
          </div>
        ))}
      </div>

      <h3>
        <Headline size={HeadlineSize.M} labelColor={Colors.TextDefaultOnLight}>
          Unassigned
        </Headline>
      </h3>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
        {unassigned.map((b) => (
          <div key={b.memberBadgeID}>
            <LabelWithTooltip
              label={b.memberBadgeDisplayName}
              tooltipText={b.memberBadgeDescription}
            />
            <img src={b.badgeImageRelativePath} width={30} height={30} />
          </div>
        ))}
      </div>
    </div>
  );
};

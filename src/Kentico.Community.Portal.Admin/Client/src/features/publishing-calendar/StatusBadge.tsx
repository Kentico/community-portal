import { Icon } from '@kentico/xperience-admin-components';
import React from 'react';
import {
  type PublishingCalendarEvent,
  getStatusIcon,
  getStatusLabel,
} from './calendarModel';

export const StatusBadge = ({ event }: { event: PublishingCalendarEvent }) => (
  <span
    className={`publishing-calendar__badge publishing-calendar__status--${event.status}`}
    data-testid="publishing-calendar-status-badge"
    data-status={event.status}
  >
    <Icon name={getStatusIcon(event)} />
    {getStatusLabel(event)}
  </span>
);

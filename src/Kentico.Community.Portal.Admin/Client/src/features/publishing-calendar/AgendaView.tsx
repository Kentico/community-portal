import {
  Button,
  ButtonColor,
  ButtonSize,
  Icon,
  type TestId,
} from '@kentico/xperience-admin-components';
import React from 'react';
import {
  type CalendarData,
  type PublishingCalendarEvent,
  type PublishingStatus,
  formatDayHeading,
  formatTime,
  getChannelIcon,
  getContentTypeIcon,
  getDayGroups,
  getStatus,
  groupKey,
  sortByDate,
} from './calendarModel';
import { StatusBadge } from './StatusBadge';

interface AgendaViewProps {
  dayKeys: string[];
  data: CalendarData;
  statuses: PublishingStatus[];
  selectedEventId: string | null;
  focusDay: string | null;
  loadingGroup: string | null;
  OnClearFocus: () => void;
  OnSelectEvent: (day: string, id: string) => void;
  OnLoadMore: (day: string, status: PublishingStatus) => void;
}

/** Chronological list of items with real times, grouped by day */
export const AgendaView = ({
  dayKeys,
  data,
  statuses,
  selectedEventId,
  focusDay,
  loadingGroup,
  OnClearFocus: onClearFocus,
  OnSelectEvent: onSelectEvent,
  OnLoadMore: onLoadMore,
}: AgendaViewProps) => {
  const days = (focusDay ? [focusDay] : dayKeys)
    .map((day) => ({ day, groups: getDayGroups(data, day, statuses) }))
    .filter((d) => d.groups.length > 0);

  return (
    <div
      className="publishing-calendar__agenda"
      data-testid="publishing-calendar-agenda"
      data-focus-day={focusDay ?? ''}
    >
      {focusDay && (
        <div
          className="publishing-calendar__agenda-focus"
          data-testid="publishing-calendar-agenda-focus"
        >
          <span>
            <Icon name="xp-calendar-number" />
            {formatDayHeading(focusDay)}
          </span>
          <Button
            label="Show whole month"
            size={ButtonSize.S}
            color={ButtonColor.Tertiary}
            onClick={onClearFocus}
            dataTestId={'publishing-calendar-agenda-clear-focus' as TestId}
          />
        </div>
      )}

      {days.length === 0 && (
        <div
          className="publishing-calendar__empty"
          data-testid="publishing-calendar-agenda-empty"
        >
          <Icon name="xp-calendar" />
          <p>Nothing scheduled or changed in this period.</p>
        </div>
      )}

      {days.map(({ day, groups }) => {
        const items = ([] as PublishingCalendarEvent[])
          .concat(...groups.map((g) => g.items))
          .sort(sortByDate);
        const total = groups.reduce((sum, g) => sum + g.total, 0);
        const capped = groups.filter((g) => g.total > g.items.length);
        return (
          <section
            key={day}
            className="publishing-calendar__agenda-day"
            data-testid="publishing-calendar-agenda-day"
            data-date={day}
          >
            <h3 className="publishing-calendar__agenda-day-heading">
              <span>{formatDayHeading(day)}</span>
              <span className="publishing-calendar__muted">
                {total} {total === 1 ? 'item' : 'items'}
              </span>
            </h3>
            <ul className="publishing-calendar__agenda-list">
              {items.map((e) => (
                <li key={e.id}>
                  <button
                    type="button"
                    className={`publishing-calendar__agenda-item publishing-calendar__status--${e.status}${e.id === selectedEventId ? ' publishing-calendar__agenda-item--selected' : ''}`}
                    onClick={() => onSelectEvent(day, e.id)}
                    data-testid="publishing-calendar-agenda-item"
                    data-status={e.status}
                  >
                    <time
                      className="publishing-calendar__agenda-time"
                      dateTime={e.date}
                      data-testid="publishing-calendar-agenda-time"
                    >
                      {formatTime(e.date)}
                    </time>
                    <span className="publishing-calendar__agenda-status">
                      <StatusBadge event={e} />
                    </span>
                    <span className="publishing-calendar__agenda-title">
                      {e.title}
                    </span>
                    <span className="publishing-calendar__agenda-meta">
                      <span>
                        <Icon
                          name={getContentTypeIcon(e.contentTypeIcon)}
                          fallbackIconName="xp-doc"
                        />
                        {e.contentType}
                      </span>
                      <span>
                        <Icon name={getChannelIcon(e.channelType)} />
                        {e.channelName || 'Content hub'}
                      </span>
                      {e.workspaceName && (
                        <span>
                          <Icon name="xp-folder" />
                          {e.workspaceName}
                        </span>
                      )}
                      {e.modifiedBy && (
                        <span>
                          <Icon name="xp-user" />
                          {e.modifiedBy}
                        </span>
                      )}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
            {capped.map((g) => (
              <div
                key={g.status}
                className="publishing-calendar__load-more"
                data-testid="publishing-calendar-agenda-load-more"
                data-status={g.status}
              >
                <span className="publishing-calendar__muted">
                  Showing {g.items.length} of {g.total}{' '}
                  {getStatus(g.status).label.toLowerCase()}
                </span>
                <Button
                  label="Load more"
                  size={ButtonSize.S}
                  color={ButtonColor.Secondary}
                  inProgress={loadingGroup === groupKey(day, g.status)}
                  onClick={() => onLoadMore(day, g.status)}
                />
              </div>
            ))}
          </section>
        );
      })}
    </div>
  );
};

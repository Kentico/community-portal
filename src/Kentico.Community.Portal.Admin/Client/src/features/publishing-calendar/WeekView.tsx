import { Icon } from '@kentico/xperience-admin-components';
import React from 'react';
import {
  type CalendarData,
  type PublishingCalendarEvent,
  type PublishingStatus,
  formatTime,
  fromDayKey,
  getContentTypeIcon,
  getDayGroups,
  getStatus,
  getStatusIcon,
  getStatusLabel,
  sortByDate,
} from './calendarModel';

interface WeekViewProps {
  dayKeys: string[];
  data: CalendarData;
  statuses: PublishingStatus[];
  selectedDay: string;
  selectedEventId: string | null;
  todayKey: string;
  OnSelectDay: (day: string) => void;
  OnSelectEvent: (day: string, id: string) => void;
  OnShowMore: (day: string) => void;
}

/** One row of day columns, each a time-sorted list of compact item cards */
export const WeekView = ({
  dayKeys,
  data,
  statuses,
  selectedDay,
  selectedEventId,
  todayKey,
  OnSelectDay: onSelectDay,
  OnSelectEvent: onSelectEvent,
  OnShowMore: onShowMore,
}: WeekViewProps) => (
  <div
    className="publishing-calendar__week"
    data-testid="publishing-calendar-week"
  >
    {dayKeys.map((day) => {
      const date = fromDayKey(day);
      const groups = getDayGroups(data, day, statuses);
      const items = ([] as PublishingCalendarEvent[])
        .concat(...groups.map((g) => g.items))
        .sort(sortByDate);
      const total = groups.reduce((sum, g) => sum + g.total, 0);
      const capped = groups.filter((g) => g.total > g.items.length);
      const classNames = [
        'publishing-calendar__week-day',
        day === todayKey ? 'publishing-calendar__week-day--today' : '',
        day === selectedDay ? 'publishing-calendar__day--selected' : '',
      ];
      return (
        <section
          key={day}
          className={classNames.filter(Boolean).join(' ')}
          aria-label={date.toLocaleDateString(undefined, {
            weekday: 'long',
            month: 'long',
            day: 'numeric',
          })}
          data-testid="publishing-calendar-week-day"
          data-date={day}
        >
          <button
            type="button"
            className="publishing-calendar__week-day-header"
            onClick={() => onSelectDay(day)}
          >
            <span className="publishing-calendar__week-day-name">
              {date.toLocaleDateString(undefined, { weekday: 'short' })}
            </span>
            <span className="publishing-calendar__week-day-number">
              {date.getDate()}
            </span>
            <span className="publishing-calendar__week-day-total">
              {total > 0 ? total : ''}
            </span>
          </button>
          <ul className="publishing-calendar__week-list">
            {items.map((e) => (
              <li key={e.id}>
                <button
                  type="button"
                  className={`publishing-calendar__week-item publishing-calendar__status--${e.status}${e.id === selectedEventId ? ' publishing-calendar__week-item--selected' : ''}`}
                  title={`${getStatusLabel(e)}: ${e.title} (${e.contentType})`}
                  onClick={() => onSelectEvent(day, e.id)}
                  data-testid="publishing-calendar-week-item"
                  data-status={e.status}
                >
                  <span className="publishing-calendar__week-item-top">
                    <span className="publishing-calendar__status-icon">
                      <Icon name={getStatusIcon(e)} />
                    </span>
                    <time
                      className="publishing-calendar__event-time"
                      dateTime={e.date}
                    >
                      {formatTime(e.date)}
                    </time>
                    <span className="publishing-calendar__week-item-type">
                      <Icon
                        name={getContentTypeIcon(e.contentTypeIcon)}
                        fallbackIconName="xp-doc"
                      />
                    </span>
                  </span>
                  <span className="publishing-calendar__week-item-title">
                    {e.title}
                  </span>
                </button>
              </li>
            ))}
          </ul>
          {capped.length > 0 && (
            <div className="publishing-calendar__week-more">
              {capped.map((g) => (
                <button
                  key={g.status}
                  type="button"
                  className={`publishing-calendar__more-link publishing-calendar__status--${g.status}`}
                  title={`${g.total - g.items.length} more ${getStatus(g.status).label} — open in agenda`}
                  onClick={() => onShowMore(day)}
                  data-testid="publishing-calendar-week-more"
                  data-status={g.status}
                >
                  <Icon name={getStatus(g.status).icon} />+
                  {g.total - g.items.length} more
                </button>
              ))}
            </div>
          )}
        </section>
      );
    })}
  </div>
);

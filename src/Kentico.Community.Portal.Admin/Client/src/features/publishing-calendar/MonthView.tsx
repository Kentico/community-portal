import { Icon } from '@kentico/xperience-admin-components';
import React from 'react';
import {
  type CalendarData,
  type PublishingStatus,
  fromDayKey,
  getCount,
  getStatus,
  weekdayLabels,
} from './calendarModel';

interface MonthViewProps {
  dayKeys: string[];
  month: number;
  data: CalendarData;
  statuses: PublishingStatus[];
  selectedDay: string;
  todayKey: string;
  OnSelectDay: (day: string, status?: PublishingStatus) => void;
}

/** High-level overview: per-status count badges per day, no item lists */
export const MonthView = ({
  dayKeys,
  month,
  data,
  statuses,
  selectedDay,
  todayKey,
  OnSelectDay: onSelectDay,
}: MonthViewProps) => (
  <div
    className="publishing-calendar__month"
    role="grid"
    data-testid="publishing-calendar-month"
  >
    <div className="publishing-calendar__month-row" role="row">
      {weekdayLabels.map((label) => (
        <div
          key={label}
          className="publishing-calendar__weekday"
          role="columnheader"
        >
          {label}
        </div>
      ))}
    </div>
    <div className="publishing-calendar__month-days">
      {dayKeys.map((day) => {
        const date = fromDayKey(day);
        const dayStatuses = statuses.filter((s) => getCount(data, day, s) > 0);
        const classNames = [
          'publishing-calendar__month-day',
          date.getMonth() !== month
            ? 'publishing-calendar__month-day--other'
            : '',
          day === todayKey ? 'publishing-calendar__month-day--today' : '',
          day === selectedDay ? 'publishing-calendar__day--selected' : '',
        ];
        return (
          <div
            key={day}
            className={classNames.filter(Boolean).join(' ')}
            role="gridcell"
            aria-selected={day === selectedDay}
            onClick={() => onSelectDay(day)}
            data-testid="publishing-calendar-month-day"
            data-date={day}
          >
            <button
              type="button"
              className="publishing-calendar__month-day-number"
              onClick={(e) => {
                e.stopPropagation();
                onSelectDay(day);
              }}
              aria-label={date.toLocaleDateString(undefined, {
                weekday: 'long',
                month: 'long',
                day: 'numeric',
                year: 'numeric',
              })}
            >
              {date.getDate()}
            </button>
            <div className="publishing-calendar__month-badges">
              {dayStatuses.map((status) => {
                const definition = getStatus(status);
                const count = getCount(data, day, status);
                return (
                  <button
                    key={status}
                    type="button"
                    className={`publishing-calendar__count-badge publishing-calendar__status--${status}`}
                    title={`${count} ${definition.label}`}
                    aria-label={`${count} ${definition.label}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      onSelectDay(day, status);
                    }}
                    data-testid="publishing-calendar-day-status-count"
                    data-status={status}
                    data-count={count}
                  >
                    <Icon name={definition.icon} />
                    {count}
                  </button>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  </div>
);

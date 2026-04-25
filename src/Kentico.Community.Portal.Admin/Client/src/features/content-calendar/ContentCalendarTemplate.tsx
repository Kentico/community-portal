import FullCalendar from '@fullcalendar/react';
import DayGridPlugin from '@fullcalendar/daygrid';
import TimeGridPlugin from '@fullcalendar/timegrid';
import ListPlugin from '@fullcalendar/list';
import InteractionPlugin from '@fullcalendar/interaction';
import { usePageCommand } from '@kentico/xperience-admin-base';
import { Icon } from '@kentico/xperience-admin-components';
import React, { useCallback, useRef, useState } from 'react';
import './content-calendar.css';

type ContentItemStatus = 'draft' | 'scheduled' | 'unpublish-scheduled';

interface ContentCalendarEvent {
  title: string;
  date: string;
  contentType: string;
  contentTypeType: string;
  status: ContentItemStatus;
  workflowStepName: string;
  workflowStepIcon: string;
  workflowName: string;
  editUrl: string;
}

interface ContentCalendarClientProperties {
  loadEventsCommandName: string;
  initialEvents: ContentCalendarEvent[];
}

interface FullCalendarEvent {
  title: string;
  start: string;
  allDay: boolean;
  extendedProps: ContentCalendarEvent;
}

function makeEventKey(start: string, title: string, status: string): string {
  return JSON.stringify({ start, title, status });
}

function toFullCalendarEvents(events: ContentCalendarEvent[]): FullCalendarEvent[] {
  return events.map((e) => ({
    title: e.title,
    start: e.date,
    allDay: true,
    extendedProps: e,
  }));
}

function getStatusIcon(event: ContentCalendarEvent): string {
  if (event.status === 'scheduled') {
    return 'xp-calendar-number';
  }
  if (event.status === 'unpublish-scheduled') {
    return 'xp-clock';
  }
  if (event.workflowStepIcon) {
    return event.workflowStepIcon;
  }
  return 'xp-two-rectangles-stacked';
}

function getStatusColor(status: ContentItemStatus): string {
  switch (status) {
    case 'scheduled':
      return 'var(--color-text-info-on-light)';
    case 'unpublish-scheduled':
      return 'var(--color-warning-text)';
    case 'draft':
    default:
      return 'var(--color-text-low-emphasis)';
  }
}

function getStatusLabel(event: ContentCalendarEvent): string {
  if (event.status === 'scheduled') return 'Scheduled to publish';
  if (event.status === 'unpublish-scheduled') return 'Scheduled to unpublish';
  if (event.workflowStepName) return event.workflowStepName;
  return 'Draft';
}

type TooltipState = {
  event: ContentCalendarEvent;
  x: number;
  y: number;
} | null;

export const ContentCalendarTemplate = (props: ContentCalendarClientProperties) => {
  const [events, setEvents] = useState<FullCalendarEvent[]>(() =>
    toFullCalendarEvents(props.initialEvents),
  );
  const [tooltip, setTooltip] = useState<TooltipState>(null);

  const loadedRangesRef = useRef<Set<string>>(new Set());

  const { execute: loadEvents } = usePageCommand<ContentCalendarEvent[]>(
    props.loadEventsCommandName,
    {
      after: (result) => {
        if (result) {
          setEvents((prev) => {
            const existing = new Set(
              prev.map((e) => makeEventKey(e.start, e.title, e.extendedProps.status)),
            );
            const next = toFullCalendarEvents(result).filter(
              (e) => !existing.has(makeEventKey(e.start, e.title, e.extendedProps.status)),
            );
            return [...prev, ...next];
          });
        }
      },
    },
  );

  const handleDatesSet = useCallback(
    (info: { start: Date; end: Date }) => {
      const key = `${info.start.toISOString()}|${info.end.toISOString()}`;
      if (loadedRangesRef.current.has(key)) return;
      loadedRangesRef.current.add(key);
      void loadEvents({ start: info.start.toISOString(), end: info.end.toISOString() });
    },
    [loadEvents],
  );

  const handleEventClick = useCallback(
    (info: { event: { extendedProps: Record<string, unknown> }; jsEvent: MouseEvent }) => {
      setTooltip({
        event: info.event.extendedProps as ContentCalendarEvent,
        x: info.jsEvent.clientX,
        y: info.jsEvent.clientY,
      });
    },
    [],
  );

  const closeTooltip = useCallback(() => setTooltip(null), []);

  return (
    <div className="content-calendar" onClick={closeTooltip}>
      {tooltip !== null && (
        <div
          className="content-calendar__tooltip-wrapper"
          style={{ left: tooltip.x, top: tooltip.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="content-calendar__tooltip">
            <button
              className="content-calendar__tooltip-close"
              onClick={closeTooltip}
              aria-label="Close"
            >
              ×
            </button>
            <div className="content-calendar__tooltip-title">{tooltip.event.title}</div>
            <div className="content-calendar__tooltip-row">
              <span className="content-calendar__tooltip-label">Type:</span>
              <span>
                {tooltip.event.contentType}
                {tooltip.event.contentTypeType ? ` (${tooltip.event.contentTypeType})` : ''}
              </span>
            </div>
            <div
              className="content-calendar__tooltip-row"
              style={{ color: getStatusColor(tooltip.event.status) }}
            >
              <Icon name={getStatusIcon(tooltip.event)} />
              <span>{getStatusLabel(tooltip.event)}</span>
            </div>
            {(tooltip.event.status === 'scheduled' ||
              tooltip.event.status === 'unpublish-scheduled') && (
              <div className="content-calendar__tooltip-row">
                <span className="content-calendar__tooltip-label">Date:</span>
                <span>
                  {new Date(tooltip.event.date).toLocaleString(undefined, {
                    dateStyle: 'medium',
                    timeStyle: 'short',
                  })}
                </span>
              </div>
            )}
            {tooltip.event.workflowName && (
              <div className="content-calendar__tooltip-row">
                <span className="content-calendar__tooltip-label">Workflow:</span>
                <span>{tooltip.event.workflowName}</span>
              </div>
            )}
            {tooltip.event.editUrl && (
              <div className="content-calendar__tooltip-row">
                <a
                  className="content-calendar__tooltip-edit-link"
                  href={tooltip.event.editUrl}
                >
                  Open
                </a>
              </div>
            )}
          </div>
        </div>
      )}

      <FullCalendar
        plugins={[DayGridPlugin, TimeGridPlugin, ListPlugin, InteractionPlugin]}
        headerToolbar={{
          left: 'prev,next today',
          center: 'title',
          right: 'dayGridMonth,timeGridWeek,timeGridDay,listMonth,listDay',
        }}
        buttonText={{
          today: 'today',
          month: 'month',
          week: 'week',
          day: 'day',
          list: 'list',
        }}
        initialView="dayGridMonth"
        navLinks={true}
        navLinkDayClick="listDay"
        dayMaxEvents={10}
        moreLinkClick="day"
        events={events}
        datesSet={handleDatesSet}
        eventClick={handleEventClick}
        eventContent={(arg) => {
          const extProps = arg.event.extendedProps as ContentCalendarEvent;
          const statusIcon = getStatusIcon(extProps);
          const statusColor = getStatusColor(extProps.status);
          return (
            <div className="content-calendar__event">
              <span style={{ color: statusColor }}>
                <Icon name={statusIcon} />
              </span>
              <span className="content-calendar__event-title">{arg.event.title}</span>
            </div>
          );
        }}
        height="auto"
      />
    </div>
  );
};

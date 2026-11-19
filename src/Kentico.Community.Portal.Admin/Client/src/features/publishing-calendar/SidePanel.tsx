import {
  Button,
  ButtonColor,
  ButtonSize,
  Headline,
  HeadlineSize,
  Icon,
  type IconName,
  LinkButton,
  type TestId,
} from '@kentico/xperience-admin-components';
import React, { useEffect, useRef } from 'react';
import {
  type ItemGroup,
  type PublishingCalendarEvent,
  type PublishingStatus,
  formatDateTime,
  formatDayHeading,
  formatTime,
  getChannelIcon,
  getContentTypeIcon,
  getStatus,
  getVersionStatusLabel,
  groupKey,
} from './calendarModel';
import { StatusBadge } from './StatusBadge';

interface DayPanelProps {
  dayKey: string;
  groups: ItemGroup[];
  focusedStatus: PublishingStatus | null;
  loadingGroup: string | null;
  OnSelectEvent: (id: string) => void;
  OnLoadMore: (status: PublishingStatus) => void;
}

/** Items of the selected day, grouped by status, with paging for capped groups */
export const DayPanel = ({
  dayKey,
  groups,
  focusedStatus,
  loadingGroup,
  OnSelectEvent: onSelectEvent,
  OnLoadMore: onLoadMore,
}: DayPanelProps) => {
  const groupRefs = useRef(new Map<PublishingStatus, HTMLElement>());
  const total = groups.reduce((sum, g) => sum + g.total, 0);

  useEffect(() => {
    const group = focusedStatus ? groupRefs.current.get(focusedStatus) : null;
    // Scroll only the panel (scrollIntoView would also scroll the page)
    const container = group?.closest('.publishing-calendar__panel-scroll');
    if (group && container) {
      container.scrollTo({ top: group.offsetTop - 8, behavior: 'smooth' });
    }
  }, [focusedStatus, dayKey]);

  return (
    <div
      className="publishing-calendar__day"
      data-testid="publishing-calendar-day"
      data-date={dayKey}
    >
      <div className="publishing-calendar__panel-header">
        <span className="publishing-calendar__panel-eyebrow">
          {total} {total === 1 ? 'item' : 'items'}
        </span>
        <Headline size={HeadlineSize.M}>{formatDayHeading(dayKey)}</Headline>
      </div>

      {groups.length === 0 ? (
        <div
          className="publishing-calendar__empty"
          data-testid="publishing-calendar-day-empty"
        >
          <Icon name="xp-calendar" />
          <p>Nothing scheduled or changed on this day.</p>
          <p className="publishing-calendar__muted">
            Select another day in the calendar, or adjust the filters.
          </p>
        </div>
      ) : (
        groups.map((g) => {
          const definition = getStatus(g.status);
          return (
            <section
              key={g.status}
              ref={(el) => {
                if (el) {
                  groupRefs.current.set(g.status, el);
                } else {
                  groupRefs.current.delete(g.status);
                }
              }}
              className={`publishing-calendar__panel-group publishing-calendar__status--${g.status}${g.status === focusedStatus ? ' publishing-calendar__panel-group--focused' : ''}`}
              data-testid="publishing-calendar-panel-group"
              data-status={g.status}
              data-shown={g.items.length}
              data-total={g.total}
              data-focused={g.status === focusedStatus}
            >
              <h3 className="publishing-calendar__panel-group-heading">
                <span className="publishing-calendar__status-icon">
                  <Icon name={definition.icon} />
                </span>
                <span>{definition.label}</span>
                <span className="publishing-calendar__panel-group-count">
                  {g.items.length < g.total
                    ? `Showing ${g.items.length} of ${g.total}`
                    : g.total}
                </span>
              </h3>
              <ul className="publishing-calendar__day-list">
                {g.items.map((e) => (
                  <li key={e.id}>
                    <button
                      type="button"
                      className={`publishing-calendar__card publishing-calendar__status--${e.status}`}
                      onClick={() => onSelectEvent(e.id)}
                      data-testid="publishing-calendar-day-item"
                      data-status={e.status}
                    >
                      <span className="publishing-calendar__card-top">
                        <StatusBadge event={e} />
                        <time
                          className="publishing-calendar__card-time"
                          dateTime={e.date}
                        >
                          {formatTime(e.date)}
                        </time>
                      </span>
                      <span className="publishing-calendar__card-title">
                        {e.title}
                      </span>
                      <span className="publishing-calendar__card-meta">
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
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
              {g.items.length < g.total && (
                <div className="publishing-calendar__load-more">
                  <Button
                    label={`Load more (${g.total - g.items.length} left)`}
                    size={ButtonSize.S}
                    color={ButtonColor.Secondary}
                    inProgress={loadingGroup === groupKey(dayKey, g.status)}
                    onClick={() => onLoadMore(g.status)}
                    dataTestId={'publishing-calendar-panel-load-more' as TestId}
                  />
                </div>
              )}
            </section>
          );
        })
      )}
    </div>
  );
};

const DetailRow = ({
  icon,
  label,
  children,
}: {
  icon: IconName;
  label: string;
  children: React.ReactNode;
}) => (
  <div className="publishing-calendar__detail-row">
    <dt>
      <Icon name={icon} />
      {label}
    </dt>
    <dd>{children}</dd>
  </div>
);

interface ItemDetailsProps {
  event: PublishingCalendarEvent;
  OnBack: () => void;
}

export const ItemDetails = ({ event, OnBack: onBack }: ItemDetailsProps) => (
  <div
    className="publishing-calendar__details"
    data-testid="publishing-calendar-item-details"
  >
    <div className="publishing-calendar__details-nav">
      <Button
        label="Back to day"
        icon="xp-arrow-left"
        size={ButtonSize.S}
        color={ButtonColor.Tertiary}
        onClick={onBack}
        dataTestId={'publishing-calendar-details-back' as TestId}
      />
    </div>

    <div className="publishing-calendar__panel-header">
      <StatusBadge event={event} />
      <Headline size={HeadlineSize.M}>{event.title}</Headline>
    </div>

    <dl className="publishing-calendar__detail-list">
      {event.scheduledPublishWhen && (
        <DetailRow icon="xp-calendar-number" label="Publishes">
          {formatDateTime(event.scheduledPublishWhen)}
        </DetailRow>
      )}
      {event.scheduledUnpublishWhen && (
        <DetailRow icon="xp-clock" label="Unpublishes">
          {formatDateTime(event.scheduledUnpublishWhen)}
        </DetailRow>
      )}
      <DetailRow
        icon={getContentTypeIcon(event.contentTypeIcon)}
        label="Content type"
      >
        {event.contentType}
      </DetailRow>
      <DetailRow icon={getChannelIcon(event.channelType)} label="Channel">
        {event.channelName
          ? `${event.channelName}${event.channelType ? ` (${event.channelType})` : ''}`
          : 'Content hub (reusable)'}
      </DetailRow>
      {event.workspaceName && (
        <DetailRow icon="xp-folder" label="Workspace">
          {event.workspaceName}
        </DetailRow>
      )}
      <DetailRow icon="xp-translate" label="Language">
        {event.languageDisplayName || event.languageName}
      </DetailRow>
      {event.workflowName && (
        <DetailRow icon="xp-scheme-path-circles" label="Workflow">
          {event.workflowName}
          {event.workflowStepName ? ` → ${event.workflowStepName}` : ''}
        </DetailRow>
      )}
      <DetailRow icon="xp-two-rectangles-stacked" label="Latest version">
        <span data-testid="publishing-calendar-version-status">
          {getVersionStatusLabel(event.versionStatus)}
        </span>
      </DetailRow>
      <DetailRow icon="xp-user" label="Last modified">
        {formatDateTime(event.modifiedWhen)}
        {event.modifiedBy ? ` by ${event.modifiedBy}` : ''}
      </DetailRow>
    </dl>

    {event.editUrl && (
      <div
        className="publishing-calendar__details-actions"
        data-testid="publishing-calendar-view-link"
      >
        <LinkButton
          href={event.editUrl}
          label="View item"
          icon="xp-eye"
          color={ButtonColor.Primary}
        />
      </div>
    )}
  </div>
);

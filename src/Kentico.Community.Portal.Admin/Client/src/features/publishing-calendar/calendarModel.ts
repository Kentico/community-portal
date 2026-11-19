import type { IconName } from '@kentico/xperience-admin-components';

export type PublishingStatus =
  | 'published'
  | 'scheduled-publish'
  | 'scheduled-unpublish'
  | 'in-workflow'
  | 'draft'
  | 'unpublished';

export type CalendarView = 'month' | 'week' | 'agenda';

export interface PublishingCalendarEvent {
  id: string;
  /** Local (browser) day key, yyyy-mm-dd */
  day: string;
  title: string;
  /** ISO instant with offset */
  date: string;
  status: PublishingStatus;
  versionStatus: string;
  contentType: string;
  contentTypeType: string;
  contentTypeIcon: string;
  channelName: string;
  channelType: string;
  workspaceName: string;
  languageName: string;
  languageDisplayName: string;
  workflowName: string;
  workflowStepName: string;
  workflowStepIcon: string;
  scheduledPublishWhen: string | null;
  scheduledUnpublishWhen: string | null;
  modifiedWhen: string;
  modifiedBy: string;
  editUrl: string;
}

export interface DayStatusCount {
  day: string;
  status: PublishingStatus;
  count: number;
}

export interface CalendarResult {
  counts: DayStatusCount[];
  items: PublishingCalendarEvent[];
}

export interface DayItemsResult {
  total: number;
  items: PublishingCalendarEvent[];
}

export interface ContentTypeOption {
  id: number;
  name: string;
  icon: string;
}

export interface ChannelOption {
  id: number;
  name: string;
  channelType: string;
}

export interface WorkflowStepOption {
  id: number;
  name: string;
  icon: string;
  /** Language variants currently in the step */
  count: number;
}

export interface WorkflowOption {
  id: number;
  name: string;
  steps: WorkflowStepOption[];
}

export interface CalendarFilters {
  contentTypeIds: number[];
  channelIds: number[];
  workflowStepIds: number[];
}

export const emptyFilters: CalendarFilters = {
  contentTypeIds: [],
  channelIds: [],
  workflowStepIds: [],
};

/** A browser-local day, sent to the server as instant boundaries */
export interface CalendarDayParam {
  key: string;
  start: string;
  end: string;
}

export interface StatusDefinition {
  status: PublishingStatus;
  label: string;
  description: string;
  icon: IconName;
}

/** Ordered by how much a marketer needs to act on it (upcoming first). */
export const statusDefinitions: StatusDefinition[] = [
  {
    status: 'scheduled-publish',
    label: 'Scheduled to publish',
    description: 'Will go live automatically at the scheduled time',
    icon: 'xp-calendar-number',
  },
  {
    status: 'scheduled-unpublish',
    label: 'Scheduled to unpublish',
    description: 'Will be removed automatically at the scheduled time',
    icon: 'xp-clock',
  },
  {
    status: 'in-workflow',
    label: 'In workflow',
    description: 'Waiting in a custom workflow step (e.g. review)',
    icon: 'xp-scheme-path-circles',
  },
  {
    status: 'draft',
    label: 'Draft',
    description: 'Edited but not yet published',
    icon: 'xp-edit',
  },
  {
    status: 'published',
    label: 'Published',
    description: 'Published (date of last change)',
    icon: 'xp-check-circle',
  },
  {
    status: 'unpublished',
    label: 'Unpublished',
    description: 'Removed from live content',
    icon: 'xp-eye-slash',
  },
];

const statusByKey = new Map(statusDefinitions.map((s) => [s.status, s]));

export function getStatus(status: PublishingStatus): StatusDefinition {
  return statusByKey.get(status) ?? statusDefinitions[3];
}

export function getStatusIcon(event: PublishingCalendarEvent): IconName {
  if (event.status === 'in-workflow' && event.workflowStepIcon) {
    return event.workflowStepIcon as IconName;
  }
  return getStatus(event.status).icon;
}

export function getStatusLabel(event: PublishingCalendarEvent): string {
  return event.status === 'in-workflow' && event.workflowStepName
    ? event.workflowStepName
    : getStatus(event.status).label;
}

export function getChannelIcon(channelType: string): IconName {
  switch (channelType) {
    case 'Website':
      return 'xp-earth';
    case 'Email':
      return 'xp-message';
    case 'Headless':
      return 'xp-headless';
    default:
      return 'xp-two-rectangles-stacked';
  }
}

export function getContentTypeIcon(icon: string): IconName {
  return (icon || 'xp-doc') as IconName;
}

const versionStatusLabels: Record<string, string> = {
  InitialDraft: 'Draft (never published)',
  Draft: 'Draft',
  Published: 'Published',
  Unpublished: 'Unpublished',
  Archived: 'Archived',
};

/** Human label for a CMS.ContentEngine.VersionStatus name */
export function getVersionStatusLabel(versionStatus: string): string {
  if (versionStatusLabels[versionStatus]) {
    return versionStatusLabels[versionStatus];
  }
  const words = versionStatus.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

/* Dates: all day math uses browser-local midnight Dates and yyyy-mm-dd keys */

export function toDayKey(date: Date): string {
  const y = date.getFullYear();
  const m = `0${date.getMonth() + 1}`.slice(-2);
  const d = `0${date.getDate()}`.slice(-2);
  return `${y}-${m}-${d}`;
}

export function fromDayKey(key: string): Date {
  const [y, m, d] = key.split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/** Monday on or before the date */
export function startOfWeek(date: Date): Date {
  return addDays(date, -((date.getDay() + 6) % 7));
}

export function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

export function dayKeysBetween(start: Date, endExclusive: Date): string[] {
  const keys: string[] = [];
  for (let d = start; d < endExclusive; d = addDays(d, 1)) {
    keys.push(toDayKey(d));
  }
  return keys;
}

export function monthDayKeys(anchor: Date): string[] {
  const start = startOfMonth(anchor);
  return dayKeysBetween(
    start,
    new Date(start.getFullYear(), start.getMonth() + 1, 1),
  );
}

/** Full weeks (Mon-Sun) covering the anchor's month */
export function monthGridDayKeys(anchor: Date): string[] {
  const first = startOfMonth(anchor);
  const nextMonth = new Date(first.getFullYear(), first.getMonth() + 1, 1);
  const end = addDays(startOfWeek(addDays(nextMonth, -1)), 7);
  return dayKeysBetween(startOfWeek(first), end);
}

export function weekDayKeys(anchor: Date): string[] {
  const start = startOfWeek(anchor);
  return dayKeysBetween(start, addDays(start, 7));
}

export function toDayParam(key: string): CalendarDayParam {
  const start = fromDayKey(key);
  return {
    key,
    start: start.toISOString(),
    end: addDays(start, 1).toISOString(),
  };
}

export function formatTime(value: string): string {
  return new Date(value).toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  });
}

export function formatDateTime(value: string | null): string {
  if (!value) {
    return '';
  }
  return new Date(value).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export function formatDayHeading(key: string): string {
  return fromDayKey(key).toLocaleDateString(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });
}

export function formatShortDay(key: string): string {
  return fromDayKey(key).toLocaleDateString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  });
}

export function formatRangeTitle(view: CalendarView, anchor: Date): string {
  if (view !== 'week') {
    return anchor.toLocaleDateString(undefined, {
      month: 'long',
      year: 'numeric',
    });
  }
  const start = startOfWeek(anchor);
  const end = addDays(start, 6);
  const startText = start.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
  });
  const endText = end.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
  return `${startText} – ${endText}`;
}

export function sortByDate(
  a: PublishingCalendarEvent,
  b: PublishingCalendarEvent,
): number {
  return new Date(a.date).getTime() - new Date(b.date).getTime();
}

export const weekdayLabels = weekDayKeys(new Date()).map((key) =>
  fromDayKey(key).toLocaleDateString(undefined, { weekday: 'short' }),
);

/* Loaded data: accurate counts + capped item groups per day/status */

export function groupKey(day: string, status: PublishingStatus): string {
  return `${day}|${status}`;
}

export interface CalendarData {
  counts: Map<string, number>;
  groups: Map<string, PublishingCalendarEvent[]>;
}

export interface ItemGroup {
  status: PublishingStatus;
  items: PublishingCalendarEvent[];
  total: number;
}

export function toCalendarData(result: CalendarResult): CalendarData {
  const counts = new Map<string, number>();
  result.counts.forEach((c) => counts.set(groupKey(c.day, c.status), c.count));
  const groups = new Map<string, PublishingCalendarEvent[]>();
  result.items.forEach((item) => {
    const key = groupKey(item.day, item.status);
    const list = groups.get(key) ?? [];
    list.push(item);
    groups.set(key, list);
  });
  groups.forEach((list) => list.sort(sortByDate));
  return { counts, groups };
}

export function appendGroupItems(
  data: CalendarData,
  day: string,
  status: PublishingStatus,
  result: DayItemsResult,
): CalendarData {
  const key = groupKey(day, status);
  const existing = data.groups.get(key) ?? [];
  const ids = new Set(existing.map((e) => e.id));
  const merged = [
    ...existing,
    ...result.items.filter((e) => !ids.has(e.id)),
  ].sort(sortByDate);
  const groups = new Map(data.groups);
  groups.set(key, merged);
  const counts = new Map(data.counts);
  counts.set(key, result.total);
  return { counts, groups };
}

export function getCount(
  data: CalendarData,
  day: string,
  status: PublishingStatus,
): number {
  return data.counts.get(groupKey(day, status)) ?? 0;
}

/** Groups with at least one item for the day, in status order */
export function getDayGroups(
  data: CalendarData,
  day: string,
  statuses: PublishingStatus[],
): ItemGroup[] {
  return statuses
    .map((status) => ({
      status,
      items: data.groups.get(groupKey(day, status)) ?? [],
      total: getCount(data, day, status),
    }))
    .filter((g) => g.total > 0);
}

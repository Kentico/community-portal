import { usePageCommandProvider } from '@kentico/xperience-admin-base';
import {
  Button,
  ButtonColor,
  ButtonSize,
  Headline,
  HeadlineSize,
  type IconName,
  Spinner,
  type TestId,
} from '@kentico/xperience-admin-components';
import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { AgendaView } from './AgendaView';
import {
  type CalendarData,
  type CalendarFilters,
  type CalendarResult,
  type CalendarView,
  type ChannelOption,
  type ContentTypeOption,
  type DayItemsResult,
  type PublishingStatus,
  type WorkflowOption,
  addDays,
  appendGroupItems,
  emptyFilters,
  formatRangeTitle,
  fromDayKey,
  getChannelIcon,
  getContentTypeIcon,
  getCount,
  getDayGroups,
  groupKey,
  monthDayKeys,
  monthGridDayKeys,
  startOfMonth,
  statusDefinitions,
  toCalendarData,
  toDayKey,
  toDayParam,
  weekDayKeys,
} from './calendarModel';
import { MonthView } from './MonthView';
import { FilterPanel } from './FilterPanel';
import { type FilterOption, type FilterOptionGroup } from './MultiSelectFilter';
import { DayPanel, ItemDetails } from './SidePanel';
import { WeekView } from './WeekView';
import './publishing-calendar.css';

interface PublishingCalendarClientProperties {
  loadCalendarCommandName: string;
  loadDayItemsCommandName: string;
  dayItemsPageSize: number;
  contentTypes: ContentTypeOption[];
  channels: ChannelOption[];
  workflows: WorkflowOption[];
}

const emptyData: CalendarData = { counts: new Map(), groups: new Map() };

const views: { id: CalendarView; label: string }[] = [
  { id: 'month', label: 'Month' },
  { id: 'week', label: 'Week' },
  { id: 'agenda', label: 'Agenda' },
];

const collapsedStorageKey = 'publishing-calendar:filters-collapsed';
/** Below this width the filter panel starts collapsed unless the viewer chose otherwise */
const narrowBreakpoint = 1100;

function readFiltersCollapsed(): boolean {
  try {
    const stored = window.localStorage.getItem(collapsedStorageKey);
    if (stored !== null) {
      return stored === 'true';
    }
  } catch {
    // Storage unavailable: fall back to the width default
  }
  return window.innerWidth < narrowBreakpoint;
}

function writeFiltersCollapsed(collapsed: boolean): void {
  try {
    window.localStorage.setItem(collapsedStorageKey, String(collapsed));
  } catch {
    // Storage unavailable: state lasts for this page view only
  }
}

/** Days loaded from the server: the visible week, or full weeks covering the month (month + agenda share them) */
function getLoadDayKeys(view: CalendarView, anchor: Date): string[] {
  return view === 'week' ? weekDayKeys(anchor) : monthGridDayKeys(anchor);
}

/** Days the legend totals and agenda cover */
function getRangeDayKeys(view: CalendarView, anchor: Date): string[] {
  return view === 'week' ? weekDayKeys(anchor) : monthDayKeys(anchor);
}

export const PublishingCalendarTemplate = (
  props: PublishingCalendarClientProperties,
) => {
  const { executeCommand } = usePageCommandProvider();
  const todayKey = toDayKey(new Date());

  const [view, setView] = useState<CalendarView>('month');
  const [anchor, setAnchor] = useState<Date>(() => fromDayKey(todayKey));
  const [hiddenStatuses, setHiddenStatuses] = useState<Set<PublishingStatus>>(
    new Set(),
  );
  const [filters, setFilters] = useState<CalendarFilters>(emptyFilters);
  const [selectedDay, setSelectedDay] = useState<string>(todayKey);
  const [focusedStatus, setFocusedStatus] = useState<PublishingStatus | null>(
    null,
  );
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const [agendaFocusDay, setAgendaFocusDay] = useState<string | null>(null);
  const [data, setData] = useState<CalendarData>(emptyData);
  const [isLoading, setIsLoading] = useState(false);
  const [loadingGroup, setLoadingGroup] = useState<string | null>(null);
  const [filtersCollapsed, setFiltersCollapsed] =
    useState(readFiltersCollapsed);

  const cache = useRef(new Map<string, CalendarData>());
  const currentRequestKey = useRef('');

  const loadDayKeys = useMemo(
    () => getLoadDayKeys(view, anchor),
    [view, anchor],
  );
  const rangeDayKeys = useMemo(
    () => getRangeDayKeys(view, anchor),
    [view, anchor],
  );
  const requestKey = `${loadDayKeys[0]}|${loadDayKeys[loadDayKeys.length - 1]}|${filters.contentTypeIds.join(',')}|${filters.channelIds.join(',')}|${filters.workflowStepIds.join(',')}`;

  useEffect(() => {
    currentRequestKey.current = requestKey;
    const cached = cache.current.get(requestKey);
    if (cached) {
      setData(cached);
      return;
    }
    setIsLoading(true);
    void executeCommand<
      CalendarResult,
      { days: ReturnType<typeof toDayParam>[]; filters: CalendarFilters }
    >(props.loadCalendarCommandName, {
      days: loadDayKeys.map(toDayParam),
      filters,
    }).then((result) => {
      const loaded = toCalendarData(result ?? { counts: [], items: [] });
      cache.current.set(requestKey, loaded);
      if (currentRequestKey.current === requestKey) {
        setData(loaded);
        setIsLoading(false);
      }
    });
    // requestKey captures loadDayKeys + filters
  }, [requestKey]);

  const loadMore = useCallback(
    (day: string, status: PublishingStatus) => {
      const key = groupKey(day, status);
      const requestKeyAtStart = currentRequestKey.current;
      const skip = data.groups.get(key)?.length ?? 0;
      setLoadingGroup(key);
      void executeCommand<
        DayItemsResult,
        {
          day: ReturnType<typeof toDayParam>;
          status: PublishingStatus;
          skip: number;
          take: number;
          filters: CalendarFilters;
        }
      >(props.loadDayItemsCommandName, {
        day: toDayParam(day),
        status,
        skip,
        take: props.dayItemsPageSize,
        filters,
      }).then((result) => {
        setLoadingGroup(null);
        if (!result || currentRequestKey.current !== requestKeyAtStart) {
          return;
        }
        setData((prev) => {
          const next = appendGroupItems(prev, day, status, result);
          cache.current.set(requestKeyAtStart, next);
          return next;
        });
      });
    },
    [
      data,
      executeCommand,
      filters,
      props.dayItemsPageSize,
      props.loadDayItemsCommandName,
    ],
  );

  const visibleStatuses = useMemo(
    () =>
      statusDefinitions
        .map((s) => s.status)
        .filter((s) => !hiddenStatuses.has(s)),
    [hiddenStatuses],
  );

  const statusTotals = useMemo(() => {
    const totals = new Map<PublishingStatus, number>();
    statusDefinitions.forEach(({ status }) =>
      totals.set(
        status,
        rangeDayKeys.reduce((sum, day) => sum + getCount(data, day, status), 0),
      ),
    );
    return totals;
  }, [data, rangeDayKeys]);

  const dayGroups = useMemo(
    () => getDayGroups(data, selectedDay, visibleStatuses),
    [data, selectedDay, visibleStatuses],
  );

  const selectedEvent = useMemo(() => {
    if (!selectedEventId) {
      return null;
    }
    for (const items of data.groups.values()) {
      const match = items.find((e) => e.id === selectedEventId);
      if (match) {
        return match;
      }
    }
    return null;
  }, [data, selectedEventId]);

  const contentTypeOptions = useMemo<FilterOption[]>(
    () =>
      props.contentTypes.map((c) => ({
        id: c.id,
        name: c.name,
        icon: getContentTypeIcon(c.icon),
      })),
    [props.contentTypes],
  );

  const channelOptions = useMemo<FilterOption[]>(
    () =>
      props.channels.map((c) => ({
        id: c.id,
        name: c.name,
        icon: getChannelIcon(c.channelType),
      })),
    [props.channels],
  );

  const workflowStepGroups = useMemo<FilterOptionGroup[]>(
    () =>
      (props.workflows ?? []).map((w) => ({
        id: w.id,
        name: w.name,
        options: w.steps.map((s) => ({
          id: s.id,
          name: s.name,
          icon: (s.icon || 'xp-scheme-path-circles') as IconName,
          count: s.count,
        })),
      })),
    [props.workflows],
  );

  const toggleStatus = useCallback((status: PublishingStatus) => {
    setHiddenStatuses((prev) => {
      const next = new Set(prev);
      if (next.has(status)) {
        next.delete(status);
      } else {
        next.add(status);
      }
      return next;
    });
  }, []);

  const activeFilterCount =
    hiddenStatuses.size +
    filters.contentTypeIds.length +
    filters.channelIds.length +
    filters.workflowStepIds.length;

  const clearFilters = useCallback(() => {
    setHiddenStatuses(new Set());
    setFilters(emptyFilters);
  }, []);

  const toggleFiltersCollapsed = useCallback(() => {
    setFiltersCollapsed((prev) => {
      writeFiltersCollapsed(!prev);
      return !prev;
    });
  }, []);

  const selectDay = useCallback((day: string, status?: PublishingStatus) => {
    setSelectedDay(day);
    setFocusedStatus(status ?? null);
    setSelectedEventId(null);
  }, []);

  const selectEvent = useCallback((day: string, id: string) => {
    setSelectedDay(day);
    setFocusedStatus(null);
    setSelectedEventId(id);
  }, []);

  const showDayInAgenda = useCallback(
    (day: string) => {
      setAnchor(fromDayKey(day));
      setView('agenda');
      setAgendaFocusDay(day);
      selectDay(day);
    },
    [selectDay],
  );

  const goTo = useCallback(
    (nextAnchor: Date) => {
      setAnchor(nextAnchor);
      setAgendaFocusDay(null);
      const days = getRangeDayKeys(view, nextAnchor);
      if (days.indexOf(selectedDay) < 0) {
        selectDay(days.indexOf(todayKey) >= 0 ? todayKey : days[0]);
      }
    },
    [selectDay, selectedDay, todayKey, view],
  );

  const navigate = (direction: 1 | -1) => {
    if (view === 'week') {
      goTo(addDays(anchor, 7 * direction));
    } else {
      const month = startOfMonth(anchor);
      goTo(new Date(month.getFullYear(), month.getMonth() + direction, 1));
    }
  };

  const changeView = (next: CalendarView) => {
    if (next === view) {
      return;
    }
    if (rangeDayKeys.indexOf(selectedDay) >= 0) {
      setAnchor(fromDayKey(selectedDay));
    }
    setAgendaFocusDay(null);
    setView(next);
  };

  const goToday = () => {
    setAnchor(fromDayKey(todayKey));
    setAgendaFocusDay(null);
    selectDay(todayKey);
  };

  return (
    <div
      className="publishing-calendar"
      data-testid="publishing-calendar"
      data-view={view}
    >
      <header className="publishing-calendar__header">
        <Headline size={HeadlineSize.M}>Publishing calendar</Headline>
        <p className="publishing-calendar__subheading">
          Scheduled publishing, workflow, and recent content changes across all
          channels.
        </p>
        {(isLoading || loadingGroup) && (
          <span
            className="publishing-calendar__loading"
            data-testid="publishing-calendar-loading"
          >
            <Spinner />
          </span>
        )}
      </header>

      <div
        className={`publishing-calendar__body${filtersCollapsed ? ' publishing-calendar__body--filters-collapsed' : ''}`}
      >
        <FilterPanel
          collapsed={filtersCollapsed}
          activeCount={activeFilterCount}
          hiddenStatuses={hiddenStatuses}
          statusTotals={statusTotals}
          filters={filters}
          contentTypeOptions={contentTypeOptions}
          channelOptions={channelOptions}
          workflowStepGroups={workflowStepGroups}
          OnToggleCollapsed={toggleFiltersCollapsed}
          OnToggleStatus={toggleStatus}
          OnChangeFilters={setFilters}
          OnClear={clearFilters}
        />

        <section
          className="publishing-calendar__grid"
          data-testid="publishing-calendar-grid"
        >
          <div className="publishing-calendar__toolbar">
            <div className="publishing-calendar__toolbar-group publishing-calendar__nav">
              <Button
                icon="xp-chevron-left"
                title="Previous"
                size={ButtonSize.S}
                color={ButtonColor.Secondary}
                onClick={() => navigate(-1)}
                dataTestId={'publishing-calendar-prev' as TestId}
              />
              <Button
                icon="xp-chevron-right"
                title="Next"
                size={ButtonSize.S}
                color={ButtonColor.Secondary}
                onClick={() => navigate(1)}
                dataTestId={'publishing-calendar-next' as TestId}
              />
              <Button
                label="Today"
                size={ButtonSize.S}
                color={ButtonColor.Secondary}
                onClick={goToday}
                dataTestId={'publishing-calendar-today' as TestId}
              />
            </div>
            <h2
              className="publishing-calendar__toolbar-title"
              data-testid="publishing-calendar-title"
            >
              {formatRangeTitle(view, anchor)}
            </h2>
            <div
              className="publishing-calendar__toolbar-group"
              role="group"
              aria-label="Calendar view"
            >
              {views.map((v) => (
                <Button
                  key={v.id}
                  label={v.label}
                  size={ButtonSize.S}
                  color={
                    v.id === view ? ButtonColor.Primary : ButtonColor.Secondary
                  }
                  onClick={() => changeView(v.id)}
                  dataTestId={`publishing-calendar-view-${v.id}` as TestId}
                />
              ))}
            </div>
          </div>

          {view === 'month' && (
            <MonthView
              dayKeys={loadDayKeys}
              month={anchor.getMonth()}
              data={data}
              statuses={visibleStatuses}
              selectedDay={selectedDay}
              todayKey={todayKey}
              OnSelectDay={selectDay}
            />
          )}
          {view === 'week' && (
            <WeekView
              dayKeys={loadDayKeys}
              data={data}
              statuses={visibleStatuses}
              selectedDay={selectedDay}
              selectedEventId={selectedEventId}
              todayKey={todayKey}
              OnSelectDay={selectDay}
              OnSelectEvent={selectEvent}
              OnShowMore={showDayInAgenda}
            />
          )}
          {view === 'agenda' && (
            <AgendaView
              dayKeys={rangeDayKeys}
              data={data}
              statuses={visibleStatuses}
              selectedEventId={selectedEventId}
              focusDay={agendaFocusDay}
              loadingGroup={loadingGroup}
              OnClearFocus={() => setAgendaFocusDay(null)}
              OnSelectEvent={selectEvent}
              OnLoadMore={loadMore}
            />
          )}
        </section>

        <aside
          className="publishing-calendar__panel"
          aria-live="polite"
          data-testid="publishing-calendar-panel"
        >
          <div className="publishing-calendar__panel-scroll">
            {selectedEvent ? (
              <ItemDetails
                event={selectedEvent}
                OnBack={() => setSelectedEventId(null)}
              />
            ) : (
              <DayPanel
                dayKey={selectedDay}
                groups={dayGroups}
                focusedStatus={focusedStatus}
                loadingGroup={loadingGroup}
                OnSelectEvent={setSelectedEventId}
                OnLoadMore={(status) => loadMore(selectedDay, status)}
              />
            )}
          </div>
        </aside>
      </div>
    </div>
  );
};

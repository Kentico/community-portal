import {
  Button,
  ButtonColor,
  ButtonSize,
  Icon,
  type TestId,
} from '@kentico/xperience-admin-components';
import React, { useState } from 'react';
import {
  type CalendarFilters,
  type PublishingStatus,
  statusDefinitions,
} from './calendarModel';
import {
  type FilterOption,
  type FilterOptionGroup,
  FilterSection,
  type FilterSectionKey,
  GroupedMultiSelectFilter,
  MultiSelectFilter,
} from './MultiSelectFilter';

const panelId = 'publishing-calendar-filters-content';

/** Status starts expanded; the longer option lists start collapsed */
const defaultExpandedSections: ReadonlySet<FilterSectionKey> = new Set([
  'status',
]);

interface FilterPanelProps {
  collapsed: boolean;
  activeCount: number;
  hiddenStatuses: Set<PublishingStatus>;
  statusTotals: Map<PublishingStatus, number>;
  filters: CalendarFilters;
  contentTypeOptions: FilterOption[];
  channelOptions: FilterOption[];
  workflowStepGroups: FilterOptionGroup[];
  OnToggleCollapsed: () => void;
  OnToggleStatus: (status: PublishingStatus) => void;
  OnChangeFilters: (filters: CalendarFilters) => void;
  OnClear: () => void;
}

/** Collapsible left panel of collapsible sections: status, workflow step, content type, channel */
export const FilterPanel = ({
  collapsed,
  activeCount,
  hiddenStatuses,
  statusTotals,
  filters,
  contentTypeOptions,
  channelOptions,
  workflowStepGroups,
  OnToggleCollapsed: onToggleCollapsed,
  OnToggleStatus: onToggleStatus,
  OnChangeFilters: onChangeFilters,
  OnClear: onClear,
}: FilterPanelProps) => {
  const toggleLabel = collapsed ? 'Show filters' : 'Hide filters';
  const [expandedSections, setExpandedSections] = useState<
    ReadonlySet<FilterSectionKey>
  >(defaultExpandedSections);
  const toggleSection = (section: FilterSectionKey) =>
    setExpandedSections((prev) => {
      const next = new Set(prev);
      if (!next.delete(section)) {
        next.add(section);
      }
      return next;
    });
  const sectionToggle = (section: FilterSectionKey) => ({
    section,
    expanded: expandedSections.has(section),
    OnToggleExpanded: toggleSection,
  });

  return (
    <aside
      className={`publishing-calendar__filters${collapsed ? ' publishing-calendar__filters--collapsed' : ''}`}
      aria-label="Filters"
      data-testid="publishing-calendar-filters"
      data-collapsed={collapsed}
      data-active-count={activeCount}
    >
      <div className="publishing-calendar__filters-header">
        <button
          type="button"
          className="publishing-calendar__filters-toggle"
          aria-expanded={!collapsed}
          aria-controls={panelId}
          aria-label={toggleLabel}
          title={toggleLabel}
          onClick={onToggleCollapsed}
          data-testid="publishing-calendar-filters-toggle"
        >
          <Icon name="xp-funnel" />
          {!collapsed && (
            <span className="publishing-calendar__filters-title">Filters</span>
          )}
          {activeCount > 0 && (
            <span
              className="publishing-calendar__filters-active-count"
              aria-label={`${activeCount} active`}
              data-testid="publishing-calendar-filters-active-count"
            >
              {activeCount}
            </span>
          )}
          <span className="publishing-calendar__filters-toggle-chevron">
            <Icon
              name={
                collapsed ? 'xp-chevron-double-right' : 'xp-chevron-double-left'
              }
            />
          </span>
        </button>
      </div>

      <div
        id={panelId}
        className="publishing-calendar__filters-content"
        hidden={collapsed}
      >
        <FilterSection
          {...sectionToggle('status')}
          label="Status"
          appliedCount={hiddenStatuses.size}
        >
          <div
            className="publishing-calendar__legend"
            role="group"
            aria-label="Filter by status"
            data-testid="publishing-calendar-legend"
          >
            {statusDefinitions.map((s) => {
              const isVisible = !hiddenStatuses.has(s.status);
              return (
                <button
                  key={s.status}
                  type="button"
                  className={`publishing-calendar__filter publishing-calendar__status--${s.status}`}
                  aria-pressed={isVisible}
                  title={`${s.description}. Click to ${isVisible ? 'hide' : 'show'}.`}
                  onClick={() => onToggleStatus(s.status)}
                  data-testid="publishing-calendar-status-filter"
                  data-status={s.status}
                >
                  <Icon name={s.icon} />
                  <span className="publishing-calendar__filter-label">
                    {s.label}
                  </span>
                  <span
                    className="publishing-calendar__filter-count"
                    data-testid="publishing-calendar-status-filter-count"
                  >
                    {statusTotals.get(s.status) ?? 0}
                  </span>
                </button>
              );
            })}
          </div>
        </FilterSection>

        {workflowStepGroups.length > 0 && (
          <GroupedMultiSelectFilter
            {...sectionToggle('workflow-step')}
            label="Workflow step"
            hint="Shows only items currently in the selected steps."
            groups={workflowStepGroups}
            selectedIds={filters.workflowStepIds}
            OnChange={(ids) =>
              onChangeFilters({ ...filters, workflowStepIds: ids })
            }
            testId="publishing-calendar-workflow-step-filter"
            groupTestId="publishing-calendar-workflow-group"
            groupIdAttribute="workflow-id"
          />
        )}
        <MultiSelectFilter
          {...sectionToggle('content-type')}
          label="Content type"
          options={contentTypeOptions}
          selectedIds={filters.contentTypeIds}
          OnChange={(ids) =>
            onChangeFilters({ ...filters, contentTypeIds: ids })
          }
          testId="publishing-calendar-content-type-filter"
        />
        <MultiSelectFilter
          {...sectionToggle('channel')}
          label="Channel"
          options={channelOptions}
          selectedIds={filters.channelIds}
          OnChange={(ids) => onChangeFilters({ ...filters, channelIds: ids })}
          testId="publishing-calendar-channel-filter"
        />
      </div>

      {!collapsed && (
        <div className="publishing-calendar__filters-footer">
          <Button
            label="Clear filters"
            icon="xp-times"
            size={ButtonSize.S}
            color={ButtonColor.Secondary}
            disabled={activeCount === 0}
            onClick={onClear}
            dataTestId={'publishing-calendar-filters-clear' as TestId}
          />
        </div>
      )}
    </aside>
  );
};

import {
  Button,
  ButtonColor,
  ButtonSize,
  Checkbox,
  Icon,
  type IconName,
  type TestId,
} from '@kentico/xperience-admin-components';
import React, { type ReactNode, useId } from 'react';

export interface FilterOption {
  id: number;
  name: string;
  icon: IconName;
  /** Optional count shown next to the icon */
  count?: number;
}

export interface FilterOptionGroup {
  id: number;
  name: string;
  options: FilterOption[];
}

export type FilterSectionKey =
  'status' | 'workflow-step' | 'content-type' | 'channel';

/** Expand/collapse wiring shared by every filter section */
export interface FilterSectionToggleProps {
  section: FilterSectionKey;
  expanded: boolean;
  OnToggleExpanded: (section: FilterSectionKey) => void;
}

interface FilterSectionProps extends FilterSectionToggleProps {
  label: string;
  /** Filters applied in this section: badge when collapsed, enables Clear when expanded */
  appliedCount: number;
  /** Omit for no Clear action (the slot still reserves its space) */
  OnClear?: () => void;
  /** Optional data-testid (+ data-selected-count) on the collapsible body */
  bodyTestId?: string;
  children: ReactNode;
}

/**
 * Collapsible filter section. The header toggles the body; its trailing slot shows the
 * applied count (collapsed) or an optional Clear action (expanded) and always reserves its space.
 */
export const FilterSection = ({
  section,
  expanded,
  OnToggleExpanded: onToggleExpanded,
  label,
  appliedCount,
  OnClear: onClear,
  bodyTestId,
  children,
}: FilterSectionProps) => {
  const headingId = useId();
  const bodyId = useId();
  const canClear = appliedCount > 0;

  return (
    <section
      className="publishing-calendar__filter-section"
      aria-labelledby={headingId}
      data-testid="publishing-calendar-filter-section"
      data-section={section}
      data-expanded={expanded}
      data-applied-count={appliedCount}
    >
      <div className="publishing-calendar__filter-section-heading">
        <h3>
          <button
            id={headingId}
            type="button"
            className="publishing-calendar__filter-section-toggle"
            aria-expanded={expanded}
            aria-controls={bodyId}
            onClick={() => onToggleExpanded(section)}
            data-testid="publishing-calendar-filter-section-toggle"
          >
            <span className="publishing-calendar__filter-section-chevron">
              <Icon name="xp-chevron-down" />
            </span>
            {label}
          </button>
        </h3>
        <span className="publishing-calendar__filter-section-meta">
          {!expanded && canClear && (
            <span
              className="publishing-calendar__filters-active-count"
              aria-label={`${appliedCount} applied`}
              data-testid="publishing-calendar-filter-section-count"
              data-count={appliedCount}
            >
              {appliedCount}
            </span>
          )}
          {expanded && onClear && (
            <span
              className="publishing-calendar__filter-section-clear"
              data-hidden={!canClear}
            >
              <Button
                label="Clear"
                size={ButtonSize.S}
                color={ButtonColor.Tertiary}
                disabled={!canClear}
                onClick={onClear}
                dataTestId={
                  'publishing-calendar-filter-section-clear' as TestId
                }
              />
            </span>
          )}
        </span>
      </div>
      <div
        id={bodyId}
        className="publishing-calendar__filter-section-body"
        hidden={!expanded}
        data-testid={bodyTestId}
        data-selected-count={bodyTestId ? appliedCount : undefined}
      >
        {children}
      </div>
    </section>
  );
};

interface OptionItemProps {
  option: FilterOption;
  checked: boolean;
  OnToggle: (id: number, checked: boolean) => void;
  testId: string;
}

const OptionItem = ({
  option,
  checked,
  OnToggle: onToggle,
  testId,
}: OptionItemProps) => (
  <li
    className="publishing-calendar__select-option"
    data-testid={`${testId}-option`}
    data-id={option.id}
  >
    <Checkbox
      label={option.name}
      checked={checked}
      onChange={(_, value) => onToggle(option.id, value)}
    />
    <span className="publishing-calendar__select-option-meta">
      <Icon name={option.icon} fallbackIconName="xp-doc" />
      {option.count !== undefined && (
        <span
          className="publishing-calendar__select-option-count"
          data-testid={`${testId}-option-count`}
        >
          {option.count}
        </span>
      )}
    </span>
  </li>
);

/** Selection toggle that keeps the ids in option order */
function useToggle(
  orderedIds: number[],
  selectedIds: number[],
  onChange: (ids: number[]) => void,
) {
  const selected = new Set(selectedIds);
  const toggle = (id: number, checked: boolean) => {
    const next = new Set(selected);
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    onChange(orderedIds.filter((o) => next.has(o)));
  };
  return { selected, toggle };
}

interface MultiSelectFilterProps extends FilterSectionToggleProps {
  label: string;
  options: FilterOption[];
  selectedIds: number[];
  OnChange: (ids: number[]) => void;
  testId: string;
}

/** Collapsible checkbox list. Empty selection = no filtering. */
export const MultiSelectFilter = ({
  section,
  expanded,
  OnToggleExpanded: onToggleExpanded,
  label,
  options,
  selectedIds,
  OnChange: onChange,
  testId,
}: MultiSelectFilterProps) => {
  const { selected, toggle } = useToggle(
    options.map((o) => o.id),
    selectedIds,
    onChange,
  );

  return (
    <FilterSection
      section={section}
      expanded={expanded}
      OnToggleExpanded={onToggleExpanded}
      label={label}
      appliedCount={selectedIds.length}
      OnClear={() => onChange([])}
      bodyTestId={testId}
    >
      <ul className="publishing-calendar__option-list">
        {options.map((o) => (
          <OptionItem
            key={o.id}
            option={o}
            checked={selected.has(o.id)}
            OnToggle={toggle}
            testId={testId}
          />
        ))}
      </ul>
    </FilterSection>
  );
};

interface GroupedMultiSelectFilterProps extends FilterSectionToggleProps {
  label: string;
  groups: FilterOptionGroup[];
  selectedIds: number[];
  OnChange: (ids: number[]) => void;
  testId: string;
  groupTestId: string;
  /** data-* attribute name carrying each group id, e.g. "workflow-id" */
  groupIdAttribute: string;
  hint?: string;
}

/** Collapsible checkbox list with group headings (e.g. workflow > steps). Empty selection = no filtering. */
export const GroupedMultiSelectFilter = ({
  section,
  expanded,
  OnToggleExpanded: onToggleExpanded,
  label,
  groups,
  selectedIds,
  OnChange: onChange,
  testId,
  groupTestId,
  groupIdAttribute,
  hint,
}: GroupedMultiSelectFilterProps) => {
  const { selected, toggle } = useToggle(
    groups.flatMap((g) => g.options.map((o) => o.id)),
    selectedIds,
    onChange,
  );

  return (
    <FilterSection
      section={section}
      expanded={expanded}
      OnToggleExpanded={onToggleExpanded}
      label={label}
      appliedCount={selectedIds.length}
      OnClear={() => onChange([])}
      bodyTestId={testId}
    >
      {hint && <p className="publishing-calendar__filter-hint">{hint}</p>}
      <div className="publishing-calendar__option-list">
        {groups.map((g) => (
          <div
            key={g.id}
            className="publishing-calendar__option-group"
            role="group"
            aria-label={g.name}
            data-testid={groupTestId}
            {...{ [`data-${groupIdAttribute}`]: g.id }}
          >
            <div className="publishing-calendar__option-group-heading">
              {g.name}
            </div>
            <ul className="publishing-calendar__option-group-list">
              {g.options.map((o) => (
                <OptionItem
                  key={o.id}
                  option={o}
                  checked={selected.has(o.id)}
                  OnToggle={toggle}
                  testId={testId}
                />
              ))}
            </ul>
          </div>
        ))}
      </div>
    </FilterSection>
  );
};

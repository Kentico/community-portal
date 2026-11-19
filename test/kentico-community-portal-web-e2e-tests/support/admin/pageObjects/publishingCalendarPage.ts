import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../../config.js";

export type PublishingCalendarView = "month" | "week" | "agenda";

export type PublishingCalendarFilterSection =
  "status" | "workflow-step" | "content-type" | "channel";

/** The Publishing calendar admin application (PublishingCalendarPage). Uses data-testid hooks only. */
export class PublishingCalendarPage {
  constructor(private readonly page: Page) {}

  /**
   * Opens the application root, which redirects to its calendar child page, and waits for the first load.
   * Expands the filter panel (it starts collapsed on narrow viewports).
   */
  async open(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/publishing-calendar`);
    await expect(this.page).toHaveURL(/\/publishing-calendar\/calendar/i, {
      timeout: 15_000,
    });
    await expect(this.root()).toBeVisible({ timeout: 15_000 });
    await this.waitForLoad();
    await this.setFiltersExpanded(true);
  }

  async waitForLoad(): Promise<void> {
    await expect(
      this.page.getByTestId("publishing-calendar-loading"),
    ).toHaveCount(0, {
      timeout: 30_000,
    });
  }

  root(): Locator {
    return this.page.getByTestId("publishing-calendar");
  }

  async switchView(view: PublishingCalendarView): Promise<void> {
    await this.page.getByTestId(`publishing-calendar-view-${view}`).click();
    await expect(this.root()).toHaveAttribute("data-view", view);
    await this.waitForLoad();
  }

  statusFilters(): Locator {
    return this.page.getByTestId("publishing-calendar-status-filter");
  }

  statusFilter(status: string): Locator {
    return this.page.locator(
      `[data-testid="publishing-calendar-status-filter"][data-status="${status}"]`,
    );
  }

  contentTypeFilter(): Locator {
    return this.page.getByTestId("publishing-calendar-content-type-filter");
  }

  channelFilter(): Locator {
    return this.page.getByTestId("publishing-calendar-channel-filter");
  }

  /** Grouped by workflow; hidden when no workflows exist. */
  workflowStepFilter(): Locator {
    return this.page.getByTestId("publishing-calendar-workflow-step-filter");
  }

  workflowGroups(): Locator {
    return this.workflowStepFilter().getByTestId(
      "publishing-calendar-workflow-group",
    );
  }

  /** Collapsible filter panel section (status starts expanded, others collapsed). */
  filterSection(section: PublishingCalendarFilterSection): Locator {
    return this.page.locator(
      `[data-testid="publishing-calendar-filter-section"][data-section="${section}"]`,
    );
  }

  filterSectionToggle(section: PublishingCalendarFilterSection): Locator {
    return this.filterSection(section).getByTestId(
      "publishing-calendar-filter-section-toggle",
    );
  }

  /** Applied-filter count badge; only rendered while the section is collapsed. */
  filterSectionCount(section: PublishingCalendarFilterSection): Locator {
    return this.filterSection(section).getByTestId(
      "publishing-calendar-filter-section-count",
    );
  }

  /** Section Clear; only rendered while expanded, and not for status. */
  filterSectionClear(section: PublishingCalendarFilterSection): Locator {
    return this.filterSection(section).getByTestId(
      "publishing-calendar-filter-section-clear",
    );
  }

  async setFilterSectionExpanded(
    section: PublishingCalendarFilterSection,
    expanded: boolean,
  ): Promise<void> {
    const sectionLocator = this.filterSection(section);
    if (
      (await sectionLocator.getAttribute("data-expanded")) !== String(expanded)
    ) {
      await this.filterSectionToggle(section).click();
    }
    await expect(sectionLocator).toHaveAttribute(
      "data-expanded",
      String(expanded),
    );
  }

  /** Toggles the first option of a checkbox-list filter, expanding its section first. */
  async toggleFirstFilterOption(filter: Locator): Promise<void> {
    const section = filter.locator(
      "xpath=ancestor::*[@data-testid='publishing-calendar-filter-section'][1]",
    );
    if ((await section.getAttribute("data-expanded")) !== "true") {
      await section
        .getByTestId("publishing-calendar-filter-section-toggle")
        .click();
      await expect(section).toHaveAttribute("data-expanded", "true");
    }
    const testId = await filter.getAttribute("data-testid");
    await filter
      .getByTestId(`${testId}-option`)
      .first()
      .locator("label")
      .click();
    await this.waitForLoad();
  }

  /** Left filter panel (status toggles, content type, channel, workflow step). */
  filterPanel(): Locator {
    return this.page.getByTestId("publishing-calendar-filters");
  }

  filtersToggle(): Locator {
    return this.page.getByTestId("publishing-calendar-filters-toggle");
  }

  filtersActiveCount(): Locator {
    return this.page.getByTestId("publishing-calendar-filters-active-count");
  }

  clearFiltersButton(): Locator {
    return this.page.getByTestId("publishing-calendar-filters-clear");
  }

  async setFiltersExpanded(expanded: boolean): Promise<void> {
    const toggle = this.filtersToggle();
    if ((await toggle.getAttribute("aria-expanded")) !== String(expanded)) {
      await toggle.click();
    }
    await expect(toggle).toHaveAttribute("aria-expanded", String(expanded));
  }

  /** A month-grid day cell, by ISO date (yyyy-mm-dd). */
  dayCell(isoDate: string): Locator {
    return this.page.locator(
      `[data-testid="publishing-calendar-month-day"][data-date="${isoDate}"]`,
    );
  }

  /** Per-status count badges in month cells. */
  dayStatusCounts(): Locator {
    return this.page.getByTestId("publishing-calendar-day-status-count");
  }

  weekDays(): Locator {
    return this.page.getByTestId("publishing-calendar-week-day");
  }

  weekItems(): Locator {
    return this.page.getByTestId("publishing-calendar-week-item");
  }

  agendaItems(): Locator {
    return this.page.getByTestId("publishing-calendar-agenda-item");
  }

  agendaTimes(): Locator {
    return this.page.getByTestId("publishing-calendar-agenda-time");
  }

  panel(): Locator {
    return this.page.getByTestId("publishing-calendar-panel");
  }

  dayAgenda(): Locator {
    return this.panel().getByTestId("publishing-calendar-day");
  }

  panelGroups(): Locator {
    return this.panel().getByTestId("publishing-calendar-panel-group");
  }

  panelItems(): Locator {
    return this.panel().getByTestId("publishing-calendar-day-item");
  }

  itemDetails(): Locator {
    return this.panel().getByTestId("publishing-calendar-item-details");
  }

  viewLink(): Locator {
    return this.panel().getByTestId("publishing-calendar-view-link");
  }
}

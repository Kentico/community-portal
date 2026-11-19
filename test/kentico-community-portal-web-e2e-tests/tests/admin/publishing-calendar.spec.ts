import { expect, type Locator, test } from "@playwright/test";
import { PublishingCalendarPage } from "../../support/admin/pageObjects/publishingCalendarPage.js";

/*
 * Publishing calendar admin app (PublishingCalendarPage + PublishingCalendarTemplate).
 * Asserts on data-testid / data-* hooks, not on captions or seeded content.
 * Relies on content changed in the current month (a DB restore stamps items with the restore date).
 */

const statuses = [
  "scheduled-publish",
  "scheduled-unpublish",
  "in-workflow",
  "draft",
  "published",
  "unpublished",
];

async function sumLegendCounts(
  calendar: PublishingCalendarPage,
): Promise<number> {
  const counts = await calendar
    .statusFilters()
    .getByTestId("publishing-calendar-status-filter-count")
    .allTextContents();
  return counts.reduce((sum, c) => sum + Number(c), 0);
}

/** The month cell containing a badge */
function cellOf(badge: Locator): Locator {
  return badge.locator(
    'xpath=ancestor::*[@data-testid="publishing-calendar-month-day"]',
  );
}

test.describe("publishing calendar", () => {
  test("status legend toggles filter all views", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    await expect(calendar.statusFilters()).toHaveCount(statuses.length);
    for (const status of statuses) {
      await expect(calendar.statusFilter(status)).toHaveAttribute(
        "aria-pressed",
        "true",
      );
    }

    const badge = calendar.dayStatusCounts().first();
    await expect(badge).toBeVisible();
    const status = (await badge.getAttribute("data-status")) ?? "";

    await calendar.statusFilter(status).click();
    await expect(calendar.statusFilter(status)).toHaveAttribute(
      "aria-pressed",
      "false",
    );
    await expect(
      page.locator(
        `[data-testid="publishing-calendar-day-status-count"][data-status="${status}"]`,
      ),
    ).toHaveCount(0);
  });

  test("filter panel collapses, remembers state, and clears filters", async ({
    page,
  }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-collapsed",
      "false",
    );
    await expect(calendar.clearFiltersButton()).toBeDisabled();

    await calendar.statusFilter(statuses[0]).click();
    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-active-count",
      "1",
    );
    await expect(calendar.clearFiltersButton()).toBeEnabled();

    // Collapsed rail keeps the active count, hides the filters
    await calendar.setFiltersExpanded(false);
    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-collapsed",
      "true",
    );
    await expect(calendar.statusFilters().first()).toBeHidden();
    await expect(calendar.filtersActiveCount()).toBeVisible();

    // Collapsed state is remembered per viewer
    await page.reload();
    await expect(calendar.filtersToggle()).toHaveAttribute(
      "aria-expanded",
      "false",
    );

    await calendar.setFiltersExpanded(true);
    await calendar.toggleFirstFilterOption(calendar.channelFilter());
    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-active-count",
      "1",
    );

    await calendar.clearFiltersButton().click();
    await calendar.waitForLoad();
    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-active-count",
      "0",
    );
    await expect(calendar.channelFilter()).toHaveAttribute(
      "data-selected-count",
      "0",
    );
  });

  test("filter sections collapse, show applied count, and clear", async ({
    page,
  }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    // Default: status expanded, the rest collapsed
    await expect(calendar.filterSection("status")).toHaveAttribute(
      "data-expanded",
      "true",
    );
    await expect(calendar.statusFilters().first()).toBeVisible();
    for (const section of [
      "workflow-step",
      "content-type",
      "channel",
    ] as const) {
      await expect(calendar.filterSection(section)).toHaveAttribute(
        "data-expanded",
        "false",
      );
      await expect(calendar.filterSectionToggle(section)).toHaveAttribute(
        "aria-expanded",
        "false",
      );
    }
    await expect(calendar.contentTypeFilter()).toBeHidden();
    // Status has no section Clear
    await expect(calendar.filterSectionClear("status")).toHaveCount(0);

    // Collapsed status shows the hidden-status count
    await calendar.statusFilter(statuses[0]).click();
    await calendar.setFilterSectionExpanded("status", false);
    await expect(calendar.statusFilters().first()).toBeHidden();
    await expect(calendar.filterSectionCount("status")).toHaveAttribute(
      "data-count",
      "1",
    );
    await calendar.setFilterSectionExpanded("status", true);
    await calendar.statusFilter(statuses[0]).click();
    await calendar.waitForLoad();

    // Expanded with nothing applied: Clear unavailable
    await calendar.setFilterSectionExpanded("content-type", true);
    await expect(calendar.contentTypeFilter()).toBeVisible();
    await expect(calendar.filterSectionClear("content-type")).toBeDisabled();

    await calendar.toggleFirstFilterOption(calendar.contentTypeFilter());
    await expect(calendar.filterSection("content-type")).toHaveAttribute(
      "data-applied-count",
      "1",
    );
    // Badge only while collapsed
    await expect(calendar.filterSectionCount("content-type")).toHaveCount(0);

    await calendar.setFilterSectionExpanded("content-type", false);
    await expect(calendar.contentTypeFilter()).toBeHidden();
    await expect(calendar.filterSectionCount("content-type")).toHaveAttribute(
      "data-count",
      "1",
    );

    await calendar.setFilterSectionExpanded("content-type", true);
    await calendar.filterSectionClear("content-type").click();
    await calendar.waitForLoad();
    await expect(calendar.contentTypeFilter()).toHaveAttribute(
      "data-selected-count",
      "0",
    );
    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-active-count",
      "0",
    );
    await expect(calendar.filterSectionClear("content-type")).toBeDisabled();
  });

  test("month cells show status counts and focus the panel group", async ({
    page,
  }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    const badge = calendar.dayStatusCounts().first();
    await expect(badge).toBeVisible();
    expect(Number(await badge.getAttribute("data-count"))).toBeGreaterThan(0);
    // Month cells are an overview: no item lists
    await expect(calendar.weekItems()).toHaveCount(0);
    await expect(calendar.agendaItems()).toHaveCount(0);

    const status = (await badge.getAttribute("data-status")) ?? "";
    const day = (await cellOf(badge).getAttribute("data-date")) ?? "";
    await badge.click();

    await expect(calendar.dayCell(day)).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await expect(calendar.dayAgenda()).toHaveAttribute("data-date", day);
    await expect(
      calendar
        .panel()
        .locator(
          `[data-testid="publishing-calendar-panel-group"][data-status="${status}"]`,
        ),
    ).toHaveAttribute("data-focused", "true");
  });

  test("selecting a day shows that day in the side panel", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    const cells = page.getByTestId("publishing-calendar-month-day");
    const cell = cells.nth(10);
    const day = (await cell.getAttribute("data-date")) ?? "";
    await cell.click({ position: { x: 4, y: 40 } });

    await expect(cell).toHaveAttribute("aria-selected", "true");
    await expect(calendar.dayAgenda()).toHaveAttribute("data-date", day);
    await expect(calendar.itemDetails()).toHaveCount(0);
  });

  test("content type filter narrows counts", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();
    const before = await sumLegendCounts(calendar);

    await calendar.toggleFirstFilterOption(calendar.contentTypeFilter());

    await expect(calendar.contentTypeFilter()).toHaveAttribute(
      "data-selected-count",
      "1",
    );
    expect(await sumLegendCounts(calendar)).toBeLessThanOrEqual(before);
  });

  test("channel filter narrows counts", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();
    const before = await sumLegendCounts(calendar);

    await calendar.toggleFirstFilterOption(calendar.channelFilter());

    await expect(calendar.channelFilter()).toHaveAttribute(
      "data-selected-count",
      "1",
    );
    expect(await sumLegendCounts(calendar)).toBeLessThanOrEqual(before);
  });

  test("workflow step filter groups steps by workflow and narrows counts", async ({
    page,
  }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();
    const before = await sumLegendCounts(calendar);

    await calendar.setFilterSectionExpanded("workflow-step", true);
    await expect(calendar.workflowStepFilter()).toBeVisible();
    await expect(calendar.workflowGroups().first()).toHaveAttribute(
      "data-workflow-id",
      /^\d+$/,
    );
    await expect(
      calendar
        .workflowGroups()
        .first()
        .getByTestId("publishing-calendar-workflow-step-filter-option")
        .first(),
    ).toBeVisible();

    await calendar.toggleFirstFilterOption(calendar.workflowStepFilter());

    await expect(calendar.workflowStepFilter()).toHaveAttribute(
      "data-selected-count",
      "1",
    );
    await expect(calendar.filterPanel()).toHaveAttribute(
      "data-active-count",
      "1",
    );
    expect(await sumLegendCounts(calendar)).toBeLessThanOrEqual(before);
  });

  test("week view lists items per day and +more opens that day in agenda", async ({
    page,
  }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();
    await calendar.switchView("week");

    await expect(calendar.weekDays()).toHaveCount(7);
    await expect(calendar.weekItems().first()).toBeVisible();

    const more = page.getByTestId("publishing-calendar-week-more").first();
    test.skip((await more.count()) === 0, "No capped day this week");
    const day =
      (await more
        .locator(
          'xpath=ancestor::*[@data-testid="publishing-calendar-week-day"]',
        )
        .getAttribute("data-date")) ?? "";
    await more.click();

    await expect(calendar.root()).toHaveAttribute("data-view", "agenda");
    await expect(
      page.getByTestId("publishing-calendar-agenda"),
    ).toHaveAttribute("data-focus-day", day);
    await expect(calendar.dayAgenda()).toHaveAttribute("data-date", day);
  });

  test("agenda shows real item times", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();
    await calendar.switchView("agenda");

    await expect(calendar.agendaItems().first()).toBeVisible();
    // ISO instant with a time part (not an all-day date)
    await expect(calendar.agendaTimes().first()).toHaveAttribute(
      "datetime",
      /T\d{2}:\d{2}/,
    );
  });

  test("item details link to the item", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    await calendar.dayStatusCounts().first().click();
    await calendar.panelItems().first().click();

    await expect(calendar.itemDetails()).toBeVisible();
    await expect(calendar.viewLink().locator("a")).toHaveAttribute(
      "href",
      /.+/,
    );
  });

  test("panel loads more items for a capped status", async ({ page }) => {
    const calendar = new PublishingCalendarPage(page);
    await calendar.open();

    const badges = calendar.dayStatusCounts();
    const count = await badges.count();
    let capped: Locator | null = null;
    for (let i = 0; i < count && !capped; i++) {
      await badges.nth(i).click();
      const group = calendar
        .panelGroups()
        .filter({
          has: page.getByTestId("publishing-calendar-panel-load-more"),
        })
        .first();
      if ((await group.count()) > 0) {
        const status = await group.getAttribute("data-status");
        capped = calendar
          .panel()
          .locator(
            `[data-testid="publishing-calendar-panel-group"][data-status="${status}"]`,
          );
      }
    }
    test.skip(
      capped === null,
      "No day with more items than the per-status cap",
    );
    if (!capped) {
      return;
    }

    const shown = Number(await capped.getAttribute("data-shown"));
    const total = Number(await capped.getAttribute("data-total"));
    expect(shown).toBeLessThan(total);

    await capped.getByTestId("publishing-calendar-panel-load-more").click();

    await expect
      .poll(async () => Number(await capped.getAttribute("data-shown")))
      .toBeGreaterThan(shown);
  });
});

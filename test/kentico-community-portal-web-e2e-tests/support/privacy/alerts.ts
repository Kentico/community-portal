import { expect, type Locator, type Page } from "@playwright/test";

/*
 * Site alerts (notification tray). Local data: the "Portal alert settings" content item lists
 * these alert messages (CI repository).
 */
export const alertSelectors = {
  bell: "#notificationBell:visible, #notificationBellMobile:visible",
  tray: "#notificationTray",
  item: "[test-id=notificationItem]",
  dismiss: "[test-id=notificationDismiss]",
} as const;

/** Alerts with no contact group: shown to every visitor. Matched by message text. */
export const everyoneAlerts = {
  qAndAGuidelines: "Search existing discussions first",
  communityNews: "Read the latest community updates",
} as const;

/** "Welcome" alert, shown only to the Unregistered visitor contact group (activity based). */
export const personalizedAlerts = {
  welcomeUnregistered: "New to the Kentico Community Portal?",
} as const;

export function alertItem(page: Page, text: string): Locator {
  return page
    .locator(alertSelectors.tray)
    .locator(alertSelectors.item)
    .filter({ hasText: text });
}

export async function openNotifications(page: Page): Promise<void> {
  await page.locator(alertSelectors.bell).first().click();
  await expect(page.locator(alertSelectors.tray)).toHaveClass(/\bopen\b/);
}

/** Dismisses an alert from the open tray and waits for the dismissal to be saved. */
export async function dismissAlert(page: Page, text: string): Promise<void> {
  const item = alertItem(page, text);
  await Promise.all([
    page.waitForResponse("**/notification/**"),
    item.locator(alertSelectors.dismiss).click(),
  ]);
  await expect(item).toHaveCount(0);
}

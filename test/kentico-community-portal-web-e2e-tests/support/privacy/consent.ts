import { expect, type Page } from "@playwright/test";

/*
 * `test-id` selectors of the cookie banner and the cookie settings widget. If the markup changes,
 * change them here and nowhere else.
 */
export const consentSelectors = {
  banner: "[test-id=cookieBanner]",
  bannerAcceptAll: "[test-id=cookieBannerAcceptAll]",
  bannerNecessaryOnly: "[test-id=cookieBannerNecessaryOnly]",
  /** In the admin-edited banner message. */
  bannerSettingsLink: "[test-id=cookieBannerMessage] a[href*='cookies-policy']",
  bannerHeading: "[test-id=cookieBannerHeading]",
  bannerMessage: "[test-id=cookieBannerMessage]",
  bannerTrackingBullet: "[test-id=cookieBannerTracking]",
  /** Shown instead of the tracking option in read-only mode. */
  bannerReadOnlyNote: "[test-id=cookieBannerReadOnly]",

  settings: "[test-id=cookieSettings]",
  settingsAnalyticsToggle: "[test-id=cookieSettingsAnalytics]",
  settingsTrackingToggle: "[test-id=cookieSettingsTracking]",
  settingsSave: "[test-id=cookieSettingsSave]",
  settingsAcceptAll: "[test-id=cookieSettingsAcceptAll]",
  settingsNecessaryOnly: "[test-id=cookieSettingsNecessaryOnly]",
  settingsStatus: "[test-id=cookieSettingsStatus]",
  /** Shown next to the disabled tracking toggle in read-only mode. */
  settingsReadOnlyNote: "[test-id=cookieSettingsReadOnly]",
} as const;

/** Where the "Cookie settings" link points. */
export const cookieSettingsPath = "/cookies-policy#cookie-settings";

/** The banner and settings forms post here (`CookiesController`, `/cookies/<action>`). */
const consentResponsePattern = "**/cookies/**";

export type ConsentToggles = {
  analytics: boolean;
  tracking: boolean;
};

/** Where the choice is made. "settings" needs {@link openSettings} first. */
export type ChoiceSource = "banner" | "settings";

/** Clicks "Accept all" (banner by default, or the settings widget) and waits for the choice to be saved. */
export async function acceptAll(
  page: Page,
  from: ChoiceSource = "banner",
): Promise<void> {
  await submitChoice(
    page,
    from === "banner"
      ? consentSelectors.bannerAcceptAll
      : consentSelectors.settingsAcceptAll,
  );
}

/** Clicks "Necessary only" (banner by default, or the settings widget) and waits for the choice to be saved. */
export async function necessaryOnly(
  page: Page,
  from: ChoiceSource = "banner",
): Promise<void> {
  await submitChoice(
    page,
    from === "banner"
      ? consentSelectors.bannerNecessaryOnly
      : consentSelectors.settingsNecessaryOnly,
  );
}

/**
 * Opens the cookie settings widget. From the banner when it's showing (covers the link), else by
 * navigating to the settings page.
 */
export async function openSettings(
  page: Page,
  options: { viaBanner?: boolean } = {},
): Promise<void> {
  if (options.viaBanner) {
    await page.locator(consentSelectors.bannerSettingsLink).click();
    await page.waitForURL("**/cookies-policy**");
  } else {
    await page.goto(cookieSettingsPath);
  }

  await expect(page.locator(consentSelectors.settings)).toBeVisible();
}

/**
 * Sets the optional categories on the settings widget and saves. Necessary is locked on and isn't
 * part of the input. Call {@link openSettings} first.
 */
export async function setToggles(
  page: Page,
  toggles: ConsentToggles,
): Promise<void> {
  await page
    .locator(consentSelectors.settingsAnalyticsToggle)
    .setChecked(toggles.analytics);
  await page
    .locator(consentSelectors.settingsTrackingToggle)
    .setChecked(toggles.tracking);

  await submitChoice(page, consentSelectors.settingsSave);
}

/**
 * Sets only the Analytics switch on the settings widget and clicks "Save choices". For read-only
 * mode, where the tracking switch is disabled. Call {@link openSettings} first.
 */
export async function saveAnalyticsOnly(
  page: Page,
  analytics: boolean,
): Promise<void> {
  await page
    .locator(consentSelectors.settingsAnalyticsToggle)
    .setChecked(analytics);

  await submitChoice(page, consentSelectors.settingsSave);
}

export async function expectBannerVisible(page: Page): Promise<void> {
  await expect(page.locator(consentSelectors.banner)).toBeVisible();
}

export async function expectBannerHidden(page: Page): Promise<void> {
  await expect(page.locator(consentSelectors.banner)).not.toBeVisible();
}

async function submitChoice(page: Page, buttonSelector: string): Promise<void> {
  await Promise.all([
    page.waitForResponse(consentResponsePattern),
    page.locator(buttonSelector).click(),
  ]);
}

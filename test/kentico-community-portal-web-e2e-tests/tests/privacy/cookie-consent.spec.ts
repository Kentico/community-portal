import { expect, test, type Page } from "@playwright/test";
import {
  acceptAll,
  consentSelectors,
  expectBannerHidden,
  expectBannerVisible,
  necessaryOnly,
  openSettings,
  setToggles,
} from "../../support/privacy/consent.js";
import {
  cookieLevels,
  cookieNames,
  getConsentCookie,
  getCookieLevel,
  getCookieValue,
  getCurrentContactGuid,
  setCookies,
} from "../../support/privacy/cookies.js";
import {
  alertItem,
  dismissAlert,
  everyoneAlerts,
  openNotifications,
  personalizedAlerts,
} from "../../support/privacy/alerts.js";
import {
  activityScriptSelector,
  expectNoActivityPost,
  RequestRecorder,
  waitForActivityPost,
} from "../../support/privacy/network.js";
import {
  consentNames,
  effectiveConsent,
  getConsentAgreementsByContact,
  hasActiveAgreement,
  readDataLayerConsent,
} from "../../support/privacy/state.js";

/*
 * Cookie consent: banner, settings widget, withdraw, pre-v2 cookies and alert dismissal.
 * Assertions use this context's own cookies, so the specs run in parallel safely.
 */

async function expectAnalyticsStorage(
  page: Page,
  value: "granted" | "denied",
): Promise<void> {
  await expect
    .poll(
      async () =>
        effectiveConsent(await readDataLayerConsent(page)).analytics_storage,
    )
    .toBe(value);
}

test.describe("cookie banner", () => {
  test("Necessary only keeps the visitor untracked", async ({ page, context }) => {
    await page.goto("/");
    await expectBannerVisible(page);

    await necessaryOnly(page);

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    const consent = await getConsentCookie(context);
    expect(consent).toMatchObject({ version: "v2", analytics: false, tracking: false });
    expect(Math.abs(consent!.savedAt.getTime() - Date.now())).toBeLessThan(5 * 60_000);

    const recorder = RequestRecorder.attach(page);
    await page.goto("/blog");
    await expectBannerHidden(page);
    await page.waitForLoadState("load");

    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();
    await expectNoActivityPost(page, recorder);
  });

  test("Accept all tracks the visitor and records the agreement", async ({
    page,
    context,
  }) => {
    await page.goto("/");
    await acceptAll(page);

    expect(await getCookieLevel(context)).toBe(cookieLevels.all);
    expect(await getConsentCookie(context)).toMatchObject({
      analytics: true,
      tracking: true,
    });

    const contactGuid = await getCurrentContactGuid(context);
    expect(contactGuid).toBeTruthy();

    const activity = waitForActivityPost(page);
    await page.goto("/blog");
    await activity;
    await expectBannerHidden(page);
    // Positive control for the selector expectNoActivityPost checks
    await expect(page.locator(activityScriptSelector)).toHaveCount(1);

    const agreements = await getConsentAgreementsByContact(
      contactGuid!,
      consentNames.websiteTracking,
    );
    expect(hasActiveAgreement(agreements, consentNames.websiteTracking)).toBe(true);
  });

  test("shows the admin-edited copy with the Website tracking consent text", async ({
    page,
  }) => {
    await page.goto("/");

    await expect(page.locator(consentSelectors.bannerHeading)).toHaveText(
      "Cookies on Kentico Community",
    );
    await expect(page.locator(consentSelectors.bannerMessage)).toContainText(
      "measure site usage with Google Analytics",
    );
    // Website tracking consent short text, placed by the message token
    await expect(
      page.locator(`${consentSelectors.bannerMessage} li ${consentSelectors.bannerTrackingBullet}`),
    ).toContainText("Track my activity on Kentico Community");
    await expect(page.locator(consentSelectors.bannerMessage)).not.toContainText("{{");
  });

  test("links to the cookie settings", async ({ page }) => {
    await page.goto("/");
    await openSettings(page, { viaBanner: true });

    await expect(page.locator(consentSelectors.settingsStatus)).toBeVisible();
  });
});

test.describe("cookie settings", () => {
  test("Analytics only grants analytics without tracking", async ({
    page,
    context,
  }) => {
    await openSettings(page);
    await setToggles(page, { analytics: true, tracking: false });

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getConsentCookie(context)).toMatchObject({
      analytics: true,
      tracking: false,
    });
    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();

    await page.goto("/");
    await expectAnalyticsStorage(page, "granted");
    await expectBannerHidden(page);
  });

  test("Tracking only records the agreement and keeps analytics denied", async ({
    page,
    context,
  }) => {
    await openSettings(page);
    await setToggles(page, { analytics: false, tracking: true });

    expect(await getCookieLevel(context)).toBe(cookieLevels.all);
    expect(await getConsentCookie(context)).toMatchObject({
      analytics: false,
      tracking: true,
    });

    const contactGuid = await getCurrentContactGuid(context);
    expect(contactGuid).toBeTruthy();
    const agreements = await getConsentAgreementsByContact(
      contactGuid!,
      consentNames.websiteTracking,
    );
    expect(hasActiveAgreement(agreements, consentNames.websiteTracking)).toBe(true);

    await page.goto("/");
    await expectAnalyticsStorage(page, "denied");
  });

  test("shows the saved choice", async ({ page }) => {
    await openSettings(page);
    await setToggles(page, { analytics: true, tracking: false });

    await page.reload();
    await expect(page.locator(consentSelectors.settingsAnalyticsToggle)).toBeChecked();
    await expect(page.locator(consentSelectors.settingsTrackingToggle)).not.toBeChecked();
    await expect(page.locator(consentSelectors.settingsStatus)).toContainText(
      "Necessary + Analytics",
    );
  });

  test("withdrawing revokes the agreement and stops tracking", async ({
    page,
    context,
  }) => {
    await page.goto("/");
    await acceptAll(page);
    const contactGuid = await getCurrentContactGuid(context);
    expect(contactGuid).toBeTruthy();

    await openSettings(page);
    await necessaryOnly(page, "settings");

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();

    const agreements = await getConsentAgreementsByContact(
      contactGuid!,
      consentNames.websiteTracking,
    );
    expect(agreements.length).toBeGreaterThan(0);
    expect(hasActiveAgreement(agreements, consentNames.websiteTracking)).toBe(false);

    const recorder = RequestRecorder.attach(page);
    await page.goto("/blog");
    await page.waitForLoadState("load");
    await expectNoActivityPost(page, recorder);
    await expectAnalyticsStorage(page, "denied");
  });
});

test.describe("pre-v2 consent cookies", () => {
  test("reset the visitor to Essential and show the banner", async ({
    page,
    context,
  }) => {
    await setCookies(context, {
      [cookieNames.legacyConsentLevel]: "4",
      [cookieNames.legacyLevelSelection]: "true",
      [cookieNames.cookieLevel]: "200",
      [cookieNames.currentContact]: "8b0bd2f4-4c7e-4d0c-9d2c-5b1d5a3f1e01",
    });

    const recorder = RequestRecorder.attach(page);
    await page.goto("/");
    await page.waitForLoadState("load");

    await expectBannerVisible(page);
    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();
    expect(await getCookieValue(context, cookieNames.legacyConsentLevel)).toBeUndefined();
    expect(await getCookieValue(context, cookieNames.legacyLevelSelection)).toBeUndefined();
    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    await expectNoActivityPost(page, recorder);
  });
});

test.describe("site alerts", () => {
  test("a dismissal is remembered at Essential", async ({ page, context }) => {
    await page.goto("/");
    await necessaryOnly(page);
    await page.reload();

    await openNotifications(page);
    await expect(alertItem(page, everyoneAlerts.qAndAGuidelines)).toBeVisible();
    await dismissAlert(page, everyoneAlerts.qAndAGuidelines);

    await page.reload();
    await openNotifications(page);
    await expect(alertItem(page, everyoneAlerts.qAndAGuidelines)).toHaveCount(0);
    await expect(alertItem(page, everyoneAlerts.communityNews)).toBeVisible();

    expect(await getCookieValue(context, "kcp.notification-cleared")).toBeTruthy();
    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();
  });

  test("contact group alerts are hidden without a tracked contact", async ({ page }) => {
    await page.goto("/");
    await necessaryOnly(page);

    const response = await page.reload();
    expect(response?.ok()).toBe(true);

    await openNotifications(page);
    await expect(alertItem(page, everyoneAlerts.communityNews)).toBeVisible();
    await expect(alertItem(page, personalizedAlerts.welcomeUnregistered)).toHaveCount(0);
  });
});

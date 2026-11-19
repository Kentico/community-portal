import { expect, test, type BrowserContext, type Page } from "@playwright/test";
import {
  acceptAll,
  consentSelectors,
  necessaryOnly,
  expectBannerHidden,
  expectBannerVisible,
  openSettings,
  saveAnalyticsOnly,
  setToggles,
} from "../../support/privacy/consent.js";
import {
  cookieLevels,
  cookieNames,
  getConsentCookie,
  getCookieLevel,
  getCookieValue,
  getCurrentContactGuid,
} from "../../support/privacy/cookies.js";
import {
  consentNames,
  getConsentAgreementsByContact,
  getContactsCreatedBy,
  hasActiveAgreement,
  setReadOnlyMode,
  withCorrelation,
} from "../../support/privacy/state.js";

/*
 * Read-only mode: no consent agreement can be recorded or revoked.
 * - Asking for tracking saves a provisional choice (analytics as chosen, tracking off) that holds
 *   while read-only lasts; afterwards the banner asks again.
 * - Withdrawing stops tracking at once; the agreement is revoked when read-only ends, unless the
 *   visitor asks for tracking again first.
 * - A visitor with an active agreement stays tracked ("Save choices" and "Accept all").
 * - No contact is created while read-only.
 *
 * Read-only mode is process-wide, so this runs in the `isolated-tests` project after all other
 * specs, and always switches it back off.
 */
test.describe.configure({ mode: "serial" });

// Safety net; tests also switch it off mid-test to check what happens afterwards
test.afterEach(async () => {
  await setReadOnlyMode(false);
});

/** Accepts all outside read-only mode. Returns the tracked contact. */
async function startTracking(page: Page, context: BrowserContext): Promise<string> {
  await page.goto("/");
  await acceptAll(page);
  expect(await getCookieLevel(context)).toBe(cookieLevels.all);
  const contactGuid = await getCurrentContactGuid(context);
  expect(contactGuid).toBeTruthy();

  return contactGuid!;
}

async function hasTrackingAgreement(contactGuid: string): Promise<boolean> {
  return hasActiveAgreement(
    await getConsentAgreementsByContact(contactGuid, consentNames.websiteTracking),
    consentNames.websiteTracking,
  );
}

/** Tracking on, backed by the same contact's agreement. */
async function expectStillTracked(context: BrowserContext, contactGuid: string): Promise<void> {
  expect(await getCookieLevel(context)).toBe(cookieLevels.all);
  expect(await getConsentCookie(context)).toMatchObject({
    tracking: true,
    provisional: false,
    pendingRevoke: undefined,
  });
  expect(await getCurrentContactGuid(context)).toBe(contactGuid);
  expect(await hasTrackingAgreement(contactGuid)).toBe(true);
}

test("read-only mode: Accept all is provisional and asked again afterwards", async ({
  page,
  context,
}) => {
  try {
    expect(await setReadOnlyMode(true)).toBe(true);
    // Read-only: the consent POST may only change response cookies, never create a contact
    const correlation = await withCorrelation(context);

    await page.goto("/");
    await expectBannerVisible(page);
    await expect(page.locator(consentSelectors.bannerReadOnlyNote)).toBeVisible();
    await expect(page.locator(consentSelectors.bannerTrackingBullet)).toHaveCount(0);

    await acceptAll(page);

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getConsentCookie(context)).toMatchObject({
      version: "v2",
      analytics: true,
      tracking: false,
      provisional: true,
    });
    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();

    // Holds for the rest of the read-only period
    await openSettings(page);
    await expectBannerHidden(page);
    await expect(page.locator(consentSelectors.settingsTrackingToggle)).toBeDisabled();
    await expect(page.locator(consentSelectors.settingsTrackingToggle)).not.toBeChecked();
    await expect(page.locator(consentSelectors.settingsReadOnlyNote)).toBeVisible();
    await expect(page.locator(consentSelectors.settingsAnalyticsToggle)).toBeChecked();
    await page.goto("/blog");
    await expectBannerHidden(page);
    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getContactsCreatedBy(correlation)).toEqual([]);
  } finally {
    await setReadOnlyMode(false);
  }

  await page.goto("/");
  await expectBannerVisible(page);
  await expect(page.locator(consentSelectors.bannerTrackingBullet)).toBeVisible();
  expect(await getConsentCookie(context)).toBeUndefined();
  expect(await getCookieLevel(context)).toBe(cookieLevels.essential);

  await acceptAll(page);

  expect(await getCookieLevel(context)).toBe(cookieLevels.all);
  expect(await getConsentCookie(context)).toMatchObject({
    analytics: true,
    tracking: true,
    provisional: false,
  });
  const contactGuid = await getCurrentContactGuid(context);
  expect(contactGuid).toBeTruthy();
  const agreements = await getConsentAgreementsByContact(
    contactGuid!,
    consentNames.websiteTracking,
  );
  expect(hasActiveAgreement(agreements, consentNames.websiteTracking)).toBe(true);
});

test("read-only mode: withdrawing revokes the agreement once read-only ends", async ({
  page,
  context,
}) => {
  await page.goto("/");
  await acceptAll(page);
  const contactGuid = await getCurrentContactGuid(context);
  expect(contactGuid).toBeTruthy();

  try {
    expect(await setReadOnlyMode(true)).toBe(true);
    const correlation = await withCorrelation(context);

    await openSettings(page);
    await necessaryOnly(page, "settings");

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getCookieValue(context, cookieNames.currentContact)).toBeUndefined();
    expect(await getConsentCookie(context)).toMatchObject({
      analytics: false,
      tracking: false,
      pendingRevoke: contactGuid,
    });
    expect(
      hasActiveAgreement(
        await getConsentAgreementsByContact(contactGuid!, consentNames.websiteTracking),
        consentNames.websiteTracking,
      ),
    ).toBe(true);
    expect(await getContactsCreatedBy(correlation)).toEqual([]);
  } finally {
    await setReadOnlyMode(false);
  }

  await page.goto("/blog");
  await expectBannerHidden(page);
  expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
  expect(await getConsentCookie(context)).toMatchObject({
    analytics: false,
    tracking: false,
    provisional: false,
    pendingRevoke: undefined,
  });
  expect(
    hasActiveAgreement(
      await getConsentAgreementsByContact(contactGuid!, consentNames.websiteTracking),
      consentNames.websiteTracking,
    ),
  ).toBe(false);
});

test("read-only mode: Save choices keeps tracking on for a tracked visitor", async ({
  page,
  context,
}) => {
  const contactGuid = await startTracking(page, context);

  try {
    expect(await setReadOnlyMode(true)).toBe(true);
    const correlation = await withCorrelation(context);

    await openSettings(page);
    await expect(page.locator(consentSelectors.settingsTrackingToggle)).toBeDisabled();
    await expect(page.locator(consentSelectors.settingsTrackingToggle)).toBeChecked();

    // The disabled switch isn't posted; that must not read as a withdrawal
    await saveAnalyticsOnly(page, false);

    await expectStillTracked(context, contactGuid);
    expect(await getConsentCookie(context)).toMatchObject({ analytics: false });
    expect(await getContactsCreatedBy(correlation)).toEqual([]);
  } finally {
    await setReadOnlyMode(false);
  }

  await page.goto("/blog");
  await expectBannerHidden(page);
  await expectStillTracked(context, contactGuid);
});

test("read-only mode: Accept all keeps tracking on for a visitor with an agreement", async ({
  page,
  context,
}) => {
  const contactGuid = await startTracking(page, context);
  // Analytics off first, so Accept all has something to save
  await openSettings(page);
  await setToggles(page, { analytics: false, tracking: true });

  try {
    expect(await setReadOnlyMode(true)).toBe(true);
    const correlation = await withCorrelation(context);

    await openSettings(page);
    await acceptAll(page, "settings");

    await expectStillTracked(context, contactGuid);
    expect(await getConsentCookie(context)).toMatchObject({ analytics: true });
    expect(await getContactsCreatedBy(correlation)).toEqual([]);
  } finally {
    await setReadOnlyMode(false);
  }

  await page.goto("/blog");
  await expectBannerHidden(page);
  await expectStillTracked(context, contactGuid);
});

test("read-only mode: Accept all after withdrawing cancels the pending revoke", async ({
  page,
  context,
}) => {
  const contactGuid = await startTracking(page, context);

  try {
    expect(await setReadOnlyMode(true)).toBe(true);

    await openSettings(page);
    await necessaryOnly(page, "settings");
    expect(await getConsentCookie(context)).toMatchObject({ pendingRevoke: contactGuid });

    await acceptAll(page, "settings");

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getConsentCookie(context)).toMatchObject({
      tracking: false,
      provisional: true,
      pendingRevoke: undefined,
    });
    expect(await hasTrackingAgreement(contactGuid)).toBe(true);
  } finally {
    await setReadOnlyMode(false);
  }

  // The provisional choice is dropped and the visitor asked again; the agreement stands
  await page.goto("/blog");
  await expectBannerVisible(page);
  expect(await getConsentCookie(context)).toBeUndefined();
  expect(await hasTrackingAgreement(contactGuid)).toBe(true);
});

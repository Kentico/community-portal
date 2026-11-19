import { expect, test, type Browser, type Page } from "@playwright/test";
import { appBaseUrl } from "../../support/config.js";
import {
  createRegistrationIdentity,
  type RegistrationIdentity,
} from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  confirmEmail,
  logout,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import {
  acceptAll,
  expectBannerHidden,
  expectBannerVisible,
  necessaryOnly,
} from "../../support/privacy/consent.js";
import {
  cookieLevels,
  expectNoTrackingCookies,
  getConsentCookie,
  getCookie,
  getCookieLevel,
  getCurrentContactGuid,
  setCookies,
} from "../../support/privacy/cookies.js";
import {
  expectNoActivityPost,
  expectNoGoogleTracking,
  RequestRecorder,
  waitForActivityPost,
} from "../../support/privacy/network.js";
import {
  consentNames,
  getConsentAgreements,
  getContactByEmail,
  getContactsCreatedBy,
  hasActiveAgreement,
  withCorrelation,
} from "../../support/privacy/state.js";
import { setMemberRole } from "../../support/testingTools/memberRoleClient.js";

/*
 * Tracking gate: nothing tracks before consent, sign-out forgets the visitor, sign-in restores
 * tracking only for members who agreed, and role content works without tracking.
 *
 * Serial: the specs share two members to keep registrations (and the run) short. "No contact"
 * asserts check only contacts this browser's requests created, so parallel specs don't affect them.
 */
test.describe.configure({ mode: "serial" });

type Choice = "accept" | "necessary";

const members = new Map<Choice, RegistrationIdentity>();

async function newContext(browser: Browser) {
  return browser.newContext({ baseURL: appBaseUrl, ignoreHTTPSErrors: true });
}

/** Signs in and waits for the post-sign-in redirect, so a following navigation isn't interrupted. */
async function signInAndWait(
  page: Page,
  identity: RegistrationIdentity,
): Promise<void> {
  await signIn(page, identity.username, identity.password);
  await page.waitForURL((url) => !url.pathname.startsWith("/login"));
}

/**
 * A member who chose in the banner, then registered, confirmed and signed in, all in one browser
 * (since closed). Created once per choice and reused.
 */
async function getMember(
  browser: Browser,
  choice: Choice,
): Promise<RegistrationIdentity> {
  const existing = members.get(choice);
  if (existing) {
    return existing;
  }

  const identity = createRegistrationIdentity(`playwright-gate-${choice}`);
  const emailClient = await createMcpClient();
  const context = await newContext(browser);

  try {
    const page = await context.newPage();
    await page.goto("/");
    if (choice === "accept") {
      await acceptAll(page);
    } else {
      await necessaryOnly(page);
    }

    await registerMember(page, identity);
    await confirmEmail(page, emailClient, identity);
    await signInAndWait(page, identity);
  } finally {
    await context.close();
    await emailClient.close();
  }

  members.set(choice, identity);

  return identity;
}

async function signInFresh(browser: Browser, identity: RegistrationIdentity) {
  const context = await newContext(browser);
  const page = await context.newPage();
  await signInAndWait(page, identity);
  await page.goto("/");
  await assertAuthenticated(page, identity.username);

  return { context, page };
}

test.describe("first visit", () => {
  test("sets no tracking cookies, logs no activity and creates no contact", async ({
    page,
    context,
  }) => {
    const correlation = await withCorrelation(context);
    const recorder = RequestRecorder.attach(page);

    await page.goto("/");
    await page.waitForLoadState("load");
    await page.goto("/blog");
    await page.waitForLoadState("load");

    await expectBannerVisible(page);
    await expectNoTrackingCookies(context);
    await expectNoActivityPost(page, recorder);
    expectNoGoogleTracking(recorder);
    expect(await getConsentCookie(context)).toBeUndefined();

    expect(await getContactsCreatedBy(correlation)).toEqual([]);
  });
});

test.describe("sign-out", () => {
  test("forgets the visitor without creating a contact", async ({
    page,
    context,
    browser,
  }) => {
    const identity = await getMember(browser, "accept");
    await signInAndWait(page, identity);
    await page.goto("/");
    await assertAuthenticated(page, identity.username);
    expect(await getCookieLevel(context)).toBe(cookieLevels.all);

    // Host-only GA cookies, as GA4 sets them once `cookie_domain` is the portal host
    await setCookies(context, { _ga: "GA1.1.123.456", _ga_TEST: "GS1.1.1" });

    const correlation = await withCorrelation(context);
    const recorder = RequestRecorder.attach(page);
    await logout(page);
    await page.waitForLoadState("load");

    expect(await getCookieLevel(context)).toBe(cookieLevels.essential);
    expect(await getConsentCookie(context)).toBeUndefined();
    expect(await getCookie(context, "_ga")).toBeUndefined();
    expect(await getCookie(context, "_ga_TEST")).toBeUndefined();
    await expectNoTrackingCookies(context);
    await expectBannerVisible(page);
    await expectNoActivityPost(page, recorder);

    expect(await getContactsCreatedBy(correlation)).toEqual([]);
  });
});

test.describe("sign-in", () => {
  test("restores tracking for a member who agreed", async ({ browser }) => {
    const identity = await getMember(browser, "accept");

    const fresh = await signInFresh(browser, identity);

    try {
      expect(await getCookieLevel(fresh.context)).toBe(cookieLevels.all);
      expect(await getConsentCookie(fresh.context)).toMatchObject({ tracking: true });
      await expectBannerHidden(fresh.page);

      const activity = waitForActivityPost(fresh.page);
      await fresh.page.goto("/blog");
      await activity;
    } finally {
      await fresh.context.close();
    }
  });

  test("the agreement follows the member to their email contact", async ({
    browser,
  }) => {
    // Accept → register → confirm → sign in, in one browser
    const identity = await getMember(browser, "accept");

    const agreements = await getConsentAgreements(
      identity.email,
      consentNames.websiteTracking,
    );
    expect(hasActiveAgreement(agreements, consentNames.websiteTracking)).toBe(true);
  });

  test("leaves a member who didn't agree untracked", async ({ browser }) => {
    const identity = await getMember(browser, "necessary");

    const fresh = await signInFresh(browser, identity);

    try {
      expect((await getCookieLevel(fresh.context)) ?? 0).toBe(cookieLevels.essential);
      expect(await getCurrentContactGuid(fresh.context)).toBeUndefined();
      await expectBannerVisible(fresh.page);

      // Untracked sign-in still syncs the member to an email contact, server side
      expect((await getContactByEmail(identity.email)).contacts).toHaveLength(1);
    } finally {
      await fresh.context.close();
    }
  });
});

test.describe("role content at Essential", () => {
  test("an MVP sees the MVP variant without tracking", async ({ page, context, browser }) => {
    const identity = await getMember(browser, "necessary");

    try {
      await setMemberRole(identity.username, "KenticoMVP", true);
      await signInAndWait(page, identity);

      const recorder = RequestRecorder.attach(page);
      await page.goto("/community-programs-dashboard-2026");
      await page.waitForLoadState("load");

      expect((await getCookieLevel(context)) ?? 0).toBe(cookieLevels.essential);
      await expect(page.getByText("MVP content", { exact: true })).toBeVisible();
      await expect(page.getByText("Default content", { exact: true })).toHaveCount(0);
      await expectNoActivityPost(page, recorder);
    } finally {
      await setMemberRole(identity.username, "KenticoMVP", false);
    }
  });

  test("a Community Leader doesn't see the MVP variant", async ({ page, browser }) => {
    const identity = await getMember(browser, "necessary");

    try {
      await setMemberRole(identity.username, "KenticoCommunityLeader", true);
      await signInAndWait(page, identity);
      await page.goto("/community-programs-dashboard-2026");

      await expect(
        page.getByText("Community Leader content", { exact: true }),
      ).toBeVisible();
      await expect(page.getByText("MVP content", { exact: true })).toHaveCount(0);
    } finally {
      await setMemberRole(identity.username, "KenticoCommunityLeader", false);
    }
  });
});

test.describe("contact merge", () => {
  // Last: gives the "necessary" member an agreement
  test("the agreement survives merging a second browser's contact", async ({
    page,
    context,
    browser,
  }) => {
    const identity = await getMember(browser, "necessary");
    const emailContacts = await getContactByEmail(identity.email);
    expect(emailContacts.contacts).toHaveLength(1);

    await page.goto("/");
    await acceptAll(page);
    const browserContactGuid = await getCurrentContactGuid(context);
    expect(browserContactGuid).toBeTruthy();
    expect(browserContactGuid).not.toBe(emailContacts.contacts[0]?.contactGUID);

    await signInAndWait(page, identity);
    await page.goto("/");
    await assertAuthenticated(page, identity.username);

    // Signing in merges the browser contact (and its agreement) into the member's email contact
    const merged = await getContactByEmail(identity.email);
    expect(merged.contacts).toHaveLength(1);
    expect(await getCurrentContactGuid(context)).toBe(merged.contacts[0]?.contactGUID);
    expect(await getCookieLevel(context)).toBe(cookieLevels.all);

    const agreements = await getConsentAgreements(
      identity.email,
      consentNames.websiteTracking,
    );
    expect(hasActiveAgreement(agreements, consentNames.websiteTracking)).toBe(true);
  });
});

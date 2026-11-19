import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import type { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import { getEmailTracking } from "../../support/email/tracking.js";
import type { VirtualEmail } from "../../support/email/virtualEmail.js";
import { waitForEmail } from "../../support/email/waitForEmail.js";
import {
  confirmEmail,
  registerMember,
  type RegisterOptions,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { acceptAll, necessaryOnly } from "../../support/privacy/consent.js";
import {
  cookieNames,
  getCookie,
  getCurrentContactGuid,
} from "../../support/privacy/cookies.js";
import { expectNoActivityPost, RequestRecorder } from "../../support/privacy/network.js";
import {
  activityTypes,
  consentNames,
  getActivities,
  getConsentAgreements,
  getConsentAgreementsByContact,
  getContactByEmail,
  getContactsCreatedBy,
  withCorrelation,
} from "../../support/privacy/state.js";

/*
 * Email open/click tracking follows the Website tracking consent, registration works at
 * Essential (necessary cookies only), and nothing writes the legacy consent.
 *
 * Every email gets Xperience's anonymous statistics pixel and redirect links. "Tracked" here means
 * tracked to the recipient's contact: the links carry `contactGuid` and a click logs an Email click
 * activity. See support/email/tracking.ts.
 */

/** Sent by the `MemberWelcome` automation (trigger: Member registration activity, no wait). */
const welcomeSubject = "Welcome to the Kentico Community Portal";
/** Xperience queues activities and writes them in batches (every 2s in Development/CI). */
const activityLogTimeoutMs = 10_000;

async function waitForWelcomeEmail(
  emailClient: Client,
  inbox: string,
): Promise<VirtualEmail> {
  return waitForEmail(emailClient, {
    inbox,
    subjectContains: welcomeSubject,
  });
}

/**
 * Requests a tracked link without following the redirect, which points at the production site in
 * the email content.
 */
async function clickTrackedLink(
  request: APIRequestContext,
  url: string,
): Promise<void> {
  const response = await request.get(url, { maxRedirects: 0 });
  expect(response.status(), `tracked link ${url}`).toBeGreaterThanOrEqual(300);
  expect(response.status()).toBeLessThan(400);
}

async function registerAndConfirm(
  page: Page,
  emailClient: Client,
  choice: "accept" | "necessary",
  options: RegisterOptions = {},
) {
  const identity = createRegistrationIdentity(`playwright-email-${choice}`);

  await page.goto("/");
  if (choice === "accept") {
    await acceptAll(page);
  } else {
    await necessaryOnly(page);
  }

  await registerMember(page, identity, options);
  await confirmEmail(page, emailClient, identity);

  return identity;
}

test.describe("email tracking", () => {
  // Registration, confirmation and the email wait can pass Playwright's default 30s per test
  test.describe.configure({ timeout: 60_000 });

  test("Necessary only: the welcome email isn't tracked to the contact", async ({
    page,
  }) => {
    const emailClient = await createMcpClient();

    try {
      const identity = await registerAndConfirm(page, emailClient, "necessary");

      // The Member registration activity is logged at Essential too...
      await expect
        .poll(
          async () =>
            (await getActivities(identity.email, activityTypes.memberRegistration))
              .length,
          { timeout: activityLogTimeoutMs },
        )
        .toBe(1);

      // ...and starts the MemberWelcome automation
      const welcome = await waitForWelcomeEmail(emailClient, identity.email);
      const tracking = getEmailTracking(welcome);

      // Anonymous statistics links only; nothing identifies the contact, so a click can't be
      // logged as an activity
      expect(tracking.trackedLinks.length).toBeGreaterThan(0);
      expect(tracking.contactGuids).toEqual([]);
    } finally {
      await emailClient.close();
    }
  });

  test("Accept all: the welcome email is tracked to the member's contact", async ({
    page,
    request,
  }) => {
    const emailClient = await createMcpClient();

    try {
      // No newsletter opt-in: Website tracking alone allows email tracking
      const identity = await registerAndConfirm(page, emailClient, "accept");

      const welcome = await waitForWelcomeEmail(emailClient, identity.email);
      const tracking = getEmailTracking(welcome);

      const contacts = (await getContactByEmail(identity.email)).contacts;
      expect(contacts).toHaveLength(1);
      expect(tracking.trackedLinks.length).toBeGreaterThan(0);
      expect(tracking.contactGuids).toEqual([contacts[0]!.contactGUID]);

      await clickTrackedLink(request, tracking.trackedLinks[0]!);
      await expect
        .poll(
          async () =>
            (await getActivities(identity.email, activityTypes.emailClick)).length,
          { timeout: activityLogTimeoutMs },
        )
        .toBe(1);
    } finally {
      await emailClient.close();
    }
  });
});

test.describe("registration at Essential", () => {
  test("registering creates an email contact but no tracking", async ({
    page,
    context,
  }) => {
    const emailClient = await createMcpClient();

    try {
      const correlation = await withCorrelation(context);
      const identity = await registerAndConfirm(page, emailClient, "necessary");

      const contacts = (await getContactByEmail(identity.email)).contacts;
      expect(contacts).toHaveLength(1);
      // Only the email contact (inserted before its email is set); no anonymous browser contact
      const created = await getContactsCreatedBy(correlation);
      expect(created.map((contact) => contact.contactGUID)).toEqual([
        contacts[0]!.contactGUID,
      ]);

      expect(await getCookie(context, cookieNames.currentContact)).toBeUndefined();

      const recorder = RequestRecorder.attach(page);
      await page.goto("/blog");
      await page.waitForLoadState("load");
      await expectNoActivityPost(page, recorder);
      expect(await getCookie(context, cookieNames.currentContact)).toBeUndefined();
    } finally {
      await emailClient.close();
    }
  });
});

test.describe("legacy consent", () => {
  test("registering with every checkbox ticked records no legacy agreement", async ({
    page,
    context,
  }) => {
    const emailClient = await createMcpClient();
    const identity = createRegistrationIdentity("playwright-email-legacy");

    try {
      // Accept all: the browser has a contact, so an agreement could be recorded
      await page.goto("/");
      await acceptAll(page);
      const browserContactGuid = await getCurrentContactGuid(context);
      expect(browserContactGuid).toBeTruthy();

      await registerMember(page, identity, { subscribeToNewsletter: true });
      await confirmEmail(page, emailClient, identity);

      expect(
        await getConsentAgreementsByContact(
          browserContactGuid!,
          consentNames.legacyTracking,
        ),
      ).toEqual([]);
      expect(
        await getConsentAgreements(identity.email, consentNames.legacyTracking),
      ).toEqual([]);
    } finally {
      await emailClient.close();
    }
  });
});

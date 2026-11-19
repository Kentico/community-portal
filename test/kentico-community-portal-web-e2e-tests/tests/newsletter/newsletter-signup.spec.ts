import { expect, test, type APIRequestContext } from "@playwright/test";
import { getEmailTracking } from "../../support/email/tracking.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import {
  consentPlainText,
  extractSubscriptionConfirmationUrl,
  newsletterPages,
  newsletterSelectors,
  normalizeWhitespace,
  submitNewsletterForm,
  waitForNewsletterConfirmationEmail,
} from "../../support/newsletter/flows.js";
import { acceptAll, necessaryOnly } from "../../support/privacy/consent.js";
import {
  activityTypes,
  consentNames,
  emailMarketingReconciliationTask,
  getActivities,
  getConsent,
  getConsentAgreements,
  getContactByEmail,
  getRecipientListState,
  hasActiveAgreement,
  runScheduledTask,
} from "../../support/privacy/state.js";

/*
 * The newsletter sign-up has no consent checkbox. It shows the Email marketing consent short
 * text, and the double opt-in click records the agreement.
 */

/** Xperience queues activities and writes them in batches (every 2s in Development/CI). */
const activityLogTimeoutMs = 10_000;

function newsletterEmail(label: string): string {
  return `playwright-newsletter-${label}.${Date.now()}-${Math.floor(Math.random() * 10_000)}@kentico.local`;
}

/** Requests a tracked link without following the redirect (which points at the production site). */
async function clickTrackedLink(request: APIRequestContext, url: string): Promise<void> {
  const response = await request.get(url, { maxRedirects: 0 });
  expect(response.status(), `tracked link ${url}`).toBeGreaterThanOrEqual(300);
  expect(response.status()).toBeLessThan(400);
}

test.describe("newsletter sign-up widget", () => {
  for (const path of newsletterPages) {
    test(`${path} shows the Email marketing text and no consent checkbox`, async ({ page }) => {
      const consent = await getConsent(consentNames.emailMarketing);

      await page.goto(path);

      const widget = page.locator(newsletterSelectors.widget);
      await expect(widget).toBeVisible();
      // The heading text is editor content and differs per page; assert the hook only
      await expect(widget.locator(newsletterSelectors.heading)).toBeVisible();

      // The same text the agreement records, as edited in the admin
      expect(
        normalizeWhitespace(
          await widget.locator(newsletterSelectors.consentText).innerText(),
        ),
      ).toBe(consentPlainText(consent.shortText));

      const form = widget.locator(newsletterSelectors.form);
      await expect(form.locator("input[type=email]")).toBeVisible();
      await expect(form.locator("input[type=checkbox]")).toHaveCount(0);
      const submit = form.locator("[type=submit]");
      expect(
        await submit.evaluate(
          (element) => (element as HTMLInputElement).value || element.textContent || "",
        ),
      ).toContain("Subscribe");

      await expect(widget.locator(newsletterSelectors.helpText)).toHaveText(
        "We'll email you a confirmation link. You're subscribed only after you click it.",
      );
    });
  }
});

test.describe("newsletter double opt-in", () => {
  test.describe.configure({ timeout: 60_000 });

  for (const path of newsletterPages) {
    test(`${path}: subscribed with an agreement only after the confirmation click`, async ({
      page,
    }) => {
      const emailClient = await createMcpClient();
      const email = newsletterEmail("optin");

      try {
        const consent = await getConsent(consentNames.emailMarketing);

        await page.goto(path);
        await necessaryOnly(page);
        await submitNewsletterForm(page, email);

        const confirmation = await waitForNewsletterConfirmationEmail(emailClient, email);

        // Not yet confirmed: no subscription, no agreement
        const pending = await getRecipientListState(email);
        expect(pending.subscribed).toBe(false);
        expect(pending.isListMember).toBe(false);
        expect(await getConsentAgreements(email, consentNames.emailMarketing)).toEqual([]);

        // Necessary only: anonymous email statistics only, nothing identifies the contact
        expect(getEmailTracking(confirmation).contactGuids).toEqual([]);

        await page.goto(extractSubscriptionConfirmationUrl(confirmation));

        await expect
          .poll(async () => (await getRecipientListState(email)).subscribed)
          .toBe(true);

        // Recorded by the confirmation event handler, without the reconciliation task
        const agreements = await getConsentAgreements(email, consentNames.emailMarketing);
        expect(hasActiveAgreement(agreements, consentNames.emailMarketing)).toBe(true);
        expect(agreements.at(-1)!.agreementConsentHash).toBe(consent.hash);

        // Nothing writes the retired consent any more
        expect(await getConsentAgreements(email, consentNames.legacyTracking)).toEqual([]);
      } finally {
        await emailClient.close();
      }
    });
  }

  test("no confirmation click: not subscribed and no agreement, even after reconciliation", async ({
    page,
  }) => {
    const emailClient = await createMcpClient();
    const email = newsletterEmail("noclick");

    try {
      await page.goto("/newsletter-signup");
      await necessaryOnly(page);
      await submitNewsletterForm(page, email);
      await waitForNewsletterConfirmationEmail(emailClient, email);

      await runScheduledTask(emailMarketingReconciliationTask);

      expect((await getRecipientListState(email)).subscribed).toBe(false);
      expect(await getConsentAgreements(email, consentNames.emailMarketing)).toEqual([]);
    } finally {
      await emailClient.close();
    }
  });
});

test.describe("newsletter email tracking", () => {
  test.describe.configure({ timeout: 60_000 });

  /*
   * Stands in for a newsletter test send: the confirmation email comes from the
   * same email channel and evaluator. Pixel and redirect links are always present (pseudonymous
   * statistics), so "tracked" means the links carry the contact GUID and a click logs an activity.
   */
  test("Accept all: the confirmation email is tracked to the subscriber's contact", async ({
    page,
    request,
  }) => {
    const emailClient = await createMcpClient();
    const email = newsletterEmail("tracked");

    try {
      await page.goto("/newsletter-signup");
      await acceptAll(page);
      await submitNewsletterForm(page, email);

      const confirmation = await waitForNewsletterConfirmationEmail(emailClient, email);
      const tracking = getEmailTracking(confirmation);

      const contacts = (await getContactByEmail(email)).contacts;
      expect(contacts).toHaveLength(1);
      expect(tracking.trackedLinks.length).toBeGreaterThan(0);
      expect(tracking.contactGuids).toEqual([contacts[0]!.contactGUID]);

      await clickTrackedLink(request, tracking.trackedLinks[0]!);
      await expect
        .poll(
          async () => (await getActivities(email, activityTypes.emailClick)).length,
          { timeout: activityLogTimeoutMs },
        )
        .toBe(1);
    } finally {
      await emailClient.close();
    }
  });
});

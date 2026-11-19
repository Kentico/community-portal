import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  confirmEmail,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import {
  accountNewsletterItem as newsletterItem,
  clickAndWaitForAccountAction as clickAndWaitFor,
  consentPlainText,
  normalizeWhitespace,
} from "../../support/newsletter/flows.js";
import { cookieSettingsPath } from "../../support/privacy/consent.js";
import {
  consentNames,
  getConsent,
  getConsentAgreements,
  getNewsletterUnsubscribeUrl,
  getRecipientListState,
  hasActiveAgreement,
} from "../../support/privacy/state.js";

/*
 * Members subscribe from their account page without a confirmation email (their address is
 * verified). Subscribe agrees to the Email marketing consent inline; unsubscribe revokes it.
 */

function today(): string {
  const now = new Date();
  const pad = (value: number) => value.toString().padStart(2, "0");

  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

test.describe("account page newsletter subscription", () => {
  test.describe.configure({ timeout: 60_000 });

  test("subscribe agrees, unsubscribe keeps history and revokes, subscribe again agrees", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-newsletter-account");
    const emailClient = await createMcpClient();

    try {
      const consent = await getConsent(consentNames.emailMarketing);

      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);
      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);

      await page.goto("/my-account");

      const item = page.locator(newsletterItem);
      await expect(item).toBeVisible();
      expect(
        normalizeWhitespace(
          await item.locator("[test-id=recipientListConsentText]").innerText(),
        ),
      ).toBe(consentPlainText(consent.shortText));
      await expect(item.locator("[test-id=recipientListSubscribeHelp]")).toHaveText(
        "No confirmation email needed. Your address is already verified.",
      );

      await clickAndWaitFor(
        page,
        `${newsletterItem} [test-id=recipientListSubscribe]`,
        "subscriberecipientlist",
      );

      // Subscribed at once, with an agreement to the text shown
      await expect(
        page.locator(`${newsletterItem} [test-id=recipientListSubscribedSince]`),
      ).toHaveText(`Subscribed since ${today()}.`);
      expect((await getRecipientListState(identity.email)).subscribed).toBe(true);

      const agreed = await getConsentAgreements(identity.email, consentNames.emailMarketing);
      expect(hasActiveAgreement(agreed, consentNames.emailMarketing)).toBe(true);
      expect(agreed.at(-1)!.agreementConsentHash).toBe(consent.hash);

      await clickAndWaitFor(
        page,
        `${newsletterItem} [test-id=recipientListUnsubscribe]`,
        "unsubscriberecipientlist",
      );

      await expect(
        page.locator(`${newsletterItem} [test-id=recipientListSubscribe]`),
      ).toBeVisible();

      // Marked unsubscribed like Xperience's email link does: still a list member, history kept,
      // plus an unapproved row
      const unsubscribed = await getRecipientListState(identity.email);
      expect(unsubscribed.subscribed).toBe(false);
      expect(unsubscribed.isListMember).toBe(true);
      expect(unsubscribed.confirmations.map((c) => c.isApproved)).toEqual([true, false]);

      // Revoked: a new revoked row; the agreement row stays as the record
      const revoked = await getConsentAgreements(identity.email, consentNames.emailMarketing);
      expect(hasActiveAgreement(revoked, consentNames.emailMarketing)).toBe(false);
      expect(revoked.at(-1)!.revoked).toBe(true);

      // Subscribe again: a new approved row and a new agreement
      await clickAndWaitFor(
        page,
        `${newsletterItem} [test-id=recipientListSubscribe]`,
        "subscriberecipientlist",
      );
      await expect(
        page.locator(`${newsletterItem} [test-id=recipientListSubscribedSince]`),
      ).toHaveText(`Subscribed since ${today()}.`);

      const resubscribed = await getRecipientListState(identity.email);
      expect(resubscribed.subscribed).toBe(true);
      expect(resubscribed.confirmations.map((c) => c.isApproved)).toEqual([true, false, true]);

      const reagreed = await getConsentAgreements(identity.email, consentNames.emailMarketing);
      expect(hasActiveAgreement(reagreed, consentNames.emailMarketing)).toBe(true);
      expect(reagreed.length).toBeGreaterThan(revoked.length);
    } finally {
      await emailClient.close();
    }
  });

  test("account page unsubscribe leaves the same state as the email link", async ({
    page,
    request,
  }) => {
    const identity = createRegistrationIdentity("playwright-newsletter-unsub-match");
    const emailClient = await createMcpClient();

    async function unsubscribedState() {
      const state = await getRecipientListState(identity.email);
      const agreements = await getConsentAgreements(identity.email, consentNames.emailMarketing);

      return {
        subscribed: state.subscribed,
        isListMember: state.isListMember,
        isConfirmationApproved: state.isConfirmationApproved,
        confirmations: state.confirmations.length,
        agreed: hasActiveAgreement(agreements, consentNames.emailMarketing),
      };
    }

    try {
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);
      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);

      await page.goto("/my-account");
      await clickAndWaitFor(
        page,
        `${newsletterItem} [test-id=recipientListSubscribe]`,
        "subscriberecipientlist",
      );

      // Xperience's own unsubscribe endpoint, as linked from a newsletter (one-click POST)
      const response = await request.post(await getNewsletterUnsubscribeUrl(identity.email), {
        maxRedirects: 0,
      });
      expect(response.status(), await response.text()).toBeLessThan(400);
      const byEmailLink = await unsubscribedState();

      await page.reload();
      await clickAndWaitFor(
        page,
        `${newsletterItem} [test-id=recipientListSubscribe]`,
        "subscriberecipientlist",
      );
      await clickAndWaitFor(
        page,
        `${newsletterItem} [test-id=recipientListUnsubscribe]`,
        "unsubscriberecipientlist",
      );
      const byAccountPage = await unsubscribedState();

      expect(byEmailLink).toEqual({
        subscribed: false,
        isListMember: true,
        isConfirmationApproved: false,
        confirmations: 2,
        agreed: false,
      });
      // Each unsubscribe adds one unapproved row to one approved row
      expect(byAccountPage).toEqual({ ...byEmailLink, confirmations: 4 });
    } finally {
      await emailClient.close();
    }
  });

  test("privacy section links to cookie settings and the privacy contact", async ({ page }) => {
    const identity = createRegistrationIdentity("playwright-newsletter-privacy");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);
      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);

      await page.goto("/my-account");

      const privacy = page.locator("[test-id=privacySettings]");
      await expect(privacy.locator("[test-id=privacyCookieSettingsLink]")).toHaveAttribute(
        "href",
        cookieSettingsPath,
      );
      await expect(privacy.locator("a[href='mailto:privacy@kentico.com']")).toBeVisible();
    } finally {
      await emailClient.close();
    }
  });
});

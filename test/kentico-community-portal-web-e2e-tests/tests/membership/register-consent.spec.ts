import { expect, test, type Page } from "@playwright/test";
import * as cheerio from "cheerio";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import { termsVersion } from "../../support/membership/terms.js";
import type { VirtualEmail } from "../../support/email/virtualEmail.js";
import { extractConfirmationUrl } from "../../support/email/extractLinks.js";
import {
  confirmEmail,
  confirmationSelectors,
  fillRegistrationForm,
  openRegistrationForm,
  registerMember,
  registerSelectors,
  waitForConfirmationEmail,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import {
  consentPlainText,
  normalizeWhitespace,
} from "../../support/newsletter/flows.js";
import {
  consentNames,
  findMember,
  getConsent,
  getConsentAgreements,
  getMember,
  getRecipientListState,
  hasActiveAgreement,
} from "../../support/privacy/state.js";

/*
 * Registration requires accepting the Terms of Use (stored on the member, not a consent) and
 * offers an optional newsletter opt-in. The opt-in is only stored at registration; the member is
 * subscribed, and the Email marketing agreement recorded, when they confirm their email.
 */

const termsError = "Please accept the Terms of Use to create an account.";
/** The confirmation email's `TOKEN_NewsletterOptInLine`. */
const optInLine =
  "Confirming your email also subscribes you to Kentico Community news and updates, as you requested. You can unsubscribe at any time.";

function emailText(email: VirtualEmail): { html: string; plainText: string } {
  return {
    html: normalizeWhitespace(cheerio.load(email.virtualEmailBodyHTML ?? "").text()),
    plainText: normalizeWhitespace(email.virtualEmailBodyPlainText ?? ""),
  };
}

async function submitRegistration(page: Page) {
  return Promise.all([
    page.waitForResponse("**/registrationwidget/register"),
    page.locator(`${registerSelectors.form} button[type=submit]`).click(),
  ]);
}

test.describe("registration consent", () => {
  // Registration, the confirmation email and confirming can pass Playwright's default 30s
  test.describe.configure({ timeout: 60_000 });

  test("the form shows the Terms checkbox and the Email marketing text", async ({ page }) => {
    const consent = await getConsent(consentNames.emailMarketing);

    await openRegistrationForm(page);

    const form = page.locator(registerSelectors.form);
    const terms = form.locator(registerSelectors.acceptTerms);
    await expect(terms).not.toBeChecked();
    await expect(terms).toHaveAttribute("required", /.*/);
    await expect(form.locator("a[href='/terms']")).toHaveText("Terms of Use");
    await expect(form.locator("a[href='/privacy']").first()).toHaveText("Privacy notice");

    await expect(form.locator(registerSelectors.subscribeToNewsletter)).not.toBeChecked();
    // The same text the agreement records, as edited in the admin
    expect(
      normalizeWhitespace(
        await form.locator(registerSelectors.newsletterConsentText).innerText(),
      ),
    ).toBe(consentPlainText(consent.shortText));
    await expect(form.locator(registerSelectors.newsletterHelpText)).toHaveText(
      "We'll subscribe you when you confirm your email address.",
    );
  });

  test("Terms unticked: the browser blocks the submit and no member is created", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-terms-client");
    const registerRequests: string[] = [];
    page.on("request", (request) => {
      if (request.url().toLowerCase().includes("/registrationwidget/register")) {
        registerRequests.push(request.url());
      }
    });

    await openRegistrationForm(page);
    await fillRegistrationForm(page, identity, { acceptTerms: false });
    await page.locator(`${registerSelectors.form} button[type=submit]`).click();

    const terms = page.locator(registerSelectors.acceptTerms);
    expect(
      await terms.evaluate((el) => (el as HTMLInputElement).validity.valueMissing),
    ).toBe(true);
    await expect(page.locator("[test-id=confirmationEmailMessage]")).toHaveCount(0);
    expect(registerRequests).toEqual([]);
    expect(await findMember(identity.username)).toBeNull();
  });

  test("Terms unticked: the server rejects the registration and no member is created", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-terms-server");

    await openRegistrationForm(page);
    await fillRegistrationForm(page, identity, { acceptTerms: false });
    // Skip the browser check to prove the server enforces it
    await page
      .locator(registerSelectors.acceptTerms)
      .evaluate((el) => el.removeAttribute("required"));

    const [response] = await submitRegistration(page);
    expect(response.ok()).toBe(true);

    await expect(page.locator(registerSelectors.acceptTermsError)).toHaveText(termsError);
    await expect(page.locator("[test-id=confirmationEmailMessage]")).toHaveCount(0);
    expect(await findMember(identity.username)).toBeNull();
  });

  test("newsletter ticked: subscribed and agreed only after the email is confirmed", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-reg-newsletter");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity, { subscribeToNewsletter: true });

      const registered = await getMember(identity.username);
      expect(registered.termsAcceptedVersion).toBe(termsVersion);
      expect(registered.newsletterOptInRequestedDate).not.toBeNull();

      // Not subscribed before the address is confirmed
      const pending = await getRecipientListState(identity.email);
      expect(pending.subscribed).toBe(false);
      expect(pending.confirmations).toEqual([]);
      expect(
        hasActiveAgreement(
          await getConsentAgreements(identity.email, consentNames.emailMarketing),
          consentNames.emailMarketing,
        ),
      ).toBe(false);

      const email = await waitForConfirmationEmail(emailClient, identity);
      const { html, plainText } = emailText(email);
      expect(html).toContain(optInLine);
      expect(plainText).toContain(optInLine);

      await confirmEmail(page, emailClient, identity);

      const subscribed = await getRecipientListState(identity.email);
      expect(subscribed.subscribed).toBe(true);
      expect(subscribed.confirmationDate).not.toBeNull();
      expect(subscribed.confirmations).toHaveLength(1);
      expect(
        hasActiveAgreement(
          await getConsentAgreements(identity.email, consentNames.emailMarketing),
          consentNames.emailMarketing,
        ),
      ).toBe(true);
      expect((await getMember(identity.username)).newsletterOptInRequestedDate).toBeNull();

      // Opening the link again changes nothing
      await page.goto(extractConfirmationUrl(email));
      await expect(page.locator(confirmationSelectors.page)).toHaveAttribute(
        "data-state",
        "success_alreadyconfirmed",
      );
      const again = await getRecipientListState(identity.email);
      expect(again.confirmations).toEqual(subscribed.confirmations);
      expect(
        await getConsentAgreements(identity.email, consentNames.emailMarketing),
      ).toHaveLength(1);
    } finally {
      await emailClient.close();
    }
  });

  test("newsletter unticked: never subscribed, no agreement, no opt-in line", async ({ page }) => {
    const identity = createRegistrationIdentity("playwright-reg-no-newsletter");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      expect((await getMember(identity.username)).newsletterOptInRequestedDate).toBeNull();

      const email = await waitForConfirmationEmail(emailClient, identity);
      const { html, plainText } = emailText(email);
      expect(html).not.toContain("subscribes you");
      expect(plainText).not.toContain("subscribes you");

      await confirmEmail(page, emailClient, identity);

      const state = await getRecipientListState(identity.email);
      expect(state.subscribed).toBe(false);
      expect(state.isListMember).toBe(false);
      expect(state.confirmations).toEqual([]);
      expect(
        await getConsentAgreements(identity.email, consentNames.emailMarketing),
      ).toEqual([]);
    } finally {
      await emailClient.close();
    }
  });
});

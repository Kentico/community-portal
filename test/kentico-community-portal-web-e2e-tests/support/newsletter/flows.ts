import { expect, type Page } from "@playwright/test";
import type { Client } from "@modelcontextprotocol/sdk/client/index.js";
import * as cheerio from "cheerio";
import type { VirtualEmail } from "../email/virtualEmail.js";
import { waitForEmail } from "../email/waitForEmail.js";

/** `NewsletterSignup.cshtml` (the Newsletter sign-up widget). */
export const newsletterSelectors = {
  widget: "[test-id=newsletterSignup]",
  heading: "[test-id=newsletterSignupHeading]",
  consentText: "[test-id=newsletterSignupConsentText]",
  form: "[test-id=newsletterSignupForm]",
  helpText: "[test-id=newsletterSignupHelpText]",
  trackingNote: "[test-id=newsletterSignupTrackingNote]",
} as const;

/** Pages with the widget (home page and the sign-up landing page). */
export const newsletterPages = ["/", "/newsletter-signup"] as const;

/** The widget's default thank-you message. */
export const newsletterThankYou =
  "Check your inbox for a confirmation link to finish subscribing.";

/** The `CommunityNewsletterOpt-In` autoresponder (double opt-in) email. */
const confirmationSubject = "Kentico Community Newsletter Subscription";
const confirmationLinkText = "Confirm your subscription";

/** Fills the newsletter form on the current page and waits for the thank-you message. */
export async function submitNewsletterForm(page: Page, email: string): Promise<void> {
  const form = page.locator(newsletterSelectors.form);

  await form.locator("input[type=email]").fill(email);
  await form.locator("[type=submit]").click();

  await expect(form).toContainText(newsletterThankYou);
}

export async function waitForNewsletterConfirmationEmail(
  emailClient: Client,
  inbox: string,
): Promise<VirtualEmail> {
  return waitForEmail(emailClient, {
    inbox,
    subjectContains: confirmationSubject,
  });
}

/** The double opt-in link (`ResolveSubscriptionConfirmationLink`) of the confirmation email. */
export function extractSubscriptionConfirmationUrl(email: VirtualEmail): string {
  const $ = cheerio.load(email.virtualEmailBodyHTML ?? "");
  const href = $("a")
    .filter((_, element) => $(element).text().includes(confirmationLinkText))
    .first()
    .attr("href");

  if (!href) {
    throw new Error(
      `No '${confirmationLinkText}' link in the newsletter confirmation email.`,
    );
  }

  return href;
}

/** The newsletter item of the account page's recipient lists (`AccountManagementWidget`). */
export const accountNewsletterItem = "[test-id=recipientList][data-newsletter=true]";

/** Clicks an account page button and waits for its `AccountManagementWidget/<action>` response. */
export async function clickAndWaitForAccountAction(
  page: Page,
  selector: string,
  action: "subscriberecipientlist" | "unsubscriberecipientlist",
): Promise<void> {
  const [response] = await Promise.all([
    page.waitForResponse((response) =>
      response.url().toLowerCase().includes(`/accountmanagementwidget/${action}`),
    ),
    page.locator(selector).click(),
  ]);
  expect(response.status(), `${action} response status`).toBeLessThan(400);
}

/** Plain text of consent HTML, whitespace collapsed, for comparing with rendered text. */
export function consentPlainText(html: string): string {
  return normalizeWhitespace(cheerio.load(html).text());
}

export function normalizeWhitespace(text: string): string {
  return text.replace(/\s+/g, " ").trim();
}

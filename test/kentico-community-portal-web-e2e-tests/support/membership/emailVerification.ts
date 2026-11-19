import { expect, type Page } from "@playwright/test";
import type { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { extractConfirmationUrl } from "../email/extractLinks.js";
import type { VirtualEmail } from "../email/virtualEmail.js";
import { waitForEmail } from "../email/waitForEmail.js";

/** `MemberEmailVerification` email: new account email and alternate email links. */
export const verificationSubject =
  "Verify your email address for your Kentico Community Portal account";
/** `MemberEmailChangedNotification` email, sent to the previous account email. */
export const emailChangedSubject =
  "The email address for your Kentico Community Portal account was changed";

/** `_EmailForm.cshtml`, `_AlternateEmailForm.cshtml`, `_EmailVerificationResult.cshtml` */
export const emailSelectors = {
  changeForm: "form[test-id=emailChangeForm]",
  currentEmail: "[test-id=currentEmail]",
  newEmail: "[test-id=newEmail]",
  newEmailError: "[test-id=newEmailError]",
  currentPassword: "[test-id=emailChangeCurrentPassword]",
  currentPasswordError: "[test-id=emailChangeCurrentPasswordError]",
  requestChange: "[test-id=requestEmailChange]",
  changeRequested: "[test-id=emailChangeRequested]",
  alternate: "[test-id=alternateEmail]",
  alternateCurrent: "[test-id=alternateEmailCurrent]",
  alternateStatus: "[test-id=alternateEmailStatus]",
  alternateInput: "[test-id=alternateEmailInput]",
  alternateError: "[test-id=alternateEmailError]",
  saveAlternate: "[test-id=saveAlternateEmail]",
  resendAlternate: "[test-id=resendAlternateEmailVerification]",
  removeAlternate: "[test-id=removeAlternateEmail]",
  alternateStatusMessage: "[test-id=alternateEmailStatusMessage]",
  verificationResult: "[test-id=emailVerificationResult]",
  confirmVerification: "[test-id=emailVerificationConfirm]",
} as const;

export type VerificationOutcome =
  | "pending"
  | "verified"
  | "alreadyverified"
  | "inuse"
  | "invalid";

export async function requestEmailChange(
  page: Page,
  newEmail: string,
  currentPassword: string,
): Promise<void> {
  const form = page.locator(emailSelectors.changeForm);
  await form.locator(emailSelectors.newEmail).fill(newEmail);
  await form.locator(emailSelectors.currentPassword).fill(currentPassword);

  await Promise.all([
    page.waitForResponse("**/accountmanagementwidget/requestemailchange"),
    form.locator(emailSelectors.requestChange).click(),
  ]);
}

export async function saveAlternateEmail(
  page: Page,
  alternateEmail: string,
): Promise<void> {
  const container = page.locator(emailSelectors.alternate);
  await container.locator(emailSelectors.alternateInput).fill(alternateEmail);

  await Promise.all([
    page.waitForResponse("**/accountmanagementwidget/updatealternateemail"),
    container.locator(emailSelectors.saveAlternate).click(),
  ]);
}

export function waitForVerificationEmail(
  emailClient: Client,
  inbox: string,
  sinceId?: number,
): Promise<VirtualEmail> {
  return waitForEmail(emailClient, {
    inbox,
    subjectContains: verificationSubject,
    timeoutMs: 30_000,
    sinceId,
  });
}

/**
 * Opens the emailed link: the page only asks for confirmation (email scanners pre-open links, so
 * opening changes nothing).
 */
export async function openVerificationLinkOnly(
  page: Page,
  url: string,
): Promise<void> {
  await page.goto(url);
  await expect(page.locator(emailSelectors.verificationResult)).toHaveAttribute(
    "data-outcome",
    "pending",
  );
}

/** Opens the emailed link, clicks the confirm button, and asserts the outcome the page reports. */
export async function openVerificationLink(
  page: Page,
  url: string,
  outcome: VerificationOutcome,
): Promise<void> {
  await openVerificationLinkOnly(page, url);

  await Promise.all([
    page.waitForResponse(
      (r) =>
        r.request().method() === "POST" &&
        new URL(r.url()).pathname.toLowerCase().startsWith("/member-email/"),
    ),
    page.locator(emailSelectors.confirmVerification).click(),
  ]);
  await expect(page.locator(emailSelectors.verificationResult)).toHaveAttribute(
    "data-outcome",
    outcome,
  );
}

/**
 * The emailed link is a click-tracking redirect (`/Kentico.Emails/EmailClickedLink`) whose hash
 * covers its query, so tampering with it 404s. Returns the verification URL it redirects to.
 */
export async function resolveTrackedLink(
  page: Page,
  url: string,
): Promise<string> {
  const response = await page.request.get(url, { maxRedirects: 0 });
  const location = response.headers()["location"];
  expect(location, "tracked link should redirect").toBeTruthy();

  return new URL(location, url).toString();
}

export { extractConfirmationUrl as extractVerificationUrl };

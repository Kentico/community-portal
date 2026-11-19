import { expect, type Page } from "@playwright/test";
import {
  extractConfirmationUrl,
  extractRecoveryUrl,
} from "../email/extractLinks.js";
import { getVirtualEmailId, type VirtualEmail } from "../email/virtualEmail.js";
import { waitForEmail } from "../email/waitForEmail.js";
import type { RegistrationIdentity } from "../data/testIdentity.js";
import type { Client } from "@modelcontextprotocol/sdk/client/index.js";

const confirmationSubject =
  "Confirm your email for your new Kentico Community Portal account";
const confirmationSender =
  '"Kentico Community" <no-reply@community.kentico.com>';
const recoverySubject = "Reset Password Confirmation";

export type RegisterOptions = {
  /** Ticks the required Terms of Use checkbox. Default true. */
  acceptTerms?: boolean;
  /** Ticks the optional newsletter checkbox. Default false. */
  subscribeToNewsletter?: boolean;
};

/** `_RegisterForm.cshtml` */
export const registerSelectors = {
  form: "form[test-id=registerForm]",
  acceptTerms: "[test-id=acceptTerms]",
  acceptTermsError: "[test-id=acceptTermsError]",
  subscribeToNewsletter: "[test-id=subscribeToNewsletter]",
  newsletterConsentText: "[test-id=newsletterConsentText]",
  newsletterHelpText: "[test-id=newsletterHelpText]",
} as const;

/** Opens /register the way a visitor does, from the sign-in link. */
export async function openRegistrationForm(page: Page): Promise<void> {
  await page.goto("/");
  await page.locator("nav [test-id=signInLinkNav]").click();
  await page.waitForURL("**/login?returnURL=/");

  await page.locator("[test-id=registerLink]").click();
  await page.waitForURL("**/register");
}

export async function fillRegistrationForm(
  page: Page,
  identity: RegistrationIdentity,
  options: RegisterOptions = {},
): Promise<void> {
  await page.locator("[test-id=userName]").fill(identity.username);
  await page.locator("[test-id=email]").fill(identity.email);
  await page.locator("[test-id=password]").fill(identity.password);
  await page.locator("[test-id=passwordConfirmation]").fill(identity.password);
  await page
    .locator(registerSelectors.acceptTerms)
    .setChecked(options.acceptTerms ?? true);
  await page
    .locator(registerSelectors.subscribeToNewsletter)
    .setChecked(options.subscribeToNewsletter ?? false);
}

export async function registerMember(
  page: Page,
  identity: RegistrationIdentity,
  options: RegisterOptions = {},
): Promise<void> {
  await openRegistrationForm(page);
  await fillRegistrationForm(page, identity, options);

  await Promise.all([
    page.waitForResponse("**/registrationwidget/register"),
    page.locator(`${registerSelectors.form} button[type=submit]`).click(),
  ]);

  await expect(
    page.locator("[test-id=confirmationEmailMessage]"),
  ).toBeVisible();
}

export async function assertUnconfirmedLoginBlocked(
  page: Page,
  identity: RegistrationIdentity,
): Promise<void> {
  await page.locator("[test-id=registrationSignInLink]").click();
  await page.waitForURL("**/login");

  await page.locator("[test-id=userNameOrEmail]").fill(identity.username);
  await page.locator("[test-id=password]").fill(identity.password);

  await Promise.all([
    page.waitForResponse("**/loginwidget/login"),
    page.locator("form[test-id=signInForm] button[type=submit]").click(),
  ]);

  await expect(
    page.locator("form[test-id=emailNotConfirmedForm]"),
  ).toBeVisible();
}

export function waitForConfirmationEmail(
  emailClient: Client,
  identity: RegistrationIdentity,
): Promise<VirtualEmail> {
  return waitForEmail(emailClient, {
    inbox: identity.email,
    subjectContains: confirmationSubject,
    timeoutMs: 30_000,
  });
}

/** `EmailConfirmation.cshtml`: the registration confirmation page. */
export const confirmationSelectors = {
  /** `data-state`: `pending` until the confirm button is clicked, then the result. */
  page: "[test-id=emailConfirmation]",
  confirmForm: "form[test-id=confirmEmailForm]",
  confirm: "[test-id=confirmEmail]",
  resend: "[test-id=resendConfirmationEmail]",
} as const;

export type ConfirmationState =
  | "pending"
  | "success_confirmed"
  | "success_alreadyconfirmed"
  | "failure_confirmationfailed";

/**
 * Opens the emailed registration link. Opening it changes nothing (email scanners pre-open links):
 * the page asks the member to click the confirm button, which POSTs the token.
 */
export async function openConfirmationLink(
  page: Page,
  url: string,
): Promise<void> {
  await page.goto(url);
  await expect(page.locator(confirmationSelectors.page)).toHaveAttribute(
    "data-state",
    "pending",
  );
}

/** Clicks the confirm button and asserts the resulting state. */
export async function clickConfirmEmail(
  page: Page,
  expected: ConfirmationState = "success_confirmed",
): Promise<void> {
  await Promise.all([
    page.waitForResponse("**/emailconfirmationwidget/confirmemail"),
    page.locator(confirmationSelectors.confirm).click(),
  ]);
  await expect(page.locator(confirmationSelectors.page)).toHaveAttribute(
    "data-state",
    expected,
  );
}

export async function confirmEmail(
  page: Page,
  emailClient: Client,
  identity: RegistrationIdentity,
): Promise<number> {
  const email = await waitForConfirmationEmail(emailClient, identity);

  expect(email.virtualEmailSender).toBe(confirmationSender);

  await openConfirmationLink(page, extractConfirmationUrl(email));
  await clickConfirmEmail(page);

  return getVirtualEmailId(email);
}

/** `_LoginForm.cshtml` and the partials the login widget returns instead of it. */
export const signInSelectors = {
  /** Model error summary. Only has text after a failed attempt. */
  error: "form[test-id=signInForm] .validation-summary",
  moderationMessage: "[test-id=accountModerationMessage]",
  /** `_AccountDisabled.cshtml`: disabled by an administrator after confirming their email. */
  disabledMessage: "[test-id=accountDisabledMessage]",
} as const;

export const signInFailedMessage = "Your sign-in attempt was not successful.";

export async function signIn(
  page: Page,
  usernameOrEmail: string,
  password: string,
): Promise<void> {
  await page.goto("/login");
  await page.locator("[test-id=userNameOrEmail]").fill(usernameOrEmail);
  await page.locator("[test-id=password]").fill(password);

  await Promise.all([
    page.waitForResponse("**/loginwidget/login"),
    page.locator("form[test-id=signInForm] button[type=submit]").click(),
  ]);
}

export async function assertAuthenticated(
  page: Page,
  username: string,
): Promise<void> {
  await expect(page.locator("nav [test-id=signInLinkNav]")).not.toBeVisible();
  await expect(page.locator("nav [test-id=username]")).toHaveText(username);
}

export async function logout(page: Page): Promise<void> {
  await page.locator("[test-id=username]").hover();

  /*
   * Let the page's own requests finish first. The auth cookie is re-issued on every request
   * (ValidationInterval = Zero), so one still in flight (e.g. Vite's style.scss in dev) can
   * restore the session after logout
   */
  await page.waitForLoadState("load");

  // Wait for the logout POST: the page is often already on "/", so waitForURL alone resolves early
  const [response] = await Promise.all([
    page.waitForResponse(
      (r) => r.request().method() === "POST" && new URL(r.url()).pathname.toLowerCase() === "/logout/logout",
    ),
    page.locator("form[test-id=logoutForm] button[type=submit]").click(),
  ]);
  expect(response.status(), "logout POST status").toBeLessThan(400);
  await page.waitForURL("**/");
  await page.waitForLoadState();
}

export async function requestPasswordRecovery(
  page: Page,
  email: string,
): Promise<void> {
  await page.goto("/login");
  await page.locator("[test-id=accountRecoveryLink]").click();
  await page.waitForURL("**/account-recovery");
  await page.locator("[test-id=email]").fill(email);

  await Promise.all([
    page.waitForResponse("**/accountrecoverywidget/requestrecoveryemail"),
    page
      .locator("form[test-id=requestRecoveryEmailForm] button[type=submit]")
      .click(),
  ]);
}

export async function resetPassword(
  page: Page,
  emailClient: Client,
  identity: RegistrationIdentity,
  sinceId?: number,
): Promise<void> {
  const email = await waitForEmail(emailClient, {
    inbox: identity.email,
    subjectContains: recoverySubject,
    timeoutMs: 30_000,
    sinceId,
  });

  await page.goto(extractRecoveryUrl(email));
  await page.locator("[test-id=password]").fill(identity.newPassword);
  await page
    .locator("[test-id=passwordConfirmation]")
    .fill(identity.newPassword);

  await Promise.all([
    page.waitForResponse("**/resetpasswordwidget/resetpassword"),
    page.locator("form[test-id=resetPasswordForm] button[type=submit]").click(),
  ]);

  await expect(
    page.locator("[test-id=resetPasswordConfirmation]"),
  ).toBeVisible();
}

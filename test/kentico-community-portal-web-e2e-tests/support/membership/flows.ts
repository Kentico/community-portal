import { expect, type Page } from "@playwright/test";
import {
  extractConfirmationUrl,
  extractRecoveryUrl,
} from "../email/extractLinks.js";
import { getVirtualEmailId } from "../email/virtualEmail.js";
import { waitForEmail } from "../email/waitForEmail.js";
import type { RegistrationIdentity } from "../data/testIdentity.js";
import type { Client } from "@modelcontextprotocol/sdk/client/index.js";

const confirmationSubject =
  "Confirm your email for your new Kentico Community Portal account";
const confirmationSender =
  '"Kentico Community" <no-reply@community.kentico.com>';
const recoverySubject = "Reset Password Confirmation";

export async function registerMember(
  page: Page,
  identity: RegistrationIdentity,
): Promise<void> {
  await page.goto("/");
  await page.locator("nav [test-id=signInLinkNav]").click();
  await page.waitForURL("**/login?returnURL=/");

  await page.locator("[test-id=registerLink]").click();
  await page.waitForURL("**/register");

  await page.locator("[test-id=userName]").fill(identity.username);
  await page.locator("[test-id=email]").fill(identity.email);
  await page.locator("[test-id=password]").fill(identity.password);
  await page.locator("[test-id=passwordConfirmation]").fill(identity.password);
  await page.locator("[test-id=consentAgreement]").check();

  await Promise.all([
    page.waitForResponse("**/registrationwidget/register"),
    page.locator("form[test-id=registerForm] button[type=submit]").click(),
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

export async function confirmEmail(
  page: Page,
  emailClient: Client,
  identity: RegistrationIdentity,
): Promise<number> {
  const email = await waitForEmail(emailClient, {
    inbox: identity.email,
    subjectContains: confirmationSubject,
    timeoutMs: 30_000,
  });

  expect(email.virtualEmailSender).toBe(confirmationSender);

  await page.goto(extractConfirmationUrl(email));
  await expect(page.locator("[test-id=confirmationMessage]")).toBeVisible();

  return getVirtualEmailId(email);
}

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

  await Promise.all([
    page.waitForURL("**/"),
    page.locator("form[test-id=logoutForm] button[type=submit]").click(),
  ]);
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

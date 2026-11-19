import { expect, test, type Page } from "@playwright/test";
import type { Client } from "@modelcontextprotocol/sdk/client/index.js";
import {
  createRegistrationIdentity,
  type RegistrationIdentity,
} from "../../support/data/testIdentity.js";
import {
  emailChangedSubject,
  emailSelectors,
  extractVerificationUrl,
  openVerificationLink,
  openVerificationLinkOnly,
  requestEmailChange,
  resolveTrackedLink,
  waitForVerificationEmail,
} from "../../support/membership/emailVerification.js";
import {
  assertAuthenticated,
  confirmEmail,
  logout,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { waitForEmail } from "../../support/email/waitForEmail.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { getMember } from "../../support/privacy/state.js";

async function registerAndSignIn(
  page: Page,
  emailClient: Client,
  identity: RegistrationIdentity,
): Promise<void> {
  await registerMember(page, identity);
  await confirmEmail(page, emailClient, identity);
  await signIn(page, identity.username, identity.password);
  await assertAuthenticated(page, identity.username);
}

function newEmailFor(identity: RegistrationIdentity): string {
  return identity.email.replace("@", ".new@");
}

test.describe("member account email change", () => {
  test("changes the email only after the new address is verified", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-email-change");
    const newEmail = newEmailFor(identity);
    const emailClient = await createMcpClient();

    try {
      await registerAndSignIn(page, emailClient, identity);
      await page.goto("/my-account");

      await requestEmailChange(page, newEmail, identity.password);
      await expect(page.locator(emailSelectors.changeRequested)).toBeVisible();

      // Nothing changes until the link is clicked
      expect((await getMember(identity.username)).email).toBe(identity.email);

      const verificationEmail = await waitForVerificationEmail(
        emailClient,
        newEmail,
      );
      const url = extractVerificationUrl(verificationEmail);
      await openVerificationLink(page, url, "verified");

      const member = await getMember(identity.username);
      expect(member.email).toBe(newEmail);
      // The username never changes with the email
      expect(member.userName).toBe(identity.username);
      // Confirmed at registration; the change keeps it
      expect(member.emailConfirmedDate).not.toBeNull();

      // The previous address is told about the change
      await waitForEmail(emailClient, {
        inbox: identity.email,
        subjectContains: emailChangedSubject,
        timeoutMs: 30_000,
      });

      // The session that clicked the link stays signed in
      await page.goto("/my-account");
      await assertAuthenticated(page, identity.username);
      await expect(page.locator(emailSelectors.currentEmail)).toHaveText(
        newEmail,
      );

      // Re-using the link is harmless
      await openVerificationLink(page, url, "alreadyverified");
    } finally {
      await emailClient.close();
    }
  });

  test("verifies the new email from a signed-out browser", async ({ page }) => {
    const identity = createRegistrationIdentity("playwright-email-signedout");
    const newEmail = newEmailFor(identity);
    const emailClient = await createMcpClient();

    try {
      await registerAndSignIn(page, emailClient, identity);
      await page.goto("/my-account");
      await requestEmailChange(page, newEmail, identity.password);
      await expect(page.locator(emailSelectors.changeRequested)).toBeVisible();

      const url = extractVerificationUrl(
        await waitForVerificationEmail(emailClient, newEmail),
      );

      await logout(page);

      // A tampered token changes nothing
      const tampered = new URL(await resolveTrackedLink(page, url));
      tampered.searchParams.set("token", "invalid-token");
      await openVerificationLink(page, tampered.toString(), "invalid");
      expect((await getMember(identity.username)).email).toBe(identity.email);

      await openVerificationLink(page, url, "verified");
      await expect(
        page.locator("[test-id=emailVerificationSignInLink]"),
      ).toBeVisible();

      // The new email signs in
      await signIn(page, newEmail, identity.password);
      await assertAuthenticated(page, identity.username);
    } finally {
      await emailClient.close();
    }
  });

  test("opening the link alone changes nothing", async ({ page }) => {
    const identity = createRegistrationIdentity("pw-email-get");
    const newEmail = newEmailFor(identity);
    const emailClient = await createMcpClient();

    try {
      await registerAndSignIn(page, emailClient, identity);
      await page.goto("/my-account");
      await requestEmailChange(page, newEmail, identity.password);
      await expect(page.locator(emailSelectors.changeRequested)).toBeVisible();

      const url = extractVerificationUrl(
        await waitForVerificationEmail(emailClient, newEmail),
      );

      // What an email scanner does: a plain GET, and a browser that never clicks
      const scanned = await page.request.get(url);
      expect(scanned.ok()).toBe(true);
      await openVerificationLinkOnly(page, url);

      expect((await getMember(identity.username)).email).toBe(identity.email);

      // The link still works afterwards
      await openVerificationLink(page, url, "verified");
      expect((await getMember(identity.username)).email).toBe(newEmail);
    } finally {
      await emailClient.close();
    }
  });

  test("keeps a 'stay signed in' session persistent", async ({ page, context }) => {
    const identity = createRegistrationIdentity("pw-email-persist");
    const newEmail = newEmailFor(identity);
    const emailClient = await createMcpClient();

    const authCookie = async () =>
      (await context.cookies()).find(
        (c) => c.name === ".AspNetCore.Identity.Application",
      );

    try {
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);

      await page.goto("/login");
      await page.locator("[test-id=userNameOrEmail]").fill(identity.username);
      await page.locator("[test-id=password]").fill(identity.password);
      await page.locator("[test-id=staySignedIn]").check();
      await Promise.all([
        page.waitForResponse("**/loginwidget/login"),
        page.locator("form[test-id=signInForm] button[type=submit]").click(),
      ]);
      await assertAuthenticated(page, identity.username);

      const before = await authCookie();
      // A persistent cookie has an expiry; a session cookie is -1
      expect(before?.expires).toBeGreaterThan(0);

      await page.goto("/my-account");
      await requestEmailChange(page, newEmail, identity.password);
      await openVerificationLink(
        page,
        extractVerificationUrl(
          await waitForVerificationEmail(emailClient, newEmail),
        ),
        "verified",
      );

      // Re-issued for the new security stamp, still persistent
      const after = await authCookie();
      expect(after?.value).not.toBe(before?.value);
      expect(after?.expires).toBeGreaterThan(0);

      await page.goto("/my-account");
      await assertAuthenticated(page, identity.username);
    } finally {
      await emailClient.close();
    }
  });

  test("rejects a wrong password and an email used by another member", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-email-reject");
    const other = createRegistrationIdentity("playwright-email-other");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, other);
      await confirmEmail(page, emailClient, other);

      await registerAndSignIn(page, emailClient, identity);
      await page.goto("/my-account");

      await requestEmailChange(page, newEmailFor(identity), "wrong-password-1");
      await expect(
        page.locator(emailSelectors.currentPasswordError),
      ).not.toBeEmpty();
      await expect(page.locator(emailSelectors.changeRequested)).toHaveCount(0);

      await requestEmailChange(page, other.email, identity.password);
      await expect(page.locator(emailSelectors.newEmailError)).not.toBeEmpty();
      await expect(page.locator(emailSelectors.changeRequested)).toHaveCount(0);

      expect((await getMember(identity.username)).email).toBe(identity.email);
    } finally {
      await emailClient.close();
    }
  });
});

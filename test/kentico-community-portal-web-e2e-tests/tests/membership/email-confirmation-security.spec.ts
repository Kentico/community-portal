import { expect, test, type Page } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import { extractConfirmationUrl } from "../../support/email/extractLinks.js";
import { getVirtualEmailId } from "../../support/email/virtualEmail.js";
import { waitForEmail } from "../../support/email/waitForEmail.js";
import {
  assertAuthenticated,
  assertUnconfirmedLoginBlocked,
  clickConfirmEmail,
  confirmationSelectors,
  confirmEmail,
  fillRegistrationForm,
  logout,
  openConfirmationLink,
  openRegistrationForm,
  registerMember,
  registerSelectors,
  signIn,
  signInSelectors,
  waitForConfirmationEmail,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { findMember, getMember } from "../../support/privacy/state.js";
import { setMemberEnabled } from "../../support/testingTools/moderationClient.js";
import {
  emailSelectors,
  extractVerificationUrl,
  openVerificationLink,
  saveAlternateEmail,
  waitForVerificationEmail,
} from "../../support/membership/emailVerification.js";

const confirmationSubject =
  "Confirm your email for your new Kentico Community Portal account";
const resendPath = "/EmailConfirmationWidget/ResendConfirmationEmail";

/** An antiforgery token for the current browser context, from the sign-in form. */
async function getAntiforgeryToken(page: Page): Promise<string> {
  await page.goto("/login");

  return page
    .locator("form[test-id=signInForm] input[name=__RequestVerificationToken]")
    .inputValue();
}

test.describe("registration email confirmation security", () => {
  test("opening the confirmation link alone confirms nothing", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("pw-confirm-get");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      const url = extractConfirmationUrl(
        await waitForConfirmationEmail(emailClient, identity),
      );

      // What an email scanner does: a plain GET, and a browser that never clicks
      const scanned = await page.request.get(url);
      expect(scanned.ok()).toBe(true);
      await openConfirmationLink(page, url);

      let member = await getMember(identity.username);
      expect(member.enabled).toBe(false);
      expect(member.emailConfirmedDate).toBeNull();

      await clickConfirmEmail(page);

      member = await getMember(identity.username);
      expect(member.enabled).toBe(true);
      expect(member.emailConfirmedDate).not.toBeNull();
    } finally {
      await emailClient.close();
    }
  });

  test("an unconfirmed member can resend the confirmation email", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("pw-confirm-resend");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      const first = await waitForConfirmationEmail(emailClient, identity);

      await assertUnconfirmedLoginBlocked(page, identity);
      await Promise.all([
        page.waitForResponse((r) =>
          new URL(r.url()).pathname.toLowerCase() ===
          resendPath.toLowerCase(),
        ),
        page.locator(confirmationSelectors.resend).click(),
      ]);
      await expect(
        page.locator("[test-id=confirmationEmailMessage]"),
      ).toBeVisible();

      const resent = await waitForEmail(emailClient, {
        inbox: identity.email,
        subjectContains: confirmationSubject,
        timeoutMs: 30_000,
        sinceId: getVirtualEmailId(first),
      });

      await openConfirmationLink(page, extractConfirmationUrl(resent));
      await clickConfirmEmail(page);
    } finally {
      await emailClient.close();
    }
  });

  test("a disabled member who confirmed before can't re-enable themselves", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("pw-confirm-disabled");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      const confirmationId = await confirmEmail(page, emailClient, identity);

      // An administrator disables the member: the confirmed date stays
      await setMemberEnabled(identity.username, false);
      const disabled = await getMember(identity.username);
      expect(disabled.enabled).toBe(false);
      expect(disabled.emailConfirmedDate).not.toBeNull();

      // Wrong password: the generic failure, nothing about the account
      await signIn(page, identity.username, "wrong-password-1");
      await expect(page.locator(signInSelectors.error)).not.toBeEmpty();
      await expect(page.locator(signInSelectors.disabledMessage)).toHaveCount(0);

      // Right password: can't sign in, and no verify-email prompt
      await signIn(page, identity.username, identity.password);
      await expect(page.locator(signInSelectors.disabledMessage)).toBeVisible();
      await expect(
        page.locator("form[test-id=emailNotConfirmedForm]"),
      ).toHaveCount(0);

      // Resend anyway: the same response as for anyone, but nothing is sent
      const token = await getAntiforgeryToken(page);
      const response = await page.request.post(
        `${resendPath}?username=${encodeURIComponent(identity.username)}`,
        { form: { __RequestVerificationToken: token } },
      );
      expect(response.status()).toBe(200);
      expect(await response.text()).toContain(
        'test-id="confirmationEmailMessage"',
      );

      await expect(
        waitForEmail(emailClient, {
          inbox: identity.email,
          subjectContains: confirmationSubject,
          timeoutMs: 5_000,
          sinceId: confirmationId,
        }),
      ).rejects.toThrow();

      expect((await getMember(identity.username)).enabled).toBe(false);
    } finally {
      await setMemberEnabled(identity.username, true);
      await emailClient.close();
    }
  });

  test("resend without an antiforgery token is rejected", async ({ page }) => {
    const identity = createRegistrationIdentity("pw-confirm-csrf");

    await registerMember(page, identity);

    const url = `${resendPath}?username=${encodeURIComponent(identity.username)}`;

    // The app re-executes the 400 as an error page, so assert on "rejected", not the exact code
    const rejected = await page.request.post(url);
    expect(rejected.ok()).toBe(false);
    expect(await rejected.text()).not.toContain(
      'test-id="confirmationEmailMessage"',
    );

    // Same request with a token is accepted
    const token = await getAntiforgeryToken(page);
    const accepted = await page.request.post(url, {
      form: { __RequestVerificationToken: token },
    });
    expect(accepted.status()).toBe(200);
  });

  test("registration rejects another member's verified alternate email", async ({
    page,
  }) => {
    const owner = createRegistrationIdentity("pw-alt-owner");
    const alternateEmail = owner.email.replace("@", ".work@");
    const newcomer = {
      ...createRegistrationIdentity("pw-alt-newcomer"),
      email: alternateEmail,
    };
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, owner);
      await confirmEmail(page, emailClient, owner);
      await signIn(page, owner.username, owner.password);
      await assertAuthenticated(page, owner.username);
      await page.goto("/my-account");
      await saveAlternateEmail(page, alternateEmail);
      await openVerificationLink(
        page,
        extractVerificationUrl(
          await waitForVerificationEmail(emailClient, alternateEmail),
        ),
        "verified",
      );
      await page.goto("/my-account");
      await expect(page.locator(emailSelectors.alternate)).toHaveAttribute(
        "data-status",
        "verified",
      );
      await logout(page);

      await openRegistrationForm(page);
      await fillRegistrationForm(page, newcomer);
      await Promise.all([
        page.waitForResponse("**/registrationwidget/register"),
        page.locator(`${registerSelectors.form} button[type=submit]`).click(),
      ]);

      await expect(
        page.locator(`${registerSelectors.form} .validation-summary`),
      ).not.toBeEmpty();
      await expect(
        page.locator("[test-id=confirmationEmailMessage]"),
      ).toHaveCount(0);
      expect(await findMember(newcomer.username)).toBeNull();
    } finally {
      await emailClient.close();
    }
  });
});

test.describe("sign-in return URL", () => {
  test("an external return URL redirects to the site instead", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("pw-return-url");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);

      await page.goto("/login");
      // The hidden field is posted by the browser, so it can be anything
      await page
        .locator("form[test-id=signInForm] input[name=ReturnURL]")
        .evaluate((input: HTMLInputElement) => {
          input.value = "https://example.com/phish";
        });
      await page.locator("[test-id=userNameOrEmail]").fill(identity.username);
      await page.locator("[test-id=password]").fill(identity.password);

      const [response] = await Promise.all([
        page.waitForResponse("**/loginwidget/login"),
        page.locator("form[test-id=signInForm] button[type=submit]").click(),
      ]);
      expect(response.headers()["hx-redirect"]).toBe("/");

      await page.waitForURL((url) => url.pathname === "/");
      expect(new URL(page.url()).origin).toBe(new URL(response.url()).origin);
      await assertAuthenticated(page, identity.username);
    } finally {
      await emailClient.close();
    }
  });
});

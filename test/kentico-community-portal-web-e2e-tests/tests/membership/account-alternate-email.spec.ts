import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import { getVirtualEmailId } from "../../support/email/virtualEmail.js";
import {
  emailSelectors,
  extractVerificationUrl,
  openVerificationLink,
  saveAlternateEmail,
  waitForVerificationEmail,
} from "../../support/membership/emailVerification.js";
import {
  assertAuthenticated,
  confirmEmail,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { getMember } from "../../support/privacy/state.js";

test.describe("member alternate email", () => {
  test("sets, re-sends, verifies, changes, and removes an alternate email", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-alt-email");
    const alternateEmail = identity.email.replace("@", ".work@");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);
      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);
      await page.goto("/my-account");

      const container = page.locator(emailSelectors.alternate);
      await expect(container).toHaveAttribute("data-status", "none");

      // The account email can't be the alternate email
      await saveAlternateEmail(page, identity.email);
      await expect(
        container.locator(emailSelectors.alternateError),
      ).not.toBeEmpty();
      await expect(container).toHaveAttribute("data-status", "none");

      await saveAlternateEmail(page, alternateEmail);
      await expect(container).toHaveAttribute("data-status", "unverified");
      await expect(
        container.locator(emailSelectors.alternateCurrent),
      ).toHaveText(alternateEmail);

      // Stored, but not trusted
      let member = await getMember(identity.username);
      expect(member.alternateEmail).toBe(alternateEmail);
      expect(member.alternateEmailVerifiedDate).toBeNull();

      const firstEmail = await waitForVerificationEmail(
        emailClient,
        alternateEmail,
      );

      await Promise.all([
        page.waitForResponse(
          "**/accountmanagementwidget/resendalternateemailverification",
        ),
        container.locator(emailSelectors.resendAlternate).click(),
      ]);
      await expect(
        container.locator(emailSelectors.alternateStatusMessage),
      ).toBeVisible();

      const resentEmail = await waitForVerificationEmail(
        emailClient,
        alternateEmail,
        getVirtualEmailId(firstEmail),
      );

      await openVerificationLink(
        page,
        extractVerificationUrl(resentEmail),
        "verified",
      );

      member = await getMember(identity.username);
      expect(member.alternateEmailVerifiedDate).not.toBeNull();

      await page.goto("/my-account");
      await expect(container).toHaveAttribute("data-status", "verified");
      await expect(container.locator(emailSelectors.resendAlternate)).toHaveCount(
        0,
      );

      // Changing a verified alternate email: the new one is untrusted until its own link is clicked
      const changedEmail = identity.email.replace("@", ".agency@");
      await saveAlternateEmail(page, changedEmail);
      await expect(container).toHaveAttribute("data-status", "unverified");
      await expect(
        container.locator(emailSelectors.alternateCurrent),
      ).toHaveText(changedEmail);

      member = await getMember(identity.username);
      expect(member.alternateEmail).toBe(changedEmail);
      expect(member.alternateEmailVerifiedDate).toBeNull();

      await openVerificationLink(
        page,
        extractVerificationUrl(
          await waitForVerificationEmail(emailClient, changedEmail),
        ),
        "verified",
      );
      expect(
        (await getMember(identity.username)).alternateEmailVerifiedDate,
      ).not.toBeNull();

      // The link for the previous alternate email no longer verifies anything
      await openVerificationLink(
        page,
        extractVerificationUrl(resentEmail),
        "invalid",
      );

      await page.goto("/my-account");
      await expect(container).toHaveAttribute("data-status", "verified");

      await Promise.all([
        page.waitForResponse("**/accountmanagementwidget/removealternateemail"),
        container.locator(emailSelectors.removeAlternate).click(),
      ]);
      await expect(container).toHaveAttribute("data-status", "none");

      member = await getMember(identity.username);
      expect(member.alternateEmail).toBe("");
      expect(member.alternateEmailVerifiedDate).toBeNull();

      // A link for a removed alternate email no longer verifies anything
      await openVerificationLink(
        page,
        extractVerificationUrl(firstEmail),
        "invalid",
      );
    } finally {
      await emailClient.close();
    }
  });
});

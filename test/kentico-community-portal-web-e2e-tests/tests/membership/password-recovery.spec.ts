import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  assertUnconfirmedLoginBlocked,
  confirmEmail,
  registerMember,
  requestPasswordRecovery,
  resetPassword,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";

test.describe("membership password recovery", () => {
  test("resets the password and signs in with the new password", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-recovery");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await assertUnconfirmedLoginBlocked(page, identity);
      const confirmationEmailId = await confirmEmail(
        page,
        emailClient,
        identity,
      );

      await requestPasswordRecovery(page, identity.email);
      await resetPassword(page, emailClient, identity, confirmationEmailId);

      await page
        .locator("[test-id=signInLinkResetPasswordConfirmation]")
        .click();
      await page.waitForURL("**/login");
      await signIn(page, identity.username, identity.newPassword);
      await assertAuthenticated(page, identity.username);
    } finally {
      await emailClient.close();
    }
  });
});

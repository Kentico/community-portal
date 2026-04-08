import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  assertUnconfirmedLoginBlocked,
  confirmEmail,
  logout,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { setMemberModerationStatus } from "../../support/testingTools/moderationClient.js";

test.describe("membership moderation", () => {
  test("blocks confirmed spam accounts from signing in", async ({ page }) => {
    const identity = createRegistrationIdentity("playwright-moderation");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await assertUnconfirmedLoginBlocked(page, identity);
      await confirmEmail(page, emailClient, identity);

      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);
      await logout(page);

      await expect(page.locator("nav [test-id=signInLinkNav]")).toBeVisible();
      await expect(page.locator("nav [test-id=username]")).not.toBeVisible();

      await setMemberModerationStatus(identity.username, "Spam");
      await signIn(page, identity.username, identity.password);
      await expect(
        page.locator("[test-id=accountModerationMessage]"),
      ).toBeVisible();

      await setMemberModerationStatus(identity.username, "None");
    } finally {
      await emailClient.close();
    }
  });
});

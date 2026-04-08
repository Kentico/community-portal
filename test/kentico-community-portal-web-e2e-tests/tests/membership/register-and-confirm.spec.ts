import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  assertUnconfirmedLoginBlocked,
  confirmEmail,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";

test.describe("membership registration", () => {
  test("registers, confirms email, and signs in", async ({ page }) => {
    const identity = createRegistrationIdentity();
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await assertUnconfirmedLoginBlocked(page, identity);
      await confirmEmail(page, emailClient, identity);
      await page.locator("[test-id=confirmationSignInLink]").click();
      await page.waitForURL("**/login");
      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);
    } finally {
      await emailClient.close();
    }
  });
});

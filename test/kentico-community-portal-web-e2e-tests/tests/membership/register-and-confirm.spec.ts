import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  assertUnconfirmedLoginBlocked,
  confirmEmail,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { termsVersion } from "../../support/membership/terms.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { getMember } from "../../support/privacy/state.js";

test.describe("membership registration", () => {
  test("registers, confirms email, and signs in", async ({ page }) => {
    const identity = createRegistrationIdentity();
    const emailClient = await createMcpClient();

    try {
      const before = Date.now();
      await registerMember(page, identity);

      // The accepted Terms version and date are stored on the member
      const member = await getMember(identity.username);
      expect(member.termsAcceptedVersion).toBe(termsVersion);
      expect(member.termsAcceptedDate).not.toBeNull();
      // Server local time without an offset; allow for any time zone
      expect(
        Math.abs(Date.parse(member.termsAcceptedDate!) - before),
      ).toBeLessThan(15 * 60 * 60 * 1000);
      expect(member.newsletterOptInRequestedDate).toBeNull();

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

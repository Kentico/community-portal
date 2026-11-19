import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  confirmEmail,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";

test.describe("my account badges", () => {
  test("cannot submit badge updates when the member has no badges", async ({
    page,
  }) => {
    const identity = createRegistrationIdentity("playwright-badges");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);
      await signIn(page, identity.username, identity.password);
      await assertAuthenticated(page, identity.username);

      await page.goto("/my-account");

      const badgesForm = page.locator("form[test-id=badgesForm]");
      await expect(badgesForm).toBeVisible();

      // A member who has just registered has earned no badges yet
      await expect(
        badgesForm.locator("[test-id=noBadgesMessage]"),
      ).toBeVisible();
      await expect(
        badgesForm.locator("[test-id=selectedBadgeCount]"),
      ).toHaveCount(0);
      await expect(badgesForm.locator("[test-id=badgeSelection]")).toBeHidden();

      // Submitting used to reach the server and fail with a 500 and an error toast
      await expect(
        badgesForm.locator("[test-id=updateBadges]"),
      ).toBeDisabled();
    } finally {
      await emailClient.close();
    }
  });
});

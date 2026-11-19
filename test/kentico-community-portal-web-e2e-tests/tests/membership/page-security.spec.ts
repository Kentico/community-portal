import { expect, test } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  assertAuthenticated,
  confirmEmail,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { setMemberRole } from "../../support/testingTools/memberRoleClient.js";

/**
 * Covers ticket #347: native Member Role page security ("Requires authentication" /
 * "Membership roles") configured on the two LandingPage instances that still carry the legacy
 * ContentAuthorizationFields tags. These specs prove the native router enforces the same
 * restriction as the old ContentAuthorizationFilter, ahead of the filter's removal.
 */
test.describe("native page security", () => {
  test.describe("/my-account (requires authentication, any member)", () => {
    test("blocks an anonymous visitor", async ({ page }) => {
      await page.goto("/my-account");

      await expect(page).toHaveURL(/\/login(\?|$)/);
    });

    test("allows an authenticated member", async ({ page }) => {
      const identity = createRegistrationIdentity("playwright-security");
      const emailClient = await createMcpClient();

      try {
        await registerMember(page, identity);
        await confirmEmail(page, emailClient, identity);
        await signIn(page, identity.username, identity.password);
        await assertAuthenticated(page, identity.username);

        await page.goto("/my-account");

        await expect(page).toHaveURL(/\/my-account/);
        await expect(page.locator("form[test-id=badgesForm]")).toBeVisible();
      } finally {
        await emailClient.close();
      }
    });
  });

  test.describe("/community-programs-dashboard-2026 (requires KenticoMVP, KenticoCommunityLeader, or KenticoEmployee)", () => {
    test("blocks an anonymous visitor", async ({ page }) => {
      await page.goto("/community-programs-dashboard-2026");

      await expect(page).toHaveURL(/\/login(\?|$)/);
    });

    test("blocks an authenticated member without one of the roles", async ({
      page,
    }) => {
      const identity = createRegistrationIdentity("playwright-security");
      const emailClient = await createMcpClient();

      try {
        await registerMember(page, identity);
        await confirmEmail(page, emailClient, identity);
        await signIn(page, identity.username, identity.password);
        await assertAuthenticated(page, identity.username);

        // A role mismatch for an already-authenticated member returns 403 on the requested URL
        // and renders the Forbidden error page, instead of redirecting to /login (#356).
        const response = await page.goto("/community-programs-dashboard-2026");

        expect(response?.status()).toBe(403);
        await expect(page).toHaveURL(/\/community-programs-dashboard-2026/);
        await expect(page.locator("section.section--error-hero h1")).toContainText("403");
        await expect(page.getByRole("heading", { name: "Access restricted." })).toBeVisible();
      } finally {
        await emailClient.close();
      }
    });

    test("allows an authenticated member with the KenticoMVP role", async ({
      page,
    }) => {
      const identity = createRegistrationIdentity("playwright-security");
      const emailClient = await createMcpClient();

      try {
        await registerMember(page, identity);
        await confirmEmail(page, emailClient, identity);
        await signIn(page, identity.username, identity.password);
        await assertAuthenticated(page, identity.username);

        await setMemberRole(identity.username, "KenticoMVP", true);

        await page.goto("/community-programs-dashboard-2026");

        await expect(page).toHaveURL(/\/community-programs-dashboard-2026/);
      } finally {
        await setMemberRole(identity.username, "KenticoMVP", false);
        await emailClient.close();
      }
    });
  });
});

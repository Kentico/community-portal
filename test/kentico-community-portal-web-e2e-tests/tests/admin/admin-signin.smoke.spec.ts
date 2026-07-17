import { expect, test } from "@playwright/test";
import { AdminShellPage } from "../../support/admin/pageObjects/adminShellPage.js";

test.describe("admin sign-in smoke", () => {
  test("loads administration shell for authenticated admin", async ({
    page,
  }) => {
    const adminShell = new AdminShellPage(page);

    await adminShell.goto();
    await adminShell.expectSignedIn();

    // Assert a built-in Xperience application (not the portal-specific
    // Community Members app already checked by expectSignedIn) to keep this
    // smoke test scoped to core admin infrastructure.
    await expect(
      page.locator('a[aria-label="System"]'),
    ).toBeVisible();
  });
});

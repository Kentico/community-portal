import { test } from "@playwright/test";
import { AdminShellPage } from "../../support/admin/pageObjects/adminShellPage.js";

test.describe("community members administration smoke", () => {
  test("opens the Community Members area", async ({ page }) => {
    const adminShell = new AdminShellPage(page);

    await adminShell.goto();
    await adminShell.expectSignedIn();

    const membersPage = await adminShell.openCommunityMembers();
    await membersPage.expectLoaded();
  });
});

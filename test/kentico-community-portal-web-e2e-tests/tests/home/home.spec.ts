import { expect, test } from "@playwright/test";

test.describe("home page", () => {
  test("renders without the error hero", async ({ page }) => {
    await page.goto("/");

    // Structural hooks, not editor content: the template hero and Page Builder sections
    await expect(page.locator("[test-id=homeHero]")).toBeVisible();
    await expect(page.locator("[test-id=gridSection]").first()).toBeVisible();
    await expect(page.locator(".section--error-hero")).not.toBeVisible();
  });
});

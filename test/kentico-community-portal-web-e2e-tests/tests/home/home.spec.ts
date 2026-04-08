import { expect, test } from "@playwright/test";

test.describe("home page", () => {
  test("renders without the error hero", async ({ page }) => {
    await page.goto("/");

    await expect(page.locator("#gridsection-updates")).toBeVisible();
    await expect(page.locator(".section--error-hero")).not.toBeVisible();
  });
});

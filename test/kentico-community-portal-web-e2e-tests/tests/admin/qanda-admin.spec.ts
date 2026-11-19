import { expect, test } from "@playwright/test";
import { QAndAAdminPage } from "../../support/admin/pageObjects/qAndAAdminPage.js";

test.describe("Q&A administration", () => {
  test("defaults to discussion events, with answers last", async ({ page }) => {
    const qanda = new QAndAAdminPage(page);
    await qanda.open();

    await expect(page).toHaveURL(/\/q-and-a\/discussion-events/i);
    await expect(page).not.toHaveURL(/\/q-and-a\/answers/i);

    await expect
      .poll(async () => (await qanda.navSlugs()).at(-1), { timeout: 15_000 })
      .toBe("answers");
    expect((await qanda.navSlugs())[0]).toBe("discussion-events");
  });

  test("member subscriptions listing has a date column", async ({ page }) => {
    const qanda = new QAndAAdminPage(page);
    await qanda.openMemberSubscriptions();

    await expect(qanda.subscriptionDateColumnHeader()).toBeVisible({ timeout: 15_000 });
  });
});

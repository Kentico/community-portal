import { expect, test } from "@playwright/test";
import { EmailQueuePage } from "../../support/admin/pageObjects/emailQueuePage.js";

/*
 * Email queue listing customizations (EmailQueueListExtender).
 * Asserts on the listing configuration (column and command names), not on captions or rows.
 * The destructive "Delete failed" command is never run here.
 */

test.describe("email queue administration", () => {
  test("sorts by created date, newest first, by default", async ({ page }) => {
    const emailQueue = new EmailQueuePage(page);
    await emailQueue.open();

    expect(emailQueue.defaultSort()).toEqual({
      sortBy: "EmailCreated",
      sortType: "desc",
    });
  });

  test("asks for confirmation before deleting failed emails", async ({
    page,
  }) => {
    const emailQueue = new EmailQueuePage(page);
    await emailQueue.open();

    await emailQueue.deleteFailedButton().click();

    // Never confirmed: only the dialog is asserted
    await expect(emailQueue.confirmationDialog()).toBeVisible();
  });
});

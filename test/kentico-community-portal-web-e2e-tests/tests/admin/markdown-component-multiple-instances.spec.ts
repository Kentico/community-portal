import { expect, test, type Locator, type Page } from "@playwright/test";
import { AdminShellPage } from "../../support/admin/pageObjects/adminShellPage.js";

// The Newsletter sign-up widget has two Markdown form component properties
// ("Help text" and "Email tracking note"). Each instance must keep its own
// editor and value, including after toggling between the rich editor and the
// raw markdown textarea.
const HELP_TEXT_SNIPPET = "confirmation link";
const TRACKING_NOTE_SNIPPET = "We measure opens and clicks";
const TYPED_TEXT = "Typed by the Playwright E2E suite";

function markdownField(page: Page, label: string): Locator {
  return page
    .locator(".markdown-editor-container")
    .filter({ has: page.getByText(label, { exact: true }) });
}

async function expectEditorShows(field: Locator, text: string): Promise<void> {
  await expect(field.locator(".ProseMirror")).toContainText(text, {
    timeout: 10_000,
  });
}

async function expectMarkdownShows(
  field: Locator,
  text: string,
): Promise<void> {
  await expect(field.locator("textarea.raw-editor")).toHaveValue(
    new RegExp(text),
  );
}

test.describe("page builder markdown form component", () => {
  test("keeps multiple Markdown fields on one form independent", async ({
    page,
  }) => {
    test.setTimeout(120_000);

    const adminShell = new AdminShellPage(page);
    await adminShell.goto();
    await adminShell.expectSignedIn();

    const websiteChannel = await adminShell.openWebsiteChannel();
    await websiteChannel.expandPage("Newsletter");
    await websiteChannel.openPage("Newsletter Signup");
    const pageBuilder = await websiteChannel.openPageBuilder();

    await pageBuilder.ensurePublished();
    await pageBuilder.startEditing();

    try {
      await pageBuilder.openWidgetConfiguration();

      const helpText = markdownField(page, "Help text");
      const trackingNote = markdownField(page, "Email tracking note");

      await expect(helpText).toBeVisible({ timeout: 15_000 });
      await expect(trackingNote).toBeVisible();

      // Both rich editors render their own value.
      await expectEditorShows(helpText, HELP_TEXT_SNIPPET);
      await expectEditorShows(trackingNote, TRACKING_NOTE_SNIPPET);

      // Toggling one field must not affect the other.
      await trackingNote.getByRole("button", { name: "View markdown" }).click();
      await expectMarkdownShows(trackingNote, TRACKING_NOTE_SNIPPET);
      await expectEditorShows(helpText, HELP_TEXT_SNIPPET);

      await helpText.getByRole("button", { name: "View markdown" }).click();
      await expectMarkdownShows(helpText, HELP_TEXT_SNIPPET);
      await expectMarkdownShows(trackingNote, TRACKING_NOTE_SNIPPET);

      await trackingNote.getByRole("button", { name: "View editor" }).click();
      await expectEditorShows(trackingNote, TRACKING_NOTE_SNIPPET);
      await expectMarkdownShows(helpText, HELP_TEXT_SNIPPET);

      await helpText.getByRole("button", { name: "View editor" }).click();
      await expectEditorShows(helpText, HELP_TEXT_SNIPPET);
      await expectEditorShows(trackingNote, TRACKING_NOTE_SNIPPET);

      // Editing one field updates only that field's value.
      await trackingNote.locator(".ProseMirror").click();
      await page.keyboard.press("Control+End");
      await page.keyboard.type(` ${TYPED_TEXT}`);
      // Edits reach the form through a 250ms debounce.
      await page.waitForTimeout(500);

      await trackingNote.getByRole("button", { name: "View markdown" }).click();
      await helpText.getByRole("button", { name: "View markdown" }).click();
      await expectMarkdownShows(trackingNote, TYPED_TEXT);
      await expectMarkdownShows(helpText, HELP_TEXT_SNIPPET);
      await expect(helpText.locator("textarea.raw-editor")).not.toHaveValue(
        new RegExp(TYPED_TEXT),
      );
    } finally {
      // Discard the draft (and the unapplied widget property edits) so the
      // page returns to its published state.
      // The open properties side panel blocks the page action bar. Closing it
      // with unapplied edits asks for confirmation in an in-app dialog
      // (Xperience 31.10+), which has its own Cancel button.
      page.on("dialog", (dialog) => void dialog.accept());
      const cancel = page
        .getByTestId("side-panel-footer")
        .getByRole("button", { name: "Cancel", exact: true });
      const discard = page
        .getByRole("dialog", { name: "You have unsaved changes" })
        .getByRole("button", { name: "Discard", exact: true });
      if (await cancel.isVisible()) {
        await expect(async () => {
          if (await discard.isVisible()) {
            await discard.click({ timeout: 2_000 });
          } else {
            await cancel.click({ timeout: 2_000 });
            const confirm = await discard
              .waitFor({ state: "visible", timeout: 1_000 })
              .then(() => true)
              .catch(() => false);
            if (confirm) {
              await discard.click({ timeout: 2_000 });
            }
          }
          await expect(cancel).toBeHidden({ timeout: 2_000 });
        }).toPass({ timeout: 15_000 });
      }
      await pageBuilder.revertToPublished();
    }
  });
});

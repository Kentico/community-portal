import { expect, test } from "@playwright/test";
import { AdminShellPage } from "../../support/admin/pageObjects/adminShellPage.js";

const MARKDOWN_WIDGET_IDENTIFIER = "CommunityPortal.MarkdownWidget";
const WIDGET_CONTENT = "Hello from the Playwright E2E suite";

test.describe("page builder markdown widget", () => {
  test("adds and configures a Markdown widget on the Documentation page", async ({
    page,
  }) => {
    // The flow spans several page builder iframe loads plus draft cleanup,
    // which does not fit in the default 30s test timeout.
    test.setTimeout(120_000);

    const consoleErrors: string[] = [];
    page.on("console", (message) => {
      if (message.type() === "error") {
        consoleErrors.push(message.text());
      }
    });

    const adminShell = new AdminShellPage(page);
    await adminShell.goto();
    await adminShell.expectSignedIn();

    const websiteChannel = await adminShell.openWebsiteChannel();
    await websiteChannel.openPage("Documentation");
    const pageBuilder = await websiteChannel.openPageBuilder();

    await pageBuilder.ensurePublished();
    await pageBuilder.startEditing();

    try {
      await pageBuilder.addWidget(MARKDOWN_WIDGET_IDENTIFIER);

      // Only console errors from here on are attributable to the Markdown
      // editor form component rendering in the side panel.
      consoleErrors.length = 0;

      await pageBuilder.openWidgetConfiguration();

      await expect(page.locator(".markdown-editor-form-item")).toBeVisible({
        timeout: 15_000,
      });
      await expect(page.locator('[data-milkdown-root="true"]')).toBeVisible();

      const editor = page.locator('.milkdown [contenteditable="true"]');
      await editor.click();
      await page.keyboard.type(WIDGET_CONTENT);
      await expect(editor).toContainText(WIDGET_CONTENT);

      expect(
        consoleErrors,
        "the Markdown editor should render without console errors",
      ).toEqual([]);

      // The editor propagates edits to the widget properties through a 250ms
      // debounce; give it time to flush before applying.
      await page.waitForTimeout(500);
      await page.getByRole("button", { name: "Apply" }).click();

      await expect(pageBuilder.widgetBody()).toContainText(WIDGET_CONTENT, {
        timeout: 15_000,
      });
    } finally {
      // Discard the draft so the page returns to its published state and
      // repeated runs always start from the "Edit page" button.
      await pageBuilder.revertToPublished();
    }
  });
});

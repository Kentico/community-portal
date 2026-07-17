import {
  expect,
  type FrameLocator,
  type Locator,
  type Page,
} from "@playwright/test";

// The Page Builder canvas is a same-origin iframe titled "Page builder".
// Builder chrome inside the iframe (add-widget button, widget headers, the
// widget selection dialog) is built from Kentico web components with open
// shadow roots, which Playwright CSS selectors pierce automatically. The
// widget properties side panel renders in the admin shell (top document),
// not inside the iframe.
export class PageBuilderPage {
  readonly builderFrame: FrameLocator;

  constructor(private readonly page: Page) {
    this.builderFrame = page.frameLocator('iframe[title="Page builder"]');
  }

  async expectLoaded(): Promise<void> {
    await expect(this.page).toHaveURL(/page-builder/i);
  }

  // Leftover drafts from earlier runs hide the "Edit page" button, so revert
  // to the published version before editing to keep this flow repeatable.
  async ensurePublished(): Promise<void> {
    const editButton = this.page.getByRole("button", { name: "Edit page" });

    try {
      await expect(editButton).toBeVisible({ timeout: 5_000 });
    } catch {
      await this.revertToPublished();
    }
  }

  async startEditing(): Promise<void> {
    await this.page.getByRole("button", { name: "Edit page" }).click();

    await expect(this.page.getByText("Draft (New version)")).toBeVisible({
      timeout: 20_000,
    });
  }

  async addWidget(componentIdentifier: string): Promise<void> {
    const addWidgetButton = this.builderFrame
      .locator('kentico-add-component-button a:has(i[title="Add widget"])')
      .first();

    await addWidgetButton.click({ timeout: 30_000 });

    await this.builderFrame
      .locator(`li[data-component-identifier="${componentIdentifier}"]`)
      .click();

    await expect(this.builderFrame.locator(".ktc-widget").first()).toBeVisible(
      { timeout: 15_000 },
    );
  }

  async openWidgetConfiguration(): Promise<void> {
    await this.builderFrame.locator(".ktc-widget").first().hover();
    await this.builderFrame.locator('i[title="Configure widget"]').click();
  }

  widgetBody(): Locator {
    return this.builderFrame.locator(".ktc-widget-body-wrapper").first();
  }

  async revertToPublished(): Promise<void> {
    // The unlabeled overflow menu button is the last button in the top action
    // bar, after "Edit page" or "Save"/"Publish".
    await this.page.getByRole("banner").getByRole("button").last().click();
    await this.page
      .getByRole("button", { name: "Revert to published" })
      .click();
    await this.page
      .getByRole("dialog")
      .getByRole("button", { name: "Revert to published" })
      .click();

    await expect(
      this.page.getByRole("button", { name: "Edit page" }),
    ).toBeVisible({ timeout: 20_000 });
  }
}

import { expect, type Page } from "@playwright/test";
import { PageBuilderPage } from "./pageBuilderPage.js";

export class WebsiteChannelPage {
  constructor(private readonly page: Page) {}

  async expectLoaded(): Promise<void> {
    await expect(this.page).toHaveURL(/webpages-1/i);
    await expect(this.page.getByRole("tree")).toBeVisible({
      timeout: 10_000,
    });
  }

  // Shows the child pages of a collapsed tree item so openPage can find them.
  // Hovering a row changes its accessible name, so check the parent treeitem's
  // state before interacting with the row.
  async expandPage(pageName: string): Promise<void> {
    const pageLink = this.page
      .getByRole("tree")
      .getByRole("link", { name: pageName, exact: true });

    await expect(pageLink).toBeVisible({ timeout: 10_000 });
    const expanded = await pageLink
      .locator("xpath=..")
      .getAttribute("aria-expanded");
    if (expanded !== "true") {
      await pageLink.getByTestId("tree-item-expand").click();
    }
  }

  async openPage(pageName: string): Promise<void> {
    const pageLink = this.page
      .getByRole("tree")
      .getByRole("link", { name: pageName, exact: true });

    await expect(pageLink).toBeVisible({ timeout: 10_000 });
    await pageLink.click();

    await expect(
      this.page.getByRole("link", { name: "Page Builder" }),
    ).toBeVisible({ timeout: 10_000 });
  }

  // Creates a Landing Page (Default template) under the website root and
  // leaves it open as an initial draft. Returns the live site path.
  async createLandingPage(pageName: string): Promise<string> {
    await this.page
      .getByRole("tree")
      .getByRole("link", { name: "Website", exact: true })
      .click();
    await this.page.getByTestId("create-page-button").click();

    await this.page.getByLabel("Page name").fill(pageName);
    await this.page.getByText("Landing Page", { exact: true }).click();
    await this.page.getByRole("button", { name: "Continue" }).click();

    await this.page
      .getByTestId("template-tile-KenticoCommunity.LandingPage_Default")
      .click();
    await this.page.getByRole("button", { name: "Continue" }).click();

    await this.page.getByLabel("Title").first().fill(pageName);
    const slug = await this.page.getByLabel("URL slug").inputValue();
    await this.page.getByRole("button", { name: "Save", exact: true }).click();

    await expect(
      this.page.getByRole("link", { name: "Page Builder" }),
    ).toBeVisible({ timeout: 20_000 });

    return `/${slug.toLowerCase()}`;
  }

  async movePageToRecycleBin(pageName: string): Promise<void> {
    await this.openPage(pageName);

    // Only the selected tree row shows its menu button, and only on hover.
    // Hovering a row changes its accessible name, so match by text.
    const tree = this.page.getByRole("tree");
    await tree.getByText(pageName, { exact: true }).hover();
    await tree.getByTestId("tree-item-menu-button").click();
    await this.page.getByText("Move to recycle bin", { exact: true }).click();
    await this.page
      .getByRole("button", { name: "Move to recycle bin", exact: true })
      .click();

    await expect(
      this.page
        .getByRole("tree")
        .getByRole("link", { name: pageName, exact: true }),
    ).toBeHidden({ timeout: 20_000 });
  }

  async openPageBuilder(): Promise<PageBuilderPage> {
    await this.page.getByRole("link", { name: "Page Builder" }).click();

    const pageBuilder = new PageBuilderPage(this.page);
    await pageBuilder.expectLoaded();

    return pageBuilder;
  }
}

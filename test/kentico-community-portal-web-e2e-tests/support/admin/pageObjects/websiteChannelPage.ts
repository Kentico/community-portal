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

  async openPageBuilder(): Promise<PageBuilderPage> {
    await this.page.getByRole("link", { name: "Page Builder" }).click();

    const pageBuilder = new PageBuilderPage(this.page);
    await pageBuilder.expectLoaded();

    return pageBuilder;
  }
}

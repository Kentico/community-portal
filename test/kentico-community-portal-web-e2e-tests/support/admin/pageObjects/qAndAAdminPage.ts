import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../../config.js";

/** The Q&A admin application (QAndAApplicationPage) and its child listings. */
export class QAndAAdminPage {
  /** Caption of the DiscussionMemberSubscriptionDateModified column. */
  static readonly subscriptionDateColumn = "Modified";

  constructor(private readonly page: Page) {}

  /** Opens the application root, which redirects to its first child page. */
  async open(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/q-and-a`);
    await expect(this.page).toHaveURL(/\/q-and-a\/[a-z-]+/i, { timeout: 15_000 });
  }

  async openMemberSubscriptions(): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/q-and-a/member-subscriptions`);
    await expect(this.page).toHaveURL(/\/q-and-a\/member-subscriptions/i);
  }

  /** Slugs of the application's child pages, in navigation order. */
  async navSlugs(): Promise<string[]> {
    const links = this.page.locator('a[href*="/q-and-a/"]');
    await expect(links.first()).toBeVisible({ timeout: 15_000 });

    const hrefs = await links.evaluateAll((anchors) =>
      anchors.map((a) => a.getAttribute("href") ?? ""),
    );

    const slugs = hrefs
      .map((href) => /\/q-and-a\/([a-z-]+)\/?$/i.exec(href)?.[1])
      .filter((slug): slug is string => slug !== undefined);

    return [...new Set(slugs)];
  }

  subscriptionDateColumnHeader(): Locator {
    return this.page.getByText(QAndAAdminPage.subscriptionDateColumn, { exact: true });
  }
}

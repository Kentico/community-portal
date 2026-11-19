import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../../config.js";

/** A recipient list's Recipients listing (RecipientListSubscriberListExtender). */
export class RecipientListSubscribersPage {
  constructor(private readonly page: Page) {}

  /** Opens the list from the Recipient lists application by its display name. */
  async open(listDisplayName: string): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/recipient-lists/list`);
    await this.page.getByRole("link", { name: listDisplayName }).click();
    await this.page.getByRole("link", { name: "Recipients", exact: true }).click();

    await expect(this.page).toHaveURL(/recipient-lists\/list\/\d+\/subscribers/i);
    await expect(this.page.getByText("Confirmed On")).toBeVisible({ timeout: 15_000 });
  }

  /** The listing searches on Enter, not as you type. */
  async search(text: string): Promise<void> {
    const input = this.page.getByPlaceholder("Search");

    await input.fill(text);
    await input.press("Enter");
  }

  /** Table rows, header included. The listing renders no table (0 rows) when nothing matches. */
  rows(): Locator {
    return this.page.getByRole("row");
  }

  row(email: string): Locator {
    return this.rows().filter({ hasText: email });
  }
}

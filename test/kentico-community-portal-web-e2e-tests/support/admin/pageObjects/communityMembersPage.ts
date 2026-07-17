import { expect, type Page } from "@playwright/test";

export class CommunityMembersPage {
  constructor(private readonly page: Page) {}

  async expectLoaded(): Promise<void> {
    await expect(this.page).toHaveURL(/community-members/i);

    await expect(
      this.page.getByText("Member Management", { exact: true }),
    ).toBeVisible();
  }
}

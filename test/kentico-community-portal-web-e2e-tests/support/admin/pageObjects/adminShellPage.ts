import { expect, type Page } from "@playwright/test";
import { adminBaseUrl } from "../../config.js";
import { CommunityMembersPage } from "./communityMembersPage.js";
import { WebsiteChannelPage } from "./websiteChannelPage.js";

export class AdminShellPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto(adminBaseUrl);
  }

  async expectSignedIn(): Promise<void> {
    await expect(this.page).toHaveURL(/\/admin/i);
    await expect
      .poll(async () => this.page.url().toLowerCase().includes("sign-in"))
      .toBeFalsy();
    await expect(
      this.page.getByRole("link", { name: /community members/i }),
    ).toBeVisible();
  }

  async openCommunityMembers(): Promise<CommunityMembersPage> {
    const membersLink = this.page.getByRole("link", {
      name: /community members/i,
    });

    await expect(membersLink).toBeVisible({ timeout: 10_000 });
    await membersLink.click();

    await expect(this.page).toHaveURL(/community-members/i);

    return new CommunityMembersPage(this.page);
  }

  async openWebsiteChannel(): Promise<WebsiteChannelPage> {
    const websiteLink = this.page.getByRole("link", {
      name: "Website",
      exact: true,
    });

    await expect(websiteLink).toBeVisible({ timeout: 10_000 });
    await websiteLink.click();

    const websiteChannel = new WebsiteChannelPage(this.page);
    await websiteChannel.expectLoaded();

    return websiteChannel;
  }
}

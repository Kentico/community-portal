import { expect, type Page } from "@playwright/test";

export class CommunityMembersPage {
  constructor(private readonly page: Page) {}

  async expectLoaded(): Promise<void> {
    await expect(this.page).toHaveURL(/community-members/i);

    // The URL changes on client-side routing, well before there is anything to assert on:
    // the admin shell then fetches this feature's own module bundle and POSTs /admin/api/page
    // before React renders the headline, and the body reads "Loading" until it does. The
    // global 5s expect timeout covers a rendered page, not a cold module load, so this sat
    // right at the boundary - it passed in 8.6s on one CI run and timed out on the next.
    // Matches the explicit 10s already used for admin navigation in AdminShellPage.
    await expect(
      this.page.getByText("Member Management", { exact: true }),
    ).toBeVisible({ timeout: 15_000 });
  }
}

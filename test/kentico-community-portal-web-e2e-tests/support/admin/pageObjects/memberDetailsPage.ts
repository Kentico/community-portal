import { expect, type Locator, type Page } from "@playwright/test";
import { adminBaseUrl } from "../../config.js";

/** A member's General tab in the Members application (MemberEditExtender adds the links callout). */
export class MemberDetailsPage {
  constructor(private readonly page: Page) {}

  async open(memberID: number): Promise<void> {
    await this.page.goto(`${adminBaseUrl}/members/list/${memberID}/edit`);

    await expect(this.page).toHaveURL(new RegExp(`members/list/${memberID}/edit`, "i"));
    // Cold admin module load, see CommunityMembersPage
    await expect(this.contactLink().or(this.noContact())).toBeVisible({ timeout: 15_000 });
  }

  profileLink(): Locator {
    return this.page.getByTestId("member-profile-link");
  }

  contactLink(): Locator {
    return this.page.getByTestId("member-contact-link");
  }

  noContact(): Locator {
    return this.page.getByTestId("member-no-contact");
  }

  /** Edit form field input. Xperience sets data-testid to the field (column) name. */
  field(column: "MemberEmail" | "MemberAlternateEmail"): Locator {
    return this.page.locator(`input[data-testid="${column}"]`);
  }

  /** The field's server validation message. */
  fieldError(column: "MemberEmail" | "MemberAlternateEmail"): Locator {
    return this.page
      .locator(".kxp-form-item", { has: this.field(column) })
      .getByTestId("invalid-state-validation");
  }

  /** Fills the field and submits the form. Resolves once the submit command responds. */
  async save(column: "MemberEmail" | "MemberAlternateEmail", value: string): Promise<void> {
    await this.field(column).fill(value);

    await Promise.all([
      // Page commands all POST to /admin/api/cmd; the body names the command
      this.page.waitForResponse(
        (r) =>
          r.request().method() === "POST" &&
          r.url().endsWith("/admin/api/cmd") &&
          /\bSubmit\b/.test(r.request().postData() ?? ""),
      ),
      this.page.getByTestId("submit-form-button").click(),
    ]);
  }
}

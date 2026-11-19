import { expect, test } from "@playwright/test";
import { MemberDetailsPage } from "../../support/admin/pageObjects/memberDetailsPage.js";
import { appBaseUrl } from "../../support/config.js";
import { seedDataSubject } from "../../support/privacy/dataProtection.js";
import { getContactByEmail } from "../../support/privacy/state.js";

/*
 * The member General tab links to the public profile and to a contact with the
 * member's email (MemberEditExtender).
 */

test.describe("member details links", () => {
  test("links to the public profile and the linked contact", async ({ page }) => {
    // Seeds a member and two contacts with its email
    const seed = await seedDataSubject();
    const { contacts } = await getContactByEmail(seed.email);
    const contactIDs = contacts.map((c) => c.contactID);
    expect(contactIDs.length).toBeGreaterThan(0);

    const details = new MemberDetailsPage(page);
    await details.open(seed.memberID);

    const profileHref = await details.profileLink().getAttribute("href");
    expect(profileHref).toBeTruthy();

    const profileUrl = new URL(profileHref ?? "");
    expect(profileUrl.host).toBe(new URL(appBaseUrl).host);
    expect(profileUrl.pathname).toBe(`/member/${seed.memberID}`);

    await expect(details.contactLink()).toHaveAttribute(
      "href",
      new RegExp(`^/admin/contact-management/contacts/(${contactIDs.join("|")})/overview$`, "i"),
    );
    await expect(details.noContact()).toHaveCount(0);
  });
});

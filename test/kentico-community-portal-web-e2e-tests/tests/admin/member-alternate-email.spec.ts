import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { MemberDetailsPage } from "../../support/admin/pageObjects/memberDetailsPage.js";
import { appBaseUrl } from "../../support/config.js";
import { saveAlternateEmail } from "../../support/membership/emailVerification.js";
import { signIn } from "../../support/membership/flows.js";
import { seedDataSubject } from "../../support/privacy/dataProtection.js";
import { getMember } from "../../support/privacy/state.js";

/*
 * Admins manage a member's alternate email on the member General tab (Edit form).
 * MemberEmailIntegrityHandler: an admin-set email is verified on save, clearing it clears
 * the date. MemberEmailValidationRules: another member's account or verified alternate
 * email is rejected with a field error.
 */

/** Seeded user names and emails are the prefix, so each run needs its own. */
const uniquePrefix = (prefix: string) => `${prefix}-${randomUUID().slice(0, 8)}`;

test.describe("member alternate email (admin)", () => {
  test("sets the email as verified, then clears it", async ({ page }) => {
    const seed = await seedDataSubject(uniquePrefix("admin-alt-email"));
    const alternateEmail = seed.email.replace("@", ".work@");

    const details = new MemberDetailsPage(page);
    await details.open(seed.memberID);

    await details.save("MemberAlternateEmail", alternateEmail);
    await expect(details.fieldError("MemberAlternateEmail")).toHaveCount(0);

    let member = await getMember(seed.userName);
    expect(member.alternateEmail).toBe(alternateEmail);
    expect(member.alternateEmailVerifiedDate).not.toBeNull();

    await details.save("MemberAlternateEmail", "");
    await expect(details.fieldError("MemberAlternateEmail")).toHaveCount(0);

    member = await getMember(seed.userName);
    expect(member.alternateEmail).toBe("");
    expect(member.alternateEmailVerifiedDate).toBeNull();
  });

  test("rejects another member's account or verified alternate email", async ({ page }) => {
    const member = await seedDataSubject(uniquePrefix("admin-alt-email-a"));
    const other = await seedDataSubject(uniquePrefix("admin-alt-email-b"));
    const otherAlternate = other.email.replace("@", ".work@");

    const details = new MemberDetailsPage(page);

    // Another member's account email
    await details.open(member.memberID);
    await details.save("MemberAlternateEmail", other.email);
    await expect(details.field("MemberAlternateEmail")).toHaveAttribute("aria-invalid", "true");
    await expect(details.fieldError("MemberAlternateEmail")).toBeVisible();
    expect((await getMember(member.userName)).alternateEmail ?? "").toBe("");

    // Another member's verified (admin-set) alternate email, as alternate or account email
    await details.open(other.memberID);
    await details.save("MemberAlternateEmail", otherAlternate);
    expect((await getMember(other.userName)).alternateEmailVerifiedDate).not.toBeNull();

    await details.open(member.memberID);
    await details.save("MemberAlternateEmail", otherAlternate.toUpperCase());
    await expect(details.fieldError("MemberAlternateEmail")).toBeVisible();

    await details.open(member.memberID);
    await details.save("MemberEmail", otherAlternate);
    await expect(details.fieldError("MemberEmail")).toBeVisible();
    expect((await getMember(member.userName)).email).toBe(member.email);
  });

  test("verifying an address removes it from other members' unverified alternate emails", async ({
    page,
    browser,
  }) => {
    const claimant = await seedDataSubject(uniquePrefix("admin-alt-email-c"));
    const owner = await seedDataSubject(uniquePrefix("admin-alt-email-d"));
    const contested = owner.email.replace("@", ".work@");

    // The claimant adds the address on My Account, but never verifies it
    const memberContext = await browser.newContext({
      baseURL: appBaseUrl,
      ignoreHTTPSErrors: true,
      storageState: { cookies: [], origins: [] },
    });
    try {
      const memberPage = await memberContext.newPage();
      await signIn(memberPage, claimant.userName, claimant.password);
      await memberPage.goto("/my-account");
      await saveAlternateEmail(memberPage, contested);
    } finally {
      await memberContext.close();
    }

    let claimantState = await getMember(claimant.userName);
    expect(claimantState.alternateEmail).toBe(contested);
    expect(claimantState.alternateEmailVerifiedDate).toBeNull();

    // An unverified alternate email doesn't block the admin
    const details = new MemberDetailsPage(page);
    await details.open(owner.memberID);
    await details.save("MemberAlternateEmail", contested);
    await expect(details.fieldError("MemberAlternateEmail")).toHaveCount(0);

    await expect
      .poll(async () => (await getMember(claimant.userName)).alternateEmail)
      .toBe("");
    claimantState = await getMember(claimant.userName);
    expect(claimantState.alternateEmailVerifiedDate).toBeNull();
  });
});

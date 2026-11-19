import { expect, test } from "@playwright/test";
import {
  assertAuthenticated,
  signIn,
  signInFailedMessage,
  signInSelectors,
} from "../../support/membership/flows.js";
import {
  collectPersonalData,
  type DataProtectionSeed,
  erasePersonalData,
  getDataProtectionState,
  seedDataSubject,
} from "../../support/privacy/dataProtection.js";

/*
 * The portal's identity collector, data collector and eraser, run through Xperience's
 * registers like the admin Data protection app. Each test seeds its own member, so the specs are
 * parallel-safe. Erasure keeps the member (archive + anonymize).
 */

const erasedEmail = (memberID: number) => `erased-${memberID}@erased.invalid`;

function prefix(name: string): string {
  return `e2e-dp-${name}-${Date.now()}-${Math.floor(Math.random() * 10_000)}`;
}

test.describe("Data protection", () => {
  test("right to access lists every kind of personal data", async () => {
    const seed = await seedDataSubject(prefix("access"));

    const text = await collectPersonalData(seed.email, "human");

    for (const section of [
      "Member:",
      "External logins:",
      "Discussion notification settings:",
      "Badges:",
      "Form: Newsletter Signup:",
      "Form: Community Feedback:",
      "Form: MVP activity:",
      "Contacts:",
      "Activities:",
      "Consent agreements:",
      "Email subscriptions:",
      "Sent emails:",
      "Email opens and clicks:",
    ]) {
      expect(text, `missing section ${section}`).toContain(section);
    }
    expect(text).toContain(`User name: ${seed.userName}`);
  });

  test("data portability returns one JSON document", async () => {
    const seed = await seedDataSubject(prefix("portability"));

    const json = JSON.parse(await collectPersonalData(seed.email, "machine")) as Record<
      string,
      Record<string, string | null>[]
    >;

    expect(Object.keys(json)).toEqual(
      expect.arrayContaining(["Member", "Contacts", "Form: MVP activity", "Sent emails"]),
    );
    expect(json["Member"]?.[0]?.["Email"]).toBe(seed.email);
  });

  test("an email with no stored contact still finds form data", async () => {
    const seed = await seedDataSubject(prefix("formonly"));
    await erasePersonalData(seed.email, { DeleteContacts: true });

    // No contact has the email now; the identity collector carries it on a transient contact
    const text = await collectPersonalData(seed.email, "human");

    expect(text).not.toContain("Contacts:");
    expect(text).toContain("Form: Newsletter Signup:");
  });

  test.describe("right to be forgotten", () => {
    let seed: DataProtectionSeed;
    /** Another member with rows in the same forms, list and email configuration. Must be untouched. */
    let bystander: DataProtectionSeed;
    /** The erased subject's machine-readable export, taken before erasure. */
    let exportBeforeErasure: string;

    test.beforeEach(async () => {
      [seed, bystander] = await Promise.all([
        seedDataSubject(prefix("erase")),
        seedDataSubject(prefix("bystander")),
      ]);
      exportBeforeErasure = await collectPersonalData(seed.email, "machine");
      await erasePersonalData(seed.email);
    });

    test("anonymizes, disables and archives the member, keeping badges", async () => {
      const state = await getDataProtectionState(seed);

      expect(state.memberExists).toBe(true);
      expect(state.memberEmail).toBe(erasedEmail(seed.memberID));
      expect(state.memberUserName).toBe(`former-member-${seed.memberID}`);
      expect(state.memberFirstName).toBe("Former member");
      expect(state.memberLastName).toBe("");
      expect(state.memberBio).toBe("");
      expect(state.memberLinkedIn).toBe("");
      expect(state.memberModerationStatus).toBe("Archived");
      expect(state.memberEnabled).toBe(false);
      expect(state.externalLogins).toBe(0);
      expect(state.badges).toBeGreaterThan(0);
    });

    test("deletes contacts and subscriptions", async () => {
      const state = await getDataProtectionState(seed);

      expect(state.contacts).toBe(0);
      expect(state.listMembershipExists).toBe(false);
      expect(state.notificationSettings).toBe(0);
    });

    test("deletes short-lived form rows and scrubs history rows", async () => {
      const state = await getDataProtectionState(seed);
      const row = (name: string) => state.formRows.find((r) => r.formName === name);

      expect(row("NewsletterSignup")?.exists).toBe(false);
      expect(row("CommunityFeedbackForm")?.exists).toBe(false);

      const history = row("MVPActivity");
      expect(history?.exists).toBe(true);
      expect(history?.memberID).toBe(seed.memberID);
      expect(history?.email).toBe(erasedEmail(seed.memberID));
    });

    test("unlinks email statistics", async () => {
      const state = await getDataProtectionState(seed);

      expect(state.sentEmailExists).toBe(false);
      expect(state.emailStatisticsHitExists).toBe(true);
      expect(state.emailStatisticsHitMailoutGuid).toBe("00000000-0000-0000-0000-000000000000");
    });

    test("leaves nothing for the original email", async () => {
      const text = await collectPersonalData(seed.email, "human");

      expect(text).not.toContain(seed.email);
      expect(text).not.toContain(bystander.email);
    });

    test("keeps every row of another data subject", async () => {
      const state = await getDataProtectionState(bystander);

      expect(state).toMatchObject({
        memberExists: true,
        memberEmail: bystander.email,
        memberUserName: bystander.userName,
        memberFirstName: "Erasure",
        memberModerationStatus: "None",
        externalLogins: 1,
        notificationSettings: 1,
        listMembershipExists: true,
        sentEmailExists: true,
        emailStatisticsHitExists: true,
        emailStatisticsHitMailoutGuid: bystander.mailoutGuid,
      });
      // Same-email contacts merge in the background, so only "some remain" is stable
      expect(state.contacts).toBeGreaterThan(0);
      expect(state.badges).toBeGreaterThan(0);
      expect(state.formRows).toHaveLength(bystander.formRows.length);
      for (const row of state.formRows) {
        expect(row, row.formName).toMatchObject({ exists: true, email: bystander.email });
      }
      expect(state.formRows.find((r) => r.formName === "MVPActivity")?.memberID).toBe(
        bystander.memberID,
      );

      // Still exportable in full
      const text = await collectPersonalData(bystander.email, "human");
      expect(text).toContain(`User name: ${bystander.userName}`);
      for (const section of ["Form: Newsletter Signup:", "Sent emails:", "Email opens and clicks:"]) {
        expect(text, `missing section ${section}`).toContain(section);
      }
    });

    test("the subject's export held none of another subject's data", async () => {
      expect(exportBeforeErasure).toContain(seed.email);
      expect(exportBeforeErasure).not.toContain(bystander.email);
      expect(exportBeforeErasure).not.toContain(bystander.userName);
    });
  });

  test("an erased member can't sign in", async ({ page, context }) => {
    const seed = await seedDataSubject(prefix("signin"));

    // Positive control: the same credentials work before erasure
    await signIn(page, seed.userName, seed.password);
    await assertAuthenticated(page, seed.userName);
    await context.clearCookies();

    await erasePersonalData(seed.email);

    for (const userNameOrEmail of [seed.userName, seed.email]) {
      await signIn(page, userNameOrEmail, seed.password);

      await expect(page.locator(signInSelectors.error)).toContainText(signInFailedMessage);
    }

    // The renamed account is archived and its password erased. Sign-in checks the password before
    // showing an account state (ADR 0005), so it gets the generic failure, not the moderation message
    await signIn(page, `former-member-${seed.memberID}`, seed.password);
    await expect(page.locator(signInSelectors.error)).toContainText(signInFailedMessage);
    await expect(page.locator(signInSelectors.moderationMessage)).toHaveCount(0);

    await page.goto("/my-account");
    await expect(page).toHaveURL(/\/login(\?|$)/);
  });

  test("options run independently", async () => {
    const seed = await seedDataSubject(prefix("partial"));

    await erasePersonalData(seed.email, { UnlinkEmailStatistics: true });
    const state = await getDataProtectionState(seed);

    expect(state.sentEmailExists).toBe(false);
    expect(state.memberEmail).toBe(seed.email);
    expect(state.contacts).toBeGreaterThan(0);
    expect(state.formRows.every((r) => r.exists)).toBe(true);
  });
});

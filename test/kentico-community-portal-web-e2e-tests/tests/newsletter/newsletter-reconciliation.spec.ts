import { expect, test } from "@playwright/test";
import { erasePersonalData } from "../../support/privacy/dataProtection.js";
import {
  consentNames,
  emailMarketingReconciliationTask,
  getConsentAgreements,
  getNewsletterUnsubscribeUrl,
  getRecipientListState,
  hasActiveAgreement,
  runScheduledTask,
  seedContacts,
  type SeededContact,
} from "../../support/privacy/state.js";

/*
 * The Email marketing reconciliation task, the safety net for EmailMarketingAgreementHandler.
 * It's idempotent and only touches Email marketing agreements, so it's safe to run while other
 * specs run.
 *
 * Seeding a subscription saves a confirmation row, so EmailMarketingAgreementHandler has already
 * agreed (or not) by the time a spec runs. The drift tests change state around the handler first.
 */

/** Development and CI `EmailMarketingOptions:AgreementCutoffDate`. */
const agreementCutoff = new Date(2026, 0, 1);

/** Whole days from now back to `date`, plus `offset` days further back. */
function daysBeforeNow(date: Date, offset: number): number {
  return Math.floor((Date.now() - date.getTime()) / 86_400_000) + offset;
}

// One at a time: each test checks its drift exists before running the task, and a parallel
// run of the task by a sibling test would fix it first
test.describe.configure({ mode: "default" });

function subscriberOf(seeded: SeededContact[]): SeededContact {
  const subscriber = seeded.find((contact) => contact.kind === "Subscriber");
  expect(subscriber?.email).toBeTruthy();

  return subscriber!;
}

async function hasEmailMarketingAgreement(email: string): Promise<boolean> {
  return hasActiveAgreement(
    await getConsentAgreements(email, consentNames.emailMarketing),
    consentNames.emailMarketing,
  );
}

test.describe("Email marketing reconciliation", () => {
  test("agrees for a subscriber confirmed after the cutoff who has no agreement", async () => {
    const subscriber = subscriberOf(
      await seedContacts({
        prefix: `e2e-newsletter-missing-${Date.now()}`,
        // A day after the cutoff
        createdDaysAgo: daysBeforeNow(agreementCutoff, -1),
        subscriberWithoutAgreement: true,
      }),
    );
    const email = subscriber.email!;
    expect((await getRecipientListState(email)).subscribed).toBe(true);
    expect(await getConsentAgreements(email, consentNames.emailMarketing)).toEqual([]);

    await runScheduledTask(emailMarketingReconciliationTask);

    expect(await hasEmailMarketingAgreement(email)).toBe(true);
  });

  test("gives no agreement to a subscriber confirmed before the cutoff", async () => {
    const subscriber = subscriberOf(
      await seedContacts({
        prefix: `e2e-newsletter-cutoff-${Date.now()}`,
        // A day before the cutoff
        createdDaysAgo: daysBeforeNow(agreementCutoff, 1),
      }),
    );
    const email = subscriber.email!;

    await runScheduledTask(emailMarketingReconciliationTask);

    // Still a subscriber (their old opt-in stands), but they never saw the consent text
    expect((await getRecipientListState(email)).subscribed).toBe(true);
    expect(await getConsentAgreements(email, consentNames.emailMarketing)).toEqual([]);
  });

  test("revokes the agreement of a contact removed from the list", async () => {
    const subscriber = subscriberOf(
      await seedContacts({
        prefix: `e2e-newsletter-removed-${Date.now()}`,
        // Older than PendingMembershipGrace, so it isn't treated as still joining
        createdDaysAgo: 1,
      }),
    );
    const email = subscriber.email!;
    expect(await hasEmailMarketingAgreement(email)).toBe(true);

    // Removes the list membership only; no confirmation row changes, so the handler doesn't run
    await erasePersonalData(email, { RemoveSubscriptions: true });
    const list = await getRecipientListState(email);
    expect(list.isListMember).toBe(false);
    expect(list.isConfirmationApproved).toBe(true);
    expect(await hasEmailMarketingAgreement(email)).toBe(true);

    await runScheduledTask(emailMarketingReconciliationTask);

    const agreements = await getConsentAgreements(email, consentNames.emailMarketing);
    expect(hasActiveAgreement(agreements, consentNames.emailMarketing)).toBe(false);
    expect(agreements.at(-1)!.revoked).toBe(true);
  });

  test("unsubscribing from the email link revokes the agreement", async ({ request }) => {
    const subscriber = subscriberOf(
      await seedContacts({ prefix: `e2e-newsletter-unsub-${Date.now()}` }),
    );
    const email = subscriber.email!;
    expect(await hasEmailMarketingAgreement(email)).toBe(true);

    // Xperience's own unsubscribe endpoint, as linked from a newsletter (one-click POST)
    const response = await request.post(await getNewsletterUnsubscribeUrl(email), {
      maxRedirects: 0,
    });
    expect(response.status(), await response.text()).toBeLessThan(400);
    expect((await getRecipientListState(email)).subscribed).toBe(false);

    await runScheduledTask(emailMarketingReconciliationTask);

    const agreements = await getConsentAgreements(email, consentNames.emailMarketing);
    expect(hasActiveAgreement(agreements, consentNames.emailMarketing)).toBe(false);
    expect(agreements.at(-1)!.revoked).toBe(true);
  });
});

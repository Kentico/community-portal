import { expect, test } from "@playwright/test";
import {
  formRetentionPolicies,
  formRowExists,
  keptActivityTypes,
  retentionDays,
  retentionTasks,
  seedFormRow,
  setFormRetentionPolicy,
} from "../../support/privacy/dataProtection.js";
import {
  getActivities,
  getContactByGuid,
  runScheduledTask,
  seedContacts,
  type SeededContact,
  type SeededContactKind,
} from "../../support/privacy/state.js";

/*
 * Retention tasks (defaults: anonymous contacts 1 year, everything else that expires 3 years
 * after the last activity). Isolated: the tasks delete across the whole database
 * and one test changes a form's retention policy.
 */

const OLDER_THAN_THREE_YEARS = 1200;
const BETWEEN_ONE_AND_THREE_YEARS = 400;

async function exists(contact: SeededContact): Promise<boolean> {
  return (await getContactByGuid(contact.contactGUID)).exists;
}

function byKind(seeded: SeededContact[], kind: SeededContactKind): SeededContact {
  const contact = seeded.find((c) => c.kind === kind);
  expect(contact, `seeded ${kind}`).toBeTruthy();

  return contact!;
}

test.describe.configure({ mode: "serial" });

test.describe("Contact retention", () => {
  test("deletes inactive anonymous and known contacts, keeps members and subscribers", async () => {
    const old = await seedContacts({
      prefix: `e2e-retention-old-${Date.now()}`,
      createdDaysAgo: OLDER_THAN_THREE_YEARS,
      lastActivityDaysAgo: OLDER_THAN_THREE_YEARS,
    });
    const recent = await seedContacts({
      prefix: `e2e-retention-recent-${Date.now()}`,
      createdDaysAgo: BETWEEN_ONE_AND_THREE_YEARS,
      lastActivityDaysAgo: BETWEEN_ONE_AND_THREE_YEARS,
    });

    await runScheduledTask(retentionTasks.contacts);

    expect(await exists(byKind(old, "Anonymous"))).toBe(false);
    expect(await exists(byKind(old, "Email"))).toBe(false);
    expect(await exists(byKind(old, "Subscriber"))).toBe(true);
    expect(await exists(byKind(old, "Member"))).toBe(true);

    expect(await exists(byKind(recent, "Anonymous"))).toBe(false);
    expect(await exists(byKind(recent, "Email"))).toBe(true);
  });

  test("deletes contacts a day past their period, keeps them a day inside it", async () => {
    const { AnonymousContactInactiveDays: anonymousDays, KnownContactInactiveDays: knownDays } =
      retentionDays;
    const seed = (name: string, lastActivityDaysAgo: number) =>
      seedContacts({
        prefix: `e2e-retention-${name}-${Date.now()}`,
        createdDaysAgo: OLDER_THAN_THREE_YEARS,
        lastActivityDaysAgo,
      });
    const anonymousInside = await seed("anon-inside", anonymousDays - 1);
    const anonymousOutside = await seed("anon-outside", anonymousDays + 1);
    const knownInside = await seed("known-inside", knownDays - 1);
    const knownOutside = await seed("known-outside", knownDays + 1);

    await runScheduledTask(retentionTasks.contacts);

    expect(await exists(byKind(anonymousInside, "Anonymous"))).toBe(true);
    expect(await exists(byKind(anonymousOutside, "Anonymous"))).toBe(false);
    // Known contacts have the longer period
    expect(await exists(byKind(anonymousOutside, "Email"))).toBe(true);

    expect(await exists(byKind(knownInside, "Email"))).toBe(true);
    expect(await exists(byKind(knownOutside, "Email"))).toBe(false);
    expect(await exists(byKind(knownOutside, "Subscriber"))).toBe(true);
    expect(await exists(byKind(knownOutside, "Member"))).toBe(true);
  });

  test("keeps contacts active within the period", async () => {
    const active = await seedContacts({
      prefix: `e2e-retention-active-${Date.now()}`,
      createdDaysAgo: OLDER_THAN_THREE_YEARS,
      lastActivityDaysAgo: 10,
    });

    await runScheduledTask(retentionTasks.contacts);

    expect(await exists(byKind(active, "Anonymous"))).toBe(true);
    expect(await exists(byKind(active, "Email"))).toBe(true);
  });
});

test.describe("Activity retention", () => {
  test("deletes activities past the period except kept types", async () => {
    const days = retentionDays.ActivityDays;
    const kept = [
      { type: "pagevisit", daysAgo: 10 },
      { type: "pagevisit", daysAgo: days - 1 },
      ...keptActivityTypes.map((type) => ({ type, daysAgo: OLDER_THAN_THREE_YEARS })),
    ];
    const deleted = [
      { type: "pagevisit", daysAgo: days + 1 },
      { type: "pagevisit", daysAgo: OLDER_THAN_THREE_YEARS },
    ];
    const seeded = await seedContacts({
      prefix: `e2e-retention-activity-${Date.now()}`,
      createdDaysAgo: OLDER_THAN_THREE_YEARS,
      activities: [...kept, ...deleted],
    });
    const email = byKind(seeded, "Email").email!;
    const title = ({ type, daysAgo }: { type: string; daysAgo: number }) =>
      `E2E seeded ${type} ${daysAgo} days ago`;
    const titles = async () => (await getActivities(email)).map((a) => a.activityTitle).sort();
    expect(await titles()).toEqual([...kept, ...deleted].map(title).sort());

    await runScheduledTask(retentionTasks.activities);

    expect(await titles()).toEqual(kept.map(title).sort());
  });
});

test.describe("Form data retention", () => {
  test("deletes old short-lived rows and keeps history rows", async () => {
    const shortLived = await seedFormRow("NewsletterSignup", OLDER_THAN_THREE_YEARS, {
      Email: `e2e-retention-${Date.now()}@kentico.local`,
    });
    const recentShortLived = await seedFormRow("NewsletterSignup", 10, {
      Email: `e2e-retention-recent-${Date.now()}@kentico.local`,
    });
    const history = await seedFormRow("MVPActivity", OLDER_THAN_THREE_YEARS);

    await runScheduledTask(retentionTasks.formData);

    expect(await formRowExists(shortLived)).toBe(false);
    expect(await formRowExists(recentShortLived)).toBe(true);
    expect(await formRowExists(history)).toBe(true);
  });

  test("honors a form's own retention days", async () => {
    const previous = await setFormRetentionPolicy({
      formName: "CommunityFeedbackForm",
      policy: formRetentionPolicies.shortLived,
      retentionDays: 30,
    });

    try {
      const old = await seedFormRow("CommunityFeedbackForm", 45);
      const recent = await seedFormRow("CommunityFeedbackForm", 5);

      await runScheduledTask(retentionTasks.formData);

      expect(await formRowExists(old)).toBe(false);
      expect(await formRowExists(recent)).toBe(true);
    } finally {
      await setFormRetentionPolicy(previous);
    }
  });

  test("keeps rows of an unclassified form", async () => {
    const previous = await setFormRetentionPolicy({
      formName: "Poll_2025_02_13_FebruaryRefresh",
      policy: null,
      retentionDays: 0,
    });

    try {
      const row = await seedFormRow("Poll_2025_02_13_FebruaryRefresh", OLDER_THAN_THREE_YEARS);

      await runScheduledTask(retentionTasks.formData);

      expect(await formRowExists(row)).toBe(true);
    } finally {
      await setFormRetentionPolicy(previous);
    }
  });
});

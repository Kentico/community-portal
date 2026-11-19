import { readFileSync } from "node:fs";
import { appBaseUrl } from "../config.js";

/*
 * Clients for the data protection and form retention endpoints in
 * E2ETestingEndpoints.cs. Collect and erase run the registered Xperience collectors and erasers,
 * the same path as the admin Data protection app.
 */

const testingBaseUrl = `${appBaseUrl}/testing/e2e`;

/** Keys of the Right to be forgotten dialog options (ErasureOptionKeys.cs). */
export const erasureOptions = {
  anonymizeMember: "AnonymizeMember",
  deleteContacts: "DeleteContacts",
  deleteShortLivedFormData: "DeleteShortLivedFormData",
  scrubFormEmails: "ScrubFormEmails",
  removeSubscriptions: "RemoveSubscriptions",
  unlinkEmailStatistics: "UnlinkEmailStatistics",
} as const;

export const formRetentionPolicies = {
  history: "History",
  shortLived: "ShortLived",
} as const;

/** Retention tasks by registered identifier (no configuration needed to run them inline). */
export const retentionTasks = {
  contacts: "KenticoCommunity.ContactRetentionTask",
  activities: "KenticoCommunity.ActivityRetentionTask",
  formData: "KenticoCommunity.FormDataRetentionTask",
} as const;

/** Retention periods (days) from the app's own `RetentionOptions`, so a change needs no spec edits. */
export const retentionDays = (
  JSON.parse(
    readFileSync(
      new URL("../../../../src/Kentico.Community.Portal.Web/appsettings.json", import.meta.url),
      "utf8",
    ),
  ) as {
    RetentionOptions: {
      AnonymousContactInactiveDays: number;
      KnownContactInactiveDays: number;
      ActivityDays: number;
    };
  }
).RetentionOptions;

/** `RetentionOptions.KeptActivityTypes` (C# default, not in appsettings.json). */
export const keptActivityTypes = ["memberregistration", "bizformsubmit"] as const;

export type SeededFormRow = {
  formName: string;
  className: string;
  itemID: number;
};

export type DataProtectionSeed = {
  memberID: number;
  userName: string;
  email: string;
  password: string;
  contactIDs: number[];
  formRows: SeededFormRow[];
  listMembershipID: number;
  sentEmailID: number;
  mailoutGuid: string;
  emailStatisticsHitID: number;
};

export type FormRowState = {
  formName: string;
  exists: boolean;
  email: string | null;
  memberID: number | null;
};

export type DataProtectionState = {
  memberExists: boolean;
  memberEmail: string | null;
  memberUserName: string | null;
  memberFirstName: string | null;
  memberLastName: string | null;
  memberBio: string | null;
  memberLinkedIn: string | null;
  memberModerationStatus: string | null;
  memberEnabled: boolean | null;
  externalLogins: number;
  notificationSettings: number;
  badges: number;
  /** Contacts with the seeded email. Xperience merges them in the background, so the count can drop to 1. */
  contacts: number;
  listMembershipExists: boolean;
  formRows: FormRowState[];
  sentEmailExists: boolean;
  emailStatisticsHitExists: boolean;
  emailStatisticsHitMailoutGuid: string | null;
};

export type FormRetentionPolicy = {
  formName: string;
  /** null = unclassified */
  policy: string | null;
  retentionDays: number;
  /** Returned with the previous policy; passing it back restores the same row (no CI change). */
  policyGuid?: string | null;
};

async function post<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${testingBaseUrl}${path}`, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(
      `Data protection testing tool '${path}' failed. ${response.status} ${await response.text()}`,
    );
  }

  const text = await response.text();

  return (text ? JSON.parse(text) : undefined) as T;
}

/** Seeds a member with one of each kind of personal data the portal stores. */
export function seedDataSubject(prefix?: string): Promise<DataProtectionSeed> {
  return post("/data-protection/seed-member", { prefix });
}

export async function collectPersonalData(
  email: string,
  format: "human" | "machine",
): Promise<string> {
  const result = await post<{ text: string }>("/data-protection/collect", {
    email,
    format,
  });

  return result.text;
}

/** Runs the Right to be forgotten erasers. Every option is on unless `options` is given. */
export async function erasePersonalData(
  email: string,
  options?: Partial<Record<(typeof erasureOptions)[keyof typeof erasureOptions], boolean>>,
): Promise<void> {
  await post("/data-protection/erase", { email, options });
}

export function getDataProtectionState(
  seed: DataProtectionSeed,
): Promise<DataProtectionState> {
  return post("/data-protection/state", seed);
}

/** Sets (or with `policy: null` removes) a form's retention policy. Returns the previous one. */
export function setFormRetentionPolicy(
  policy: FormRetentionPolicy,
): Promise<FormRetentionPolicy> {
  return post("/form-retention/policy", policy);
}

export function seedFormRow(
  formName: string,
  insertedDaysAgo: number,
  values?: Record<string, string | number>,
): Promise<SeededFormRow> {
  return post("/form-retention/seed-row", { formName, insertedDaysAgo, values });
}

export async function formRowExists(row: SeededFormRow): Promise<boolean> {
  const params = new URLSearchParams({
    className: row.className,
    itemID: String(row.itemID),
  });
  const response = await fetch(`${testingBaseUrl}/form-retention/row-exists?${params}`);

  if (!response.ok) {
    throw new Error(`row-exists failed. ${response.status} ${await response.text()}`);
  }

  return ((await response.json()) as { exists: boolean }).exists;
}

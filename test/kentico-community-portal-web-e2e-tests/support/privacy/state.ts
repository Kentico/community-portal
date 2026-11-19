import { randomUUID } from "node:crypto";
import type { BrowserContext } from "@playwright/test";
import { appBaseUrl } from "../config.js";

/*
 * Clients for the privacy endpoints in E2ETestingEndpoints.cs (`/testing/e2e`). They read stored
 * state directly from the database, so specs assert on what was saved rather than on the code
 * under test.
 */

const testingBaseUrl = `${appBaseUrl}/testing/e2e`;

/** Consent code names. */
export const consentNames = {
  websiteTracking: "KenticoCommunityWebsiteTracking",
  emailMarketing: "EmailMarketing",
  /** Legacy consent, no longer used. Nothing may write agreements to it; specs only assert there are none. */
  legacyTracking: "KenticoCommunityPortalTracking",
} as const;

export const recipientListNames = {
  newsletter: "KenticoCommunityNewsletter",
} as const;

export type Contact = {
  contactID: number;
  contactGUID: string;
  email: string | null;
  firstName: string;
  lastName: string;
  created: string;
  contactGroups: string[];
};

export type ContactsResult = {
  exists: boolean;
  /** Newest first. */
  contacts: Contact[];
};

/** A contact as it was when inserted. It may since have been merged or deleted. */
export type CorrelatedContact = {
  contactGUID: string;
  created: string;
  email: string | null;
};

export type ConsentAgreement = {
  agreementID: number;
  contactID: number;
  contactGUID: string;
  consentName: string;
  revoked: boolean;
  /** Hash of the consent text the visitor agreed to. */
  agreementConsentHash: string;
  /** Hash of the consent text now. Differs after the text is edited. */
  currentConsentHash: string;
  time: string;
};

export type RecipientListState = {
  email: string;
  listName: string;
  /** List member and approved confirmation, as RecipientListManager defines it. */
  subscribed: boolean;
  isListMember: boolean;
  isConfirmationApproved: boolean;
  confirmationDate: string | null;
  contactIDs: number[];
  /** Every confirmation row, oldest first. Xperience adds one per confirm and unsubscribe. */
  confirmations: { date: string; isApproved: boolean; contactID: number }[];
};

export type MemberState = {
  memberID: number;
  userName: string;
  email: string;
  enabled: boolean;
  moderationStatus: string | null;
  /** Null when the member field doesn't exist in the database. */
  termsAcceptedVersion: string | null;
  termsAcceptedDate: string | null;
  newsletterOptInRequestedDate: string | null;
  /** Null when the member field doesn't exist in the database. */
  alternateEmail: string | null;
  /** Null until the alternate email is verified. */
  alternateEmailVerifiedDate: string | null;
  /** Null until the member confirms their email (or an administrator enables them). Kept when disabled. */
  emailConfirmedDate: string | null;
};

export type RunTaskResult = {
  codeName: string;
  identifier: string;
  succeeded: boolean;
  message: string | null;
};

export type SeedContactsOptions = {
  /** Shared by every seeded email and the member name. Generated when omitted. */
  prefix?: string;
  createdDaysAgo?: number;
  /** When set, each contact gets one page visit activity this many days ago. */
  lastActivityDaysAgo?: number;
  recipientListName?: string;
  /** Extra backdated activities every seeded contact gets. Titled `E2E seeded {type} {daysAgo} days ago`. */
  activities?: { type: string; daysAgo: number }[];
  /**
   * Deletes the Email marketing agreement the confirmation handler records for the `Subscriber`
   * contact, so it's subscribed without one.
   */
  subscriberWithoutAgreement?: boolean;
};

export type SeededContactKind =
  | "Anonymous"
  | "Email"
  | "Subscriber"
  | "Member";

export type SeededContact = {
  kind: SeededContactKind;
  contactID: number;
  contactGUID: string;
  email: string | null;
  created: string;
};

async function send<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${testingBaseUrl}${path}`, {
    ...init,
    headers: init?.body
      ? { "content-type": "application/json", ...init.headers }
      : init?.headers,
  });

  if (!response.ok) {
    const body = await response.text();

    throw new Error(
      `Privacy testing tool '${path}' failed. ${response.status} ${body}`,
    );
  }

  return (await response.json()) as T;
}

function query(params: Record<string, string | undefined>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) {
      search.set(key, value);
    }
  }

  return search.toString();
}

export function getContactByEmail(email: string): Promise<ContactsResult> {
  return send(`/contact?${query({ email })}`);
}

export function getContactByGuid(guid: string): Promise<ContactsResult> {
  return send(`/contact?${query({ guid })}`);
}

/** Header the app's E2E contact correlation handler reads (`E2EContactCorrelation.HEADER_NAME`). */
const correlationHeader = "x-e2e-correlation";

const portalRoutePattern = `${new URL(appBaseUrl).origin}/**`;

/**
 * Tags every request this context makes to the portal from now on with a new correlation id, and
 * returns it. Pass it to {@link getContactsCreatedBy} to see which contacts those requests created,
 * unaffected by contacts other specs create in parallel. Each call starts a new id, so call it
 * right before the steps under test.
 *
 * Added by routing rather than `setExtraHTTPHeaders`, so cross-origin requests (Vite assets,
 * third parties) don't get the header and need no CORS preflight. Route header overrides also
 * apply to redirects.
 */
export async function withCorrelation(context: BrowserContext): Promise<string> {
  const id = randomUUID();

  await context.unroute(portalRoutePattern);
  await context.route(portalRoutePattern, (route) =>
    route.fallback({
      headers: { ...route.request().headers(), [correlationHeader]: id },
    }),
  );

  return id;
}

/**
 * Contacts inserted during requests tagged with the correlation id (see {@link withCorrelation}).
 * The app records them as they're inserted, in-request; every portal contact creation path runs
 * in a request, so an empty list means none were created.
 */
export async function getContactsCreatedBy(
  correlation: string,
): Promise<CorrelatedContact[]> {
  const result = await send<{ contacts: CorrelatedContact[] }>(
    `/contacts/created-by?${query({ correlation })}`,
  );

  return result.contacts;
}

/** Every agreement row, oldest first, for all contacts with the email. */
export function getConsentAgreements(
  email: string,
  consent?: string,
): Promise<ConsentAgreement[]> {
  return send(`/consent-agreements?${query({ email, consent })}`);
}

export function getConsentAgreementsByContact(
  guid: string,
  consent?: string,
): Promise<ConsentAgreement[]> {
  return send(`/consent-agreements/by-contact?${query({ guid, consent })}`);
}

/**
 * Whether any of the contacts has a current (latest, not revoked) agreement to the consent.
 * Xperience writes a new row for every agree and revoke, so only the latest row per contact counts.
 */
export function hasActiveAgreement(
  agreements: ConsentAgreement[],
  consentName: string,
): boolean {
  const latestByContact = new Map<number, ConsentAgreement>();

  for (const agreement of agreements) {
    if (agreement.consentName === consentName) {
      latestByContact.set(agreement.contactID, agreement);
    }
  }

  return [...latestByContact.values()].some((agreement) => !agreement.revoked);
}

/** Activity type code names the specs read. */
export const activityTypes = {
  memberRegistration: "memberregistration",
  emailClick: "emailclick",
} as const;

export type Activity = {
  activityID: number;
  contactGUID: string;
  activityType: string;
  activityTitle: string;
  activityValue: string;
  activityURL: string;
  created: string;
};

/** Activities, oldest first, of all contacts with the email. Optionally one activity type only. */
export function getActivities(
  email: string,
  type?: string,
): Promise<Activity[]> {
  return send(`/activities?${query({ email, type })}`);
}

export function getRecipientListState(
  email: string,
  list: string = recipientListNames.newsletter,
): Promise<RecipientListState> {
  return send(`/recipient-list?${query({ email, list })}`);
}

export function getMember(userName: string): Promise<MemberState> {
  return send(`/member?${query({ userName })}`);
}

/** Like {@link getMember}, but null when no member has the user name. */
export async function findMember(userName: string): Promise<MemberState | null> {
  const response = await fetch(`${testingBaseUrl}/member?${query({ userName })}`);

  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(
      `Privacy testing tool '/member' failed. ${response.status} ${await response.text()}`,
    );
  }

  return (await response.json()) as MemberState;
}

/** Runs a scheduled task inline by configuration code name (or registered identifier). */
export async function runScheduledTask(codeName: string): Promise<RunTaskResult> {
  const result = await send<RunTaskResult>("/run-task", {
    method: "POST",
    body: JSON.stringify({ codeName }),
  });

  if (!result.succeeded) {
    throw new Error(
      `Scheduled task '${codeName}' reported a failure: ${result.message}`,
    );
  }

  return result;
}

/** Seeds one contact of each kind (anonymous, email, subscriber, member). */
export function seedContacts(
  options: SeedContactsOptions = {},
): Promise<SeededContact[]> {
  return send("/seed-contacts", {
    method: "POST",
    body: JSON.stringify(options),
  });
}

/**
 * Creates or removes the test-only Tag Manager snippet that records `dataLayer` pushes into
 * `window.__gtmStub` (read it with {@link readGtmStub}). It's global to the site, so disable it in
 * `afterAll` and run the specs that use it serially.
 */
export async function setTagManagerStub(enabled: boolean): Promise<void> {
  await send("/tag-manager-snippet", {
    method: "POST",
    body: JSON.stringify({ enabled }),
  });
}

/**
 * Every `dataLayer` entry the GTM stub saw on the current page. `gtag()` calls appear as arrays,
 * e.g. `['consent', 'default', { analytics_storage: 'denied', ... }]`. Specs that don't need the
 * snippet order read the page's own `dataLayer` with {@link readDataLayerConsent} instead.
 */
export async function readGtmStub(page: {
  evaluate<R>(fn: () => R): Promise<R>;
}): Promise<unknown[]> {
  return page.evaluate(
    () => (window as unknown as { __gtmStub?: unknown[] }).__gtmStub ?? [],
  );
}

/**
 * The `gtag('consent', <command>, …)` parameter objects the stub recorded, in order. The stub
 * records `gtag()` `arguments` objects as arrays.
 */
export function consentCommands(
  entries: unknown[],
  command: "default" | "update",
): Record<string, string>[] {
  return entries
    .filter(
      (entry): entry is [string, string, Record<string, string>] =>
        Array.isArray(entry) && entry[0] === "consent" && entry[1] === command,
    )
    .map((entry) => entry[2]);
}

/** How many `dataLayer` entries already existed when the GTM stub snippet ran. */
export async function readGtmStubLoadedWith(page: {
  evaluate<R>(fn: () => R): Promise<R>;
}): Promise<number | undefined> {
  return page.evaluate(
    () =>
      (window as unknown as { __gtmStubLoadedWith?: number }).__gtmStubLoadedWith,
  );
}

/** Google Consent Mode parameters the portal sets. The `ad_*` types are always denied. */
export const consentModeDenied = {
  analytics_storage: "denied",
  ad_storage: "denied",
  ad_user_data: "denied",
  ad_personalization: "denied",
} as const;

export const consentModeAnalyticsGranted = {
  ...consentModeDenied,
  analytics_storage: "granted",
} as const;

export type DataLayerConsentCommand = {
  command: string;
  params: Record<string, string>;
  /**
   * The entry is a real `arguments` object, as `gtag()` pushes. GTM runs only those as commands;
   * plain arrays or objects are ignored.
   */
  isGtagArguments: boolean;
};

/**
 * Every `consent` command in the page's own `window.dataLayer`, in order. Reads this page only, so
 * specs run in parallel safely (no Tag Manager stub needed).
 */
export async function readDataLayerConsent(page: {
  evaluate<R>(fn: () => R): Promise<R>;
}): Promise<DataLayerConsentCommand[]> {
  return page.evaluate(() => {
    const entries =
      (window as unknown as { dataLayer?: unknown[] }).dataLayer ?? [];
    const commands: {
      command: string;
      params: Record<string, string>;
      isGtagArguments: boolean;
    }[] = [];

    for (const entry of entries) {
      const item = entry as Record<number, unknown> | null;

      if (item === null || typeof item !== "object" || item[0] !== "consent") {
        continue;
      }

      commands.push({
        command: String(item[1]),
        params: JSON.parse(JSON.stringify(item[2] ?? {})) as Record<
          string,
          string
        >,
        isGtagArguments:
          Object.prototype.toString.call(entry) === "[object Arguments]",
      });
    }

    return commands;
  });
}

/** The consent state after every command in order, as GTM applies them. */
export function effectiveConsent(
  commands: DataLayerConsentCommand[],
): Record<string, string> {
  return commands.reduce<Record<string, string>>(
    (state, { params }) => ({ ...state, ...params }),
    {},
  );
}

/**
 * Switches read-only mode for the whole app process. Every concurrent spec is affected, so use it
 * only from a serial spec and turn it off in a `finally`.
 */
export async function setReadOnlyMode(enabled: boolean): Promise<boolean> {
  const result = await send<{ enabled: boolean }>("/read-only", {
    method: "POST",
    body: JSON.stringify({ enabled }),
  });

  return result.enabled;
}

export type ConsentTexts = {
  name: string;
  displayName: string;
  /** HTML, as stored in the admin. */
  shortText: string;
  fullText: string;
  hash: string;
};

/** A consent's admin texts, to compare with what a page renders. */
export function getConsent(name: string): Promise<ConsentTexts> {
  return send(`/consent?${query({ name })}`);
}

/** Scheduled task configuration code name. */
export const emailMarketingReconciliationTask =
  "EmailMarketingAgreementReconciliation";

/**
 * The signed unsubscribe link a newsletter email would carry for the address, made by Xperience's
 * own generator. Absolute, on the portal host.
 */
export async function getNewsletterUnsubscribeUrl(email: string): Promise<string> {
  const result = await send<{ url: string }>("/newsletter-unsubscribe-url", {
    method: "POST",
    body: JSON.stringify({ email }),
  });

  return new URL(result.url, appBaseUrl).toString();
}

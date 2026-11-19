import { expect, type BrowserContext, type Cookie } from "@playwright/test";
import { appBaseUrl } from "../config.js";

/** Cookie names the privacy specs read or seed. */
export const cookieNames = {
  /** Xperience cookie level. 0 = Essential, 1000 = All (contact tracking). */
  cookieLevel: "CMSCookieLevel",
  /** Xperience contact cookie. Must not exist below `CookieLevel.All`. */
  currentContact: "CurrentContact",
  /** Set by the activity logger when it logs a landing page. */
  landingPageLoaded: "CMSLandingPageLoaded",
  /** Consent cookie v2: `v2|a=<0|1>|t=<0|1>|<unix seconds>`. */
  consent: "kcp.cookieconsent",
  /** Pre-v2 consent cookies. Their presence must reset the visitor to Essential. */
  legacyConsentLevel: "kenticocommunityportal.cookieconsentlevel",
  legacyLevelSelection: "kenticocommunityportal.cookielevelselection",
} as const;

export const cookieLevels = {
  essential: 0,
  all: 1000,
} as const;

/**
 * Cookies that only exist when the visitor is tracked: the Xperience contact and activity cookies
 * and Google Analytics/Ads cookies.
 */
export const trackingCookiePatterns: readonly RegExp[] = [
  /^CurrentContact$/,
  /^CMSLandingPageLoaded$/,
  /^_ga($|_)/,
  /^_gid$/,
  /^_gcl_/,
];

export async function getCookie(
  context: BrowserContext,
  name: string,
): Promise<Cookie | undefined> {
  const cookies = await context.cookies(appBaseUrl);

  return cookies.find((cookie) => cookie.name === name);
}

export async function getCookieValue(
  context: BrowserContext,
  name: string,
): Promise<string | undefined> {
  return (await getCookie(context, name))?.value;
}

/** The Xperience cookie level, or undefined when the cookie isn't set. */
export async function getCookieLevel(
  context: BrowserContext,
): Promise<number | undefined> {
  const value = await getCookieValue(context, cookieNames.cookieLevel);

  return value === undefined ? undefined : Number.parseInt(value, 10);
}

/** Clears the named cookies, or every cookie when no names are given. */
export async function clearCookies(
  context: BrowserContext,
  names?: readonly string[],
): Promise<void> {
  if (!names) {
    await context.clearCookies();

    return;
  }

  for (const name of names) {
    await context.clearCookies({ name });
  }
}

/** Seeds cookies on the portal host, e.g. pre-v2 consent cookies or a stale `CurrentContact`. */
export async function setCookies(
  context: BrowserContext,
  cookies: Record<string, string>,
): Promise<void> {
  await context.addCookies(
    Object.entries(cookies).map(([name, value]) => ({
      name,
      value,
      url: appBaseUrl,
    })),
  );
}

export async function getTrackingCookies(
  context: BrowserContext,
): Promise<Cookie[]> {
  const cookies = await context.cookies();

  return cookies.filter((cookie) =>
    trackingCookiePatterns.some((pattern) => pattern.test(cookie.name)),
  );
}

export async function expectNoTrackingCookies(
  context: BrowserContext,
): Promise<void> {
  const names = (await getTrackingCookies(context)).map(
    (cookie) => `${cookie.name} (${cookie.domain})`,
  );

  expect(names, "tracking cookies present").toEqual([]);
}

export type ConsentCookie = {
  version: string;
  analytics: boolean;
  tracking: boolean;
  savedAt: Date;
  /** `|ro=1`: tracking was requested in read-only mode. Treated as no choice once read-only ends. */
  provisional: boolean;
  /** `|rv=<contact GUID>`: tracking withdrawn in read-only mode; the agreement is revoked afterwards. */
  pendingRevoke: string | undefined;
};

/**
 * Parses the v2 consent cookie (`v2|a=1|t=0|1760486400`, optionally `|ro=1` or `|rv=<guid>`). Returns undefined for a missing or
 * malformed value, so a spec can assert "no valid v2 cookie" without try/catch.
 */
export function parseConsentCookie(
  value: string | undefined,
): ConsentCookie | undefined {
  if (!value) {
    return undefined;
  }

  const [version, analytics, tracking, savedAt, marker, ...rest] =
    decodeURIComponent(value).split("|");
  const flag = (part: string | undefined, key: string) =>
    part === `${key}=1` ? true : part === `${key}=0` ? false : undefined;

  const a = flag(analytics, "a");
  const t = flag(tracking, "t");
  const seconds = Number.parseInt(savedAt ?? "", 10);

  if (
    version !== "v2" ||
    a === undefined ||
    t === undefined ||
    Number.isNaN(seconds) ||
    (marker !== undefined && marker !== "ro=1" && !marker.startsWith("rv=")) ||
    rest.length > 0
  ) {
    return undefined;
  }

  return {
    version,
    analytics: a,
    tracking: t,
    savedAt: new Date(seconds * 1000),
    provisional: marker === "ro=1",
    pendingRevoke: marker?.startsWith("rv=") ? marker.slice(3) : undefined,
  };
}

export async function getConsentCookie(
  context: BrowserContext,
): Promise<ConsentCookie | undefined> {
  return parseConsentCookie(
    await getCookieValue(context, cookieNames.consent),
  );
}

/**
 * The contact GUID from the `CurrentContact` cookie. Xperience stores `<guid>|<flags>` (e.g.
 * `<guid>|secure`), URL-encoded.
 */
export async function getCurrentContactGuid(
  context: BrowserContext,
): Promise<string | undefined> {
  const value = await getCookieValue(context, cookieNames.currentContact);

  return value ? decodeURIComponent(value).split("|")[0] : undefined;
}

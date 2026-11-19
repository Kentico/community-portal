import * as cheerio from "cheerio";
import type { VirtualEmail } from "./virtualEmail.js";

/*
 * Xperience email tracking endpoints (`UseEmailStatisticsLogging`). Every email from an email
 * channel gets the open pixel and redirect links: they feed the anonymous per-email statistics.
 * Personal tracking - email activities on the recipient's contact - is what
 * `IEmailActivityTrackingEvaluator` decides. When it allows tracking, each link carries the
 * recipient's `contactGuid`; when it doesn't, the links have no contact identifier.
 */
const clickedLinkPath = "/kentico.emails/emailclickedlink";
const openPixelPath = "/kentico.emails/openemail.gif";

export type EmailTracking = {
  /** Click-tracking redirect links (`/Kentico.Emails/EmailClickedLink`). */
  trackedLinks: string[];
  /** The open-tracking pixel (`/Kentico.Emails/OpenEmail.gif`). */
  openPixels: string[];
  /** Contact GUIDs the tracking URLs identify the recipient by. Empty = not tracked personally. */
  contactGuids: string[];
};

export function getEmailTracking(email: VirtualEmail): EmailTracking {
  const $ = cheerio.load(email.virtualEmailBodyHTML ?? "");
  const urls = (selector: string, attribute: string) =>
    $(selector)
      .map((_, element) => $(element).attr(attribute))
      .get()
      .filter((url): url is string => !!url);

  const trackedLinks = urls("a[href]", "href").filter((url) =>
    pathIs(url, clickedLinkPath),
  );
  const openPixels = urls("img[src]", "src").filter((url) =>
    pathIs(url, openPixelPath),
  );

  const contactGuids = [
    ...new Set(
      [...trackedLinks, ...openPixels]
        .map((url) => new URL(url).searchParams.get("contactGuid"))
        .filter((guid): guid is string => !!guid),
    ),
  ];

  return { trackedLinks, openPixels, contactGuids };
}

function pathIs(url: string, path: string): boolean {
  try {
    return new URL(url).pathname.toLowerCase() === path;
  } catch {
    return false;
  }
}

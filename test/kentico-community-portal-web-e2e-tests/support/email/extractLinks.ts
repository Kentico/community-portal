import * as cheerio from "cheerio";
import type { VirtualEmail } from "./virtualEmail.js";

const confirmationSelectors = [
  "a[data-confirmation-url]",
  'a[href*="confirm-email"]',
  'a[href*="confirm"]',
];

const recoverySelectors = [
  "a[data-confirmation-url]",
  'a[href*="reset-password"]',
  'a[href*="resetpassword"]',
  'a[href*="account-recovery"]',
];

export function extractConfirmationUrl(email: VirtualEmail): string {
  const html = email.virtualEmailBodyHTML;

  if (html) {
    const $ = cheerio.load(html);

    for (const selector of confirmationSelectors) {
      const href = $(selector).first().attr("href");

      if (href) {
        return href;
      }
    }
  }

  const fallbackHref = findUrlInText(html, ["confirm-email", "confirm"]);

  if (fallbackHref) {
    return fallbackHref;
  }

  throw new Error(
    "Could not extract a confirmation URL from the virtual email payload.",
  );
}

export function extractRecoveryUrl(email: VirtualEmail): string {
  const html = email.virtualEmailBodyHTML;

  if (html) {
    const $ = cheerio.load(html);

    for (const selector of recoverySelectors) {
      const href = $(selector).first().attr("href");

      if (href) {
        return href;
      }
    }
  }

  const fallbackHref = findUrlInText(html, [
    "reset-password",
    "resetpassword",
    "account-recovery",
  ]);

  if (fallbackHref) {
    return fallbackHref;
  }

  throw new Error(
    "Could not extract a recovery URL from the virtual email payload.",
  );
}

function findUrlInText(text: string, fragments: string[]): string {
  const normalizedText = text.toLowerCase();

  if (!fragments.some((fragment) => normalizedText.includes(fragment))) {
    return "";
  }

  const match = text.match(/https?:\/\/[^\s"'<>]+/i);

  return match ? match[0] : "";
}

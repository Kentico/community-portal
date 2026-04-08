import * as cheerio from "cheerio";

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

export function extractConfirmationUrl(email: unknown): string {
  const html = extractEmailHtml(email);

  if (html) {
    const $ = cheerio.load(html);

    for (const selector of confirmationSelectors) {
      const href = $(selector).first().attr("href");

      if (href) {
        return href;
      }
    }
  }

  const fallbackHref = findUrlInStrings(email, ["confirm-email", "confirm"]);

  if (fallbackHref) {
    return fallbackHref;
  }

  throw new Error(
    "Could not extract a confirmation URL from the virtual email payload.",
  );
}

export function extractRecoveryUrl(email: unknown): string {
  const html = extractEmailHtml(email);

  if (html) {
    const $ = cheerio.load(html);

    for (const selector of recoverySelectors) {
      const href = $(selector).first().attr("href");

      if (href) {
        return href;
      }
    }
  }

  const fallbackHref = findUrlInStrings(email, [
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

function extractEmailHtml(value: unknown): string {
  const stringValues = collectStringValues(value);
  const htmlCandidate = stringValues.find((candidate) =>
    /<a\b|<html\b|<body\b/i.test(candidate),
  );

  return htmlCandidate ?? "";
}

function findUrlInStrings(value: unknown, fragments: string[]): string {
  const stringValues = collectStringValues(value);

  for (const candidate of stringValues) {
    const normalizedCandidate = candidate.toLowerCase();

    if (!fragments.some((fragment) => normalizedCandidate.includes(fragment))) {
      continue;
    }

    const match = candidate.match(/https?:\/\/[^\s"'<>]+/i);

    if (match) {
      return match[0];
    }
  }

  return "";
}

function collectStringValues(value: unknown): string[] {
  const strings: string[] = [];

  visit(value, strings);

  return strings;
}

function visit(value: unknown, strings: string[]): void {
  if (typeof value === "string") {
    strings.push(value);
    return;
  }

  if (Array.isArray(value)) {
    for (const entry of value) {
      visit(entry, strings);
    }

    return;
  }

  if (value && typeof value === "object") {
    for (const entry of Object.values(value as Record<string, unknown>)) {
      visit(entry, strings);
    }
  }
}

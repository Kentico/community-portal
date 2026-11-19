import { readFileSync } from "node:fs";

/*
 * The Terms of Use version comes from the app's own setting (TermsOptions:CurrentVersion), so a
 * version change needs no spec edits. /terms shows it through the {{TermsVersion}} placeholder.
 */
const appSettings = JSON.parse(
  readFileSync(
    new URL("../../../../src/Kentico.Community.Portal.Web/appsettings.json", import.meta.url),
    "utf8",
  ),
) as { TermsOptions: { CurrentVersion: string } };

export const termsVersion = appSettings.TermsOptions.CurrentVersion;

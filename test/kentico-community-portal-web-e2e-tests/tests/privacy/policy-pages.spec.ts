import { expect, test } from "@playwright/test";
import { termsVersion } from "../../support/membership/terms.js";

/*
 * Policy pages and footer links.
 * - Footer links resolve.
 * - /terms, /privacy and /cookies-policy render their headings.
 * - The cookie table lists only portal cookies (no kentico.com-only Clarity, Facebook or LinkedIn rows).
 * - /privacy doesn't mention server logs.
 */


const footerLinks = [
  { name: "Privacy", href: "/privacy" },
  { name: "Terms of Use", href: "/terms" },
  { name: "Cookie policy", href: "/cookies-policy" },
  { name: "Cookie settings", href: "/cookies-policy#cookie-settings" },
];

/** Cookies kentico.com sets that the portal doesn't (Clarity, Facebook, LinkedIn and other ad cookies). */
const kenticoComOnlyCookies =
  /_clck|_clsk|_cltk|clarity|_fbp|facebook|linkedin|bcookie|bscookie|lidc|_lfa|_uet|_gcl_au|MUID/i;

test.describe("policy pages", () => {
  test("footer links resolve", async ({ page, request }) => {
    await page.goto("/");

    const footer = page.locator("footer#footer");

    for (const { name, href } of footerLinks) {
      const link = footer.getByRole("link", { name, exact: true });
      await expect(link, name).toHaveAttribute("href", href);

      const response = await request.get(href);
      expect(response.status(), href).toBe(200);
    }

    await expect(
      footer.getByRole("link", { name: "Cookies policy", exact: true }),
    ).toHaveCount(0);
  });

  test("cookie settings link opens the settings widget", async ({ page }) => {
    await page.goto("/cookies-policy#cookie-settings");

    await expect(page.locator("#cookie-settings")).toBeVisible();
  });

  test("/terms renders its heading and version", async ({ page }) => {
    await page.goto("/terms");

    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Kentico Community Terms of Use",
      }),
    ).toBeVisible();
    await expect(page.locator("main")).toContainText(`Version ${termsVersion}`);
  });

  test("/privacy renders its heading", async ({ page }) => {
    await page.goto("/privacy");

    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Privacy at Kentico Community",
      }),
    ).toBeVisible();
    await expect(page.locator("#website-tracking")).toBeAttached();
    await expect(page.locator("#newsletter")).toBeAttached();
    await expect(page.locator("#retention")).toBeAttached();

    // Server logs aren't described on /privacy
    await expect(page.locator("main")).not.toContainText(/server logs/i);
  });

  test("/cookies-policy lists only portal cookies", async ({ page }) => {
    await page.goto("/cookies-policy");

    await expect(
      page.getByRole("heading", {
        level: 1,
        name: "Cookies on Kentico Community",
      }),
    ).toBeVisible();

    const rows = page.locator("main table tbody tr");
    await expect(rows.first()).toBeVisible();

    const names = await rows.locator("td:first-child").allInnerTexts();
    expect(names.join("\n")).toContain("kcp.cookieconsent");
    expect(names.join("\n")).toContain("CurrentContact");

    for (const text of await rows.allInnerTexts()) {
      expect(text).not.toMatch(kenticoComOnlyCookies);
    }
  });
});

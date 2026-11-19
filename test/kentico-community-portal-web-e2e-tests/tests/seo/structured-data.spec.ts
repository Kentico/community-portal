import { type APIRequestContext, expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * Structured data (JSON-LD).
 * - Home page serves exactly one valid Organization block.
 * - Its logo is absolute and is the logo the header shows.
 * - Other pages serve no Organization.
 * - Editorial values are not asserted.
 */

async function jsonLd(request: APIRequestContext, path: string) {
  const response = await request.get(path);
  expect(response.status(), path).toBe(200);

  const $ = cheerio.load(await response.text());
  const blocks = $('script[type="application/ld+json"]')
    .toArray()
    .map((el) => JSON.parse($(el).text()) as Record<string, unknown>);

  return { $, blocks };
}

test.describe("structured data", () => {
  test("home page serves a valid Organization", async ({ request }) => {
    const { $, blocks } = await jsonLd(request, "/");
    expect(blocks).toHaveLength(1);

    const org = blocks[0]!;
    expect(org["@context"]).toBe("https://schema.org");
    expect(org["@type"]).toBe("Organization");
    expect(org.name).toBeTruthy();
    expect(URL.canParse(String(org.url)), "url").toBe(true);

    const logo = String(org.logo);
    expect(URL.canParse(logo), `logo "${logo}"`).toBe(true);
    expect(new URL(logo).protocol).toMatch(/^https?:$/);

    const headerLogo = $("#header .navbar-brand img").attr("src");
    expect(new URL(logo).pathname).toBe(headerLogo);
  });

  test("other pages serve no Organization", async ({ request }) => {
    const { blocks } = await jsonLd(request, "/blog");

    expect(blocks.filter((b) => b["@type"] === "Organization")).toHaveLength(0);
  });
});

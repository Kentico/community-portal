import { expect, test } from "@playwright/test";

/*
 * robots.txt
 * - Served as text.
 * - Declares the sitemap as an absolute URL that resolves to XML.
 * - Editorial body (User-agent groups, Content-Signal) is not asserted: it is editor content.
 */

test.describe("robots.txt", () => {
  test("declares a reachable sitemap", async ({ request }) => {
    const response = await request.get("/robots.txt");

    expect(response.status()).toBe(200);
    expect(response.headers()["content-type"]).toContain("text/plain");

    const sitemapLines = (await response.text())
      .split(/\r?\n/)
      .filter((line) => /^sitemap:/i.test(line.trim()));

    expect(sitemapLines).toHaveLength(1);

    const sitemapUrl = new URL(sitemapLines[0].replace(/^\s*sitemap:\s*/i, ""));

    expect(sitemapUrl.protocol).toMatch(/^https?:$/);
    expect(sitemapUrl.pathname).toBe("/sitemap.xml");

    const sitemap = await request.get(sitemapUrl.href);

    expect(sitemap.status()).toBe(200);
    expect(sitemap.headers()["content-type"]).toMatch(/xml/);
    expect(await sitemap.text()).toContain("<urlset");
  });
});

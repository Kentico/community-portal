import { expect, test, type APIRequestContext } from "@playwright/test";

/*
 * Covers ticket #340: /llms.txt indexes developer resources for coding agents (https://llmstxt.org).
 * - Served as text, well-formed: H1, blockquote summary, H2 sections of markdown link lists.
 * - Every link is absolute; site links point at indexable pages (each is in the sitemap).
 * - Developer entry points are present: Q&A and the extensions hub.
 * - Utility pages are absent.
 * - Editorial values (titles, descriptions) are not asserted.
 */

const linkLine = /^- \[(?:[^\]\\]|\\.)+\]\(([^)\s]+)\)(?:: .+)?$/;

async function getLlmsTxt(request: APIRequestContext) {
  const response = await request.get("/llms.txt");
  expect(response.status()).toBe(200);
  expect(response.headers()["content-type"]).toMatch(/^text\/(plain|markdown)/);

  const text = await response.text();
  const lines = text.split(/\r?\n/);
  const urls = lines.flatMap((line) => {
    const match = linkLine.exec(line);
    return match ? [new URL(match[1]!)] : [];
  });

  return { text, lines, urls };
}

async function getSitemapPaths(request: APIRequestContext) {
  const response = await request.get("/sitemap.xml");
  expect(response.status()).toBe(200);

  return new Set(
    [...(await response.text()).matchAll(/<loc>([^<]+)<\/loc>/g)].map(([, loc]) =>
      normalizePath(new URL(loc!, "http://localhost").pathname),
    ),
  );
}

function normalizePath(path: string) {
  return path.toLowerCase().replace(/(.)\/$/, "$1");
}

test.describe("llms.txt", () => {
  test("is well-formed llms.txt markdown", async ({ request }) => {
    const { lines, urls } = await getLlmsTxt(request);

    expect(lines[0], "H1 site name").toMatch(/^# \S/);
    expect(lines.filter((l) => l.startsWith("# ")), "single H1").toHaveLength(1);
    expect(lines.find((l) => l.trim() !== "" && !l.startsWith("# ")), "blockquote summary").toMatch(/^> \S/);

    const sections = lines.filter((l) => l.startsWith("## "));
    expect(sections.length, "H2 sections").toBeGreaterThan(0);

    // After the first section, content is only list items of links, or blank lines
    const body = lines.slice(lines.indexOf(sections[0]!));
    for (const line of body) {
      if (line === "" || line.startsWith("## ")) {
        continue;
      }
      expect(line, "section line is a markdown link list item").toMatch(linkLine);
    }

    expect(urls.length).toBeGreaterThan(0);
    for (const url of urls) {
      expect(url.protocol, url.href).toMatch(/^https?:$/);
    }
  });

  test("links Q&A and the extensions hub", async ({ request, baseURL }) => {
    const { urls } = await getLlmsTxt(request);
    const origin = new URL(baseURL!).host;
    const sitePaths = urls.filter((u) => u.host === origin).map((u) => normalizePath(u.pathname));

    expect(sitePaths).toEqual(expect.arrayContaining(["/q-and-a", "/resource-hub/extensions"]));
  });

  test("site links are indexable pages", async ({ request, baseURL }) => {
    const { urls } = await getLlmsTxt(request);
    const sitemapPaths = await getSitemapPaths(request);
    const origin = new URL(baseURL!).host;

    for (const url of urls.filter((u) => u.host === origin)) {
      expect(sitemapPaths.has(normalizePath(url.pathname)), `${url.pathname} is in the sitemap`).toBe(true);
    }
  });

  for (const path of ["/login", "/register", "/account-recovery", "/newsletter-signup", "/q-and-a/new-question", "/update-in-progress"]) {
    test(`excludes utility page ${path}`, async ({ request }) => {
      const { urls } = await getLlmsTxt(request);

      expect(urls.map((u) => normalizePath(u.pathname))).not.toContain(path);
    });
  }
});

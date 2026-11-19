import { type APIRequestContext, expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * Canonical URL.
 * - Every indexable page type serves exactly one <link rel="canonical">.
 * - Its href is absolute, with no fragment and no query string except a listing's page number.
 * - Error responses serve none.
 */

/** One URL per content-tree page type. */
const pagePaths = [
  "/",
  "/blog",
  "/blog/Xperience-by-Kentico-Refresh-December-12-2024",
  "/q-and-a",
  "/community",
  "/resource-hub",
  "/support",
  "/privacy",
];

async function load(request: APIRequestContext, path: string) {
  const response = await request.get(path);
  expect(response.status(), path).toBe(200);

  return cheerio.load(await response.text());
}

function expectAbsoluteCanonical($: cheerio.CheerioAPI, path: string) {
  const canonical = $('head link[rel="canonical"]');
  expect(canonical, path).toHaveLength(1);

  const href = canonical.attr("href") ?? "";
  expect(URL.canParse(href), `${path} -> "${href}"`).toBe(true);

  const url = new URL(href);
  expect(url.protocol, href).toMatch(/^https?:$/);
  expect(url.hash, href).toBe("");

  return url;
}

/** Path of the first Q&A question listed in the sitemap. */
async function questionPath(request: APIRequestContext): Promise<string> {
  const response = await request.get("/sitemap.xml");
  expect(response.status()).toBe(200);

  const path = [...(await response.text()).matchAll(/<loc>([^<]+)<\/loc>/g)]
    .map(([, loc]) => new URL(loc!, "http://localhost").pathname)
    .find((p) => /^\/q-and-a\/q\/.+/i.test(p));
  expect(path, "Q&A question in sitemap").toBeTruthy();

  return path!;
}

/** Path of the first link carrying the given test-id hook. */
function linkedPath($: cheerio.CheerioAPI, testId: string): string {
  const href = $(`a[test-id="${testId}"]`).first().attr("href");
  expect(href, testId).toBeTruthy();

  return href!;
}

test.describe("canonical URL", () => {
  for (const path of pagePaths) {
    test(`${path} serves an absolute canonical URL`, async ({ request }) => {
      const url = expectAbsoluteCanonical(await load(request, path), path);
      expect(url.search).toBe("");
    });
  }

  test("paginated listing is canonical to its own page", async ({ request }) => {
    const first = expectAbsoluteCanonical(
      await load(request, "/blog?page=1&utm_source=test"),
      "/blog?page=1",
    );
    expect(first.search).toBe("");

    const second = expectAbsoluteCanonical(
      await load(request, "/blog?page=2&utm_source=test"),
      "/blog?page=2",
    );
    expect(second.search).toBe("?page=2");
  });

  test("Q&A question serves an absolute canonical URL", async ({ request }) => {
    const path = await questionPath(request);

    expectAbsoluteCanonical(await load(request, path), path);
  });

  test("member profile canonical ignores the query string", async ({
    request,
  }) => {
    const path = linkedPath(
      await load(request, await questionPath(request)),
      "memberProfileLink",
    );

    const url = expectAbsoluteCanonical(
      await load(request, `${path}?utm_source=test`),
      path,
    );
    expect(url.pathname).toBe(path);
    expect(url.search).toBe("");
  });

  test("not found response serves no canonical URL", async ({ request }) => {
    const response = await request.get("/seo-canonical-missing-page");
    expect(response.status()).toBe(404);

    const $ = cheerio.load(await response.text());
    expect($('link[rel="canonical"]')).toHaveLength(0);
  });
});

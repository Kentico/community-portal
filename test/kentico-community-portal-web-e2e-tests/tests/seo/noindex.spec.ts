import { type APIRequestContext, expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * Covers ticket #322: routes that hold no answer serve noindex.
 * - Error responses (incl. a direct GET of /error/404): noindex, no canonical.
 * - Member profiles: noindex, absent from the sitemap.
 * - Auto-created blog discussions: noindex, absent from the sitemap.
 * - Real content routes: no noindex.
 */

async function sitemapPaths(request: APIRequestContext): Promise<string[]> {
  const response = await request.get("/sitemap.xml");
  expect(response.status()).toBe(200);

  return [...(await response.text()).matchAll(/<loc>([^<]+)<\/loc>/g)].map(
    ([, loc]) => new URL(loc!, "http://localhost").pathname,
  );
}

async function load(request: APIRequestContext, path: string, status = 200) {
  const response = await request.get(path);
  expect(response.status(), path).toBe(status);

  return cheerio.load(await response.text());
}

function robots($: cheerio.CheerioAPI): string {
  return ($('head meta[name="robots"]').attr("content") ?? "").toLowerCase();
}

function expectNoIndex($: cheerio.CheerioAPI, path: string) {
  expect(robots($), path).toContain("noindex");
  expect(robots($), path).not.toContain("noarchive");
}

function linkedPath($: cheerio.CheerioAPI, testId: string): string | undefined {
  return $(`a[test-id="${testId}"]`).first().attr("href");
}

/** Path of the first Q&A question in the sitemap; blog discussions are not listed there. */
async function questionPath(request: APIRequestContext): Promise<string> {
  const path = (await sitemapPaths(request)).find((p) => /^\/q-and-a\/q\/.+/i.test(p));
  expect(path, "Q&A question in sitemap").toBeTruthy();

  return path!;
}

/** Discussion linked from the first blog post in the sitemap that has one. */
async function blogDiscussionPath(request: APIRequestContext): Promise<string> {
  const posts = (await sitemapPaths(request)).filter((p) => /^\/blog\/.+/i.test(p));

  for (const post of posts) {
    const href = linkedPath(await load(request, post), "blogDiscussionLink");
    if (href) {
      return new URL(href, "http://localhost").pathname;
    }
  }

  throw new Error("No blog post links to a discussion");
}

test.describe("noindex", () => {
  test("not found response serves noindex", async ({ request }) => {
    const $ = await load(request, "/seo-noindex-missing-page", 404);

    expectNoIndex($, "404");
    expect($('link[rel="canonical"]')).toHaveLength(0);
  });

  test("direct GET of /error/404 serves noindex", async ({ request }) => {
    const $ = await load(request, "/error/404", 404);

    expectNoIndex($, "/error/404");
    expect($('link[rel="canonical"]')).toHaveLength(0);
  });

  test("member profile serves noindex and is absent from the sitemap", async ({ request }) => {
    const path = linkedPath(await load(request, await questionPath(request)), "memberProfileLink");
    expect(path, "memberProfileLink").toBeTruthy();

    expectNoIndex(await load(request, path!), path!);
    expect(await sitemapPaths(request)).not.toContainEqual(expect.stringMatching(/^\/member\//i));
  });

  test("blog discussion serves noindex and is absent from the sitemap", async ({ request }) => {
    const path = await blogDiscussionPath(request);

    expectNoIndex(await load(request, path), path);
    expect((await sitemapPaths(request)).map((p) => p.toLowerCase())).not.toContain(
      path.toLowerCase(),
    );
  });

  test("content routes do not serve noindex", async ({ request }) => {
    const sitemap = await sitemapPaths(request);
    const blogPost = sitemap.find((p) => /^\/blog\/.+/i.test(p));
    expect(blogPost, "blog post in sitemap").toBeTruthy();

    for (const path of ["/", "/blog", blogPost!, await questionPath(request)]) {
      expect(robots(await load(request, path)), path).not.toContain("noindex");
    }
  });
});

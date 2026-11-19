import { type APIRequestContext, expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * Head tags.
 * - og:type matches the page type (article, profile, website).
 * - Twitter Card tags on every page.
 * - RSS autodiscovery links resolve to RSS feeds.
 * - Blog posts carry article publication and author metadata and a resolvable social image.
 * - Q&A questions carry a title and description for the preview card.
 */

const blogPostPath = "/blog/Xperience-by-Kentico-Refresh-December-12-2024";

async function load(request: APIRequestContext, path: string) {
  const response = await request.get(path);
  expect(response.status(), path).toBe(200);

  return cheerio.load(await response.text());
}

const meta = ($: cheerio.CheerioAPI, key: string) =>
  $(`head meta[property="${key}"], head meta[name="${key}"]`).first().attr("content") ?? "";

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

function expectTwitterCard($: cheerio.CheerioAPI, path: string) {
  expect(meta($, "twitter:card"), path).toMatch(/^summary(_large_image)?$/);
  expect(meta($, "twitter:title"), path).not.toBe("");
}

test.describe("head tags", () => {
  for (const path of ["/", "/blog", "/q-and-a"]) {
    test(`${path} is a website with a Twitter Card`, async ({ request }) => {
      const $ = await load(request, path);

      expect(meta($, "og:type")).toBe("website");
      expectTwitterCard($, path);
    });
  }

  test("blog post is an article with a resolvable image", async ({ request }) => {
    const $ = await load(request, blogPostPath);

    expect(meta($, "og:type")).toBe("article");
    expectTwitterCard($, blogPostPath);
    expect(Date.parse(meta($, "article:published_time"))).not.toBeNaN();
    expect(meta($, "article:author")).not.toBe("");
    expect(meta($, "author")).not.toBe("");

    const image = await request.get(meta($, "og:image"));
    expect(image.status()).toBe(200);
    expect(image.headers()["content-type"]).toMatch(/^image\//);
    expect(meta($, "twitter:image")).toBe(meta($, "og:image"));
  });

  test("Q&A question has a preview card", async ({ request }) => {
    const path = await questionPath(request);
    const $ = await load(request, path);

    expect(meta($, "og:type")).toBe("article");
    expectTwitterCard($, path);
    expect(meta($, "og:title")).not.toBe("");
    expect(meta($, "og:description")).not.toBe("");
    expect(meta($, "twitter:description")).not.toBe("");
  });

  test("member profile is a profile", async ({ request }) => {
    const question = await load(request, await questionPath(request));
    const path = question('a[test-id="memberProfileLink"]').first().attr("href");
    expect(path, "memberProfileLink").toBeTruthy();

    const $ = await load(request, path!);

    expect(meta($, "og:type")).toBe("profile");
    expectTwitterCard($, path!);
  });

  test("RSS autodiscovery links resolve to feeds", async ({ request }) => {
    const $ = await load(request, "/");
    const hrefs = $('head link[rel="alternate"][type="application/rss+xml"]')
      .map((_, el) => $(el).attr("href") ?? "")
      .get();

    // Blog and Q&A
    expect(hrefs).toHaveLength(2);

    for (const href of hrefs) {
      const feed = await request.get(href);
      expect(feed.status(), href).toBe(200);
      expect(feed.headers()["content-type"], href).toMatch(/(rss|xml)/);
      expect(await feed.text(), href).toContain("<rss");
    }
  });
});

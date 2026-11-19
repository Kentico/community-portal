import { expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * BlogPosting structured data (JSON-LD).
 * - A blog post serves exactly one valid BlogPosting block.
 * - Its author is an embedded Person whose name is visible on the page.
 * - Its url is the page's canonical URL.
 * - Editorial values are not asserted.
 */

const postPath = "/blog/Xperience-by-Kentico-Refresh-December-12-2024";

test.describe("BlogPosting structured data", () => {
  test("blog post serves a valid BlogPosting", async ({ request }) => {
    const response = await request.get(postPath);
    expect(response.status(), postPath).toBe(200);

    const $ = cheerio.load(await response.text());
    const postings = $('script[type="application/ld+json"]')
      .toArray()
      .map((el) => JSON.parse($(el).text()) as Record<string, unknown>)
      .filter((b) => b["@type"] === "BlogPosting");
    expect(postings).toHaveLength(1);

    const posting = postings[0]!;
    expect(posting["@context"]).toBe("https://schema.org");
    expect(String(posting.headline ?? "").trim()).not.toBe("");

    const published = String(posting.datePublished);
    expect(Number.isNaN(Date.parse(published)), `datePublished "${published}"`).toBe(false);

    const canonical = $('head link[rel="canonical"]').attr("href");
    expect(posting.url).toBe(canonical);
    expect(posting.mainEntityOfPage).toBe(canonical);

    const author = posting.author as Record<string, unknown>;
    expect(author["@type"]).toBe("Person");
    const name = String(author.name ?? "").trim();
    expect(name).not.toBe("");

    const visibleAuthor = $('[test-id="blogPostAuthorName"]').first();
    expect(visibleAuthor, "author name hook").toHaveLength(1);
    expect(visibleAuthor.text().trim()).toBe(name);

    if (author.url !== undefined) {
      expect(new URL(String(author.url)).pathname).toBe(visibleAuthor.attr("href"));
    }
  });
});

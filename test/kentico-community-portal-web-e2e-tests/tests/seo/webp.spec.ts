import { expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * WebP images.
 * - Content images (Image widget) are served as image/webp.
 * - Includes every image variant listed in srcset.
 * - The default member avatar (static file) is served as image/webp.
 */

const postPath = "/blog/Xperience-by-Kentico-Refresh-December-12-2024";

/** src and every srcset candidate URL. Strips a leading "~" app-root marker. */
function imageURLs(src: string, srcset: string): string[] {
  const candidates = srcset
    .split(",")
    .map((c) => c.trim().split(/\s+/)[0] ?? "")
    .filter((c) => c !== "");

  return [...new Set([src, ...candidates])].map((u) => u.replace(/^~/, ""));
}

test.describe("WebP images", () => {
  test("blog post content images and variants are served as WebP", async ({ request }) => {
    const response = await request.get(postPath);
    expect(response.status(), postPath).toBe(200);

    const $ = cheerio.load(await response.text());
    const images = $('img[test-id="imageWidgetImage"]').toArray();
    expect(images.length, "image widget hook").toBeGreaterThan(0);

    const urls = images.flatMap((el) => imageURLs($(el).attr("src") ?? "", $(el).attr("srcset") ?? ""));

    for (const url of urls) {
      expect(url, "image URL").not.toBe("");

      const image = await request.get(url);
      expect(image.status(), url).toBe(200);
      expect(image.headers()["content-type"], url).toMatch(/^image\/webp/);
    }
  });

  test("default member avatar is served as WebP", async ({ request }) => {
    // No member has ID 0, so the default avatar is returned
    const path = "/member/avatar/0";
    const image = await request.get(path);

    expect(image.status(), path).toBe(200);
    expect(image.headers()["content-type"], path).toMatch(/^image\/webp/);
  });
});

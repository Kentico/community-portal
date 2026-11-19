import { expect, test, type APIRequestContext } from "@playwright/test";

/*
 * Covers ticket #325: /sitemap.xml lists public content only, with honest metadata.
 * - Public pages are listed with a lastmod.
 * - Utility pages are absent ("Sitemap - exclude" set in content).
 * - No <changefreq>.
 */

async function getSitemap(request: APIRequestContext) {
  const response = await request.get("/sitemap.xml");
  expect(response.status()).toBe(200);

  const xml = await response.text();
  const paths = [...xml.matchAll(/<loc>([^<]+)<\/loc>/g)].map(([, loc]) =>
    new URL(loc!, "http://localhost").pathname.toLowerCase().replace(/(.)\/$/, "$1"),
  );

  return { xml, paths };
}

test.describe("sitemap", () => {
  test("lists public pages with a lastmod", async ({ request }) => {
    const { xml, paths } = await getSitemap(request);

    expect(paths).toEqual(expect.arrayContaining(["/blog", "/q-and-a", "/privacy"]));
    expect(xml).toMatch(/<lastmod>\d{4}-\d{2}-\d{2}/);
  });

  test("omits changefreq", async ({ request }) => {
    const { xml } = await getSitemap(request);

    expect(xml).not.toContain("<changefreq>");
  });


  for (const path of [
    "/account-recovery",
    "/reset-password",
    "/email-confirmation",
    "/subscription-confirmation",
    "/unsubscription-confirmation",
    "/login",
    "/register",
    "/newsletter-signup",
    "/q-and-a/new-question",
    "/update-in-progress",
  ]) {
    test(`excludes utility page ${path}`, async ({ request }) => {
      const { paths } = await getSitemap(request);

      expect(paths).not.toContain(path);
    });
  }
});

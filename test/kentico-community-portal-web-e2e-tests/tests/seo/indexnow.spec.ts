import { expect, test } from "@playwright/test";

/*
 * IndexNow key file
 * - /{key}.txt serves the key as text (IndexNow protocol ownership check).
 * - Any other *.txt name is not found.
 * - The key is the Development/CI test key (appsettings.CI.json, IndexNow:Key).
 *   Those environments submit nothing (empty IndexNow:Endpoint).
 */

const KEY = process.env.INDEXNOW_KEY ?? "e2e-indexnow-test-key";

test.describe("IndexNow", () => {
  test("serves the key file", async ({ request }) => {
    const response = await request.get(`/${KEY}.txt`);

    expect(response.status()).toBe(200);
    expect(response.headers()["content-type"]).toContain("text/plain");
    expect((await response.text()).trim()).toBe(KEY);
  });

  test("does not serve other key names", async ({ request }) => {
    const response = await request.get("/not-the-indexnow-key.txt");

    expect(response.status()).toBe(404);
  });
});

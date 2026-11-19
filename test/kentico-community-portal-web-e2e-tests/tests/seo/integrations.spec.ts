import { expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * Extensions and integrations hub.
 * - Serves one valid ItemList of SoftwareApplication entries.
 * - One list item per rendered entry, from the full server-rendered list.
 * - Each entry has a unique, non-empty id for #anchor links.
 * - Editorial values are not asserted.
 */

const hubPath = "/resource-hub/extensions";

test.describe("extensions hub", () => {
  test("serves an ItemList of every entry, each anchorable", async ({
    request,
  }) => {
    const response = await request.get(hubPath);
    expect(response.status(), hubPath).toBe(200);

    const $ = cheerio.load(await response.text());

    const entries = $('[data-testid="integration-entry"]').toArray();
    expect(
      entries.length,
      `No integration entries rendered on ${hubPath} - the CI data should include IntegrationContent items`,
    ).toBeGreaterThan(0);

    const ids = entries.map((el) => $(el).attr("id") ?? "");
    for (const id of ids) {
      expect(id, "entry id").not.toBe("");
    }
    expect(new Set(ids).size, "entry ids are unique").toBe(ids.length);

    const lists = $('script[type="application/ld+json"]')
      .toArray()
      .map((el) => JSON.parse($(el).text()) as Record<string, unknown>)
      .filter((b) => b["@type"] === "ItemList");
    expect(lists).toHaveLength(1);

    const list = lists[0]!;
    expect(list["@context"]).toBe("https://schema.org");

    const elements = list.itemListElement as Record<string, unknown>[];
    expect(Array.isArray(elements), "itemListElement").toBe(true);
    expect(elements.length).toBeGreaterThan(0);
    expect(elements).toHaveLength(entries.length);

    elements.forEach((element, index) => {
      expect(element["@type"]).toBe("ListItem");
      expect(element.position).toBe(index + 1);

      const app = element.item as Record<string, unknown>;
      expect(app["@type"]).toBe("SoftwareApplication");
      expect(app.name, `item ${index + 1} name`).toBeTruthy();

      const url = String(app.url);
      expect(URL.canParse(url), `item ${index + 1} url "${url}"`).toBe(true);
      expect(new URL(url).protocol).toBe("https:");
    });
  });
});

/*
 * Extensions hub search and filter (client-side).
 * - Values are read from the rendered page, not asserted editorial content.
 * - Filtering hides entries; every entry stays server-rendered (see above).
 */
test.describe("extensions hub search", () => {
  const entries = '[data-testid="integration-entry"]';

  test("search and type filter compose", async ({ page }) => {
    await page.goto(hubPath);

    const all = page.locator(entries);
    const total = await all.count();
    expect(total).toBeGreaterThan(1);

    const target = all.first();
    const id = (await target.getAttribute("id"))!;
    const type = (await target.getAttribute("data-type"))!;
    const title = (await target.locator("h3").innerText()).trim();

    // Title words in reverse order still match: terms are AND-ed, not a phrase
    const query = title.split(/\s+/).reverse().join(" ").toUpperCase();
    await page.getByTestId("integrations-search").fill(query);

    await expect(page.locator(`#${id}`)).toBeVisible();
    await expect(page.locator(`${entries}:visible`)).not.toHaveCount(0);
    await expect(page).toHaveURL(/[?&]q=/);

    const typeSelect = page.getByTestId("integrations-type");
    const otherType = await typeSelect
      .locator("option")
      .evaluateAll(
        (options, current) =>
          options
            .map((o) => (o as HTMLOptionElement).value)
            .find((v) => v !== "" && v !== current) ?? null,
        type,
      );

    if (otherType) {
      await typeSelect.selectOption(otherType);
      await expect(page.locator(`#${id}`)).toBeHidden();
      for (const el of await page.locator(`${entries}:visible`).all()) {
        await expect(el).toHaveAttribute("data-type", otherType);
      }
    }

    if (type) {
      await typeSelect.selectOption(type);
      await expect(page.locator(`#${id}`)).toBeVisible();
    }

    await page
      .getByTestId("integrations-search")
      .fill("zzzz-no-integration-matches");
    await expect(page.locator(`${entries}:visible`)).toHaveCount(0);
    await page.getByRole("button", { name: "Clear filters" }).click();
    await expect(page.locator(`${entries}:visible`)).toHaveCount(total);
  });

  test("state restores from the URL", async ({ page }) => {
    await page.goto(`${hubPath}?q=zzzz-no-integration-matches`);

    await expect(page.getByTestId("integrations-search")).toHaveValue(
      "zzzz-no-integration-matches",
    );
    await expect(page.locator(`${entries}:visible`)).toHaveCount(0);
  });

  test("an entry anchor wins over filters", async ({ page, request }) => {
    const $ = cheerio.load(await (await request.get(hubPath)).text());
    const id = $(entries).last().attr("id")!;

    await page.goto(`${hubPath}?q=zzzz-no-integration-matches#${id}`);

    const entry = page.locator(`#${id}`);
    await expect(entry).toBeVisible();
    await expect(entry).toBeInViewport();
    await expect(page.getByTestId("integrations-search")).toHaveValue("");

    // Hidden by a later search, then revealed by following its anchor
    await page
      .getByTestId("integrations-search")
      .fill("zzzz-no-integration-matches");
    await expect(entry).toBeHidden();
    await page.evaluate((hash) => (window.location.hash = hash), "");
    await page.evaluate((hash) => (window.location.hash = hash), id);
    await expect(entry).toBeVisible();
    await expect(entry).toBeInViewport();
  });

  test("entry logos load lazily", async ({ request }) => {
    const $ = cheerio.load(await (await request.get(hubPath)).text());

    for (const img of $(`${entries} img`).toArray()) {
      expect($(img).attr("loading"), $(img).attr("src")).toBe("lazy");
    }
  });
});

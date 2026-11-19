import { type APIRequestContext, expect, test } from "@playwright/test";
import * as cheerio from "cheerio";

/*
 * Q&A structured data (QAPage).
 * - A question page serves exactly one valid QAPage whose mainEntity is a Question.
 * - Each answer is an Answer with an absolute URL whose fragment is an answer element on the page.
 * - acceptedAnswer points at the answer rendered as accepted; suggestedAnswer at the others.
 * - No page serves DiscussionForumPosting or FAQPage.
 * - Editorial values are not asserted.
 */

type Node = Record<string, unknown>;

/** How many sitemap questions to scan for one with answers. */
const QUESTION_SCAN_LIMIT = 15;

async function load(request: APIRequestContext, path: string) {
  const response = await request.get(path);
  expect(response.status(), path).toBe(200);

  const $ = cheerio.load(await response.text());
  const blocks = $('script[type="application/ld+json"]')
    .toArray()
    .map((el) => JSON.parse($(el).text()) as Node);

  return { $, blocks };
}

/** Paths of Q&A questions listed in the sitemap (blog discussions are excluded from it). */
async function questionPaths(request: APIRequestContext): Promise<string[]> {
  const response = await request.get("/sitemap.xml");
  expect(response.status()).toBe(200);

  const paths = [...(await response.text()).matchAll(/<loc>([^<]+)<\/loc>/g)]
    .map(([, loc]) => new URL(loc!, "http://localhost").pathname)
    .filter((p) => /^\/q-and-a\/q\/.+/i.test(p));
  expect(paths.length, "Q&A questions in sitemap").toBeGreaterThan(0);

  return paths;
}

function qaPage(blocks: Node[], path: string): Node {
  const pages = blocks.filter((b) => b["@type"] === "QAPage");
  expect(pages, path).toHaveLength(1);

  const page = pages[0]!;
  expect(page["@context"]).toBe("https://schema.org");

  return page;
}

function expectNoForbiddenTypes(blocks: Node[], path: string) {
  const json = JSON.stringify(blocks);
  expect(json, path).not.toContain("DiscussionForumPosting");
  expect(json, path).not.toContain("FAQPage");
}

function expectPerson(value: unknown, label: string) {
  const person = value as Node;
  expect(person?.["@type"], label).toBe("Person");
  expect(person.name, label).toBeTruthy();
  if (person.url !== undefined) {
    expect(URL.canParse(String(person.url)), `${label} url`).toBe(true);
  }
}

/** Asserts an Answer node and returns the id of the element its URL points at. */
function expectAnswer(
  $: cheerio.CheerioAPI,
  value: unknown,
  label: string,
): string {
  const answer = value as Node;
  expect(answer["@type"], label).toBe("Answer");
  expect(typeof answer.dateCreated, label).toBe("string");
  expectPerson(answer.author, `${label} author`);

  const href = String(answer.url);
  expect(URL.canParse(href), `${label} url "${href}"`).toBe(true);
  const url = new URL(href);
  expect(url.protocol, href).toMatch(/^https?:$/);

  const id = url.hash.slice(1);
  expect(id, href).toBeTruthy();
  expect($(`[id="${id}"][q-and-a-answer]`), href).toHaveLength(1);

  return id;
}

test.describe("Q&A structured data", () => {
  test("question page serves a valid QAPage", async ({ request }) => {
    const [path] = await questionPaths(request);
    const { $, blocks } = await load(request, path!);

    expectNoForbiddenTypes(blocks, path!);
    const question = qaPage(blocks, path!).mainEntity as Node;

    expect(question["@type"]).toBe("Question");
    expect(question.name).toBeTruthy();
    expect(question.answerCount).toBe($("[q-and-a-answer]").length);
    expectPerson(question.author, "question author");
  });

  test("answers are marked accepted or suggested", async ({ request }) => {
    const paths = (await questionPaths(request)).slice(0, QUESTION_SCAN_LIMIT);

    let answered = false;
    for (const path of paths) {
      const { $, blocks } = await load(request, path);
      expectNoForbiddenTypes(blocks, path);

      const question = qaPage(blocks, path).mainEntity as Node;
      const answerCount = Number(question.answerCount);
      if (answerCount === 0) {
        expect(question.acceptedAnswer, path).toBeUndefined();
        expect(question.suggestedAnswer, path).toBeUndefined();
        continue;
      }
      answered = true;

      const suggested = (question.suggestedAnswer ?? []) as unknown[];
      const ids = suggested.map((a, i) =>
        expectAnswer($, a, `${path} suggestedAnswer[${i}]`),
      );
      for (const id of ids) {
        expect($(`[id="${id}"]`).attr("data-accepted"), id).toBe("false");
      }

      if (question.acceptedAnswer !== undefined) {
        const id = expectAnswer(
          $,
          question.acceptedAnswer,
          `${path} acceptedAnswer`,
        );
        expect($(`[id="${id}"]`).attr("data-accepted"), id).toBe("true");
        ids.push(id);
      } else {
        expect($('[q-and-a-answer][data-accepted="true"]'), path).toHaveLength(0);
      }

      expect(ids, path).toHaveLength(answerCount);
      expect(new Set(ids).size, path).toBe(answerCount);
    }

    test.info().annotations.push({
      type: "coverage",
      description: answered
        ? "answered question found"
        : `no answered question in first ${paths.length} sitemap questions`,
    });
  });

  test("other pages serve no QAPage", async ({ request }) => {
    for (const path of ["/", "/q-and-a"]) {
      const { blocks } = await load(request, path);

      const pages = blocks.filter((b) => b["@type"] === "QAPage");
      expect(pages, path).toHaveLength(0);
      expectNoForbiddenTypes(blocks, path);
    }
  });
});

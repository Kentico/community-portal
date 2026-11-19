import { expect, test, type Page } from "@playwright/test";
import {
  acceptAll,
  necessaryOnly,
  openSettings,
  setToggles,
} from "../../support/privacy/consent.js";
import {
  consentCommands,
  consentModeAnalyticsGranted,
  consentModeDenied,
  readGtmStub,
  readGtmStubLoadedWith,
  setTagManagerStub,
} from "../../support/privacy/state.js";

/*
 * Tag Manager consent gate: no Tag Manager snippet (prod's GTM snippet sits at HeadTop) renders or
 * loads until the visitor allows Analytics (`ConsentGatedCodeSnippetsService`). Allowing it loads
 * the snippets on the same page through Tag Manager's `updateCodeSnippets()`.
 * Uses the test-only Tag Manager stub snippet, which is site-wide, so this runs in the
 * `isolated-tests` project and always removes the stub.
 */
test.describe.configure({ mode: "serial" });

test.beforeAll(async () => {
  await setTagManagerStub(true);
});

test.afterAll(async () => {
  await setTagManagerStub(false);
});

const stubSelector = "script[data-e2e-gtm-stub]";

/** Neither the stub's element nor its global exist on the page. */
async function expectNoStub(page: Page): Promise<void> {
  await expect(page.locator(stubSelector)).toHaveCount(0);
  expect(
    await page.evaluate(
      () => (window as unknown as { __gtmStub?: unknown }).__gtmStub,
    ),
  ).toBeUndefined();
}

/** The stub's element is on the page and the stub ran. */
async function expectStub(page: Page): Promise<void> {
  await expect(page.locator(stubSelector)).toHaveCount(1);
  await expect
    .poll(() => readGtmStubLoadedWith(page), { message: "stub ran" })
    .toBeGreaterThan(0);
}

/**
 * Marks the current document, so {@link expectSameDocument} can tell the page wasn't reloaded.
 */
async function markDocument(page: Page): Promise<void> {
  await page.evaluate(() => {
    (window as unknown as { __e2eDocument?: boolean }).__e2eDocument = true;
  });
}

async function expectSameDocument(page: Page): Promise<void> {
  expect(
    await page.evaluate(
      () => (window as unknown as { __e2eDocument?: boolean }).__e2eDocument,
    ),
    "page not reloaded",
  ).toBe(true);
}

test("first visit: no Tag Manager snippet", async ({ page }) => {
  await page.goto("/");

  await expectNoStub(page);
});

test("Necessary only: no Tag Manager snippet, now or on the next page", async ({
  page,
}) => {
  await page.goto("/");
  await necessaryOnly(page);
  await page.waitForLoadState("networkidle");

  await expectNoStub(page);

  await page.goto("/blog");
  await expectNoStub(page);
});

test("Accept all: snippet loads without a reload and on the next page", async ({
  page,
}) => {
  await page.goto("/");
  await markDocument(page);

  await acceptAll(page);

  await expectStub(page);
  await expectSameDocument(page);
  // Inserted after the banner's consent update, so the stub saw default, then update
  const entries = await readGtmStub(page);
  expect(consentCommands(entries, "default")).toEqual([consentModeDenied]);
  expect(consentCommands(entries, "update")).toEqual([
    consentModeAnalyticsGranted,
  ]);

  await page.goto("/blog");
  await expectStub(page);
});

test("Analytics on in cookie settings: snippet loads without a reload", async ({
  page,
}) => {
  await openSettings(page);
  await markDocument(page);

  await setToggles(page, { analytics: true, tracking: false });

  await expectStub(page);
  await expectSameDocument(page);

  await page.goto("/");
  await expectStub(page);
});

test("withdrawing Analytics: no snippet on the next page", async ({ page }) => {
  await page.goto("/");
  await acceptAll(page);
  await expectStub(page);

  await openSettings(page);
  await setToggles(page, { analytics: false, tracking: true });

  await page.goto("/");
  await expectNoStub(page);
});

test("consent default and the saved choice run before Tag Manager HeadTop snippets", async ({
  page,
}) => {
  await page.goto("/");
  await acceptAll(page);

  await page.goto("/blog", { waitUntil: "domcontentloaded" });

  // The stub snippet is in <head>, after the portal's consent script
  const order = await page.evaluate(() => {
    const consent = document.querySelector("script[data-consent-mode]");
    const stub = document.querySelector("script[data-e2e-gtm-stub]");

    return {
      consent: !!consent,
      stub: !!stub,
      stubInHead: stub?.parentElement === document.head,
      consentFirst:
        !!consent &&
        !!stub &&
        (consent.compareDocumentPosition(stub) &
          Node.DOCUMENT_POSITION_FOLLOWING) !==
          0,
    };
  });
  expect(order).toEqual({
    consent: true,
    stub: true,
    stubInHead: true,
    consentFirst: true,
  });

  // Both already in dataLayer when the stub ran, as gtag() calls (recorded as arrays)
  const loadedWith = await readGtmStubLoadedWith(page);
  expect(loadedWith).toBeGreaterThan(0);
  const beforeStub = (await readGtmStub(page)).slice(0, loadedWith);
  expect(consentCommands(beforeStub, "default")).toEqual([consentModeDenied]);
  expect(consentCommands(beforeStub, "update")).toEqual([
    consentModeAnalyticsGranted,
  ]);
});

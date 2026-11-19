import { expect, test, type BrowserContext, type Page } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import {
  assertAuthenticated,
  confirmEmail,
  logout,
  registerMember,
  signIn,
} from "../../support/membership/flows.js";
import { newsletterSelectors } from "../../support/newsletter/flows.js";
import {
  acceptAll,
  necessaryOnly,
  openSettings,
  setToggles,
} from "../../support/privacy/consent.js";
import { getCookie, setCookies } from "../../support/privacy/cookies.js";
import {
  expectNoRecaptcha,
  RequestRecorder,
  waitForRecaptchaRequest,
} from "../../support/privacy/network.js";
import {
  consentModeAnalyticsGranted,
  consentModeDenied,
  effectiveConsent,
  readDataLayerConsent,
} from "../../support/privacy/state.js";

/*
 * Third-party scripts.
 * - reCAPTCHA loads only where a captcha is used.
 * - Google Consent Mode signals, read from each page's own `window.dataLayer` (parallel-safe). They
 *   assert what the portal emits, not what GTM does with it (no GTM locally).
 * - Host-only GA cookies are deleted when analytics is refused.
 * The "consent default before Tag Manager snippets" check needs the site-wide Tag Manager stub, so
 * it's in `third-party.isolated.spec.ts`.
 */

/** A CI-content blog post. */
const blogPostPath = "/blog/Xperience-by-Kentico-Refresh-December-12-2024";

/** Host-only GA cookies, as GA4 sets them once `cookie_domain` is the portal host. */
const gaCookies = { _ga: "GA1.1.123.456", _ga_TEST: "GS1.1.1" };

async function expectGaCookies(
  context: BrowserContext,
  present: boolean,
): Promise<void> {
  for (const name of Object.keys(gaCookies)) {
    const cookie = await getCookie(context, name);
    if (present) {
      expect(cookie, name).toBeDefined();
    } else {
      expect(cookie, name).toBeUndefined();
    }
  }
}

test.describe("reCAPTCHA", () => {
  // Login and password recovery have no captcha; rate limiting protects them
  for (const path of ["/q-and-a", blogPostPath, "/login", "/account-recovery"]) {
    test(`${path} loads no reCAPTCHA`, async ({ page }) => {
      const recorder = RequestRecorder.attach(page);

      const response = await page.goto(path);
      expect(response?.ok(), `${path} status`).toBe(true);
      await page.waitForLoadState("networkidle");

      expectNoRecaptcha(recorder);
    });
  }

  for (const path of ["/register"]) {
    test(`${path} loads reCAPTCHA`, async ({ page }) => {
      const recaptcha = waitForRecaptchaRequest(page);

      await page.goto(path);

      expect((await recaptcha).url()).toContain("google.com/recaptcha/api.js");
    });
  }

  // Form Builder forms with the reCAPTCHA component can sit on any page, so they load reCAPTCHA
  // once the form scrolls into view. The home page newsletter form is below the fold.
  const viewports = {
    desktop: { viewport: { width: 1920, height: 1080 } },
    "desktop (tall)": { viewport: { width: 1080, height: 1920 } },
    mobile: {
      viewport: { width: 390, height: 844 },
      isMobile: true,
      hasTouch: true,
    },
  };

  for (const [name, options] of Object.entries(viewports)) {
    test.describe(`home page, ${name}`, () => {
      test.use(options);

      test("loads reCAPTCHA once the newsletter form scrolls into view", async ({
        page,
      }) => {
        const recorder = RequestRecorder.attach(page);
        await page.goto("/");
        // The form's script (a dynamic import) has run
        await page.waitForLoadState("networkidle");

        const form = page.locator(newsletterSelectors.form);
        await expect(form, "below the fold").not.toBeInViewport();
        expectNoRecaptcha(recorder);

        const recaptcha = waitForRecaptchaRequest(page);
        await form.scrollIntoViewIfNeeded();

        expect((await recaptcha).url()).toContain(
          "google.com/recaptcha/api.js",
        );
      });
    });
  }

  test("without IntersectionObserver, the home page loads reCAPTCHA at once", async ({
    page,
  }) => {
    await page.addInitScript(() => {
      // @ts-expect-error simulating a browser without it
      delete window.IntersectionObserver;
    });
    const recaptcha = waitForRecaptchaRequest(page);

    await page.goto("/");

    expect((await recaptcha).url()).toContain("google.com/recaptcha/api.js");
  });

  // A sign-up landing page: the form is in view at load
  test("/newsletter-signup loads reCAPTCHA without user action", async ({
    page,
  }) => {
    const recaptcha = waitForRecaptchaRequest(page);

    await page.goto("/newsletter-signup");

    expect((await recaptcha).url()).toContain("google.com/recaptcha/api.js");
  });
});

/** Every consent command on the page is a real `gtag()` call. */
async function expectGtagCommands(page: Page) {
  const commands = await readDataLayerConsent(page);

  expect(commands.length, "consent commands").toBeGreaterThan(0);
  expect(
    commands.filter((command) => !command.isGtagArguments),
    "consent entries that aren't gtag() arguments",
  ).toEqual([]);

  return commands;
}

/** A fresh page load: `consent default` first, all denied, then the saved choice (if any). */
async function expectPageLoadConsent(
  page: Page,
  expected: Record<string, string>,
): Promise<void> {
  const commands = await expectGtagCommands(page);

  expect(commands[0]).toMatchObject({
    command: "default",
    params: consentModeDenied,
  });
  expect(commands.filter((c) => c.command === "default")).toHaveLength(1);
  expect(effectiveConsent(commands)).toEqual(expected);
}

test.describe("Consent Mode signals", () => {
  test("first visit: consent default, everything denied", async ({ page }) => {
    await page.goto("/");

    await expectPageLoadConsent(page, consentModeDenied);
    expect(
      (await readDataLayerConsent(page)).filter((c) => c.command === "update"),
    ).toEqual([]);
  });

  test("Accept all: consent update grants analytics, never ads", async ({
    page,
  }) => {
    await page.goto("/");
    await acceptAll(page);

    // Pushed by the banner response, without a reload
    await expect
      .poll(async () => (await expectGtagCommands(page)).at(-1))
      .toEqual({
        command: "update",
        params: consentModeAnalyticsGranted,
        isGtagArguments: true,
      });

    await page.goto("/blog");
    await expectPageLoadConsent(page, consentModeAnalyticsGranted);
    expect((await readDataLayerConsent(page)).at(-1)?.command).toBe("update");
  });

  test("Necessary only: denied", async ({ page }) => {
    await page.goto("/");
    await necessaryOnly(page);

    await expect
      .poll(async () => (await expectGtagCommands(page)).at(-1))
      .toEqual({
        command: "update",
        params: consentModeDenied,
        isGtagArguments: true,
      });

    await page.goto("/blog");
    await expectPageLoadConsent(page, consentModeDenied);
  });

  test("Analytics on in cookie settings: granted on the next page", async ({
    page,
  }) => {
    await openSettings(page);
    await setToggles(page, { analytics: true, tracking: false });

    await expect
      .poll(async () => effectiveConsent(await expectGtagCommands(page)))
      .toEqual(consentModeAnalyticsGranted);

    await page.goto("/");
    await expectPageLoadConsent(page, consentModeAnalyticsGranted);
  });
});

test.describe("GA cookie cleanup", () => {
  test("Necessary only deletes host-only GA cookies", async ({
    page,
    context,
  }) => {
    await page.goto("/");
    await setCookies(context, gaCookies);

    await necessaryOnly(page);

    await expectGaCookies(context, false);
  });

  test("turning Analytics off deletes host-only GA cookies", async ({
    page,
    context,
  }) => {
    await openSettings(page);
    await setToggles(page, { analytics: true, tracking: false });
    await setCookies(context, gaCookies);

    await page.reload();
    await setToggles(page, { analytics: false, tracking: false });

    await expectGaCookies(context, false);
  });

  test("Accept all keeps them", async ({ page, context }) => {
    await page.goto("/");
    await setCookies(context, gaCookies);

    await acceptAll(page);

    await expectGaCookies(context, true);
  });
});

test.describe("sign-out", () => {
  test("deletes host-only GA cookies and the next page is denied", async ({
    page,
    context,
  }) => {
    test.setTimeout(90_000);

    const identity = createRegistrationIdentity("playwright-wp5-signout");
    const emailClient = await createMcpClient();
    try {
      await page.goto("/");
      await acceptAll(page);
      await registerMember(page, identity);
      await confirmEmail(page, emailClient, identity);
    } finally {
      await emailClient.close();
    }

    await signIn(page, identity.username, identity.password);
    await page.waitForURL((url) => !url.pathname.startsWith("/login"));
    await page.goto("/");
    await assertAuthenticated(page, identity.username);
    await expectPageLoadConsent(page, consentModeAnalyticsGranted);

    await setCookies(context, gaCookies);
    await logout(page);
    await page.waitForLoadState("load");

    await expectGaCookies(context, false);
    await expectPageLoadConsent(page, consentModeDenied);
  });
});

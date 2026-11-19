import {
  expect,
  type BrowserContext,
  type Page,
  type Request,
} from "@playwright/test";

export type RecordedRequest = {
  url: string;
  method: string;
  resourceType: string;
  /** The page URL the request was made from, for failure messages. */
  frameUrl: string;
};

/** Xperience activity logger: `POST /kentico.activities/kenticoactivitylogger/log`. */
export function isActivityPost(request: RecordedRequest): boolean {
  return (
    request.method === "POST" &&
    new URL(request.url).pathname.toLowerCase().includes("/kentico.activities/")
  );
}

/** The logger script `ActivityLoggingScriptV2` renders: `/kentico.resource/activities/kenticoactivitylogger/loggerv2.js`. */
const activityScriptPath = "/kentico.resource/activities/kenticoactivitylogger/";

/** The logger script tag. Only rendered for visitors who allowed Website tracking. */
export const activityScriptSelector = `script[src*="${activityScriptPath}" i]`;

export function isActivityScriptRequest(request: RecordedRequest): boolean {
  return new URL(request.url).pathname.toLowerCase().startsWith(activityScriptPath);
}

const recaptchaHosts = ["google.com", "gstatic.com", "recaptcha.net"];

export function isRecaptchaRequest(request: RecordedRequest): boolean {
  const url = new URL(request.url);

  return (
    hostMatches(url.hostname, recaptchaHosts) &&
    url.pathname.includes("/recaptcha")
  );
}

/** Analytics and advertising hosts. None may be contacted before consent. */
export const googleTrackingHosts = [
  "googletagmanager.com",
  "google-analytics.com",
  "doubleclick.net",
  "googlesyndication.com",
] as const;

export function isGoogleTrackingRequest(request: RecordedRequest): boolean {
  return hostMatches(new URL(request.url).hostname, googleTrackingHosts);
}

/**
 * Records every request made in a browser context (all pages and frames, including requests the
 * browser serves from cache or that fail). Attach before the first navigation.
 */
export class RequestRecorder {
  readonly requests: RecordedRequest[] = [];
  private readonly listener = (request: Request) => {
    this.requests.push({
      url: request.url(),
      method: request.method(),
      resourceType: request.resourceType(),
      frameUrl: safeFrameUrl(request),
    });
  };

  private constructor(private readonly target: BrowserContext | Page) {}

  static attach(target: BrowserContext | Page): RequestRecorder {
    const recorder = new RequestRecorder(target);
    target.on("request", recorder.listener);

    return recorder;
  }

  detach(): void {
    this.target.off("request", this.listener);
  }

  /** Forgets what was recorded so far, e.g. between "before" and "after consent" phases. */
  clear(): void {
    this.requests.length = 0;
  }

  activityPosts(): RecordedRequest[] {
    return this.requests.filter(isActivityPost);
  }

  activityScriptRequests(): RecordedRequest[] {
    return this.requests.filter(isActivityScriptRequest);
  }

  recaptchaRequests(): RecordedRequest[] {
    return this.requests.filter(isRecaptchaRequest);
  }

  googleTrackingRequests(): RecordedRequest[] {
    return this.requests.filter(isGoogleTrackingRequest);
  }
}

/**
 * Asserts the activity logger didn't run on the current page. The logger posts after `load`, so
 * this checks its script isn't on the page or requested, then waits for network idle before
 * checking for the POST.
 */
export async function expectNoActivityPost(
  page: Page,
  recorder: RequestRecorder,
): Promise<void> {
  await expect(
    page.locator(activityScriptSelector),
    "activity logger script",
  ).toHaveCount(0);
  await page.waitForLoadState("networkidle");

  expect(
    describe(recorder.activityScriptRequests()),
    "activity logger script requests",
  ).toEqual([]);
  expect(describe(recorder.activityPosts()), "activity POSTs").toEqual([]);
}

export function expectNoRecaptcha(recorder: RequestRecorder): void {
  expect(describe(recorder.recaptchaRequests()), "reCAPTCHA requests").toEqual(
    [],
  );
}

export function expectNoGoogleTracking(recorder: RequestRecorder): void {
  expect(
    describe(recorder.googleTrackingRequests()),
    "Google analytics/ads requests",
  ).toEqual([]);
}

/** Activity, reCAPTCHA and Google analytics/ads requests, all in one assertion set. */
export async function expectNoTrackingRequests(
  page: Page,
  recorder: RequestRecorder,
): Promise<void> {
  await expectNoActivityPost(page, recorder);
  expectNoRecaptcha(recorder);
  expectNoGoogleTracking(recorder);
}

/**
 * Resolves with the next activity POST. The logger posts after page load, so start waiting before
 * the navigation that should trigger it.
 */
export function waitForActivityPost(
  page: Page,
  timeoutMs = 10_000,
): Promise<Request> {
  return page.waitForRequest(
    (request) =>
      isActivityPost({
        url: request.url(),
        method: request.method(),
        resourceType: request.resourceType(),
        frameUrl: "",
      }),
    { timeout: timeoutMs },
  );
}

/**
 * Resolves with the next reCAPTCHA request (the `api.js` loader first). Start waiting before the
 * navigation or interaction that should load it.
 */
export function waitForRecaptchaRequest(
  page: Page,
  timeoutMs = 10_000,
): Promise<Request> {
  return page.waitForRequest(
    (request) =>
      isRecaptchaRequest({
        url: request.url(),
        method: request.method(),
        resourceType: request.resourceType(),
        frameUrl: "",
      }),
    { timeout: timeoutMs },
  );
}

function hostMatches(hostname: string, hosts: readonly string[]): boolean {
  return hosts.some((host) => hostname === host || hostname.endsWith(`.${host}`));
}

function describe(requests: RecordedRequest[]): string[] {
  return requests.map(
    (request) => `${request.method} ${request.url} (from ${request.frameUrl})`,
  );
}

function safeFrameUrl(request: Request): string {
  try {
    return request.frame().url();
  } catch {
    // Service worker requests have no frame
    return "";
  }
}

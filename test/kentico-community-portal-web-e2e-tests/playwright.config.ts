import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "@playwright/test";
import {
  adminBaseUrl,
  appBaseUrl,
  isLoopbackUrl,
  webServerTimeoutMs,
} from "./support/config.js";

const isCi = !!process.env.CI;

// The app serves HTTPS with the untrusted ASP.NET Core development certificate. `ignoreHTTPSErrors`
// below covers browser traffic, but the testing-tool clients use Node's global fetch, which does
// its own validation against the bundled CA list and rejects that certificate. Same intent, applied
// to the test process.
//
// This switch is process-wide - it cannot be scoped to a single client without a custom undici
// dispatcher - so the URLs the suite talks to are checked first. Pointed at anything but this
// machine the suite would silently stop verifying certificates, which is worth failing over.
const nonLoopbackUrls = [appBaseUrl, adminBaseUrl].filter(
  (url) => !isLoopbackUrl(url),
);

if (nonLoopbackUrls.length > 0) {
  throw new Error(
    `The E2E suite disables TLS verification for the local development certificate, so it can only run against this machine. Non-loopback URL(s) configured: ${nonLoopbackUrls.join(", ")}.`,
  );
}

process.env.NODE_TLS_REJECT_UNAUTHORIZED = "0";
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "../..");
const webProjectPath = path.resolve(
  repoRoot,
  "src/Kentico.Community.Portal.Web/Kentico.Community.Portal.Web.csproj",
);
const publishedDllPath = path.resolve(
  repoRoot,
  "publish/Kentico.Community.Portal.Web.dll",
);
// Storage state written by the `admin-setup` project's auth.setup.ts and
// reused by `admin-tests` below, so authenticated admin tests can start
// signed in instead of each performing its own sign-in.
const adminStorageStatePath = path.resolve(
  __dirname,
  "playwright/.auth/admin.json",
);
const webServerCommand = isCi
  ? `dotnet ${publishedDllPath}`
  : `dotnet run --project ${webProjectPath}`;
const webServerCwd = isCi ? path.dirname(publishedDllPath) : repoRoot;
const appEnvironment =
  process.env.ASPNETCORE_ENVIRONMENT ?? (isCi ? "CI" : "Development");
const webServerEnv = {
  ...process.env,
  ASPNETCORE_ENVIRONMENT: appEnvironment,
  DOTNET_ENVIRONMENT: process.env.DOTNET_ENVIRONMENT ?? appEnvironment,
};

// Third-party telemetry endpoints the admin UI calls on load. Blocked for every project via
// launchOptions below. Add hosts here rather than routing them away in individual specs.
const blockedTelemetryHosts = ["data.eu.pendo.io", "analytics.kentico.com"];

const webServers = [
  {
    command: webServerCommand,
    cwd: webServerCwd,
    env: webServerEnv,
    url: appBaseUrl,
    timeout: webServerTimeoutMs,
    ignoreHTTPSErrors: true,
    reuseExistingServer: !isCi,
    stdout: "pipe" as const,
    stderr: "pipe" as const,
  },
];

export default defineConfig({
  testDir: "./tests",
  projects: [
    {
      name: "portal-tests",
      testIgnore: [/tests\/admin\//, /\.isolated\.spec\.ts$/],
    },
    {
      name: "admin-setup",
      testMatch: /tests\/admin\/auth\.setup\.ts/,
    },
    {
      name: "admin-tests",
      testMatch: /tests\/admin\/.*\.spec\.ts/,
      dependencies: ["admin-setup"],
      use: {
        storageState: adminStorageStatePath,
      },
    },
    // Specs that change process-wide state (e.g. read-only mode) and would break concurrent
    // specs. They run after everything else. Run one on its own with `--no-deps`.
    // One worker: they'd also break each other (read-only mode blocks the Tag Manager stub write).
    {
      name: "isolated-tests",
      testMatch: /\.isolated\.spec\.ts$/,
      dependencies: ["portal-tests", "admin-tests"],
      fullyParallel: false,
      workers: 1,
    },
  ],
  fullyParallel: true,
  // Locally the machine also runs the Debug app, Vite, SQL and other work. More than 4 workers
  // pushes specs past their timeout (see docs/Run-Tests.md). CI keeps Playwright's default.
  workers: isCi ? undefined : 4,
  forbidOnly: isCi,
  // No retries in CI. The two retries here predated the TypeScript suite, when the app was
  // started as a backgrounded process and specs raced its startup; `webServer` below now owns
  // that lifecycle and waits for the app before any spec runs. Retrying a genuinely broken
  // spec three times was adding ~6 minutes to every red run and buying nothing. Restore this
  // if real flakiness reappears - but fix the flake rather than hiding it.
  retries: 0,
  reporter: [["list"], ["html", { open: "never" }]],
  expect: {
    timeout: 5_000,
  },
  use: {
    baseURL: appBaseUrl,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
    colorScheme: "light",
    viewport: {
      width: 1080,
      height: 1920,
    },
    ignoreHTTPSErrors: true,
    launchOptions: {
      // The admin UI reports to Pendo and Kentico analytics on load - nine internet
      // round-trips from the runner during a single admin spec, interleaved with the page
      // load being asserted on. None of it is under test.
      //
      // Blocked in Chromium's resolver rather than with page.route so no spec has to know
      // about it: ~NOTFOUND fails resolution immediately instead of waiting on a connection.
      args: [
        `--host-resolver-rules=${blockedTelemetryHosts
          .map((host) => `MAP ${host} ~NOTFOUND`)
          .join(", ")}`,
      ],
    },
  },
  webServer: webServers,
});

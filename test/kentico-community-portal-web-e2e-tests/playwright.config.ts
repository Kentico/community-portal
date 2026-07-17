import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineConfig } from "@playwright/test";
import { appBaseUrl, webServerTimeoutMs } from "./support/config.js";

const isCi = !!process.env.CI;
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
      testIgnore: [/tests\/admin\//],
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
  ],
  fullyParallel: true,
  forbidOnly: isCi,
  retries: isCi ? 2 : 0,
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
  },
  webServer: webServers,
});

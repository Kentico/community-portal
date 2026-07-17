import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { test } from "@playwright/test";
import { signInToAdmin } from "../../support/admin/auth.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const adminStorageStatePath = path.resolve(
  __dirname,
  "../../playwright/.auth/admin.json",
);

// Runs once as the `admin-setup` project (see playwright.config.ts) before
// the `admin-tests` project. Signs into the admin UI and persists the
// resulting storage state to disk so dependent admin tests start already
// authenticated instead of signing in individually.
test("authenticate admin and persist storage state", async ({ page }) => {
  await signInToAdmin(page);

  await fs.mkdir(path.dirname(adminStorageStatePath), { recursive: true });
  await page.context().storageState({ path: adminStorageStatePath });
});

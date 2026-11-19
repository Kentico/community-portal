import {
  expect,
  test,
  type FrameLocator,
  type Locator,
  type Page,
  type Request,
} from "@playwright/test";
import { AdminShellPage } from "../../support/admin/pageObjects/adminShellPage.js";
import { appBaseUrl } from "../../support/config.js";

const MARKDOWN_WIDGET_IDENTIFIER = "CommunityPortal.MarkdownWidget";
// The inline editor sends edits to Page Builder through a 300ms debounce.
const EDIT_FLUSH_MS = 600;

const EDITOR_ASSET_PATTERN = /page-builder-markdown-editor|milkdown/i;

const runId = Date.now();
const PAGE_NAME = `E2E Markdown Inline ${runId}`;
const HEADING_A = `Heading A ${runId}`;
const PARAGRAPH_A = `Paragraph A ${runId}`;
const RAW_LINE_A = `Raw line A ${runId}`;
const PARAGRAPH_B = `Paragraph B ${runId}`;

function inlineEditors(frame: FrameLocator): Locator {
  return frame.getByTestId("markdown-inline-editor");
}

async function expectEditorReady(frame: FrameLocator, editor: Locator) {
  // Page Builder's inline editor wrapper is flagged once Crepe is mounted.
  await expect(
    frame.locator('[data-md-ready="true"]').filter({ has: editor }),
  ).toBeVisible({ timeout: 30_000 });
  await expect(editor.locator(".ProseMirror")).toBeVisible();
}

// Page Builder overlays its add-widget button on the middle of a short
// widget, so click the corner of the editor instead of its center.
async function focusEditor(editor: Locator) {
  await editor.locator(".ProseMirror").click({ position: { x: 4, y: 4 } });
}

// Server-rendered Markdig output, as shown in preview and on the live site.
async function expectRenderedContent(body: Locator) {
  await expect(body.locator("h1", { hasText: HEADING_A })).toBeVisible({
    timeout: 20_000,
  });
  await expect(body.locator("p", { hasText: PARAGRAPH_A })).toBeVisible();
  await expect(body.locator("p", { hasText: RAW_LINE_A })).toBeVisible();
  await expect(body.locator("p", { hasText: PARAGRAPH_B })).toBeVisible();
  await expect(body.getByTestId("markdown-inline-editor")).toHaveCount(0);
  await expect(body.locator(".milkdown, .ProseMirror")).toHaveCount(0);
}

async function save(page: Page) {
  await page.waitForTimeout(EDIT_FLUSH_MS);
  const saveButton = page.getByRole("button", { name: "Save", exact: true });
  await saveButton.click();
  // Save is disabled while the draft has no unsaved changes.
  await expect(saveButton).toBeDisabled({ timeout: 20_000 });
}

test.describe("page builder markdown widget inline editor", () => {
  test("edits Markdown widgets in place and renders them on the live site", async ({
    page,
    browser,
  }) => {
    test.setTimeout(300_000);
    // Fail a stuck step fast enough for the cleanup below to still run.
    page.setDefaultTimeout(20_000);

    const errors: string[] = [];
    page.on("console", (message) => {
      // Telemetry hosts are blocked in playwright.config.ts, so the admin
      // shell logs failed lookups for them on every load.
      if (
        message.type() === "error" &&
        !message.text().includes("net::ERR_NAME_NOT_RESOLVED")
      ) {
        errors.push(`console: ${message.text()} (${message.location().url})`);
      }
    });
    page.on("pageerror", (error) => {
      errors.push(`pageerror: ${error.message}`);
    });

    const adminShell = new AdminShellPage(page);
    await adminShell.goto();
    await adminShell.expectSignedIn();

    const websiteChannel = await adminShell.openWebsiteChannel();
    const livePath = await websiteChannel.createLandingPage(PAGE_NAME);

    try {
      const pageBuilder = await websiteChannel.openPageBuilder();
      const frame = pageBuilder.builderFrame;
      const editors = inlineEditors(frame);
      const editorA = editors.filter({ hasText: HEADING_A });

      await test.step("editor loads in edit mode", async () => {
        errors.length = 0;
        const editorScript = page.waitForRequest(/page-builder-markdown-editor/);
        await pageBuilder.addWidget(MARKDOWN_WIDGET_IDENTIFIER);

        await expect(editors).toHaveCount(1);
        await expectEditorReady(frame, editors);
        await expect(
          editors.getByTestId("markdown-inline-editor-toggle"),
        ).toBeVisible();
        await expect(
          editors.getByTestId("markdown-inline-editor-raw"),
        ).toBeHidden();
        // Edit mode is the only place the editor script is requested.
        await editorScript;
        expect(errors, "inline editor loads without errors").toEqual([]);
      });

      await test.step("rich text edits persist", async () => {
        await focusEditor(editors);
        await page.keyboard.type(`# ${HEADING_A}`);
        await page.keyboard.press("Enter");
        await page.keyboard.type(PARAGRAPH_A);

        await expect(editorA.locator(".ProseMirror h1")).toHaveText(HEADING_A);
        await expect(editorA.locator(".ProseMirror p")).toHaveText(PARAGRAPH_A);

        await save(page);
        await page.reload();

        await expect(editorA).toHaveCount(1, { timeout: 30_000 });
        await expectEditorReady(frame, editorA);
        await expect(editorA.locator(".ProseMirror h1")).toHaveText(HEADING_A);
        await expect(editorA.locator(".ProseMirror p")).toHaveText(PARAGRAPH_A);
      });

      await test.step("content uses site styles, editor UI uses Crepe styles", async () => {
        const paragraph = editorA.locator(".ProseMirror p");
        const fontFamily = (el: Element) => getComputedStyle(el).fontFamily;

        const bodyFont = await frame.locator("body").evaluate(fontFamily);
        expect(await paragraph.evaluate(fontFamily)).toBe(bodyFont);

        // Selecting text opens the Crepe floating toolbar.
        const toolbar = editorA.locator('.milkdown-toolbar[data-show="true"]');
        await expect(async () => {
          await paragraph.click({ position: { x: 4, y: 4 } });
          await page.keyboard.press("End");
          await page.keyboard.press("Shift+Home");
          await expect(toolbar).toBeVisible({ timeout: 2_000 });
        }).toPass({ timeout: 15_000 });

        const crepeFont = await toolbar.evaluate((el) =>
          getComputedStyle(el).getPropertyValue("--crepe-font-default").trim(),
        );
        expect(crepeFont).toContain("Noto Sans");
        const toolbarFont = await toolbar.evaluate(fontFamily);
        expect(toolbarFont).toMatch(/^"?Noto Sans/);
        expect(toolbarFont).not.toBe(bodyFont);

        await page.keyboard.press("End");
      });

      await test.step("raw markdown edits round-trip", async () => {
        // Raw mode has no rendered text to filter on; this is the only widget.
        const toggle = editors.getByTestId("markdown-inline-editor-toggle");
        const raw = editors.getByTestId("markdown-inline-editor-raw");

        await toggle.click();
        await expect(raw).toBeVisible();
        await expect(toggle).toHaveAttribute("aria-pressed", "true");
        await expect(
          editors.getByTestId("markdown-inline-editor-root"),
        ).toBeHidden();
        await expect(raw).toHaveValue(new RegExp(`^# ${HEADING_A}`));
        await expect(raw).toHaveValue(new RegExp(PARAGRAPH_A));

        await raw.focus();
        await page.keyboard.press("Control+End");
        await page.keyboard.press("Enter");
        await page.keyboard.press("Enter");
        await page.keyboard.type(RAW_LINE_A);

        await toggle.click();
        await expectEditorReady(frame, editorA);
        await expect(raw).toBeHidden();
        await expect(editorA.locator(".ProseMirror")).toContainText(
          RAW_LINE_A,
        );
        await expect(editorA.locator(".ProseMirror h1")).toHaveText(HEADING_A);

        await save(page);
      });

      await test.step("multiple widgets edit independently", async () => {
        await pageBuilder.addWidget(MARKDOWN_WIDGET_IDENTIFIER);
        await expect(editors).toHaveCount(2, { timeout: 15_000 });

        const editorB = editors.filter({ hasNotText: HEADING_A });
        await expectEditorReady(frame, editorB);
        await focusEditor(editorB);
        await page.keyboard.type(PARAGRAPH_B);

        await expect(editorB.locator(".ProseMirror")).toContainText(
          PARAGRAPH_B,
        );
        await expect(editorA.locator(".ProseMirror")).not.toContainText(
          PARAGRAPH_B,
        );
        await expect(editorB.locator(".ProseMirror")).not.toContainText(
          RAW_LINE_A,
        );

        await save(page);
        await page.reload();

        await expect(editors).toHaveCount(2, { timeout: 30_000 });
        const savedB = editors.filter({ hasText: PARAGRAPH_B });
        await expectEditorReady(frame, editorA);
        await expectEditorReady(frame, savedB);
        await expect(editorA.locator(".ProseMirror")).toContainText(
          RAW_LINE_A,
        );
        await expect(editorA.locator(".ProseMirror")).not.toContainText(
          PARAGRAPH_B,
        );
        await expect(savedB.locator(".ProseMirror")).not.toContainText(
          HEADING_A,
        );
      });

      expect(errors, "inline editor runs without errors").toEqual([]);

      await test.step("preview renders HTML without the editor", async () => {
        const editorRequests: string[] = [];
        const onRequest = (request: Request) => {
          if (EDITOR_ASSET_PATTERN.test(request.url())) {
            editorRequests.push(request.url());
          }
        };
        page.on("request", onRequest);

        await page.getByRole("link", { name: "Preview" }).click();
        const preview = page.frameLocator('iframe[title="Preview"]');

        await expectRenderedContent(preview.locator("body"));
        await (await page.locator('iframe[title="Preview"]').elementHandle())
          ?.contentFrame()
          .then((previewFrame) => previewFrame?.waitForLoadState("load"));
        page.off("request", onRequest);
        expect(editorRequests, "preview does not load Milkdown").toEqual([]);
      });

      await test.step("live site renders HTML without the editor", async () => {
        // The Preview tab has no Publish action.
        await page.getByRole("link", { name: "Page Builder" }).click();
        await pageBuilder.publish();

        // A visitor without the admin session.
        const context = await browser.newContext({ ignoreHTTPSErrors: true });
        try {
          const livePage = await context.newPage();
          const requests: string[] = [];
          livePage.on("request", (request) => requests.push(request.url()));

          await livePage.goto(`${appBaseUrl}${livePath}`);
          await expectRenderedContent(livePage.locator("body"));
          await livePage.waitForLoadState("load");

          expect(
            requests.filter((url) => EDITOR_ASSET_PATTERN.test(url)),
            "live site does not load Milkdown",
          ).toEqual([]);
        } finally {
          await context.close();
        }
      });
    } finally {
      await adminShell.goto();
      const cleanupChannel = await adminShell.openWebsiteChannel();
      await cleanupChannel.movePageToRecycleBin(PAGE_NAME);
    }
  });
});

import { expect, test, type Browser } from "@playwright/test";
import { RecipientListSubscribersPage } from "../../support/admin/pageObjects/recipientListSubscribersPage.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import {
  extractSubscriptionConfirmationUrl,
  submitNewsletterForm,
  waitForNewsletterConfirmationEmail,
} from "../../support/newsletter/flows.js";
import { necessaryOnly } from "../../support/privacy/consent.js";
import { getNewsletterUnsubscribeUrl, getRecipientListState } from "../../support/privacy/state.js";

/*
 * The Recipients listing shows active subscribers only (list member, latest confirmation approved).
 * Xperience keeps unsubscribed contacts in the list with an unapproved row.
 */

const newsletterDisplayName = "Kentico Community news and updates";

function newsletterEmail(label: string): string {
  return `playwright-admin-${label}.${Date.now()}-${Math.floor(Math.random() * 10_000)}@kentico.local`;
}

/** Double opt-in from the sign-up page, in a visitor context without the admin session. */
async function subscribe(browser: Browser, email: string): Promise<void> {
  const context = await browser.newContext({ ignoreHTTPSErrors: true, storageState: undefined });
  const emailClient = await createMcpClient();

  try {
    const page = await context.newPage();
    await page.goto("/newsletter-signup");
    await necessaryOnly(page);
    await submitNewsletterForm(page, email);

    const confirmation = await waitForNewsletterConfirmationEmail(emailClient, email);
    await page.goto(extractSubscriptionConfirmationUrl(confirmation));

    await expect.poll(async () => (await getRecipientListState(email)).subscribed).toBe(true);
  } finally {
    await emailClient.close();
    await context.close();
  }
}

test.describe("recipient list subscribers administration", () => {
  test.describe.configure({ timeout: 90_000 });

  test("lists a subscribed recipient, not an unsubscribed one", async ({
    browser,
    page,
    request,
  }) => {
    const subscribed = newsletterEmail("subscribed");
    const unsubscribed = newsletterEmail("unsubscribed");

    await subscribe(browser, subscribed);
    await subscribe(browser, unsubscribed);

    const response = await request.post(await getNewsletterUnsubscribeUrl(unsubscribed), {
      maxRedirects: 0,
    });
    expect(response.status(), await response.text()).toBeLessThan(400);

    const state = await getRecipientListState(unsubscribed);
    expect(state.subscribed).toBe(false);
    expect(state.isListMember).toBe(true);

    const subscribers = new RecipientListSubscribersPage(page);
    await subscribers.open(newsletterDisplayName);

    await subscribers.search(subscribed);
    await expect(subscribers.row(subscribed)).toHaveCount(1);

    // Still a list member, but not listed
    await subscribers.search(unsubscribed);
    await expect(subscribers.rows()).toHaveCount(0);
  });
});

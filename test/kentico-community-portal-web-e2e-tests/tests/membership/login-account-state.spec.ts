import { expect, test, type Page } from "@playwright/test";
import { createRegistrationIdentity } from "../../support/data/testIdentity.js";
import {
  confirmEmail,
  registerMember,
  signIn,
  signInSelectors,
} from "../../support/membership/flows.js";
import { createMcpClient } from "../../support/mcp/createMcpClient.js";
import { setMemberModerationStatus } from "../../support/testingTools/moderationClient.js";

/** Every hook that would tell an account's state apart from "no such account". */
const accountStateHooks = [
  signInSelectors.moderationMessage,
  signInSelectors.disabledMessage,
  "form[test-id=emailNotConfirmedForm]",
];

type SignInOutcome = { status: number; error: string; stateHooks: number };

async function signInOutcome(
  page: Page,
  usernameOrEmail: string,
  password: string,
): Promise<SignInOutcome> {
  await page.goto("/login");
  await page.locator("[test-id=userNameOrEmail]").fill(usernameOrEmail);
  await page.locator("[test-id=password]").fill(password);

  const [response] = await Promise.all([
    page.waitForResponse("**/loginwidget/login"),
    page.locator("form[test-id=signInForm] button[type=submit]").click(),
  ]);
  await expect(page.locator(signInSelectors.error)).not.toBeEmpty();

  let stateHooks = 0;
  for (const hook of accountStateHooks) {
    stateHooks += await page.locator(hook).count();
  }

  return {
    status: response.status(),
    error: (await page.locator(signInSelectors.error).innerText()).trim(),
    stateHooks,
  };
}

test.describe("sign-in with a wrong password", () => {
  test("an unconfirmed or moderated account looks like no account", async ({
    page,
  }) => {
    const unconfirmed = createRegistrationIdentity("pw-login-unconf");
    const moderated = createRegistrationIdentity("pw-login-moderated");
    const emailClient = await createMcpClient();

    try {
      await registerMember(page, unconfirmed);
      await registerMember(page, moderated);
      await confirmEmail(page, emailClient, moderated);
      await setMemberModerationStatus(moderated.username, "Spam");

      const nonexistent = await signInOutcome(
        page,
        createRegistrationIdentity("pw-login-nobody").username,
        "wrong-password-1",
      );
      expect(nonexistent.stateHooks).toBe(0);

      expect(
        await signInOutcome(page, unconfirmed.username, "wrong-password-1"),
      ).toEqual(nonexistent);
      expect(
        await signInOutcome(page, moderated.username, "wrong-password-1"),
      ).toEqual(nonexistent);

      // The right password still explains the state
      await signIn(page, unconfirmed.username, unconfirmed.password);
      await expect(
        page.locator("form[test-id=emailNotConfirmedForm]"),
      ).toBeVisible();
      await signIn(page, moderated.username, moderated.password);
      await expect(page.locator(signInSelectors.moderationMessage)).toBeVisible();
    } finally {
      await setMemberModerationStatus(moderated.username, "None").catch(() => {});
      await emailClient.close();
    }
  });
});

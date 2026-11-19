import { appBaseUrl } from "../config.js";

export async function setMemberModerationStatus(
  userName: string,
  moderationStatus: "None" | "Spam" | "Flagged" | "Archived",
): Promise<void> {
  const response = await fetch(`${appBaseUrl}/testing/e2e/member-moderation`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
    },
    body: JSON.stringify({
      userName,
      moderationStatus,
    }),
  });

  if (!response.ok) {
    const body = await response.text();

    throw new Error(
      `Failed to set moderation status for '${userName}' to '${moderationStatus}'. ${response.status} ${body}`,
    );
  }
}

/** Enables or disables a member like an administrator does on the member Edit form. */
export async function setMemberEnabled(
  userName: string,
  enabled: boolean,
): Promise<void> {
  const response = await fetch(`${appBaseUrl}/testing/e2e/member-enabled`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
    },
    body: JSON.stringify({ userName, enabled }),
  });

  if (!response.ok) {
    const body = await response.text();

    throw new Error(
      `Failed to set enabled for '${userName}' to '${enabled}'. ${response.status} ${body}`,
    );
  }
}

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

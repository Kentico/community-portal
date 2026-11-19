import { appBaseUrl } from "../config.js";

export async function setMemberRole(
  userName: string,
  roleName: "KenticoMVP" | "KenticoCommunityLeader" | "KenticoEmployee",
  granted: boolean,
): Promise<void> {
  const response = await fetch(`${appBaseUrl}/testing/e2e/member-role`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
    },
    body: JSON.stringify({
      userName,
      roleName,
      granted,
    }),
  });

  if (!response.ok) {
    const body = await response.text();

    throw new Error(
      `Failed to set role '${roleName}' for '${userName}' to granted=${granted}. ${response.status} ${body}`,
    );
  }
}

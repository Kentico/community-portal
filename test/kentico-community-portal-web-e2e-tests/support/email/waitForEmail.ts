import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import type { VirtualEmail } from "./virtualEmail.js";

type ToolTextContent = {
  type?: string;
  text?: string;
};

type ToolResponse = {
  isError?: boolean;
  structuredContent?: unknown;
  content?: ToolTextContent[];
};

export type WaitForEmailOptions = {
  inbox: string;
  subjectContains: string;
  timeoutMs?: number;
  sinceId?: number;
};

export async function waitForEmail(
  client: Client,
  options: WaitForEmailOptions,
): Promise<VirtualEmail> {
  const response = (await client.callTool({
    name: "wait_for_email",
    arguments: {
      inbox: options.inbox,
      subjectContains: options.subjectContains,
      timeoutMs: options.timeoutMs ?? 30_000,
      ...(typeof options.sinceId === "number"
        ? { sinceId: options.sinceId }
        : {}),
    },
  })) as ToolResponse;

  if (response.isError) {
    throw new Error(
      [
        `wait_for_email failed for inbox '${options.inbox}'.`,
        `Expected subject containing '${options.subjectContains}'.`,
      ].join(" "),
    );
  }

  if (response.structuredContent) {
    return response.structuredContent as VirtualEmail;
  }

  const textPayload = response.content
    ?.filter((item) => item.type === "text" && typeof item.text === "string")
    .map((item) => item.text?.trim())
    .find((text) => !!text);

  if (!textPayload) {
    throw new Error(
      "wait_for_email did not return structuredContent or text content.",
    );
  }

  try {
    return JSON.parse(textPayload) as VirtualEmail;
  } catch {
    throw new Error(
      "wait_for_email returned text content that could not be parsed as JSON.",
    );
  }
}

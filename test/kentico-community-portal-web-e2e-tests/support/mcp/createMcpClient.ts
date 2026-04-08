import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StreamableHTTPClientTransport } from "@modelcontextprotocol/sdk/client/streamableHttp.js";
import { mcpBaseUrl } from "../config.js";

export async function createMcpClient() {
  if (mcpBaseUrl.startsWith("https://localhost:")) {
    process.env.NODE_TLS_REJECT_UNAUTHORIZED ??= "0";
  }

  const transport = new StreamableHTTPClientTransport(new URL(mcpBaseUrl));

  const client = new Client({
    name: "kentico-community-portal-playwright",
    version: "1.0.0",
  });

  await client.connect(transport);

  return client;
}

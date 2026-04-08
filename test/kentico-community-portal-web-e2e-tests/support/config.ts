export const appBaseUrl =
  process.env.PORTAL_BASE_URL ?? "https://localhost:45039";
export const mcpBaseUrl = process.env.PORTAL_MCP_URL ?? `${appBaseUrl}/mcp`;
export const webServerTimeoutMs = Number.parseInt(
  process.env.PLAYWRIGHT_WEB_SERVER_TIMEOUT_MS ?? "30000",
  10,
);

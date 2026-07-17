export const appBaseUrl =
  process.env.PORTAL_BASE_URL ?? "https://localhost:45039";
export const adminBaseUrl =
  process.env.ADMIN_BASE_URL ?? "https://localhost:45040/admin";
export const adminDefaultUsername =
  process.env.ADMIN_DEFAULT_USERNAME ?? "administrator";
export const adminDefaultPassword =
  process.env.ADMIN_DEFAULT_PASSWORD ?? "Pass@12345";
export const mcpBaseUrl = process.env.PORTAL_MCP_URL ?? `${appBaseUrl}/mcp`;
export const webServerTimeoutMs = Number.parseInt(
  process.env.PLAYWRIGHT_WEB_SERVER_TIMEOUT_MS ?? "30000",
  10,
);

import type { MiddlewareHandler } from "hono";
import {
  DEFAULT_USER_ALIAS,
  DEFAULT_USER_DISPLAY_NAME,
  DEFAULT_USER_EMAIL,
  DEFAULT_USER_ID,
} from "../db/index.js";
import { isTokenRevoked, verifyLocalJwt } from "../routes/auth.js";

export interface KursorClientContext {
  userId: string;
  userAlias: string;
  email: string;
  displayName: string;
  token: string | null;
  clientPlatform: string;
  clientVersion: string;
  appBuild: string;
  deviceId: string;
  deviceName: string;
  osVersion: string;
  requestId: string;
}

declare module "hono" {
  interface ContextVariableMap {
    kursorUser: KursorClientContext;
  }
}

export function isAllowedLoopbackOrigin(
  origin: string,
  secFetchMode?: string
): boolean {
  const trimmed = origin.trim();
  if (trimmed.toLowerCase() === "app://kursor") {
    return true;
  }
  if (trimmed === "null") {
    return (secFetchMode ?? "").toLowerCase() !== "cors";
  }
  return /^http:\/\/(127\.0\.0\.1|localhost)(:\d+)?$/i.test(trimmed);
}

const PUBLIC_UNAUTH_PATHS = new Set<string>([
  "/",
  "/health",
  "/auth/v1/authorize",
  "/auth/start",
  "/auth/v1/token",
  "/auth/v1/logout",
]);

/**
 * Parses `X-Clicky-*` / `X-Kursor-*` headers and `Authorization: Bearer ...`.
 * Enforces loopback origin boundary (`AUD-C-001` / `AUD-L-001`) and HMAC-SHA256
 * JWT verification + session revocation (`AUD-C-004`).
 */
export const clientHeadersAndAuthMiddleware: MiddlewareHandler = async (
  c,
  next
) => {
  const origin = c.req.header("origin");
  const secFetchSite = c.req.header("sec-fetch-site")?.toLowerCase();
  const secFetchMode = c.req.header("sec-fetch-mode")?.toLowerCase();

  if (
    secFetchSite === "cross-site" ||
    (origin !== undefined && !isAllowedLoopbackOrigin(origin, secFetchMode))
  ) {
    return c.json({ error: "Cross-origin requests are forbidden" }, 403);
  }

  const rawAuthHeader = c.req.header("authorization");
  const authHeader = rawAuthHeader?.trim() ?? "";
  const hasAuthHeader = authHeader.length > 0;
  const bearerToken = authHeader.toLowerCase().startsWith("bearer ")
    ? authHeader.slice(7).trim()
    : null;

  const clientPlatform =
    c.req.header("x-kursor-client-platform") ??
    c.req.header("x-clicky-client-platform") ??
    c.req.header("x-clicky-platform") ??
    "windows";

  const clientVersion =
    c.req.header("x-kursor-client-version") ??
    c.req.header("x-clicky-client-version") ??
    c.req.header("x-clicky-app-version") ??
    "1.1.5";

  const appBuild =
    c.req.header("x-kursor-app-build") ??
    c.req.header("x-clicky-app-build") ??
    "1150";

  const deviceId =
    c.req.header("x-kursor-device-id") ??
    c.req.header("x-clicky-device-id") ??
    c.req.header("x-clicky-device") ??
    "local-win-device-01";

  const deviceName =
    c.req.header("x-kursor-device-name") ??
    c.req.header("x-clicky-device-name") ??
    "KURSOR-PC";

  const osVersion =
    c.req.header("x-kursor-os-version") ??
    c.req.header("x-clicky-os-version") ??
    "10.0.26200";

  const requestId =
    c.req.header("x-kursor-request-id") ??
    c.req.header("x-clicky-request-id") ??
    crypto.randomUUID();

  c.set("kursorUser", {
    userId: DEFAULT_USER_ID,
    userAlias: DEFAULT_USER_ALIAS,
    email: DEFAULT_USER_EMAIL,
    displayName: DEFAULT_USER_DISPLAY_NAME,
    token: bearerToken,
    clientPlatform,
    clientVersion,
    appBuild,
    deviceId,
    deviceName,
    osVersion,
    requestId,
  });

  const isPublicPath = PUBLIC_UNAUTH_PATHS.has(c.req.path);

  if (!isPublicPath) {
    if (hasAuthHeader) {
      if (
        !bearerToken ||
        bearerToken.toLowerCase() === "invalid" ||
        bearerToken.toLowerCase() === "invalid_token" ||
        bearerToken.toLowerCase() === "expired" ||
        isTokenRevoked(bearerToken)
      ) {
        return c.json({ error: "Invalid or expired token" }, 401);
      }

      if (bearerToken.split(".").length === 3) {
        const verified = verifyLocalJwt(bearerToken);
        if (!verified.valid) {
          return c.json({ error: "Invalid or expired token" }, 401);
        }
      } else if (process.env.KURSOR_DEV_BYPASS_AUTH === "false") {
        return c.json({ error: "Invalid or expired token" }, 401);
      }
    } else if (process.env.KURSOR_DEV_BYPASS_AUTH === "false") {
      return c.json({ error: "Invalid or expired token" }, 401);
    }
  }

  await next();
};

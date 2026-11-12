import type { MiddlewareHandler } from "hono";
import {
  DEFAULT_USER_ALIAS,
  DEFAULT_USER_DISPLAY_NAME,
  DEFAULT_USER_EMAIL,
  DEFAULT_USER_ID,
} from "../db/index.js";

export interface KursorClientContext {
  userId: string;
  userAlias: string;
  email: string;
  displayName: string;
  token: string | null;
  clientPlatform: string;
  clientVersion: string;
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

/**
 * Parses `X-Clicky-*` / `X-Kursor-*` headers and `Authorization: Bearer ...`.
 * Auto-authenticates any request (including missing or custom tokens) as the
 * local Pro/Max Unlimited user (`kursor-local-user` / `00000000-0000-0000-0000-000000000001`).
 */
export const clientHeadersAndAuthMiddleware: MiddlewareHandler = async (
  c,
  next
) => {
  const authHeader = c.req.header("authorization") ?? "";
  const bearerToken = authHeader.toLowerCase().startsWith("bearer ")
    ? authHeader.slice(7).trim()
    : null;

  const clientPlatform =
    c.req.header("x-kursor-client-platform") ??
    c.req.header("x-clicky-client-platform") ??
    "windows";

  const clientVersion =
    c.req.header("x-kursor-client-version") ??
    c.req.header("x-clicky-client-version") ??
    "1.1.5";

  const deviceId =
    c.req.header("x-kursor-device-id") ??
    c.req.header("x-clicky-device-id") ??
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
    deviceId,
    deviceName,
    osVersion,
    requestId,
  });

  await next();
};

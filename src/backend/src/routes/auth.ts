import { Hono } from "hono";
import {
  db,
  DEFAULT_USER_DISPLAY_NAME,
  DEFAULT_USER_EMAIL,
  DEFAULT_USER_ID,
} from "../db/index.js";

export const authRoutes = new Hono();

function base64UrlEncode(input: string): string {
  return Buffer.from(input, "utf8")
    .toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

export function createLocalJwt(
  userId = DEFAULT_USER_ID,
  email = DEFAULT_USER_EMAIL,
  expiresInSeconds = 31536000
): { accessToken: string; refreshToken: string; expiresIn: number; expiresAt: number } {
  const nowSec = Math.floor(Date.now() / 1000);
  const expSec = nowSec + expiresInSeconds;

  const header = base64UrlEncode(JSON.stringify({ alg: "HS256", typ: "JWT" }));
  const payload = base64UrlEncode(
    JSON.stringify({
      sub: userId,
      email,
      role: "authenticated",
      aud: "authenticated",
      iat: nowSec,
      exp: expSec,
    })
  );
  const signature = base64UrlEncode("kursor-local-dev-signature");
  const accessToken = `${header}.${payload}.${signature}`;
  const refreshToken = `kursor_rt_${Buffer.from(`${userId}:${nowSec}`).toString("base64url")}`;

  return {
    accessToken,
    refreshToken,
    expiresIn: expiresInSeconds,
    expiresAt: expSec,
  };
}

function getUserPayload() {
  const row = db
    .prepare(
      "SELECT id, email, display_name, avatar_url, created_at, updated_at FROM users WHERE id = ?"
    )
    .get(DEFAULT_USER_ID) as
    | {
        id: string;
        email: string;
        display_name: string;
        avatar_url: string | null;
        created_at: string;
        updated_at: string;
      }
    | undefined;

  const id = row?.id ?? DEFAULT_USER_ID;
  const email = row?.email ?? DEFAULT_USER_EMAIL;
  const fullName = row?.display_name ?? DEFAULT_USER_DISPLAY_NAME;
  const avatarUrl =
    row?.avatar_url ?? "https://api.dicebear.com/9.x/bottts/svg?seed=kursor";
  const createdAt = row?.created_at ?? new Date().toISOString();
  const updatedAt = row?.updated_at ?? createdAt;

  return {
    id,
    aud: "authenticated",
    role: "authenticated",
    email,
    email_confirmed_at: createdAt,
    created_at: createdAt,
    updated_at: updatedAt,
    app_metadata: {
      provider: "google",
      providers: ["google", "email"],
    },
    user_metadata: {
      full_name: fullName,
      name: fullName,
      avatar_url: avatarUrl,
      email,
    },
  };
}

/**
 * GET /auth/v1/authorize
 * Immediately redirects to `heyclicky://auth/callback?code=kursor_local_code&state=...`
 * or `kursor://auth/callback?...` if requested via `redirect_to` / `redirect_uri`.
 */
authRoutes.get("/auth/v1/authorize", (c) => {
  const state = c.req.query("state") ?? "kursor_state";
  const redirectTo =
    c.req.query("redirect_to") ??
    c.req.query("redirect_uri") ??
    "heyclicky://auth/callback";

  const { accessToken, refreshToken, expiresIn } = createLocalJwt();

  const separator = redirectTo.includes("?") ? "&" : "?";
  const location =
    `${redirectTo}${separator}` +
    `code=kursor_local_code` +
    `&state=${encodeURIComponent(state)}` +
    `&access_token=${encodeURIComponent(accessToken)}` +
    `&refresh_token=${encodeURIComponent(refreshToken)}` +
    `&token_type=bearer` +
    `&expires_in=${expiresIn}`;

  return c.redirect(location, 302);
});

/**
 * GET /auth/start
 * Used by HeyClicky's `SupabaseAuthService.GetGoogleSignInUrl(state)`
 * (`{WorkerBaseUrl}/auth/start?provider=google&platform=windows&state={state}`).
 */
authRoutes.get("/auth/start", (c) => {
  const state = c.req.query("state") ?? "kursor_state";
  const scheme = c.req.query("scheme") === "kursor" ? "kursor" : "heyclicky";
  const { accessToken, refreshToken } = createLocalJwt();

  const location =
    `${scheme}://auth-callback?` +
    `access_token=${encodeURIComponent(accessToken)}` +
    `&refresh_token=${encodeURIComponent(refreshToken)}` +
    `&code=kursor_local_code` +
    `&state=${encodeURIComponent(state)}`;

  return c.redirect(location, 302);
});

/**
 * POST /auth/v1/token
 * Supports `grant_type=pkce`, `grant_type=password`, `grant_type=refresh_token`.
 */
authRoutes.post("/auth/v1/token", async (c) => {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty or form body
  }

  const grantType =
    c.req.query("grant_type") ??
    (typeof body.grant_type === "string" ? body.grant_type : "pkce");

  const { accessToken, refreshToken, expiresIn, expiresAt } = createLocalJwt();
  const user = getUserPayload();

  // Persist session in SQLite
  try {
    db.prepare(
      `INSERT INTO sessions (id, user_id, access_token, refresh_token, expires_at, created_at)
       VALUES (?, ?, ?, ?, ?, ?)`
    ).run(
      crypto.randomUUID(),
      user.id,
      accessToken,
      refreshToken,
      expiresAt,
      new Date().toISOString()
    );
  } catch {
    // Non-fatal if session insert fails
  }

  return c.json({
    access_token: accessToken,
    refresh_token: refreshToken,
    expires_in: expiresIn,
    expires_at: expiresAt,
    token_type: "bearer",
    grant_type: grantType,
    user,
  });
});

/**
 * GET /auth/v1/user
 * Returns the seeded user profile matching `SupabaseUserResponse` in `SupabaseAuthService.cs`.
 */
authRoutes.get("/auth/v1/user", (c) => {
  return c.json(getUserPayload());
});

/**
 * POST /auth/v1/logout
 * Clears active sessions and returns 200 OK.
 */
authRoutes.post("/auth/v1/logout", (c) => {
  const userCtx = c.get("kursorUser");
  if (userCtx?.token) {
    try {
      db.prepare("DELETE FROM sessions WHERE access_token = ?").run(
        userCtx.token
      );
    } catch {
      // Ignore
    }
  }
  return c.json({ ok: true }, 200);
});

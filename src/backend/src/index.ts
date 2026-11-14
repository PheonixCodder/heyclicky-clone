import dotenv from "dotenv";
dotenv.config({ override: true });
import { Hono } from "hono";
import { serve } from "@hono/node-server";
import { fileURLToPath } from "node:url";
import { DB_PATH, DEFAULT_USER_ID } from "./db/index.js";
import { gzipMiddleware } from "./middleware/gzip.js";
import {
  clientHeadersAndAuthMiddleware,
  isAllowedLoopbackOrigin,
} from "./middleware/auth.js";
import { authRoutes } from "./routes/auth.js";
import { workerRoutes } from "./routes/worker.js";
import { voiceRoutes } from "./routes/voice.js";
import { brainRoutes } from "./routes/brain.js";
import { agentRoutes } from "./routes/agents.js";

export const app = new Hono();

export const MAX_REQUEST_BODY_BYTES = 15 * 1024 * 1024;

// 1. Strict Loopback CORS & Core Middlewares (AUD-C-001 / AUD-L-001 / AUD-C-015)
app.use("*", async (c, next) => {
  c.header("Vary", "Origin");

  const origin = c.req.header("origin");
  const secFetchSite = c.req.header("sec-fetch-site")?.toLowerCase();
  const secFetchMode = c.req.header("sec-fetch-mode")?.toLowerCase();

  if (
    secFetchSite === "cross-site" ||
    (origin !== undefined && !isAllowedLoopbackOrigin(origin, secFetchMode))
  ) {
    return c.json({ error: "Cross-origin requests are forbidden" }, 403);
  }

  if (origin !== undefined) {
    c.header("Access-Control-Allow-Origin", origin);
    c.header("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
    c.header(
      "Access-Control-Allow-Headers",
      "Content-Type, Authorization, X-Clicky-Platform, X-Clicky-Device, X-Clicky-App-Version, X-Clicky-App-Build, X-Clicky-OS-Version, X-Kursor-Client-Platform, X-Kursor-Device-Id, X-Kursor-Client-Version"
    );
  }

  if (c.req.method === "OPTIONS") {
    return c.body(null, 204);
  }

  const contentLength = c.req.header("content-length");
  if (contentLength !== undefined) {
    const parsedLength = Number(contentLength);
    if (
      Number.isFinite(parsedLength) &&
      parsedLength > MAX_REQUEST_BODY_BYTES
    ) {
      return c.json(
        {
          error: "payload_too_large",
          message: "Request body exceeds 15MB limit",
        },
        413
      );
    }
  }

  await next();
  c.header("Vary", "Origin");
  if (origin !== undefined && isAllowedLoopbackOrigin(origin, secFetchMode)) {
    c.header("Access-Control-Allow-Origin", origin);
  }
});
app.use("*", gzipMiddleware);
app.use("*", clientHeadersAndAuthMiddleware);

// 2. Root & Health Check
app.get("/", (c) => {
  return c.json({
    service: "kursor-backend-worker",
    version: "1.1.5",
    status: "ok",
    user_id: DEFAULT_USER_ID,
    db_path: DB_PATH,
  });
});

app.get("/health", (c) => {
  return c.json({
    status: "ok",
    timestamp: new Date().toISOString(),
  });
});

// 3. Mount Route Modules
app.route("/", authRoutes);
app.route("/", workerRoutes);
app.route("/", voiceRoutes);
app.route("/", brainRoutes);
app.route("/", agentRoutes);

export function startServer(
  port = Number(process.env.PORT || 8787),
  hostname = process.env.HOST || "127.0.0.1"
) {
  return new Promise<{
    server: ReturnType<typeof serve>;
    port: number;
    url: string;
  }>((resolve) => {
    const server = serve(
      {
        fetch: app.fetch,
        port,
        hostname,
      },
      (info) => {
        const url = `http://${hostname}:${info.port}`;
        console.log(
          `[Kursor Backend] Listening on ${url} (SQLite: ${DB_PATH})`
        );
        resolve({ server, port: info.port, url });
      }
    );
  });
}

// Auto-start when executed directly (`tsx src/index.ts`)
const isMain =
  process.argv[1] &&
  fileURLToPath(import.meta.url).toLowerCase() ===
    process.argv[1].toLowerCase();

if (isMain) {
  startServer().catch((err) => {
    console.error("[Kursor Backend] Fatal startup error:", err);
    process.exit(1);
  });
}

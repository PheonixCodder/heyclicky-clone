import dotenv from "dotenv";
dotenv.config({ override: true });
import { Hono } from "hono";
import { cors } from "hono/cors";
import { serve } from "@hono/node-server";
import { fileURLToPath } from "node:url";
import { DB_PATH, DEFAULT_USER_ID } from "./db/index.js";
import { gzipMiddleware } from "./middleware/gzip.js";
import { clientHeadersAndAuthMiddleware } from "./middleware/auth.js";
import { authRoutes } from "./routes/auth.js";
import { workerRoutes } from "./routes/worker.js";
import { voiceRoutes } from "./routes/voice.js";
import { brainRoutes } from "./routes/brain.js";
import { agentRoutes } from "./routes/agents.js";

export const app = new Hono();

// 1. CORS & Core Middlewares
app.use("*", cors());
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

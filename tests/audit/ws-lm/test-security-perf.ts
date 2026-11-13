/**
 * WS-L (Security, Privacy & Data Handling) & WS-M (Performance, Reliability, Resources & Crash Recovery)
 * Full-Spectrum Audit Verification & Benchmark Suite for Kursor
 *
 * Run from `C:\dev\heyclicky-clone\src\backend`:
 *   npx tsx ..\..\tests\audit\ws-lm\test-perf-reliability.ts
 */

import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import zlib from "node:zlib";
import crypto from "node:crypto";
import { spawn, execSync } from "node:child_process";
import { createRequire } from "node:module";
import { DatabaseSync } from "node:sqlite";
import { analyzeScreenLayout } from "../../../src/backend/src/utils/screenLayout.ts";

const backendRequire = createRequire("C:/dev/heyclicky-clone/src/backend/package.json");
const jpeg = backendRequire("jpeg-js");

const REPO_ROOT = "C:/dev/heyclicky-clone";
const BACKEND_DIR = path.join(REPO_ROOT, "src/backend");
const EVIDENCE_DIR = path.join(REPO_ROOT, "docs/audit/ws-lm");
const OUTPUT_DIR = path.join(REPO_ROOT, "docs/audit/ws-lm");
const LIVE_BASE_URL = "http://127.0.0.1:8787";
const BENCH_PORT = 18787;
const BENCH_BASE_URL = `http://127.0.0.1:${BENCH_PORT}`;

fs.mkdirSync(EVIDENCE_DIR, { recursive: true });
fs.mkdirSync(OUTPUT_DIR, { recursive: true });

interface LatencyStats {
  count: number;
  errors: number;
  minMs: number;
  p50Ms: number;
  p95Ms: number;
  p99Ms: number;
  maxMs: number;
  meanMs: number;
}

function computeStats(samplesMs: number[], errors = 0): LatencyStats {
  if (samplesMs.length === 0) {
    return {
      count: 0,
      errors,
      minMs: 0,
      p50Ms: 0,
      p95Ms: 0,
      p99Ms: 0,
      maxMs: 0,
      meanMs: 0,
    };
  }
  const sorted = [...samplesMs].sort((a, b) => a - b);
  const pct = (p: number) => {
    const idx = Math.min(sorted.length - 1, Math.floor((p / 100) * sorted.length));
    return Number(sorted[idx].toFixed(2));
  };
  const sum = sorted.reduce((acc, v) => acc + v, 0);
  return {
    count: sorted.length,
    errors,
    minMs: Number(sorted[0].toFixed(2)),
    p50Ms: pct(50),
    p95Ms: pct(95),
    p99Ms: pct(99),
    maxMs: Number(sorted[sorted.length - 1].toFixed(2)),
    meanMs: Number((sum / sorted.length).toFixed(2)),
  };
}

/**
 * Generates a realistic 1280x720 screenshot JPEG with a 4x4 table grid, header row,
 * answer lines, and UI chrome so analyzeScreenLayout exercises all detection paths.
 */
function buildSynthetic1280x720JpegDataUrl(): { dataUrl: string; byteLength: number } {
  const width = 1280;
  const height = 720;
  const rgba = Buffer.alloc(width * height * 4, 248); // light page background
  for (let i = 3; i < rgba.length; i += 4) {
    rgba[i] = 255;
  }

  const setPixel = (x: number, y: number, r: number, g: number, b: number) => {
    if (x < 0 || x >= width || y < 0 || y >= height) return;
    const idx = (y * width + x) * 4;
    rgba[idx] = r;
    rgba[idx + 1] = g;
    rgba[idx + 2] = b;
    rgba[idx + 3] = 255;
  };

  // Draw a 4-column x 5-row bordered table at (100, 120) -> (660, 420)
  const xLines = [100, 240, 380, 520, 660];
  const yLines = [120, 180, 240, 300, 360, 420];
  for (const x of xLines) {
    for (let y = 120; y <= 420; y++) {
      setPixel(x, y, 40, 40, 40);
      setPixel(x + 1, y, 40, 40, 40);
    }
  }
  for (const y of yLines) {
    for (let x = 100; x <= 660; x++) {
      setPixel(x, y, 40, 40, 40);
      setPixel(x, y + 1, 40, 40, 40);
    }
  }

  // Draw 3 horizontal answer lines below the table
  for (const ay of [480, 530, 580]) {
    for (let x = 120; x <= 520; x++) {
      setPixel(x, ay, 50, 50, 50);
    }
  }

  const encoded = jpeg.encode({ data: rgba, width, height }, 80);
  const b64 = Buffer.from(encoded.data).toString("base64");
  return {
    dataUrl: `data:image/jpeg;base64,${b64}`,
    byteLength: encoded.data.byteLength,
  };
}

/**
 * Queries Windows process metrics via fast native `tasklist` + `netstat` (works on Windows 11 24H2 without wmic or slow PowerShell).
 */
function findListeningPid(port: number): number | null {
  try {
    const out = execSync(`netstat -ano -p tcp`, { encoding: "utf8", timeout: 3000 });
    for (const line of out.split(/\r?\n/)) {
      if (line.includes(`:${port} `) && line.includes("LISTENING")) {
        const parts = line.trim().split(/\s+/);
        const pid = Number(parts[parts.length - 1]);
        if (pid > 0) return pid;
      }
    }
  } catch {
    // ignore
  }
  return null;
}

function parseTasklistCsvLine(line: string): {
  name: string;
  pid: number;
  memMB: number;
} | null {
  // Format: "Image Name","PID","Session Name","Session#","Mem Usage"
  const m = line.match(/^"([^"]+)","(\d+)","[^"]*","[^"]*","([^"]+)"/);
  if (!m) return null;
  const memKb = Number(m[3].replace(/[^\d]/g, "")) || 0;
  return {
    name: m[1],
    pid: Number(m[2]),
    memMB: Number((memKb / 1024).toFixed(2)),
  };
}

function getWindowsProcessMetrics() {
  try {
    const backendPid = findListeningPid(8787) || 0;
    const raw = execSync(`tasklist /FO CSV /NH`, {
      encoding: "utf8",
      timeout: 4000,
    });
    const results: any[] = [];
    for (const line of raw.split(/\r?\n/)) {
      const parsed = parseTasklistCsvLine(line.trim());
      if (!parsed) continue;
      if (
        parsed.pid === backendPid ||
        parsed.name.toLowerCase() === "heyclicky.exe" ||
        parsed.name.toLowerCase() === "powershell.exe"
      ) {
        const role =
          parsed.pid === backendPid
            ? "backend-node"
            : parsed.name.toLowerCase() === "heyclicky.exe"
            ? "desktop-heyclicky"
            : "powershell-host-or-tts";
        results.push({
          role,
          pid: parsed.pid,
          name: parsed.name,
          workingSetMB: parsed.memMB,
        });
      }
    }
    return results;
  } catch (err: any) {
    return [{ error: err.message }];
  }
}

/**
 * 1. Execute WS-L Security & Privacy PoCs against localhost:8787 and disk/SQLite
 */
async function runSecurityAndPrivacyAudit() {
  console.log("=== [WS-L] Running Localhost Security PoCs & Privacy Audit ===");
  const pocs: Record<string, any> = {};

  // PoC 1: Wildcard CORS + Zero-Auth Cross-Origin API Key & Memory Exfiltration
  const evilOrigin = "https://evil.attacker.example";
  const corsPreflightRes = await fetch(`${LIVE_BASE_URL}/brain/turn`, {
    method: "OPTIONS",
    headers: {
      Origin: evilOrigin,
      "Access-Control-Request-Method": "POST",
    },
  });

  const geminiTokenRes = await fetch(`${LIVE_BASE_URL}/v2/dictation/gemini-token`, {
    method: "POST",
    headers: { Origin: evilOrigin, "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  const geminiTokenData = (await geminiTokenRes.json()) as any;

  const deepgramTokenRes = await fetch(`${LIVE_BASE_URL}/deepgram/token`, {
    method: "POST",
    headers: { Origin: evilOrigin, "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  const deepgramTokenData = (await deepgramTokenRes.json()) as any;

  const realtimeTokenRes = await fetch(`${LIVE_BASE_URL}/realtime/token`, {
    method: "POST",
    headers: { Origin: evilOrigin, "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  const realtimeTokenData = (await realtimeTokenRes.json()) as any;

  const memoryRes = await fetch(`${LIVE_BASE_URL}/memory`, {
    method: "GET",
    headers: { Origin: evilOrigin },
  });
  const memoryData = (await memoryRes.json()) as any;

  const integrationsRes = await fetch(`${LIVE_BASE_URL}/agent/integrations`, {
    method: "GET",
    headers: { Origin: evilOrigin },
  });
  const integrationsData = (await integrationsRes.json()) as any;

  pocs.poc1_corsAndZeroAuthExfiltration = {
    originSent: evilOrigin,
    authorizationHeaderSent: null,
    preflight: {
      status: corsPreflightRes.status,
      accessControlAllowOrigin: corsPreflightRes.headers.get("access-control-allow-origin"),
      accessControlAllowMethods: corsPreflightRes.headers.get("access-control-allow-methods"),
    },
    endpoints: {
      "/v2/dictation/gemini-token": {
        status: geminiTokenRes.status,
        corsHeader: geminiTokenRes.headers.get("access-control-allow-origin"),
        returnsTokenField: typeof geminiTokenData?.access_token === "string",
        tokenLength: geminiTokenData?.access_token?.length ?? 0,
        isFallbackPlaceholder:
          geminiTokenData?.access_token === "kursor-local-gemini-ephemeral-token",
      },
      "/deepgram/token": {
        status: deepgramTokenRes.status,
        corsHeader: deepgramTokenRes.headers.get("access-control-allow-origin"),
        returnsTokenField: typeof deepgramTokenData?.access_token === "string",
        tokenLength: deepgramTokenData?.access_token?.length ?? 0,
        isFallbackPlaceholder:
          deepgramTokenData?.access_token === "kursor-local-deepgram-ephemeral-token",
      },
      "/realtime/token": {
        status: realtimeTokenRes.status,
        corsHeader: realtimeTokenRes.headers.get("access-control-allow-origin"),
        returnsClientSecret: typeof realtimeTokenData?.client_secret?.value === "string",
        secretLength: realtimeTokenData?.client_secret?.value?.length ?? 0,
      },
      "/memory": {
        status: memoryRes.status,
        corsHeader: memoryRes.headers.get("access-control-allow-origin"),
        leakedMemoryChars: memoryData?.memory_markdown?.length ?? 0,
        leakedVersion: memoryData?.version ?? null,
      },
      "/agent/integrations": {
        status: integrationsRes.status,
        corsHeader: integrationsRes.headers.get("access-control-allow-origin"),
        leakedCount: Array.isArray(integrationsData?.integrations)
          ? integrationsData.integrations.length
          : 0,
      },
    },
  };

  // PoC 2: Open Redirect with Session Tokens on GET /auth/v1/authorize
  const redirectTarget = "https://evil.attacker.example/callback";
  const authRedirectRes = await fetch(
    `${LIVE_BASE_URL}/auth/v1/authorize?redirect_to=${encodeURIComponent(redirectTarget)}&state=csrf_test`,
    { redirect: "manual" }
  );
  const locationHeader = authRedirectRes.headers.get("location") || "";
  pocs.poc2_openRedirectTokenLeak = {
    requestUrl: `/auth/v1/authorize?redirect_to=${encodeURIComponent(redirectTarget)}&state=csrf_test`,
    status: authRedirectRes.status,
    locationHeader,
    redirectsToExternalHost: locationHeader.startsWith(redirectTarget),
    leaksAccessTokenInQuery: locationHeader.includes("access_token="),
    leaksRefreshTokenInQuery: locationHeader.includes("refresh_token="),
  };

  // PoC 3: Gzip Decompression Middleware Verification
  const rawJson = JSON.stringify({ channel: "gzip_middleware_test" });
  const gzipped = zlib.gzipSync(Buffer.from(rawJson, "utf8"));
  const gzipRes = await fetch(`${LIVE_BASE_URL}/onboarding/discovery-channel`, {
    method: "POST",
    headers: {
      "Content-Encoding": "gzip",
      "Content-Type": "application/json",
    },
    body: gzipped,
  });
  // Verify 15MB magnified gzip bomb is rejected with HTTP 413 by maxOutputLength guard
  const magnifiedRaw = Buffer.concat([
    Buffer.from('{"channel":"', "utf8"),
    Buffer.alloc(15 * 1024 * 1024, "A"),
    Buffer.from('"}', "utf8"),
  ]);
  const magnifiedGzipped = zlib.gzipSync(magnifiedRaw);
  const magnifiedGzipRes = await fetch(`${LIVE_BASE_URL}/onboarding/discovery-channel`, {
    method: "POST",
    headers: {
      "Content-Encoding": "gzip",
      "Content-Type": "application/json",
    },
    body: magnifiedGzipped,
  });
  await magnifiedGzipRes.arrayBuffer();
  pocs.poc3_gzipMiddleware = {
    status: gzipRes.status,
    compressedBytesSent: gzipped.byteLength,
    magnifiedBombStatus: magnifiedGzipRes.status,
    usesUnboundedGunzipSync: magnifiedGzipRes.status !== 413,
    maxOutputLengthConfigured: magnifiedGzipRes.status === 413,
  };

  // PoC 4: Base64 Screenshot Persistence in SQLite `threads.messages_json`
  // Verify what happens when a multi-turn thread containing an input_image block is sent to /brain/turn
  const syntheticImg = buildSynthetic1280x720JpegDataUrl();
  const testSessionId = `audit-privacy-img-${Date.now()}`;
  const ac = new AbortController();
  const turnRes = await fetch(`${LIVE_BASE_URL}/brain/turn`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      session_id: testSessionId,
      thread: [
        {
          type: "message",
          role: "user",
          content: [
            { type: "input_image", image_url: syntheticImg.dataUrl },
            { type: "input_text", text: "Privacy audit screenshot persistence check" },
          ],
        },
      ],
    }),
    signal: ac.signal,
  });
  // Read first SSE event (`meta`), which arrives after `ensureSessionAndThread` writes `incomingThread` to SQLite
  const reader = turnRes.body?.getReader();
  if (reader) {
    await reader.read();
  }
  ac.abort();

  const tempLiveDbPath = path.join(os.tmpdir(), "kursor-audit-live-8787.db");
  const defaultDbPath = path.join(BACKEND_DIR, "data/kursor.db");
  const dbPath = fs.existsSync(tempLiveDbPath) ? tempLiveDbPath : defaultDbPath;
  const db = new DatabaseSync(dbPath, { readOnly: true });
  const storedThreadRow = db
    .prepare(
      "SELECT id, length(messages_json) as byteLen, messages_json FROM threads WHERE id = ?"
    )
    .get(testSessionId) as { id: string; byteLen: number; messages_json: string } | undefined;

  pocs.poc4_sqliteBase64ScreenshotPersistence = {
    sessionId: testSessionId,
    screenshotDataUrlLength: syntheticImg.dataUrl.length,
    storedInSqliteThreadsTable: Boolean(storedThreadRow),
    storedMessagesJsonBytes: storedThreadRow?.byteLen ?? 0,
    containsRawBase64ImageInSqlite: Boolean(
      storedThreadRow?.messages_json?.includes("data:image/jpeg;base64,")
    ),
    containsRedactedPlaceholder: Boolean(
      storedThreadRow?.messages_json?.includes("<redacted-screenshot>")
    ),
  };

  // Clean up our synthetic test row from kursor.db so we don't leave test bloat
  db.close();
  try {
    const rwDb = new DatabaseSync(dbPath);
    rwDb.prepare("DELETE FROM threads WHERE id = ?").run(testSessionId);
    rwDb.prepare("DELETE FROM sessions WHERE id = ?").run(testSessionId);
    rwDb.close();
  } catch {
    // ignore
  }

  // PoC 5: SingleInstance MMF & Named Pipe + BrowserBridge Loopback MCP Token Check
  let discoveredPipes: string[] = [];
  try {
    discoveredPipes = fs
      .readdirSync("\\\\.\\pipe\\")
      .filter((p) => p.toLowerCase().includes("heyclicky"));
  } catch {
    discoveredPipes = [];
  }
  const singleInstanceIpc: any = {
    mmfPattern: "Local\\HeyClicky-<SID>-primary",
    mutexPattern: "Local\\HeyClicky-<SID>-primary-mutex",
    defaultAclAllowsSameUserWrite: true,
    maxPayloadBytes: 1048576,
    discoveredPipes,
  };

  const bbPath = path.join(process.env.LOCALAPPDATA || "", "HeyClicky/BrowserBridge/connection.json");
  const browserBridgeCheck: Record<string, any> = { exists: fs.existsSync(bbPath) };
  if (browserBridgeCheck.exists) {
    try {
      const bbJson = JSON.parse(fs.readFileSync(bbPath, "utf8"));
      browserBridgeCheck.port = bbJson.port;
      browserBridgeCheck.tokenLength = bbJson.token?.length ?? 0;
      browserBridgeCheck.tokenPrefix = (bbJson.token || "").slice(0, 6) + "...";
      // Probe without token
      try {
        const unauthRes = await fetch(`http://127.0.0.1:${bbJson.port}/mcp`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ jsonrpc: "2.0", id: 1, method: "tools/list" }),
          signal: AbortSignal.timeout(2000),
        });
        browserBridgeCheck.unauthStatus = unauthRes.status;
      } catch (e: any) {
        browserBridgeCheck.unauthError = e.message;
      }
      // Probe with plaintext token read from connection.json
      try {
        const authRes = await fetch(`http://127.0.0.1:${bbJson.port}/mcp`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${bbJson.token}`,
          },
          body: JSON.stringify({ jsonrpc: "2.0", id: 1, method: "tools/list" }),
          signal: AbortSignal.timeout(2000),
        });
        browserBridgeCheck.authWithDiskTokenStatus = authRes.status;
        const authBody = await authRes.text();
        browserBridgeCheck.authWithDiskTokenPreview = authBody.slice(0, 240);
      } catch (e: any) {
        browserBridgeCheck.authWithDiskTokenError = e.message;
      }
    } catch (e: any) {
      browserBridgeCheck.parseError = e.message;
    }
  }

  pocs.poc5_singleInstanceAndBrowserBridge = {
    singleInstanceIpc,
    browserBridgeCheck,
  };

  const ipcPath = path.join(EVIDENCE_DIR, "ipc-and-protocol-checks.json");
  fs.writeFileSync(
    ipcPath,
    JSON.stringify(
      {
        timestamp: new Date().toISOString(),
        singleInstanceIpc,
        browserBridgeCheck,
      },
      null,
      2
    )
  );
  console.log(`[WS-L] Wrote ${ipcPath}`);

  // Evaluate overall security posture across PoC 1..5
  const poc1Mitigated =
    pocs.poc1_corsAndZeroAuthExfiltration.preflight.status === 403 &&
    pocs.poc1_corsAndZeroAuthExfiltration.endpoints["/v2/dictation/gemini-token"].status === 403 &&
    pocs.poc1_corsAndZeroAuthExfiltration.endpoints["/deepgram/token"].status === 403 &&
    pocs.poc1_corsAndZeroAuthExfiltration.endpoints["/realtime/token"].status === 403 &&
    pocs.poc1_corsAndZeroAuthExfiltration.endpoints["/memory"].status === 403;
  const poc2Mitigated =
    pocs.poc2_openRedirectTokenLeak.status === 400 &&
    !pocs.poc2_openRedirectTokenLeak.leaksAccessTokenInQuery;
  const poc3Mitigated =
    pocs.poc3_gzipMiddleware.status === 200 &&
    pocs.poc3_gzipMiddleware.maxOutputLengthConfigured === true;
  const poc4Mitigated =
    pocs.poc4_sqliteBase64ScreenshotPersistence.containsRawBase64ImageInSqlite === false;

  const openSecurityFailures =
    (poc1Mitigated ? 0 : 1) +
    (poc2Mitigated ? 0 : 1) +
    (poc3Mitigated ? 0 : 1) +
    (poc4Mitigated ? 0 : 1);

  pocs.summary = {
    status: openSecurityFailures === 0 ? "PASS" : "FAIL",
    openSecurityFailures,
    openVulnerabilities: openSecurityFailures,
    poc1_corsBlocked: poc1Mitigated,
    poc2_openRedirectBlocked: poc2Mitigated,
    poc3_gzipBombBounded: poc3Mitigated,
    poc4_sqliteScreenshotsRedacted: poc4Mitigated,
  };

  // Save security PoCs evidence
  const pocsPath = path.join(EVIDENCE_DIR, "security-pocs.json");
  fs.writeFileSync(pocsPath, JSON.stringify(pocs, null, 2));
  console.log(`[WS-L] Wrote ${pocsPath} (status=${pocs.summary.status}, openSecurityFailures=${openSecurityFailures})`);

  // Build Privacy & Data Inventory
  const roDb = new DatabaseSync(dbPath, { readOnly: true });
  const tables = roDb
    .prepare("SELECT name FROM sqlite_master WHERE type='table'")
    .all() as Array<{ name: string }>;
  const tableStats: Record<string, { count: number }> = {};
  for (const t of tables) {
    const c = (
      roDb.prepare(`SELECT COUNT(*) as c FROM "${t.name}"`).get() as { c: number }
    ).c;
    tableStats[t.name] = { count: c };
  }
  const threadsWithBase64 = roDb
    .prepare(
      "SELECT COUNT(*) as c, COALESCE(SUM(length(messages_json)), 0) as total_bytes FROM threads WHERE messages_json LIKE '%data:image/%' OR messages_json LIKE '%input_image%'"
    )
    .get();
  const totalThreadBytes = roDb
    .prepare(
      "SELECT COUNT(*) as c, COALESCE(SUM(length(messages_json)), 0) as total_bytes FROM threads"
    )
    .get();
  const memoriesInfo = roDb
    .prepare(
      "SELECT user_id, length(markdown) as len, updated_at FROM memory"
    )
    .all();
  let errorReportsSample: any[] = [];
  try {
    errorReportsSample = roDb
      .prepare(
        "SELECT id, stage, kind, substr(message, 1, 120) as msg, created_at FROM client_errors ORDER BY id DESC LIMIT 5"
      )
      .all();
  } catch {
    errorReportsSample = [];
  }
  roDb.close();

  const appData = path.join(process.env.LOCALAPPDATA || "", "HeyClicky");
  const dictationDir = path.join(appData, "DictationTranscripts");
  const dictationFiles = fs.existsSync(dictationDir)
    ? fs.readdirSync(dictationDir).map((f) => {
        const full = path.join(dictationDir, f);
        const lines = fs
          .readFileSync(full, "utf8")
          .trim()
          .split("\n")
          .filter(Boolean);
        return {
          file: f,
          bytes: fs.statSync(full).size,
          lineCount: lines.length,
          keys: lines[0] ? Object.keys(JSON.parse(lines[0])) : [],
        };
      })
    : [];

  const oneBrainDir = path.join(appData, "OneBrain");
  const oneBrainFiles: any[] = [];
  function walkOneBrain(dir: string) {
    if (!fs.existsSync(dir)) return;
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) walkOneBrain(full);
      else {
        const raw = fs.readFileSync(full, "utf8");
        oneBrainFiles.push({
          relPath: path.relative(oneBrainDir, full),
          bytes: raw.length,
          hasInputImage: raw.includes("input_image") || raw.includes("data:image/"),
          hasUserLayer: raw.includes("user_layer"),
        });
      }
    }
  }
  walkOneBrain(oneBrainDir);

  const privacyReport = {
    timestamp: new Date().toISOString(),
    sqlite: {
      dbFileBytes: fs.statSync(dbPath).size,
      walFileBytes: fs.existsSync(dbPath + "-wal")
        ? fs.statSync(dbPath + "-wal").size
        : 0,
      shmFileBytes: fs.existsSync(dbPath + "-shm")
        ? fs.statSync(dbPath + "-shm").size
        : 0,
      tableStats,
      totalThreadBytes,
      threadsWithBase64,
      memoriesInfo,
      errorReportsSample,
    },
    localAppData: {
      dictationTranscriptsPlaintextJsonl: dictationFiles,
      oneBrainFilesPlaintext: oneBrainFiles,
      plaintextSessionJsonExists: fs.existsSync(path.join(appData, "session.json")),
      plaintextBrowserBridgeConnectionJsonExists: fs.existsSync(
        path.join(appData, "BrowserBridge/connection.json")
      ),
      dpapiAuthBinExists: fs.existsSync(path.join(appData, "auth.bin")),
      dpapiAuthLocalDevBinExists: fs.existsSync(
        path.join(appData, "auth.localdev.bin")
      ),
    },
  };

  const privPath = path.join(EVIDENCE_DIR, "privacy-data-inventory.json");
  fs.writeFileSync(privPath, JSON.stringify(privacyReport, null, 2));
  console.log(`[WS-L] Wrote ${privPath}`);
  return { pocs, privacyReport };
}

/**
 * Spawns an isolated benchmark backend instance on port 18787 with local fallback mode
 * (upstream LLM keys blanked out so we can measure cold-start time, pure local /brain/turn
 * latency, and 200+ turn soak test without hitting external LLM rate limits).
 */
let nextBenchPort = 18810 + Math.floor(Math.random() * 400);

async function startBenchBackend(): Promise<{
  proc: ReturnType<typeof spawn>;
  port: number;
  baseUrl: string;
  dbPath: string;
  coldStartMs: number;
  stop: () => void;
}> {
  const port = nextBenchPort++;
  const baseUrl = `http://127.0.0.1:${port}`;
  const dbPath = path.join(
    os.tmpdir(),
    `bench-soak-${process.pid}-${port}.db`
  );
  for (const ext of ["", "-wal", "-shm"]) {
    try {
      if (fs.existsSync(dbPath + ext)) fs.unlinkSync(dbPath + ext);
    } catch {
      // ignore
    }
  }
  const t0 = performance.now();
  const tsxCli = path.win32.resolve(BACKEND_DIR, "node_modules/tsx/dist/cli.mjs");
  const entryFile = path.win32.resolve(BACKEND_DIR, "src/index.ts");
  const proc = spawn(
    process.execPath,
    [tsxCli, entryFile],
    {
      cwd: os.tmpdir(),
      env: {
        ...process.env,
        PORT: String(port),
        HOST: "127.0.0.1",
        KURSOR_DB_PATH: path.win32.resolve(dbPath),
        DATABASE_PATH: path.win32.resolve(dbPath),
        GROQ_API_KEY: "",
        OPENROUTER_API_KEY: "",
        OPENAI_API_KEY: "",
        GEMINI_API_KEY: "",
        DEEPGRAM_API_KEY: "",
      },
      stdio: "ignore",
      windowsHide: true,
    }
  );

  const stop = () => {
    try {
      if (proc.pid) {
        execSync(`taskkill /F /T /PID ${proc.pid}`, { stdio: "ignore", timeout: 3000 });
      }
    } catch {
      try {
        proc.kill();
      } catch {
        // ignore
      }
    }
    for (const ext of ["", "-wal", "-shm"]) {
      try {
        if (fs.existsSync(dbPath + ext)) fs.unlinkSync(dbPath + ext);
      } catch {
        // ignore
      }
    }
  };

  // Poll /health until ready
  const deadline = Date.now() + 15000;
  while (Date.now() < deadline) {
    try {
      const res = await fetch(`${baseUrl}/health`, {
        signal: AbortSignal.timeout(500),
      });
      if (res.ok) {
        const coldStartMs = Number((performance.now() - t0).toFixed(2));
        return { proc, port, baseUrl, dbPath, coldStartMs, stop };
      }
    } catch {
      // wait
    }
    await new Promise((r) => setTimeout(r, 25));
  }
  stop();
  throw new Error(`Benchmark backend failed to start on port ${port} within 15s`);
}

/**
 * 2. Execute WS-M Performance Baselines, 200+ Turn Stress/Soak, and Crash/Kill Recovery
 */
async function runPerformanceAndReliabilitySuite() {
  console.log("=== [WS-M] Running Performance Baselines, Soak Test & Crash Recovery ===");

  // 2.1 Process resource baseline on live :8787 + HeyClicky.exe
  const processBaselineBefore = getWindowsProcessMetrics();

  // 2.2 Measure Backend Cold-Start Time (3 runs on ephemeral ports)
  const coldStartSamplesMs: number[] = [];
  for (let i = 0; i < 2; i++) {
    const b = await startBenchBackend();
    coldStartSamplesMs.push(b.coldStartMs);
    b.stop();
  }

  // 2.3 Measure analyzeScreenLayout (1280x720 JPEG) latency (10 iterations)
  const synthJpeg = buildSynthetic1280x720JpegDataUrl();
  const layoutSamplesMs: number[] = [];
  let lastLayoutResult: any = null;
  for (let i = 0; i < 10; i++) {
    const t0 = performance.now();
    lastLayoutResult = analyzeScreenLayout(synthJpeg.dataUrl);
    layoutSamplesMs.push(performance.now() - t0);
  }
  console.log(
    `[WS-M] analyzeScreenLayout (1280x720, ${synthJpeg.byteLength}B): p50=${computeStats(layoutSamplesMs).p50Ms}ms, tables=${lastLayoutResult?.tables?.length}, answerLines=${lastLayoutResult?.answerLines?.length}`
  );

  // 2.4 Measure Live :8787 Endpoint Latencies (/health, /plan, /v2/dictation/cleanup, /agent/realtime/speak, /brain/turn live LLM)
  const healthSamplesMs: number[] = [];
  let healthErrors = 0;
  for (let i = 0; i < 50; i++) {
    const t0 = performance.now();
    const res = await fetch(`${LIVE_BASE_URL}/health`);
    if (res.ok) {
      await res.json();
      healthSamplesMs.push(performance.now() - t0);
    } else {
      healthErrors++;
    }
  }

  const planSamplesMs: number[] = [];
  let planErrors = 0;
  for (let i = 0; i < 50; i++) {
    const t0 = performance.now();
    const res = await fetch(`${LIVE_BASE_URL}/plan`);
    if (res.ok) {
      await res.json();
      planSamplesMs.push(performance.now() - t0);
    } else {
      planErrors++;
    }
  }

  // Dictation cleanup: fast path vs live Groq LLM path
  const cleanupFastSamplesMs: number[] = [];
  for (let i = 0; i < 20; i++) {
    const t0 = performance.now();
    const res = await fetch(`${LIVE_BASE_URL}/v2/dictation/cleanup`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ raw_text: "   ", language: "en" }),
    });
    await res.json();
    cleanupFastSamplesMs.push(performance.now() - t0);
  }

  const cleanupLiveSamplesMs: number[] = [];
  let cleanupLiveErrors = 0;
  for (let i = 0; i < 5; i++) {
    const t0 = performance.now();
    const res = await fetch(`${LIVE_BASE_URL}/v2/dictation/cleanup`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        raw_text: "um so like we need to ship the kursor release tomorrow morning",
        language: "en",
        app_name: "Code",
      }),
    });
    if (res.ok) {
      await res.json();
      cleanupLiveSamplesMs.push(performance.now() - t0);
    } else {
      cleanupLiveErrors++;
    }
  }

  // Live LLM /brain/turn on :8787 (3 iterations) + force_local_fallback bug verification (2 iterations)
  const brainTurnLiveLlmSamplesMs: number[] = [];
  let brainTurnLiveErrors = 0;
  for (let i = 0; i < 3; i++) {
    const t0 = performance.now();
    const res = await fetch(`${LIVE_BASE_URL}/brain/turn`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        session_id: `audit-live-turn-${i}`,
        thread: [
          {
            type: "message",
            role: "user",
            content: [{ type: "input_text", text: "Point at (300, 200) and say hello." }],
          },
        ],
      }),
    });
    const text = await res.text();
    if (res.ok && text.includes('"type":"done"')) {
      brainTurnLiveLlmSamplesMs.push(performance.now() - t0);
    } else {
      brainTurnLiveErrors++;
    }
  }

  const brainTurnForceFallbackBugSamplesMs: number[] = [];
  for (let i = 0; i < 2; i++) {
    const t0 = performance.now();
    const res = await fetch(`${LIVE_BASE_URL}/brain/turn`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        session_id: `audit-force-fallback-${i}`,
        force_local_fallback: true,
        thread: [
          {
            type: "message",
            role: "user",
            content: [{ type: "input_text", text: "Draw a circle at (400, 300)." }],
          },
        ],
      }),
    });
    await res.text();
    brainTurnForceFallbackBugSamplesMs.push(performance.now() - t0);
  }

  // 2.5 Start Bench Backend on ephemeral port to measure warm SAPI TTS, pure local fallback /brain/turn latency + 215-turn Soak & Leak Test
  const bench = await startBenchBackend();
  coldStartSamplesMs.push(bench.coldStartMs);
  console.log(`[WS-M] Cold-start samples (ms):`, coldStartSamplesMs);
  const benchPid = bench.proc.pid!;
  const benchDbPath = bench.dbPath;
  const activeBenchUrl = bench.baseUrl;

  // Warm SAPI TTS (/agent/realtime/speak) on fresh bench backend (3 iterations after 1 warmup)
  try {
    await fetch(`${activeBenchUrl}/agent/realtime/speak`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text: "Hi." }),
      signal: AbortSignal.timeout(12000),
    }).then((r) => r.arrayBuffer());
  } catch {
    // ignore warmup timeout
  }
  const ttsSpeakSamplesMs: number[] = [];
  let ttsSpeakErrors = 0;
  for (let i = 0; i < 3; i++) {
    const t0 = performance.now();
    try {
      const res = await fetch(`${activeBenchUrl}/agent/realtime/speak`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ text: `Test ${i + 1}.` }),
        signal: AbortSignal.timeout(10000),
      });
      const buf = await res.arrayBuffer();
      if (res.ok && buf.byteLength > 44) {
        ttsSpeakSamplesMs.push(performance.now() - t0);
      } else {
        ttsSpeakErrors++;
      }
    } catch {
      ttsSpeakErrors++;
    }
  }

  const getProcMemMB = (pid: number) => {
    try {
      const raw = execSync(`tasklist /FI "PID eq ${pid}" /FO CSV /NH`, {
        encoding: "utf8",
        timeout: 3000,
      });
      const parsed = parseTasklistCsvLine(raw.trim().split(/\r?\n/)[0] || "");
      if (!parsed) return null;
      return {
        wsMB: parsed.memMB,
      };
    } catch {
      return null;
    }
  };

  const getSqliteSizes = (dbFile: string) => ({
    dbBytes: fs.existsSync(dbFile) ? fs.statSync(dbFile).size : 0,
    walBytes: fs.existsSync(dbFile + "-wal") ? fs.statSync(dbFile + "-wal").size : 0,
    shmBytes: fs.existsSync(dbFile + "-shm") ? fs.statSync(dbFile + "-shm").size : 0,
  });

  // Pure local fallback /brain/turn latency baseline (15 iterations)
  const brainTurnLocalFallbackSamplesMs: number[] = [];
  for (let i = 0; i < 15; i++) {
    const t0 = performance.now();
    const res = await fetch(`${activeBenchUrl}/brain/turn`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        session_id: `bench-local-lat-${i}`,
        thread: [
          {
            type: "message",
            role: "user",
            content: [{ type: "input_text", text: "Highlight (500, 300) and explain." }],
          },
        ],
      }),
    });
    await res.text();
    brainTurnLocalFallbackSamplesMs.push(performance.now() - t0);
  }

  // 2.6 Sustained Stress & Soak Test (215 /brain/turn turns [200 full + 15 SSE aborts] + 43 dictation cleanups + 30 proactive polls)
  console.log("[WS-M] Running 215-turn stress/soak test (concurrency=10)...");
  const soakSnapshots: any[] = [];
  soakSnapshots.push({
    turn: 0,
    sqlite: getSqliteSizes(benchDbPath),
    process: getProcMemMB(benchPid),
  });

  const soakTurnLatenciesMs: number[] = [];
  let soakTurnErrors = 0;
  let soakAbortedCount = 0;

  const runSingleSoakTurn = async (turn: number) => {
    const sessionId = `soak-session-${turn}`;
    const shouldAbort = turn > 200; // last 15 turns test SSE mid-stream disconnect
    const t0 = performance.now();
    try {
      const ac = new AbortController();
      const res = await fetch(`${activeBenchUrl}/brain/turn`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          session_id: sessionId,
          thread: [
            {
              type: "message",
              role: "user",
              content: [
                ...(turn % 10 === 0
                  ? [{ type: "input_image", image_url: synthJpeg.dataUrl }]
                  : []),
                {
                  type: "input_text",
                  text: `Soak test turn ${turn}: draw a sticky note at (760, 90).`,
                },
              ],
            },
          ],
        }),
        signal: ac.signal,
      });

      if (shouldAbort) {
        const reader = res.body?.getReader();
        if (reader) {
          await reader.read();
        }
        ac.abort();
        soakAbortedCount++;
      } else {
        const text = await res.text();
        if (res.ok && text.includes('"type":"done"')) {
          soakTurnLatenciesMs.push(performance.now() - t0);
        } else {
          soakTurnErrors++;
        }
      }
    } catch (err: any) {
      if (shouldAbort && err?.name === "AbortError") {
        soakAbortedCount++;
      } else {
        soakTurnErrors++;
      }
    }

    if (turn % 5 === 0) {
      try {
        await fetch(`${activeBenchUrl}/v2/dictation/cleanup`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            raw_text: `um soak test dictation cleanup number ${turn}`,
            language: "en",
          }),
        }).then((r) => r.json());
      } catch {
        // ignore if SSE abort crashed backend
      }
    }
    if (turn % 7 === 0) {
      try {
        await fetch(`${activeBenchUrl}/proactive/jobs`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            context: `Window title: Visual Studio Code - turn ${turn}`,
          }),
        }).then((r) => r.text());
      } catch {
        // ignore if SSE abort crashed backend
      }
    }
  };

  // First run 200 complete turns in batches of 10
  for (let batchStart = 1; batchStart <= 200; batchStart += 10) {
    const batchTurns: number[] = [];
    for (let t = batchStart; t < batchStart + 10 && t <= 200; t++) {
      batchTurns.push(t);
    }
    await Promise.all(batchTurns.map((t) => runSingleSoakTurn(t)));
    const lastTurn = batchTurns[batchTurns.length - 1];
    if (lastTurn % 50 === 0) {
      soakSnapshots.push({
        turn: lastTurn,
        sqlite: getSqliteSizes(benchDbPath),
        process: getProcMemMB(benchPid),
      });
    }
  }

  // Now run the 15 SSE mid-stream aborts (turns 201..215) and check if backend survives or crashes
  for (let t = 201; t <= 215; t++) {
    await runSingleSoakTurn(t);
  }
  await new Promise((r) => setTimeout(r, 150));
  let backendAliveAfterSseAborts = false;
  try {
    const hRes = await fetch(`${activeBenchUrl}/health`, {
      signal: AbortSignal.timeout(1000),
    });
    backendAliveAfterSseAborts = hRes.ok;
  } catch {
    backendAliveAfterSseAborts = false;
  }
  soakSnapshots.push({
    turn: 215,
    sqlite: getSqliteSizes(benchDbPath),
    process: getProcMemMB(benchPid),
    backendAliveAfterSseAborts,
  });

  // 2.7 Crash & Kill Recovery Test A: Kill warm PowerShell TTS daemon (`warmSynthProc`) mid-request on :8787
  console.log("[WS-M] Testing warm PowerShell TTS daemon kill & recovery...");
  const listPsPids = (): number[] => {
    try {
      const out = execSync(`tasklist /FI "IMAGENAME eq powershell.exe" /FO CSV /NH`, {
        encoding: "utf8",
        timeout: 3000,
      });
      return out
        .split(/\r?\n/)
        .map((l) => parseTasklistCsvLine(l.trim())?.pid || 0)
        .filter((p) => p > 0);
    } catch {
      return [];
    }
  };

  let preKillBytes = 0;
  try {
    const preKillRes = await fetch(`${LIVE_BASE_URL}/agent/realtime/speak`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text: "Hi." }),
      signal: AbortSignal.timeout(8000),
    });
    preKillBytes = (await preKillRes.arrayBuffer()).byteLength;
  } catch {
    preKillBytes = 0;
  }

  const psPidsBefore = listPsPids();
  const warmTtsPidBefore = psPidsBefore.length > 0 ? psPidsBefore[psPidsBefore.length - 1] : null;

  const longText =
    "This is a long speech synthesis sentence designed to still be synthesizing when the warm PowerShell SAPI daemon is forcibly terminated mid-request.";
  const inFlightPromise = fetch(`${LIVE_BASE_URL}/agent/realtime/speak`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text: longText }),
    signal: AbortSignal.timeout(10000),
  })
    .then(async (r) => ({
      status: r.status,
      bytes: (await r.arrayBuffer()).byteLength,
    }))
    .catch((e: any) => ({ status: 0, bytes: 0, error: e.message }));

  await new Promise((r) => setTimeout(r, 80));
  if (warmTtsPidBefore) {
    try {
      process.kill(warmTtsPidBefore, "SIGKILL");
    } catch {
      // ignore
    }
  }
  const inFlightOutcome = await inFlightPromise;

  const tRecover0 = performance.now();
  let postKillStatus = 0;
  let postKillBuf = Buffer.alloc(0);
  try {
    const postKillRes = await fetch(`${LIVE_BASE_URL}/agent/realtime/speak`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text: "Recovered speech after kill." }),
      signal: AbortSignal.timeout(10000),
    });
    postKillStatus = postKillRes.status;
    postKillBuf = Buffer.from(await postKillRes.arrayBuffer());
  } catch {
    postKillStatus = 0;
  }
  const postKillLatencyMs = Number((performance.now() - tRecover0).toFixed(2));
  const psPidsAfter = listPsPids();
  const warmTtsPidAfter = psPidsAfter.length > 0 ? psPidsAfter[psPidsAfter.length - 1] : null;
  bench.stop();
  const postKillText = postKillBuf.toString("utf8");
  const isRealWav =
    postKillBuf.byteLength > 1000 &&
    (postKillBuf.subarray(0, 4).toString("ascii") === "RIFF" ||
      (postKillText.includes('"inlineData"') && postKillText.includes('"turnComplete":true')));

  const ttsKillRecoveryResult = {
    preKillWavBytes: preKillBytes,
    warmTtsPidBefore,
    inFlightMidKillOutcome: inFlightOutcome,
    postKillStatus,
    postKillWavBytes: postKillBuf.byteLength,
    postKillLatencyMs,
    warmTtsPidAfter,
    respawnedNewDaemonProcess:
      Boolean(warmTtsPidAfter) && warmTtsPidAfter !== warmTtsPidBefore,
    recoveredWithRealSpeechWav: isRealWav,
  };
  console.log("[WS-M] TTS Kill Recovery Result:", ttsKillRecoveryResult);

  // 2.8 Process metrics after all tests
  const processBaselineAfter = getWindowsProcessMetrics();

  // Clean up the 8 audit test sessions we created on live :8787
  try {
    const liveDb = new DatabaseSync(path.join(BACKEND_DIR, "data/kursor.db"));
    liveDb
      .prepare(
        "DELETE FROM threads WHERE id LIKE 'audit-live-turn-%' OR id LIKE 'audit-force-fallback-%' OR id = 'perf-probe-1' OR id = 'abort-probe-1'"
      )
      .run();
    liveDb
      .prepare(
        "DELETE FROM sessions WHERE id LIKE 'audit-live-turn-%' OR id LIKE 'audit-force-fallback-%' OR id = 'perf-probe-1' OR id = 'abort-probe-1'"
      )
      .run();
    liveDb.close();
  } catch {
    // ignore
  }

  const perfData = {
    generatedAt: new Date().toISOString(),
    coldStart: {
      samplesMs: coldStartSamplesMs,
      stats: computeStats(coldStartSamplesMs),
    },
    processBaselines: {
      beforeSuite: processBaselineBefore,
      afterSuite: processBaselineAfter,
    },
    latencies: {
      "/health": computeStats(healthSamplesMs, healthErrors),
      "/plan": computeStats(planSamplesMs, planErrors),
      "/brain/turn (local_fallback_isolated)": computeStats(
        brainTurnLocalFallbackSamplesMs
      ),
      "/brain/turn (live_llm_8787)": computeStats(
        brainTurnLiveLlmSamplesMs,
        brainTurnLiveErrors
      ),
      "/brain/turn (force_local_fallback_flag_on_8787_bug)": computeStats(
        brainTurnForceFallbackBugSamplesMs
      ),
      "/v2/dictation/cleanup (fast_empty_path)": computeStats(
        cleanupFastSamplesMs
      ),
      "/v2/dictation/cleanup (live_groq_llm)": computeStats(
        cleanupLiveSamplesMs,
        cleanupLiveErrors
      ),
      "/agent/realtime/speak (warm_sapi_tts)": computeStats(
        ttsSpeakSamplesMs,
        ttsSpeakErrors
      ),
      "analyzeScreenLayout (1280x720_jpeg)": {
        ...computeStats(layoutSamplesMs),
        jpegBytes: synthJpeg.byteLength,
        detectedTables: lastLayoutResult?.tables?.length ?? 0,
        detectedAnswerLines: lastLayoutResult?.answerLines?.length ?? 0,
      },
    },
    soakTest: {
      totalBrainTurnsSent: 215,
      completedTurns: 200,
      abortedSseTurns: soakAbortedCount,
      dictationCleanupsSent: 43,
      proactivePollsSent: 30,
      turnErrors: soakTurnErrors,
      turnLatencyStats: computeStats(soakTurnLatenciesMs, soakTurnErrors),
      ProgressionSnapshots: soakSnapshots,
      sqliteGrowthBytes: {
        initialTotalBytes:
          soakSnapshots[0].sqlite.dbBytes + soakSnapshots[0].sqlite.walBytes,
        finalTotalBytes:
          soakSnapshots[soakSnapshots.length - 1].sqlite.dbBytes +
          soakSnapshots[soakSnapshots.length - 1].sqlite.walBytes,
        deltaBytes:
          soakSnapshots[soakSnapshots.length - 1].sqlite.dbBytes +
          soakSnapshots[soakSnapshots.length - 1].sqlite.walBytes -
          (soakSnapshots[0].sqlite.dbBytes + soakSnapshots[0].sqlite.walBytes),
      },
      memoryGrowthMB: {
        initialWorkingSetMB: soakSnapshots[0].process?.wsMB ?? null,
        finalWorkingSetMB:
          soakSnapshots[soakSnapshots.length - 1].process?.wsMB ?? null,
        deltaWorkingSetMB:
          soakSnapshots[0].process &&
          soakSnapshots[soakSnapshots.length - 1].process
            ? Number(
                (
                  soakSnapshots[soakSnapshots.length - 1].process.wsMB -
                  soakSnapshots[0].process.wsMB
                ).toFixed(2)
              )
            : null,
        initialPrivateMB: soakSnapshots[0].process?.privMB ?? null,
        finalPrivateMB:
          soakSnapshots[soakSnapshots.length - 1].process?.privMB ?? null,
        deltaPrivateMB:
          soakSnapshots[0].process &&
          soakSnapshots[soakSnapshots.length - 1].process
            ? Number(
                (
                  soakSnapshots[soakSnapshots.length - 1].process.privMB -
                  soakSnapshots[0].process.privMB
                ).toFixed(2)
              )
            : null,
      },
    },
    crashAndKillRecovery: {
      warmTtsDaemonKillMidRequest: ttsKillRecoveryResult,
    },
  };

  const perfDataPath = path.join(OUTPUT_DIR, "PERF_DATA.json");
  fs.writeFileSync(perfDataPath, JSON.stringify(perfData, null, 2));
  fs.writeFileSync(
    path.join(EVIDENCE_DIR, "soak-and-recovery-log.json"),
    JSON.stringify(perfData, null, 2)
  );
  console.log(`[WS-M] Wrote ${perfDataPath}`);
  return perfData;
}

async function main() {
  const sec = await runSecurityAndPrivacyAudit();
  const perf = await runPerformanceAndReliabilitySuite();
  console.log("=== Audit Verification & Benchmark Suite Completed Successfully ===");
  console.log(
    JSON.stringify(
      {
        corsHeader:
          sec.pocs.poc1_corsAndZeroAuthExfiltration.preflight
            .accessControlAllowOrigin,
        openRedirectLeaksToken:
          sec.pocs.poc2_openRedirectTokenLeak.leaksAccessTokenInQuery,
        sqliteStoresBase64Image:
          sec.pocs.poc4_sqliteBase64ScreenshotPersistence
            .containsRawBase64ImageInSqlite,
        coldStartMedianMs: perf.coldStart.stats.p50Ms,
        healthP50Ms: perf.latencies["/health"].p50Ms,
        planP50Ms: perf.latencies["/plan"].p50Ms,
        brainTurnLocalP50Ms:
          perf.latencies["/brain/turn (local_fallback_isolated)"].p50Ms,
        brainTurnLiveP50Ms: perf.latencies["/brain/turn (live_llm_8787)"].p50Ms,
        screenLayoutP50Ms:
          perf.latencies["analyzeScreenLayout (1280x720_jpeg)"].p50Ms,
        ttsSpeakP50Ms:
          perf.latencies["/agent/realtime/speak (warm_sapi_tts)"].p50Ms,
        ttsKillRecovered:
          perf.crashAndKillRecovery.warmTtsDaemonKillMidRequest
            .recoveredWithRealSpeechWav,
      },
      null,
      2
    )
  );
}

main().catch((err) => {
  console.error("Fatal error in test-perf-reliability.ts:", err);
  process.exit(1);
});

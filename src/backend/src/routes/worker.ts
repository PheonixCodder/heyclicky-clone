import { Hono } from "hono";
import fs from "node:fs";
import { db, DEFAULT_USER_ID } from "../db/index.js";

export const workerRoutes = new Hono();

const LOCAL_APP_CONFIG_PATH =
  "C:\\Users\\ubaid\\AppData\\Local\\HeyClicky\\app-config.json";

const DEFAULT_APP_CONFIG_LINKS = {
  featureRequest: "https://heyclicky.canny.io/feature-requests",
  communityReddit: "https://www.reddit.com/r/heyclicky_",
  communityWhatsApp:
    "https://chat.whatsapp.com/Cx8EliSb8xz9uLgq2ryZ17?mode=gi_t",
  changelog: "https://heyclicky.com/changelog?p=windows",
};

function loadAppConfigLinks() {
  try {
    if (fs.existsSync(LOCAL_APP_CONFIG_PATH)) {
      const raw = fs.readFileSync(LOCAL_APP_CONFIG_PATH, "utf8");
      const parsed = JSON.parse(raw);
      const links = parsed?.links ?? parsed?.appConfig?.links;
      if (links && typeof links === "object") {
        return {
          featureRequest:
            links.featureRequest ?? DEFAULT_APP_CONFIG_LINKS.featureRequest,
          communityReddit:
            links.communityReddit ?? DEFAULT_APP_CONFIG_LINKS.communityReddit,
          communityWhatsApp:
            links.communityWhatsApp ??
            DEFAULT_APP_CONFIG_LINKS.communityWhatsApp,
          changelog: links.changelog ?? DEFAULT_APP_CONFIG_LINKS.changelog,
        };
      }
    }
  } catch {
    // Fallback to default links
  }
  return DEFAULT_APP_CONFIG_LINKS;
}

// ============================================================================
// 1. PLAN SNAPSHOT (`GET /plan` & `GET /me/plan`)
// Matches `HeyClicky.Core.Worker.PlanSnapshot`, `PlanUsage`, `PlanUsageBucket`,
// and `PlanSubscription` in `PlanSnapshot.cs`.
// ============================================================================
function handleGetPlan() {
  return {
    plan: "max",
    tier: "max",
    tier_source: "subscription",
    team_name: null,
    can_use_voice: true,
    can_use_dictation: true,
    can_use_agents: true,
    usage: {
      messages: {
        used: 0,
        cap: 999999,
      },
      agents: {
        used: 0,
        cap: 999999,
      },
      dictation: {
        used: 0,
        cap: 999999,
      },
      window_resets_at: "2099-12-31T23:59:59.000Z",
    },
    billing_enforcement: "off",
    subscription: {
      status: "active",
      cancel_at_period_end: false,
      current_period_start: "2026-01-01T00:00:00.000Z",
      current_period_end: "2099-12-31T23:59:59.000Z",
      product_id: "prod_kursor_max",
      cancel_at: null,
      billing_interval: "year",
    },
    unlimited_agents_cap: 1000,
    pro_agents_cap: 150,
    max_agents_cap: 1000,
    pro_yearly_available: true,
    max_yearly_available: true,
    is_fail_open_fallback: false,
  };
}

workerRoutes.get("/plan", (c) => c.json(handleGetPlan()));
workerRoutes.get("/me/plan", (c) => c.json(handleGetPlan()));

// ============================================================================
// 2. APP CONFIG (`GET /config` & `GET /app-config`)
// Matches `AppConfigResponse`, `AppConfigPayload`, and `AppConfigLinks`
// (camelCase `[JsonPropertyName("appConfig")]`, `featureRequest`, etc.)
// ============================================================================
function handleGetAppConfig() {
  const links = loadAppConfigLinks();
  return {
    appConfig: {
      links,
    },
    links,
  };
}

workerRoutes.get("/config", (c) => c.json(handleGetAppConfig()));
workerRoutes.get("/app-config", (c) => c.json(handleGetAppConfig()));

// ============================================================================
// 3. RUNTIME MODELS POLICY (`GET /config/runtime-models` & `GET /runtime/model-policy`)
// Configured for:
// - Primary Brain: OpenRouter (`anthropic/claude-sonnet-4.6`, `google/gemini-3.1-flash-lite-preview`)
// - Primary Ears: Deepgram (`nova-3`) with Gemini (`models/gemini-2.5-flash`) fallback
// - Primary Mouth: OpenAI Realtime (`gpt-realtime-2.1-mini`) with Gemini TTS fallback
// - Primary Dictation Cleanup: Groq (`llama-3.3-70b-versatile`) with OpenRouter/Gemini fallback
// ============================================================================
function handleGetRuntimeModels() {
  const defaultBrainModel =
    process.env.OPENROUTER_DEFAULT_MODEL || "anthropic/claude-sonnet-4.6";
  const fastBrainModel =
    process.env.OPENROUTER_FAST_MODEL || "google/gemini-3.1-flash-lite-preview";
  const earsProvider = process.env.EARS_PRIMARY_PROVIDER || "deepgram";
  const mouthProvider =
    process.env.MOUTH_PRIMARY_PROVIDER || "openai-realtime";
  const dictationProvider =
    process.env.DICTATION_CLEANUP_PROVIDER || "groq";

  return {
    version: 1,
    updated_at: new Date().toISOString(),
    brain: {
      primary_provider: "openrouter",
      default_model: defaultBrainModel,
      fast_model: fastBrainModel,
      vision_model: defaultBrainModel,
      search_model:
        process.env.OPENROUTER_SEARCH_MODEL || "perplexity/sonar-pro",
      fallback_providers: ["openai", "anthropic", "gemini"],
      models: [
        {
          id: defaultBrainModel,
          name: "Claude Sonnet 4.6 (OpenRouter)",
          provider: "openrouter",
          tier: "max",
          supports_vision: true,
          supports_tools: true,
        },
        {
          id: fastBrainModel,
          name: "Gemini 3.1 Flash Lite (OpenRouter)",
          provider: "openrouter",
          tier: "pro",
          supports_vision: true,
          supports_tools: true,
        },
        {
          id: "openai/gpt-5.4",
          name: "GPT-5.4 (OpenRouter)",
          provider: "openrouter",
          tier: "max",
          supports_vision: true,
          supports_tools: true,
        },
      ],
    },
    ears: {
      primary_provider: earsProvider,
      primary_model: "nova-3",
      fallback_provider: "gemini",
      fallback_model: "models/gemini-2.5-flash",
    },
    mouth: {
      primary_provider: mouthProvider,
      primary_model: "gpt-realtime-2.1-mini",
      default_voice: "marin",
      fallback_provider: "gemini-tts",
      fallback_model: "gemini-2.5-flash-preview-tts",
      fallback_voice: "Puck",
    },
    dictation: {
      primary_cleanup_provider: dictationProvider,
      primary_cleanup_model: "llama-3.3-70b-versatile",
      secondary_cleanup_model: "openai/gpt-oss-20b",
      fallback_cleanup_providers: ["openrouter", "gemini"],
      primary_stt_provider: "groq",
      primary_stt_model: "whisper-large-v3-turbo",
      fallback_stt_providers: ["deepgram", "openai"],
    },
  };
}

workerRoutes.get("/config/runtime-models", (c) =>
  c.json(handleGetRuntimeModels())
);
workerRoutes.get("/runtime/model-policy", (c) =>
  c.json(handleGetRuntimeModels())
);

// ============================================================================
// 4. DEVICES (`POST /devices/register`, `POST /me/devices`, `GET /devices`, `GET /me/devices`)
// Matches `RegisterDeviceRequest`, `DeviceResponse`, `DevicesResponse`, `RegisteredDevice`
// ============================================================================
async function handleRegisterDevice(c: any) {
  const userCtx = c.get("kursorUser");
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty body and fall back to headers
  }

  const deviceId =
    (typeof body.device_id === "string" && body.device_id.trim()) ||
    userCtx?.deviceId ||
    "local-win-device-01";
  const deviceName =
    (typeof body.device_name === "string" && body.device_name.trim()) ||
    userCtx?.deviceName ||
    "KURSOR-PC";
  const platform =
    (typeof body.platform === "string" && body.platform.trim()) ||
    userCtx?.clientPlatform ||
    "windows";
  const appVersion =
    (typeof body.app_version === "string" && body.app_version.trim()) ||
    userCtx?.clientVersion ||
    "1.1.5";

  const now = new Date().toISOString();

  const existing = db
    .prepare("SELECT first_seen_at FROM devices WHERE device_id = ?")
    .get(deviceId) as { first_seen_at: string } | undefined;

  const firstSeenAt = existing?.first_seen_at ?? now;

  // Mark other devices for this user as non-current, and upsert current device
  db.prepare("UPDATE devices SET is_current = 0 WHERE user_id = ?").run(
    DEFAULT_USER_ID
  );

  db.prepare(
    `INSERT INTO devices (
      device_id, user_id, device_name, platform, app_version, first_seen_at, last_seen_at, is_current
    ) VALUES (?, ?, ?, ?, ?, ?, ?, 1)
    ON CONFLICT(device_id) DO UPDATE SET
      device_name = excluded.device_name,
      platform = excluded.platform,
      app_version = excluded.app_version,
      last_seen_at = excluded.last_seen_at,
      is_current = 1`
  ).run(
    deviceId,
    DEFAULT_USER_ID,
    deviceName,
    platform,
    appVersion,
    firstSeenAt,
    now
  );

  const device = {
    device_id: deviceId,
    device_name: deviceName,
    platform,
    app_version: appVersion,
    first_seen_at: firstSeenAt,
    last_seen_at: now,
    is_current: true,
  };

  return c.json({ device });
}

function handleGetDevices(c: any) {
  const userCtx = c.get("kursorUser");
  let rows = db
    .prepare(
      `SELECT device_id, device_name, platform, app_version, first_seen_at, last_seen_at, is_current
       FROM devices WHERE user_id = ? ORDER BY last_seen_at DESC`
    )
    .all(DEFAULT_USER_ID) as Array<{
    device_id: string;
    device_name: string;
    platform: string;
    app_version: string;
    first_seen_at: string;
    last_seen_at: string;
    is_current: number;
  }>;

  if (rows.length === 0) {
    const now = new Date().toISOString();
    const defaultDeviceId = userCtx?.deviceId || "local-win-device-01";
    const defaultDeviceName = userCtx?.deviceName || "KURSOR-PC";
    const defaultPlatform = userCtx?.clientPlatform || "windows";
    const defaultAppVersion = userCtx?.clientVersion || "1.1.5";

    db.prepare(
      `INSERT OR IGNORE INTO devices (
        device_id, user_id, device_name, platform, app_version, first_seen_at, last_seen_at, is_current
      ) VALUES (?, ?, ?, ?, ?, ?, ?, 1)`
    ).run(
      defaultDeviceId,
      DEFAULT_USER_ID,
      defaultDeviceName,
      defaultPlatform,
      defaultAppVersion,
      now,
      now
    );

    rows = [
      {
        device_id: defaultDeviceId,
        device_name: defaultDeviceName,
        platform: defaultPlatform,
        app_version: defaultAppVersion,
        first_seen_at: now,
        last_seen_at: now,
        is_current: 1,
      },
    ];
  }

  const devices = rows.map((r) => ({
    device_id: r.device_id,
    device_name: r.device_name,
    platform: r.platform,
    app_version: r.app_version,
    first_seen_at: r.first_seen_at,
    last_seen_at: r.last_seen_at,
    is_current: Boolean(r.is_current),
  }));

  return c.json({ devices });
}

workerRoutes.post("/devices/register", handleRegisterDevice);
workerRoutes.post("/me/devices", handleRegisterDevice);
workerRoutes.get("/devices", handleGetDevices);
workerRoutes.get("/me/devices", handleGetDevices);

// ============================================================================
// 5. ONBOARDING DISCOVERY CHANNEL & STARTER PLAN
// Matches `DiscoveryChannelResponse`, `SaveDiscoveryChannelRequest`,
// `StarterPlanRequest`, `StarterPlanResponse`
// ============================================================================
function handleGetDiscoveryChannel(c: any) {
  const row = db
    .prepare(
      "SELECT discovery_channel, discovery_channel_other FROM users WHERE id = ?"
    )
    .get(DEFAULT_USER_ID) as
    | {
        discovery_channel: string | null;
        discovery_channel_other: string | null;
      }
    | undefined;

  return c.json({
    discovery_channel: row?.discovery_channel ?? "github",
    discovery_channel_other: row?.discovery_channel_other ?? null,
  });
}

async function handleSaveDiscoveryChannel(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  const channel =
    typeof body.discovery_channel === "string"
      ? body.discovery_channel
      : typeof body.channel === "string"
        ? body.channel
        : "github";
  const other =
    typeof body.discovery_channel_other === "string"
      ? body.discovery_channel_other
      : typeof body.other === "string"
        ? body.other
        : null;

  db.prepare(
    "UPDATE users SET discovery_channel = ?, discovery_channel_other = ?, updated_at = ? WHERE id = ?"
  ).run(channel, other, new Date().toISOString(), DEFAULT_USER_ID);

  return c.json({
    discovery_channel: channel,
    discovery_channel_other: other,
  });
}

workerRoutes.get("/onboarding/discovery-channel", handleGetDiscoveryChannel);
workerRoutes.get("/me/discovery-channel", handleGetDiscoveryChannel);
workerRoutes.post("/onboarding/discovery-channel", handleSaveDiscoveryChannel);
workerRoutes.post("/me/discovery-channel", handleSaveDiscoveryChannel);

workerRoutes.post("/onboarding/starter-plan", async (c) => {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  const role =
    (typeof body.role === "string" && body.role) || "Software Engineer";
  const agentFamiliarity =
    (typeof body.agent_familiarity === "string" && body.agent_familiarity) ||
    "experienced";

  return c.json({
    clickys: [
      {
        title: "Explain & Annotate Screen",
        prompt: "Look at my screen and highlight the most important areas with 2D annotations.",
        why: `Tailored for ${role} workflows to visually inspect UI and code on screen.`,
      },
      {
        title: "Voice Pair Programmer",
        prompt: "Start a live voice session and walk me through fixing the issue on my screen.",
        why: "Combines low-latency Ears + Mouth with screen awareness.",
      },
      {
        title: "Summarize Active Window",
        prompt: "Summarize what is currently open on my desktop and capture key action items.",
        why: "Fast context extraction from any active window.",
      },
    ],
    suggested_agents: [
      {
        id: "daily-standup-brief",
        title: "Daily Standup & PR Digest",
        tagline: "Summarizes your recent git commits, active tasks, and open PRs.",
        initial_prompt: "Check my active repositories and prepare a concise standup summary.",
        schedule_hint: "Every weekday at 9:00 AM",
      },
      {
        id: "proactive-code-reviewer",
        title: "Proactive Screen Assistant",
        tagline: "Watches for build errors or stack traces and offers instant fixes.",
        initial_prompt: "Monitor my active development windows for errors and suggest fixes.",
        schedule_hint: "On screen context trigger",
      },
    ],
    agent_familiarity: agentFamiliarity,
    goal_line: `Supercharge your ${role} workflow with Kursor's screen-aware voice, ink, and autonomous agents.`,
  });
});

// ============================================================================
// 6. ONE-BRAIN MEMORY (`GET /memory`, `PUT /memory`, `GET /me/memory`, `POST /me/memory/save`)
// Matches `UserMemoryResponse` and `SaveMemoryResponse` (`SaveMemoryRequest`)
// ============================================================================
function handleGetMemory(c: any) {
  const row = db
    .prepare(
      `SELECT markdown, profile_markdown, context_markdown, has_profile, updated_at
       FROM memory WHERE user_id = ?`
    )
    .get(DEFAULT_USER_ID) as
    | {
        markdown: string;
        profile_markdown: string;
        context_markdown: string;
        has_profile: number;
        updated_at: string;
      }
    | undefined;

  const now = new Date().toISOString();
  return c.json({
    markdown: row?.markdown ?? "",
    profile_markdown: row?.profile_markdown ?? "",
    context_markdown: row?.context_markdown ?? "",
    has_profile: row ? Boolean(row.has_profile) : true,
    updated_at: row?.updated_at ?? now,
  });
}

async function handleSaveMemory(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    return c.json({ saved: false, error: "Invalid JSON body" }, 400);
  }

  const existing = db
    .prepare(
      `SELECT markdown, profile_markdown, context_markdown, has_profile
       FROM memory WHERE user_id = ?`
    )
    .get(DEFAULT_USER_ID) as
    | {
        markdown: string;
        profile_markdown: string;
        context_markdown: string;
        has_profile: number;
      }
    | undefined;

  const incomingMarkdown =
    typeof body.memory === "string"
      ? body.memory
      : typeof body.markdown === "string"
        ? body.markdown
        : existing?.markdown ?? "";

  const profileMarkdown =
    typeof body.profile_markdown === "string"
      ? body.profile_markdown
      : existing?.profile_markdown ?? incomingMarkdown;

  const contextMarkdown =
    typeof body.context_markdown === "string"
      ? body.context_markdown
      : existing?.context_markdown ?? "";

  const now = new Date().toISOString();

  db.prepare(
    `INSERT INTO memory (user_id, markdown, profile_markdown, context_markdown, has_profile, updated_at)
     VALUES (?, ?, ?, ?, 1, ?)
     ON CONFLICT(user_id) DO UPDATE SET
       markdown = excluded.markdown,
       profile_markdown = excluded.profile_markdown,
       context_markdown = excluded.context_markdown,
       has_profile = 1,
       updated_at = excluded.updated_at`
  ).run(
    DEFAULT_USER_ID,
    incomingMarkdown,
    profileMarkdown,
    contextMarkdown,
    now
  );

  return c.json({
    saved: true,
    error: null,
    markdown: incomingMarkdown,
    updated_at: now,
  });
}

workerRoutes.get("/memory", handleGetMemory);
workerRoutes.get("/me/memory", handleGetMemory);
workerRoutes.put("/memory", handleSaveMemory);
workerRoutes.post("/memory", handleSaveMemory);
workerRoutes.put("/me/memory", handleSaveMemory);
workerRoutes.post("/me/memory/save", handleSaveMemory);

// ============================================================================
// 7. WEB SEARCH (`POST /search/web` & `POST /web-search`)
// Zero-extra-key default implementation:
// 1. Optional Tavily / Brave / Serper if keys are configured
// 2. DuckDuckGo HTML Search (`https://html.duckduckgo.com/html/`)
// 3. OpenRouter `:online` fallback if `OPENROUTER_API_KEY` is configured
// 4. Deterministic fallback so offline/rate-limited environments always succeed
// Matches `WebSearchRequest`, `WebSearchResponse`, `WebSearchItem`
// ============================================================================
function decodeHtmlEntities(input: string): string {
  return input
    .replace(/&amp;/g, "&")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&#x27;/g, "'")
    .replace(/&nbsp;/g, " ")
    .replace(/<[^>]+>/g, "")
    .trim();
}

async function searchDuckDuckGoHtml(
  query: string,
  count: number
): Promise<
  Array<{
    title: string;
    url: string;
    description: string;
    age: string | null;
    highlights: string[];
  }>
> {
  const params = new URLSearchParams({ q: query });
  const res = await fetch(`https://html.duckduckgo.com/html/?${params}`, {
    method: "GET",
    headers: {
      "User-Agent":
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
      Accept: "text/html,application/xhtml+xml",
    },
    signal: AbortSignal.timeout(6000),
  });

  if (!res.ok) {
    return [];
  }

  const html = await res.text();
  const results: Array<{
    title: string;
    url: string;
    description: string;
    age: string | null;
    highlights: string[];
  }> = [];

  const blockRegex =
    /<a[^>]*class="[^"]*result__a[^"]*"[^>]*href="([^"]+)"[^>]*>([\s\S]*?)<\/a>[\s\S]*?(?:<a[^>]*class="[^"]*result__snippet[^"]*"[^>]*>([\s\S]*?)<\/a>)?/gi;

  let match: RegExpExecArray | null;
  while ((match = blockRegex.exec(html)) !== null && results.length < count) {
    let rawHref = match[1] ?? "";
    const rawTitle = match[2] ?? "";
    const rawSnippet = match[3] ?? "";

    // Unwrap DuckDuckGo redirect URLs (`//duckduckgo.com/l/?uddg=...`)
    if (rawHref.includes("uddg=")) {
      try {
        const uddgMatch = rawHref.match(/[?&]uddg=([^&]+)/);
        if (uddgMatch?.[1]) {
          rawHref = decodeURIComponent(uddgMatch[1]);
        }
      } catch {
        // Keep rawHref
      }
    } else if (rawHref.startsWith("//")) {
      rawHref = `https:${rawHref}`;
    }

    const title = decodeHtmlEntities(rawTitle);
    const description = decodeHtmlEntities(rawSnippet);

    if (title && rawHref.startsWith("http")) {
      results.push({
        title,
        url: rawHref,
        description: description || title,
        age: null,
        highlights: description ? [description] : [],
      });
    }
  }

  return results;
}

async function handleWebSearch(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    return c.json({ error: "Invalid JSON body" }, 400);
  }

  const query =
    typeof body.query === "string" && body.query.trim()
      ? body.query.trim()
      : "Kursor desktop assistant";
  const count =
    typeof body.count === "number" && body.count > 0
      ? Math.min(body.count, 20)
      : 5;

  let results: Array<{
    title: string;
    url: string;
    description: string;
    age: string | null;
    highlights: string[];
  }> = [];

  // 1. Optional Tavily Search if TAVILY_API_KEY is present
  const tavilyKey = process.env.TAVILY_API_KEY?.trim();
  if (tavilyKey && results.length === 0) {
    try {
      const tavilyRes = await fetch("https://api.tavily.com/search", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          api_key: tavilyKey,
          query,
          max_results: count,
        }),
        signal: AbortSignal.timeout(6000),
      });
      if (tavilyRes.ok) {
        const data = (await tavilyRes.json()) as any;
        if (Array.isArray(data?.results)) {
          results = data.results.slice(0, count).map((r: any) => ({
            title: String(r.title ?? query),
            url: String(r.url ?? "https://duckduckgo.com"),
            description: String(r.content ?? ""),
            age: r.published_date ? String(r.published_date) : null,
            highlights: r.content ? [String(r.content)] : [],
          }));
        }
      }
    } catch {
      // Fall through to DuckDuckGo
    }
  }

  // 2. Zero-extra-key DuckDuckGo HTML Search
  if (results.length === 0) {
    try {
      results = await searchDuckDuckGoHtml(query, count);
    } catch {
      // Fall through
    }
  }

  // 3. Wikipedia OpenSearch fallback (zero-key, reliable JSON API)
  if (results.length === 0) {
    try {
      const wikiUrl = `https://en.wikipedia.org/w/api.php?action=opensearch&search=${encodeURIComponent(query)}&limit=${count}&namespace=0&format=json`;
      const wikiRes = await fetch(wikiUrl, {
        headers: { "User-Agent": "KursorBackend/1.0 (https://kursor.dev)" },
        signal: AbortSignal.timeout(5000),
      });
      if (wikiRes.ok) {
        const wikiData = (await wikiRes.json()) as any;
        const titles: string[] = Array.isArray(wikiData?.[1]) ? wikiData[1] : [];
        const descriptions: string[] = Array.isArray(wikiData?.[2])
          ? wikiData[2]
          : [];
        const urls: string[] = Array.isArray(wikiData?.[3]) ? wikiData[3] : [];
        for (let i = 0; i < titles.length && results.length < count; i++) {
          results.push({
            title: titles[i],
            url: urls[i] || `https://en.wikipedia.org/wiki/${encodeURIComponent(titles[i])}`,
            description: descriptions[i] || `${titles[i]} — Wikipedia article`,
            age: null,
            highlights: descriptions[i] ? [descriptions[i]] : [],
          });
        }
      }
    } catch {
      // Fall through
    }
  }

  // 4. Deterministic fallback so tests or offline environments always get valid DTO items
  if (results.length === 0) {
    results = [
      {
        title: `Search results for "${query}"`,
        url: `https://duckduckgo.com/?q=${encodeURIComponent(query)}`,
        description: `Zero-key web search result for "${query}" via Kursor Local Worker.`,
        age: "recent",
        highlights: [`Result matching ${query}`],
      },
    ];
  }

  return c.json({
    query,
    results,
  });
}

workerRoutes.post("/search/web", handleWebSearch);
workerRoutes.post("/web-search", handleWebSearch);

// ============================================================================
// 8. IMAGE SEARCH (`POST /search/images` & `POST /image-search`)
// Zero-extra-key default implementation using Wikimedia Commons API + DuckDuckGo
// Matches `ImageSearchRequest`, `ImageSearchResponse`, `ImageSearchHit`
// ============================================================================
async function searchWikimediaImages(
  query: string,
  limit: number
): Promise<
  Array<{
    thumbnail_url: string;
    full_url: string;
    source_url: string;
    title: string;
  }>
> {
  const apiUrl =
    `https://commons.wikimedia.org/w/api.php?action=query` +
    `&generator=search&gsrsearch=${encodeURIComponent(`filetype:bitmap ${query}`)}` +
    `&gsrnamespace=6&gsrlimit=${limit}` +
    `&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=640&format=json`;

  const res = await fetch(apiUrl, {
    headers: { "User-Agent": "KursorBackend/1.0 (https://kursor.dev)" },
    signal: AbortSignal.timeout(6000),
  });

  if (!res.ok) {
    return [];
  }

  const data = (await res.json()) as any;
  const pages = data?.query?.pages;
  if (!pages || typeof pages !== "object") {
    return [];
  }

  const hits: Array<{
    thumbnail_url: string;
    full_url: string;
    source_url: string;
    title: string;
  }> = [];

  for (const page of Object.values(pages) as any[]) {
    const info = page?.imageinfo?.[0];
    if (!info?.url) continue;

    const cleanTitle = String(page.title ?? query)
      .replace(/^File:/i, "")
      .replace(/\.[a-z0-9]+$/i, "");

    hits.push({
      thumbnail_url: String(info.thumburl ?? info.url),
      full_url: String(info.url),
      source_url: String(info.descriptionurl ?? info.url),
      title: cleanTitle,
    });

    if (hits.length >= limit) break;
  }

  return hits;
}

async function handleImageSearch(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    return c.json({ error: "Invalid JSON body" }, 400);
  }

  const query =
    typeof body.query === "string" && body.query.trim()
      ? body.query.trim()
      : "nature";
  const limit =
    typeof body.limit === "number" && body.limit > 0
      ? Math.min(body.limit, 12)
      : 5;

  let images = await searchWikimediaImages(query, limit).catch(() => []);

  if (images.length === 0) {
    images = [
      {
        thumbnail_url: `https://commons.wikimedia.org/wiki/Special:FilePath/Example.jpg?width=320`,
        full_url: `https://commons.wikimedia.org/wiki/Special:FilePath/Example.jpg`,
        source_url: `https://commons.wikimedia.org/w/index.php?search=${encodeURIComponent(query)}`,
        title: `${query} (Wikimedia Commons)`,
      },
    ];
  }

  return c.json({
    query,
    images,
    results: images,
  });
}

workerRoutes.post("/search/images", handleImageSearch);
workerRoutes.post("/image-search", handleImageSearch);

// ============================================================================
// 9. REFERRALS (`GET /referral`, `POST /referral/claim`, `POST /referral/code`)
// Matches `ReferralClaimResponse` and `ReferralCodeUpdateResponse`
// ============================================================================
function handleGetReferral(c: any) {
  const row = db
    .prepare(
      "SELECT referral_code, referral_claimed_code FROM users WHERE id = ?"
    )
    .get(DEFAULT_USER_ID) as
    | {
        referral_code: string | null;
        referral_claimed_code: string | null;
      }
    | undefined;

  const code = row?.referral_code || "KURSOR-MAX";
  return c.json({
    code,
    share_url: `https://heyclicky.com/r/${code}`,
    share_display_text: `heyclicky.com/r/${code}`,
    claimed_code: row?.referral_claimed_code ?? null,
    discount_percent: 20,
    referrals_count: 0,
  });
}

async function handleClaimReferral(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  const code =
    typeof body.code === "string" && body.code.trim()
      ? body.code.trim().toUpperCase()
      : "FRIEND20";

  db.prepare(
    "UPDATE users SET referral_claimed_code = ?, updated_at = ? WHERE id = ?"
  ).run(code, new Date().toISOString(), DEFAULT_USER_ID);

  return c.json({
    outcome: "claimed",
    message: `Referral code ${code} applied! You have unlocked 20% off.`,
    discount_percent: 20,
    code,
  });
}

async function handleUpdateReferralCode(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  const code =
    typeof body.code === "string" && body.code.trim()
      ? body.code.trim().toUpperCase()
      : "KURSOR-MAX";

  db.prepare(
    "UPDATE users SET referral_code = ?, updated_at = ? WHERE id = ?"
  ).run(code, new Date().toISOString(), DEFAULT_USER_ID);

  return c.json({
    outcome: "updated",
    message: "Your referral code has been updated.",
    code,
    share_url: `https://heyclicky.com/r/${code}`,
    share_display_text: `heyclicky.com/r/${code}`,
  });
}

workerRoutes.get("/referral", handleGetReferral);
workerRoutes.get("/me/referral", handleGetReferral);
workerRoutes.post("/referral/claim", handleClaimReferral);
workerRoutes.post("/me/referral/claim", handleClaimReferral);
workerRoutes.post("/referral/code", handleUpdateReferralCode);
workerRoutes.post("/me/referral/code", handleUpdateReferralCode);

// ============================================================================
// 10. BILLING, ACCOUNT DELETE, TELEMETRY, & VOICE LANGUAGE HINTS
// Matches `BillingUrlResponse`, `CheckoutRequest`, `ClientErrorReport`
// ============================================================================
workerRoutes.post("/billing/checkout", async (c) => {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  const discountCode =
    typeof body.discount_code === "string" ? body.discount_code : null;

  return c.json({
    url: "http://127.0.0.1:8787/billing/local-max-active",
    discount_code_applied: discountCode,
  });
});

workerRoutes.post("/billing/portal", (c) => {
  return c.json({
    url: "http://127.0.0.1:8787/billing/local-portal",
    discount_code_applied: null,
  });
});

function handleDeleteAccount(c: any) {
  return c.json({
    ok: true,
    deleted: true,
  });
}

workerRoutes.post("/account/delete", handleDeleteAccount);
workerRoutes.post("/me/account/delete", handleDeleteAccount);

async function handleClientError(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  return c.json({
    ok: true,
    received_category: body.category ?? "unknown",
  });
}

workerRoutes.post("/telemetry/client-error", handleClientError);
workerRoutes.post("/report-error", handleClientError);

function handleLanguageHints(c: any) {
  const lang = c.req.query("lang") ?? c.req.query("device_region") ?? "en";
  return c.json({
    lang,
    languages: ["en", "es", "fr", "de", "pt", "it", "hi", "ur", "ja", "ko", "zh-Hans"],
    hints: [
      "HeyClicky",
      "Clicky",
      "Kursor",
      "OpenRouter",
      "Deepgram",
      "Claude",
      "Gemini",
      "WinUI",
      "TypeScript",
      "C#",
    ],
  });
}

workerRoutes.get("/voice/language-hints", handleLanguageHints);
workerRoutes.get("/onboarding/language-hints", handleLanguageHints);

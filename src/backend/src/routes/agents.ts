import { Hono } from "hono";
import crypto from "node:crypto";
import { db, DEFAULT_USER_ID } from "../db/index.js";

export const agentRoutes = new Hono();

// ============================================================================
// 0. SEED DEFAULT UNREAD NOTIFICATION IN SQLITE (IF EMPTY)
// ============================================================================
function ensureSeededNotifications(): void {
  const countRow = db
    .prepare("SELECT COUNT(*) as cnt FROM notifications WHERE user_id = ?")
    .get(DEFAULT_USER_ID) as { cnt: number } | undefined;

  if (!countRow || countRow.cnt === 0) {
    const now = new Date().toISOString();
    db.prepare(
      `INSERT INTO notifications (id, user_id, type, title, body, action_url, is_read, created_at)
       VALUES (?, ?, ?, ?, ?, ?, 0, ?)`
    ).run(
      "notif_kursor_welcome_01",
      DEFAULT_USER_ID,
      "cron",
      "Daily Workspace Digest Ready",
      "Kursor inspected your workspace and prepared your morning briefing.",
      "cron_daily_digest",
      now
    );
  }
}

ensureSeededNotifications();

// ============================================================================
// 1. PROACTIVE AGENTS & SUGGESTION CARDS
// Matches `ProactiveAgentsClient.cs`:
// - POST `/proactive/jobs` & `/proactive-agents/jobs`
// - GET `/proactive/jobs/:jobId` & `/proactive-agents/jobs/:jobId`
// - POST `/proactive/cards/resolve` & `/proactive-agents/resolution`
// - POST `/proactive/cards/status` & `/proactive-agents/suggestions/statuses`
// - POST `/proactive/morning/claim` & `/proactive-agents/invitation-claim`
// ============================================================================
interface StoredProactiveJob {
  job_id: string;
  status: "succeeded";
  state: "completed";
  reason: string;
  local_day: string;
  morning_headline: string;
  morning_headline_line: string;
  curated_evidence: string;
  suggested_agents: Array<{
    id: string;
    suggestion_id: number;
    title: string;
    description: string;
    agent_prompt: string;
    prompt: string;
    clicky_slug: string | null;
    integration: string | null;
    new_clicky: {
      name: string;
      role: string;
      description: string;
    } | null;
    routine: {
      every_minutes: number;
      title: string;
    } | null;
  }>;
  cards: Array<{
    id: string;
    suggestion_id: number;
    title: string;
    description: string;
    agent_prompt: string;
    prompt: string;
    clicky_slug: string | null;
    integration: string | null;
  }>;
}

const proactiveJobs = new Map<string, StoredProactiveJob>();
const proactiveCardStatuses = new Map<string, string>();
let nextSuggestionNumericId = 101;

function createProactiveJob(body: Record<string, any>): StoredProactiveJob {
  const jobId = `proactive_job_${crypto.randomUUID().slice(0, 12)}`;
  const reason =
    (typeof body.check_reason === "string" && body.check_reason) ||
    (typeof body.reason === "string" && body.reason) ||
    "scheduled_slot";
  const localDay =
    (typeof body.local_day === "string" && body.local_day) ||
    new Date().toISOString().slice(0, 10);

  const fgApp =
    typeof body.foreground_activity?.app_name === "string"
      ? body.foreground_activity.app_name
      : typeof body.foreground_activity === "string"
        ? body.foreground_activity
        : "VS Code";

  const id1 = nextSuggestionNumericId++;
  const id2 = nextSuggestionNumericId++;
  const strId1 = String(id1);
  const strId2 = String(id2);

  const cards = [
    {
      id: strId1,
      suggestion_id: id1,
      title: `Summarize & Audit ${fgApp} Session`,
      description: `Review recent changes in ${fgApp} and generate a concise action checklist.`,
      agent_prompt: `Inspect my recent ${fgApp} workspace activity and write a prioritized summary report.`,
      prompt: `Inspect my recent ${fgApp} workspace activity and write a prioritized summary report.`,
      clicky_slug: "workspace-auditor",
      integration: "github",
      new_clicky: {
        name: "Workspace Auditor",
        role: "Code & Workflow Analyst",
        description: "Summarizes active engineering work and spots regressions.",
      },
      routine: {
        every_minutes: 240,
        title: "Workspace Check-in",
      },
    },
    {
      id: strId2,
      suggestion_id: id2,
      title: "Morning Calendar & Inbox Triage",
      description:
        "Scan upcoming meetings and highlight urgent messages for today.",
      agent_prompt:
        "Check my upcoming calendar events and summarize key preparation notes for today.",
      prompt:
        "Check my upcoming calendar events and summarize key preparation notes for today.",
      clicky_slug: "daily-briefing",
      integration: "googlecalendar",
      new_clicky: {
        name: "Daily Briefing",
        role: "Executive Assistant",
        description: "Prepares daily schedules and meeting briefs.",
      },
      routine: {
        every_minutes: 1440,
        title: "Morning Briefing",
      },
    },
  ];

  const now = new Date().toISOString();
  for (const card of cards) {
    proactiveCardStatuses.set(card.id, "presented");
    db.prepare(
      `INSERT INTO proactive_cards (
        id, user_id, card_type, title, subtitle, prompt, payload_json, dismissed, created_at
      ) VALUES (?, ?, 'suggestion', ?, ?, ?, ?, 0, ?)
      ON CONFLICT(id) DO UPDATE SET
        title = excluded.title,
        subtitle = excluded.subtitle,
        prompt = excluded.prompt,
        payload_json = excluded.payload_json,
        dismissed = 0`
    ).run(
      card.id,
      DEFAULT_USER_ID,
      card.title,
      card.description,
      card.agent_prompt,
      JSON.stringify({ ...card, status: "presented" }),
      now
    );
  }

  const job: StoredProactiveJob = {
    job_id: jobId,
    status: "succeeded",
    state: "completed",
    reason,
    local_day: localDay,
    morning_headline: "Good morning! Here is your Kursor daily focus.",
    morning_headline_line: "2 proactive workflows are ready to run.",
    curated_evidence: `Active foreground context (${fgApp}) and connected workspace tools.`,
    suggested_agents: cards,
    cards,
  };

  proactiveJobs.set(jobId, job);
  return job;
}

async function handleStartProactiveJob(c: any) {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }
  const job = createProactiveJob(body);
  return c.json(job);
}

function handleGetProactiveJob(c: any) {
  const jobId = c.req.param("jobId");
  let job = proactiveJobs.get(jobId);
  if (!job) {
    job = createProactiveJob({ reason: "poll_fallback" });
    job.job_id = jobId;
    proactiveJobs.set(jobId, job);
  }
  return c.json(job);
}

async function handleResolveProactiveCards(c: any) {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const rawIds: Array<string | number> = Array.isArray(body.card_ids)
    ? body.card_ids
    : Array.isArray(body.suggestion_ids)
      ? body.suggestion_ids
      : body.card_id != null
        ? [body.card_id]
        : [];

  const resolution =
    (typeof body.resolution === "string" && body.resolution) ||
    (typeof body.status === "string" && body.status) ||
    "accepted";

  const reason =
    (typeof body.reason === "string" && body.reason) ||
    (typeof body.deny_reason === "string" && body.deny_reason) ||
    null;

  const isDismissed =
    resolution === "dismissed" ||
    resolution === "denied" ||
    resolution === "expired" ||
    resolution === "accepted" ||
    resolution === "approved"
      ? 1
      : 0;

  for (const rawId of rawIds) {
    const idStr = String(rawId);
    proactiveCardStatuses.set(idStr, resolution);

    const existing = db
      .prepare("SELECT payload_json FROM proactive_cards WHERE id = ?")
      .get(idStr) as { payload_json: string } | undefined;

    let payload: Record<string, any> = {};
    if (existing?.payload_json) {
      try {
        payload = JSON.parse(existing.payload_json);
      } catch {
        payload = {};
      }
    }
    payload.status = resolution;
    if (reason) payload.reason = reason;

    if (existing) {
      db.prepare(
        "UPDATE proactive_cards SET dismissed = ?, payload_json = ? WHERE id = ?"
      ).run(isDismissed, JSON.stringify(payload), idStr);
    } else {
      db.prepare(
        `INSERT INTO proactive_cards (
          id, user_id, card_type, title, subtitle, prompt, payload_json, dismissed, created_at
        ) VALUES (?, ?, 'suggestion', ?, ?, ?, ?, ?, ?)`
      ).run(
        idStr,
        DEFAULT_USER_ID,
        `Card ${idStr}`,
        "",
        "",
        JSON.stringify(payload),
        isDismissed,
        new Date().toISOString()
      );
    }
  }

  return c.json({
    ok: true,
    resolved: true,
    status: resolution,
    card_ids: rawIds.map(String),
  });
}

async function handleGetProactiveCardStatuses(c: any) {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const rawIds: Array<string | number> = Array.isArray(body.card_ids)
    ? body.card_ids
    : Array.isArray(body.suggestion_ids)
      ? body.suggestion_ids
      : [];

  const statuses: Record<string, string> = {};

  if (rawIds.length > 0) {
    for (const rawId of rawIds) {
      const idStr = String(rawId);
      const memStatus = proactiveCardStatuses.get(idStr);
      if (memStatus) {
        statuses[idStr] = memStatus;
        continue;
      }
      const row = db
        .prepare(
          "SELECT dismissed, payload_json FROM proactive_cards WHERE id = ?"
        )
        .get(idStr) as { dismissed: number; payload_json: string } | undefined;
      if (row) {
        try {
          const parsed = JSON.parse(row.payload_json);
          statuses[idStr] =
            parsed.status || (row.dismissed ? "resolved" : "active");
        } catch {
          statuses[idStr] = row.dismissed ? "resolved" : "active";
        }
      } else {
        statuses[idStr] = "active";
      }
    }
  } else {
    for (const [k, v] of proactiveCardStatuses.entries()) {
      statuses[k] = v;
    }
  }

  return c.json({
    ...statuses,
    statuses,
  });
}

async function handleMorningClaim(c: any) {
  return c.json({
    claimed: true,
    granted: true,
  });
}

agentRoutes.post("/proactive/jobs", handleStartProactiveJob);
agentRoutes.post("/proactive-agents/jobs", handleStartProactiveJob);
agentRoutes.get("/proactive/jobs/:jobId", handleGetProactiveJob);
agentRoutes.get("/proactive-agents/jobs/:jobId", handleGetProactiveJob);
agentRoutes.post("/proactive/cards/resolve", handleResolveProactiveCards);
agentRoutes.post("/proactive-agents/resolution", handleResolveProactiveCards);
agentRoutes.post("/proactive/cards/status", handleGetProactiveCardStatuses);
agentRoutes.post(
  "/proactive-agents/suggestions/statuses",
  handleGetProactiveCardStatuses
);
agentRoutes.post("/proactive/morning/claim", handleMorningClaim);
agentRoutes.post("/proactive-agents/invitation-claim", handleMorningClaim);

// ============================================================================
// 2. AGENT NOTIFICATIONS, CRONS & TURN LEASES
// Matches `AgentBackendClient.cs`, `AgentNotification.cs`, `AgentWorkerApi.cs`,
// and `AgentTurnLease.cs`.
// ============================================================================
function handleGetUnreadNotifications(c: any) {
  const rows = db
    .prepare(
      `SELECT id, type, title, body, action_url, is_read, created_at
       FROM notifications
       WHERE user_id = ? AND is_read = 0
       ORDER BY created_at DESC
       LIMIT 20`
    )
    .all(DEFAULT_USER_ID) as Array<{
    id: string;
    type: string;
    title: string;
    body: string;
    action_url: string | null;
    is_read: number;
    created_at: string;
  }>;

  const notifications = rows.map((r) => ({
    id: r.id,
    source_type: r.type || "cron",
    source_id: r.action_url || "cron_daily_digest",
    run_id: `run_${r.id}`,
    title: r.title,
    summary: r.body,
    body: r.body,
    type: r.type,
    severity: "info",
    read_at: r.is_read ? r.created_at : null,
    is_read: Boolean(r.is_read),
    created_at: r.created_at,
  }));

  return c.json({
    notifications,
    unread: notifications,
  });
}

function handleMarkNotificationRead(c: any) {
  const id = c.req.param("id");
  db.prepare(
    "UPDATE notifications SET is_read = 1 WHERE id = ? AND user_id = ?"
  ).run(id, DEFAULT_USER_ID);

  return c.json({
    ok: true,
    id,
    read: true,
  });
}

function handleMarkAllNotificationsRead(c: any) {
  db.prepare("UPDATE notifications SET is_read = 1 WHERE user_id = ?").run(
    DEFAULT_USER_ID
  );

  return c.json({
    ok: true,
    read_all: true,
  });
}

function handleGetCronDetail(c: any) {
  const cronId = c.req.param("id");
  const now = new Date();
  const nextRun = new Date(now.getTime() + 3600 * 1000).toISOString();
  const startedAt = new Date(now.getTime() - 120 * 1000).toISOString();
  const finishedAt = new Date(now.getTime() - 118 * 1000).toISOString();

  const cronObj = {
    id: cronId,
    name: "Daily Workspace & PR Digest",
    description:
      "Scans active repositories, summarizes open PRs, and prepares morning highlights.",
    enabled: true,
    schedule_kind: "cron",
    schedule_expr: "0 9 * * 1-5",
    schedule_text: "Every weekday at 9:00 AM",
    timezone: "America/Los_Angeles",
    next_run_at: nextRun,
    last_run_status: "succeeded",
    source: `export default async function run(ctx: any) {\n  return { status: "ok", summary: "Daily digest completed." };\n}`,
  };

  const runs = [
    {
      id: `run_${cronId}_01`,
      status: "succeeded",
      attempt: 1,
      created_at: startedAt,
      started_at: startedAt,
      finished_at: finishedAt,
      duration_ms: 1850,
      result_summary: "All checks green; 2 pull requests summarized.",
      error: null,
      stdout_log: "[Kursor Cron] Task executed cleanly in 1850ms.",
    },
  ];

  return c.json({
    ...cronObj,
    cron: cronObj,
    runs,
    recent_runs: runs,
  });
}

agentRoutes.get("/agents/notifications/unread", handleGetUnreadNotifications);
agentRoutes.get("/agent/notifications", handleGetUnreadNotifications);
agentRoutes.post(
  "/agents/notifications/:id/read",
  handleMarkNotificationRead
);
agentRoutes.post("/agent/notifications/:id/read", handleMarkNotificationRead);
agentRoutes.post(
  "/agents/notifications/read-all",
  handleMarkAllNotificationsRead
);
agentRoutes.post(
  "/agent/notifications/read-all",
  handleMarkAllNotificationsRead
);
agentRoutes.get("/agents/crons/:id", handleGetCronDetail);
agentRoutes.get("/agent/cron/:id", handleGetCronDetail);

// Agent Session Token (`POST /agent/session-token`)
agentRoutes.post("/agent/session-token", (c) => {
  const expiresAtUnixSeconds = Math.floor(Date.now() / 1000) + 3600;
  return c.json({
    token: `kursor_agent_tok_${crypto.randomUUID().replace(/-/g, "")}`,
    expires_at: expiresAtUnixSeconds,
    capabilities: {
      developer_model_selection: true,
    },
  });
});

// Turn Leases (`POST /agent/record-agent-launch`, `GET /agent/turn-lease/:id/status`, etc.)
interface TurnLeaseState {
  lease_id: string;
  turn_id: string;
  thread_id: string | null;
  active: boolean;
  allowed: boolean;
  status: string;
  expires_at: string;
  credits_used: number;
  included_credits: number;
  cost_usd: number;
  cost_limit_usd: number;
  requires_extra_effort: boolean;
}

const turnLeases = new Map<string, TurnLeaseState>();

agentRoutes.post("/agent/record-agent-launch", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }

  const leaseId = `lease_${crypto.randomUUID().replace(/-/g, "").slice(0, 16)}`;
  const turnId =
    (typeof body.turn_id === "string" && body.turn_id) ||
    (typeof body.client_turn_id === "string" && body.client_turn_id) ||
    `turn_${crypto.randomUUID().slice(0, 8)}`;
  const threadId =
    (typeof body.thread_id === "string" && body.thread_id) ||
    (typeof body.session_id === "string" && body.session_id) ||
    null;

  const expiresAt = new Date(Date.now() + 3600 * 1000).toISOString();

  const lease: TurnLeaseState = {
    lease_id: leaseId,
    turn_id: turnId,
    thread_id: threadId,
    active: true,
    allowed: true,
    status: "active",
    expires_at: expiresAt,
    credits_used: 0,
    included_credits: 999999,
    cost_usd: 0.0,
    cost_limit_usd: 100.0,
    requires_extra_effort: false,
  };

  turnLeases.set(leaseId, lease);
  return c.json(lease);
});

agentRoutes.get("/agent/turn-lease/:id/status", (c) => {
  const leaseId = c.req.param("id");
  const existing = turnLeases.get(leaseId);
  if (existing) {
    return c.json(existing);
  }

  const expiresAt = new Date(Date.now() + 3600 * 1000).toISOString();
  return c.json({
    lease_id: leaseId,
    turn_id: `turn_${leaseId}`,
    active: true,
    allowed: true,
    status: "active",
    expires_at: expiresAt,
    credits_used: 0,
    included_credits: 999999,
    cost_usd: 0.0,
    cost_limit_usd: 100.0,
    requires_extra_effort: false,
  });
});

agentRoutes.post("/agent/turn-lease/:id/continue", (c) => {
  const leaseId = c.req.param("id");
  const existing = turnLeases.get(leaseId);
  const expiresAt = new Date(Date.now() + 3600 * 1000).toISOString();
  const updated: TurnLeaseState = existing
    ? { ...existing, requires_extra_effort: false, active: true, status: "active" }
    : {
        lease_id: leaseId,
        turn_id: `turn_${leaseId}`,
        thread_id: null,
        active: true,
        allowed: true,
        status: "active",
        expires_at: expiresAt,
        credits_used: 0,
        included_credits: 999999,
        cost_usd: 0.0,
        cost_limit_usd: 100.0,
        requires_extra_effort: false,
      };
  turnLeases.set(leaseId, updated);
  return c.json({ ...updated, continued: true });
});

agentRoutes.post(
  "/agent/turn-lease/:id/continue-after-computer-use-consent",
  (c) => {
    const leaseId = c.req.param("id");
    const expiresAt = new Date(Date.now() + 3600 * 1000).toISOString();
    return c.json({
      continued: true,
      lease_id: leaseId,
      turn_id: `turn_${leaseId}`,
      active: true,
      allowed: true,
      status: "active",
      expires_at: expiresAt,
      credits_used: 0,
      included_credits: 999999,
      cost_usd: 0.0,
      cost_limit_usd: 100.0,
      requires_extra_effort: false,
    });
  }
);

agentRoutes.post("/agent/turn-lease/:id/complete", (c) => {
  const leaseId = c.req.param("id");
  const existing = turnLeases.get(leaseId);
  if (existing) {
    existing.status = "completed";
  }
  return c.json({ ok: true, lease_id: leaseId, status: "completed" });
});

agentRoutes.post("/agent/turn-lease/:id/refund-early-stop", (c) => {
  return c.json({ refunded: true });
});

agentRoutes.post("/codex-thread-title", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Ignore
  }
  const prompt =
    typeof body.user_prompt === "string" && body.user_prompt.trim()
      ? body.user_prompt.trim()
      : "Kursor Agent Task";
  const words = prompt.split(/\s+/).slice(0, 5).join(" ");
  return c.json({ title: words || "Kursor Task" });
});

agentRoutes.post("/codex-thread-launch", (c) => {
  return c.json({
    text_start_cue: "On it — starting your background agent now.",
  });
});

agentRoutes.post("/agent-messages", (c) => {
  return c.json({ ok: true, recorded: true });
});

// ============================================================================
// 3. CONNECTORS / COMPOSIO MCP INTEGRATIONS
// Matches `AgentIntegrationInfo.cs` AND `AgentIntegration.cs` / `AgentIntegrationsApi.cs`
// ============================================================================
interface IntegrationEntry {
  id: string;
  display_name: string;
  description: string;
  use_cases: string[];
  status: "connected" | "not_connected";
  connected_count: number;
  total_count: number;
  toolkits: Array<{
    slug: string;
    name: string;
    logo: string;
    is_connected: boolean;
  }>;
  accounts: Array<{
    connected_account_id: string;
    account_name: string;
    label: string;
    avatar_url: string | null;
    account_name_kind: string;
    status: string;
    connected_at: string;
  }>;
}

const integrationsCatalog: IntegrationEntry[] = [
  {
    id: "gmail",
    display_name: "Gmail",
    description: "Read, search, draft, and send emails with Gmail.",
    use_cases: ["Triage unread emails", "Draft replies", "Search threads"],
    status: "not_connected",
    connected_count: 0,
    total_count: 1,
    toolkits: [
      {
        slug: "gmail",
        name: "Gmail",
        logo: "https://cdn.simpleicons.org/gmail",
        is_connected: false,
      },
    ],
    accounts: [],
  },
  {
    id: "googlecalendar",
    display_name: "Google Calendar",
    description: "Check upcoming meetings, schedule events, and manage invites.",
    use_cases: [
      "View upcoming meetings",
      "Create calendar events",
      "Check availability",
    ],
    status: "connected",
    connected_count: 1,
    total_count: 1,
    toolkits: [
      {
        slug: "googlecalendar",
        name: "Google Calendar",
        logo: "https://cdn.simpleicons.org/googlecalendar",
        is_connected: true,
      },
    ],
    accounts: [
      {
        connected_account_id: "conn_gcal_default",
        account_name: "local@kursor.dev",
        label: "Primary",
        avatar_url: null,
        account_name_kind: "email",
        status: "connected",
        connected_at: "2026-01-01T00:00:00.000Z",
      },
    ],
  },
  {
    id: "notion",
    display_name: "Notion",
    description: "Search pages, update databases, and write workspace docs.",
    use_cases: ["Search workspace pages", "Create notes", "Update databases"],
    status: "not_connected",
    connected_count: 0,
    total_count: 1,
    toolkits: [
      {
        slug: "notion",
        name: "Notion",
        logo: "https://cdn.simpleicons.org/notion",
        is_connected: false,
      },
    ],
    accounts: [],
  },
  {
    id: "linear",
    display_name: "Linear",
    description: "Manage engineering issues, cycles, and project roadmaps.",
    use_cases: ["Create and triage issues", "Update sprint status"],
    status: "not_connected",
    connected_count: 0,
    total_count: 1,
    toolkits: [
      {
        slug: "linear",
        name: "Linear",
        logo: "https://cdn.simpleicons.org/linear",
        is_connected: false,
      },
    ],
    accounts: [],
  },
  {
    id: "obsidian",
    display_name: "Obsidian",
    description: "Read and write markdown notes in your local Obsidian vault.",
    use_cases: ["Search local markdown notes", "Append daily notes"],
    status: "connected",
    connected_count: 1,
    total_count: 1,
    toolkits: [
      {
        slug: "obsidian",
        name: "Obsidian",
        logo: "https://cdn.simpleicons.org/obsidian",
        is_connected: true,
      },
    ],
    accounts: [],
  },
  {
    id: "spotify",
    display_name: "Spotify",
    description: "Search tracks, artists, and control music playback.",
    use_cases: ["Play songs by voice", "Search tracks and artists"],
    status: "connected",
    connected_count: 1,
    total_count: 1,
    toolkits: [
      {
        slug: "spotify",
        name: "Spotify",
        logo: "https://cdn.simpleicons.org/spotify",
        is_connected: true,
      },
    ],
    accounts: [],
  },
  {
    id: "github",
    display_name: "GitHub",
    description: "Inspect pull requests, issues, commits, and actions.",
    use_cases: ["Review PRs", "Manage issues", "Check CI status"],
    status: "connected",
    connected_count: 1,
    total_count: 1,
    toolkits: [
      {
        slug: "github",
        name: "GitHub",
        logo: "https://cdn.simpleicons.org/github",
        is_connected: true,
      },
    ],
    accounts: [
      {
        connected_account_id: "conn_gh_default",
        account_name: "kursor-dev",
        label: "Work",
        avatar_url: null,
        account_name_kind: "username",
        status: "connected",
        connected_at: "2026-01-01T00:00:00.000Z",
      },
    ],
  },
];

function handleGetIntegrations(c: any) {
  return c.json({
    composio_configured: true,
    integrations: integrationsCatalog,
  });
}

async function handleConnectIntegration(c: any) {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const toolkitParam = c.req.param("toolkit");
  const toolkit =
    (typeof toolkitParam === "string" && toolkitParam.trim()) ||
    (typeof body.toolkit === "string" && body.toolkit.trim()) ||
    (typeof body.id === "string" && body.id.trim()) ||
    "gmail";

  const entry = integrationsCatalog.find(
    (i) =>
      i.id.toLowerCase() === toolkit.toLowerCase() ||
      i.toolkits.some((t) => t.slug.toLowerCase() === toolkit.toLowerCase())
  );

  if (entry) {
    entry.status = "connected";
    entry.connected_count = Math.max(1, entry.connected_count);
    for (const tk of entry.toolkits) {
      tk.is_connected = true;
    }
  }

  const redirectUrl = `https://platform.composio.dev/oauth/connect/${encodeURIComponent(toolkit)}?session=kursor_local`;

  return c.json({
    url: redirectUrl,
    redirect_url: redirectUrl,
    toolkit,
    connected: true,
  });
}

agentRoutes.get("/connectors/status", handleGetIntegrations);
agentRoutes.get("/agent/integrations", handleGetIntegrations);
agentRoutes.post("/connectors/connect", handleConnectIntegration);
agentRoutes.post("/agent/integrations/connect", handleConnectIntegration);
agentRoutes.post(
  "/agent/integrations/:toolkit/connect",
  handleConnectIntegration
);
agentRoutes.post("/agent/integrations/:toolkit/disconnect", (c) => {
  const toolkit = c.req.param("toolkit");
  const entry = integrationsCatalog.find(
    (i) => i.id.toLowerCase() === toolkit.toLowerCase()
  );
  if (entry) {
    entry.status = "not_connected";
    entry.connected_count = 0;
  }
  return c.json({ ok: true, toolkit, disconnected: true });
});
agentRoutes.post(
  "/agent/integrations/:toolkit/accounts/:connectionId/label",
  async (c) => {
    return c.json({ ok: true });
  }
);

agentRoutes.post("/agent/composio/session", (c) => {
  const connectedToolkits = integrationsCatalog
    .filter((i) => i.status === "connected")
    .flatMap((i) => i.toolkits.map((t) => t.slug));

  return c.json({
    enabled: true,
    proxy_url: "http://127.0.0.1:8787/mcp/composio",
    session_id: `composio_sess_${crypto.randomUUID().slice(0, 12)}`,
    toolkits:
      connectedToolkits.length > 0
        ? connectedToolkits
        : ["googlecalendar", "github", "spotify"],
    multi_account_toolkits: [],
  });
});

// ============================================================================
// 4. CALENDAR & WORKER TOOLS ROUTES
// Matches `UpcomingMeetingsClient.cs` & `WorkerTools.cs`:
// - GET `/calendar/upcoming`, `/me/calendar/upcoming`
// - GET & POST `/agent/realtime/google-calendar/events`
// - POST `/agent/realtime/google-calendar/create-event` & `/agent/realtime/google-calendar/events/create`
// - POST `/tools/search-connectors`, `/tools/execute-connector`, `/tools/spotify/search`, `/agent/realtime/spotify/search`
// ============================================================================
interface CalendarEventItem {
  id: string;
  title: string;
  starts_at: string;
  ends_at: string;
  is_all_day: boolean;
  meeting_url: string | null;
  location: string | null;
  html_link: string | null;
}

const calendarEvents: CalendarEventItem[] = [
  {
    id: "evt_kursor_sync_01",
    title: "Kursor Architecture & Product Sync",
    starts_at: new Date(Date.now() + 30 * 60 * 1000).toISOString(),
    ends_at: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    is_all_day: false,
    meeting_url: "https://meet.google.com/kur-sor-sync",
    location: "Google Meet",
    html_link: "https://calendar.google.com/calendar/event?eid=evt_kursor_sync_01",
  },
];

function handleGetUpcomingCalendar(c: any) {
  return c.json({
    connected: true,
    status: "ok",
    events: calendarEvents,
  });
}

async function handleCreateCalendarEvent(c: any) {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const title =
    (typeof body.title === "string" && body.title.trim()) ||
    (typeof body.summary === "string" && body.summary.trim()) ||
    "New Kursor Meeting";
  const startsAt =
    (typeof body.starts_at === "string" && body.starts_at) ||
    (typeof body.start_time === "string" && body.start_time) ||
    new Date(Date.now() + 3600 * 1000).toISOString();
  const endsAt =
    (typeof body.ends_at === "string" && body.ends_at) ||
    (typeof body.end_time === "string" && body.end_time) ||
    new Date(Date.now() + 5400 * 1000).toISOString();

  const newEvent: CalendarEventItem = {
    id: `evt_${crypto.randomUUID().slice(0, 8)}`,
    title,
    starts_at: startsAt,
    ends_at: endsAt,
    is_all_day: Boolean(body.is_all_day ?? false),
    meeting_url:
      typeof body.meeting_url === "string"
        ? body.meeting_url
        : "https://meet.google.com/kur-sor-new",
    location: typeof body.location === "string" ? body.location : "Google Meet",
    html_link: "https://calendar.google.com/calendar/event?eid=new",
  };

  calendarEvents.push(newEvent);

  return c.json({
    status: "ok",
    created: true,
    event: newEvent,
  });
}

agentRoutes.get("/calendar/upcoming", handleGetUpcomingCalendar);
agentRoutes.get("/me/calendar/upcoming", handleGetUpcomingCalendar);
agentRoutes.get(
  "/agent/realtime/google-calendar/events",
  handleGetUpcomingCalendar
);
agentRoutes.post(
  "/agent/realtime/google-calendar/events",
  handleGetUpcomingCalendar
);
agentRoutes.post(
  "/agent/realtime/google-calendar/create-event",
  handleCreateCalendarEvent
);
agentRoutes.post(
  "/agent/realtime/google-calendar/events/create",
  handleCreateCalendarEvent
);

// Connector Tools Search & Execution
agentRoutes.post("/tools/search-connectors", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const query =
    typeof body.query === "string" ? body.query.toLowerCase().trim() : "";

  const availableTools = [
    {
      name: "GMAIL_FETCH_EMAILS",
      toolkit: "gmail",
      description: "Fetch recent emails from the user's Gmail inbox.",
    },
    {
      name: "GOOGLECALENDAR_FIND_EVENT",
      toolkit: "googlecalendar",
      description: "Find events in Google Calendar within a time range.",
    },
    {
      name: "NOTION_SEARCH_PAGES",
      toolkit: "notion",
      description: "Search Notion pages and databases by title or content.",
    },
    {
      name: "LINEAR_LIST_ISSUES",
      toolkit: "linear",
      description: "List and filter Linear engineering issues.",
    },
    {
      name: "GITHUB_LIST_PULL_REQUESTS",
      toolkit: "github",
      description: "List pull requests for a GitHub repository.",
    },
  ];

  const matched = query
    ? availableTools.filter(
        (t) =>
          t.name.toLowerCase().includes(query) ||
          t.toolkit.toLowerCase().includes(query) ||
          t.description.toLowerCase().includes(query)
      )
    : availableTools;

  return c.json({
    status: "ok",
    query,
    tools: matched.length > 0 ? matched : availableTools,
  });
});

agentRoutes.post("/tools/execute-connector", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const toolName =
    (typeof body.tool === "string" && body.tool) ||
    (typeof body.tool_name === "string" && body.tool_name) ||
    (typeof body.name === "string" && body.name) ||
    "GENERIC_CONNECTOR_TOOL";

  return c.json({
    status: "ok",
    executed: true,
    tool: toolName,
    result: {
      success: true,
      data: {
        message: `Executed connector tool ${toolName} via Kursor Local Worker.`,
        arguments: body.arguments ?? body.args ?? {},
      },
    },
  });
});

// Spotify Search (`POST /tools/spotify/search` & `POST /agent/realtime/spotify/search`)
// Note: `WorkerTools.NormalizedTrackUri` requires a 22-character alphanumeric track ID (`^[A-Za-z0-9]{22}$`)!
async function handleSpotifySearch(c: any) {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const query =
    (typeof body.query === "string" && body.query.trim()) ||
    "Daft Punk Veridis Quo";

  // 22-char valid base62 Spotify track ID
  const trackId = "4uLU6hMCjMI75M1A2tKUQC";
  const track = {
    id: trackId,
    uri: `spotify:track:${trackId}`,
    name: query,
    artists: "Kursor Artist",
    url: `https://open.spotify.com/track/${trackId}`,
  };

  return c.json({
    status: "ok",
    query,
    track,
    tracks: [track],
  });
}

agentRoutes.post("/tools/spotify/search", handleSpotifySearch);
agentRoutes.post("/agent/realtime/spotify/search", handleSpotifySearch);

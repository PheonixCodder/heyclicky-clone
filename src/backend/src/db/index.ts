import { DatabaseSync } from "node:sqlite";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export const DEFAULT_USER_ID = "00000000-0000-0000-0000-000000000001";
export const DEFAULT_USER_EMAIL = "local@kursor.dev";
export const DEFAULT_USER_DISPLAY_NAME = "Kursor User";
export const DEFAULT_USER_ALIAS = "kursor-local-user";

const defaultDbPath = path.resolve(__dirname, "../../data/kursor.db");
export const DB_PATH = process.env.DATABASE_PATH
  ? path.resolve(process.cwd(), process.env.DATABASE_PATH)
  : defaultDbPath;

// Ensure parent directory exists
fs.mkdirSync(path.dirname(DB_PATH), { recursive: true });

export const db = new DatabaseSync(DB_PATH);

// Enable WAL mode and foreign keys
db.exec("PRAGMA journal_mode = WAL;");
db.exec("PRAGMA synchronous = NORMAL;");
db.exec("PRAGMA foreign_keys = ON;");

export function initDatabase(): void {
  db.exec(`
    CREATE TABLE IF NOT EXISTS users (
      id TEXT PRIMARY KEY,
      email TEXT NOT NULL UNIQUE,
      display_name TEXT NOT NULL,
      avatar_url TEXT,
      plan TEXT NOT NULL DEFAULT 'max',
      tier TEXT NOT NULL DEFAULT 'max',
      discovery_channel TEXT,
      discovery_channel_other TEXT,
      referral_code TEXT NOT NULL DEFAULT 'KURSOR20',
      referral_claimed_code TEXT,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL
    );

    CREATE TABLE IF NOT EXISTS devices (
      device_id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      device_name TEXT NOT NULL,
      platform TEXT NOT NULL DEFAULT 'windows',
      app_version TEXT NOT NULL DEFAULT '1.1.5',
      first_seen_at TEXT NOT NULL,
      last_seen_at TEXT NOT NULL,
      is_current INTEGER NOT NULL DEFAULT 1,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS memory (
      user_id TEXT PRIMARY KEY,
      markdown TEXT NOT NULL DEFAULT '',
      profile_markdown TEXT NOT NULL DEFAULT '',
      context_markdown TEXT NOT NULL DEFAULT '',
      has_profile INTEGER NOT NULL DEFAULT 1,
      updated_at TEXT NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS sessions (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      access_token TEXT NOT NULL,
      refresh_token TEXT NOT NULL,
      expires_at INTEGER NOT NULL,
      created_at TEXT NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS threads (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      title TEXT NOT NULL DEFAULT 'New Conversation',
      mode TEXT NOT NULL DEFAULT 'normal',
      model TEXT,
      messages_json TEXT NOT NULL DEFAULT '[]',
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS notifications (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      type TEXT NOT NULL DEFAULT 'info',
      title TEXT NOT NULL,
      body TEXT NOT NULL,
      action_url TEXT,
      is_read INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS proactive_cards (
      id TEXT PRIMARY KEY,
      user_id TEXT NOT NULL,
      card_type TEXT NOT NULL DEFAULT 'suggestion',
      title TEXT NOT NULL,
      subtitle TEXT,
      prompt TEXT,
      payload_json TEXT NOT NULL DEFAULT '{}',
      dismissed INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL,
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE INDEX IF NOT EXISTS idx_sessions_user_id ON sessions(user_id);
    CREATE INDEX IF NOT EXISTS idx_sessions_access_token ON sessions(access_token);
    CREATE INDEX IF NOT EXISTS idx_notifications_user_read ON notifications(user_id, is_read, created_at DESC);
    CREATE INDEX IF NOT EXISTS idx_proactive_cards_user_dismissed ON proactive_cards(user_id, dismissed);
    CREATE INDEX IF NOT EXISTS idx_threads_user_id ON threads(user_id);

    DELETE FROM sessions WHERE expires_at > 0 AND expires_at < unixepoch();
  `);

  const now = new Date().toISOString();

  // Seed default local Pro/Max Unlimited user
  const existingUser = db
    .prepare("SELECT id FROM users WHERE id = ?")
    .get(DEFAULT_USER_ID) as { id: string } | undefined;

  if (!existingUser) {
    db.prepare(
      `INSERT INTO users (
        id, email, display_name, avatar_url, plan, tier,
        discovery_channel, discovery_channel_other, referral_code,
        created_at, updated_at
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
    ).run(
      DEFAULT_USER_ID,
      DEFAULT_USER_EMAIL,
      DEFAULT_USER_DISPLAY_NAME,
      "https://api.dicebear.com/9.x/bottts/svg?seed=kursor",
      "max",
      "max",
      "github",
      null,
      "KURSOR-MAX",
      now,
      now
    );
  }

  // Seed initial memory row for default user
  const existingMemory = db
    .prepare("SELECT user_id FROM memory WHERE user_id = ?")
    .get(DEFAULT_USER_ID) as { user_id: string } | undefined;

  if (!existingMemory) {
    const initialProfile = `# User Profile\n- **Name**: Kursor User\n- **Email**: local@kursor.dev\n- **Role**: Software Engineer & Builder\n- **OS**: Windows 11`;
    const initialContext = `# Active Context\n- Running Kursor (local-first HeyClicky v1.1.5 clone) with full Pro/Max Unlimited capabilities.`;
    const initialMarkdown = `${initialProfile}\n\n${initialContext}`;

    db.prepare(
      `INSERT INTO memory (
        user_id, markdown, profile_markdown, context_markdown, has_profile, updated_at
      ) VALUES (?, ?, ?, ?, ?, ?)`
    ).run(
      DEFAULT_USER_ID,
      initialMarkdown,
      initialProfile,
      initialContext,
      1,
      now
    );
  }

  runDatabaseMaintenance();
}

export function runDatabaseMaintenance(): void {
  try {
    db.exec(`
      DELETE FROM sessions WHERE expires_at > 0 AND expires_at < unixepoch();
      DELETE FROM sessions WHERE id NOT IN (SELECT id FROM sessions ORDER BY created_at DESC LIMIT 500);
      DELETE FROM threads WHERE id NOT IN (SELECT id FROM threads ORDER BY updated_at DESC LIMIT 500);
    `);
  } catch {
    // Ignore transient maintenance errors
  }

  try {
    db.exec("PRAGMA wal_checkpoint(PASSIVE);");
  } catch {
    // Ignore WAL checkpoint errors
  }
}

initDatabase();

const maintenanceInterval = setInterval(
  () => {
    runDatabaseMaintenance();
  },
  60 * 60 * 1000
);
maintenanceInterval.unref();

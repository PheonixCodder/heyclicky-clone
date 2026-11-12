import assert from "node:assert/strict";
import { gzipSync } from "node:zlib";
import { startServer } from "./index.js";
import { db } from "./db/index.js";

interface ParsedSseResult {
  rawText: string;
  events: Array<{ type: string; data: Record<string, any> }>;
}

/**
 * Mirrors `HeyClicky.Core.Brain.BrainSseParser` in C#:
 * - Reads `data:` lines from SSE stream
 * - If line after `data: ` starts with `{"type":"text"`, appends `.text` to `rawText`
 * - Otherwise parses JSON object with `.type` and records event
 */
function parseBrainSseStream(sseBody: string): ParsedSseResult {
  let rawText = "";
  const events: Array<{ type: string; data: Record<string, any> }> = [];

  for (const rawLine of sseBody.split("\n")) {
    const line = rawLine.replace(/\r$/, "");
    if (!line.startsWith("data:")) continue;
    const jsonStr = line.slice(5).trimStart();
    if (!jsonStr) continue;

    if (jsonStr.startsWith('{"type":"text"')) {
      const parsed = JSON.parse(jsonStr);
      if (typeof parsed.text === "string") {
        rawText += parsed.text;
      }
      continue;
    }

    const parsed = JSON.parse(jsonStr);
    if (parsed && typeof parsed.type === "string") {
      events.push({ type: parsed.type, data: parsed });
    }
  }

  return { rawText, events };
}

async function runPhase2Tests() {
  console.log("============================================================");
  console.log(" KURSOR PHASE 2 & PHASE 6 BACKEND VERIFICATION SUITE");
  console.log("============================================================\n");

  const { server, url } = await startServer(0, "127.0.0.1");
  let passed = 0;

  async function testEndpoint(name: string, fn: () => Promise<void>) {
    process.stdout.write(`  [TEST] ${name} ... `);
    try {
      await fn();
      passed++;
      console.log("PASS");
    } catch (err) {
      console.log("FAIL");
      console.error(`    Error in "${name}":`, err);
      throw err;
    }
  }

  try {
    // 1. POST /brain/turn (Gzip compressed request + SSE streaming + SQLite persistence)
    await testEndpoint(
      "POST /brain/turn (Gzip request, SSE meta/status/text/items/done, say(1, ...) & spot(...) drawing code)",
      async () => {
        const sessionId = "TEST-BRAIN-SESSION-001";
        const turnPayload = {
          session_id: sessionId,
          turn_token: "turn_tok_001",
          keep_reasoning: true,
          force_local_fallback: true,
          thread: [
            {
              type: "message",
              role: "user",
              content: [
                {
                  type: "input_text",
                  text: "Highlight the active window on my screen.",
                },
              ],
            },
          ],
          turn: {
            words: "Highlight the active window on my screen.",
            fact: "",
            time_label: "10:30 AM",
            front_app: "Code",
            front_app_bundle_id: "Code.exe",
            drawing_on_screen: "",
            input: "voice",
            live_state: "idle",
            screenshot: {
              base64: Buffer.from("fake-jpeg-bytes").toString("base64"),
              mimeType: "image/jpeg",
              widthInPixels: 1920,
              heightInPixels: 1080,
            },
          },
          canvas: { w: 1920, h: 1080 },
          environment_sent: true,
        };

        const compressed = gzipSync(
          Buffer.from(JSON.stringify(turnPayload), "utf8")
        );

        const res = await fetch(`${url}/brain/turn`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "Content-Encoding": "gzip",
            "x-clicky-accepts-gzip": "1",
            Accept: "text/event-stream",
          },
          body: compressed,
        });

        assert.equal(res.status, 200);
        assert.equal(res.headers.get("x-clicky-accepts-gzip"), "1");
        assert.ok(
          (res.headers.get("content-type") ?? "").includes("text/event-stream")
        );

        const sseText = await res.text();
        const { rawText, events } = parseBrainSseStream(sseText);

        // Verify streamed JS drawing code
        assert.ok(
          rawText.includes('say("I see your screen and I\'m ready to help!");'),
          "Streamed code should include say(...) for SayLines.cs"
        );
        assert.ok(
          rawText.includes(
            'say(1, "I see your screen and I\'m ready to help!");'
          ),
          "Streamed code should include say(1, ...)"
        );
        assert.ok(
          rawText.includes('spot(500, 300, "Kursor Active");'),
          "Streamed code should include spot(500, 300, ...)"
        );

        // Verify `meta` event
        const metaEvt = events.find((e) => e.type === "meta");
        assert.ok(metaEvt, "Should emit meta SSE event");
        assert.equal(metaEvt.data.turn_token, "turn_tok_001");
        assert.equal(metaEvt.data.keep_reasoning, true);
        assert.equal(metaEvt.data.guided_max_rounds, 8);
        assert.equal(typeof metaEvt.data.model, "string");
        assert.equal(typeof metaEvt.data.user_layer, "string");
        assert.equal(typeof metaEvt.data.user_layer_signature, "string");

        // Verify `status` event
        const statusEvt = events.find((e) => e.type === "status");
        assert.ok(statusEvt, "Should emit status SSE event");
        assert.equal(statusEvt.data.text, "Thinking...");

        // Verify `items` event
        const itemsEvt = events.find((e) => e.type === "items");
        assert.ok(itemsEvt, "Should emit items SSE event");
        assert.ok(Array.isArray(itemsEvt.data.items));
        assert.ok(itemsEvt.data.items.length >= 1);
        assert.equal(itemsEvt.data.items[0].type, "message");
        assert.equal(itemsEvt.data.items[0].role, "assistant");

        // Verify `done` event
        const doneEvt = events.find((e) => e.type === "done");
        assert.ok(doneEvt, "Should emit done SSE event");
        assert.equal(typeof doneEvt.data.usage.input_tokens, "number");
        assert.equal(typeof doneEvt.data.usage.last_input_tokens, "number");
        assert.equal(doneEvt.data.cost, 0.0);
        assert.equal(doneEvt.data.message_recorded, true);
        assert.equal(doneEvt.data.message_refunded, false);
        assert.equal(doneEvt.data.drive_messages_refunded, 0);

        // Verify SQLite thread persistence
        const threadRow = db
          .prepare("SELECT id, messages_json FROM threads WHERE id = ?")
          .get(sessionId) as { id: string; messages_json: string } | undefined;
        assert.ok(threadRow, "Thread should be persisted in SQLite");
        const savedMessages = JSON.parse(threadRow.messages_json);
        assert.ok(Array.isArray(savedMessages));
        assert.ok(savedMessages.length >= 2);
      }
    );

    // 2. POST /brain/turn with tool_calls and computer_calls
    await testEndpoint(
      "POST /brain/turn (tool_call / tool_calls & computer_calls SSE events)",
      async () => {
        const res = await fetch(`${url}/brain/turn`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            session_id: "TEST-BRAIN-TOOLS-002",
            force_local_fallback: true,
            force_tool_call: {
              call_id: "call_mem_99",
              name: "save_memory",
              arguments: JSON.stringify({ memory: "Likes dark mode" }),
              async: false,
            },
            force_computer_call: {
              call_id: "comp_99",
              actions: [{ type: "click", x: 500, y: 300 }],
              pending_safety_checks: [],
            },
          }),
        });

        assert.equal(res.status, 200);
        const { events } = parseBrainSseStream(await res.text());

        const toolCallEvt = events.find((e) => e.type === "tool_call");
        assert.ok(toolCallEvt, "Should emit tool_call SSE event");
        assert.equal(toolCallEvt.data.call.call_id, "call_mem_99");
        assert.equal(toolCallEvt.data.call.name, "save_memory");

        const toolCallsEvt = events.find((e) => e.type === "tool_calls");
        assert.ok(toolCallsEvt, "Should emit tool_calls SSE event");
        assert.equal(toolCallsEvt.data.calls.length, 1);

        const compCallsEvt = events.find((e) => e.type === "computer_calls");
        assert.ok(compCallsEvt, "Should emit computer_calls SSE event");
        assert.equal(compCallsEvt.data.calls[0].call_id, "comp_99");
      }
    );

    // 3. POST /brain/compact, /brain/session-end, /brain/prewarm
    await testEndpoint(
      "POST /brain/compact, POST /brain/session-end, & POST /brain/prewarm",
      async () => {
        const compactRes = await fetch(`${url}/brain/compact`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            session_id: "TEST-BRAIN-SESSION-001",
            input_tokens: 78000,
            thread: [
              { type: "message", role: "user", content: "Turn 1" },
              { type: "message", role: "assistant", content: "Reply 1" },
              { type: "message", role: "user", content: "Turn 2" },
              { type: "message", role: "assistant", content: "Reply 2" },
            ],
          }),
        });
        assert.equal(compactRes.status, 200);
        const compactData = (await compactRes.json()) as any;
        assert.ok(compactData.compacted);
        assert.ok(compactData.compacted.note_item);
        assert.equal(typeof compactData.compacted.keep_from_index, "number");
        assert.equal(typeof compactData.compacted.keep_leading_items, "number");
        assert.equal(typeof compactData.compacted.drop_leading_items, "number");
        assert.equal(compactData.tokens_until_fold, 45000);
        assert.ok(compactData.note_item);

        const endRes = await fetch(`${url}/brain/session-end`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            session_id: "TEST-BRAIN-SESSION-001",
            thread: [],
          }),
        });
        assert.equal(endRes.status, 200);
        const endData = (await endRes.json()) as any;
        assert.equal(endData.ok, true);

        const prewarmRes = await fetch(`${url}/brain/prewarm`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ session_id: "TEST-BRAIN-SESSION-001" }),
        });
        assert.equal(prewarmRes.status, 200);
        const prewarmData = (await prewarmRes.json()) as any;
        assert.equal(prewarmData.ok, true);
      }
    );

    // 4. Proactive Agents & Suggestion Cards
    await testEndpoint(
      "Proactive routes (/proactive/jobs, /proactive-agents/jobs, /proactive/cards/resolve, /proactive/cards/status, /proactive/morning/claim)",
      async () => {
        for (const basePath of ["/proactive", "/proactive-agents"]) {
          const startRes = await fetch(`${url}${basePath}/jobs`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              reason: "morning_briefing",
              local_day: "2026-11-11",
              foreground_activity: { app_name: "Visual Studio Code" },
            }),
          });
          assert.equal(startRes.status, 200);
          const jobData = (await startRes.json()) as any;
          assert.equal(typeof jobData.job_id, "string");
          assert.equal(jobData.state, "completed");
          assert.equal(jobData.status, "succeeded");
          assert.ok(Array.isArray(jobData.cards));
          assert.ok(jobData.cards.length >= 1);

          const pollRes = await fetch(
            `${url}${basePath}/jobs/${encodeURIComponent(jobData.job_id)}`
          );
          assert.equal(pollRes.status, 200);
          const pollData = (await pollRes.json()) as any;
          assert.equal(pollData.job_id, jobData.job_id);
          assert.equal(pollData.state, "completed");

          const cardId = jobData.cards[0].id;
          const resolveRes = await fetch(`${url}/proactive/cards/resolve`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              card_ids: [cardId],
              resolution: "accepted",
            }),
          });
          assert.equal(resolveRes.status, 200);

          const statusRes = await fetch(`${url}/proactive/cards/status`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ card_ids: [cardId] }),
          });
          assert.equal(statusRes.status, 200);
          const statusData = (await statusRes.json()) as any;
          assert.equal(statusData[cardId], "accepted");
          assert.equal(statusData.statuses[cardId], "accepted");
        }

        const claimRes = await fetch(`${url}/proactive/morning/claim`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ card_ids: [101] }),
        });
        assert.equal(claimRes.status, 200);
        const claimData = (await claimRes.json()) as any;
        assert.equal(claimData.claimed, true);
        assert.equal(claimData.granted, true);
      }
    );

    // 5. Agent Notifications, Crons & Turn Leases
    await testEndpoint(
      "Agent Notifications, Crons, Session Token & Turn Leases (/agent/* & /agents/*)",
      async () => {
        // Seed a fresh notification to test unread -> read -> read-all
        db.prepare(
          `INSERT OR REPLACE INTO notifications (id, user_id, type, title, body, action_url, is_read, created_at)
           VALUES ('notif_test_phase2', '00000000-0000-0000-0000-000000000001', 'cron', 'Test Cron Alert', 'Cron finished', 'cron_01', 0, ?)`
        ).run(new Date().toISOString());

        const unreadRes1 = await fetch(`${url}/agents/notifications/unread`);
        assert.equal(unreadRes1.status, 200);
        const unreadData1 = (await unreadRes1.json()) as any;
        assert.ok(Array.isArray(unreadData1.notifications));
        assert.ok(
          unreadData1.notifications.some(
            (n: any) => n.id === "notif_test_phase2"
          )
        );

        const unreadRes2 = await fetch(`${url}/agent/notifications`);
        assert.equal(unreadRes2.status, 200);

        const markReadRes = await fetch(
          `${url}/agents/notifications/notif_test_phase2/read`,
          { method: "POST" }
        );
        assert.equal(markReadRes.status, 200);

        const markAllRes = await fetch(
          `${url}/agent/notifications/read-all`,
          { method: "POST" }
        );
        assert.equal(markAllRes.status, 200);

        for (const cronPath of ["/agents/crons/cron_01", "/agent/cron/cron_01"]) {
          const cronRes = await fetch(`${url}${cronPath}`);
          assert.equal(cronRes.status, 200);
          const cronData = (await cronRes.json()) as any;
          assert.equal(cronData.cron.id, "cron_01");
          assert.ok(Array.isArray(cronData.runs));
        }

        const tokRes = await fetch(`${url}/agent/session-token`, {
          method: "POST",
        });
        assert.equal(tokRes.status, 200);
        const tokData = (await tokRes.json()) as any;
        assert.equal(typeof tokData.token, "string");

        const launchRes = await fetch(`${url}/agent/record-agent-launch`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            session_id: "sess_01",
            turn_id: "turn_01",
            source: "voice",
          }),
        });
        assert.equal(launchRes.status, 200);
        const launchData = (await launchRes.json()) as any;
        assert.equal(typeof launchData.lease_id, "string");
        assert.equal(launchData.allowed, true);
        assert.equal(launchData.active, true);

        const leaseStatusRes = await fetch(
          `${url}/agent/turn-lease/${launchData.lease_id}/status`
        );
        assert.equal(leaseStatusRes.status, 200);
        const leaseStatusData = (await leaseStatusRes.json()) as any;
        assert.equal(leaseStatusData.lease_id, launchData.lease_id);
        assert.equal(leaseStatusData.active, true);
      }
    );

    // 6. Connectors & Composio MCP
    await testEndpoint(
      "Connectors & Composio MCP (/connectors/status, /agent/integrations, /connectors/connect, /agent/composio/session)",
      async () => {
        for (const statusPath of ["/connectors/status", "/agent/integrations"]) {
          const res = await fetch(`${url}${statusPath}`);
          assert.equal(res.status, 200);
          const data = (await res.json()) as any;
          assert.ok(Array.isArray(data.integrations));
          const ids = data.integrations.map((i: any) => i.id);
          for (const expected of [
            "gmail",
            "googlecalendar",
            "notion",
            "linear",
            "obsidian",
            "spotify",
            "github",
          ]) {
            assert.ok(
              ids.includes(expected),
              `Missing integration ${expected}`
            );
          }
        }

        const connRes1 = await fetch(`${url}/connectors/connect`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ toolkit: "gmail" }),
        });
        assert.equal(connRes1.status, 200);
        const connData1 = (await connRes1.json()) as any;
        assert.equal(typeof connData1.url, "string");
        assert.equal(typeof connData1.redirect_url, "string");

        const connRes2 = await fetch(`${url}/agent/integrations/connect`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ toolkit: "notion" }),
        });
        assert.equal(connRes2.status, 200);

        const compRes = await fetch(`${url}/agent/composio/session`, {
          method: "POST",
        });
        assert.equal(compRes.status, 200);
        const compData = (await compRes.json()) as any;
        assert.equal(compData.enabled, true);
        assert.equal(typeof compData.proxy_url, "string");
        assert.ok(Array.isArray(compData.toolkits));
      }
    );

    // 7. Calendar & WorkerTools
    await testEndpoint(
      "Calendar & WorkerTools (/calendar/upcoming, /agent/realtime/google-calendar/*, /tools/*)",
      async () => {
        for (const calPath of [
          "/calendar/upcoming",
          "/me/calendar/upcoming",
          "/agent/realtime/google-calendar/events",
        ]) {
          const res = await fetch(`${url}${calPath}`);
          assert.equal(res.status, 200);
          const data = (await res.json()) as any;
          assert.equal(typeof data.connected, "boolean");
          assert.ok(Array.isArray(data.events));
        }

        const createEvtRes = await fetch(
          `${url}/agent/realtime/google-calendar/create-event`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              title: "Kursor Demo Review",
              starts_at: new Date(Date.now() + 7200 * 1000).toISOString(),
            }),
          }
        );
        assert.equal(createEvtRes.status, 200);
        const createEvtData = (await createEvtRes.json()) as any;
        assert.equal(createEvtData.status, "ok");
        assert.equal(createEvtData.event.title, "Kursor Demo Review");

        const searchConnRes = await fetch(`${url}/tools/search-connectors`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ query: "gmail" }),
        });
        assert.equal(searchConnRes.status, 200);
        const searchConnData = (await searchConnRes.json()) as any;
        assert.equal(searchConnData.status, "ok");
        assert.ok(Array.isArray(searchConnData.tools));
        assert.ok(searchConnData.tools.length >= 1);

        const execConnRes = await fetch(`${url}/tools/execute-connector`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            tool: "GMAIL_FETCH_EMAILS",
            arguments: { max_results: 5 },
          }),
        });
        assert.equal(execConnRes.status, 200);
        const execConnData = (await execConnRes.json()) as any;
        assert.equal(execConnData.status, "ok");
        assert.equal(execConnData.executed, true);

        for (const spotifyPath of [
          "/tools/spotify/search",
          "/agent/realtime/spotify/search",
        ]) {
          const spotRes = await fetch(`${url}${spotifyPath}`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ query: "Harder Better Faster Stronger" }),
          });
          assert.equal(spotRes.status, 200);
          const spotData = (await spotRes.json()) as any;
          assert.equal(spotData.status, "ok");
          assert.ok(
            /^spotify:track:[A-Za-z0-9]{22}$/.test(spotData.track.uri),
            "Track URI must match WorkerTools.NormalizedTrackUri 22-char base62 format"
          );
        }
      }
    );

    console.log(
      `\n============================================================`
    );
    console.log(
      ` ALL ${passed} PHASE 2 & PHASE 6 TEST SUITES PASSED SUCCESSFULLY!`
    );
    console.log(
      `============================================================`
    );
  } finally {
    server.close();
  }
}

runPhase2Tests().catch((err) => {
  console.error("Phase 2 & 6 test suite failed:", err);
  process.exit(1);
});

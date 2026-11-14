import assert from "node:assert/strict";
import { gzipSync } from "node:zlib";
import { startServer } from "./index.js";
import { DEFAULT_USER_EMAIL, DEFAULT_USER_ID } from "./db/index.js";

async function runTests() {
  console.log("============================================================");
  console.log(" KURSOR PHASE 1 BACKEND VERIFICATION SUITE");
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
    // 1. Health & Gzip Response Header
    await testEndpoint(
      "GET /health & x-clicky-accepts-gzip: 1 header",
      async () => {
        const res = await fetch(`${url}/health`);
        assert.equal(res.status, 200);
        assert.equal(res.headers.get("x-clicky-accepts-gzip"), "1");
        const data = (await res.json()) as any;
        assert.equal(data.status, "ok");
      }
    );

    // 2. Unified Supabase Auth Mock Routes (/auth/v1/*)
    await testEndpoint(
      "GET /auth/v1/authorize (302 redirect to callback with code & state)",
      async () => {
        const res = await fetch(
          `${url}/auth/v1/authorize?provider=google&state=test_state_123&redirect_to=heyclicky://auth/callback`,
          { redirect: "manual" }
        );
        assert.equal(res.status, 302);
        const location = res.headers.get("location") ?? "";
        assert.ok(location.startsWith("heyclicky://auth/callback?"));
        assert.ok(location.includes("code=kursor_local_code"));
        assert.ok(location.includes("state=test_state_123"));
      }
    );

    await testEndpoint(
      "POST /auth/v1/token (PKCE / password / refresh_token -> valid JWT & user)",
      async () => {
        for (const grantType of ["pkce", "password", "refresh_token"]) {
          const res = await fetch(
            `${url}/auth/v1/token?grant_type=${grantType}`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ auth_code: "kursor_local_code" }),
            }
          );
          assert.equal(res.status, 200);
          const data = (await res.json()) as any;
          assert.equal(typeof data.access_token, "string");
          assert.equal(data.access_token.split(".").length, 3);
          assert.equal(typeof data.refresh_token, "string");
          assert.equal(typeof data.expires_in, "number");
          assert.equal(data.token_type, "bearer");
          assert.equal(data.user.id, DEFAULT_USER_ID);
          assert.equal(data.user.email, DEFAULT_USER_EMAIL);
          assert.equal(data.user.user_metadata.full_name, "Kursor User");
        }
      }
    );

    await testEndpoint("GET /auth/v1/user (Seeded local user profile)", async () => {
      const res = await fetch(`${url}/auth/v1/user`);
      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.id, DEFAULT_USER_ID);
      assert.equal(data.email, DEFAULT_USER_EMAIL);
      assert.equal(data.user_metadata.full_name, "Kursor User");
      assert.equal(data.user_metadata.name, "Kursor User");
      assert.equal(typeof data.user_metadata.avatar_url, "string");
    });

    await testEndpoint("POST /auth/v1/logout (200 OK)", async () => {
      const res = await fetch(`${url}/auth/v1/logout`, { method: "POST" });
      assert.equal(res.status, 200);
    });

    // 3. WorkerClient Routes
    await testEndpoint(
      "GET /plan & GET /me/plan (PlanSnapshot DTO)",
      async () => {
        for (const path of ["/plan", "/me/plan"]) {
          const res = await fetch(`${url}${path}`);
          assert.equal(res.status, 200);
          const data = (await res.json()) as any;
          assert.equal(data.plan, "max");
          assert.equal(data.tier, "max");
          assert.equal(data.can_use_voice, true);
          assert.equal(data.can_use_dictation, true);
          assert.equal(data.can_use_agents, true);
          assert.equal(typeof data.usage.messages.cap, "number");
          assert.equal(typeof data.usage.agents.cap, "number");
          assert.equal(typeof data.usage.dictation.cap, "number");
          assert.equal(data.subscription.status, "active");
        }
      }
    );

    await testEndpoint(
      "GET /config & GET /app-config (AppConfigResponse DTO)",
      async () => {
        for (const path of ["/config", "/app-config"]) {
          const res = await fetch(`${url}${path}`);
          assert.equal(res.status, 200);
          const data = (await res.json()) as any;
          assert.equal(
            data.appConfig.links.featureRequest,
            "https://heyclicky.canny.io/feature-requests"
          );
          assert.equal(
            data.appConfig.links.communityReddit,
            "https://www.reddit.com/r/heyclicky_"
          );
          assert.equal(
            typeof data.appConfig.links.communityWhatsApp,
            "string"
          );
          assert.equal(
            data.appConfig.links.changelog,
            "https://heyclicky.com/changelog?p=windows"
          );
        }
      }
    );

    await testEndpoint(
      "GET /config/runtime-models (OpenRouter Brain, Deepgram Ears, OpenAI Realtime Mouth, Groq Dictation)",
      async () => {
        const res = await fetch(`${url}/config/runtime-models`);
        assert.equal(res.status, 200);
        const data = (await res.json()) as any;
        assert.equal(data.brain.primary_provider, "openrouter");
        assert.ok(["deepgram", "groq"].includes(data.ears.primary_provider));
        assert.ok(["gemini", "groq"].includes(data.ears.fallback_provider));
        assert.equal(
          data.mouth.primary_provider,
          process.env.MOUTH_PRIMARY_PROVIDER || "openai-realtime"
        );
        assert.equal(data.mouth.fallback_provider, "gemini-tts");
        assert.equal(data.dictation.primary_cleanup_provider, "groq");
      }
    );

    await testEndpoint(
      "POST /devices/register (with Content-Encoding: gzip) & GET /devices",
      async () => {
        const rawPayload = JSON.stringify({
          device_id: "win-kursor-test-99",
          device_name: "KURSOR-WORKSTATION",
          platform: "windows",
          app_version: "1.1.5",
        });
        const gzippedBody = gzipSync(Buffer.from(rawPayload, "utf8"));

        const regRes = await fetch(`${url}/devices/register`, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "Content-Encoding": "gzip",
            "X-Clicky-Client-Platform": "windows",
          },
          body: gzippedBody,
        });
        assert.equal(regRes.status, 200);
        assert.equal(regRes.headers.get("x-clicky-accepts-gzip"), "1");
        const regData = (await regRes.json()) as any;
        assert.equal(regData.device.device_id, "win-kursor-test-99");
        assert.equal(regData.device.device_name, "KURSOR-WORKSTATION");
        assert.equal(regData.device.is_current, true);

        const listRes = await fetch(`${url}/devices`);
        assert.equal(listRes.status, 200);
        const listData = (await listRes.json()) as any;
        assert.ok(Array.isArray(listData.devices));
        assert.ok(
          listData.devices.some(
            (d: any) => d.device_id === "win-kursor-test-99"
          )
        );
      }
    );

    await testEndpoint(
      "POST & GET /onboarding/discovery-channel & POST /onboarding/starter-plan",
      async () => {
        const postRes = await fetch(`${url}/onboarding/discovery-channel`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            discovery_channel: "x_twitter",
            discovery_channel_other: "AI thread",
          }),
        });
        assert.equal(postRes.status, 200);
        const postData = (await postRes.json()) as any;
        assert.equal(postData.discovery_channel, "x_twitter");

        const getRes = await fetch(`${url}/onboarding/discovery-channel`);
        assert.equal(getRes.status, 200);
        const getData = (await getRes.json()) as any;
        assert.equal(getData.discovery_channel, "x_twitter");
        assert.equal(getData.discovery_channel_other, "AI thread");

        const starterRes = await fetch(`${url}/onboarding/starter-plan`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            role: "Full-Stack Developer",
            goals: ["Code faster", "Automate PR reviews"],
            tools: ["VS Code", "GitHub"],
            agent_familiarity: "experienced",
          }),
        });
        assert.equal(starterRes.status, 200);
        const starterData = (await starterRes.json()) as any;
        assert.ok(Array.isArray(starterData.clickys));
        assert.ok(starterData.clickys.length > 0);
        assert.ok(Array.isArray(starterData.suggested_agents));
        assert.equal(typeof starterData.goal_line, "string");
      }
    );

    await testEndpoint(
      "GET /memory & PUT /memory (UserMemoryResponse & SaveMemoryResponse)",
      async () => {
        const getRes1 = await fetch(`${url}/memory`);
        assert.equal(getRes1.status, 200);
        const mem1 = (await getRes1.json()) as any;
        assert.equal(typeof mem1.markdown, "string");
        assert.equal(typeof mem1.has_profile, "boolean");

        const updatedText = "# Kursor Memory\n- Prefers TypeScript & C# .NET 9";
        const putRes = await fetch(`${url}/memory`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ memory: updatedText }),
        });
        assert.equal(putRes.status, 200);
        const saveData = (await putRes.json()) as any;
        assert.equal(saveData.saved, true);
        assert.equal(saveData.error, null);

        const getRes2 = await fetch(`${url}/memory`);
        const mem2 = (await getRes2.json()) as any;
        assert.equal(mem2.markdown, updatedText);
        assert.equal(mem2.has_profile, true);
      }
    );

    await testEndpoint(
      "POST /search/web & POST /search/images (WebSearchResponse & ImageSearchResponse)",
      async () => {
        const webRes = await fetch(`${url}/search/web`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ query: "TypeScript Hono framework", count: 3 }),
        });
        assert.equal(webRes.status, 200);
        const webData = (await webRes.json()) as any;
        assert.equal(webData.query, "TypeScript Hono framework");
        assert.ok(Array.isArray(webData.results));
        assert.ok(webData.results.length >= 1);
        assert.equal(typeof webData.results[0].title, "string");
        assert.equal(typeof webData.results[0].url, "string");

        const imgRes = await fetch(`${url}/search/images`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ query: "mountain landscape", limit: 3 }),
        });
        assert.equal(imgRes.status, 200);
        const imgData = (await imgRes.json()) as any;
        assert.ok(Array.isArray(imgData.images));
        assert.ok(imgData.images.length >= 1);
        assert.equal(typeof imgData.images[0].thumbnail_url, "string");
        assert.equal(typeof imgData.images[0].full_url, "string");
        assert.equal(typeof imgData.images[0].source_url, "string");
        assert.equal(typeof imgData.images[0].title, "string");
      }
    );

    await testEndpoint(
      "Referral, Billing, Account Delete, Telemetry, & Language Hints routes",
      async () => {
        const refGet = await fetch(`${url}/referral`);
        assert.equal(refGet.status, 200);

        const refClaim = await fetch(`${url}/referral/claim`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ code: "KURSORVIP" }),
        });
        assert.equal(refClaim.status, 200);
        const claimData = (await refClaim.json()) as any;
        assert.equal(claimData.outcome, "claimed");
        assert.equal(claimData.discount_percent, 20);

        const refCode = await fetch(`${url}/referral/code`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ code: "MYKURSOR" }),
        });
        assert.equal(refCode.status, 200);
        const codeData = (await refCode.json()) as any;
        assert.equal(codeData.code, "MYKURSOR");

        const checkoutRes = await fetch(`${url}/billing/checkout`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            interval: "year",
            tier: "max",
            discount_code: "KURSORVIP",
          }),
        });
        assert.equal(checkoutRes.status, 200);
        const checkoutData = (await checkoutRes.json()) as any;
        assert.equal(typeof checkoutData.url, "string");
        assert.equal(checkoutData.discount_code_applied, "KURSORVIP");

        const portalRes = await fetch(`${url}/billing/portal`, {
          method: "POST",
        });
        assert.equal(portalRes.status, 200);
        const portalData = (await portalRes.json()) as any;
        assert.equal(typeof portalData.url, "string");

        const delRes = await fetch(`${url}/account/delete`, {
          method: "POST",
        });
        assert.equal(delRes.status, 200);

        const errRes = await fetch(`${url}/telemetry/client-error`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            category: "voice_test",
            message: "Simulated telemetry test",
          }),
        });
        assert.equal(errRes.status, 200);

        const hintsRes = await fetch(`${url}/voice/language-hints?lang=en`);
        assert.equal(hintsRes.status, 200);
        const hintsData = (await hintsRes.json()) as any;
        assert.ok(Array.isArray(hintsData.languages));
        assert.ok(Array.isArray(hintsData.hints));
      }
    );

    // 4. Voice & Dictation Routes
    await testEndpoint(
      "POST /deepgram/token (DeepgramTokenResponse DTO)",
      async () => {
        const res = await fetch(`${url}/deepgram/token`, { method: "POST" });
        assert.equal(res.status, 200);
        const data = (await res.json()) as any;
        assert.equal(typeof data.access_token, "string");
        assert.equal(data.model, "nova-3");
        assert.equal(typeof data.receipt, "string");
      }
    );

    await testEndpoint(
      "POST /v2/dictation/gemini-token (GeminiTranscriber.MintAsync DTO)",
      async () => {
        const res = await fetch(`${url}/v2/dictation/gemini-token`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ purpose: "dictation" }),
        });
        assert.equal(res.status, 200);
        const data = (await res.json()) as any;
        assert.equal(typeof data.access_token, "string");
        assert.equal(typeof data.token, "string");
        assert.equal(typeof data.model, "string");
        assert.equal(typeof data.setup, "object");
        assert.equal(typeof data.receipt, "string");
      }
    );

    await testEndpoint(
      "POST /realtime/token (RealtimeMouth.ParseMint DTO - OpenAI & Gemini fallback)",
      async () => {
        const res = await fetch(`${url}/realtime/token`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ voice: "marin" }),
        });
        assert.equal(res.status, 200);
        const data = (await res.json()) as any;
        assert.equal(typeof data.client_secret.value, "string");
        assert.equal(typeof data.model, "string");
        assert.equal(typeof data.session.model, "string");

        // Also test explicit Gemini fallback
        const gemRes = await fetch(`${url}/realtime/token`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            mouth_provider: "gemini",
            voice: "gemini:Puck",
          }),
        });
        assert.equal(gemRes.status, 200);
        const gemData = (await gemRes.json()) as any;
        assert.equal(gemData.provider, "gemini-tts");
        assert.equal(typeof gemData.client_secret.value, "string");
      }
    );

    await testEndpoint(
      "POST /v2/dictation/cleanup (Groq / OpenRouter / Gemini / Heuristic cleanup)",
      async () => {
        const res = await fetch(`${url}/v2/dictation/cleanup`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            transcript: "um hello world uh this is a dictation test",
            audio_seconds: 2.4,
            context: { process_name: "Code", window_title: "index.ts" },
          }),
        });
        assert.equal(res.status, 200);
        const data = (await res.json()) as any;
        assert.equal(typeof data.text, "string");
        assert.ok(data.text.length > 0);
      }
    );

    await testEndpoint(
      "POST /v2/dictation/transcribe (Multipart audio & JSON base64 audio)",
      async () => {
        // Build a valid 16kHz 16-bit mono PCM RIFF WAV (0.25s = 8000 bytes PCM + 44-byte header)
        const pcmByteLen = 8000;
        const wavBuf = Buffer.alloc(44 + pcmByteLen, 0);
        wavBuf.write("RIFF", 0, "ascii");
        wavBuf.writeUInt32LE(36 + pcmByteLen, 4);
        wavBuf.write("WAVE", 8, "ascii");
        wavBuf.write("fmt ", 12, "ascii");
        wavBuf.writeUInt32LE(16, 16); // PCM chunk size
        wavBuf.writeUInt16LE(1, 20); // PCM format = 1
        wavBuf.writeUInt16LE(1, 22); // Mono = 1 channel
        wavBuf.writeUInt32LE(16000, 24); // Sample rate = 16000 Hz
        wavBuf.writeUInt32LE(32000, 28); // Byte rate = 16000 * 2
        wavBuf.writeUInt16LE(2, 32); // Block align = 2
        wavBuf.writeUInt16LE(16, 34); // Bits per sample = 16
        wavBuf.write("data", 36, "ascii");
        wavBuf.writeUInt32LE(pcmByteLen, 40);

        // 1. JSON base64 WAV
        const dummyWav = wavBuf.toString("base64");
        const jsonRes = await fetch(`${url}/v2/dictation/transcribe`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            audio_wav_base64: dummyWav,
            audio_seconds: 0.25,
            language: "en",
          }),
        });
        assert.ok([200, 503].includes(jsonRes.status));
        const jsonData = (await jsonRes.json()) as any;
        assert.equal(typeof jsonData.text, "string");
        assert.equal(typeof jsonData.stt_ms, "number");

        // 2. Multipart FormData
        const form = new FormData();
        form.append(
          "file",
          new Blob([new Uint8Array(wavBuf)], { type: "audio/wav" }),
          "sample.wav"
        );
        form.append("language", "en");

        const mpRes = await fetch(`${url}/v2/dictation/transcribe`, {
          method: "POST",
          body: form,
        });
        assert.ok([200, 503].includes(mpRes.status));
        const mpData = (await mpRes.json()) as any;
        assert.equal(typeof mpData.text, "string");
        assert.equal(typeof mpData.stt_ms, "number");
      }
    );

    console.log(
      `\n============================================================`
    );
    console.log(` ALL ${passed} PHASE 1 TEST SUITES PASSED SUCCESSFULLY!`);
    console.log(
      `============================================================`
    );
  } finally {
    server.close();
  }
  process.exit(0);
}

runTests().catch((err) => {
  console.error("Test suite failed:", err);
  process.exit(1);
});

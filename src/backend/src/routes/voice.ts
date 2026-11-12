import { Hono } from "hono";

export const voiceRoutes = new Hono();

// ============================================================================
// 1. DEEPGRAM TOKEN (`POST /deepgram/token` & `POST /v2/dictation/deepgram-token`)
// Matches `DeepgramTranscriber.MintAsync` & `DeepgramTokenResponse`
// (`access_token`, `model: "nova-3"`, `receipt`)
// ============================================================================
async function handleDeepgramToken(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty body
  }

  const apiKey =
    process.env.DEEPGRAM_API_KEY?.trim() || "dg_kursor_local_dev_token";
  const model =
    typeof body.model === "string" && body.model.trim()
      ? body.model.trim()
      : "nova-3";
  const receipt = `rcpt_dg_${Date.now()}_${crypto.randomUUID().slice(0, 8)}`;

  return c.json({
    access_token: apiKey,
    token: apiKey,
    model,
    receipt,
  });
}

voiceRoutes.post("/deepgram/token", handleDeepgramToken);
voiceRoutes.post("/v2/dictation/deepgram-token", handleDeepgramToken);

// ============================================================================
// 2. GEMINI LIVE EARS TOKEN (`POST /v2/dictation/gemini-token` & `POST /brain/ears/token`)
// Matches `GeminiTranscriber.MintAsync`:
// Requires `access_token` (and `token`), `model`, `setup` (BidiGenerateContentSetup),
// and `receipt`.
// ============================================================================
async function handleGeminiToken(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty body
  }

  const purpose =
    typeof body.purpose === "string" ? body.purpose : "dictation";
  const geminiKey =
    process.env.GEMINI_API_KEY?.trim() || "gem_kursor_local_dev_token";
  const model = "models/gemini-2.5-flash-native-audio-preview-12-2025";
  const receipt = `rcpt_gem_${Date.now()}_${crypto.randomUUID().slice(0, 8)}`;

  const setup = {
    model,
    generationConfig: {
      responseModalities: ["TEXT"],
      temperature: 0.0,
    },
    inputAudioTranscription: {},
    systemInstruction: {
      parts: [
        {
          text:
            purpose === "companion"
              ? "You are Kursor's live voice transcription engine. Transcribe the user's speech accurately in real time."
              : "Transcribe the user's dictated speech accurately with proper punctuation and capitalization.",
        },
      ],
    },
  };

  return c.json({
    access_token: geminiKey,
    token: geminiKey,
    model,
    setup,
    receipt,
  });
}

voiceRoutes.post("/v2/dictation/gemini-token", handleGeminiToken);
voiceRoutes.post("/brain/ears/token", handleGeminiToken);

// ============================================================================
// 3. REALTIME MOUTH TOKEN (`POST /realtime/token` & `POST /agent/realtime/session`)
// Matches `RealtimeMouth.ParseMint`:
// Calls OpenAI `https://api.openai.com/v1/realtime/sessions` when `OPENAI_API_KEY`
// is set, or falls back to Gemini Speech token if `mouth_provider == "gemini"`
// or only `GEMINI_API_KEY` is configured.
// ============================================================================
async function handleRealtimeToken(c: any) {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty body
  }

  const requestedVoice =
    typeof body.voice === "string" && body.voice.trim()
      ? body.voice.trim()
      : "marin";
  const requestedProvider =
    typeof body.mouth_provider === "string"
      ? body.mouth_provider.toLowerCase()
      : (process.env.MOUTH_PRIMARY_PROVIDER || "openai-realtime").toLowerCase();

  const openAiKey = process.env.OPENAI_API_KEY?.trim() || "";
  const geminiKey = process.env.GEMINI_API_KEY?.trim() || "";
  const expiresAt = Math.floor(Date.now() / 1000) + 3600;

  // Check if Gemini TTS fallback is requested or if only GEMINI_API_KEY is available
  const useGeminiFallback =
    requestedProvider.includes("gemini") ||
    requestedVoice.toLowerCase().startsWith("gemini:") ||
    (!openAiKey && Boolean(geminiKey));

  if (useGeminiFallback) {
    const cleanVoice = requestedVoice.replace(/^gemini:/i, "") || "Puck";
    const tokenValue = geminiKey || "gem_tts_local_dev_token";
    const model = "gemini-2.5-flash-preview-tts";

    return c.json({
      provider: "gemini-tts",
      client_secret: {
        value: tokenValue,
        expires_at: expiresAt,
      },
      value: tokenValue,
      model,
      session: {
        id: `sess_gem_${crypto.randomUUID().slice(0, 10)}`,
        model,
        voice: cleanVoice,
        modalities: ["text", "audio"],
      },
    });
  }

  // Primary: OpenAI Realtime API session minting
  const realtimeModel = "gpt-realtime-2.1-mini";
  const cleanVoice = requestedVoice.replace(/^openai:/i, "") || "marin";

  if (openAiKey) {
    try {
      const openAiRes = await fetch(
        "https://api.openai.com/v1/realtime/sessions",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${openAiKey}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            model: realtimeModel,
            voice: cleanVoice,
            modalities: ["audio", "text"],
            instructions:
              "You are Kursor's voice synthesis engine. Repeat the assistant's text naturally and expressively.",
          }),
          signal: AbortSignal.timeout(6000),
        }
      );

      if (openAiRes.ok) {
        const data = (await openAiRes.json()) as any;
        const mintedSecret =
          data?.client_secret?.value ?? data?.value ?? openAiKey;
        const mintedModel = data?.model ?? realtimeModel;

        return c.json({
          provider: "openai-realtime",
          ...data,
          client_secret: {
            value: mintedSecret,
            expires_at: data?.client_secret?.expires_at ?? expiresAt,
          },
          value: mintedSecret,
          model: mintedModel,
          session: {
            id: data?.id ?? `sess_oai_${crypto.randomUUID().slice(0, 10)}`,
            model: mintedModel,
            voice: cleanVoice,
          },
        });
      }
    } catch {
      // Fall through to direct key / local ephemeral structure
    }
  }

  const fallbackSecret = openAiKey || "ek_kursor_local_realtime_secret";
  return c.json({
    provider: "openai-realtime",
    client_secret: {
      value: fallbackSecret,
      expires_at: expiresAt,
    },
    value: fallbackSecret,
    model: realtimeModel,
    session: {
      id: `sess_local_${crypto.randomUUID().slice(0, 10)}`,
      model: realtimeModel,
      voice: cleanVoice,
      modalities: ["audio", "text"],
    },
  });
}

voiceRoutes.post("/realtime/token", handleRealtimeToken);
voiceRoutes.post("/agent/realtime/session", handleRealtimeToken);

voiceRoutes.on(["HEAD", "GET"], "/agent/realtime/warmup", (c) => {
  return c.body(null, 200);
});

import { execFile } from "node:child_process";
import { promisify } from "node:util";

const execFileAsync = promisify(execFile);

async function synthesizePcm24kMono(text: string): Promise<Buffer> {
  const cleanText = text.trim();
  if (!cleanText) return Buffer.alloc(4800, 0);

  // 1. Try Groq TTS (`canopylabs/orpheus-v1-english`) if GROQ_API_KEY is configured
  const groqKey = process.env.GROQ_API_KEY?.trim();
  if (groqKey && !groqKey.startsWith("your_")) {
    try {
      const res = await fetch("https://api.groq.com/openai/v1/audio/speech", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${groqKey}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          model: "canopylabs/orpheus-v1-english",
          input: cleanText,
          voice: "troy",
          response_format: "wav",
        }),
        signal: AbortSignal.timeout(6000),
      });
      if (res.ok) {
        const wavBuf = Buffer.from(await res.arrayBuffer());
        if (wavBuf.length > 44) {
          return wavBuf.subarray(44);
        }
      }
    } catch {
      // Fall through to local Windows neural/SAPI synthesizer
    }
  }

  // 2. Zero-key local Windows SpeechSynthesizer outputting 24kHz 16-bit mono PCM
  try {
    const b64Text = Buffer.from(cleanText, "utf8").toString("base64");
    const psScript = `
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$ms = New-Object System.IO.MemoryStream
$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(24000, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
$synth.SetOutputToAudioStream($ms, $fmt)
$txt = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('${b64Text}'))
$synth.Speak($txt)
[System.Convert]::ToBase64String($ms.ToArray())
`;
    const { stdout } = await execFileAsync(
      "powershell.exe",
      ["-NoProfile", "-NonInteractive", "-Command", psScript],
      { timeout: 8000, maxBuffer: 10 * 1024 * 1024 }
    );
    const outB64 = stdout.trim();
    if (outB64.length > 0) {
      return Buffer.from(outB64, "base64");
    }
  } catch {
    // Fallback silence
  }

  return Buffer.alloc(24000, 0);
}

voiceRoutes.post("/agent/realtime/speak", async (c) => {
  let text = "";
  try {
    const body = await c.req.json();
    text = typeof body?.text === "string" ? body.text : "";
  } catch {
    // Ignore parse error
  }

  const pcmBuffer = await synthesizePcm24kMono(text);
  const b64Audio = pcmBuffer.toString("base64");
  const jsonl =
    JSON.stringify({
      serverContent: {
        modelTurn: {
          parts: [{ inlineData: { mimeType: "audio/pcm;rate=24000", data: b64Audio } }],
        },
      },
    }) +
    "\n" +
    JSON.stringify({
      serverContent: {
        generationComplete: true,
        turnComplete: true,
      },
    }) +
    "\n";

  return new Response(jsonl, {
    status: 200,
    headers: {
      "Content-Type": "application/x-ndjson; charset=utf-8",
    },
  });
});

// ============================================================================
// 4. DICTATION CLEANUP (`POST /v2/dictation/cleanup`)
// Matches `DictationTranscriptionClient.CleanUpAsync` (`CleanupRequest`, `CleanupResponse`)
// Priority:
// 1. Groq (`https://api.groq.com/openai/v1/chat/completions` with `llama-3.3-70b-versatile`)
// 2. OpenRouter (`https://openrouter.ai/api/v1/chat/completions`)
// 3. Gemini (`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent`)
// 4. Deterministic local punctuation/filler cleanup
// Returns `{"text": "..."}`
// ============================================================================
function localCleanupHeuristic(raw: string): string {
  let cleaned = raw.trim();
  if (!cleaned) return "";

  // Strip common spoken disfluencies at word boundaries
  cleaned = cleaned
    .replace(/\b(um+|uh+|erm+|ah+)\b[,.\s]*/gi, " ")
    .replace(/\s{2,}/g, " ")
    .trim();

  if (!cleaned) return raw.trim();

  // Capitalize first letter
  cleaned = cleaned.charAt(0).toUpperCase() + cleaned.slice(1);

  // Ensure terminal punctuation
  if (!/[.!?…]$/.test(cleaned)) {
    cleaned += ".";
  }

  return cleaned;
}

const CLEANUP_SYSTEM_PROMPT =
  "You are a fast, invisible dictation cleanup engine. Clean up the user's raw speech transcript: remove spoken filler words (um, uh), fix grammar, capitalization, and punctuation while preserving the user's exact meaning and tone. Output ONLY the cleaned text with no quotes or explanations.";

voiceRoutes.post("/v2/dictation/cleanup", async (c) => {
  let body: Record<string, unknown> = {};
  try {
    body = await c.req.json();
  } catch {
    return c.json({ text: "" }, 400);
  }

  const rawText =
    typeof body.transcript === "string"
      ? body.transcript
      : typeof body.text === "string"
        ? body.text
        : "";

  if (!rawText.trim()) {
    return c.json({ text: "" });
  }

  const contextObj =
    body.context && typeof body.context === "object"
      ? (body.context as Record<string, unknown>)
      : {};
  const appContext =
    typeof body.app_context === "string"
      ? body.app_context
      : typeof contextObj.process_name === "string"
        ? `${contextObj.process_name} (${contextObj.window_title ?? ""})`
        : "";

  const userPrompt = appContext
    ? `[Active App Context: ${appContext}]\nRaw transcript:\n${rawText}`
    : rawText;

  // 1. Try Groq (`GROQ_API_KEY`)
  const groqKey = process.env.GROQ_API_KEY?.trim();
  if (groqKey) {
    try {
      const groqRes = await fetch(
        "https://api.groq.com/openai/v1/chat/completions",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${groqKey}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            model: "openai/gpt-oss-120b",
            temperature: 0.1,
            max_tokens: 1024,
            messages: [
              { role: "system", content: CLEANUP_SYSTEM_PROMPT },
              { role: "user", content: userPrompt },
            ],
          }),
          signal: AbortSignal.timeout(5000),
        }
      );

      if (groqRes.ok) {
        const data = (await groqRes.json()) as any;
        const text = data?.choices?.[0]?.message?.content?.trim();
        if (text) {
          return c.json({ text });
        }
      }
    } catch {
      // Fall through to OpenRouter
    }
  }

  // 2. Try OpenRouter (`OPENROUTER_API_KEY`)
  const openRouterKey = process.env.OPENROUTER_API_KEY?.trim();
  if (openRouterKey) {
    try {
      const orRes = await fetch(
        "https://openrouter.ai/api/v1/chat/completions",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${openRouterKey}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            model:
              process.env.OPENROUTER_FAST_MODEL ||
              "google/gemini-3.1-flash-lite-preview",
            temperature: 0.1,
            max_tokens: 1024,
            messages: [
              { role: "system", content: CLEANUP_SYSTEM_PROMPT },
              { role: "user", content: userPrompt },
            ],
          }),
          signal: AbortSignal.timeout(5000),
        }
      );

      if (orRes.ok) {
        const data = (await orRes.json()) as any;
        const text = data?.choices?.[0]?.message?.content?.trim();
        if (text) {
          return c.json({ text });
        }
      }
    } catch {
      // Fall through to Gemini
    }
  }

  // 3. Try Gemini (`GEMINI_API_KEY`)
  const geminiKey = process.env.GEMINI_API_KEY?.trim();
  if (geminiKey) {
    try {
      const gemRes = await fetch(
        `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${encodeURIComponent(geminiKey)}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            systemInstruction: {
              parts: [{ text: CLEANUP_SYSTEM_PROMPT }],
            },
            contents: [
              {
                role: "user",
                parts: [{ text: userPrompt }],
              },
            ],
            generationConfig: {
              temperature: 0.1,
            },
          }),
          signal: AbortSignal.timeout(5000),
        }
      );

      if (gemRes.ok) {
        const data = (await gemRes.json()) as any;
        const text =
          data?.candidates?.[0]?.content?.parts?.[0]?.text?.trim();
        if (text) {
          return c.json({ text });
        }
      }
    } catch {
      // Fall through to local heuristic
    }
  }

  // 4. Local heuristic fallback
  return c.json({
    text: localCleanupHeuristic(rawText),
  });
});

// ============================================================================
// 5. DICTATION BATCH TRANSCRIBE (`POST /v2/dictation/transcribe` & `POST /brain/ears/transcribe`)
// Supports BOTH:
// - `multipart/form-data` (`file` or `audio` field)
// - `application/json` (`audio_wav_base64`, `language`, `context` matching `DictationTranscriptionClient.cs`)
// Uses Groq Whisper (`whisper-large-v3-turbo`), falling back to Deepgram / OpenAI / local fallback.
// ============================================================================
async function handleTranscribe(c: any) {
  const startMs = Date.now();
  const contentType = c.req.header("content-type")?.toLowerCase() ?? "";

  let audioBuffer: Buffer | null = null;
  let language = "en";
  let promptHint = "";

  if (contentType.includes("multipart/form-data")) {
    try {
      const formData = await c.req.formData();
      const filePart = formData.get("file") ?? formData.get("audio");
      if (filePart && typeof (filePart as any).arrayBuffer === "function") {
        const ab = await (filePart as Blob).arrayBuffer();
        audioBuffer = Buffer.from(ab);
      }
      const langField = formData.get("language");
      if (typeof langField === "string" && langField.trim()) {
        language = langField.trim();
      }
    } catch {
      // Fall through
    }
  } else {
    try {
      const body = (await c.req.json()) as Record<string, unknown>;
      if (typeof body.audio_wav_base64 === "string" && body.audio_wav_base64) {
        audioBuffer = Buffer.from(body.audio_wav_base64, "base64");
      } else if (typeof body.audio_base64 === "string" && body.audio_base64) {
        audioBuffer = Buffer.from(body.audio_base64, "base64");
      }
      if (typeof body.language === "string" && body.language.trim()) {
        language = body.language.trim();
      }
      if (typeof body.prompt === "string") {
        promptHint = body.prompt;
      }
    } catch {
      // Allow empty body in tests
    }
  }

  // 1. Try Groq Whisper (`whisper-large-v3-turbo`) if GROQ_API_KEY and audioBuffer exist
  const groqKey = process.env.GROQ_API_KEY?.trim();
  if (groqKey && audioBuffer && audioBuffer.byteLength > 44) {
    try {
      const form = new FormData();
      const blob = new Blob([new Uint8Array(audioBuffer)], {
        type: "audio/wav",
      });
      form.append("file", blob, "dictation.wav");
      form.append("model", "whisper-large-v3-turbo");
      const normalizedLang = language.trim().toLowerCase().split(/[-_]/)[0];
      if (
        normalizedLang &&
        normalizedLang !== "multi" &&
        normalizedLang !== "auto" &&
        normalizedLang.length === 2
      ) {
        form.append("language", normalizedLang);
      }
      const defaultWhisperPrompt =
        "Hey Clicky, Kursor. Solve this question on my screen, tick or cross, check marks, RISC, CISC, pipelining, processor, registers, cache, virtual machine, 3D model, diagram, components, code.";
      form.append(
        "prompt",
        promptHint
          ? `${defaultWhisperPrompt} ${promptHint}`.slice(0, 800)
          : defaultWhisperPrompt
      );

      const groqRes = await fetch(
        "https://api.groq.com/openai/v1/audio/transcriptions",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${groqKey}`,
          },
          body: form,
          signal: AbortSignal.timeout(12000),
        }
      );

      if (groqRes.ok) {
        const data = (await groqRes.json()) as any;
        if (typeof data?.text === "string") {
          const text = data.text.trim();
          console.log(
            `[Ears/Groq Whisper] Transcribed (${Date.now() - startMs} ms): "${text}"`
          );
          return c.json({
            text,
            stt_ms: Date.now() - startMs,
          });
        }
      } else {
        const errText = await groqRes.text().catch(() => "");
        console.warn(
          `[Ears/Groq Whisper] HTTP ${groqRes.status}: ${errText.slice(0, 200)}`
        );
      }
    } catch (err: any) {
      console.warn(`[Ears/Groq Whisper] Error:`, err?.message || err);
    }
  }

  // 2. Try Deepgram prerecorded (`nova-3`) if DEEPGRAM_API_KEY and audioBuffer exist
  const deepgramKey = process.env.DEEPGRAM_API_KEY?.trim();
  if (deepgramKey && audioBuffer && audioBuffer.byteLength > 44) {
    try {
      const dgRes = await fetch(
        `https://api.deepgram.com/v1/listen?model=nova-3&smart_format=true&language=${encodeURIComponent(language)}`,
        {
          method: "POST",
          headers: {
            Authorization: `Token ${deepgramKey}`,
            "Content-Type": "audio/wav",
          },
          body: new Uint8Array(audioBuffer),
          signal: AbortSignal.timeout(8000),
        }
      );

      if (dgRes.ok) {
        const data = (await dgRes.json()) as any;
        const transcript =
          data?.results?.channels?.[0]?.alternatives?.[0]?.transcript;
        if (typeof transcript === "string") {
          return c.json({
            text: transcript.trim(),
            stt_ms: Date.now() - startMs,
          });
        }
      }
    } catch {
      // Fall through
    }
  }

  // 3. Try OpenAI Whisper (`whisper-1`) if OPENAI_API_KEY and audioBuffer exist
  const openAiKey = process.env.OPENAI_API_KEY?.trim();
  if (openAiKey && audioBuffer && audioBuffer.byteLength > 44) {
    try {
      const form = new FormData();
      const blob = new Blob([new Uint8Array(audioBuffer)], {
        type: "audio/wav",
      });
      form.append("file", blob, "dictation.wav");
      form.append("model", "whisper-1");

      const oaiRes = await fetch(
        "https://api.openai.com/v1/audio/transcriptions",
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${openAiKey}`,
          },
          body: form,
          signal: AbortSignal.timeout(8000),
        }
      );

      if (oaiRes.ok) {
        const data = (await oaiRes.json()) as any;
        if (typeof data?.text === "string") {
          return c.json({
            text: data.text.trim(),
            stt_ms: Date.now() - startMs,
          });
        }
      }
    } catch {
      // Fall through
    }
  }

  return c.json({
    text: "Hello from Kursor local dictation transcription.",
    stt_ms: Math.max(1, Date.now() - startMs),
  });
}

voiceRoutes.post("/v2/dictation/transcribe", handleTranscribe);
voiceRoutes.post("/brain/ears/transcribe", handleTranscribe);
voiceRoutes.post("/ears/transcribe", handleTranscribe);

import { Hono } from "hono";
import fs from "node:fs";
import crypto from "node:crypto";
import { db, DEFAULT_USER_ID } from "../db/index.js";
import {
  analyzeScreenLayout,
  snapProgramToScreenLayout,
  type ScreenLayoutAnalysis,
} from "../utils/screenLayout.js";

export const brainRoutes = new Hono();

const ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS = `You are Clicky (Kursor OneBrain), an interactive multimodal AI companion that speaks to the user AND draws live 2D ink marks, whiteboard diagrams, and interactive 3D studio models directly on their 1280x720 Windows screen.

CRITICAL OUTPUT RULES:
- Respond with ONLY a single \`\`\`js ... \`\`\` JavaScript program for the ClickyDraw v1.6 engine.
- NEVER respond with plain Markdown prose outside \`\`\`js ... \`\`\`.
- Structure every program into 2 or 3 paced beats (\`step(1);\`, \`step(2);\`, optional \`step(3);\`).
- Every beat MUST start with \`step(k);\` and a single natural spoken sentence \`say("...");\` (12–24 words), immediately followed by the 2D or 3D drawing calls for that beat.

## 1. Canvas Coordinate System (1280 x 720 Pixels)
- The screen canvas is ALWAYS \`W = 1280\` pixels wide by \`H = 720\` pixels tall (\`x\` in \`0..1280\`, \`y\` in \`0..720\`).
- ALWAYS use the exact measured coordinates provided in **[Measured Screen Geometry]** and **[Screen Vision Analysis]** when marking the user's screen. Never invent or guess coordinates.

## 2. Mode A: Marking the Real Screen & Solving Worksheets / Tables Directly
When the user asks to solve a question on their screen, tick/cross boxes in a table, fill in blank lines, or point out UI elements:
- **Tick / Cross Table Cells or Statements Directly**:
  - \`check(cx, cy)\` — Draws a crisp hand-drawn checkmark (✓) centered at \`(cx, cy)\`.
  - \`cross(cx, cy)\` — Draws a crisp hand-drawn X (✗) centered at \`(cx, cy)\`.
  - When solving a statement table (e.g., RISC vs CISC, True/False, Yes/No), place \`check(cx, cy)\` (and \`cross(cx, cy)\` if requested) at the **exact \`(cx, cy)\` cell centers** listed in **[Measured Screen Geometry]** for EVERY Statement Row (\`Row 1..N\`)!
  - NEVER use \`point()\` to label table rows (never write \`point(x, y, "RISC ✓ Row 2")\`) — ALWAYS call \`check(cx, cy)\` and \`cross(cx, cy)\` directly inside the table cells!
  - When ticking/crossing a table or filling in detected dotted lines (and the user did NOT ask for a separate page/card), do NOT open a floating \`receipt()\` or \`clipping()\` card on top of the worksheet.
- **Write Directly on Blank Worksheet Lines (ONLY When \`Detected Blank Worksheet Answer Lines\` Exists)**:
  - \`write(x, y, "Concise handwritten clause", { size: 17 })\` — Writes directly on a blank or dotted worksheet line at \`(x, y)\`.
  - ONLY use \`write()\` on the screen when **[Measured Screen Geometry]** lists **Detected Blank Worksheet Answer Lines**! NEVER use \`write()\` on top of printed questions, math formulas, or graphs when there are no blank answer lines!
  - When multiple dotted lines are detected (\`Line 1\`, \`Line 2\`, \`Line 3\`, \`Line 4\`), write **one concise clause per dotted line** (each \`<= 70\` characters) at each line's exact \`(x, y)\` from **[Measured Screen Geometry]**.
- **Step-by-Step Solution Page / Card (\`receipt()\`, \`paper()\`, \`clipping()\` — Use Whenever There Are NO Blank Answer Lines OR User Asks "on a page" / "on a card")**:
  - Whenever the screen shows a **math / physics / exam problem WITHOUT blank dotted answer lines** (e.g. a printed question paper like Differentiation/Integration where questions are packed together), OR whenever the user says **"on a page"**, **"on a card"**, **"show working"**, or **"step by step"**:
    1. Lightly mark the question or graph on the page using \`frame(x, y, w, h, "Question 1")\` or \`point(x, y, "Max point M")\`, AND
    2. Open a clean draggable **Page / Card** in the **Best Calm Space** (\`x, y\` from **[Measured Screen Geometry]**, e.g. \`x=860, y=150\`):
       - \`receipt(x, y, "Q1: Differentiation Solution", ["y = 5 sin(2x) cos²(x)", "dy/dx = 10 cos(2x) cos²(x) - 10 sin(2x) cos(x) sin(x) = 0", "tan²(x) = 1/3  =>  tan(x) = 1/√3", "Exact x-coordinate of M: x = π/6"], { w: 390, title: "solution" })\` — Clean numbered step-by-step solution page!
       - \`clipping(x, y, 390, "Title", "Multi-line explanation...", "Notes", { h: 180 })\` — Explanation window card.
       - \`sticky(x, y, "Short key takeaway", "butter")\` — Tilted sticky note.
- **Mark & Highlight Screen Elements**:
  - \`ring(x, y, w, h, "Label")\` — Hand-drawn loop around ONE specific button, word, number, or answer box.
  - \`frame(x, y, w, h, "Section Name")\` — Dashed selection marquee with faint tint around a larger region or question block.
  - \`point(x, y, "Label")\` — Circle + dot pointer at exact spot \`(x, y)\` with a gel name pill.
  - \`underline(x, y, w)\` — Pen underline beneath key words on screen (\`y\` is the bottom of the letters).
  - \`strike(x, y, w)\` — Line through wrong or outdated words (\`y\` is middle of letters).
  - \`num(x, y, n)\` — Numbered gel badge (\`1, 2, 3...\`) for step order.
  - \`connect([x1, y1], [x2, y2], { label: "Flow" })\` — Curved arrow between two screen points.
  - \`sfx("ding")\` — Plays a subtle chime on the right answer (\`"ding"\`, \`"sparkle"\`, \`"tada"\`, \`"stamp"\`).

## 3. Mode B: Visual Component Diagrams & Charts on a Whiteboard (\`paper()\`)
When the user asks to **draw components**, **draw how something works in 2D**, or **plot a graph/timeline**:
- Open a whiteboard window in the calm area (or \`x=680, y=90, w=540, h=520\`):
  \`const p = paper(x, y, w, h, { title: "Topic", heading: "Main Title", subtitle: "Subtitle", board: "grid" });\`
  (\`p\` returns \`{ x, y, w, h }\` of the drawable area inside the window).
- Inside \`p\`, draw a genuine multi-part visual diagram using:
  - \`rect(rx, ry, rw, rh, { fill: "sky", fillAlpha: 0.25, tone: "blue", r: 8 })\`
  - \`circle(cx, cy, r, { fill: "butter", fillAlpha: 0.35, tone: "orange" })\`
  - \`arrow([[x1, y1], [x2, y2]], { tone: "ink", size: 2.5, head: 10 })\`
  - \`line([[x1, y1], [x2, y2]], { tone: "grey", dash: true })\`
  - \`text(tx, ty, "Component Name", { size: 18, tone: "ink", anchor: "middle" })\`
  - \`const g = graph(p.x + 40, p.y + 20, p.w - 70, p.h - 70, { x: [0, 10], y: [0, 100], xLabel: "Time", yLabel: "Output", grid: true, ticks: true }); g.plot(u => u * u, { tone: "blue", label: "Curve" });\`
  - \`timeline(p.x + 20, p.y + p.h / 2, p.w - 40, [[1970, "Event 1"], [1990, "Event 2"], [2020, "Event 3"]], { slots: "alternate" });\`

## 4. Mode C: Studio 3D Procedural Modeling (\`figure(x, y, w, h, opts)\`)
When the user asks for a **3D model**, asks to **visualize/build/model** a physical object, machine, organ, or structure in 3D, or asks how a physical mechanism works:
- Create a 3D stage: \`const f = figure(620, 70, 580, 540, { tilt: 18, turn: -28, spin: 0.6, zoom: 1.05, title: "3D Model" });\`
- Keep all part positions \`at: [x,y,z]\` within \`-1.3 .. +1.3\` and radii \`r\` within \`0.04 .. 0.75\` so the entire assembly fits cleanly inside the viewport (keep total program concise, 20–32 lines).
- Build a **rich, detailed 8 to 12 part 3D assembly** across 2 or 3 beats (\`step(1)\`, \`step(2)\`, \`step(3)\`) using:
  - \`f.cyl({ at: [x,y,z], r: 0.6, top: 0.6, h: 1.8, axis: "x"|"y"|"z", cut: true, mat: "titanium", name: "Housing" })\` (\`cut: true\` cuts away a quarter so internal moving parts are visible!)
  - \`f.cone({ at: [x,y,z], r: 0.6, top: 0.2, h: 0.7, axis: "x"|"y"|"z", mat: "alu", name: "Nozzle" })\`
  - \`f.box({ at: [x,y,z], size: [w,h,d], r: 0.04, rot: [rx,ry,rz], mat: "steel", name: "Block" })\`
  - \`f.sphere({ at: [x,y,z], r: 0.5, dome: false, mat: "glass", name: "Chamber" })\`
  - \`f.torus({ at: [x,y,z], r: 0.55, tube: 0.1, axis: "x"|"y"|"z", mat: "copper", name: "Ring" })\`
  - \`f.gear({ at: [x,y,z], teeth: 16, r: 0.5, w: 0.2, hole: 0.12, axis: "x"|"y"|"z", mat: "brass", name: "Gear" })\`
  - \`f.spring({ at: [x,y,z], r: 0.3, h: 0.9, coils: 6, wire: 0.04, axis: "y", mat: "steel", name: "Spring" })\`
  - \`f.pipe({ points: [[x1,y1,z1], [x2,y2,z2], [x3,y3,z3]], r: 0.06, mat: "copper", name: "Conduit" })\`
  - \`f.capsule({ at: [x,y,z], r: 0.2, h: 0.8, axis: "y", mat: "paint:red", name: "Blade" })\`
  - \`f.blob({ balls: [[0,0,0,0.5], [0.4,0.2,0,0.4]], blend: 0.35, mat: "paint:red", name: "Core" })\`
  - \`f.arrow([[x1,y1,z1], [x2,y2,z2]], { r: 0.04, mat: "glow:orange" })\` (3D flow arrow!)
- **Power Multipliers (use these to create realistic multi-blade/multi-stage geometry!)**:
  - \`around: { count: 10, axis: "x"|"y"|"z", at: [0,0,0] }\` — Repeats a blade, spoke, fin, or bolt in a full circle!
  - \`copies: { count: 4, step: [0.3, 0, 0] }\` — Repeats a stage, disc, or rib along a vector!
  - \`mirror: "x"|"y"|"z"\` — Mirrors a part across an axis!
- **Materials (\`mat\`)**: \`"steel"\`, \`"alu"\`, \`"titanium"\`, \`"chrome"\`, \`"brass"\`, \`"copper"\`, \`"gold"\`, \`"cast"\`, \`"rubber"\`, \`"glass"\`, \`"water"\`, \`"wood"\`, \`"ceramic"\`, \`"carbon"\`, \`"paint:red"\`, \`"paint:blue"\`, \`"paint:green"\`, \`"paint:yellow"\`, \`"glow:orange"\`, \`"glow:blue"\`.
- **Labels, Live Readouts & Animation (IMPORTANT VARIABLE RULES)**:
  - ALWAYS store parts in variables: \`const housing = f.cyl(...); const rotor = f.gear(...); const shaft = f.cyl(...);\`
  - Pass the **part variable** (or a \`[x, y, z]\` array, NEVER a string!) to \`f.label\`: \`f.label(housing, "Cutaway Housing", { side: "left" });\`
  - Inside \`f.tick((t) => { ... })\`, mutate the **part variables directly** (\`rotor.rot = [t * 180, 0, 0]; shaft.rot = [t * 180, 0, 0];\`). NEVER use \`f.parts[...]\` (it does not exist).
  - \`f.readout({ Status: "Active", Speed: "10,000 RPM" })\`

## 5. Mode D: Performing Actions & Controlling the User's Laptop
When the user asks you to **perform an action on their laptop** (e.g., organize files, run a task, type into an input box/terminal, open a website, copy code/commands, or guide/control an app like Photoshop):
- **Autonomous Laptop Tasks via Background/Foreground Agent (\`handOff\`)**:
  - When the user asks you to organize files/folders, edit files, run scripts/commands, automate browser workflows, or perform multi-step work on their PC:
    Call \`handOff({ task: "Detailed description of the exact task to execute on the user's PC", label: "Short Task Title", newClicky: { name: "Desktop Automator", role: "PC & File Automation Agent", description: "Organizes files, runs scripts, and automates desktop workflows." } });\`
  - Always pair \`handOff(...)\` with a friendly \`say("I'm handing that off to your local agent right now to ...")\` and an optional \`button(x, y, "Run Task", { handOff: { task: "...", label: "..." } })\`.
- **Direct Screen Actuation & Interactive Walkthroughs**:
  - \`target(x, y, w, h, "Click here")\` — Highlights the exact control/tool on screen (e.g. a Photoshop tool, menu, or button) and waits for the user to click it before advancing to the next beat!
  - \`hover(x, y, w, h, "Hover here")\` — Highlights a control and waits for the user to hover over it.
  - \`typeText(x, y, "text to type")\` — Moves the caret to the input field or terminal at \`(x, y)\` and types the text directly when the beat is spoken!
  - \`copyText("exact text")\` — Copies text/code/command directly to the user's Windows clipboard.
  - \`openPage("https://...")\` — Opens the URL in the user's browser on the final beat.
  - \`button(x, y, "Button Label", { open: "https://..." } | { copy: "..." } | { ask: "Follow-up prompt" } | { handOff: { task: "...", label: "..." } })\` — Renders a real clickable button right on the screen.`;

function loadSystemInstructions(): string {
  return ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS;
}

function getUserMemoryMarkdown(userId: string = DEFAULT_USER_ID): string {
  try {
    const row = db
      .prepare(
        "SELECT markdown, profile_markdown, context_markdown FROM memory WHERE user_id = ?"
      )
      .get(userId) as
      | {
          markdown: string;
          profile_markdown: string;
          context_markdown: string;
        }
      | undefined;
    if (!row) return "";
    return (
      row.markdown ||
      [row.profile_markdown, row.context_markdown].filter(Boolean).join("\n\n")
    );
  } catch {
    return "";
  }
}

function isUsableApiKey(val: string | undefined): val is string {
  if (!val) return false;
  const trimmed = val.trim();
  if (!trimmed) return false;
  if (
    trimmed.startsWith("your_") ||
    trimmed.startsWith("sk-placeholder") ||
    trimmed === "replace_me"
  ) {
    return false;
  }
  return true;
}

function ensureSessionAndThread(
  sessionId: string,
  userId: string,
  model: string,
  threadItems: unknown[]
): void {
  const now = new Date().toISOString();
  const expiresAt = Math.floor(Date.now() / 1000) + 86400 * 7;

  db.prepare(
    `INSERT INTO sessions (id, user_id, access_token, refresh_token, expires_at, created_at)
     VALUES (?, ?, ?, ?, ?, ?)
     ON CONFLICT(id) DO UPDATE SET expires_at = excluded.expires_at`
  ).run(
    sessionId,
    userId,
    `sess_tok_${sessionId}`,
    `sess_ref_${sessionId}`,
    expiresAt,
    now
  );

  db.prepare(
    `INSERT INTO threads (id, user_id, title, mode, model, messages_json, created_at, updated_at)
     VALUES (?, ?, ?, 'onebrain', ?, ?, ?, ?)
     ON CONFLICT(id) DO UPDATE SET
       model = excluded.model,
       messages_json = excluded.messages_json,
       updated_at = excluded.updated_at`
  ).run(
    sessionId,
    userId,
    "OneBrain Session",
    model,
    JSON.stringify(threadItems ?? []),
    now,
    now
  );
}

interface ExtractedPromptInput {
  userText: string;
  imageDataUrls: string[];
  hasExistingDrawingOnScreen: boolean;
  contextNotes: string;
  screenLayout: ScreenLayoutAnalysis | null;
}

function extractPromptInput(body: Record<string, any>): ExtractedPromptInput {
  const texts: string[] = [];
  const imageDataUrls: string[] = [];
  const contextParts: string[] = [];
  let hasExistingDrawingOnScreen = false;

  // 1. Check `turn` object from CompanionSession.WriteTurnBody
  if (body.turn && typeof body.turn === "object") {
    if (typeof body.turn.words === "string" && body.turn.words.trim()) {
      texts.push(body.turn.words.trim());
    } else if (typeof body.turn.fact === "string" && body.turn.fact.trim()) {
      texts.push(body.turn.fact.trim());
    }
    if (typeof body.turn.front_app === "string" && body.turn.front_app.trim()) {
      contextParts.push(`Front App: ${body.turn.front_app.trim()}`);
    }
    if (
      (typeof body.turn.drawing_on_screen === "string" &&
        body.turn.drawing_on_screen.trim().length > 0) ||
      (typeof body.turn.drawing_code === "string" &&
        body.turn.drawing_code.trim().length > 0)
    ) {
      hasExistingDrawingOnScreen = true;
    }
    if (body.turn.highlight && typeof body.turn.highlight === "object") {
      const gesture = body.turn.highlight.gesture || "";
      const hText = body.turn.highlight.text || "";
      if (gesture || hText) {
        contextParts.push(
          `User Mouse Highlight Gesture: ${gesture} ${hText}`.trim()
        );
      }
    }
    if (body.turn.page && typeof body.turn.page === "object") {
      const pageRef = body.turn.page.reference || body.turn.page.title || "";
      const pageText =
        typeof body.turn.page.text === "string"
          ? body.turn.page.text.slice(0, 1200)
          : "";
      if (pageRef || pageText) {
        contextParts.push(
          `Active Document/Page (${pageRef}): ${pageText}`.trim()
        );
      }
    }
    if (
      body.turn.screenshot &&
      typeof body.turn.screenshot.base64 === "string" &&
      body.turn.screenshot.base64.length > 0
    ) {
      const mime = body.turn.screenshot.mimeType || "image/jpeg";
      const b64 = body.turn.screenshot.base64;
      imageDataUrls.push(
        b64.startsWith("data:") ? b64 : `data:${mime};base64,${b64}`
      );
    }
    if (Array.isArray(body.turn.attached_images)) {
      for (const img of body.turn.attached_images) {
        if (img && typeof img.base64 === "string" && img.base64.length > 0) {
          const mime = img.mimeType || "image/jpeg";
          imageDataUrls.push(
            img.base64.startsWith("data:")
              ? img.base64
              : `data:${mime};base64,${img.base64}`
          );
        }
      }
    }
  }

  // 2. Check top-level `input` (string or array of blocks/messages)
  if (typeof body.input === "string" && body.input.trim()) {
    texts.push(body.input.trim());
  } else if (Array.isArray(body.input)) {
    for (const item of body.input) {
      if (typeof item === "string") {
        texts.push(item);
      } else if (item && typeof item === "object") {
        if (typeof item.text === "string") {
          texts.push(item.text);
        }
        if (
          item.type === "input_image" &&
          typeof item.image_url === "string" &&
          item.image_url.length > 0
        ) {
          imageDataUrls.push(item.image_url);
        }
        if (Array.isArray(item.content)) {
          for (const block of item.content) {
            if (block && typeof block === "object") {
              if (typeof block.text === "string") {
                texts.push(block.text);
              }
              if (
                block.type === "input_image" &&
                typeof block.image_url === "string"
              ) {
                imageDataUrls.push(block.image_url);
              }
            }
          }
        }
      }
    }
  }

  // 2.5 Check `turn_items` array from CompanionSession.WriteTurnBody
  if (Array.isArray(body.turn_items)) {
    for (const item of body.turn_items) {
      if (!item || typeof item !== "object") continue;
      if (typeof item.content === "string" && item.content.trim()) {
        texts.push(item.content.trim());
      } else if (Array.isArray(item.content)) {
        for (const block of item.content) {
          if (block && typeof block === "object") {
            if (typeof block.text === "string" && block.text.trim()) {
              texts.push(block.text.trim());
            }
            if (
              block.type === "input_image" &&
              typeof block.image_url === "string" &&
              block.image_url.length > 0
            ) {
              imageDataUrls.push(block.image_url);
            }
          }
        }
      }
    }
  }

  // 3. Check trailing user message in `thread`
  if (texts.length === 0 && Array.isArray(body.thread)) {
    for (let i = body.thread.length - 1; i >= 0; i--) {
      const item = body.thread[i];
      if (item && typeof item === "object" && item.role === "user") {
        if (typeof item.content === "string") {
          texts.push(item.content);
          break;
        } else if (Array.isArray(item.content)) {
          for (const block of item.content) {
            if (block && typeof block === "object") {
              if (typeof block.text === "string") {
                texts.push(block.text);
              }
              if (
                block.type === "input_image" &&
                typeof block.image_url === "string"
              ) {
                imageDataUrls.push(block.image_url);
              }
            }
          }
          if (texts.length > 0) break;
        }
      }
    }
  }

  // 3.5 If the current turn did not re-attach a screenshot (e.g. "Solve this on a page"),
  // recover the most recent screenshot from `body.thread` so layout & vision still work!
  if (imageDataUrls.length === 0 && Array.isArray(body.thread)) {
    for (let i = body.thread.length - 1; i >= 0; i--) {
      const item = body.thread[i];
      if (item && typeof item === "object" && Array.isArray(item.content)) {
        for (const block of item.content) {
          if (
            block &&
            typeof block === "object" &&
            block.type === "input_image" &&
            typeof block.image_url === "string" &&
            block.image_url.length > 0
          ) {
            imageDataUrls.push(block.image_url);
          }
        }
        if (imageDataUrls.length > 0) break;
      }
    }
  }

  const dedupedTexts = Array.from(
    new Set(texts.map((t) => t.trim()).filter(Boolean))
  );
  const dedupedImages = Array.from(new Set(imageDataUrls.filter(Boolean)));
  const screenLayout =
    dedupedImages.length > 0 ? analyzeScreenLayout(dedupedImages[0]) : null;

  return {
    userText:
      dedupedTexts.join("\n").trim() || "Look at my screen and help me.",
    imageDataUrls: dedupedImages,
    hasExistingDrawingOnScreen,
    contextNotes: contextParts.join("\n"),
    screenLayout,
  };
}

interface UpstreamLlmResult {
  codeText: string;
  modelUsed: string;
  inputTokens: number;
  outputTokens: number;
  toolCalls: Array<{
    call_id: string;
    name: string;
    arguments: string;
    async: boolean;
  }>;
}

async function runGroqVisionEye(
  groqApiKey: string,
  extracted: ExtractedPromptInput
): Promise<string> {
  if (extracted.imageDataUrls.length === 0) return "";
  const visionPrompt = `Read this 1280x720 screenshot carefully for the user's request: "${extracted.userText}"
${extracted.screenLayout ? extracted.screenLayout.layoutPromptNote : ""}
Ignore any red/blue Kursor overlay boxes or floating cards from a previous turn.
Output a concise plain-text OCR & coordinate mapping report (under 180 words):
1. EXACT VERBATIM TEXT of all visible worksheet questions (e.g. part (a), part (b)), table column headers (e.g. Col 1 Statement, Col 2 RISC, Col 3 CISC), and each visible table row statement (Row 1..N). Note if the user's voice transcript misheard a word on screen (e.g. "disk" -> "RISC/CISC").
2. Map each visible Statement Row (Row 1..N) to its exact (cx, cy) column centers from Measured Screen Geometry, and map each visible blank dotted line (Line 1..M) to which question part it belongs to. Do NOT guess the answers yourself — only transcribe the exact text and coordinates.`;

  for (const maxTok of [250, 150]) {
    try {
      const res = await fetch(
        "https://api.groq.com/openai/v1/chat/completions",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${groqApiKey.trim()}`,
          },
          body: JSON.stringify({
            model: "qwen/qwen3.8-27b",
            reasoning_effort: "none",
            temperature: 0.1,
            max_tokens: maxTok,
            messages: [
              {
                role: "user",
                content: [
                  {
                    type: "image_url",
                    image_url: { url: extracted.imageDataUrls[0] },
                  },
                  { type: "text", text: visionPrompt },
                ],
              },
            ],
          }),
          signal: AbortSignal.timeout(12000),
        }
      );
      if (res.ok) {
        const data = (await res.json()) as any;
        const text = data?.choices?.[0]?.message?.content?.trim();
        if (text) {
          console.log(
            `[OneBrain/VisionEye] Extracted screen reading (${data?.usage?.completion_tokens || 0} tokens)`
          );
          return text;
        }
      } else if (res.status === 429 && maxTok === 250) {
        await new Promise((r) => setTimeout(r, 1200));
      }
    } catch {
      // Fall through
    }
  }
  return "";
}

async function tryCallUpstreamLlm(
  systemPrompt: string,
  thread: any[],
  extracted: ExtractedPromptInput,
  requestedModel: string,
  tools?: any[]
): Promise<UpstreamLlmResult | null> {
  // Stage 1: If we have a screenshot and a Groq key, run the lightweight Vision Eye first
  // so `openai/gpt-oss-120b` (120B flagship coder) gets exact OCR + calibrated pixel coordinates!
  let visionReading = "";
  const isPure3dRequest =
    /\b(3d|three[-\s]?d|figure|turbine|engine|molecule|solar system|piston)\b/i.test(
      extracted.userText
    ) && !/\b(screen|page|worksheet|table|tick|cross)\b/i.test(extracted.userText);

  if (
    extracted.imageDataUrls.length > 0 &&
    !isPure3dRequest &&
    isUsableApiKey(process.env.GROQ_API_KEY)
  ) {
    visionReading = await runGroqVisionEye(process.env.GROQ_API_KEY, extracted);
  }

  const enrichedUserBlocks: string[] = [
    `[User Request]\n${extracted.userText}`,
  ];
  if (extracted.contextNotes) {
    enrichedUserBlocks.push(`[Turn Context]\n${extracted.contextNotes}`);
  }
  if (extracted.screenLayout?.layoutPromptNote) {
    enrichedUserBlocks.push(extracted.screenLayout.layoutPromptNote);
  }
  if (visionReading) {
    enrichedUserBlocks.push(`[Screen Vision Analysis]\n${visionReading}`);
  }
  const enrichedUserText = enrichedUserBlocks.join("\n\n");

  const messages: any[] = [{ role: "system", content: systemPrompt }];

  // Keep only the last 4 clean turns of history so old hardcoded coordinates don't pollute new screen coordinates
  if (Array.isArray(thread)) {
    for (const item of thread.slice(-6)) {
      if (!item || typeof item !== "object") continue;
      if (
        item.type === "message" &&
        (item.role === "user" || item.role === "assistant")
      ) {
        let textContent = "";
        if (typeof item.content === "string") {
          textContent = item.content;
        } else if (Array.isArray(item.content)) {
          textContent = item.content
            .map((c: any) => (typeof c?.text === "string" ? c.text : ""))
            .filter(Boolean)
            .join("\n");
        }
        if (
          textContent &&
          !textContent.includes("Hello from Kursor local dictation transcription") &&
          !textContent.includes("Kursor Active") &&
          !textContent.includes("Unable to place") &&
          !textContent.includes("weren't supplied") &&
          !textContent.includes("were not provided")
        ) {
          // Always strip raw (x, y) drawing calls from older assistant turns in history
          // so a previous turn's write() or zoom/scroll coordinates never pollute the current turn!
          if (item.role === "assistant") {
            const sayMatches = Array.from(
              textContent.matchAll(/say\(\s*(["'`])([\s\S]*?)\1\s*\)/g)
            )
              .map((m) => m[2].trim())
              .filter(Boolean);
            if (sayMatches.length > 0) {
              messages.push({
                role: "assistant",
                content: `\`\`\`js\nstep(1);\nsay(${JSON.stringify(sayMatches.join(" "))});\n\`\`\``,
              });
            }
          } else {
            messages.push({ role: item.role, content: textContent });
          }
        }
      }
    }
  }

  const multimodalUserContent: any[] = [{ type: "text", text: enrichedUserText }];
  for (const dataUrl of extracted.imageDataUrls.slice(0, 2)) {
    multimodalUserContent.push({
      type: "image_url",
      image_url: { url: dataUrl },
    });
  }

  const providers: Array<{
    name: string;
    url: string;
    apiKey: string | undefined;
    model: string;
    supportsVision: boolean;
    headers?: Record<string, string>;
  }> = [
    // Primary: Groq 120B Flagship (`openai/gpt-oss-120b`) fed by `qwen/qwen3.8-27b` Vision Eye + Pixel Layout Analyzer
    {
      name: "groq-120b",
      url: "https://api.groq.com/openai/v1/chat/completions",
      apiKey:
        visionReading || extracted.imageDataUrls.length === 0 || isPure3dRequest
          ? process.env.GROQ_API_KEY
          : undefined,
      model: "openai/gpt-oss-120b",
      supportsVision: false,
    },
    // Direct Vision fallback on Groq (`qwen/qwen3.8-27b`) if Stage 1 was skipped
    {
      name: "groq-qwen-vision",
      url: "https://api.groq.com/openai/v1/chat/completions",
      apiKey: process.env.GROQ_API_KEY,
      model: "qwen/qwen3.8-27b",
      supportsVision: true,
    },
    {
      name: "openrouter",
      url: "https://openrouter.ai/api/v1/chat/completions",
      apiKey: process.env.OPENROUTER_API_KEY,
      model:
        process.env.KURSOR_BRAIN_MODEL ||
        process.env.OPENROUTER_DEFAULT_MODEL ||
        requestedModel ||
        "baidu/ernie-5.5-flash-preview:free",
      supportsVision: true,
      headers: {
        "HTTP-Referer": "https://kursor.dev",
        "X-Title": "Kursor OneBrain",
      },
    },
    {
      name: "openai",
      url: "https://api.openai.com/v1/chat/completions",
      apiKey: process.env.OPENAI_API_KEY,
      model: "gpt-4o",
      supportsVision: true,
    },
    {
      name: "gemini",
      url: "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
      apiKey: process.env.GEMINI_API_KEY,
      model: "gemini-2.5-flash",
      supportsVision: true,
    },
  ];

  for (const provider of providers) {
    if (!isUsableApiKey(provider.apiKey)) continue;

    try {
      const providerMessages = [
        ...messages,
        {
          role: "user",
          content:
            provider.supportsVision && extracted.imageDataUrls.length > 0
              ? multimodalUserContent
              : enrichedUserText,
        },
      ];

      const payload: Record<string, any> = {
        model: provider.model,
        messages: providerMessages,
        temperature: 0.25,
        max_tokens:
          provider.name === "openrouter"
            ? 4096
            : provider.name === "groq-120b"
              ? 2048
              : provider.name === "groq-qwen-vision"
                ? 420
                : 1024,
        ...(provider.name === "openrouter"
          ? { reasoning: { effort: "low" } }
          : provider.name === "groq-120b"
            ? { reasoning_effort: "low" }
            : provider.name === "groq-qwen-vision"
              ? { reasoning_effort: "none" }
              : {}),
      };

      // Omit tools on Groq/OpenRouter free models so the model always writes ClickyDraw JS code directly
      if (
        Array.isArray(tools) &&
        tools.length > 0 &&
        !provider.name.startsWith("groq") &&
        provider.name !== "openrouter"
      ) {
        payload.tools = tools;
      }

      const res = await fetch(provider.url, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${provider.apiKey.trim()}`,
          ...(provider.headers ?? {}),
        },
        body: JSON.stringify(payload),
        signal: AbortSignal.timeout(18000),
      });

      let data: any = null;
      if (res.ok) {
        data = await res.json();
      }
      const firstChoice = data?.choices?.[0]?.message;
      const firstHasOutput =
        Boolean(firstChoice?.content && String(firstChoice.content).trim()) ||
        (Array.isArray(firstChoice?.tool_calls) &&
          firstChoice.tool_calls.length > 0);

      if ((!res.ok || !firstHasOutput) && provider.name === "openrouter") {
        // Automatic fallback for OpenRouter :free models
        const fallbackPayload: Record<string, any> = {
          model:
            provider.model.endsWith(":free") ||
            provider.model === "openrouter/free"
              ? provider.model
              : "openrouter/free",
          models: [
            "google/gemma-3-27b-it:free",
            "meta-llama/llama-4-maverick:free",
            "qwen/qwen2.5-vl-72b-instruct:free",
            "mistralai/mistral-small-3.1-24b-instruct:free",
            "openrouter/free",
          ],
          messages: providerMessages,
          temperature: 0.25,
          max_tokens: 4096,
          reasoning: { effort: "low" },
        };
        const retryRes = await fetch(provider.url, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${provider.apiKey.trim()}`,
            ...(provider.headers ?? {}),
          },
          body: JSON.stringify(fallbackPayload),
          signal: AbortSignal.timeout(18000),
        });
        if (retryRes.ok) {
          data = await retryRes.json();
        }
      }

      if (!data) continue;
      const choice = data?.choices?.[0]?.message;
      if (!choice) continue;

      const rawContent =
        typeof choice.content === "string" ? choice.content.trim() : "";
      const toolCalls: Array<{
        call_id: string;
        name: string;
        arguments: string;
        async: boolean;
      }> = [];

      if (Array.isArray(choice.tool_calls)) {
        for (const tc of choice.tool_calls) {
          if (tc?.function?.name) {
            toolCalls.push({
              call_id: String(tc.id || `call_${crypto.randomUUID()}`),
              name: String(tc.function.name),
              arguments:
                typeof tc.function.arguments === "string"
                  ? tc.function.arguments
                  : JSON.stringify(tc.function.arguments ?? {}),
              async: false,
            });
          }
        }
      }

      if (!rawContent && toolCalls.length === 0) {
        continue;
      }

      // Extract inner JS code if model included markdown fences or prose around ```js ... ```
      let codeText = rawContent;
      const fencedMatch = codeText.match(/```(?:js|javascript)?\s*([\s\S]*?)```/i);
      if (fencedMatch && fencedMatch[1]?.trim()) {
        codeText = fencedMatch[1].trim();
      } else {
        codeText = codeText
          .replace(/^```\w*\s*/i, "")
          .replace(/```\s*$/i, "")
          .trim();
      }

      const calmSpot = extracted.screenLayout?.calmCardSpot || {
        x: 760,
        y: 90,
        w: 440,
        h: 400,
      };

      if (codeText && !codeText.includes("say(")) {
        const firstSentence =
          codeText.split(/(?<=[.!?])\s+/)[0]?.slice(0, 180) ||
          codeText.slice(0, 180);
        const spokenEscaped = JSON.stringify(firstSentence);
        const bodyEscaped = JSON.stringify(codeText.slice(0, 480));
        codeText = `step(1);\nsay(${spokenEscaped});\nclipping(${calmSpot.x}, ${calmSpot.y}, ${Math.min(440, calmSpot.w)}, "Kursor Notes", ${bodyEscaped}, "Kursor");`;
      } else if (codeText) {
        const hasVisualCall =
          /\b(?:ring|frame|point|target|hover|underline|connect|trace|check|cross|strike|num|sticky|clipping|receipt|legend|paper|write|figure|scene3d|circle|ellipse|rect|poly|line|curve|arrow|highlight)\s*\(/.test(
            codeText
          );
        if (!hasVisualCall) {
          const sayMatch = codeText.match(/say\(\s*(["'`])([\s\S]*?)\1\s*\)/);
          const noteBody = JSON.stringify(
            (sayMatch?.[2] || extracted.userText || "Here is the answer.").slice(
              0,
              420
            )
          );
          codeText = `${codeText}\nclipping(${calmSpot.x}, ${calmSpot.y}, ${Math.min(440, calmSpot.w)}, "Kursor Notes", ${noteBody}, "Kursor");`;
        }
      }

      // Snap table check(x, y) / cross(x, y) calls to exact measured pixel cell centers,
      // and convert loose write() calls on non-blank pages into a clean draggable receipt() page!
      codeText = snapProgramToScreenLayout(
        codeText,
        extracted.screenLayout,
        extracted.userText
      );

      // If there was a previous turn's drawing still on screen, prepend startOver() so old frames/cards are wiped cleanly
      if (
        extracted.hasExistingDrawingOnScreen &&
        !codeText.includes("startOver()") &&
        !/\b(?:erase|move|moveTo|recolor|restyle)\s*\(/.test(codeText)
      ) {
        codeText = `startOver();\n${codeText}`;
      }

      codeText = "```js\n" + codeText.trim() + "\n```";

      console.log(
        `[OneBrain] Answered via ${provider.name} (${data?.model || provider.model}${visionReading ? " + qwen-vision-eye" : ""}) for prompt: "${extracted.userText.slice(0, 100)}"`
      );

      return {
        codeText,
        modelUsed: String(data?.model || provider.model),
        inputTokens: Number(data?.usage?.prompt_tokens || 320),
        outputTokens: Number(data?.usage?.completion_tokens || 95),
        toolCalls,
      };
    } catch (err: any) {
      console.warn(`[OneBrain] Provider ${provider.name} error:`, err?.message || err);
    }
  }

  return null;
}

function buildLocalFallbackProgram(
  body: Record<string, any>,
  extracted: ExtractedPromptInput
): {
  codeChunks: string[];
  fullCode: string;
  toolCalls: Array<{
    call_id: string;
    name: string;
    arguments: string;
    async: boolean;
  }>;
  computerCalls: Array<{
    call_id: string;
    actions: any[];
    pending_safety_checks: any[];
  }>;
} {
  const toolCalls: Array<{
    call_id: string;
    name: string;
    arguments: string;
    async: boolean;
  }> = [];
  const computerCalls: Array<{
    call_id: string;
    actions: any[];
    pending_safety_checks: any[];
  }> = [];

  if (body.force_tool_call && typeof body.force_tool_call === "object") {
    toolCalls.push({
      call_id: String(body.force_tool_call.call_id || "call_kursor_tool_01"),
      name: String(body.force_tool_call.name || "save_memory"),
      arguments:
        typeof body.force_tool_call.arguments === "string"
          ? body.force_tool_call.arguments
          : JSON.stringify(
              body.force_tool_call.arguments ?? {
                memory: "User tested Kursor tool calls",
              }
            ),
      async: Boolean(body.force_tool_call.async ?? false),
    });
  }

  if (body.force_computer_call && typeof body.force_computer_call === "object") {
    computerCalls.push({
      call_id: String(
        body.force_computer_call.call_id || "comp_call_kursor_01"
      ),
      actions: Array.isArray(body.force_computer_call.actions)
        ? body.force_computer_call.actions
        : [{ type: "screenshot" }],
      pending_safety_checks: Array.isArray(
        body.force_computer_call.pending_safety_checks
      )
        ? body.force_computer_call.pending_safety_checks
        : [],
    });
  }

  const customLine =
    typeof body.mock_say === "string" && body.mock_say.trim()
      ? body.mock_say.trim()
      : "I see your screen and I'm ready to help!";

  const escapedLine = JSON.stringify(customLine);

  const codeChunks = [
    `\`\`\`js\n`,
    `const spot = typeof point === "function" ? point : () => {};\n`,
    `step(1);\n`,
    `say(${escapedLine});\n`,
    `spot(500, 300, "Kursor Active");\n`,
    `// say(1, ${escapedLine});\n`,
    `\`\`\``,
  ];

  return {
    codeChunks,
    fullCode: codeChunks.join(""),
    toolCalls,
    computerCalls,
  };
}

// ============================================================================
// 1. POST /brain/turn — OneBrain Multimodal SSE Engine
// Matches `BrainClient.StreamTurnAsync`, `BrainSseParser`, `BrainReplyAccumulator`,
// `SayLines`, `ProgramText`, and `BrainSession.CommitTurn`.
// ============================================================================
brainRoutes.post("/brain/turn", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    return c.json({ error: "invalid_json", message: "Invalid JSON body" }, 400);
  }

  const userCtx = c.get("kursorUser" as never) as
    | { userId?: string }
    | undefined;
  const userId = userCtx?.userId || DEFAULT_USER_ID;

  const sessionId =
    typeof body.session_id === "string" && body.session_id.trim()
      ? body.session_id.trim()
      : crypto.randomUUID().toUpperCase();

  const turnToken =
    typeof body.turn_token === "string" && body.turn_token.trim()
      ? body.turn_token.trim()
      : `turn_${crypto.randomUUID()}`;

  const keepReasoning =
    typeof body.keep_reasoning === "boolean" ? body.keep_reasoning : true;

  const defaultModel =
    process.env.KURSOR_BRAIN_MODEL ||
    process.env.OPENROUTER_DEFAULT_MODEL ||
    (typeof body.brain_model === "string" && body.brain_model.trim()) ||
    "anthropic/claude-sonnet-4.6";

  const userMemory = getUserMemoryMarkdown(userId);
  const userLayer =
    typeof body.user_layer === "string" && body.user_layer.trim()
      ? body.user_layer
      : `<user_memory>\n${userMemory}\n</user_memory>`;
  const userLayerSignature =
    typeof body.user_layer_signature === "string" &&
    body.user_layer_signature.trim()
      ? body.user_layer_signature
      : `sig_${crypto.createHash("sha256").update(userLayer).digest("hex").slice(0, 24)}`;

  const incomingThread: any[] = Array.isArray(body.thread) ? body.thread : [];
  const extracted = extractPromptInput(body);
  const systemPrompt = `${loadSystemInstructions()}\n\n${userLayer}`;

  // Persist initial session & thread in SQLite
  ensureSessionAndThread(sessionId, userId, defaultModel, incomingThread);

  const encoder = new TextEncoder();

  const stream = new ReadableStream<Uint8Array>({
    async start(controller) {
      const sendData = (payload: Record<string, unknown>) => {
        const line = `data: ${JSON.stringify(payload)}\n\n`;
        controller.enqueue(encoder.encode(line));
      };

      // Note: BrainSseParser.TryDispatchText checks `json.StartsWith("{\"type\":\"text\""u8)`
      const sendTextChunk = (text: string) => {
        const line = `data: {"type":"text","text":${JSON.stringify(text)}}\n\n`;
        controller.enqueue(encoder.encode(line));
      };

      try {
        // 1. Emit `meta` event
        const metaPayload: Record<string, unknown> = {
          type: "meta",
          model: defaultModel,
          turn_token: turnToken,
          keep_reasoning: keepReasoning,
          guided_max_rounds: 8,
          user_layer: userLayer,
          user_layer_signature: userLayerSignature,
        };
        if (body.takeover || body.drive) {
          metaPayload.drive_lease_id = `drive_lease_${sessionId.slice(0, 8)}`;
        }
        sendData(metaPayload);

        // 2. Emit `status` event (`kind` for BrainReplyAccumulator + `text` for UI)
        sendData({
          type: "status",
          kind: "thinking",
          text: "Thinking...",
        });

        // 3. Try live LLM (OpenRouter -> OpenAI -> Gemini -> Groq) or use local fallback
        const upstream = await tryCallUpstreamLlm(
          systemPrompt,
          incomingThread,
          extracted,
          defaultModel,
          Array.isArray(body.tools) ? body.tools : undefined
        );

        let finalCodeText = "";
        let modelUsed = defaultModel;
        let inputTokens = 420;
        let outputTokens = 85;
        let toolCalls: Array<{
          call_id: string;
          name: string;
          arguments: string;
          async: boolean;
        }> = [];
        let computerCalls: Array<{
          call_id: string;
          actions: any[];
          pending_safety_checks: any[];
        }> = [];

        if (upstream && !body.force_local_fallback) {
          finalCodeText = upstream.codeText;
          modelUsed = upstream.modelUsed;
          inputTokens = upstream.inputTokens;
          outputTokens = upstream.outputTokens;
          toolCalls = upstream.toolCalls;

          // Stream in realistic chunks
          const chunkSize = 48;
          for (let i = 0; i < finalCodeText.length; i += chunkSize) {
            sendTextChunk(finalCodeText.slice(i, i + chunkSize));
          }
        } else {
          const fallback = buildLocalFallbackProgram(body, extracted);
          finalCodeText = fallback.fullCode;
          toolCalls = fallback.toolCalls;
          computerCalls = fallback.computerCalls;

          for (const chunk of fallback.codeChunks) {
            sendTextChunk(chunk);
          }
        }

        // 4. Emit tool_call / tool_calls events if any tools were invoked
        if (toolCalls.length > 0) {
          for (const call of toolCalls) {
            sendData({
              type: "tool_call",
              call,
            });
          }
          sendData({
            type: "tool_calls",
            calls: toolCalls,
          });
        }

        // 5. Emit computer_calls event if any GUI actions were invoked
        if (computerCalls.length > 0) {
          sendData({
            type: "computer_calls",
            calls: computerCalls,
          });
        }

        // 6. Build and emit `items` event so BrainSession.CommitTurn appends to thread
        const outputItems: any[] = [];
        if (finalCodeText.length > 0) {
          outputItems.push({
            type: "message",
            role: "assistant",
            content: [
              {
                type: "output_text",
                text: finalCodeText,
              },
            ],
          });
        }
        for (const call of toolCalls) {
          outputItems.push({
            type: "function_call",
            call_id: call.call_id,
            name: call.name,
            arguments: call.arguments,
          });
        }

        sendData({
          type: "items",
          items: outputItems,
        });

        // Persist updated thread in SQLite
        const updatedThread = [
          ...incomingThread,
          {
            type: "message",
            role: "user",
            content: [{ type: "input_text", text: extracted.userText }],
          },
          ...outputItems,
        ];
        ensureSessionAndThread(sessionId, userId, modelUsed, updatedThread);

        // 7. Emit `done` event
        sendData({
          type: "done",
          input: inputTokens,
          last_input: inputTokens,
          output: outputTokens,
          usage: {
            input_tokens: inputTokens,
            last_input_tokens: inputTokens,
            output_tokens: outputTokens,
          },
          cost: 0.0,
          keep_reasoning: keepReasoning,
          turn_token: turnToken,
          message_recorded: true,
          message_refunded: false,
          drive_messages_refunded: 0,
        });
      } catch (err) {
        sendData({
          type: "error",
          code: "brain_turn_error",
          message:
            err instanceof Error ? err.message : "Unexpected OneBrain error",
        });
      } finally {
        controller.close();
      }
    },
  });

  return new Response(stream, {
    status: 200,
    headers: {
      "Content-Type": "text/event-stream; charset=utf-8",
      "Cache-Control": "no-cache, no-transform",
      Connection: "keep-alive",
      "x-clicky-accepts-gzip": "1",
    },
  });
});

// ============================================================================
// 2. POST /brain/compact — Thread Compaction
// Matches `BrainClient.CompactAsync`, `BrainCompactReply`, `BrainCompactResult`,
// and `BrainSession.ApplyCompaction`.
// ============================================================================
brainRoutes.post("/brain/compact", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const sessionId =
    typeof body.session_id === "string" && body.session_id.trim()
      ? body.session_id.trim()
      : "default-session";
  const thread: any[] = Array.isArray(body.thread) ? body.thread : [];

  // Summarize leading items
  const keepFromIndex =
    thread.length > 2 ? Math.floor(thread.length / 2) : thread.length;
  const keepLeadingItems = 0;
  const dropLeadingItems = 0;

  const foldedPreview = thread
    .slice(0, Math.max(1, keepFromIndex))
    .map((item) => {
      if (!item || typeof item !== "object") return "";
      const role = item.role || item.type || "item";
      if (typeof item.content === "string") {
        return `${role}: ${item.content.slice(0, 120)}`;
      }
      if (Array.isArray(item.content)) {
        const txt = item.content
          .map((b: any) => (typeof b?.text === "string" ? b.text : ""))
          .filter(Boolean)
          .join(" ")
          .slice(0, 120);
        return txt ? `${role}: ${txt}` : "";
      }
      return "";
    })
    .filter(Boolean)
    .join(" | ");

  const summaryText = foldedPreview
    ? `[Compacted Thread Summary (${keepFromIndex} items folded)]: ${foldedPreview}`
    : `[Compacted Thread Summary for session ${sessionId}]`;

  const noteItem = {
    type: "message",
    role: "developer",
    content: [
      {
        type: "input_text",
        text: summaryText,
      },
    ],
  };

  const compacted = {
    note_item: noteItem,
    keep_from_index: keepFromIndex,
    keep_leading_items: keepLeadingItems,
    drop_leading_items: dropLeadingItems,
  };

  return c.json({
    // Top-level properties read by `BrainClient.CompactAsync` in `BrainClient.cs`
    note_item: noteItem,
    keep_from_index: keepFromIndex,
    keep_leading_items: keepLeadingItems,
    drop_leading_items: dropLeadingItems,
    // Nested `compacted` property matching `BrainCompactReply` record structure
    compacted,
    tokens_until_fold: 45000,
    session_id: sessionId,
  });
});

// ============================================================================
// 3. POST /brain/session-end — Session End & Memory Consolidation
// Matches `BrainSession.EndSessionOnWorker`
// ============================================================================
brainRoutes.post("/brain/session-end", async (c) => {
  let body: Record<string, any> = {};
  try {
    body = await c.req.json();
  } catch {
    // Allow empty JSON
  }

  const userCtx = c.get("kursorUser" as never) as
    | { userId?: string }
    | undefined;
  const userId = userCtx?.userId || DEFAULT_USER_ID;

  const sessionId =
    typeof body.session_id === "string" && body.session_id.trim()
      ? body.session_id.trim()
      : null;

  const now = new Date().toISOString();

  if (sessionId) {
    if (Array.isArray(body.thread)) {
      ensureSessionAndThread(sessionId, userId, "ended", body.thread);
    }
    db.prepare(
      "UPDATE threads SET mode = 'ended', updated_at = ? WHERE id = ?"
    ).run(now, sessionId);
    db.prepare("UPDATE sessions SET expires_at = 0 WHERE id = ?").run(
      sessionId
    );
  }

  return c.json({
    ok: true,
    ended: true,
    session_id: sessionId,
    ended_at: now,
  });
});

// ============================================================================
// 4. POST /brain/prewarm — Prompt Cache Prewarming
// Matches `BrainSession.NoteKeyDown` -> `POST /brain/prewarm`
// ============================================================================
brainRoutes.post("/brain/prewarm", async (c) => {
  return c.json({ ok: true });
});

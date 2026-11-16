import { Hono } from "hono";
import fs from "node:fs";
import crypto from "node:crypto";
import { db, DEFAULT_USER_ID } from "../db/index.js";
import {
  analyzeScreenLayout,
  analyzeScreenLayoutWithOcr,
  annotateScreenshotWithRuler,
  enrichScreenLayoutWithOcr,
  getCachedWindowsOcr,
  prewarmWinOcrDaemon,
  runWindowsOcr,
  snapProgramToScreenLayout,
  type ScreenLayoutAnalysis,
} from "../utils/screenLayout.js";

if (!process.argv.some((a) => /test-phase/i.test(a))) {
  prewarmWinOcrDaemon();
}

export const brainRoutes = new Hono();

export const OPENROUTER_VISION_MODELS_TIER1 = [
  "google/gemini-2.5-flash-lite",
  "google/gemini-2.5-flash",
  "openai/gpt-4o-mini",
] as const;

export const OPENROUTER_VISION_MODELS_TIER2 = [
  "nvidia/nemotron-3.5-super-vl:free",
  "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free",
  "google/gemma-4-31b-it:free",
] as const;

let cachedInvalidOpenRouterKey = "";

export const COMPACT_GROQ_VISION_SYSTEM_PROMPT = `You are Clicky (Kursor OneBrain), an interactive AI companion that speaks AND draws live 2D ink marks, worked solutions, and 3D models on a 1280x720 screen.
CRITICAL OUTPUT RULES:
- Respond with ONLY a single \`\`\`js ... \`\`\` program for ClickyDraw v1.6 (no prose outside fences, NO // comments).
- Structure every program into 2 to 3 beats (\`step(1);\`, \`step(2);\`, optional \`step(3);\`). Start every beat with \`step(k); say("...");\` (10-20 words) followed by drawing calls.
- Coordinates & Local Screen OCR (\`[SCREEN OCR TEXT & HEADINGS]\`): Canvas is W=1280, H=720. Read exact \`(x, y, w, h, bottomY)\` of every question heading and text line from \`[SCREEN OCR TEXT & HEADINGS (1280x720 Canvas)]\`, \`[Measured Screen Geometry]\`, and \`[Screen Vision Analysis]\`. To underline a heading line \`[x, y, w, h, bottomY]\`, call \`underline(x, bottomY + 2, w)\` — NEVER underline the browser toolbar (\`y < 165\`).
- Surgical marks: \`underline(x, y, w)\`, \`point(x, y, "Label")\`, \`ring(x, y, w, h, "Label")\`. Never stack overlapping \`frame()\` boxes.
- Worksheet Answer Slots (\`[FILLABLE ANSWER SLOTS]\`):
  - Whenever \`[FILLABLE ANSWER SLOTS (1280x720 Canvas)]\` (\`SLOT_1..M\`) are listed for the requested question (unless the user explicitly asks to open a separate page/whiteboard card), you MUST write the answers directly onto \`SLOT_1..M\` on the screen using \`write(writeX, writeY, "...", { maxW })\` and NEVER open a \`paper()\`, \`receipt()\`, or \`clipping()\` card!
  - Underline the question heading using its exact \`[x, bottomY, w]\` from \`[SCREEN OCR TEXT & HEADINGS]\`: \`underline(x, bottomY + 2, w)\`.
  - Emit ONE \`write(writeX, writeY, "...", { maxW })\` call per primary labeled field/item (e.g. for 4 numbered items \`Item 1..4\`, emit 4 \`write()\` calls on the primary lines \`Item 1\`, \`Item 2\`, \`Item 3\`, \`Item 4\`; for \`Description, Benefit 1..2, Drawback 1..2\`, emit one \`write()\` call per field, using a \`(line 2)\` continuation slot only if a single explanation exceeds ~75 characters). Never emit empty \`write(x, y, "")\` calls.
  - NEVER write generic placeholder text like "Answer line one concise response" or "First concise response addressing part (a)"! Every \`write()\` call must contain the actual subject-matter answer to the printed question on screen. If top unlabeled lines belong to a previous scrolled-off question whose prompt is not visible, skip them!
  - **CRITICAL SINGLE-LINE BUDGET**: Each \`SLOT_N\` is a **single horizontal line** (\`max 1 line\`, \`<= 75 characters\`). NEVER write a multi-sentence or >80-character paragraph into a single \`write()\` call!
  - **PAIRED NAME + DESCRIPTION SLOTS**: When a slot labeled \`Architecture N\` / \`Category N\` / \`Term N\` / \`Component N\` / \`Method N\` is immediately followed by a slot labeled \`Description\` / \`Explanation\`, write **ONLY the concise name/identifier** on the first slot (e.g. \`"Multiple Instruction Multiple Data (MIMD)"\`) and write **ONLY the 1-line description** (without repeating the label or name prefix) on the \`Description\` slot below it!
  - NEVER repeat the printed slot label or item number inside your string (e.g. for \`SLOT_1 [label="Item 1"]\` or \`SLOT_3 [label="Benefit 1"]\`, write \`"Enables running multiple OSes on one physical machine, improving hardware utilization."\`, NOT \`"1. Enables..."\` or \`"Benefit 1: ..."\`).
- Summary / Worked Solution Card (\`paper()\`) & Numbered Questions Without Slots:
  - When solving a numbered question that has NO on-screen \`[FILLABLE ANSWER SLOTS]\` (e.g. pure math/calculus papers, Question 8 when its blank lines are off-screen, or when the user asks to explain "in a new page" or "step by step"), read the question text and coordinates from \`[SCREEN OCR TEXT & HEADINGS]\` and \`[Screen Vision Analysis]\`, \`underline(x, bottomY + 2, w)\` its heading at its exact OCR coordinates, and open **EXACTLY ONE** \`const a = paper(x, y, w, h, { title: "...", heading: "..." });\` for the entire turn (keep \`heading\` concise, \`<= 42\` chars, and write all sub-parts \`(a)\`, \`(b)\` / \`(i)\`, \`(ii)\` inside that single \`paper()\` card \`a\` — NEVER open a second \`paper()\` card!) in the Suggested Calm Region — NEVER use floating \`write()\` on a page with 0 slots and 0 tables, and NEVER re-fill an earlier question's table when asked to solve a different numbered question! Never combine \`paper()\` with \`receipt()\`, \`clipping()\`, \`note()\`, or \`window()\`.
  - **CRITICAL \`paper()\` LINE BUDGET (MAX 10 \`text()\` CALLS)**: Inside \`paper()\`, emit **AT MOST 8 to 10** \`text(a.x, a.y + offset, "...", { size: 15 })\` calls total (spaced by \`+28\` to \`+32\`px)! NEVER put standalone prose headers like \`"Expand RHS:"\`, \`"Collect like terms:"\`, or \`"Solve the system:"\` on their own line — combine algebraic steps compactly onto single lines (e.g. \`"(a) -7x²+2x-6 = A(4+x²)+(Bx+C)(1+x) => A=-3, B=-4, C=6"\`) and ALWAYS write the final answer to every part \`(a)\`, \`(b)\` before finishing!
  - For math/calculus questions, \`[Screen Vision Analysis]\` is authoritative for integral limits and variables (Windows OCR drops \`\\int_a^b\`). In "Use the substitution x = g(t) to show that I = \\int_a^b ...", the limits \`a..b\` on the target \`x\`-integral are ALREADY transformed \`x\`-limits — NEVER substitute \`a..b\` into \`g(t)\` a second time! When combining logarithms \`-p ln x - q ln y\` into \`-ln b\`, carefully compute \`x^p * y^q\` (e.g. \`3 ln 3 + 2 ln 2 = ln(3^3 * 2^2) = ln(27 * 4) = ln 108\`, NOT \`3^2 * 2\`)!
- Solving Worksheets & Tables Directly on Screen:
  - When asked to solve/complete the table question on screen, fill the table cells directly on the worksheet using \`write(cx, cy, "val")\` or \`check(cx, cy)\` / \`cross(cx, cy)\` by combining each column's \`cx\` (\`C_1..C_12\`) with each data row's \`cy\` (\`Data Row 1..5\`).
  - \`Col1[HDR]\` has printed row labels (e.g. IF, ID, OF, IE, WB, or statements). NEVER place \`write()\`, \`check()\`, or \`cross()\` inside \`Col1\` or \`Header Row\`!
  - Hybrid Multi-Part Questions (\`(a)\` Table + \`(b)\` Worksheet Lines): When a requested question has BOTH a table (e.g. \`Part (a)\` tick-boxes \`RISC | CISC\`) AND blank worksheet lines (\`Part (b)\` \`SLOT_1..M\`), you MUST solve **BOTH parts in the same response**: place \`check(cx, cy)\` in every row of the \`Part (a)\` table using the exact \`(cx, cy)\` from \`Detected Table/Grid 1\`, AND call \`write(writeX, writeY, "...")\` on \`SLOT_1..M\` to answer \`Part (b)\`'s prompt! Never write \`Part (a)\` table statements onto \`Part (b)\`'s answer lines.
  - In a 5-stage pipeline timing table (Data Row 1=IF, Row 2=ID, Row 3=OF, Row 4=IE, Row 5=WB) with instructions I1..I6, shift right by 1 column on EVERY row: Row 1 (IF) fills C_1..C_6; Row 2 (ID) fills C_2..C_7; Row 3 (OF) fills C_3..C_8; Row 4 (IE) fills C_4..C_9; Row 5 (WB) fills C_5..C_10 — ALL 5 rows including WB must be filled with \`write(cx, cy, "I1")\`.. \`write(cx, cy, "I6")\`! Do NOT open \`paper()\` in the same turn when filling all 30 table cells.
- Continuation: Call \`startOver();\` at \`step(1);\` only when starting a new question or replacing an existing card.`;

export function compactLayoutPromptNoteForGroqVision(note: string): string {
  if (!note) return "";
  const lines = note.split(/\r?\n/);
  const out: string[] = [];
  for (const line of lines) {
    if (
      line.includes("Pixel Coordinate Ruler") ||
      line.includes("For table/grid markings") ||
      line.includes("CRITICAL WORKSHEET LINE RULE") ||
      line.includes("contains the pre-printed row labels/stage names") ||
      /^\s*-\s*\*\*Header Row\*\*/.test(line)
    ) {
      continue;
    }
    const rowMatch = line.match(
      /^\s*-\s*\*\*Row\s+(\d+)\*\*\s*\((?:y=\d+\.\.\d+,\s*)?\*\*cy=(\d+)\*\*\)(?:\s*\["([^"]*)"\])?:\s*(.*)$/
    );
    if (rowMatch) {
      const rNum = rowMatch[1];
      const cy = rowMatch[2];
      const rLabel = rowMatch[3] ? ` ["${rowMatch[3]}"]` : "";
      const cellsRest = rowMatch[4] || "";
      const dataColMatches = Array.from(
        cellsRest.matchAll(
          /\[Data Col (\d+)(?: "([^"]*)")?\]:\s*\*\*\((\d+),\s*(\d+)\)\*\*/g
        )
      );
      if (dataColMatches.length > 0 && dataColMatches.length <= 4) {
        const sep = dataColMatches.length === 2 ? " OR " : " | ";
        const compactCols = dataColMatches
          .map((m) => {
            const dIdx = m[1];
            const dHdr = m[2] ? `["${m[2]}"]` : "";
            return `C_${dIdx}${dHdr}=(${m[3]}, ${m[4]})`;
          })
          .join(sep);
        out.push(`  - Data Row ${rNum} (cy=${cy})${rLabel}: ${compactCols}`);
      } else {
        out.push(`  - Data Row ${rNum}: cy=${cy}${rLabel}`);
      }
      continue;
    }
    const colLine = line
      .replace(
        /Col 1 \[(?:Row Labels \/ Stage Names|Row Header(?:: "([^"]*)")?)\] \(x=\d+\.\.\d+,\s*cx=(\d+)\)/g,
        (_m, hdr, cx) =>
          hdr ? `Col1[HDR:"${hdr}"](cx=${cx})` : `Col1[HDR](cx=${cx})`
      )
      .replace(
        /Col (\d+) \[Data Col (\d+)(?:: "([^"]*)")?\] \(x=\d+\.\.\d+,\s*\*\*cx=(\d+)\*\*\)/g,
        (_m, _colNum, dataColNum, hdr, cx) =>
          hdr
            ? `C_${dataColNum}["${hdr}"](cx=${cx})`
            : `C_${dataColNum}(cx=${cx})`
      )
      .replace(/\[Row Header\]/g, "[HDR]")
      .replace(/\[Data Col (\d+)(?: "([^"]*)")?\]/g, (_m, dNum, hdr) =>
        hdr ? `[C_${dNum} "${hdr}"]` : `[C_${dNum}]`
      );
    out.push(colLine);
  }
  return out.join("\n");
}

export function stripDetectedTableGridFromNote(
  note: string,
  calm: { x: number; y: number; w: number; h: number },
  hasSlotsForTargetQ: boolean,
  requestedQNum: string | null = null
): string {
  if (!note) return "";
  const lines = note.split(/\r?\n/);

  let targetQMinY = -1;
  if (requestedQNum) {
    const qPrefixRegex = new RegExp(
      `^\\s*(?:0\\s*)?${requestedQNum}(?:\\.|\\s|$)`
    );
    for (const line of lines) {
      const m = line.match(
        /^-\s*\[x=(\d+),\s*y=(\d+),\s*w=\d+,\s*h=\d+,\s*bottomY=\d+\]\s*"(.*)"$/
      );
      if (m) {
        const x = parseInt(m[1], 10);
        const y = parseInt(m[2], 10);
        const normText = m[3]
          .replace(/^\s*[fIl|](\d)\b/, "1$1")
          .replace(/^\s*[Il|]\s*\./, "1.");
        if (x < 380 && y >= 150 && qPrefixRegex.test(normText)) {
          targetQMinY = y;
          break;
        }
      }
    }
  }

  const out: string[] = [];
  let inTableBlock = false;
  let inOcrSection = false;
  let inSlotsSection = false;
  for (const line of lines) {
    if (
      /^\s*-\s*\*\*(?:Detected Table\/Grid|MULTI-PART QUESTION STRUCTURE)\b/i.test(
        line
      )
    ) {
      inTableBlock = true;
      continue;
    }
    if (inTableBlock) {
      if (/^\s{2,}-\s/.test(line)) {
        continue;
      }
      inTableBlock = false;
    }
    if (line.startsWith("### [SCREEN OCR TEXT & HEADINGS")) {
      inOcrSection = true;
      inSlotsSection = false;
      out.push(line);
      continue;
    }
    if (line.startsWith("### [FILLABLE ANSWER SLOTS")) {
      inOcrSection = false;
      inSlotsSection = true;
      if (!hasSlotsForTargetQ) {
        continue;
      }
      out.push(line);
      continue;
    }
    if (inOcrSection) {
      if (
        line.startsWith("### [") ||
        line.startsWith("- **Optional Suggested Calm Region")
      ) {
        inOcrSection = false;
      } else if (targetQMinY > 0) {
        const ym = line.match(/^- \[x=\d+,\s*y=(\d+),/);
        if (ym && parseInt(ym[1], 10) < targetQMinY - 10) {
          continue;
        }
      }
    }
    if (inSlotsSection) {
      if (
        line.startsWith("### [") ||
        line.startsWith("- **Optional Suggested Calm Region")
      ) {
        inSlotsSection = false;
      } else {
        if (!hasSlotsForTargetQ) {
          continue;
        }
        if (targetQMinY > 0 && /^- SLOT_\d+\b/.test(line)) {
          const bm = line.match(/\bbaseline\s+y=(\d+)\b/);
          if (bm && parseInt(bm[1], 10) < targetQMinY - 10) {
            continue;
          }
        }
      }
    }
    if (
      !hasSlotsForTargetQ &&
      /^\s*-\s*\*\*CRITICAL WORKSHEET LINE RULE\*\*/i.test(line)
    ) {
      continue;
    }
    out.push(line);
  }
  if (!hasSlotsForTargetQ) {
    out.push(
      `- **Target Question Instruction**: Locate the requested question${requestedQNum ? ` (Question ${requestedQNum})` : ""} in \`[SCREEN OCR TEXT & HEADINGS]\` or \`[Screen Vision Analysis]\`, \`underline(x, bottomY + 2, w)\` its heading on the page, and open ONE \`paper(${calm.x}, ${calm.y}, ${calm.w}, ${calm.h}, { title: "...", heading: "..." })\` whiteboard card to write the full worked solution to Question ${requestedQNum || ""}!`
    );
  }
  return out.join("\n");
}

const ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS = `You are Clicky (Kursor OneBrain), an interactive multimodal AI companion that speaks to the user AND draws live 2D ink marks, whiteboard diagrams, worked solutions, and interactive 3D studio models directly on their 1280x720 Windows screen.

CRITICAL OUTPUT RULES:
- Respond with ONLY a single \`\`\`js ... \`\`\` JavaScript program for the ClickyDraw v1.6 engine.
- NEVER respond with plain Markdown prose outside \`\`\`js ... \`\`\`.
- Structure every program into 2 to 4 paced beats (\`step(1);\`, \`step(2);\`, \`step(3);\`, optional \`step(4);\`).
- Every beat MUST start with \`step(k);\` and one or two concise, natural spoken sentences \`say("...");\` (10–24 words each), immediately followed by the 2D or 3D drawing calls that appear in lockstep while that sentence is spoken.

## 1. Canvas Coordinate System & Pixel Ruler Reading (1280 x 720 Pixels)
- The screen canvas is ALWAYS \`W = 1280\` pixels wide by \`H = 720\` pixels tall (\`x\` in \`0..1280\`, \`y\` in \`0..720\`).
- Every attached screenshot has a **pixel coordinate ruler** along the **top edge (\`x = 100, 200, ... 1200\` with 50px ticks)** and **left edge (\`y = 100, 200, ... 700\` with 50px ticks)**, plus subtle grid crosshairs at every \`(100*m, 100*n)\` intersection.
- **ALWAYS read the exact \`(x, y)\` coordinates** of question numbers, text baselines, equations, diagrams, and UI buttons directly from the top and left ruler ticks on the screenshot (as well as **[Measured Screen Geometry]** when provided) before placing any mark! Never guess coordinates.

## 2. Marking the Screen Lightly (Surgical Marks — NEVER Stack \`frame()\` Boxes)
- To mark a question number, sub-question (\`(a)\`, \`(b)(i)\`), equation, or key phrase on a PDF/document:
  - Prefer a surgical \`underline(x, y, w)\` directly beneath the question label or key words (\`y\` is the bottom baseline of the letters), OR
  - \`point(x, y, "Label")\` (circle + dot pointer at exact spot \`(x, y)\` with a gel name pill), OR
  - \`ring(x, y, w, h, "Label")\` around ONE small specific term, number, button, or answer box.
- **NEVER stack multiple large \`frame()\` rectangles across a document or overlap \`frame()\` boxes!** Use at most ONE \`frame(x, y, w, h, "Label")\` only when highlighting a single distinct panel or diagram region.

## 3. Working Through Math, Science & Exam Questions (\`paper()\` + Progressive \`step(1..N)\`)
When the user asks to solve a math, calculus, physics, CS, or exam question (where there are no blank worksheet lines to fill in), or asks for a worked explanation:
1. Lightly mark the question or sub-part on the document using \`underline(x, y, w)\` or \`point(x, y, "Label")\` as you introduce each step.
2. Open **ONE** clean whiteboard window on the calm side of the screen (using the **Best Calm Space** from **[Measured Screen Geometry]**, e.g. \`x=780, y=70, w=460, h=560\` when the document is on the left, or \`x=32, y=120, w=340, h=480\` when the document is on the right):
   \`const a = paper(780, 70, 460, 560, { title: "Question 1", heading: "Differentiation & Stationary Points" });\`
   (\`a\` returns \`{ x, y, w, h }\` of the drawable writing area below the heading!).
3. Inside \`paper()\`, write each equation, derivation step, or diagram **progressively across \`step(1)\`, \`step(2)\`, \`step(3)\` (and \`step(4)\`)** using \`text(a.x, a.y + offset, "...", { size: 17, bold: true/false, w: a.w })\` so each line of working is written in lockstep as Clicky speaks that step:
   \`\`\`js
   step(1);
   say("Let's differentiate y equals 5 sin 2x cos squared x using the product rule.");
   underline(142, 198, 210);
   const a = paper(780, 70, 460, 520, { title: "Question 1", heading: "Stationary Point M" });
   text(a.x, a.y + 18, "y = 5 sin(2x) cos²(x)", { size: 18, weight: 600 });
   text(a.x, a.y + 52, "dy/dx = 10 cos(2x) cos²(x) - 10 sin(2x) cos(x) sin(x)", { size: 17 });

   step(2);
   say("At the maximum point M, dy by dx equals zero, and since cos x is non-zero we factor it out.");
   text(a.x, a.y + 92, "At M, dy/dx = 0:", { size: 17, tone: "grey" });
   text(a.x, a.y + 122, "cos(2x) cos(x) - sin(2x) sin(x) = 0", { size: 17 });
   text(a.x, a.y + 154, "Using cos(2x + x) = cos(3x) = 0  (or tan²x = 1/3)", { size: 17 });

   step(3);
   say("Solving cos 3x equals zero gives the exact x-coordinate of M as pi over 6.");
   text(a.x, a.y + 194, "3x = π/2  ⇒  x = π/6", { size: 20, weight: 600, tone: "blue" });
   lessonTitle("Stationary Point by Differentiation");
   seen("Solving calculus differentiation exam question on screen");
   \`\`\`
4. For a short 3–6 item checklist or summary list, you may use \`receipt(x, y, title, items, { w: 400 })\`. **NEVER pass empty strings \`""\` in the \`items\` array** of \`receipt()\` (every element is numbered \`1.\`, \`2.\`, etc.).

## 4. Solving Worksheets, Tables & Blank Dotted Lines Directly on Screen
- **Understanding Table Coordinates in \`[Measured Screen Geometry]\`**:
  - \`Col 1 [Row Header]\` is ALWAYS the leftmost column containing printed row labels (e.g., \`IF\`, \`ID\`, \`OF\`, \`IE\`, \`WB\`, input variables, or statement text). **NEVER place \`write()\`, \`check()\`, or \`cross()\` inside \`Col 1\`!**
  - Data columns start at **\`Col 2 [Data Col 1]\`** (1st data column, e.g., Clock Cycle \`1\`), **\`Col 3 [Data Col 2]\`** (Clock Cycle \`2\`), **\`Col 4 [Data Col 3]\`**, etc.
  - Data rows are \`Row 1\`, \`Row 2\`, ..., \`Row N\` (below \`Header Row\`). You MUST fill **EVERY** data row (\`Row 1\` through \`Row N\`, including the final row such as \`WB\`!) using the exact \`(cx, cy)\` cell centers from **[Measured Screen Geometry]**.
- **Tick / Cross Tables (\`check(cx, cy)\` / \`cross(cx, cy)\`)**:
  - \`check(cx, cy)\` — Draws a crisp hand-drawn checkmark (✓) centered at \`(cx, cy)\`.
  - \`cross(cx, cy)\` — Draws a crisp hand-drawn X (✗) centered at \`(cx, cy)\`.
  - When solving a statement table (e.g., RISC vs CISC, True/False), place \`check(cx, cy)\` (and \`cross(cx, cy)\` if requested) at the **exact \`(cx, cy)\` cell centers** in \`Col 2..M\` for EVERY row (\`Row 1..N\`)!
  - NEVER use \`point()\` to label table rows — ALWAYS call \`check(cx, cy)\` and \`cross(cx, cy)\` directly inside the table cells!
- **Text / Value Fill Tables (\`write(cx, cy, "val", { size: 15, anchor: "middle" })\`)**:
  - When filling in a table with text, numbers, pipeline instructions, or trace values (e.g., 5-stage pipeline table, truth table, trace table, schedule), call \`write(cx, cy, "value", { size: 15, anchor: "middle" })\` at each target cell's exact \`(cx, cy)\` center in \`Col 2..M\` across **all** data rows \`Row 1..N\`. Always pass \`anchor: "middle"\` so the text is centered inside the cell!
- **Write Directly on Blank Worksheet Lines**:
  - \`write(x, y, "Concise handwritten clause", { size: 17 })\` — Writes directly on a blank or dotted worksheet line at \`(x, y)\`. Outside of table cells and **Detected Blank Worksheet Answer Lines**, NEVER use \`write()\` on top of printed questions, math formulas, or graphs!
  - **CRITICAL SINGLE-LINE BUDGET**: Each \`SLOT_N\` is a **single horizontal line** (\`max 1 line\`, \`<= 75 characters\`). NEVER write a multi-sentence or >80-character paragraph into a single \`write()\` call!
  - **PAIRED NAME + DESCRIPTION SLOTS**: When a slot labeled \`Architecture N\` / \`Category N\` / \`Term N\` / \`Component N\` / \`Method N\` is immediately followed by a slot labeled \`Description\` / \`Explanation\`, write **ONLY the concise name/identifier** on the first slot (e.g. \`"Multiple Instruction Multiple Data (MIMD)"\`) and write **ONLY the 1-line description** (without repeating the label or name prefix) on the \`Description\` slot below it!
- **Hybrid Multi-Part Questions (\`(a)\` Table + \`(b)\` Worksheet Lines)**:
  - When a requested question has BOTH a table (e.g. \`Part (a)\` tick-boxes \`RISC | CISC\`) AND blank worksheet lines (\`Part (b)\` \`SLOT_1..M\`), you MUST solve **BOTH parts in the same response**: place \`check(cx, cy)\` in every row of the \`Part (a)\` table using the exact \`(cx, cy)\` from \`Detected Table/Grid 1\`, AND call \`write(writeX, writeY, "...")\` on \`SLOT_1..M\` to answer \`Part (b)\`'s prompt! Never write \`Part (a)\` table statements onto \`Part (b)\`'s answer lines.
- **Single Card Rule (NEVER Stack \`paper()\` and \`receipt()\`)**:
  - Never open BOTH a \`paper()\` card AND a \`receipt()\` (or \`clipping()\`) card on the same turn! Choose at most ONE card (\`paper()\` OR \`receipt()\`), or no card at all when filling a self-explanatory table or worksheet.

## 5. Canvas Continuation vs \`startOver()\`
- Check **[Turn Context]** (\`Active Drawing on Screen\` and \`Current Drawing Code on Screen\`) and your previous assistant turns in the conversation history:
  - **Continue on the existing canvas (do NOT call \`startOver()\`)** when the user asks a follow-up about what you just drew, asks you to point to / underline another part of the same question, or asks to add a step or highlight an existing part (\`highlight("p1", "coral")\`, \`erase("p1")\`, \`move("p1", dx, dy)\`).
  - **Call \`startOver();\` at the start of \`step(1);\`** when switching to a brand-new question, opening a new \`paper()\` or \`receipt()\` card to replace an existing card on screen, or when the user asks to clear/redo the layout.

## 6. Visual Diagrams & Charts Inside \`paper()\`
- Inside \`const a = paper(x, y, w, h, { title: "...", heading: "..." });\`, combine shapes and text cleanly:
  - \`rect(a.x + dx, a.y + dy, rw, rh, { fill: "sky", fillAlpha: 0.25, tone: "blue", r: 6 })\`
  - \`circle(cx, cy, r, { fill: "butter", fillAlpha: 0.35, tone: "orange" })\`
  - \`arrow([[x1, y1], [x2, y2]], { tone: "ink", size: 1.5, head: 9 })\`
  - \`line([[x1, y1], [x2, y2]], { tone: "grey", size: 1.5, dash: true })\`
  - \`text(tx, ty, "Label", { size: 17, tone: "ink", anchor: "middle" })\`
  - \`const g = graph(a.x + 40, a.y + 20, a.w - 70, a.h - 70, { x: [0, 10], y: [0, 100], xLabel: "Time", yLabel: "Output", grid: true, ticks: true }); g.plot(u => u * u, { tone: "blue", label: "Curve" });\`
  - \`timeline(a.x + 20, a.y + a.h / 2, a.w - 40, [[1970, "Event 1"], [1990, "Event 2"], [2020, "Event 3"]], { slots: "alternate" });\`

## 7. Studio 3D Procedural Modeling (\`figure(x, y, w, h, opts)\`)
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
- **Power Multipliers**:
  - \`around: { count: 10, axis: "x"|"y"|"z", center: [0,0,0] }\`, \`copies: { count: 4, step: [0.3, 0, 0] }\`, \`mirror: "x"|"y"|"z"\`.
- **Materials (\`mat\`)**: \`"steel"\`, \`"alu"\`, \`"titanium"\`, \`"chrome"\`, \`"brass"\`, \`"copper"\`, \`"gold"\`, \`"cast"\`, \`"rubber"\`, \`"glass"\`, \`"water"\`, \`"wood"\`, \`"ceramic"\`, \`"carbon"\`, \`"paint:red"\`, \`"paint:blue"\`, \`"paint:green"\`, \`"paint:yellow"\`, \`"glow:orange"\`, \`"glow:blue"\`.
- **Labels, Live Readouts & Animation**:
  - ALWAYS store parts in variables: \`const housing = f.cyl(...); const rotor = f.gear(...); const shaft = f.cyl(...);\`
  - Pass the **part variable** (or a \`[x, y, z]\` array, NEVER a string!) to \`f.label\`: \`f.label(housing, "Cutaway Housing", { side: "left" });\`
  - Inside \`f.tick((t) => { ... })\`, mutate the **part variables directly** (\`rotor.rot = [t * 180, 0, 0]; shaft.rot = [t * 180, 0, 0];\`). NEVER use \`f.parts[...]\`.
  - \`f.readout({ Status: "Active", Speed: "10,000 RPM" })\`

## 8. Performing Actions & Controlling the User's Laptop
- **Autonomous Laptop Tasks via Agent (\`handOff\`)**:
  - \`handOff({ task: "Detailed description of the exact task to execute on the user's PC", label: "Short Task Title", newClicky: { name: "Desktop Automator", role: "PC & File Automation Agent", description: "Organizes files, runs scripts, and automates desktop workflows." } });\`
- **Direct Screen Actuation & Interactive Walkthroughs**:
  - \`target(x, y, w, h, "Click here")\`, \`hover(x, y, w, h, "Hover here")\`, \`typeText(x, y, "text to type")\`, \`copyText("exact text")\`, \`openPage("https://...")\`, \`button(x, y, "Button Label", { open: "https://..." } | { copy: "..." } | { ask: "Follow-up prompt" } | { handOff: { task: "...", label: "..." } })\`.`;

const ONE_BRAIN_SYSTEM_PROMPT = ONE_BRAIN_DRAWING_AND_3D_INSTRUCTIONS;

function loadSystemInstructions(): string {
  return ONE_BRAIN_SYSTEM_PROMPT;
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

function stripBase64Screenshots(val: unknown): unknown {
  if (typeof val === "string") {
    if (val.startsWith("data:image/")) {
      return "[screenshot_omitted]";
    }
    return val.replace(
      /data:image\/[a-zA-Z0-9.+-]+;base64,[A-Za-z0-9+/=]+/g,
      "[screenshot_omitted]"
    );
  }
  if (Array.isArray(val)) {
    return val.map(stripBase64Screenshots);
  }
  if (val && typeof val === "object") {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(val as Record<string, unknown>)) {
      if (
        (k === "image_url" || k === "base64") &&
        typeof v === "string" &&
        (v.startsWith("data:image/") || v.length > 512)
      ) {
        out[k] = "[screenshot_omitted]";
      } else {
        out[k] = stripBase64Screenshots(v);
      }
    }
    return out;
  }
  return val;
}

function stripCommentsAndStrings(code: string): string {
  let out = "";
  let i = 0;
  const n = code.length;
  while (i < n) {
    const ch = code[i];
    const next = i + 1 < n ? code[i + 1] : "";
    if (ch === "/" && next === "/") {
      out += "  ";
      i += 2;
      while (i < n && code[i] !== "\n") {
        out += " ";
        i++;
      }
      continue;
    }
    if (ch === "/" && next === "*") {
      out += "  ";
      i += 2;
      while (i < n && !(code[i] === "*" && code[i + 1] === "/")) {
        out += code[i] === "\n" ? "\n" : " ";
        i++;
      }
      if (i < n) {
        out += "  ";
        i += 2;
      }
      continue;
    }
    if (ch === '"' || ch === "'" || ch === "`") {
      const quote = ch;
      out += quote;
      i++;
      while (i < n && code[i] !== quote) {
        if (code[i] === "\\" && i + 1 < n) {
          out += "  ";
          i += 2;
        } else {
          out += code[i] === "\n" ? "\n" : " ";
          i++;
        }
      }
      if (i < n) {
        out += quote;
        i++;
      }
      continue;
    }
    out += ch;
    i++;
  }
  return out;
}

function advanceStringOrComment(code: string, i: number): number {
  const n = code.length;
  const ch = code[i];
  const next = i + 1 < n ? code[i + 1] : "";
  if (ch === "/" && next === "/") {
    i += 2;
    while (i < n && code[i] !== "\n") i++;
    return i;
  }
  if (ch === "/" && next === "*") {
    i += 2;
    while (i < n && !(code[i] === "*" && code[i + 1] === "/")) i++;
    return i < n ? i + 2 : n;
  }
  if (ch === '"' || ch === "'" || ch === "`") {
    const quote = ch;
    i++;
    while (i < n && code[i] !== quote) {
      if (code[i] === "\\" && i + 1 < n) {
        i += 2;
      } else {
        i++;
      }
    }
    return i < n ? i + 1 : n;
  }
  return i + 1;
}

function splitTopLevelElements(inner: string): string[] {
  const items: string[] = [];
  let start = 0;
  let i = 0;
  let pDepth = 0;
  let bDepth = 0;
  let cDepth = 0;
  const n = inner.length;

  while (i < n) {
    const ch = inner[i];
    const next = i + 1 < n ? inner[i + 1] : "";
    if (
      ch === '"' ||
      ch === "'" ||
      ch === "`" ||
      (ch === "/" && (next === "/" || next === "*"))
    ) {
      i = advanceStringOrComment(inner, i);
      continue;
    }
    if (ch === "(") pDepth++;
    else if (ch === ")" && pDepth > 0) pDepth--;
    else if (ch === "[") bDepth++;
    else if (ch === "]" && bDepth > 0) bDepth--;
    else if (ch === "{") cDepth++;
    else if (ch === "}" && cDepth > 0) cDepth--;
    else if (ch === "," && pDepth === 0 && bDepth === 0 && cDepth === 0) {
      items.push(inner.slice(start, i));
      start = i + 1;
    }
    i++;
  }
  items.push(inner.slice(start));
  return items;
}

function sanitizeReceiptEmptyItems(code: string): string {
  let out = "";
  let i = 0;
  const n = code.length;

  while (i < n) {
    const ch = code[i];
    const next = i + 1 < n ? code[i + 1] : "";
    if (
      ch === '"' ||
      ch === "'" ||
      ch === "`" ||
      (ch === "/" && (next === "/" || next === "*"))
    ) {
      const nextIdx = advanceStringOrComment(code, i);
      out += code.slice(i, nextIdx);
      i = nextIdx;
      continue;
    }

    const isReceiptStart =
      (i === 0 || !/[A-Za-z0-9_$]/.test(code[i - 1])) &&
      code.startsWith("receipt", i) &&
      !/[A-Za-z0-9_$]/.test(code[i + 7] ?? "");

    if (isReceiptStart) {
      let j = i + 7;
      while (j < n && /\s/.test(code[j])) j++;
      if (j < n && code[j] === "(") {
        const openParenIdx = j;
        let k = openParenIdx + 1;
        let pDepth = 1;
        let bDepth = 0;
        let cDepth = 0;
        const commaIndices: number[] = [];

        while (k < n && pDepth > 0) {
          const c = code[k];
          const nx = k + 1 < n ? code[k + 1] : "";
          if (
            c === '"' ||
            c === "'" ||
            c === "`" ||
            (c === "/" && (nx === "/" || nx === "*"))
          ) {
            k = advanceStringOrComment(code, k);
            continue;
          }
          if (c === "(") pDepth++;
          else if (c === ")") {
            pDepth--;
            if (pDepth === 0) break;
          } else if (c === "[") bDepth++;
          else if (c === "]" && bDepth > 0) bDepth--;
          else if (c === "{") cDepth++;
          else if (c === "}" && cDepth > 0) cDepth--;
          else if (c === "," && pDepth === 1 && bDepth === 0 && cDepth === 0) {
            commaIndices.push(k);
          }
          k++;
        }

        if (pDepth === 0 && commaIndices.length >= 3) {
          const closeParenIdx = k;
          const arg3Start = commaIndices[2] + 1;
          const arg3End =
            commaIndices.length >= 4 ? commaIndices[3] : closeParenIdx;
          const rawArg3 = code.slice(arg3Start, arg3End);
          const trimmedArg3 = rawArg3.trim();

          if (trimmedArg3.startsWith("[") && trimmedArg3.endsWith("]")) {
            const innerArray = trimmedArg3.slice(1, -1);
            const rawElements = splitTopLevelElements(innerArray);
            const keptElements = rawElements
              .map((el) => el.trim())
              .filter(
                (el) => el.length > 0 && !/^(["'`])\s*\1$/.test(el)
              );
            const leadingWs = rawArg3.match(/^\s*/)?.[0] || " ";
            const trailingWs = rawArg3.match(/\s*$/)?.[0] || "";
            const rebuiltArg3 = `${leadingWs}[${keptElements.join(", ")}]${trailingWs}`;

            out +=
              code.slice(i, arg3Start) +
              rebuiltArg3 +
              code.slice(arg3End, closeParenIdx + 1);
            i = closeParenIdx + 1;
            continue;
          }
        }
      }
    }

    out += ch;
    i++;
  }

  return out;
}

export function stripRedundantCardsWhenPaperExists(code: string): string {
  const stripped = stripCommentsAndStrings(code);
  if (
    !/\bpaper\s*\(/.test(stripped) ||
    !/\b(?:receipt|clipping|note|window)\s*\(/.test(stripped)
  ) {
    return code;
  }

  let out = "";
  let i = 0;
  const n = code.length;

  while (i < n) {
    const ch = code[i];
    const next = i + 1 < n ? code[i + 1] : "";
    if (
      ch === '"' ||
      ch === "'" ||
      ch === "`" ||
      (ch === "/" && (next === "/" || next === "*"))
    ) {
      const nextIdx = advanceStringOrComment(code, i);
      out += code.slice(i, nextIdx);
      i = nextIdx;
      continue;
    }

    const prevBoundary = i === 0 || !/[A-Za-z0-9_$.]/.test(code[i - 1]);
    let fnName: string | null = null;
    if (prevBoundary) {
      for (const candidate of ["receipt", "clipping", "note", "window"]) {
        if (
          code.startsWith(candidate, i) &&
          !/[A-Za-z0-9_$]/.test(code[i + candidate.length] ?? "")
        ) {
          fnName = candidate;
          break;
        }
      }
    }

    if (fnName) {
      let j = i + fnName.length;
      while (j < n && /\s/.test(code[j])) j++;
      if (j < n && code[j] === "(") {
        let k = j + 1;
        let pDepth = 1;
        while (k < n && pDepth > 0) {
          const c = code[k];
          const nx = k + 1 < n ? code[k + 1] : "";
          if (
            c === '"' ||
            c === "'" ||
            c === "`" ||
            (c === "/" && (nx === "/" || nx === "*"))
          ) {
            k = advanceStringOrComment(code, k);
            continue;
          }
          if (c === "(") pDepth++;
          else if (c === ")") {
            pDepth--;
            if (pDepth === 0) break;
          }
          k++;
        }
        if (pDepth === 0) {
          out += "Object({ x: 0, y: 0, w: 0, h: 0 })";
          i = k + 1;
          continue;
        }
      }
    }

    out += ch;
    i++;
  }

  return out
    .replace(
      /(?<=;)[ \t]*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?[ \t]*(?:\/\/[^\r\n]*)?/g,
      ""
    )
    .replace(
      /(?:^|(?<=[\r\n]))[ \t]*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?[ \t]*(?:\/\/[^\r\n]*)?(?:\r?\n|$)/g,
      ""
    )
    .replace(
      /^[ \t]*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?[ \t]*/gm,
      ""
    )
    .trim();
}

export function mergeMultiplePaperCards(code: string): string {
  const stripped = stripCommentsAndStrings(code);
  const paperMatches = stripped.match(/\bpaper\s*\(/g);
  if (!paperMatches || paperMatches.length < 2) {
    return code;
  }

  interface PaperDeclSpan {
    stmtStart: number;
    callStart: number;
    openParenIdx: number;
    closeParenIdx: number;
    stmtEnd: number;
    varName: string | null;
  }

  const spans: PaperDeclSpan[] = [];
  const n = code.length;
  let i = 0;

  while (i < n) {
    const ch = code[i];
    const next = i + 1 < n ? code[i + 1] : "";
    if (
      ch === '"' ||
      ch === "'" ||
      ch === "`" ||
      (ch === "/" && (next === "/" || next === "*"))
    ) {
      i = advanceStringOrComment(code, i);
      continue;
    }

    const prevBoundary = i === 0 || !/[A-Za-z0-9_$.]/.test(code[i - 1]);
    if (
      prevBoundary &&
      code.startsWith("paper", i) &&
      !/[A-Za-z0-9_$]/.test(code[i + 5] ?? "")
    ) {
      let j = i + 5;
      while (j < n && /\s/.test(code[j])) j++;
      if (j < n && code[j] === "(") {
        let k = j + 1;
        let pDepth = 1;
        while (k < n && pDepth > 0) {
          const c = code[k];
          const nx = k + 1 < n ? code[k + 1] : "";
          if (
            c === '"' ||
            c === "'" ||
            c === "`" ||
            (c === "/" && (nx === "/" || nx === "*"))
          ) {
            k = advanceStringOrComment(code, k);
            continue;
          }
          if (c === "(") pDepth++;
          else if (c === ")") {
            pDepth--;
            if (pDepth === 0) break;
          }
          k++;
        }
        if (pDepth === 0) {
          const prefixSlice = code.slice(0, i);
          const declMatch = prefixSlice.match(
            /(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*$/
          );
          const stmtStart = declMatch ? i - declMatch[0].length : i;
          const varName = declMatch ? declMatch[1] : null;
          let stmtEnd = k + 1;
          while (
            stmtEnd < n &&
            (code[stmtEnd] === " " || code[stmtEnd] === "\t")
          ) {
            stmtEnd++;
          }
          if (stmtEnd < n && code[stmtEnd] === ";") {
            stmtEnd++;
          }
          spans.push({
            stmtStart,
            callStart: i,
            openParenIdx: j,
            closeParenIdx: k,
            stmtEnd,
            varName,
          });
          i = stmtEnd;
          continue;
        }
      }
    }
    i++;
  }

  if (spans.length < 2) {
    return code;
  }

  const first = spans[0];
  const firstVar = first.varName || "a";
  const declaredVars = new Set<string>([firstVar]);
  const spanAssignedVars: string[] = [firstVar];
  for (let sIdx = 1; sIdx < spans.length; sIdx++) {
    const rawVar = spans[sIdx].varName;
    const assigned =
      rawVar && !declaredVars.has(rawVar) ? rawVar : `__paper${sIdx}`;
    declaredVars.add(assigned);
    spanAssignedVars.push(assigned);
  }

  const firstInner = code.slice(first.openParenIdx + 1, first.closeParenIdx);
  const bumpedFirstInner = firstInner.replace(
    /^(\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?\s*,\s*)(-?\d+(?:\.\d+)?)/,
    (_m, prefixArgs: string, hStr: string) => {
      const h = parseFloat(hStr);
      if (!isNaN(h) && h < 480) {
        const bumpedH = Math.min(540, Math.max(Math.round(h), 480));
        return `${prefixArgs}${bumpedH}`;
      }
      return `${prefixArgs}${hStr}`;
    }
  );
  const firstDeclPrefix = first.varName
    ? code.slice(first.stmtStart, first.callStart)
    : `const ${firstVar} = `;
  const firstTrailing = code.slice(first.closeParenIdx + 1, first.stmtEnd);
  const firstReplacement = `${firstDeclPrefix}paper(${bumpedFirstInner})${firstTrailing || ";"}`;

  let out = code.slice(0, first.stmtStart) + firstReplacement;
  let cumulativeOffset = 0;

  for (let sIdx = 0; sIdx < spans.length; sIdx++) {
    const curSpan = spans[sIdx];
    const nextSpan = sIdx + 1 < spans.length ? spans[sIdx + 1] : null;
    const sectionStart = curSpan.stmtEnd;
    const sectionEnd = nextSpan ? nextSpan.stmtStart : code.length;
    let sectionCode = code.slice(sectionStart, sectionEnd);

    const activeVar = spanAssignedVars[sIdx];
    if (sIdx > 0 && activeVar !== firstVar) {
      sectionCode = sectionCode.replace(
        new RegExp(`\\b${firstVar}(\\s*\\.\\s*[xywh])\\b`, "g"),
        `${activeVar}$1`
      );
    }

    out += sectionCode;

    if (nextSpan) {
      const strippedSection = stripCommentsAndStrings(sectionCode);
      const offsetRegex = new RegExp(
        `\\b(?:${activeVar}|${firstVar})\\s*\\.\\s*y\\s*\\+\\s*(\\d+(?:\\.\\d+)?)`,
        "g"
      );
      let maxSectionOffset = 0;
      for (const om of strippedSection.matchAll(offsetRegex)) {
        const val = parseFloat(om[1]);
        if (!isNaN(val) && val > maxSectionOffset) {
          maxSectionOffset = val;
        }
      }
      if (maxSectionOffset <= 0) {
        maxSectionOffset = sIdx === 0 ? 120 : 90;
      }
      cumulativeOffset += Math.round(maxSectionOffset + 35);
      const nextVar = spanAssignedVars[sIdx + 1];
      out += `const ${nextVar} = { x: ${firstVar}.x, y: ${firstVar}.y + ${cumulativeOffset}, w: ${firstVar}.w, h: ${firstVar}.h };`;
    }
  }

  return out;
}

export function convertPaperTextToSlotWritesIfNeeded(
  code: string,
  slotCount: number
): string {
  if (!code || slotCount <= 0) return code;
  const cleanedCode = code.replace(
    /[ \t]*\bwrite\s*\(\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?\s*,\s*(["'`])\s*\1(?:\s*,\s*\{[^{}]*\})?\s*\)\s*;?[ \t]*\r?\n?/g,
    ""
  );
  const stripped = stripCommentsAndStrings(cleanedCode);
  if (/\b(?:write|check|cross)\s*\(/.test(stripped)) return cleanedCode;
  if (!/\bpaper\s*\(/.test(stripped)) return cleanedCode;
  code = cleanedCode;

  const boundVars = new Set<string>();
  const varAssignRegex =
    /\b(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*paper\s*\(/g;
  for (const m of code.matchAll(varAssignRegex)) {
    if (m[1]) boundVars.add(m[1]);
  }
  if (boundVars.size === 0) {
    boundVars.add("a");
  }

  interface TextCallMatch {
    start: number;
    end: number;
    textValue: string;
  }

  const matches: TextCallMatch[] = [];
  const n = code.length;
  let i = 0;
  while (i < n) {
    const ch = code[i];
    const next = i + 1 < n ? code[i + 1] : "";
    if (
      ch === '"' ||
      ch === "'" ||
      ch === "`" ||
      (ch === "/" && (next === "/" || next === "*"))
    ) {
      i = advanceStringOrComment(code, i);
      continue;
    }
    const prevBoundary = i === 0 || !/[A-Za-z0-9_$.]/.test(code[i - 1]);
    if (
      prevBoundary &&
      code.startsWith("text", i) &&
      !/[A-Za-z0-9_$]/.test(code[i + 4] ?? "")
    ) {
      let j = i + 4;
      while (j < n && /\s/.test(code[j])) j++;
      if (j < n && code[j] === "(") {
        let k = j + 1;
        let pDepth = 1;
        while (k < n && pDepth > 0) {
          const c = code[k];
          const nx = k + 1 < n ? code[k + 1] : "";
          if (
            c === '"' ||
            c === "'" ||
            c === "`" ||
            (c === "/" && (nx === "/" || nx === "*"))
          ) {
            k = advanceStringOrComment(code, k);
            continue;
          }
          if (c === "(") pDepth++;
          else if (c === ")") {
            pDepth--;
            if (pDepth === 0) break;
          }
          k++;
        }
        if (pDepth === 0) {
          const innerArgs = code.slice(j + 1, k);
          const cleanInner = stripCommentsAndStrings(innerArgs);
          let usesBoundVar = false;
          for (const v of boundVars) {
            if (
              new RegExp(`\\b${v}\\s*\\.\\s*(?:x|y|w|h)\\b`).test(cleanInner)
            ) {
              usesBoundVar = true;
              break;
            }
          }
          if (usesBoundVar) {
            const strMatch = innerArgs.match(
              /^[^,]+,[^,]+,\s*(["'`])((?:\\.|(?!\1)[\s\S])*)\1/
            );
            if (strMatch) {
              const quote = strMatch[1];
              let unescaped = strMatch[2];
              if (quote === '"' || quote === "'") {
                unescaped = unescaped.replace(/\\(["'\\])/g, "$1");
              }
              const trimmedVal = unescaped.trim();
              if (trimmedVal) {
                matches.push({
                  start: i,
                  end: k + 1,
                  textValue: trimmedVal,
                });
              }
            }
          }
          i = k + 1;
          continue;
        }
      }
    }
    i++;
  }

  if (matches.length === 0) return code;

  let dropFirstHeader = false;
  if (
    matches.length >= 2 &&
    matches[0].textValue.length <= 35 &&
    /^(?:question\s*\d+|q\d+|virtual\s+machine|solution|answer)\b/i.test(
      matches[0].textValue
    ) &&
    !/^(?:description|benefit|drawback|advantage|disadvantage)\b/i.test(
      matches[0].textValue
    )
  ) {
    dropFirstHeader = true;
  }

  let out = code;
  for (let mIdx = matches.length - 1; mIdx >= 0; mIdx--) {
    const m = matches[mIdx];
    if (mIdx === 0 && dropFirstHeader) {
      continue;
    }
    const writeIdx = dropFirstHeader ? mIdx - 1 : mIdx;
    const dummyY = 350 + writeIdx * 40;
    const replacement = `write(300, ${dummyY}, ${JSON.stringify(m.textValue)})`;
    out = out.slice(0, m.start) + replacement + out.slice(m.end);
  }

  return out;
}

export function stripPaperAndBoundTextWhenRefused(code: string): string {
  const stripped = stripCommentsAndStrings(code);
  if (!/\b(?:write|check|cross)\s*\(/.test(stripped)) {
    return code;
  }
  if (!/\b(?:paper|receipt|clipping|note|window)\s*\(/.test(stripped)) {
    return code;
  }

  // Track variable names bound to `paper(...)`, e.g. `const a = paper(...);`
  const boundVars = new Set<string>();
  const varAssignRegex =
    /\b(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*paper\s*\(/g;
  for (const m of code.matchAll(varAssignRegex)) {
    if (m[1]) boundVars.add(m[1]);
  }

  let out = "";
  let i = 0;
  const n = code.length;

  while (i < n) {
    const ch = code[i];
    const next = i + 1 < n ? code[i + 1] : "";
    if (
      ch === '"' ||
      ch === "'" ||
      ch === "`" ||
      (ch === "/" && (next === "/" || next === "*"))
    ) {
      const nextIdx = advanceStringOrComment(code, i);
      out += code.slice(i, nextIdx);
      i = nextIdx;
      continue;
    }

    const prevBoundary = i === 0 || !/[A-Za-z0-9_$.]/.test(code[i - 1]);
    let fnName: string | null = null;
    if (prevBoundary) {
      for (const candidate of [
        "paper",
        "receipt",
        "clipping",
        "note",
        "window",
        "text",
        "rect",
        "line",
      ]) {
        if (
          code.startsWith(candidate, i) &&
          !/[A-Za-z0-9_$]/.test(code[i + candidate.length] ?? "")
        ) {
          fnName = candidate;
          break;
        }
      }
    }

    if (fnName) {
      let j = i + fnName.length;
      while (j < n && /\s/.test(code[j])) j++;
      if (j < n && code[j] === "(") {
        let k = j + 1;
        let pDepth = 1;
        while (k < n && pDepth > 0) {
          const c = code[k];
          const nx = k + 1 < n ? code[k + 1] : "";
          if (
            c === '"' ||
            c === "'" ||
            c === "`" ||
            (c === "/" && (nx === "/" || nx === "*"))
          ) {
            k = advanceStringOrComment(code, k);
            continue;
          }
          if (c === "(") pDepth++;
          else if (c === ")") {
            pDepth--;
            if (pDepth === 0) break;
          }
          k++;
        }
        if (pDepth === 0) {
          const callSlice = code.slice(i, k + 1);
          const isCardCall =
            fnName === "paper" ||
            fnName === "receipt" ||
            fnName === "clipping" ||
            fnName === "note" ||
            fnName === "window";
          let usesBoundVar = false;
          if (!isCardCall && boundVars.size > 0) {
            const cleanArgs = stripCommentsAndStrings(callSlice);
            for (const v of boundVars) {
              if (new RegExp(`\\b${v}\\s*\\.\\s*(?:x|y|w|h)\\b`).test(cleanArgs)) {
                usesBoundVar = true;
                break;
              }
            }
          }
          if (isCardCall || usesBoundVar) {
            out += "Object({ x: 0, y: 0, w: 0, h: 0 })";
            i = k + 1;
            continue;
          }
        }
      }
    }

    out += ch;
    i++;
  }

  return out
    .replace(
      /(?:const|let|var)\s+[A-Za-z_$][\w$]*\s*=\s*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?/g,
      ""
    )
    .replace(
      /(?<=;)[ \t]*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?[ \t]*(?:\/\/[^\r\n]*)?/g,
      ""
    )
    .replace(
      /(?:^|(?<=[\r\n]))[ \t]*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?[ \t]*(?:\/\/[^\r\n]*)?(?:\r?\n|$)/g,
      ""
    )
    .replace(
      /^[ \t]*Object\(\{\s*x:\s*0,\s*y:\s*0,\s*w:\s*0,\s*h:\s*0\s*\}\)[ \t]*;?[ \t]*/gm,
      ""
    )
    .trim();
}

export function sanitizeDrawingProgram(rawProgram: string): string {
  let code = rawProgram
    .replace(/\b__hostRaw\b/g, "undefined")
    .replace(/\b__host\b/g, "undefined")
    .replace(/\bObject\s*\.\s*prototype\b/g, "({})")
    .replace(/\bwhile\s*\(\s*(?:true|1)\s*\)/g, "if (false)")
    .replace(/\bfor\s*\(\s*;\s*;\s*\)/g, "if (false)");

  // 0. Strip any trailing truncated statement if an upstream LLM hit max_tokens mid-line
  const rawLines = code.split(/\r?\n/);
  while (rawLines.length > 1) {
    const lastLine = rawLines[rawLines.length - 1].trim();
    if (!lastLine) {
      rawLines.pop();
      continue;
    }
    const dq = (lastLine.match(/(?<!\\)"/g) || []).length;
    const sq = (lastLine.match(/(?<!\\)'/g) || []).length;
    const opens = (lastLine.match(/\(/g) || []).length;
    const closes = (lastLine.match(/\)/g) || []).length;
    if (dq % 2 !== 0 || sq % 2 !== 0 || opens > closes) {
      rawLines.pop();
      code = rawLines.join("\n");
    } else {
      break;
    }
  }

  // 1. Clean up empty/whitespace-only string literals `""` inside `receipt(x, y, title, [ ... ], ...)` calls
  // using a quote- and bracket-aware scanner so strings containing `]` (e.g. `"[0, pi/2]"`) are preserved verbatim.
  code = sanitizeReceiptEmptyItems(code);

  // 1b. Prevent stacking a `receipt()` or `clipping()` card on top of a `paper()` card in the same turn,
  // and merge multiple `paper()` cards in the same turn into a single card.
  code = stripRedundantCardsWhenPaperExists(code);
  code = mergeMultiplePaperCards(code);

  // 1c. Strip empty or whitespace-only write(x, y, ""[, opts]) calls
  code = code.replace(
    /[ \t]*\bwrite\s*\(\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?\s*,\s*(["'`])\s*\1(?:\s*,\s*\{[^{}]*\})?\s*\)\s*;?[ \t]*\r?\n?/g,
    ""
  );

  // 1d. Prevent `paper()` heading overflow (> 42 chars wraps onto 2-3 lines inside the header and crowds `a.y + 18`)
  code = code.replace(
    /(\bheading\s*:\s*)(["'])((?:\\.|(?!\2)[\s\S])*)\2/g,
    (full, prefix: string, quote: string, rawHeading: string) => {
      let unescaped = rawHeading;
      if (quote === '"' || quote === "'") {
        unescaped = rawHeading.replace(/\\(["'\\])/g, "$1");
      }
      const cleaned = unescaped.replace(/\s+/g, " ").trim();
      if (cleaned.length <= 42) return full;
      const slice = cleaned.slice(0, 42);
      const lastSpace = slice.lastIndexOf(" ");
      const cut = (lastSpace >= 18 ? slice.slice(0, lastSpace) : slice)
        .replace(/[,;:\-–—+(]+$/, "")
        .trim();
      return `${prefix}${JSON.stringify(cut)}`;
    }
  );

  // 1e. Auto-fit `paper()` height & `text(a.x, a.y + offset, ...)` vertical spacing when a worked solution
  // has many lines or offsets that would overflow the `paper()` card (`offset > h - 25`) or the 720px canvas (`y + offset > 670`).
  const paperDeclMatch = code.match(
    /\b(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*paper\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)/
  );
  if (paperDeclMatch) {
    const pVar = paperDeclMatch[1];
    const pX = parseFloat(paperDeclMatch[2]);
    const pY = parseFloat(paperDeclMatch[3]);
    const pW = parseFloat(paperDeclMatch[4]);
    const pH = parseFloat(paperDeclMatch[5]);
    const escapedVar = pVar.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const boundTextRegex = new RegExp(
      `\\btext\\s*\\(\\s*${escapedVar}\\s*\\.\\s*x(?:\\s*[+-]\\s*\\d+(?:\\.\\d+)?)?\\s*,\\s*${escapedVar}\\s*\\.\\s*y\\s*\\+\\s*(\\d+(?:\\.\\d+)?)\\s*,\\s*((["'\`])(?:\\\\.|(?!\\3)[\\s\\S])*\\3)(?:\\s*,\\s*\\{([^{}]*)\\})?\\s*\\)`,
      "g"
    );
    const textMatches = Array.from(code.matchAll(boundTextRegex));
    if (textMatches.length > 0) {
      const maxOff = Math.max(
        ...textMatches.map((m) => parseFloat(m[1]) || 0)
      );
      if (
        textMatches.length > 12 ||
        maxOff > pH - 25 ||
        pY + maxOff > 670
      ) {
        const newY = Math.min(pY, 80);
        const newH = Math.min(600, Math.max(pH, 580, 680 - newY));
        const newW = Math.max(pW, 440);
        const newX =
          pX + newW > 1268 ? Math.max(16, 1268 - newW) : pX;
        const count = textMatches.length;
        const lineStep = Math.max(
          15,
          Math.min(26, Math.floor((newH - 36) / Math.max(1, count)))
        );
        const fontSize =
          lineStep <= 16 ? 12 : lineStep <= 19 ? 13 : lineStep <= 22 ? 14 : 15;
        const maxLines = Math.floor((newH - 20) / lineStep);
        // If count still exceeds maxLines (> 38 lines), drop middle prose lines so the final answer lines at the end are ALWAYS preserved!
        const keepIndices = new Set<number>();
        if (count <= maxLines) {
          for (let k = 0; k < count; k++) keepIndices.add(k);
        } else {
          const headCount = Math.ceil(maxLines / 2);
          const tailCount = maxLines - headCount;
          for (let k = 0; k < headCount; k++) keepIndices.add(k);
          for (let k = count - tailCount; k < count; k++) keepIndices.add(k);
        }

        code = code.replace(
          new RegExp(
            `(\\b(?:const|let|var)\\s+${escapedVar}\\s*=\\s*paper\\s*\\()\\s*-?\\d+(?:\\.\\d+)?\\s*,\\s*-?\\d+(?:\\.\\d+)?\\s*,\\s*-?\\d+(?:\\.\\d+)?\\s*,\\s*-?\\d+(?:\\.\\d+)?`
          ),
          `$1${newX}, ${newY}, ${newW}, ${newH}`
        );

        let rawIdx = 0;
        let keptIdx = 0;
        code = code.replace(
          boundTextRegex,
          (_full, _offStr: string, strLiteral: string) => {
            const curRaw = rawIdx++;
            if (!keepIndices.has(curRaw)) return "";
            const idx = keptIdx++;
            const yOff = 18 + idx * lineStep;
            return `text(${pVar}.x, ${pVar}.y + ${yOff}, ${strLiteral}, { size: ${fontSize} })`;
          }
        );
      }
    }
  }

  // 2. 2D-aware safety guard against overlapping `frame(x, y, w, h, label, opts?)` calls:
  // Only convert a subsequent `frame()` into `point()` when BOTH its horizontal `[x, x+w]` AND vertical `[y, y+h]`
  // intervals overlap an earlier kept `frame()`. Side-by-side or non-overlapping frames are preserved intact.
  const keptFrameIntervals: Array<{
    x0: number;
    x1: number;
    y0: number;
    y1: number;
  }> = [];
  code = code.replace(
    /\bframe\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)(?:\s*,\s*((["'`])(?:\\.|(?!\6)[\s\S])*?\6|[^,)\s]+))?(?:\s*,\s*\{[^{}]*\})?\s*\)/g,
    (
      fullMatch,
      xStr: string,
      yStr: string,
      wStr: string,
      hStr: string,
      labelArg?: string
    ) => {
      const x = Math.round(parseFloat(xStr));
      const y = Math.round(parseFloat(yStr));
      const w = Math.max(0, Math.round(parseFloat(wStr)));
      const h = Math.max(0, Math.round(parseFloat(hStr)));
      const x0 = x;
      const x1 = x + w;
      const y0 = y;
      const y1 = y + h;

      const overlapsExisting = keptFrameIntervals.some(
        (prev) =>
          Math.min(x1, prev.x1) - Math.max(x0, prev.x0) > 0 &&
          Math.min(y1, prev.y1) - Math.max(y0, prev.y0) > 0
      );
      if (!overlapsExisting) {
        keptFrameIntervals.push({ x0, x1, y0, y1 });
        return fullMatch;
      }

      const ptY = Math.min(705, y + 20);
      if (
        labelArg &&
        labelArg.trim() &&
        labelArg.trim() !== "null" &&
        labelArg.trim() !== "undefined"
      ) {
        return `point(${x}, ${ptY}, ${labelArg.trim()})`;
      }
      return `point(${x}, ${ptY})`;
    }
  );

  return code;
}

function sanitizeGeneratedClickyJs(code: string): string {
  return sanitizeDrawingProgram(code);
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

  const sanitizedThread = stripBase64Screenshots(threadItems ?? []);

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
    JSON.stringify(sanitizedThread),
    now,
    now
  );
}

// Bounded in-memory cache of recent screenshots (`fileId -> dataUrl`, max 64 entries)
// Matches `BrainSession.cs` / `BrainClient.cs` `clicky_file` & `screenshot_file` protocol.
const MAX_SCREENSHOT_CACHE_ENTRIES = 64;
const screenshotCache = new Map<string, string>();

function setScreenshotCacheEntry(key: string, dataUrl: string): void {
  if (screenshotCache.has(key)) {
    screenshotCache.delete(key);
  }
  screenshotCache.set(key, dataUrl);
  while (screenshotCache.size > MAX_SCREENSHOT_CACHE_ENTRIES) {
    const oldestKey = screenshotCache.keys().next().value;
    if (oldestKey !== undefined) {
      screenshotCache.delete(oldestKey);
    } else {
      break;
    }
  }
}

function storeScreenshotInCache(
  screenshotBase64: string,
  mimeType: string = "image/jpeg"
): { fileId: string; dataUrl: string } {
  const dataUrl = screenshotBase64.startsWith("data:")
    ? screenshotBase64
    : `data:${mimeType};base64,${screenshotBase64}`;
  const rawBase64 = screenshotBase64.replace(/^data:image\/[^;]+;base64,/, "");
  const fileId =
    "sf_" +
    crypto.createHash("sha1").update(screenshotBase64).digest("hex").slice(0, 16);
  setScreenshotCacheEntry(fileId, dataUrl);

  // Also index by rawBase64 and dataUrl SHA-1 in case caller hashes either form
  const rawId =
    "sf_" + crypto.createHash("sha1").update(rawBase64).digest("hex").slice(0, 16);
  if (rawId !== fileId) {
    setScreenshotCacheEntry(rawId, dataUrl);
  }
  const urlId =
    "sf_" + crypto.createHash("sha1").update(dataUrl).digest("hex").slice(0, 16);
  if (urlId !== fileId && urlId !== rawId) {
    setScreenshotCacheEntry(urlId, dataUrl);
  }
  return { fileId, dataUrl };
}

interface ExtractedPromptInput {
  userText: string;
  imageDataUrls: string[];
  hasExistingDrawingOnScreen: boolean;
  drawingOnScreen: string;
  drawingCode: string;
  liveState: string;
  flowBrief: string;
  pageInfo: {
    kind?: string;
    reference?: string;
    text?: string;
    pageCount?: number;
  } | null;
  canvasSize: { w: number; h: number } | null;
  contextNotes: string;
  screenLayout: ScreenLayoutAnalysis | null;
  screenshotFileId: string | null;
  missingScreenshotFile: boolean;
}

export function extractLastUserTurn(body: Record<string, any>): ExtractedPromptInput {
  const texts: string[] = [];
  const imageDataUrls: string[] = [];
  const contextParts: string[] = [];
  let hasExistingDrawingOnScreen = false;
  let drawingOnScreen = "";
  let drawingCode = "";
  let liveState = "";
  let flowBrief = "";
  let pageInfo: ExtractedPromptInput["pageInfo"] = null;
  let canvasSize: { w: number; h: number } | null = null;
  let screenshotFileId: string | null = null;
  let missingScreenshotFile = false;

  const tryResolveScreenshotRef = (obj: any): boolean => {
    if (!obj || typeof obj !== "object") return false;
    const hasInlineImage =
      (typeof obj.image_url === "string" && obj.image_url.length > 0) ||
      (typeof obj.base64 === "string" && obj.base64.length > 0);
    if (hasInlineImage) return false;

    const candidateId =
      typeof obj.clicky_file === "string" && obj.clicky_file.trim()
        ? obj.clicky_file.trim()
        : typeof obj.screenshot_file === "string" && obj.screenshot_file.trim()
          ? obj.screenshot_file.trim()
          : typeof obj.file_id === "string" && obj.file_id.trim()
            ? obj.file_id.trim()
            : obj.type === "clicky_file" && typeof obj.id === "string" && obj.id.trim()
              ? obj.id.trim()
              : null;

    if (!candidateId) {
      if (obj.type === "clicky_file") {
        missingScreenshotFile = true;
      }
      return false;
    }

    const cached = screenshotCache.get(candidateId);
    if (cached) {
      imageDataUrls.push(cached);
      if (!screenshotFileId) screenshotFileId = candidateId;
      return true;
    } else {
      missingScreenshotFile = true;
      return false;
    }
  };

  // Check top-level screenshot_file / clicky_file reference if present
  tryResolveScreenshotRef(body);

  // Extract top-level `body.canvas` ({ w, h } or { width, height })
  if (body.canvas && typeof body.canvas === "object") {
    const cw = Number(body.canvas.w ?? body.canvas.width ?? 0);
    const ch = Number(body.canvas.h ?? body.canvas.height ?? 0);
    if (cw > 0 && ch > 0) {
      canvasSize = { w: cw, h: ch };
    }
  }

  // Extract top-level `drawing_on_screen` / `drawing_code` if provided outside `turn`
  if (
    typeof body.drawing_on_screen === "string" &&
    body.drawing_on_screen.trim()
  ) {
    drawingOnScreen = body.drawing_on_screen.trim();
    hasExistingDrawingOnScreen = true;
  }
  if (typeof body.drawing_code === "string" && body.drawing_code.trim()) {
    drawingCode = body.drawing_code.trim();
    hasExistingDrawingOnScreen = true;
  }

  // 1. Check `turn` object from CompanionSession.WriteTurnBody (`CompanionSession.cs:3026-3182`)
  if (body.turn && typeof body.turn === "object") {
    tryResolveScreenshotRef(body.turn);
    if (typeof body.turn.words === "string" && body.turn.words.trim()) {
      texts.push(body.turn.words.trim());
    } else if (typeof body.turn.fact === "string" && body.turn.fact.trim()) {
      texts.push(body.turn.fact.trim());
    }
    if (typeof body.turn.front_app === "string" && body.turn.front_app.trim()) {
      const bundleId =
        typeof body.turn.front_app_bundle_id === "string" &&
        body.turn.front_app_bundle_id.trim()
          ? ` (${body.turn.front_app_bundle_id.trim()})`
          : "";
      contextParts.push(`Front App: ${body.turn.front_app.trim()}${bundleId}`);
    }
    if (
      typeof body.turn.time_label === "string" &&
      body.turn.time_label.trim()
    ) {
      contextParts.push(`Local Time: ${body.turn.time_label.trim()}`);
    }
    if (
      typeof body.turn.live_state === "string" &&
      body.turn.live_state.trim()
    ) {
      liveState = body.turn.live_state.trim();
      contextParts.push(`Live State: ${liveState}`);
    }
    if (
      typeof body.turn.stable_note === "string" &&
      body.turn.stable_note.trim()
    ) {
      contextParts.push(`Stable Note: ${body.turn.stable_note.trim()}`);
    }
    if (body.turn.flow_brief != null) {
      const fbStr =
        typeof body.turn.flow_brief === "string"
          ? body.turn.flow_brief.trim()
          : JSON.stringify(body.turn.flow_brief);
      if (fbStr && fbStr !== "{}" && fbStr !== "null") {
        flowBrief = fbStr;
        contextParts.push(`Flow Brief: ${flowBrief}`);
      }
    }
    if (
      typeof body.turn.drawing_on_screen === "string" &&
      body.turn.drawing_on_screen.trim().length > 0
    ) {
      drawingOnScreen = body.turn.drawing_on_screen.trim();
      hasExistingDrawingOnScreen = true;
    }
    if (
      typeof body.turn.drawing_code === "string" &&
      body.turn.drawing_code.trim().length > 0
    ) {
      drawingCode = body.turn.drawing_code.trim();
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
      const pageKind =
        typeof body.turn.page.kind === "string"
          ? body.turn.page.kind.trim()
          : "";
      const pageRef =
        typeof body.turn.page.reference === "string" &&
        body.turn.page.reference.trim()
          ? body.turn.page.reference.trim()
          : typeof body.turn.page.title === "string" &&
              body.turn.page.title.trim()
            ? body.turn.page.title.trim()
            : typeof body.turn.page.url === "string" &&
                body.turn.page.url.trim()
              ? body.turn.page.url.trim()
              : "";
      const pageCount =
        typeof body.turn.page.page_count === "number" &&
        body.turn.page.page_count > 0
          ? body.turn.page.page_count
          : undefined;
      const pageText =
        typeof body.turn.page.text === "string"
          ? body.turn.page.text.slice(0, 2400)
          : "";
      pageInfo = {
        kind: pageKind || undefined,
        reference: pageRef || undefined,
        text: pageText || undefined,
        pageCount,
      };
      if (pageKind || pageRef || pageText) {
        const metaHeader = [
          pageKind ? `kind=${pageKind}` : "",
          pageRef ? `ref=${pageRef}` : "",
          pageCount ? `pages=${pageCount}` : "",
        ]
          .filter(Boolean)
          .join(", ");
        contextParts.push(
          `Active Document/Page${metaHeader ? ` (${metaHeader})` : ""}:\n${pageText}`.trim()
        );
      }
    }
    if (body.turn.screenshot && typeof body.turn.screenshot === "object") {
      if (!canvasSize) {
        const sw = Number(body.turn.screenshot.widthInPixels ?? 0);
        const sh = Number(body.turn.screenshot.heightInPixels ?? 0);
        if (sw > 0 && sh > 0) {
          canvasSize = { w: sw, h: sh };
        }
      }
      if (
        typeof body.turn.screenshot.base64 === "string" &&
        body.turn.screenshot.base64.length > 0
      ) {
        const mime = body.turn.screenshot.mimeType || "image/jpeg";
        const b64 = body.turn.screenshot.base64;
        const stored = storeScreenshotInCache(b64, mime);
        screenshotFileId = stored.fileId;
        imageDataUrls.push(stored.dataUrl);
      } else {
        tryResolveScreenshotRef(body.turn.screenshot);
      }
    }
    if (Array.isArray(body.turn.attached_images)) {
      for (const img of body.turn.attached_images) {
        if (img && typeof img.base64 === "string" && img.base64.length > 0) {
          const mime = img.mimeType || "image/jpeg";
          const stored = storeScreenshotInCache(img.base64, mime);
          if (!screenshotFileId) screenshotFileId = stored.fileId;
          imageDataUrls.push(stored.dataUrl);
        } else {
          tryResolveScreenshotRef(img);
        }
      }
    }
  }

  // In un-compacted multi-turn sessions, `turn.drawing_code` may be empty while the previous
  // turn's drawing code resides in the most recent `assistant` message of `body.thread`.
  if (Array.isArray(body.thread)) {
    for (let i = body.thread.length - 1; i >= 0; i--) {
      const item = body.thread[i];
      if (!item || typeof item !== "object" || item.role !== "assistant") {
        continue;
      }
      let assistantText = "";
      if (typeof item.content === "string") {
        assistantText = item.content;
      } else if (Array.isArray(item.content)) {
        assistantText = item.content
          .map((c: any) => (typeof c?.text === "string" ? c.text : ""))
          .filter(Boolean)
          .join("\n");
      }
      if (!assistantText.trim()) continue;
      const innerAssistantCode = assistantText
        .replace(/^```(?:js|javascript)?\s*/i, "")
        .replace(/```\s*$/i, "")
        .trim();
      if (
        /\b(?:paper|receipt|clipping|figure|scene3d|frame|ring|point|spot|underline|check|cross|write|text|arrow|box)\s*\(/.test(
          stripCommentsAndStrings(innerAssistantCode)
        )
      ) {
        hasExistingDrawingOnScreen = true;
        if (!drawingCode) {
          drawingCode = innerAssistantCode.slice(0, 1500);
        }
        break;
      }
    }
  }

  if (canvasSize) {
    contextParts.push(`Canvas Size: ${canvasSize.w}x${canvasSize.h}`);
  }
  if (drawingOnScreen) {
    contextParts.push(
      `Active Drawing on Screen (parts & bounding boxes currently visible):\n${drawingOnScreen}`
    );
  }
  if (drawingCode) {
    const truncatedDrawingCode =
      drawingCode.length > 1500
        ? `${drawingCode.slice(0, 1500)}\n// ... (truncated)`
        : drawingCode;
    contextParts.push(
      `Current Drawing Code on Screen:\n\`\`\`js\n${truncatedDrawingCode}\n\`\`\``
    );
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
          const stored = storeScreenshotInCache(item.image_url);
          if (!screenshotFileId) screenshotFileId = stored.fileId;
          imageDataUrls.push(stored.dataUrl);
        } else {
          tryResolveScreenshotRef(item);
        }
        if (Array.isArray(item.content)) {
          for (const block of item.content) {
            if (block && typeof block === "object") {
              if (typeof block.text === "string") {
                texts.push(block.text);
              }
              if (
                block.type === "input_image" &&
                typeof block.image_url === "string" &&
                block.image_url.length > 0
              ) {
                const stored = storeScreenshotInCache(block.image_url);
                if (!screenshotFileId) screenshotFileId = stored.fileId;
                imageDataUrls.push(stored.dataUrl);
              } else {
                tryResolveScreenshotRef(block);
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
      tryResolveScreenshotRef(item);
      if (item.type === "function_call_output") {
        const outStr =
          typeof item.output === "string"
            ? item.output
            : JSON.stringify(item.output ?? "");
        if (outStr.trim()) {
          contextParts.push(
            `Tool Output (${item.call_id || "tool"}): ${outStr.trim()}`
          );
        }
      } else if (typeof item.content === "string" && item.content.trim()) {
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
              const stored = storeScreenshotInCache(block.image_url);
              if (!screenshotFileId) screenshotFileId = stored.fileId;
              imageDataUrls.push(stored.dataUrl);
            } else {
              tryResolveScreenshotRef(block);
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
      if (!item || typeof item !== "object") continue;
      if (item.type === "function_call_output") {
        const outStr =
          typeof item.output === "string"
            ? item.output
            : JSON.stringify(item.output ?? "");
        if (outStr.trim()) {
          texts.push(
            `Continue after tool output (${item.call_id || "tool"}): ${outStr.trim()}`
          );
          break;
        }
      }
      if (item.role === "user") {
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
                typeof block.image_url === "string" &&
                block.image_url.length > 0
              ) {
                const stored = storeScreenshotInCache(block.image_url);
                if (!screenshotFileId) screenshotFileId = stored.fileId;
                imageDataUrls.push(stored.dataUrl);
              } else {
                tryResolveScreenshotRef(block);
              }
            }
          }
          if (texts.length > 0) break;
        }
      }
    }
  }

  // 3.5 If the current turn did not re-attach a screenshot (e.g. "Solve this on a page"),
  // recover the most recent screenshot (or clicky_file reference) from `body.thread` so layout & vision still work!
  if (imageDataUrls.length === 0 && Array.isArray(body.thread)) {
    for (let i = body.thread.length - 1; i >= 0; i--) {
      const item = body.thread[i];
      if (!item || typeof item !== "object") continue;
      tryResolveScreenshotRef(item);
      if (Array.isArray(item.content)) {
        for (const block of item.content) {
          if (block && typeof block === "object") {
            if (
              block.type === "input_image" &&
              typeof block.image_url === "string" &&
              block.image_url.length > 0
            ) {
              const stored = storeScreenshotInCache(block.image_url);
              if (!screenshotFileId) screenshotFileId = stored.fileId;
              imageDataUrls.push(stored.dataUrl);
            } else {
              tryResolveScreenshotRef(block);
            }
          }
        }
      }
      if (imageDataUrls.length > 0) break;
    }
  }

  const dedupedTexts = Array.from(
    new Set(texts.map((t) => t.trim()).filter(Boolean))
  );
  const dedupedImages = Array.from(new Set(imageDataUrls.filter(Boolean)));
  let screenLayout: ScreenLayoutAnalysis | null = null;
  if (dedupedImages.length > 0) {
    const layoutKey =
      screenshotFileId ||
      `${dedupedImages[0].length}_${dedupedImages[0].slice(-80)}`;
    if (screenLayoutCache.has(layoutKey)) {
      screenLayout = screenLayoutCache.get(layoutKey) ?? null;
    } else {
      screenLayout = analyzeScreenLayout(dedupedImages[0]);
      if (screenLayout) {
        const cachedOcr = getCachedWindowsOcr(dedupedImages[0]);
        if (cachedOcr) {
          enrichScreenLayoutWithOcr(screenLayout, cachedOcr);
        }
      }
      if (screenLayoutCache.size >= 24) {
        const firstKey = screenLayoutCache.keys().next().value;
        if (firstKey) screenLayoutCache.delete(firstKey);
      }
      screenLayoutCache.set(layoutKey, screenLayout);
    }
  }

  return {
    userText:
      dedupedTexts.join("\n").trim() || "Look at my screen and help me.",
    imageDataUrls: dedupedImages,
    hasExistingDrawingOnScreen,
    drawingOnScreen,
    drawingCode,
    liveState,
    flowBrief,
    pageInfo,
    canvasSize,
    contextNotes: contextParts.join("\n"),
    screenLayout,
    screenshotFileId,
    missingScreenshotFile: missingScreenshotFile && dedupedImages.length === 0,
  };
}

const extractPromptInput = extractLastUserTurn;

interface UpstreamLlmResult {
  codeText: string;
  modelUsed: string;
  providerUsed: string;
  inputTokens: number;
  outputTokens: number;
  toolCalls: Array<{
    call_id: string;
    name: string;
    arguments: string;
    async: boolean;
  }>;
}

const screenLayoutCache = new Map<string, ScreenLayoutAnalysis | null>();
const visionEyeCache = new Map<string, string>();
let groqQwenRateLimitedUntilMs = 0;
let groq120bRateLimitedUntilMs = 0;

async function runGroqVisionEye(
  groqApiKey: string,
  extracted: ExtractedPromptInput,
  skipGroqQwen = false
): Promise<string> {
  if (extracted.imageDataUrls.length === 0) return "";
  const cacheKey =
    extracted.screenshotFileId ||
    `${extracted.imageDataUrls[0].length}_${extracted.imageDataUrls[0].slice(-80)}`;
  const cachedReading = visionEyeCache.get(cacheKey);
  if (cachedReading) {
    return cachedReading;
  }
  const visionPrompt = `Read this 1280x720 screenshot (with top x=100..1200 and left y=100..700 pixel rulers).
Ignore Kursor overlay cards on the left or right ("5-Stage Pipeline Table", "Summary", "Worked Solution", "Question 7"), ignore page headers/publishers, and never repeat (x,y) on every line.
In under 220 words, list:
1. EVERY QUESTION ON THE PAGE (from top to bottom, including any question at the very bottom): Give ONE (x, y) coordinate per question and transcribe its full prompt text verbatim.
CRITICAL FOR MATH & EQUATIONS: Local OCR drops integral bounds, fractions, and radicals. Transcribe EVERY mathematical expression, integral with exact lower and upper limits in LaTeX (e.g. \\int_{0}^{2}, \\int_{1}^{5}), fraction, exponent, and exact variable letter (distinguish t vs r, 2 vs 5) 100% verbatim! When transcribing degraded scanned math PDFs (like MAR 25/P32/Q10), watch out for scan smudges near exponents in denominators (e.g. (1+x)(4+x^2) where a scan smudge near x^2 can look like 2x^2 — verify algebraic consistency with the numerator -7x^2+2x-6 and target answer form a\\pi - \\ln b!).
2. TABLE STRUCTURE (if any): Column header labels and exact row labels for Row 1..N.`;

  const rawImgUrl = extracted.imageDataUrls[0];
  const annotatedImgUrl =
    extracted.screenLayout?.annotatedImageDataUrl ||
    (typeof annotateScreenshotWithRuler === "function"
      ? annotateScreenshotWithRuler(rawImgUrl)
      : null) ||
    rawImgUrl;

  if (!skipGroqQwen && isUsableApiKey(groqApiKey)) {
    const maxTokList = [400, 350, 280, 220];
    for (let attempt = 0; attempt < maxTokList.length; attempt++) {
      const maxTok = maxTokList[attempt];
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
                      image_url: { url: rawImgUrl },
                    },
                    { type: "text", text: visionPrompt },
                  ],
                },
              ],
            }),
            signal: AbortSignal.timeout(8000),
          }
        );
        if (res.ok) {
          const data = (await res.json()) as any;
          const text = data?.choices?.[0]?.message?.content?.trim();
          if (text) {
            console.log(
              `[OneBrain/VisionEye] Extracted screen reading (${data?.usage?.completion_tokens || 0} tokens)`
            );
            visionEyeCache.set(cacheKey, text);
            return text;
          }
        } else if (
          res.status === 503 ||
          res.status === 502 ||
          res.status === 500 ||
          res.status === 504
        ) {
          const backoffMs = 500 * (attempt + 1);
          console.warn(
            `[OneBrain/VisionEye] Groq qwen/qwen3.8-27b HTTP ${res.status} (over capacity), backing off ${backoffMs}ms...`
          );
          await new Promise((r) => setTimeout(r, backoffMs));
          continue;
        } else if (res.status === 429) {
          const errBody = await res.text().catch(() => "");
          const waitMatch = errBody.match(
            /try again in\s+(?:(\d+(?:\.\d+)?)ms|(\d+(?:\.\d+)?)s)/i
          );
          const waitMs = waitMatch
            ? waitMatch[1]
              ? Math.ceil(parseFloat(waitMatch[1])) + 80
              : Math.ceil(parseFloat(waitMatch[2]) * 1000) + 80
            : 0;
          if (/input tokens per minute \(ITPM\)/i.test(errBody)) {
            if (waitMs > 0 && waitMs <= 4000) {
              await new Promise((r) => setTimeout(r, waitMs));
              continue;
            }
            break;
          }
          const otpmMatch = errBody.match(/Limit\s+(\d+),\s*Used\s+(\d+)/i);
          const avail = otpmMatch
            ? Number(otpmMatch[1]) - Number(otpmMatch[2]) - 15
            : 220;
          if (avail < 180) {
            if (waitMs > 0 && waitMs <= 4000) {
              await new Promise((r) => setTimeout(r, waitMs));
              continue;
            }
            break;
          }
          if (waitMs > 0 && waitMs <= 4000) {
            await new Promise((r) => setTimeout(r, waitMs));
          }
        }
      } catch {
        // Fall through
      }
    }
  }

  // Fallback VisionEye via Gemini 2.5 Flash if GEMINI_API_KEY is available
  const gemKey = process.env.GEMINI_API_KEY?.trim();
  if (isUsableApiKey(gemKey)) {
    try {
      const gemRes = await fetch(
        "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${gemKey}`,
          },
          body: JSON.stringify({
            model: "gemini-2.5-flash",
            temperature: 0.1,
            max_tokens: 400,
            messages: [
              {
                role: "user",
                content: [
                  {
                    type: "image_url",
                    image_url: { url: rawImgUrl },
                  },
                  { type: "text", text: visionPrompt },
                ],
              },
            ],
          }),
          signal: AbortSignal.timeout(5000),
        }
      );
      if (gemRes.ok) {
        const gemData = (await gemRes.json()) as any;
        const gemText = gemData?.choices?.[0]?.message?.content?.trim();
        if (gemText) {
          console.log(
            `[OneBrain/GeminiVisionEye] Extracted screen reading (${gemData?.usage?.completion_tokens || 0} tokens)`
          );
          visionEyeCache.set(cacheKey, gemText);
          return gemText;
        }
      }
    } catch {
      // Fall through
    }
  }

  // Fallback VisionEye via OpenRouter multimodal vision models if OPENROUTER_API_KEY is available
  const orKey = process.env.OPENROUTER_API_KEY?.trim();
  if (
    isUsableApiKey(orKey) &&
    (!cachedInvalidOpenRouterKey || cachedInvalidOpenRouterKey !== orKey)
  ) {
    try {
      const orRes = await fetch(
        "https://openrouter.ai/api/v1/chat/completions",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${orKey}`,
            "HTTP-Referer": "https://kursor.dev",
            "X-Title": "Kursor OneBrain VisionEye",
          },
          body: JSON.stringify({
            model: OPENROUTER_VISION_MODELS_TIER1[0],
            models: [...OPENROUTER_VISION_MODELS_TIER1],
            temperature: 0.1,
            max_tokens: 400,
            messages: [
              {
                role: "user",
                content: [
                  {
                    type: "image_url",
                    image_url: { url: annotatedImgUrl },
                  },
                  { type: "text", text: visionPrompt },
                ],
              },
            ],
          }),
          signal: AbortSignal.timeout(6500),
        }
      );
      if (orRes.status === 401 || orRes.status === 403) {
        cachedInvalidOpenRouterKey = orKey;
      } else if (orRes.ok) {
        const orData = (await orRes.json()) as any;
        const orText = orData?.choices?.[0]?.message?.content?.trim();
        if (orText) {
          console.log(
            `[OneBrain/OpenRouterVisionEye] Extracted screen reading via ${orData?.model || "openrouter"} (${orData?.usage?.completion_tokens || 0} tokens)`
          );
          visionEyeCache.set(cacheKey, orText);
          return orText;
        }
      }
    } catch {
      // Fall through
    }
  }
  return cachedReading || "";
}

export async function tryCallUpstreamLlm(
  systemPrompt: string,
  thread: any[],
  extracted: ExtractedPromptInput,
  requestedModel: string,
  tools?: any[],
  abortSignal?: AbortSignal
): Promise<UpstreamLlmResult | null> {
  const hasImage = extracted.imageDataUrls.length > 0;
  if (hasImage && extracted.screenLayout && !extracted.screenLayout.ocrLines) {
    const ocr = await runWindowsOcr(extracted.imageDataUrls[0]);
    if (ocr && extracted.screenLayout) {
      enrichScreenLayoutWithOcr(extracted.screenLayout, ocr);
    }
    extracted.screenLayout.ocrLines = extracted.screenLayout.ocrLines ?? [];
  }
  const hasToolsArray = Boolean(
    tools && Array.isArray(tools) && tools.length > 0
  );
  const promptAsksForExternalTool =
    /\b(search\s+(?:the\s+)?(?:web|internet|online|google|images?)|look\s+up\s+online|create\s+(?:a\s+)?routine|schedule\s+(?:a\s+)?routine|list\s+integrations|start\s+(?:an?\s+)?agent|background\s+agent)\b/i.test(
      extracted.userText
    );
  const needsTools = hasToolsArray && (!hasImage || promptAsksForExternalTool);
  let visionReading = "";
  let visionEyeAttempted = false;
  let qwenVisionItpmExhausted = Date.now() < groqQwenRateLimitedUntilMs;
  const is3dRequest =
    /\b(3d|three[-\s]?d|figure|model|visualize|engine|turbine|molecule|solar|anatomy|organ|gear|mesh|piston)\b/i.test(
      extracted.userText
    );
  const isPure3dRequest =
    is3dRequest &&
    !/\b(screen|page|worksheet|table|tick|cross)\b/i.test(extracted.userText);

  // Dynamically omit Section 7 (3D modeling instructions, ~700 tokens) on 2D turns
  const effectiveSystemPrompt = is3dRequest
    ? systemPrompt
    : systemPrompt.replace(
        /\n*## 7\. Studio 3D Procedural Modeling[\s\S]*?(?=\n## 8\. Performing Actions)/,
        ""
      );

  const calm = extracted.screenLayout?.calmCardSpot || {
    x: 16,
    y: 120,
    w: 300,
    h: 420,
  };
  const wordToNumMap: Record<string, string> = {
    one: "1",
    two: "2",
    three: "3",
    four: "4",
    five: "5",
    six: "6",
    seven: "7",
    eight: "8",
    nine: "9",
    ten: "10",
    eleven: "11",
    twelve: "12",
  };
  const extractQNumFromText = (text: string): string | null => {
    if (!text) return null;
    const normText = text
      .replace(/\b(question(?:\s+number)?|problem|number)\s+c\s*,?\s*x\b/gi, "$1 6")
      .replace(
        /\b(question(?:\s+number)?|problem)\s+by\s+(\d{1,2})(?:\s+and\s+[a-d]\b)?/gi,
        "$1 $2"
      );
    const m =
      normText.match(
        /\b(?:question(?:\s+number)?|q|problem|number)\s*#?\s*(\d{1,2})\b/i
      ) ||
      normText.match(
        /\b(?:question(?:\s+number)?|problem|number)\s+(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\b/i
      );
    if (!m) return null;
    return wordToNumMap[m[1].toLowerCase()] || m[1];
  };

  const ocrLinesForLayout = extracted.screenLayout?.ocrLines ?? [];
  const nonOrphanScreenSlots = (
    extracted.screenLayout?.answerLines ?? []
  ).filter((s) => !s.isScrolledOffTopOrphan);
  const examHeaderCodeRegex =
    /\b(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM|S\/P|\d{2}\/[A-Z0-9]{2,4}\/Q\d+)\b/i;
  const isOcrLineOnAnswerSlot = (l: { y: number; cy: number; text: string }) =>
    !examHeaderCodeRegex.test(l.text) &&
    (extracted.screenLayout?.answerLines ?? []).some(
      (al) => Math.abs(l.cy - al.y) <= 18
    );
  const visibleHeaderQNums = new Set<string>();
  for (const l of ocrLinesForLayout) {
    if (l.y < 20 || l.x >= 380 || isOcrLineOnAnswerSlot(l)) continue;
    const norm = l.text
      .replace(/^\s*[fIl|](\d)\b/, "1$1")
      .replace(/^\s*[Il|]\s*\./, "1.");
    const hm = norm.match(
      /^\s*(\d{1,2})\s*(?:[.)\]:-]\s*|\s+(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM|S\/P|\d{2}\/|\([a-zivx]+\)|\b(?:The|Let|Given|A|An|In|For|Find|Use|Show|Hence|Evaluate|Prove|Differentiate|Integrate|Express|Sketch|Describe|Complete|Comploto|Outline|State|Explain|Calculate|Give|Identify|Solve)\b))/i
    );
    if (hm) {
      visibleHeaderQNums.add(hm[1]);
    }
  }
  const visibleSlotOrTableQNums = new Set<string>();
  for (const s of nonOrphanScreenSlots) {
    if (s.questionNumber && s.questionNumber !== "prev") {
      visibleSlotOrTableQNums.add(s.questionNumber);
    }
  }
  for (const t of extracted.screenLayout?.tables ?? []) {
    if (t.questionNumber && t.questionNumber !== "prev") {
      visibleSlotOrTableQNums.add(t.questionNumber);
    }
  }
  const allVisibleScreenQNums = new Set<string>([
    ...visibleHeaderQNums,
    ...visibleSlotOrTableQNums,
  ]);

  let requestedQNum = extractQNumFromText(extracted.userText);
  const asksNextOrBelowQuestion =
    /\b(?:next\s+question|question\s+below|below\s+the\s+table|after\s+the\s+table|bottom\s+question|second\s+question|last\s+question)\b/i.test(
      extracted.userText
    );
  // Rule A: Only inherit `prevQ` from conversation history if `prevQ` is STILL present on the current screen!
  if (!requestedQNum && !asksNextOrBelowQuestion && Array.isArray(thread)) {
    for (let i = thread.length - 1; i >= 0; i--) {
      const item = thread[i];
      if (!item || typeof item !== "object" || item.role !== "user") continue;
      const msgText =
        typeof item.content === "string"
          ? item.content
          : Array.isArray(item.content)
            ? item.content.map((c: any) => c?.text || "").join("\n")
            : "";
      const prevQ = extractQNumFromText(msgText);
      if (prevQ) {
        if (
          allVisibleScreenQNums.size === 0 ||
          allVisibleScreenQNums.has(prevQ)
        ) {
          requestedQNum = prevQ;
        }
        break;
      }
    }
  }
  // Rule B: Auto-bind to the single active fillable question on screen when `requestedQNum` is still null.
  // Even if the next question's title is peeking at the very bottom of the page with 0 visible slots/tables,
  // `visibleSlotOrTableQNums.size === 1` uniquely identifies the single fillable question on screen.
  if (
    !requestedQNum &&
    !asksNextOrBelowQuestion &&
    visibleSlotOrTableQNums.size === 1
  ) {
    requestedQNum = [...visibleSlotOrTableQNums][0];
  } else if (
    !requestedQNum &&
    !asksNextOrBelowQuestion &&
    visibleSlotOrTableQNums.size === 0 &&
    visibleHeaderQNums.size === 1
  ) {
    requestedQNum = [...visibleHeaderQNums][0];
  }

  const targetQHeaderLine = requestedQNum
    ? (ocrLinesForLayout.find((l) => {
        if (l.y < 20 || l.x >= 380 || isOcrLineOnAnswerSlot(l)) return false;
        const norm = l.text
          .replace(/^\s*[fIl|](\d)\b/, "1$1")
          .replace(/^\s*[Il|]\s*\./, "1.");
        return (
          new RegExp(`^\\s*(?:0\\s*)?${requestedQNum}(?:\\.|\\s|$)`).test(
            norm
          ) ||
          (requestedQNum === "1" &&
            l.x < 360 &&
            /^\s*(?:1|I|II|l)\s+(?:\([a-zivx]+\)|The|Let|Given|A|An|In|For|Find|Use|Show|Hence|Evaluate|Prove|Differentiate|Integrate|Express|Sketch|Describe|Explain|State|Calculate|Solve|Complete|Outline|Give|Identify)\b/i.test(
              l.text
            ))
        );
      }) ?? null)
    : null;

  const hasDetectedTable = (extracted.screenLayout?.tables?.length ?? 0) > 0;
  const firstTable = extracted.screenLayout?.tables?.[0];

  // Detect Whisper STT phonetic confusion ("E-part" / "part e" for "a-part" / Part (a)) when there is no `(e)` on the page
  const mentionsEPart = /\b(?:e-?part|part\s*\(?e\)?)\b/i.test(
    extracted.userText
  );
  const pageHasSubPartE = ocrLinesForLayout.some((l) =>
    /^\s*\(e\)/i.test(l.text)
  );
  const pageHasSubPartAOrTable =
    hasDetectedTable ||
    ocrLinesForLayout.some((l) => /^\s*\(a\)/i.test(l.text));
  const isWhisperEPartForAPart =
    mentionsEPart && !pageHasSubPartE && pageHasSubPartAOrTable;

  const asksSubPart =
    isWhisperEPartForAPart ||
    /\b(?:part\s*\(?[a-ea-z1-4i]+\)?|(?:pi|pie)\s*[1234i]+|[a-e]-?part|first\s+part|second\s+part|both\s+parts)\b/i.test(
      extracted.userText
    );

  const explicitlyAsksAboutTable =
    (isWhisperEPartForAPart ||
      /\b(?:complete\s+the\s+table|in\s+the\s+table|fill\s+(?:in\s+)?the\s+table|table|row|col(?:umn)?|cell|grid|tick|boxes|pipeline|stage|cycle|wb|part\s*\(?a\)?|\ba-?part\b|first\s+part)\b/i.test(
        extracted.userText
      )) &&
    !asksNextOrBelowQuestion;

  const asksNewNumberedQuestion =
    !explicitlyAsksAboutTable &&
    !asksSubPart &&
    (/\b(?:question|q)\s*(?:number\s+|no\.?\s*|#)?(?:\d+|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\b/i.test(
      extracted.userText
    ) ||
      asksNextOrBelowQuestion);

  const hasMarksInsideTable = (src: string, tbl: typeof firstTable): boolean => {
    if (!src || !tbl) return false;
    const strippedSrc = stripCommentsAndStrings(src);
    const markMatches = strippedSrc.matchAll(
      /\b(?:write|check|cross)\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)/g
    );
    for (const m of markMatches) {
      const mx = parseFloat(m[1]);
      const my = parseFloat(m[2]);
      if (
        mx >= tbl.x - 40 &&
        mx <= tbl.x + tbl.w + 40 &&
        my >= tbl.y - 30 &&
        my <= tbl.y + tbl.h + 30
      ) {
        return true;
      }
    }
    return false;
  };

  const tableAlreadyFilledInHistory =
    hasDetectedTable &&
    firstTable !== undefined &&
    ((Array.isArray(thread) &&
      thread.some((item: any) => {
        if (item?.role !== "assistant") return false;
        const text =
          typeof item.content === "string"
            ? item.content
            : Array.isArray(item.content)
              ? item.content.map((c: any) => c?.text || "").join("\n")
              : "";
        return hasMarksInsideTable(text, firstTable);
      })) ||
      hasMarksInsideTable(extracted.contextNotes || "", firstTable));

  const tableOwnerQNum = firstTable?.questionNumber ?? null;
  const tableBelongsToRequestedQ = Boolean(
    requestedQNum &&
      firstTable &&
      ((tableOwnerQNum && tableOwnerQNum === requestedQNum) ||
        (!tableOwnerQNum &&
          targetQHeaderLine &&
          targetQHeaderLine.y < firstTable.y))
  );
  const tableClearlyBelongsToAnotherQ = Boolean(
    firstTable &&
      ((requestedQNum && tableOwnerQNum && tableOwnerQNum !== requestedQNum) ||
        (targetQHeaderLine &&
          targetQHeaderLine.y >= firstTable.y + firstTable.h - 10) ||
        asksNextOrBelowQuestion)
  );
  const skipTableGridForNewQuestion =
    asksNewNumberedQuestion &&
    !tableBelongsToRequestedQ &&
    (tableAlreadyFilledInHistory || tableClearlyBelongsToAnotherQ);

  const answerLines = (extracted.screenLayout?.answerLines ?? []).filter(
    (s) => !s.isScrolledOffTopOrphan
  );
  const userExplicitlyRefusesPageCard =
    /\b(?:not\s+on\s+(?:a\s+|an\s+|the\s+|another\s+)?(?:ink\s+)?(?:page|card|whiteboard)|no\s+need\s+to\s+(?:create|open|make|use)\s+(?:a\s+|an\s+|the\s+)?(?:ink\s+)?(?:page|card|whiteboard)|no\s+(?:ink\s+)?(?:page|card|whiteboard)|without\s+(?:a\s+|an\s+)?(?:ink\s+)?(?:page|card|whiteboard)|don'?t\s+(?:create|open|make|use)\s+(?:a\s+|an\s+|the\s+)?(?:ink\s+)?(?:page|card|whiteboard))\b/i.test(
      extracted.userText
    );
  const userExplicitlyRequestsPageCard =
    /\b(?:on a page|on another page|open a page|in a new page|on a card|on a whiteboard|separate page|show working|explain\s+(?:it\s+|to\s+me\s+|me\s+)?(?:step\s+by\s+step|in\s+detail|how\s+did\s+you|how\s+you)|step\s+by\s+step\s+in\s+detail|how\s+did\s+you\s+(?:solve|come\s+up\s+with|get))\b/i.test(
      extracted.userText
    ) && !userExplicitlyRefusesPageCard;
  const userReportsWrongAnswer =
    /\b(?:you(?:'re|\s+are)\s+wrong|not\s+correct|incorrect|wrong\s+answer|that(?:'s|\s+is)\s+wrong|mistake|recalculate|check\s+again|solve\s+it\s+properly)\b/i.test(
      extracted.userText
    );
  const hasSlotsForTargetQ =
    answerLines.length > 0 &&
    (requestedQNum
      ? answerLines.some(
          (s) =>
            String(s.questionNumber ?? "") === requestedQNum ||
            Boolean(
              !s.questionNumber &&
                (targetQHeaderLine
                  ? s.y > targetQHeaderLine.y &&
                    !ocrLinesForLayout.some(
                      (ol) =>
                        ol.y > targetQHeaderLine.y + 8 &&
                        ol.y < s.y &&
                        ol.x < 360 &&
                        !isOcrLineOnAnswerSlot(ol) &&
                        /^\s*\d{1,2}\s*(?:[.):-]|\s+(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM))\b/i.test(
                          ol.text
                        )
                    )
                  : !skipTableGridForNewQuestion &&
                    visibleHeaderQNums.size === 0 &&
                    visibleSlotOrTableQNums.size === 0 &&
                    (!firstTable || s.y > firstTable.y + firstTable.h))
            )
        )
      : skipTableGridForNewQuestion && firstTable
        ? answerLines.some((s) => s.y > firstTable.y + firstTable.h)
        : true);

  const shouldIsolateNewQuestionWithoutSlots =
    skipTableGridForNewQuestion ||
    (asksNewNumberedQuestion && !hasSlotsForTargetQ && answerLines.length > 0);

  const calmOnlyNote = `### [DETECTED SCREEN GEOMETRY (1280x720 Canvas)]\n- **Suggested Calm Region for \`paper()\` Whiteboard Card**: \`x=${calm.x}, y=${calm.y}, w=${calm.w}, h=${calm.h}\` (open a \`paper()\` card here to write the full solution to the requested question, and \`underline()\` the question heading on the page!)`;
  const effectiveLayoutNote = extracted.screenLayout?.layoutPromptNote
    ? shouldIsolateNewQuestionWithoutSlots
      ? stripDetectedTableGridFromNote(
          extracted.screenLayout.layoutPromptNote,
          calm,
          hasSlotsForTargetQ,
          requestedQNum
        )
      : extracted.screenLayout.layoutPromptNote
    : shouldIsolateNewQuestionWithoutSlots
      ? calmOnlyNote
      : "";

  const userClarificationSuffix = [
    requestedQNum && !extractQNumFromText(extracted.userText)
      ? `(Context: Continuing Question ${requestedQNum})`
      : "",
    isWhisperEPartForAPart
      ? `(Note: "E-part" is voice transcription for "a-part" / Part (a), the table in Question ${requestedQNum || tableOwnerQNum || "on screen"} — complete Part (a)'s table using check(cx, cy)!)`
      : "",
    userReportsWrongAnswer
      ? `(CRITICAL: The user reported your previous answer was mathematically incorrect. Re-read the exact original question and integral limits from [Screen Vision Analysis] carefully from scratch, verify whether limits in "show that I = \\int_a^b ..." are already in terms of the substituted variable, and write the complete corrected step-by-step solution inside a paper() card!)`
      : "",
  ]
    .filter(Boolean)
    .join(" ");
  const annotatedUserRequestText = userClarificationSuffix
    ? `${extracted.userText}\n${userClarificationSuffix}`
    : extracted.userText;

  const effectiveContextNotes = userReportsWrongAnswer
    ? extracted.contextNotes
        .replace(
          /\n*Current Drawing Code on Screen:\n```js[\s\S]*?```/g,
          ""
        )
        .trim()
    : extracted.contextNotes;

  const baseUserBlocks: string[] = [
    `[User Request]\n${annotatedUserRequestText}`,
  ];
  if (effectiveContextNotes && !shouldIsolateNewQuestionWithoutSlots) {
    baseUserBlocks.push(`[Turn Context]\n${effectiveContextNotes}`);
  }
  if (effectiveLayoutNote) {
    baseUserBlocks.push(effectiveLayoutNote);
  }
  const baseUserText = baseUserBlocks.join("\n\n");

  const compactUserBlocks: string[] = [
    `[User Request]\n${annotatedUserRequestText}`,
  ];
  if (effectiveContextNotes && !shouldIsolateNewQuestionWithoutSlots) {
    compactUserBlocks.push(
      `[Turn Context]\n${effectiveContextNotes.slice(0, 600)}`
    );
  }
  if (effectiveLayoutNote) {
    compactUserBlocks.push(
      compactLayoutPromptNoteForGroqVision(effectiveLayoutNote)
    );
  }
  const compactUserText = compactUserBlocks.join("\n\n");

  const messages: any[] = [{ role: "system", content: effectiveSystemPrompt }];

  // Keep recent turns of history, preserving full assistant drawing code (up to 2,500 chars)
  // including `say`, `paper`, `receipt`, `text`, `write`, `underline`, `point`, `frame`, and equations
  // so multi-turn conversations never lose memory of what was drawn in earlier turns.
  if (Array.isArray(thread)) {
    const recentThread = thread.slice(-10);
    for (let idx = 0; idx < recentThread.length; idx++) {
      const item = recentThread[idx];
      if (!item || typeof item !== "object") continue;

      if (item.type === "function_call") {
        const callName = String(item.name || "tool");
        const callArgs =
          typeof item.arguments === "string"
            ? item.arguments
            : JSON.stringify(item.arguments ?? {});
        messages.push({
          role: "assistant",
          content: `[Tool Call: ${callName}(${callArgs}) (call_id=${item.call_id || ""})]`,
        });
        continue;
      }

      if (item.type === "function_call_output") {
        const outStr =
          typeof item.output === "string"
            ? item.output
            : JSON.stringify(item.output ?? "");
        messages.push({
          role: "user",
          content: `[Tool Output (call_id=${item.call_id || ""})]: ${outStr}`,
        });
        continue;
      }

      if (item.role === "developer") {
        let devText = "";
        if (typeof item.content === "string") {
          devText = item.content;
        } else if (Array.isArray(item.content)) {
          devText = item.content
            .map((c: any) => (typeof c?.text === "string" ? c.text : ""))
            .filter(Boolean)
            .join("\n");
        }
        if (devText.trim()) {
          messages.push({
            role: "user",
            content: `[Developer Context]\n${devText.trim()}`,
          });
        }
        continue;
      }

      if (
        (!item.type || item.type === "message") &&
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
          // Avoid duplicating the current user turn if `thread` ends with the current user message
          if (
            idx === recentThread.length - 1 &&
            item.role === "user" &&
            textContent.trim() === extracted.userText.trim()
          ) {
            continue;
          }
          if (item.role === "assistant") {
            if (userReportsWrongAnswer) {
              messages.push({
                role: "assistant",
                content:
                  "```js\n// [Previous assistant answer rejected by user as incorrect — recalculate from scratch]\n```",
              });
              continue;
            }
            const innerCode = textContent
              .replace(/^```(?:js|javascript)?\s*/i, "")
              .replace(/```\s*$/i, "")
              .trim();
            const truncatedAssistantCode =
              innerCode.length > 2500
                ? `${innerCode.slice(0, 2500)}\n// ... (truncated)`
                : innerCode;
            messages.push({
              role: "assistant",
              content: `\`\`\`js\n${truncatedAssistantCode}\n\`\`\``,
            });
          } else {
            messages.push({ role: item.role, content: textContent });
          }
        }
      }
    }
  }

  // For vision-capable providers, pass the ruler-annotated screenshot(s) so the model can read
  // exact x = 100..1200 and y = 100..700 pixel ticks directly off the top and left edges!
  const multimodalUserContent: any[] = [{ type: "text", text: baseUserText }];
  const compactMultimodalUserContent: any[] = [
    { type: "text", text: compactUserText },
  ];
  if (hasImage) {
    for (let i = 0; i < Math.min(2, extracted.imageDataUrls.length); i++) {
      const rawImgUrl = extracted.imageDataUrls[i];
      const annotatedUrl =
        (i === 0 ? extracted.screenLayout?.annotatedImageDataUrl : null) ||
        (typeof annotateScreenshotWithRuler === "function"
          ? annotateScreenshotWithRuler(rawImgUrl)
          : null) ||
        rawImgUrl;
      multimodalUserContent.push({
        type: "image_url",
        image_url: { url: annotatedUrl },
      });
      if (i === 0) {
        compactMultimodalUserContent.push({
          type: "image_url",
          image_url: { url: annotatedUrl },
        });
      }
    }
  }

  const envDefaultOpenRouterModel =
    process.env.KURSOR_BRAIN_MODEL?.trim() ||
    process.env.ONEBRAIN_MODEL?.trim() ||
    process.env.OPENROUTER_DEFAULT_MODEL?.trim() ||
    "";

  const userForcedOpenRouterModel =
    requestedModel &&
    requestedModel.trim() &&
    requestedModel.trim() !== "anthropic/claude-sonnet-4.6" &&
    requestedModel.trim() !== envDefaultOpenRouterModel
      ? requestedModel.trim()
      : "";

  const explicitOpenRouterModel =
    userForcedOpenRouterModel || envDefaultOpenRouterModel;

  const openRouterProvider = {
    name: "openrouter",
    url: "https://openrouter.ai/api/v1/chat/completions",
    apiKey: process.env.OPENROUTER_API_KEY,
    model: userForcedOpenRouterModel || OPENROUTER_VISION_MODELS_TIER1[0],
    supportsVision: true,
    headers: {
      "HTTP-Referer": "https://kursor.dev",
      "X-Title": "Kursor OneBrain",
    },
  };

  const geminiProvider = {
    name: "gemini",
    url: "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
    apiKey: process.env.GEMINI_API_KEY,
    model: "gemini-2.5-flash",
    supportsVision: true,
  };

  const openAiProvider = {
    name: "openai",
    url: "https://api.openai.com/v1/chat/completions",
    apiKey: process.env.OPENAI_API_KEY,
    model: "gpt-4o",
    supportsVision: true,
  };

  const groqQwenVisionProvider = {
    name: "groq-qwen-vision",
    url: "https://api.groq.com/openai/v1/chat/completions",
    apiKey: process.env.GROQ_API_KEY,
    model: "qwen/qwen3.8-27b",
    supportsVision: true,
  };

  const groq120bProvider = {
    name: "groq-120b",
    url: "https://api.groq.com/openai/v1/chat/completions",
    apiKey: process.env.GROQ_API_KEY,
    model: "openai/gpt-oss-120b",
    supportsVision: false,
  };

  const groq8bProvider = {
    name: "groq-8b",
    url: "https://api.groq.com/openai/v1/chat/completions",
    apiKey: process.env.GROQ_API_KEY,
    model: "openai/gpt-oss-20b",
    supportsVision: false,
  };

  // Whenever `hasImage && !needsTools && !userForcedOpenRouterModel`, run `groq-120b` (`openai/gpt-oss-120b`) FIRST
  // paired with once-per-screenshot cached `runGroqVisionEye` (`qwen/qwen3.8-27b`), followed by `groq-8b`, `groq-qwen-vision`, and `openRouterProvider`.
  const providers: Array<{
    name: string;
    url: string;
    apiKey: string | undefined;
    model: string;
    supportsVision: boolean;
    headers?: Record<string, string>;
  }> = hasImage
    ? !needsTools && !userForcedOpenRouterModel
      ? [
          groq120bProvider,
          groq8bProvider,
          groqQwenVisionProvider,
          openRouterProvider,
          geminiProvider,
          openAiProvider,
        ]
      : [
          openRouterProvider,
          groqQwenVisionProvider,
          groq120bProvider,
          groq8bProvider,
          geminiProvider,
          openAiProvider,
        ]
    : [
        openRouterProvider,
        groq120bProvider,
        geminiProvider,
        openAiProvider,
        groq8bProvider,
      ];

  const compactHistory =
    shouldIsolateNewQuestionWithoutSlots || userReportsWrongAnswer
      ? []
      : messages.slice(1).slice(-3).map((m) => ({
          role: m.role,
          content:
            typeof m.content === "string" && m.content.length > 450
              ? `${m.content.slice(0, 450)}\n// ...`
              : m.content,
        }));

  for (const provider of providers) {
    if (!isUsableApiKey(provider.apiKey)) continue;
    if (abortSignal?.aborted) return null;
    if (
      provider.name === "groq-qwen-vision" &&
      Date.now() < groqQwenRateLimitedUntilMs
    ) {
      continue;
    }
    if (
      provider.name === "groq-120b" &&
      Date.now() < groq120bRateLimitedUntilMs
    ) {
      continue;
    }

    try {
      const trimmedApiKey = provider.apiKey.trim();
      if (
        provider.name === "openrouter" &&
        cachedInvalidOpenRouterKey &&
        cachedInvalidOpenRouterKey === trimmedApiKey
      ) {
        throw new Error(
          "OpenRouter key previously rejected with 401/403 (fast-skipped)"
        );
      }

      // Lazily run `runGroqVisionEye` ONLY if/when we use a text-only provider (`supportsVision === false`)
      // AND local Windows WinRT OCR is insufficient (e.g. math/calculus/diagram content, or the requested question is missing/clipped).
      let finalUserTextForProvider = baseUserText;
      let compactFinalUserTextForProvider = compactUserText;
      const ocrLines = extracted.screenLayout?.ocrLines ?? [];
      const docProseLines = ocrLines.filter(
        (l) => l.y >= 160 && l.text.trim().length >= 12
      );
      const mathOrDiagramRegex =
        /\b(?:integrate|integration|differentiate|derivative|integral|calculus|equation|formula|sin|cos|tan|sec|csc|cot|ln|log|dx|dy|lim|sqrt|matrix|vector|graph|plot|chart|diagram|figure|circuit|angle|triangle|polygon|prove\s+that|show\s+that|exact\s+value|constant\s+k)\b|[∫∑∏√π±≤≥≠≈∞]|2x\s*-\s*1|sin\s*x|cos\s*x|ln\s*x/i;
      const hasMangledEquationFragment =
        answerLines.length === 0 &&
        !hasDetectedTable &&
        (docProseLines.length < 4 ||
          ocrLines.some(
            (l) =>
              l.y >= 160 &&
              l.x > 450 &&
              l.text.trim().length > 0 &&
              l.text.trim().length < 12
          ));
      const hasMathOrDiagramContent =
        mathOrDiagramRegex.test(extracted.userText) ||
        ocrLines.some((l) => l.y >= 160 && mathOrDiagramRegex.test(l.text)) ||
        hasMangledEquationFragment;

      const promptVerbRegex =
        /\b(?:describe|explain|state|outline|identify|calculate|complete|show|give|define|why|how|what|find|use|write|declare|tick|match|draw|integrate|differentiate|evaluate|prove|express|sketch|solve|defined|let|given|substitution)\b/i;

      const topSlotsLackReadablePrompt =
        answerLines.length > 0 &&
        !ocrLines.some(
          (l) =>
            l.y >= 20 &&
            l.y < answerLines[0].y - 4 &&
            l.text.trim().length >= 18 &&
            promptVerbRegex.test(l.text)
        );

      const targetQMissingOrClipped = (() => {
        if (!requestedQNum) {
          return false;
        }
        if (ocrLines.length === 0) {
          return true;
        }
        const qHdr = ocrLines.find((l) => {
          if (l.y < 20 || l.x >= 380) return false;
          const norm = l.text
            .replace(/^\s*[fIl|](\d)\b/, "1$1")
            .replace(/^\s*[Il|]\s*\./, "1.");
          return (
            new RegExp(`^\\s*(?:0\\s*)?${requestedQNum}(?:\\.|\\s|$)`).test(
              norm
            ) ||
            (requestedQNum === "1" &&
              l.x < 360 &&
              /^\s*(?:1|I|II|l)\s+(?:\([a-zivx]+\)|The|Let|Given|A|An|In|For|Find|Use|Show|Hence|Evaluate|Prove|Differentiate|Integrate|Express|Sketch|Describe|Explain|State|Calculate|Solve|Complete|Outline|Give|Identify)\b/i.test(
                l.text
              ))
          );
        });
        if (!qHdr) return true;
        const linesBelowHeader = ocrLines.filter(
          (l) =>
            l.y > qHdr.bottomY - 2 &&
            l.x >= qHdr.x - 40 &&
            !/^\(\d+\)$|^©|^OUCLES/i.test(l.text.trim())
        );
        const hasVerb =
          promptVerbRegex.test(qHdr.text) ||
          linesBelowHeader.some((l) => promptVerbRegex.test(l.text));
        if (!hasVerb || linesBelowHeader.length === 0) {
          return true;
        }
        if (!hasSlotsForTargetQ && qHdr.bottomY >= 650) {
          const lowestBottomY = linesBelowHeader.reduce(
            (max, l) => Math.max(max, l.bottomY),
            qHdr.bottomY
          );
          if (lowestBottomY >= 710) {
            return true;
          }
        }
        return false;
      })();

      const hasSufficientLocalOcr =
        !hasMathOrDiagramContent &&
        !targetQMissingOrClipped &&
        !topSlotsLackReadablePrompt &&
        !userReportsWrongAnswer &&
        ((hasSlotsForTargetQ &&
          answerLines.length >= 2 &&
          docProseLines.length >= 3) ||
          (hasDetectedTable &&
            !skipTableGridForNewQuestion &&
            docProseLines.length >= 5) ||
          (!requestedQNum &&
            answerLines.length >= 2 &&
            docProseLines.length >= 4));

      if (
        !provider.supportsVision &&
        hasImage &&
        !isPure3dRequest &&
        !hasSufficientLocalOcr
      ) {
        if (!visionEyeAttempted) {
          visionEyeAttempted = true;
          visionReading = await runGroqVisionEye(
            process.env.GROQ_API_KEY || "",
            extracted,
            qwenVisionItpmExhausted
          );
        }
        if (visionReading) {
          let effectiveVisionReading = visionReading;
          if (shouldIsolateNewQuestionWithoutSlots && requestedQNum) {
            const noTableSection = visionReading.replace(
              /\n*\s*(?:\*\*)?2\.\s*(?:\*\*)?TABLE STRUCTURE[\s\S]*$/i,
              ""
            );
            const qLineMatch = noTableSection.match(
              new RegExp(
                `(?:^|\\n)\\s*(?:[-*•]\\s*)?(?:\\*\\*)?(?:Question\\s*${requestedQNum}\\b|Q${requestedQNum}\\b|${requestedQNum}\\s*[.:)—-])[\\s\\S]*$`,
                "i"
              )
            );
            if (qLineMatch && qLineMatch[0].trim()) {
              effectiveVisionReading = qLineMatch[0].trim();
            } else {
              effectiveVisionReading = noTableSection.trim();
            }
          }
          const visionHeader =
            hasMathOrDiagramContent || userReportsWrongAnswer
              ? `[Screen Vision Analysis (AUTHORITATIVE FOR ALL MATH EQUATIONS, INTEGRAL LIMITS & VARIABLES — Windows OCR above drops integral bounds like \\int_0^2 and \\int_1^5; use the exact LaTeX equations and limits below! Note: in "Use substitution x = g(t) to show that I = \\int_a^b ...", the limits a..b are ALREADY the transformed x-limits — do NOT substitute a..b into g(t) again!)]`
              : `[Screen Vision Analysis]`;
          finalUserTextForProvider = `${baseUserText}\n\n${visionHeader}\n${effectiveVisionReading}`;
          compactFinalUserTextForProvider = `${compactUserText}\n\n${visionHeader}\n${effectiveVisionReading}`;
          if (multimodalUserContent[0]?.type === "text") {
            multimodalUserContent[0] = {
              type: "text",
              text: finalUserTextForProvider,
            };
          }
          if (compactMultimodalUserContent[0]?.type === "text") {
            compactMultimodalUserContent[0] = {
              type: "text",
              text: compactFinalUserTextForProvider,
            };
          }
        }
      }

      // Use compact prompt + compactHistory + compact geometry across all Groq models (`groq-qwen-vision`, `groq-120b`, `groq-8b`)
      // and `openrouter` vision when `!needsTools && !is3dRequest` so total tokens stay ~1,700-3,200 (<< 7,000 ITPM / 8,000 TPM).
      const useCompactPrompt =
        provider.name === "groq-qwen-vision" ||
        (!needsTools &&
          !is3dRequest &&
          (provider.name === "groq-120b" ||
            provider.name === "groq-8b" ||
            (provider.name === "openrouter" && hasImage)));

      const effectiveCompactSystemPrompt = shouldIsolateNewQuestionWithoutSlots
        ? COMPACT_GROQ_VISION_SYSTEM_PROMPT.replace(
            /\n- Solving Worksheets & Tables Directly on Screen:[\s\S]*?(?=\n- Continuation:)/,
            ""
          )
        : COMPACT_GROQ_VISION_SYSTEM_PROMPT;

      let providerMessages: any[];
      if (useCompactPrompt) {
        providerMessages = [
          { role: "system", content: effectiveCompactSystemPrompt },
          ...compactHistory,
          {
            role: "user",
            content:
              provider.supportsVision && hasImage
                ? compactMultimodalUserContent
                : compactFinalUserTextForProvider,
          },
        ];
      } else {
        providerMessages = [
          ...messages,
          {
            role: "user",
            content:
              provider.supportsVision && hasImage
                ? multimodalUserContent
                : finalUserTextForProvider,
          },
        ];
      }

      const openRouterTier1Models = Array.from(
        new Set(
          userForcedOpenRouterModel
            ? [userForcedOpenRouterModel, ...OPENROUTER_VISION_MODELS_TIER1]
            : [...OPENROUTER_VISION_MODELS_TIER1]
        )
      ).slice(0, 3);

      const isMathOrSelfCorrectionTurn =
        hasMathOrDiagramContent || userReportsWrongAnswer;

      const payload: Record<string, any> = {
        model: provider.model,
        ...(provider.name === "openrouter"
          ? { models: openRouterTier1Models }
          : {}),
        messages: providerMessages,
        temperature:
          provider.name === "groq-qwen-vision"
            ? 0.2
            : isMathOrSelfCorrectionTurn
              ? 0.1
              : 0.25,
        max_tokens:
          provider.name === "openrouter"
            ? hasImage
              ? isMathOrSelfCorrectionTurn
                ? 1200
                : 900
              : 2048
            : provider.name === "groq-120b"
              ? is3dRequest
                ? 2048
                : isMathOrSelfCorrectionTurn
                  ? 1200
                  : 900
              : provider.name === "groq-8b"
                ? is3dRequest
                  ? 1536
                  : isMathOrSelfCorrectionTurn
                    ? 1050
                    : 900
                : provider.name === "groq-qwen-vision"
                  ? 450
                  : 2048,
        ...(provider.name === "openrouter"
          ? { reasoning: { effort: "low" } }
          : provider.name === "groq-120b"
            ? {
                reasoning_effort: isMathOrSelfCorrectionTurn ? "medium" : "low",
              }
            : provider.name === "groq-8b"
              ? {
                  reasoning_effort: "low",
                }
              : provider.name === "groq-qwen-vision"
                ? { reasoning_effort: "none" }
                : {}),
      };

      // Forward tools when calling tool-capable models (omit tools on free vision endpoints to avoid 404/token bloat)
      const isFreeVisionCall =
        hasImage &&
        provider.name === "openrouter" &&
        String(provider.model).endsWith(":free");
      if (
        Array.isArray(tools) &&
        tools.length > 0 &&
        provider.name !== "groq-qwen-vision" &&
        !isFreeVisionCall
      ) {
        payload.tools = tools;
      }

      const timeoutMs =
        provider.name === "openrouter" && hasImage
          ? 6500
          : provider.name === "groq-qwen-vision"
            ? 8000
            : 14000;
      const callStartMs = Date.now();
      const timeoutSig = AbortSignal.timeout(timeoutMs);
      const reqSignal = abortSignal
        ? AbortSignal.any([abortSignal, timeoutSig])
        : timeoutSig;

      const res = await fetch(provider.url, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${trimmedApiKey}`,
          ...(provider.headers ?? {}),
        },
        body: JSON.stringify(payload),
        signal: reqSignal,
      });

      let data: any = null;
      if (res.ok) {
        data = await res.json();
      } else {
        const errText = await res.text().catch(() => "");
        console.warn(
          `[OneBrain] Provider ${provider.name} HTTP ${res.status}: ${errText.slice(0, 200)}`
        );
        if (
          provider.name === "openrouter" &&
          (res.status === 401 || res.status === 403)
        ) {
          cachedInvalidOpenRouterKey = trimmedApiKey;
          continue;
        }
        if (provider.name === "groq-qwen-vision" && res.status === 429) {
          if (/input tokens per minute \(ITPM\)/i.test(errText)) {
            qwenVisionItpmExhausted = true;
            groqQwenRateLimitedUntilMs = Date.now() + 15000;
          } else if (/output tokens per minute \(OTPM\)/i.test(errText)) {
            const otpmMatch = errText.match(/Limit\s+(\d+),\s*Used\s+(\d+)/i);
            const availOut = otpmMatch
              ? Number(otpmMatch[1]) - Number(otpmMatch[2]) - 20
              : 320;
            if (availOut >= 260) {
              const retryMaxTokens = Math.min(550, availOut);
              const otpmRetryPayload = {
                ...payload,
                max_tokens: retryMaxTokens,
              };
              const otpmRetryTimeout = AbortSignal.timeout(7000);
              const otpmRetrySignal = abortSignal
                ? AbortSignal.any([abortSignal, otpmRetryTimeout])
                : otpmRetryTimeout;
              const otpmRetryRes = await fetch(provider.url, {
                method: "POST",
                headers: {
                  "Content-Type": "application/json",
                  Authorization: `Bearer ${trimmedApiKey}`,
                  ...(provider.headers ?? {}),
                },
                body: JSON.stringify(otpmRetryPayload),
                signal: otpmRetrySignal,
              });
              if (otpmRetryRes.ok) {
                data = await otpmRetryRes.json();
              } else {
                const otpmErrText = await otpmRetryRes.text().catch(() => "");
                console.warn(
                  `[OneBrain] Provider ${provider.name} (otpm-retry max_tokens=${retryMaxTokens}) HTTP ${otpmRetryRes.status}: ${otpmErrText.slice(0, 200)}`
                );
                qwenVisionItpmExhausted = true;
                groqQwenRateLimitedUntilMs = Date.now() + 15000;
              }
            } else {
              qwenVisionItpmExhausted = true;
              groqQwenRateLimitedUntilMs = Date.now() + 15000;
            }
          } else {
            qwenVisionItpmExhausted = true;
            groqQwenRateLimitedUntilMs = Date.now() + 15000;
          }
        } else if (
          provider.name === "groq-120b" &&
          res.status === 429 &&
          /tokens per day \(TPD\)/i.test(errText)
        ) {
          groq120bRateLimitedUntilMs = Date.now() + 60000;
        } else if (
          (provider.name === "groq-120b" || provider.name === "groq-8b") &&
          res.status === 429 &&
          /tokens per minute \(TPM\)/i.test(errText)
        ) {
          const tpmMatch = errText.match(/Limit\s+(\d+),\s*Used\s+(\d+)/i);
          const availTpm = tpmMatch
            ? Number(tpmMatch[1]) - Number(tpmMatch[2])
            : 0;
          const waitMsMatch = errText.match(
            /try again in\s+(?:(\d+(?:\.\d+)?)ms|(\d+(?:\.\d+)?)s)/i
          );
          const rawWaitMs = waitMsMatch
            ? waitMsMatch[1]
              ? Math.ceil(parseFloat(waitMsMatch[1])) + 60
              : Math.ceil(parseFloat(waitMsMatch[2]) * 1000) + 60
            : 400;
          const effectiveWaitMs = rawWaitMs + 150;
          if (effectiveWaitMs <= 7500 && !abortSignal?.aborted) {
            await new Promise((r) => setTimeout(r, effectiveWaitMs));
            const tpmRetryPayload = {
              ...payload,
              max_tokens: Math.min(
                Number(payload.max_tokens) || 900,
                isMathOrSelfCorrectionTurn ? 1150 : 750
              ),
              reasoning_effort: isMathOrSelfCorrectionTurn ? "medium" : "low",
            };
            const tpmRetryTimeout = AbortSignal.timeout(8000);
            const tpmRetrySignal = abortSignal
              ? AbortSignal.any([abortSignal, tpmRetryTimeout])
              : tpmRetryTimeout;
            const tpmRetryRes = await fetch(provider.url, {
              method: "POST",
              headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${trimmedApiKey}`,
                ...(provider.headers ?? {}),
              },
              body: JSON.stringify(tpmRetryPayload),
              signal: tpmRetrySignal,
            });
            if (tpmRetryRes.ok) {
              data = await tpmRetryRes.json();
            }
          }
        }
      }
      const firstChoice = data?.choices?.[0]?.message;
      const firstHasOutput =
        Boolean(firstChoice?.content && String(firstChoice.content).trim()) ||
        (Array.isArray(firstChoice?.tool_calls) &&
          firstChoice.tool_calls.length > 0);

      // Only retry OpenRouter Tier 2 if Tier 1 failed fast (< 2500ms, e.g. HTTP 400/404/429), never after a slow stall!
      const elapsedTier1Ms = Date.now() - callStartMs;
      if (
        (!res.ok || !firstHasOutput) &&
        provider.name === "openrouter" &&
        elapsedTier1Ms < 2500
      ) {
        const fallbackPayload: Record<string, any> = {
          model: OPENROUTER_VISION_MODELS_TIER2[0],
          models: [...OPENROUTER_VISION_MODELS_TIER2],
          messages: providerMessages,
          temperature: 0.25,
          max_tokens: hasImage ? 900 : 2048,
          reasoning: { effort: "low" },
          ...(Array.isArray(tools) && tools.length > 0 && !hasImage
            ? { tools }
            : {}),
        };
        const retryTimeout = AbortSignal.timeout(hasImage ? 6000 : 12000);
        const retrySignal = abortSignal
          ? AbortSignal.any([abortSignal, retryTimeout])
          : retryTimeout;
        const retryRes = await fetch(provider.url, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${provider.apiKey.trim()}`,
            ...(provider.headers ?? {}),
          },
          body: JSON.stringify(fallbackPayload),
          signal: retrySignal,
        });
        if (retryRes.ok) {
          data = await retryRes.json();
        } else {
          const retryErrText = await retryRes.text().catch(() => "");
          console.warn(
            `[OneBrain] Provider ${provider.name} (tier2) HTTP ${retryRes.status}: ${retryErrText.slice(0, 200)}`
          );
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
        const strippedForVisualCheck = stripCommentsAndStrings(codeText);
        const hasVisualCall =
          /\b(?:ring|frame|point|spot|target|hover|underline|connect|trace|check|cross|strike|num|sticky|clipping|receipt|legend|paper|write|text|figure|scene3d|circle|ellipse|rect|poly|line|curve|arrow|graph|timeline|highlight|handOff|button|openPage|copyText|typeText|erase|move|moveTo|recolor|restyle|startOver)\s*\(/.test(
            strippedForVisualCheck
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

      const shouldFillSlotsWithoutPaper =
        userExplicitlyRefusesPageCard ||
        (hasSlotsForTargetQ &&
          (extracted.screenLayout?.answerLines?.length ?? 0) > 0 &&
          !userExplicitlyRequestsPageCard);

      if (
        shouldFillSlotsWithoutPaper &&
        answerLines.length > 0
      ) {
        codeText = convertPaperTextToSlotWritesIfNeeded(
          codeText,
          answerLines.length
        );
      }

      // Always snap generated ClickyDraw JS coordinates (`check`, `cross`, `write`, `underline`, `spot`)
      // against OCR-enriched `extracted.screenLayout` when present!
      if (extracted.screenLayout) {
        codeText = snapProgramToScreenLayout(
          codeText,
          extracted.screenLayout,
          annotatedUserRequestText
        );
        const tbl = extracted.screenLayout?.tables?.[0];
        if (tbl && !skipTableGridForNewQuestion) {
          const dataRows = tbl.rows.filter((r) => !r.isHeader);
          const answerCols =
            tbl.cols.length >= 2 ? tbl.cols.slice(1) : tbl.cols;
          if (dataRows.length === 5 && answerCols.length >= 10) {
            codeText = codeText.replace(
              /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])I([1-6])\3/g,
              (
                full: string,
                _xStr: string,
                yStr: string,
                quote: string,
                instrNumStr: string
              ) => {
                const y = parseFloat(yStr);
                const k = parseInt(instrNumStr, 10);
                const rIdx = dataRows.findIndex((r) => Math.abs(r.cy - y) <= 18);
                if (rIdx < 0) return full;
                const targetCol = answerCols[rIdx + (k - 1)];
                if (!targetCol) return full;
                return `write(${targetCol.cx}, ${dataRows[rIdx].cy}, ${quote}I${k}${quote}`;
              }
            );
          }
        }
      }

      if (shouldFillSlotsWithoutPaper) {
        codeText = stripPaperAndBoundTextWhenRefused(codeText);
      }

      // Sanitize dangerous V8 patterns, empty receipt strings, and overlapping frames
      codeText = sanitizeDrawingProgram(codeText);

      // Guard against max_tokens truncation that stripped step(2) and left only step(1); underline(...) without the answer body
      const strippedAfterSanitize = stripCommentsAndStrings(codeText);
      const isTruncatedUnderlineOnly =
        (isMathOrSelfCorrectionTurn ||
          asksNewNumberedQuestion ||
          hasSlotsForTargetQ ||
          hasDetectedTable) &&
        /\bunderline\s*\(/.test(strippedAfterSanitize) &&
        !/\b(?:paper|write|check|cross|text|receipt|clipping|figure|scene3d|handOff)\s*\(/.test(
          strippedAfterSanitize
        );
      if (isTruncatedUnderlineOnly) {
        console.warn(
          `[OneBrain] Provider ${provider.name} truncated after underline() (finish_reason=${data?.choices?.[0]?.finish_reason}); falling through to next provider`
        );
        continue;
      }

      // Inject `startOver()` if the new program opens a brand-new `paper()`, `receipt()`, `card()`, `clipping()`, `figure()`, or `scene3d()`,
      // OR if it re-writes on the same worksheet `answerLines` (`|y - prevY| <= 18`) as a previous turn without `startOver()`.
      const strippedCode = stripCommentsAndStrings(codeText);
      const prevWriteYs: number[] = [];
      const collectWriteYs = (src: string) => {
        if (!src) return;
        const strippedSrc = stripCommentsAndStrings(src);
        for (const m of strippedSrc.matchAll(
          /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)/g
        )) {
          const wy = parseFloat(m[2]);
          if (!isNaN(wy)) prevWriteYs.push(wy);
        }
      };
      if (extracted.drawingCode) {
        collectWriteYs(extracted.drawingCode);
      }
      if (Array.isArray(thread)) {
        for (let i = thread.length - 1; i >= 0; i--) {
          const item = thread[i];
          if (item?.role === "assistant") {
            const t =
              typeof item.content === "string"
                ? item.content
                : Array.isArray(item.content)
                  ? item.content.map((c: any) => c?.text || "").join("\n")
                  : "";
            if (t) collectWriteYs(t);
            break;
          }
        }
      }
      const newWriteOverlapsPrevSlot =
        prevWriteYs.length > 0 &&
        Array.from(
          strippedCode.matchAll(
            /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)/g
          )
        ).some((m) => {
          const ny = parseFloat(m[2]);
          return (
            !isNaN(ny) && prevWriteYs.some((py) => Math.abs(ny - py) <= 18)
          );
        });

      if (
        (extracted.hasExistingDrawingOnScreen || prevWriteYs.length > 0) &&
        (/\b(?:paper|receipt|card|clipping|figure|scene3d)\s*\(/.test(
          strippedCode
        ) ||
          newWriteOverlapsPrevSlot) &&
        !/\bstartOver\s*\(/.test(strippedCode)
      ) {
        codeText = `startOver();\n${codeText}`;
      }

      codeText = "```js\n" + codeText.trim() + "\n```";

      console.log(
        `[OneBrain] Answered via ${provider.name} (${data?.model || provider.model}${!provider.supportsVision && visionReading ? " + vision-eye" : ""}) for prompt: "${extracted.userText.slice(0, 100)}"`
      );

      return {
        codeText,
        modelUsed: String(data?.model || provider.model),
        providerUsed: provider.name,
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

export async function handleBrainRequest(body: Record<string, any>): Promise<{
  provider: string;
  model: string;
  codeText: string;
  inputTokens: number;
  outputTokens: number;
  toolCalls: Array<{
    call_id: string;
    name: string;
    arguments: string;
    async: boolean;
  }>;
  elapsedMs: number;
}> {
  const t0 = Date.now();
  const userId = DEFAULT_USER_ID;
  const userMemory = getUserMemoryMarkdown(userId);
  const userLayer =
    typeof body.user_layer === "string" && body.user_layer.trim()
      ? body.user_layer
      : `<user_memory>\n${userMemory}\n</user_memory>`;
  const systemPrompt = `${loadSystemInstructions()}\n\n${userLayer}`;
  const incomingThread: any[] = Array.isArray(body.thread) ? body.thread : [];
  const extracted = extractPromptInput(body);
  if (
    !body.force_local_fallback &&
    extracted.imageDataUrls.length > 0 &&
    extracted.screenLayout &&
    !extracted.screenLayout.ocrLines
  ) {
    const ocr = await runWindowsOcr(extracted.imageDataUrls[0]);
    if (ocr && extracted.screenLayout) {
      enrichScreenLayoutWithOcr(extracted.screenLayout, ocr);
    }
    extracted.screenLayout.ocrLines = extracted.screenLayout.ocrLines ?? [];
  }

  const envDefaultModel =
    process.env.KURSOR_BRAIN_MODEL?.trim() ||
    process.env.ONEBRAIN_MODEL?.trim() ||
    process.env.OPENROUTER_DEFAULT_MODEL?.trim() ||
    "";
  const rawRequestedModel =
    (typeof body.brain_model === "string" && body.brain_model.trim()) ||
    (typeof body.model === "string" && body.model.trim()) ||
    "";
  const explicitRequestModel =
    rawRequestedModel &&
    rawRequestedModel !== "anthropic/claude-sonnet-4.6" &&
    rawRequestedModel !== envDefaultModel
      ? rawRequestedModel
      : "";
  const defaultModel =
    explicitRequestModel ||
    envDefaultModel ||
    rawRequestedModel ||
    "anthropic/claude-sonnet-4.6";

  const needsTools = Boolean(
    body.tools && Array.isArray(body.tools) && body.tools.length > 0
  );

  const upstream = body.force_local_fallback
    ? null
    : await tryCallUpstreamLlm(
        systemPrompt,
        incomingThread,
        extracted,
        explicitRequestModel,
        needsTools ? body.tools : undefined
      );

  if (upstream && !body.force_local_fallback) {
    return {
      provider: upstream.providerUsed,
      model: upstream.modelUsed,
      codeText: upstream.codeText,
      inputTokens: upstream.inputTokens,
      outputTokens: upstream.outputTokens,
      toolCalls: upstream.toolCalls,
      elapsedMs: Date.now() - t0,
    };
  }

  const fallback = buildLocalFallbackProgram(body, extracted);
  return {
    provider: "local-fallback",
    model: defaultModel,
    codeText: sanitizeGeneratedClickyJs(fallback.fullCode),
    inputTokens: 420,
    outputTokens: 85,
    toolCalls: fallback.toolCalls,
    elapsedMs: Date.now() - t0,
  };
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

  const envDefaultModel =
    process.env.KURSOR_BRAIN_MODEL?.trim() ||
    process.env.ONEBRAIN_MODEL?.trim() ||
    process.env.OPENROUTER_DEFAULT_MODEL?.trim() ||
    "";

  const rawRequestedModel =
    (typeof body.brain_model === "string" && body.brain_model.trim()) ||
    (typeof body.model === "string" && body.model.trim()) ||
    "";

  const explicitRequestModel =
    rawRequestedModel &&
    rawRequestedModel !== "anthropic/claude-sonnet-4.6" &&
    rawRequestedModel !== envDefaultModel
      ? rawRequestedModel
      : "";

  const defaultModel =
    explicitRequestModel ||
    envDefaultModel ||
    rawRequestedModel ||
    "anthropic/claude-sonnet-4.6";

  const needsTools = Boolean(
    body.tools && Array.isArray(body.tools) && body.tools.length > 0
  );

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
  if (
    !body.force_local_fallback &&
    extracted.imageDataUrls.length > 0 &&
    extracted.screenLayout &&
    !extracted.screenLayout.ocrLines
  ) {
    const ocr = await runWindowsOcr(extracted.imageDataUrls[0]);
    if (ocr && extracted.screenLayout) {
      enrichScreenLayoutWithOcr(extracted.screenLayout, ocr);
    }
    extracted.screenLayout.ocrLines = extracted.screenLayout.ocrLines ?? [];
  }
  if (extracted.missingScreenshotFile) {
    return c.json(
      {
        error: "screenshot_files_unavailable",
        message:
          "Referenced screenshot file is not in cache; retry with inline image.",
      },
      409
    );
  }
  const systemPrompt = `${loadSystemInstructions()}\n\n${userLayer}`;

  // Persist initial session & thread in SQLite
  ensureSessionAndThread(sessionId, userId, defaultModel, incomingThread);

  const encoder = new TextEncoder();
  const reqSignal = c.req.raw.signal;

  const stream = new ReadableStream<Uint8Array>({
    async start(controller) {
      let streamClosed = false;
      const onAbort = () => {
        streamClosed = true;
      };
      if (reqSignal) {
        if (reqSignal.aborted) {
          streamClosed = true;
        } else {
          reqSignal.addEventListener("abort", onAbort, { once: true });
        }
      }

      const sendData = (payload: Record<string, unknown>) => {
        if (streamClosed || reqSignal?.aborted) return;
        try {
          const line = `data: ${JSON.stringify(payload)}\n\n`;
          controller.enqueue(encoder.encode(line));
        } catch {
          streamClosed = true;
        }
      };

      // Note: BrainSseParser.TryDispatchText checks `json.StartsWith("{\"type\":\"text\""u8)`
      const sendTextChunk = (text: string) => {
        if (streamClosed || reqSignal?.aborted) return;
        try {
          const line = `data: {"type":"text","text":${JSON.stringify(text)}}\n\n`;
          controller.enqueue(encoder.encode(line));
        } catch {
          streamClosed = true;
        }
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

        // 3. Check `force_local_fallback` BEFORE calling live upstream LLMs
        const upstream = body.force_local_fallback
          ? null
          : await tryCallUpstreamLlm(
              systemPrompt,
              incomingThread,
              extracted,
              explicitRequestModel,
              needsTools ? body.tools : undefined,
              reqSignal
            );

        let finalCodeText = "";
        let modelUsed = defaultModel;
        let providerUsed = "local-fallback";
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
          providerUsed = upstream.providerUsed;
          inputTokens = upstream.inputTokens;
          outputTokens = upstream.outputTokens;
          toolCalls = upstream.toolCalls;

          // Stream in realistic chunks
          const chunkSize = 48;
          for (let i = 0; i < finalCodeText.length; i += chunkSize) {
            if (streamClosed || reqSignal?.aborted) break;
            sendTextChunk(finalCodeText.slice(i, i + chunkSize));
          }
        } else {
          const fallback = buildLocalFallbackProgram(body, extracted);
          finalCodeText = sanitizeGeneratedClickyJs(fallback.fullCode);
          toolCalls = fallback.toolCalls;
          computerCalls = fallback.computerCalls;

          for (const chunk of fallback.codeChunks) {
            if (streamClosed || reqSignal?.aborted) break;
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
        const donePayload: Record<string, unknown> = {
          type: "done",
          provider: providerUsed,
          model: modelUsed,
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
        };
        if (extracted.screenshotFileId) {
          donePayload.screenshot_file = extracted.screenshotFileId;
        }
        sendData(donePayload);
      } catch (err) {
        sendData({
          type: "error",
          code: "brain_turn_error",
          message:
            err instanceof Error ? err.message : "Unexpected OneBrain error",
        });
      } finally {
        reqSignal?.removeEventListener("abort", onAbort);
        if (!streamClosed) {
          streamClosed = true;
          try {
            controller.close();
          } catch {
            // Controller already closed
          }
        }
      }
    },
    cancel() {
      // Client disconnected mid-stream
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

  const userCtx = c.get("kursorUser" as never) as
    | { userId?: string }
    | undefined;
  const userId = userCtx?.userId || DEFAULT_USER_ID;

  const explicitSessionId =
    typeof body.session_id === "string" && body.session_id.trim()
      ? body.session_id.trim()
      : typeof body.thread_id === "string" && body.thread_id.trim()
        ? body.thread_id.trim()
        : null;
  const sessionId = explicitSessionId || "default-session";
  const thread: any[] = Array.isArray(body.thread)
    ? body.thread
    : Array.isArray(body.items)
      ? body.items
      : [];

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

  const compactedItems = [
    ...thread.slice(dropLeadingItems, keepLeadingItems),
    noteItem,
    ...thread.slice(keepFromIndex),
  ];

  if (explicitSessionId) {
    ensureSessionAndThread(
      explicitSessionId,
      userId,
      typeof body.model === "string" && body.model.trim()
        ? body.model.trim()
        : "compacted",
      compactedItems
    );
  }

  const compacted = {
    note_item: noteItem,
    keep_from_index: keepFromIndex,
    keep_leading_items: keepLeadingItems,
    drop_leading_items: dropLeadingItems,
    items: compactedItems,
  };

  return c.json({
    // Top-level properties read by `BrainClient.CompactAsync` in `BrainClient.cs`
    note_item: noteItem,
    keep_from_index: keepFromIndex,
    keep_leading_items: keepLeadingItems,
    drop_leading_items: dropLeadingItems,
    items: compactedItems,
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
  prewarmWinOcrDaemon();
  return c.json({ ok: true });
});

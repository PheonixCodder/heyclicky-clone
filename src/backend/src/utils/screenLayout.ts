import jpeg from "jpeg-js";

export interface TableCell {
  row: number;
  col: number;
  x: number;
  y: number;
  w: number;
  h: number;
  cx: number;
  cy: number;
}

export interface DetectedTable {
  x: number;
  y: number;
  w: number;
  h: number;
  xLines: number[];
  yLines: number[];
  rows: Array<{
    rowIndex: number;
    y0: number;
    y1: number;
    cy: number;
    isHeader: boolean;
  }>;
  cols: Array<{
    colIndex: number;
    x0: number;
    x1: number;
    cx: number;
    w: number;
  }>;
  cells: TableCell[];
}

export interface AnswerLine {
  x: number;
  y: number;
  w: number;
}

export interface ScreenLayoutAnalysis {
  width: number;
  height: number;
  tables: DetectedTable[];
  answerLines: AnswerLine[];
  calmCardSpot: { x: number; y: number; w: number; h: number; label: string };
  layoutPromptNote: string;
}

function clusterPositions(positions: number[], maxGap = 6): number[] {
  if (positions.length === 0) return [];
  const sorted = [...positions].sort((a, b) => a - b);
  const clusters: number[][] = [[sorted[0]]];
  for (let i = 1; i < sorted.length; i++) {
    const prev = clusters[clusters.length - 1];
    if (sorted[i] - prev[prev.length - 1] <= maxGap) {
      prev.push(sorted[i]);
    } else {
      clusters.push([sorted[i]]);
    }
  }
  return clusters.map((c) =>
    Math.round(c.reduce((s, v) => s + v, 0) / c.length)
  );
}

export function analyzeScreenLayout(
  imageDataUrl: string
): ScreenLayoutAnalysis | null {
  try {
    const base64 = imageDataUrl.replace(/^data:image\/\w+;base64,/, "");
    if (!base64) return null;
    const buf = Buffer.from(base64, "base64");
    const decoded = jpeg.decode(buf, { useTArray: true });
    const W = decoded.width;
    const H = decoded.height;
    const data = decoded.data;
    if (!W || !H || !data || data.length < W * H * 4) return null;

    const luma = (x: number, y: number): number => {
      const cx = Math.max(0, Math.min(W - 1, x));
      const cy = Math.max(0, Math.min(H - 1, y));
      const idx = (cy * W + cx) * 4;
      return 0.299 * data[idx] + 0.587 * data[idx + 1] + 0.114 * data[idx + 2];
    };

    // Detect red user highlight loop or Kursor ink so it can bridge an existing border line without starting fake lines
    const isRedHighlight = (x: number, y: number): boolean => {
      const cx = Math.max(0, Math.min(W - 1, x));
      const cy = Math.max(0, Math.min(H - 1, y));
      const idx = (cy * W + cx) * 4;
      const r = data[idx];
      const g = data[idx + 1];
      const b = data[idx + 2];
      return r > 175 && r - Math.min(g, b) > 45;
    };

    // Relative-contrast horizontal border pixel:
    // Checks that surrounding page background (y-4, y+4) is light (>160) and the line (min of y-1..y+1) drops by >30 luma.
    // Works across anti-aliased downscaled 1px lines and 1-2px skewed CamScanner PDF scans.
    const isHLinePixel = (x: number, y: number): boolean => {
      const bgAbove = isRedHighlight(x, y - 5) ? 235 : luma(x, y - 5);
      const bgBelow = isRedHighlight(x, y + 5) ? 235 : luma(x, y + 5);
      const bg = 0.5 * (bgAbove + bgBelow);
      if (bg < 160) return false;
      const lineMin = Math.min(
        luma(x, y - 2),
        luma(x, y - 1),
        luma(x, y),
        luma(x, y + 1),
        luma(x, y + 2)
      );
      return bg - lineMin > 28;
    };

    // Relative-contrast vertical border pixel:
    // Automatically ignores dark-grey PDF viewer margins because bg < 160 there.
    const isVLinePixel = (x: number, y: number): boolean => {
      const bgLeft = isRedHighlight(x - 5, y) ? 235 : luma(x - 5, y);
      const bgRight = isRedHighlight(x + 5, y) ? 235 : luma(x + 5, y);
      const bg = 0.5 * (bgLeft + bgRight);
      if (bg < 160) return false;
      const lineMin = Math.min(
        luma(x - 2, y),
        luma(x - 1, y),
        luma(x, y),
        luma(x + 1, y),
        luma(x + 2, y)
      );
      return bg - lineMin > 28;
    };

    // 1. Detect solid horizontal table border lines (skip top browser chrome y < 135 and bottom taskbar y > H - 20)
    const hCandidates: Array<{
      y: number;
      x0: number;
      x1: number;
      len: number;
    }> = [];

    for (let y = 135; y < H - 20; y++) {
      let runStart = -1;
      let gap = 0;
      let hits = 0;
      let transitions = 0;
      let prevHit = false;
      let bestX0 = 0;
      let bestX1 = 0;
      let bestLen = 0;
      let curY = y;

      for (let x = 30; x < W - 20; x++) {
        if (runStart >= 0 && x % 8 === 0) {
          if (luma(x, curY + 1) + 8 < luma(x, curY - 1) && curY - y < 6) {
            curY++;
          } else if (
            luma(x, curY - 1) + 8 < luma(x, curY + 1) &&
            y - curY < 6
          ) {
            curY--;
          }
        }
        const hit =
          isHLinePixel(x, curY) || (runStart >= 0 && isRedHighlight(x, curY));
        if (hit !== prevHit) {
          transitions++;
          prevHit = hit;
        }
        if (hit) {
          if (runStart < 0) {
            runStart = x;
            curY = y;
            transitions = 0;
          }
          hits++;
          gap = 0;
        } else if (runStart >= 0) {
          gap++;
          if (gap <= 18) continue;
          const endX = x - gap;
          const span = endX - runStart;
          // Solid border line has high continuous density (>= 0.78) and low transitions (rejects dotted lines)
          if (
            span > bestLen &&
            hits >= span * 0.78 &&
            transitions <= span * 0.16
          ) {
            bestLen = span;
            bestX0 = runStart;
            bestX1 = endX;
          }
          runStart = -1;
          curY = y;
          hits = 0;
          gap = 0;
          transitions = 0;
        }
      }
      if (runStart >= 0) {
        const endX = W - 20 - gap;
        const span = endX - runStart;
        if (
          span > bestLen &&
          hits >= span * 0.78 &&
          transitions <= span * 0.16
        ) {
          bestLen = span;
          bestX0 = runStart;
          bestX1 = endX;
        }
      }

      if (bestLen >= 170) {
        hCandidates.push({ y, x0: bestX0, x1: bestX1, len: bestLen });
      }
    }

    // Cluster horizontal candidates by Y (maxGap = 5, merging tilted segments by min(x0) and max(x1))
    const sortedH = [...hCandidates].sort((a, b) => a.y - b.y);
    const hClusters: Array<typeof hCandidates> = [];
    for (const h of sortedH) {
      const last = hClusters[hClusters.length - 1];
      const overlapsX =
        last &&
        Math.min(h.x1, Math.max(...last.map((v) => v.x1))) -
          Math.max(h.x0, Math.min(...last.map((v) => v.x0))) >
          100;
      if (last && h.y - last[last.length - 1].y <= 5 && overlapsX) {
        last.push(h);
      } else {
        hClusters.push([h]);
      }
    }

    const hLines = hClusters.map((c) => ({
      y: Math.round(c.reduce((s, v) => s + v.y, 0) / c.length),
      x0: Math.min(...c.map((v) => v.x0)),
      x1: Math.max(...c.map((v) => v.x1)),
    }));

    // Verify that two horizontal lines yA and yB are connected by at least 3 vertical column borders
    const countConnectingVLines = (
      yA: number,
      yB: number,
      x0: number,
      x1: number
    ): number => {
      if (yB - yA < 7 || yB - yA > 115) return 0;
      const vXs: number[] = [];
      for (let x = Math.max(20, x0 - 6); x <= Math.min(W - 20, x1 + 6); x++) {
        let hits = 0;
        let total = 0;
        for (let y = yA + 1; y <= yB - 1; y++) {
          total++;
          if (isVLinePixel(x, y) || isRedHighlight(x, y)) hits++;
        }
        if (total >= 4 && hits >= total * 0.62) {
          vXs.push(x);
        }
      }
      return clusterPositions(vXs, 6).filter(
        (x, idx, arr) => idx === 0 || x - arr[idx - 1] >= 18
      ).length;
    };

    const tables: DetectedTable[] = [];

    if (hLines.length >= 3) {
      let bestGroup: typeof hLines = [];
      for (let i = 0; i < hLines.length; i++) {
        const group = [hLines[i]];
        for (let j = i + 1; j < hLines.length; j++) {
          const prev = group[group.length - 1];
          const dy = hLines[j].y - prev.y;
          const sameSpan =
            Math.abs(hLines[j].x0 - group[0].x0) <= 32 &&
            Math.abs(hLines[j].x1 - group[0].x1) <= 32;
          if (
            sameSpan &&
            dy >= 7 &&
            dy <= 115 &&
            countConnectingVLines(
              prev.y,
              hLines[j].y,
              group[0].x0,
              group[0].x1
            ) >= 3
          ) {
            group.push(hLines[j]);
          }
        }
        if (group.length > bestGroup.length) {
          bestGroup = group;
        }
      }

      if (bestGroup.length >= 3) {
        const yMin = bestGroup[0].y;
        const yMax = bestGroup[bestGroup.length - 1].y;
        const tableH = yMax - yMin;
        const avgX0 = Math.round(
          bestGroup.reduce((s, g) => s + g.x0, 0) / bestGroup.length
        );
        const avgX1 = Math.round(
          bestGroup.reduce((s, g) => s + g.x1, 0) / bestGroup.length
        );

        const vCandidates: number[] = [];
        for (
          let x = Math.max(20, avgX0 - 20);
          x <= Math.min(W - 20, avgX1 + 25);
          x++
        ) {
          let hits = 0;
          let total = 0;
          for (let y = yMin + 2; y <= yMax - 2; y += 2) {
            total++;
            if (isVLinePixel(x, y) || isRedHighlight(x, y)) hits++;
          }
          if (total > 6 && hits >= total * 0.58) {
            vCandidates.push(x);
          }
        }

        const xLines = clusterPositions(vCandidates, 6).filter(
          (x, idx, arr) => idx === 0 || x - arr[idx - 1] >= 20
        );

        if (xLines.length >= 3 && tableH >= 40) {
          const xMin = xLines[0];
          const xMax = xLines[xLines.length - 1];
          const numRawRows = bestGroup.length - 1;
          const firstRowH = bestGroup[1].y - bestGroup[0].y;
          // If the table is clipped near the top of the viewport (yMin < 175) or has <= 3 rows where the top row is tall,
          // the header row was scrolled off-screen so all visible rows are data/statement rows.
          const hasVisibleHeader =
            yMin >= 175 &&
            (numRawRows >= 4 || (numRawRows >= 2 && firstRowH <= 30));

          const rows = [];
          for (let r = 0; r < numRawRows; r++) {
            const y0 = bestGroup[r].y;
            const y1 = bestGroup[r + 1].y;
            rows.push({
              rowIndex: r,
              y0,
              y1,
              cy: Math.round((y0 + y1) / 2),
              isHeader: hasVisibleHeader && r === 0,
            });
          }
          const cols = [];
          for (let c = 0; c < xLines.length - 1; c++) {
            const x0 = xLines[c];
            const x1 = xLines[c + 1];
            cols.push({
              colIndex: c,
              x0,
              x1,
              cx: Math.round((x0 + x1) / 2),
              w: x1 - x0,
            });
          }
          const cells: TableCell[] = [];
          for (const row of rows) {
            for (const col of cols) {
              cells.push({
                row: row.rowIndex,
                col: col.colIndex,
                x: col.x0,
                y: row.y0,
                w: col.w,
                h: row.y1 - row.y0,
                cx: col.cx,
                cy: row.cy,
              });
            }
          }
          tables.push({
            x: xMin,
            y: yMin,
            w: xMax - xMin,
            h: tableH,
            xLines,
            yLines: bestGroup.map((g) => g.y),
            rows,
            cols,
            cells,
          });
        }
      }
    }

    // 2. Detect dotted/blank worksheet answer lines (skipping any detected table)
    // Uses a ±2px vertical band for the thin line itself, and verifies that y±4, y±8, and y-14 are ALL white paper
    // so ordinary lines of printed text (which are 10-16px tall and dark at y±4) are NEVER mistaken for blank lines!
    const minLumaBand = (x: number, y: number, halfBand: number): number => {
      let m = 255;
      for (let dy = -halfBand; dy <= halfBand; dy++) {
        const lv = luma(x, y + dy);
        if (lv < m) m = lv;
      }
      return m;
    };
    const isLightPaper = (x: number, y: number): boolean => luma(x, y) > 188;

    const answerLines: AnswerLine[] = [];
    for (let y = 180; y < H - 16; y++) {
      if (tables.some((t) => y >= t.y - 12 && y <= t.y + t.h + 12)) continue;

      // Scan for a continuous periodic chain of small dots (dotW in 1..8px, gapW in 1..11px)
      // Printed text lines have word spaces (>11px) and wide glyph strokes (>8px) so they never form a 30+ dot chain!
      let curStart = -1;
      let curEnd = -1;
      let dotCount = 0;
      let runLen = 0;
      let inDot = false;

      let bestStart = -1;
      let bestEnd = -1;
      let bestDots = 0;

      for (let x = 120; x < W - 80; x++) {
        const d =
          minLumaBand(x, y, 1) < 162 &&
          isLightPaper(x, y - 4) &&
          isLightPaper(x, y - 8) &&
          isLightPaper(x, y + 4) &&
          isLightPaper(x, y + 8);
        if (d === inDot) {
          runLen++;
          // If a dark blob is too wide (>8px) or a white gap is too wide (>12px), the periodic dot chain breaks
          if ((inDot && runLen > 8) || (!inDot && runLen > 12)) {
            if (dotCount > bestDots) {
              bestDots = dotCount;
              bestStart = curStart;
              bestEnd = curEnd;
            }
            curStart = -1;
            dotCount = 0;
          }
        } else {
          if (inDot) {
            // Just finished a valid dot (1..8px wide)
            if (curStart < 0) curStart = x - runLen;
            curEnd = x;
            dotCount++;
          }
          inDot = d;
          runLen = 1;
        }
      }
      if (dotCount > bestDots) {
        bestDots = dotCount;
        bestStart = curStart;
        bestEnd = curEnd;
      }

      const span = bestEnd - bestStart;
      if (bestDots >= 50 && span >= W * 0.35) {
        if (
          answerLines.length === 0 ||
          y - answerLines[answerLines.length - 1].y >= 20
        ) {
          answerLines.push({
            x: bestStart + 6,
            y,
            w: span - 12,
          });
        } else {
          const prev = answerLines[answerLines.length - 1];
          if (span - 12 > prev.w) {
            prev.x = bestStart + 6;
            prev.y = y;
            prev.w = span - 12;
          }
        }
      }
    }

    // 3. Find the calmest spot for an explanation card (paper / clipping / receipt / figure)
    // Never place a card near y > 480 where multi-item cards overflow the 720px canvas height!
    const candidateSpots = [
      { x: 860, y: 150, w: 390, h: 420, label: "right margin" },
      { x: 20, y: 150, w: 290, h: 420, label: "left margin" },
      { x: 760, y: 150, w: 480, h: 420, label: "right half" },
    ];

    let bestSpot = candidateSpots[0];
    let bestScore = Infinity;
    for (const spot of candidateSpots) {
      let overlapPenalty = 0;
      for (const t of tables) {
        const ox = Math.max(
          0,
          Math.min(spot.x + spot.w, t.x + t.w) - Math.max(spot.x, t.x)
        );
        const oy = Math.max(
          0,
          Math.min(spot.y + spot.h, t.y + t.h) - Math.max(spot.y, t.y)
        );
        overlapPenalty += ox * oy * 10;
      }
      let edgeSum = 0;
      let count = 0;
      for (let y = spot.y; y < Math.min(H - 2, spot.y + spot.h); y += 8) {
        for (let x = spot.x; x < Math.min(W - 2, spot.x + spot.w); x += 8) {
          const i1 = (y * W + x) * 4;
          const i2 = (y * W + (x + 2)) * 4;
          edgeSum += Math.abs(data[i1] - data[i2]);
          count++;
        }
      }
      const busy = (count > 0 ? edgeSum / count : 0) + overlapPenalty;
      if (busy < bestScore) {
        bestScore = busy;
        bestSpot = spot;
      }
    }

    // 4. Build concise prompt note with exact pixel coordinates
    const noteLines: string[] = [
      `## Measured Screen Geometry (${W}x${H} exact canvas pixels)`,
    ];

    if (tables.length > 0) {
      for (let ti = 0; ti < tables.length; ti++) {
        const t = tables[ti];
        noteLines.push(
          `- **Detected Table ${ti + 1}**: frame box \`[x=${t.x}, y=${t.y}, w=${t.w}, h=${t.h}]\` (${t.rows.length} rows × ${t.cols.length} columns)`
        );
        noteLines.push(
          `  - Column X centers: ${t.cols
            .map(
              (c) =>
                `Col ${c.colIndex + 1} (x=${c.x0}..${c.x1}, **cx=${c.cx}**)`
            )
            .join(" | ")}`
        );
        let stmtCounter = 0;
        for (const r of t.rows) {
          const role = r.isHeader
            ? "Header Row"
            : `Statement Row ${++stmtCounter}`;
          const colCenters = t.cols
            .map((c) => `Col ${c.colIndex + 1}: **(${c.cx}, ${r.cy})**`)
            .join(", ");
          noteLines.push(
            `  - **${role}** (y=${r.y0}..${r.y1}, **cy=${r.cy}**): ${colCenters}`
          );
        }
        noteLines.push(
          `  - CRITICAL FOR TABLE TICKS/CROSSES: Place \`check(cx, cy)\` or \`cross(cx, cy)\` using the EXACT \`(cx, cy)\` coordinates listed above for each Statement Row (e.g. Col 2 \`cx=${t.cols[1]?.cx ?? t.cols[0].cx}\` or Col 3 \`cx=${t.cols[2]?.cx ?? t.cols[t.cols.length - 1].cx}\`, and \`cy\` for Statement Row 1..${stmtCounter}). NEVER use \`point()\` inside a table, and NEVER add a floating \`receipt()\` / \`clipping()\` window when solving a worksheet on screen!`
        );
      }
    }

    if (answerLines.length > 0) {
      const primaryTable = tables[0];
      noteLines.push(
        `- **Detected Blank Worksheet Answer Lines** (${answerLines.length} dotted lines — write directly on them using \`write(x, y, "concise point", { size: 17 })\`, placing ONE concise clause per dotted line at its exact \`x\` and \`y\`):`
      );
      for (let i = 0; i < Math.min(12, answerLines.length); i++) {
        const al = answerLines[i];
        const positionTag = primaryTable
          ? al.y < primaryTable.y
            ? " (ABOVE table — belongs to earlier question above the table)"
            : " (BELOW table — belongs to follow-up question below the table)"
          : "";
        noteLines.push(
          `  - Line ${i + 1}: \`x=${al.x}, y=${al.y - 4}, w=${al.w}\`${positionTag}`
        );
      }
    } else if (tables.length === 0) {
      noteLines.push(
        `- **No Blank Worksheet Answer Lines or Tick-Tables Detected on Screen**: Do NOT call \`write()\` directly over the printed page text! Instead, open a clean draggable Page/Card in the calm space (\`receipt(${bestSpot.x}, ${bestSpot.y}, "Solution", ["Step 1: ...", "Step 2: ...", "Answer: ..."], { w: ${Math.min(410, bestSpot.w)} })\` or \`paper(${bestSpot.x}, ${bestSpot.y}, ${Math.min(440, bestSpot.w)}, ${bestSpot.h}, { title: "Solution", heading: "...", board: "plain" })\`) to show your step-by-step working clearly!`
      );
    }

    noteLines.push(
      `- **Best Calm Space for Page/Card (\`receipt\`, \`paper\`, \`clipping\`)**: \`x=${bestSpot.x}, y=${bestSpot.y}, w=${bestSpot.w}, h=${bestSpot.h}\` (${bestSpot.label})`
    );

    return {
      width: W,
      height: H,
      tables,
      answerLines,
      calmCardSpot: bestSpot,
      layoutPromptNote: noteLines.join("\n"),
    };
  } catch {
    return null;
  }
}

function splitTextForAnswerLines(text: string, maxCharsPerLine: number): string[] {
  const cleaned = text.replace(/\s+/g, " ").trim();
  if (cleaned.length <= maxCharsPerLine) return [cleaned];
  const words = cleaned.split(" ");
  const chunks: string[] = [];
  let cur = "";
  for (const w of words) {
    const candidate = cur ? `${cur} ${w}` : w;
    if (cur && candidate.length > maxCharsPerLine) {
      chunks.push(cur);
      cur = w;
    } else {
      cur = candidate;
    }
  }
  if (cur) chunks.push(cur);
  return chunks;
}

/**
 * Post-processes generated ClickyDraw JS code so that:
 * 1) If the user asked to solve on a page/card OR there are no blank lines/tables on screen (e.g. a dense math/exam paper),
 *    any loose `write()` calls scribbled over the document are automatically converted into a clean draggable `receipt()` page in the calm margin!
 * 2) When solving a table with `check()`/`cross()` (and user did NOT ask for a page), redundant `receipt()`/`clipping()` summary cards are stripped.
 * 3) Every `check(x, y)` and `cross(x, y)` call snaps to the exact mathematical pixel center `(cx, cy)` of its table cell.
 * 4) Every `write(x, y, text)` call on a worksheet with real dotted lines snaps to `al.x` and `al.y - 4` and wraps across consecutive dotted lines.
 */
export function snapProgramToScreenLayout(
  code: string,
  layout: ScreenLayoutAnalysis | null,
  userText: string = ""
): string {
  let processed = code;

  const userWantsPageCard =
    /\b(on a page|on another page|open a page|on a card|on a whiteboard|separate page|show working|step by step)\b/i.test(
      userText
    ) && !/\bnot on (?:a |another )?page\b/i.test(userText);
  const userForbidsPageCard = /\b(not on (?:a |another )?page|directly on (?:my )?screen|only on (?:my )?screen)\b/i.test(
    userText
  );

  const hasTableMarks = /\b(check|cross)\s*\(/.test(processed);
  const hasCardAlready = /\b(receipt|clipping|paper|sticky)\s*\(/.test(processed);
  const numTables = layout ? layout.tables.length : 0;
  const numAnswerLines = layout ? layout.answerLines.length : 0;

  // Case A: User explicitly asked for a page/card, OR the page has NO blank answer lines and NO table (e.g. printed math exam paper),
  // and the model scribbled loose `write(...)` calls directly on top of the printed page instead of opening a card!
  if (
    !hasCardAlready &&
    (userWantsPageCard ||
      (!userForbidsPageCard && numTables === 0 && numAnswerLines === 0))
  ) {
    const writeFullRegex =
      /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])([\s\S]*?)\3(?:\s*,\s*(\{[^)]*\}))?\s*\)\s*;?/g;
    const writeMatches = Array.from(processed.matchAll(writeFullRegex));
    if (writeMatches.length > 0) {
      const items = writeMatches
        .map((m) => m[4].replace(/\s+/g, " ").trim())
        .filter(Boolean);
      const spot = layout?.calmCardSpot || {
        x: 820,
        y: 150,
        w: 420,
        h: 420,
        label: "right margin",
      };
      const canvasW = layout?.width || 1280;
      const canvasH = layout?.height || 720;
      const cardW = Math.max(360, Math.min(430, spot.w));
      const cardX = spot.x < 400 ? spot.x : Math.min(spot.x, canvasW - cardW - 28);
      const cardY = Math.min(spot.y, canvasH - (60 + items.length * 32) - 32);

      let replacedFirst = false;
      processed = processed.replace(writeFullRegex, () => {
        if (!replacedFirst) {
          replacedFirst = true;
          return `receipt(${cardX}, ${Math.max(56, cardY)}, "Step-by-Step Solution", ${JSON.stringify(items)}, { w: ${cardW}, title: "solution" });`;
        }
        return "";
      });
      return processed.replace(/\n{3,}/g, "\n\n").trim();
    }
  }

  if (!layout) return processed;

  // Case B: Solving a table or real dotted lines on screen (and user did NOT ask for a page/card) -> strip redundant summary cards
  if (
    !userWantsPageCard &&
    (userForbidsPageCard ||
      (hasTableMarks && layout.tables.length > 0) ||
      (/\bwrite\s*\(/.test(processed) && layout.answerLines.length > 0))
  ) {
    processed = processed.replace(
      /^[ \t]*(?:receipt|clipping)\s*\([\s\S]*?\)\s*;?[ \t]*$/gm,
      ""
    );
  }

  if (layout.tables.length > 0) {
    const table = layout.tables[0];
    const dataRows = table.rows.filter((r) => !r.isHeader);
    const answerCols =
      table.cols.length >= 2 ? table.cols.slice(1) : table.cols;

    if (dataRows.length > 0 && answerCols.length > 0) {
      // Step 1: Convert any `point(x, y, "...✓..." / "...✗..." / "...Row N...")` into `check(x, y)` or `cross(x, y)`
      processed = processed.replace(
        /\bpoint\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])([\s\S]*?)\3\s*(?:,[^)]*)?\)/g,
        (full, xStr: string, yStr: string, _q: string, label: string) => {
          const hasCheck = /(?:✓|✔|✅|\bcheck\b|\btick\b)/i.test(label);
          const hasCross = /(?:✗|✘|❌|\bcross\b)/i.test(label);
          const rowMatch = label.match(/\brow\s*(\d+)/i);
          if (!hasCheck && !hasCross && !rowMatch) return full;

          const fn = hasCross && !hasCheck ? "cross" : "check";
          let targetX = parseFloat(xStr);
          let targetY = parseFloat(yStr);

          if (/\bcisc\b/i.test(label) && answerCols.length >= 2) {
            targetX = answerCols[1].cx;
          } else if (/\brisc\b/i.test(label)) {
            targetX = answerCols[0].cx;
          }
          if (rowMatch) {
            const rIdx = parseInt(rowMatch[1], 10) - 1;
            if (rIdx >= 0 && rIdx < dataRows.length) {
              targetY = dataRows[rIdx].cy;
            }
          }
          return `${fn}(${targetX}, ${targetY})`;
        }
      );

      // Step 2: Snap all check(x, y) and cross(x, y) calls to exact table cell centers (cx, cy)
      const callRegex =
        /\b(check|cross)\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)(\s*(?:,[^)]*)?)\)/g;
      const matches = Array.from(processed.matchAll(callRegex));
      if (matches.length > 0) {
        const avgX =
          matches.reduce((s, m) => s + parseFloat(m[2]), 0) / matches.length;
        const minAnswerColX = answerCols[0].x0;
        const needsVitRescale =
          avgX < minAnswerColX - 80 &&
          avgX * (1280 / 840) >= minAnswerColX - 60 &&
          avgX * (1280 / 840) <= table.x + table.w + 60;

        const checkMatches = matches.filter((m) => m[1] === "check");
        const crossMatches = matches.filter((m) => m[1] === "cross");
        const checksOnePerRow = checkMatches.length === dataRows.length;
        const crossesOnePerRow = crossMatches.length === dataRows.length;
        const totalOnePerRow = matches.length === dataRows.length;
        const totalTwoPerRow = matches.length === dataRows.length * 2;

        let callIndex = 0;
        let checkIndex = 0;
        let crossIndex = 0;

        processed = processed.replace(
          callRegex,
          (_full, fnName: string, xStr: string, yStr: string, rest: string) => {
            const idx = callIndex++;
            const cIdx = fnName === "check" ? checkIndex++ : crossIndex++;
            let rawX = parseFloat(xStr);
            let rawY = parseFloat(yStr);

            if (needsVitRescale) {
              rawX = rawX * (1280 / 840);
              rawY = rawY * (720 / 565);
            }

            // Snap X to the closest answer column center (e.g. RISC Col 2 vs CISC Col 3)
            let bestCol = answerCols[0];
            let bestDx = Math.abs(rawX - bestCol.cx);
            for (const col of answerCols) {
              const dx = Math.abs(rawX - col.cx);
              if (dx < bestDx) {
                bestDx = dx;
                bestCol = col;
              }
            }

            // Find closest dataRow by Y
            let bestRow = dataRows[0];
            let bestDy = Math.abs(rawY - bestRow.cy);
            for (const row of dataRows) {
              const dy = Math.abs(rawY - row.cy);
              if (dy < bestDy) {
                bestDy = dy;
                bestRow = row;
              }
            }

            let snappedY = bestRow.cy;
            if (bestDy > 20) {
              if (
                (fnName === "check" && checksOnePerRow) ||
                (fnName === "cross" && crossesOnePerRow)
              ) {
                snappedY = dataRows[cIdx].cy;
              } else if (totalOnePerRow && idx < dataRows.length) {
                snappedY = dataRows[idx].cy;
              } else if (
                totalTwoPerRow &&
                Math.floor(idx / 2) < dataRows.length
              ) {
                snappedY = dataRows[Math.floor(idx / 2)].cy;
              }
            }

            return `${fnName}(${bestCol.cx}, ${snappedY}${rest || ""})`;
          }
        );
      }
    }
  }

  // Step 3: Snap every `write(x, y, "text", ...)` call to the detected blank/dotted AnswerLines,
  // aligning X to the true left start of the dotted line (al.x) and Y to its baseline (al.y - 4),
  // and splitting long single-line strings across consecutive dotted lines!
  if (layout.answerLines.length > 0) {
    const table = layout.tables[0];
    const hasTableChecks = /\b(check|cross)\s*\(/.test(processed);
    const linesBelowTable = table
      ? layout.answerLines.filter((al) => al.y > table.y + table.h)
      : layout.answerLines;

    const candidateLines =
      hasTableChecks && linesBelowTable.length > 0
        ? linesBelowTable
        : layout.answerLines;

    const writeFullRegex =
      /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])([\s\S]*?)\3(?:\s*,\s*(\{[^)]*\}))?\s*\)/g;
    let nextLineCursor = 0;

    processed = processed.replace(
      writeFullRegex,
      (_full, _xStr: string, yStr: string, _q: string, rawText: string) => {
        const wy = parseFloat(yStr);
        let startIdx = nextLineCursor;
        if (startIdx < candidateLines.length) {
          let bestIdx = startIdx;
          let bestDy = Math.abs(wy - (candidateLines[startIdx].y - 4));
          for (let i = startIdx; i < candidateLines.length; i++) {
            const dy = Math.abs(wy - (candidateLines[i].y - 4));
            if (dy < bestDy && dy < 28) {
              bestDy = dy;
              bestIdx = i;
            }
          }
          startIdx = bestIdx;
        }

        const refLine =
          candidateLines[Math.min(startIdx, candidateLines.length - 1)];
        const maxChars = Math.max(55, Math.min(82, Math.floor(refLine.w / 10.5)));
        const chunks = splitTextForAnswerLines(rawText, maxChars);

        const outCalls: string[] = [];
        for (let c = 0; c < chunks.length; c++) {
          const targetIdx = startIdx + c;
          const al =
            candidateLines[Math.min(targetIdx, candidateLines.length - 1)];
          const yCoord =
            targetIdx < candidateLines.length
              ? al.y - 4
              : al.y - 4 + (targetIdx - (candidateLines.length - 1)) * 28;
          outCalls.push(
            `write(${al.x}, ${yCoord}, ${JSON.stringify(chunks[c])}, { size: 17, place: false })`
          );
          nextLineCursor = Math.max(nextLineCursor, targetIdx + 1);
        }
        return outCalls.join(";\n");
      }
    );
  }

  return processed;
}

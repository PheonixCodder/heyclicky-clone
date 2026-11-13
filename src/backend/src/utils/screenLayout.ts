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
    if (buf.length > 4 * 1024 * 1024) return null;

    const decoded = jpeg.decode(buf, {
      useTArray: true,
      maxResolutionInMP: 16,
      maxMemoryUsageInMB: 128,
    });
    const origW = decoded.width;
    const origH = decoded.height;
    const data = decoded.data;
    if (!origW || !origH || !data || data.length < origW * origH * 4) {
      return null;
    }

    // Downsample pixel scanning to a virtual <=1280x720 grid when input exceeds 1280x720
    const needsDownsample = origW > 1280 || origH > 720;
    const scaleFactor = needsDownsample
      ? Math.min(1280 / origW, 720 / origH)
      : 1;
    const W = needsDownsample
      ? Math.max(320, Math.round(origW * scaleFactor))
      : origW;
    const H = needsDownsample
      ? Math.max(240, Math.round(origH * scaleFactor))
      : origH;

    const pixelOffset = (x: number, y: number): number => {
      const cx = Math.max(0, Math.min(W - 1, x));
      const cy = Math.max(0, Math.min(H - 1, y));
      if (!needsDownsample) {
        return (cy * origW + cx) * 4;
      }
      const sx = Math.min(origW - 1, Math.floor((cx * origW) / W));
      const sy = Math.min(origH - 1, Math.floor((cy * origH) / H));
      return (sy * origW + sx) * 4;
    };

    const luma = (x: number, y: number): number => {
      const idx = pixelOffset(x, y);
      return 0.299 * data[idx] + 0.587 * data[idx + 1] + 0.114 * data[idx + 2];
    };

    // Detect red user highlight loop or Kursor ink so it can bridge an existing border line without starting fake lines
    const isRedHighlight = (x: number, y: number): boolean => {
      const idx = pixelOffset(x, y);
      const r = data[idx];
      const g = data[idx + 1];
      const b = data[idx + 2];
      return r > 175 && r - Math.min(g, b) > 45;
    };

    // Relative-contrast horizontal border pixel:
    // Detects thin horizontal borders where pixel luminance differs from local background luminance by >= 26,
    // supporting both dark-on-light and light-on-dark (dark mode) tables while rejecting thick solid text blocks.
    const isHLinePixel = (x: number, y: number): boolean => {
      const redAbove = isRedHighlight(x, y - 5);
      const redBelow = isRedHighlight(x, y + 5);
      const bgAbove = redAbove ? luma(x, y - 8) : luma(x, y - 5);
      const bgBelow = redBelow ? luma(x, y + 8) : luma(x, y + 5);
      if (!redAbove && !redBelow && Math.abs(bgAbove - bgBelow) > 40) {
        return false;
      }
      const bgLum = 0.5 * (bgAbove + bgBelow);
      let maxDiff = 0;
      for (let dy = -2; dy <= 2; dy++) {
        const diff = Math.abs(luma(x, y + dy) - bgLum);
        if (diff > maxDiff) maxDiff = diff;
      }
      return maxDiff >= 26;
    };

    // Relative-contrast vertical border pixel:
    // Supports both light-mode and dark-mode vertical borders where pixel luminance differs from local background by >= 26.
    const isVLinePixel = (x: number, y: number): boolean => {
      const redLeft = isRedHighlight(x - 5, y);
      const redRight = isRedHighlight(x + 5, y);
      const bgLeft = redLeft ? luma(x - 8, y) : luma(x - 5, y);
      const bgRight = redRight ? luma(x + 8, y) : luma(x + 5, y);
      if (!redLeft && !redRight && Math.abs(bgLeft - bgRight) > 40) {
        return false;
      }
      const bgLum = 0.5 * (bgLeft + bgRight);
      let maxDiff = 0;
      for (let dx = -2; dx <= 2; dx++) {
        const diff = Math.abs(luma(x + dx, y) - bgLum);
        if (diff > maxDiff) maxDiff = diff;
      }
      return maxDiff >= 26;
    };

    // 1. Detect solid horizontal table border lines (skip top browser chrome y < 135 and bottom edge y > H - 5)
    const hCandidates: Array<{
      y: number;
      x0: number;
      x1: number;
      len: number;
    }> = [];

    for (let y = 135; y < H - 5; y++) {
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
          const bgRef = 0.5 * (luma(x, curY - 5) + luma(x, curY + 5));
          const diffCur = Math.abs(luma(x, curY) - bgRef);
          const diffDown = Math.abs(luma(x, curY + 1) - bgRef);
          const diffUp = Math.abs(luma(x, curY - 1) - bgRef);
          if (diffDown > diffCur + 6 && diffDown > diffUp && curY - y < 6) {
            curY++;
          } else if (
            diffUp > diffCur + 6 &&
            diffUp > diffDown &&
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

      if (bestLen >= 145) {
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

    // Verify that two horizontal lines yA and yB are connected by vertical column/frame borders
    const countConnectingVLines = (
      yA: number,
      yB: number,
      x0: number,
      x1: number
    ): number => {
      if (yB - yA < 7 || yB - yA > 140) return 0;
      const vXs: number[] = [];
      for (let x = Math.max(20, x0 - 6); x <= Math.min(W - 20, x1 + 6); x++) {
        let hits = 0;
        let total = 0;
        for (let y = yA + 1; y <= yB - 1; y++) {
          total++;
          if (isVLinePixel(x, y) || isRedHighlight(x, y)) hits++;
        }
        if (total >= 4 && hits >= total * 0.6) {
          vXs.push(x);
        }
      }
      return clusterPositions(vXs, 4).filter(
        (x, idx, arr) => idx === 0 || x - arr[idx - 1] >= 12
      ).length;
    };

    const tables: DetectedTable[] = [];

    if (hLines.length >= 3) {
      const usedHLineIndices = new Set<number>();
      for (let i = 0; i < hLines.length; i++) {
        if (usedHLineIndices.has(i)) continue;
        const group = [hLines[i]];
        const groupIndices = [i];
        for (let j = i + 1; j < hLines.length; j++) {
          if (usedHLineIndices.has(j)) continue;
          const prev = group[group.length - 1];
          const dy = hLines[j].y - prev.y;
          const sameSpan =
            Math.abs(hLines[j].x0 - group[0].x0) <= 36 &&
            Math.abs(hLines[j].x1 - group[0].x1) <= 36;
          if (
            sameSpan &&
            dy >= 7 &&
            dy <= 140 &&
            countConnectingVLines(
              prev.y,
              hLines[j].y,
              group[0].x0,
              group[0].x1
            ) >= 2
          ) {
            group.push(hLines[j]);
            groupIndices.push(j);
          }
        }

        if (group.length >= 3) {
          for (const idx of groupIndices) usedHLineIndices.add(idx);
          const yMin = group[0].y;
          const yMax = group[group.length - 1].y;
          const tableH = yMax - yMin;
          const avgX0 = Math.round(
            group.reduce((s, g) => s + g.x0, 0) / group.length
          );
          const avgX1 = Math.round(
            group.reduce((s, g) => s + g.x1, 0) / group.length
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
            if (total > 6 && hits >= total * 0.52) {
              vCandidates.push(x);
            }
          }

          const xLines = clusterPositions(vCandidates, 4).filter(
            (x, idx, arr) => idx === 0 || x - arr[idx - 1] >= 12
          );

          if (xLines.length >= 3 && tableH >= 40) {
            const xMin = xLines[0];
            const xMax = xLines[xLines.length - 1];
            const numRawRows = group.length - 1;
            const firstRowH = group[1].y - group[0].y;
            const hasVisibleHeader =
              yMin >= 135 &&
              (numRawRows >= 4 || (numRawRows >= 2 && firstRowH <= 30));

            const rows = [];
            for (let r = 0; r < numRawRows; r++) {
              const y0 = group[r].y;
              const y1 = group[r + 1].y;
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
              yLines: group.map((g) => g.y),
              rows,
              cols,
              cells,
            });
          }
        }
      }
    }

    // 2. Detect dotted/dashed and solid underline worksheet answer lines (skipping any detected table)
    const minLumaBand = (x: number, y: number, halfBand: number): number => {
      let m = 255;
      for (let dy = -halfBand; dy <= halfBand; dy++) {
        const lv = luma(x, y + dy);
        if (lv < m) m = lv;
      }
      return m;
    };

    const isClearHandwritingSpace = (
      x: number,
      y: number,
      bgRef: number
    ): boolean => {
      return (
        bgRef >= 165 &&
        Math.abs(luma(x, y - 4) - bgRef) <= 18 &&
        Math.abs(luma(x, y - 8) - bgRef) <= 18 &&
        Math.abs(luma(x, y - 12) - bgRef) <= 18 &&
        Math.abs(luma(x, y + 4) - bgRef) <= 18 &&
        Math.abs(luma(x, y + 8) - bgRef) <= 18
      );
    };

    const pushOrMergeAnswerLine = (
      list: AnswerLine[],
      candX: number,
      candY: number,
      candW: number
    ) => {
      if (list.length === 0 || candY - list[list.length - 1].y >= 12) {
        list.push({
          x: candX,
          y: candY,
          w: candW,
        });
      } else {
        const prev = list[list.length - 1];
        if (candW > prev.w) {
          prev.x = candX;
          prev.y = candY;
          prev.w = candW;
        }
      }
    };

    const answerLines: AnswerLine[] = [];
    const solidBlankCandidates: AnswerLine[] = [];

    for (let y = 135; y < H - 16; y++) {
      if (tables.some((t) => y >= t.y - 12 && y <= t.y + t.h + 12)) continue;

      // 2a. Scan for a periodic chain of dots or dashes (dotW in 1..14px, gapW in 1..16px)
      let curStart = -1;
      let curEnd = -1;
      let dotCount = 0;
      let runLen = 0;
      let inDot = false;

      let bestStart = -1;
      let bestEnd = -1;
      let bestDots = 0;

      for (let x = 80; x < W - 40; x++) {
        const bgPaper = 0.5 * (luma(x, y - 6) + luma(x, y + 6));
        const d =
          isClearHandwritingSpace(x, y, bgPaper) &&
          bgPaper - minLumaBand(x, y, 1) >= 24;
        if (d === inDot) {
          runLen++;
          if ((inDot && runLen > 14) || (!inDot && runLen > 16)) {
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
      if (
        (bestDots >= 25 && span >= Math.min(340, W * 0.28)) ||
        (bestDots >= 18 && span >= 320)
      ) {
        pushOrMergeAnswerLine(answerLines, bestStart + 6, y, span - 12);
        continue;
      }

      // 2b. Scan for solid horizontal blank underline rules (thickness 1..3px, width >= 45px, clear 12px space above, not a full-page ruled line or axis/box)
      let solidStart = -1;
      let solidGap = 0;
      let solidHits = 0;
      let vCrossings = 0;
      let bestSolidX0 = -1;
      let bestSolidX1 = -1;
      let bestSolidLen = 0;
      let bestSolidVCross = 0;

      for (let x = 80; x < W - 40; x++) {
        const bgPaper = 0.5 * (luma(x, y - 6) + luma(x, y + 6));
        const isThinSolidRule =
          isClearHandwritingSpace(x, y, bgPaper) &&
          bgPaper - minLumaBand(x, y, 1) >= 26 &&
          Math.abs(luma(x, y - 3) - bgPaper) <= 22 &&
          Math.abs(luma(x, y + 3) - bgPaper) <= 22;

        if (isThinSolidRule) {
          if (solidStart < 0) {
            solidStart = x;
            solidHits = 0;
            vCrossings = 0;
          }
          solidHits++;
          solidGap = 0;
          if (isVLinePixel(x, y - 8) || isVLinePixel(x, y + 8)) {
            vCrossings++;
          }
        } else if (solidStart >= 0) {
          solidGap++;
          if (solidGap <= 4) continue;
          const endX = x - solidGap;
          const sSpan = endX - solidStart;
          if (sSpan > bestSolidLen && solidHits >= sSpan * 0.92) {
            bestSolidLen = sSpan;
            bestSolidX0 = solidStart;
            bestSolidX1 = endX;
            bestSolidVCross = vCrossings;
          }
          solidStart = -1;
          solidHits = 0;
          solidGap = 0;
          vCrossings = 0;
        }
      }
      if (solidStart >= 0) {
        const endX = W - 40 - solidGap;
        const sSpan = endX - solidStart;
        if (sSpan > bestSolidLen && solidHits >= sSpan * 0.92) {
          bestSolidLen = sSpan;
          bestSolidX0 = solidStart;
          bestSolidX1 = endX;
          bestSolidVCross = vCrossings;
        }
      }

      // Accept solid underline blanks (>= 45px, and for standalone page lines 300px..52% canvas width with zero vertical crossings)
      const notSpanningTableBorder = !tables.some(
        (t) =>
          Math.abs(bestSolidX0 - t.x) <= 20 &&
          Math.abs(bestSolidX1 - (t.x + t.w)) <= 20
      );
      if (
        bestSolidLen >= 45 &&
        bestSolidLen >= 300 &&
        bestSolidLen <= W * 0.52 &&
        bestSolidVCross === 0 &&
        notSpanningTableBorder
      ) {
        pushOrMergeAnswerLine(
          solidBlankCandidates,
          bestSolidX0 + 6,
          y,
          bestSolidLen - 12
        );
      }
    }

    if (
      solidBlankCandidates.length >= 1 &&
      solidBlankCandidates.length <= 4
    ) {
      for (const sc of solidBlankCandidates) {
        if (!answerLines.some((al) => Math.abs(al.y - sc.y) < 12)) {
          answerLines.push(sc);
        }
      }
      answerLines.sort((a, b) => a.y - b.y);
    }

    // 3. Find the calmest spot for an explanation card (paper / clipping / receipt / figure)
    // Never place a card near y > 480 where multi-item cards overflow the 720px canvas height!
    const maxRightSpotX = Math.min(860, Math.max(16, W - 380));
    const maxMidSpotX = Math.min(760, Math.max(16, W - 380));
    const clampedSpotY = Math.min(150, Math.max(16, H - 220));
    const candidateSpots = [
      {
        x: maxRightSpotX,
        y: clampedSpotY,
        w: Math.min(390, Math.max(160, W - maxRightSpotX - 16)),
        h: Math.min(420, Math.max(140, H - clampedSpotY - 16)),
        label: "right margin",
      },
      {
        x: 16,
        y: clampedSpotY,
        w: Math.min(290, Math.max(160, W - 32)),
        h: Math.min(420, Math.max(140, H - clampedSpotY - 16)),
        label: "left margin",
      },
      {
        x: maxMidSpotX,
        y: clampedSpotY,
        w: Math.min(480, Math.max(160, W - maxMidSpotX - 16)),
        h: Math.min(420, Math.max(140, H - clampedSpotY - 16)),
        label: "right half",
      },
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
          const i1 = pixelOffset(x, y);
          const i2 = pixelOffset(x + 2, y);
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

    // Scale coordinates back to original image dimensions if downsampled, and ensure calmCardSpot is clamped to canvas
    if (needsDownsample) {
      const sx = (v: number) => Math.round((v * origW) / W);
      const sy = (v: number) => Math.round((v * origH) / H);
      for (const t of tables) {
        t.x = sx(t.x);
        t.y = sy(t.y);
        t.w = sx(t.w);
        t.h = sy(t.h);
        t.xLines = t.xLines.map(sx);
        t.yLines = t.yLines.map(sy);
        for (const r of t.rows) {
          r.y0 = sy(r.y0);
          r.y1 = sy(r.y1);
          r.cy = sy(r.cy);
        }
        for (const c of t.cols) {
          c.x0 = sx(c.x0);
          c.x1 = sx(c.x1);
          c.cx = sx(c.cx);
          c.w = sx(c.w);
        }
        for (const cell of t.cells) {
          cell.x = sx(cell.x);
          cell.y = sy(cell.y);
          cell.w = sx(cell.w);
          cell.h = sy(cell.h);
          cell.cx = sx(cell.cx);
          cell.cy = sy(cell.cy);
        }
      }
      for (const al of answerLines) {
        al.x = sx(al.x);
        al.y = sy(al.y);
        al.w = sx(al.w);
      }
      bestSpot = {
        x: sx(bestSpot.x),
        y: sy(bestSpot.y),
        w: sx(bestSpot.w),
        h: sy(bestSpot.h),
        label: bestSpot.label,
      };
    }

    const clampedW = Math.max(
      1,
      Math.min(bestSpot.w, Math.max(1, origW - 32))
    );
    const clampedH = Math.max(
      1,
      Math.min(bestSpot.h, Math.max(1, origH - 32))
    );
    const clampedX =
      origW >= clampedW + 32
        ? Math.max(16, Math.min(860, origW - clampedW - 16, bestSpot.x))
        : Math.max(0, origW - clampedW);
    const clampedY =
      origH >= clampedH + 32
        ? Math.max(16, Math.min(origH - clampedH - 16, bestSpot.y))
        : Math.max(0, origH - clampedH);

    bestSpot = {
      x: clampedX,
      y: clampedY,
      w: clampedW,
      h: clampedH,
      label: bestSpot.label,
    };

    // 4. Build concise prompt note with exact pixel coordinates
    const noteLines: string[] = [
      `## Measured Screen Geometry (${origW}x${origH} exact canvas pixels)`,
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
      width: origW,
      height: origH,
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
 * Balanced-parenthesis statement stripper for `receipt(...)` and `clipping(...)` calls.
 * Respects string literals ('...', "...", `...`) and comments (//..., /*...*\/) so inline comments
 * or multiline array arguments with inner `)` never corrupt subsequent statements.
 */
function stripReceiptAndClippingStatements(code: string): string {
  let out = "";
  let i = 0;
  const n = code.length;

  const findBalancedCloseParen = (openParenIdx: number): number => {
    let depth = 1;
    let p = openParenIdx + 1;
    while (p < n && depth > 0) {
      const ch = code[p];
      if (ch === "'" || ch === '"' || ch === "`") {
        const quote = ch;
        p++;
        while (p < n) {
          if (code[p] === "\\") {
            p += 2;
            continue;
          }
          if (code[p] === quote) {
            p++;
            break;
          }
          p++;
        }
        continue;
      }
      if (ch === "/" && code[p + 1] === "/") {
        p += 2;
        while (p < n && code[p] !== "\n") p++;
        continue;
      }
      if (ch === "/" && code[p + 1] === "*") {
        p += 2;
        while (p < n && !(code[p] === "*" && code[p + 1] === "/")) p++;
        if (p < n) p += 2;
        continue;
      }
      if (ch === "(") depth++;
      else if (ch === ")") depth--;
      p++;
    }
    return depth === 0 ? p - 1 : -1;
  };

  while (i < n) {
    const ch = code[i];
    // Preserve strings and comments verbatim so we never match inside them
    if (ch === "'" || ch === '"' || ch === "`") {
      const start = i;
      const quote = ch;
      i++;
      while (i < n) {
        if (code[i] === "\\") {
          i += 2;
          continue;
        }
        if (code[i] === quote) {
          i++;
          break;
        }
        i++;
      }
      out += code.slice(start, i);
      continue;
    }
    if (ch === "/" && code[i + 1] === "/") {
      const start = i;
      i += 2;
      while (i < n && code[i] !== "\n") i++;
      out += code.slice(start, i);
      continue;
    }
    if (ch === "/" && code[i + 1] === "*") {
      const start = i;
      i += 2;
      while (i < n && !(code[i] === "*" && code[i + 1] === "/")) i++;
      if (i < n) i += 2;
      out += code.slice(start, i);
      continue;
    }

    // Check if current position starts an optional `const/let/var x = ` + `receipt(` or `clipping(`
    const slice = code.slice(i);
    const stmtMatch = slice.match(
      /^(?:[ \t]*(?:(?:const|let|var)\s+[A-Za-z_$][\w$]*\s*=\s*)?)(?:receipt|clipping)\s*\(/
    );
    if (
      stmtMatch &&
      (i === 0 ||
        code[i - 1] === "\n" ||
        code[i - 1] === ";" ||
        code[i - 1] === "{")
    ) {
      const openParenIdx = i + stmtMatch[0].length - 1;
      const closeParenIdx = findBalancedCloseParen(openParenIdx);
      if (closeParenIdx >= 0) {
        let endIdx = closeParenIdx + 1;
        while (endIdx < n && (code[endIdx] === " " || code[endIdx] === "\t")) {
          endIdx++;
        }
        if (endIdx < n && code[endIdx] === ";") {
          endIdx++;
        }
        while (endIdx < n && (code[endIdx] === " " || code[endIdx] === "\t")) {
          endIdx++;
        }
        // Also consume an optional trailing single-line comment on the same line
        if (endIdx < n && code[endIdx] === "/" && code[endIdx + 1] === "/") {
          endIdx += 2;
          while (endIdx < n && code[endIdx] !== "\n" && code[endIdx] !== "\r") {
            endIdx++;
          }
        }
        i = endIdx;
        continue;
      }
    }

    out += ch;
    i++;
  }

  return out;
}

/**
 * Replaces JS string literal contents and single/multi-line comments with spaces of equal length
 * so regex checks (`hasTableMarks`, `hasCardAlready`, etc.) never trigger false positives on comments or spoken strings.
 */
export function stripCommentsAndStrings(code: string): string {
  let out = "";
  let i = 0;
  const n = code.length;
  while (i < n) {
    const ch = code[i];
    if (ch === "'" || ch === '"' || ch === "`") {
      const quote = ch;
      out += quote;
      i++;
      while (i < n) {
        if (code[i] === "\\") {
          out += "  ";
          i += 2;
          continue;
        }
        if (code[i] === quote) {
          out += quote;
          i++;
          break;
        }
        out += code[i] === "\n" ? "\n" : " ";
        i++;
      }
      continue;
    }
    if (ch === "/" && code[i + 1] === "/") {
      while (i < n && code[i] !== "\n") {
        out += " ";
        i++;
      }
      continue;
    }
    if (ch === "/" && code[i + 1] === "*") {
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
    out += ch;
    i++;
  }
  return out;
}

/**
 * Post-processes generated ClickyDraw JS code so that:
 * 1) If the user asked to solve on a page/card OR there are no blank lines/tables on screen (e.g. a dense math/exam paper),
 *    any loose `write()` calls scribbled over the document are automatically converted into a clean draggable `receipt()` page in the calm margin!
 * 2) When solving a table with `check()`/`cross()` (and user did NOT ask for a page), redundant `receipt()`/`clipping()` summary cards are stripped.
 * 3) Every `check(x, y)` and `cross(x, y)` call within range of a table snaps to the exact mathematical pixel center `(cx, cy)` of its table cell.
 * 4) Every `write(x, y, text)` call on a worksheet with real dotted/blank lines snaps to `al.x` and `al.y - 4` and wraps across consecutive lines.
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

  const strippedCode = stripCommentsAndStrings(processed);
  const hasTableMarks = /\b(check|cross)\s*\(/.test(strippedCode);
  const hasCardAlready = /\b(receipt|clipping|paper|sticky)\s*\(/.test(
    strippedCode
  );
  const numTables = layout ? layout.tables.length : 0;
  const numAnswerLines = layout ? layout.answerLines.length : 0;

  // Regex matching a quoted string literal WITH support for escaped quotes (`\'`, `\"`)
  // and an optional options object `{ ... }` (even when strings or expressions inside `{ ... }` contain `)`!)
  const writeCallPattern =
    /((?:[ \t]*(?:const|let|var)\s+([A-Za-z_$][\w$]*)\s*=\s*)?)\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])((?:\\.|(?!\5)[\s\S])*)\5(?:\s*,\s*(\{[^{}]*\}))?\s*\)(\s*;?)/g;

  // Case A: User explicitly asked for a page/card, OR the page has NO blank answer lines and NO table (e.g. printed math exam paper),
  // and the model scribbled loose `write(...)` calls directly on top of the printed page instead of opening a card!
  if (
    !hasCardAlready &&
    (userWantsPageCard ||
      (!userForbidsPageCard && numTables === 0 && numAnswerLines === 0))
  ) {
    const writeMatches = Array.from(processed.matchAll(writeCallPattern));
    if (writeMatches.length > 0) {
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
      const cardX =
        spot.x < 400 ? spot.x : Math.min(spot.x, canvasW - cardW - 28);

      // Group writeMatches by the `step(k)` beat they belong to so multi-beat programs never lose all visual calls in beat 2+
      const stepIndices: number[] = [0];
      for (const sm of processed.matchAll(/\bstep\s*\(\s*\d+\s*\)/g)) {
        if (typeof sm.index === "number") {
          stepIndices.push(sm.index);
        }
      }
      const getBeatForIndex = (pos: number): number => {
        let beat = 0;
        for (let s = 0; s < stepIndices.length; s++) {
          if (pos >= stepIndices[s]) beat = s;
        }
        return beat;
      };

      const beatItemsMap = new Map<number, string[]>();
      for (const m of writeMatches) {
        const b = getBeatForIndex(m.index ?? 0);
        const unescaped = m[6]
          .replace(/\\(['"`\\])/g, "$1")
          .replace(/\s+/g, " ")
          .trim();
        if (unescaped) {
          const list = beatItemsMap.get(b) || [];
          list.push(unescaped);
          beatItemsMap.set(b, list);
        }
      }

      const distinctBeatsWithWrites = Array.from(beatItemsMap.keys());
      const allItems = writeMatches
        .map((m) =>
          m[6]
            .replace(/\\(['"`\\])/g, "$1")
            .replace(/\s+/g, " ")
            .trim()
        )
        .filter(Boolean);

      const replacedBeats = new Set<number>();
      let globalReplacedFirst = false;

      processed = processed.replace(
        writeCallPattern,
        (
          full,
          varPrefix: string,
          varName: string | undefined,
          _xStr: string,
          _yStr: string,
          _q: string,
          _rawStr: string,
          _opts: string | undefined,
          _semi: string,
          offset: number
        ) => {
          const beat = getBeatForIndex(offset);
          const usePerBeat = distinctBeatsWithWrites.length > 1;
          const shouldEmitReceipt = usePerBeat
            ? !replacedBeats.has(beat)
            : !globalReplacedFirst;

          if (shouldEmitReceipt) {
            replacedBeats.add(beat);
            globalReplacedFirst = true;
            const itemsForReceipt = usePerBeat
              ? beatItemsMap.get(beat) || allItems
              : allItems;
            const cardY = Math.max(
              16,
              Math.min(spot.y, canvasH - (60 + itemsForReceipt.length * 32) - 16)
            );
            const receiptCall = `receipt(${cardX}, ${cardY}, "Step-by-Step Solution", ${JSON.stringify(itemsForReceipt)}, { w: ${cardW}, title: "solution" });`;
            return varPrefix ? `${varPrefix}${receiptCall}` : receiptCall;
          }

          // For 2nd+ write() in the same beat: if assigned to a variable (`const w2 = write(...)`),
          // check if `varName` is referenced later in the code; if so keep `const w2 = null;`, otherwise strip cleanly!
          if (varPrefix && varName) {
            const afterSlice = processed.slice(offset + full.length);
            const escapedVar = varName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
            if (new RegExp(`\\b${escapedVar}\\b`).test(afterSlice)) {
              return `${varPrefix}null;`;
            }
          }
          return "";
        }
      );
      return processed.replace(/\n{3,}/g, "\n\n").trim();
    }
  }

  if (!layout) return processed;

  // Case B: Solving a table or real dotted lines on screen (and user did NOT ask for a page/card) -> strip redundant summary cards
  const strippedNow = stripCommentsAndStrings(processed);
  if (
    !userWantsPageCard &&
    (userForbidsPageCard ||
      (hasTableMarks && layout.tables.length > 0) ||
      (/\bwrite\s*\(/.test(strippedNow) && layout.answerLines.length > 0))
  ) {
    processed = stripReceiptAndClippingStatements(processed);
  }

  if (layout.tables.length > 0) {
    const table = layout.tables[0];
    const dataRows = table.rows.filter((r) => !r.isHeader);
    const answerCols =
      table.cols.length >= 2 ? table.cols.slice(1) : table.cols;

    if (dataRows.length > 0 && answerCols.length > 0) {
      // Step 1: Convert `point(x, y, "...✓..." / "...✗...")` near the table into `check(x, y)` or `cross(x, y)`
      processed = processed.replace(
        /\bpoint\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])((?:\\.|(?!\3)[\s\S])*)\3(\s*(?:,\s*(?:\{[^{}]*\}|[^()]*))?)\)/g,
        (full, xStr: string, yStr: string, _q: string, label: string) => {
          const hasCheck = /(?:✓|✔|✅|\bcheck\b|\btick\b)/i.test(label);
          const hasCross = /(?:✗|✘|❌|\bcross\b)/i.test(label);
          // Only convert when unambiguously check OR cross (never both, and never bare "Row 1" without check/cross)
          if (hasCheck === hasCross) return full;

          let targetX = parseFloat(xStr);
          let targetY = parseFloat(yStr);

          const isNearTable =
            targetX >= table.x - 140 &&
            targetX <= table.x + table.w + 140 &&
            targetY >= table.y - 140 &&
            targetY <= table.y + table.h + 140;
          if (!isNearTable) return full;

          const fn = hasCross ? "cross" : "check";
          const rowMatch = label.match(/\brow\s*(\d+)/i);
          if (rowMatch) {
            const rIdx = parseInt(rowMatch[1], 10) - 1;
            if (rIdx >= 0 && rIdx < dataRows.length) {
              targetY = dataRows[rIdx].cy;
            }
          }
          return `${fn}(${targetX}, ${targetY})`;
        }
      );

      // Step 2: Snap check(x, y) and cross(x, y) calls that are within 140px of the table to exact cell centers (cx, cy)
      const callRegex =
        /\b(check|cross)\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)(\s*(?:,\s*(?:\{[^{}]*\}|[^()]*))?)\)/g;
      const allMatches = Array.from(processed.matchAll(callRegex));
      if (allMatches.length > 0) {
        const minAnswerColX = answerCols[0].x0;
        const avgX =
          allMatches.reduce((s, m) => s + parseFloat(m[2]), 0) /
          allMatches.length;
        const maxX = Math.max(...allMatches.map((m) => parseFloat(m[2])));
        const needsVitRescale =
          allMatches.length >= 2 &&
          avgX < minAnswerColX - 80 &&
          maxX < minAnswerColX - 40 &&
          avgX * (1280 / 840) >= minAnswerColX - 60 &&
          avgX * (1280 / 840) <= table.x + table.w + 60;

        const isCoordNearTable = (rawX: number, rawY: number): boolean => {
          const rx = needsVitRescale ? rawX * (1280 / 840) : rawX;
          const ry = needsVitRescale ? rawY * (720 / 565) : rawY;
          return (
            rx >= table.x - 140 &&
            rx <= table.x + table.w + 140 &&
            ry >= table.y - 140 &&
            ry <= table.y + table.h + 140 &&
            (table.cols.length < 2 || rx >= table.x)
          );
        };

        const tableMatches = allMatches.filter((m) =>
          isCoordNearTable(parseFloat(m[2]), parseFloat(m[3]))
        );
        const checkMatches = tableMatches.filter((m) => m[1] === "check");
        const crossMatches = tableMatches.filter((m) => m[1] === "cross");
        const checksOnePerRow = checkMatches.length === dataRows.length;
        const crossesOnePerRow = crossMatches.length === dataRows.length;
        const totalOnePerRow = tableMatches.length === dataRows.length;
        const totalTwoPerRow = tableMatches.length === dataRows.length * 2;

        let callIndex = 0;
        let checkIndex = 0;
        let crossIndex = 0;

        processed = processed.replace(
          callRegex,
          (full, fnName: string, xStr: string, yStr: string, rest: string) => {
            let rawX = parseFloat(xStr);
            let rawY = parseFloat(yStr);
            if (!isCoordNearTable(rawX, rawY)) {
              return full;
            }

            const idx = callIndex++;
            const cIdx = fnName === "check" ? checkIndex++ : crossIndex++;

            if (needsVitRescale) {
              rawX = rawX * (1280 / 840);
              rawY = rawY * (720 / 565);
            }

            // Snap X to the closest answer column center
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
            if (bestDy > 45) {
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
    const hasTableChecks = /\b(check|cross)\s*\(/.test(
      stripCommentsAndStrings(processed)
    );
    const linesBelowTable = table
      ? layout.answerLines.filter((al) => al.y > table.y + table.h)
      : layout.answerLines;

    const writeSnapRegex =
      /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])((?:\\.|(?!\3)[\s\S])*)\3(?:\s*,\s*(\{[^{}]*\}))?\s*\)/g;

    // Check if any write() call targets the region above the table
    const allWriteMatches = Array.from(processed.matchAll(writeSnapRegex));
    const hasWriteAboveTable =
      table != null &&
      allWriteMatches.some((m) => parseFloat(m[2]) < table.y);

    const candidateLines =
      hasTableChecks && !hasWriteAboveTable && linesBelowTable.length > 0
        ? linesBelowTable
        : layout.answerLines;

    let nextLineCursor = 0;

    processed = processed.replace(
      writeSnapRegex,
      (
        _full,
        _xStr: string,
        yStr: string,
        _q: string,
        rawText: string,
        _opts: string | undefined,
        offset: number
      ) => {
        const wy = parseFloat(yStr);
        const unescapedText = rawText.replace(/\\(['"`\\])/g, "$1");
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
        const chunks = splitTextForAnswerLines(unescapedText, maxChars);

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

        if (outCalls.length > 1) {
          // If preceded on the same line by an unbraced `if (...)` or `else`, wrap in a comma expression `(...)`
          const lineStart = processed.lastIndexOf("\n", offset - 1) + 1;
          const prefixOnLine = processed.slice(lineStart, offset);
          if (/\b(?:if\s*\([^)]*\)|else)\s*$/.test(prefixOnLine)) {
            return `(${outCalls.join(", ")})`;
          }
        }
        return outCalls.join(";\n");
      }
    );
  }

  return processed;
}

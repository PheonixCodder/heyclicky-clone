import zlib from "node:zlib";
import jpeg from "jpeg-js";
import {
  prewarmWinOcrDaemon,
  closeWinOcrDaemon,
  runWindowsOcr,
  getCachedWindowsOcr,
  type OcrWordBox,
  type OcrLineBox,
  type OcrScreenResult,
} from "./winOcr.js";

export {
  prewarmWinOcrDaemon,
  closeWinOcrDaemon,
  runWindowsOcr,
  getCachedWindowsOcr,
  type OcrWordBox,
  type OcrLineBox,
  type OcrScreenResult,
};

export interface DecodedImage {
  width: number;
  height: number;
  data: Uint8Array;
}

export interface TableCell {
  row: number;
  col: number;
  x: number;
  y: number;
  w: number;
  h: number;
  cx: number;
  cy: number;
  text?: string;
}

export type ScreenGridCell = TableCell;

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
    label?: string;
  }>;
  cols: Array<{
    colIndex: number;
    x0: number;
    x1: number;
    cx: number;
    w: number;
    headerText?: string;
  }>;
  cells: TableCell[];
  questionNumber?: string;
  subPart?: string;
  subPartPrompt?: string;
}

export interface ScreenBlankLine {
  lineIndex: number;
  x0: number;
  rawX0?: number;
  x1: number;
  y: number;
  w: number;
  writeX: number;
  writeY: number;
  maxWidth: number;
  kind?: "dotted" | "solid" | "dashed";
  label?: string;
  questionNumber?: string;
  subPart?: string;
  subPartPrompt?: string;
  isScrolledOffTopOrphan?: boolean;
  x?: number;
}

export type AnswerLine = ScreenBlankLine;

export interface ScreenLayoutAnalysis {
  width: number;
  height: number;
  tables: DetectedTable[];
  cells: TableCell[];
  answerLines: ScreenBlankLine[];
  ocrLines?: OcrLineBox[];
  calmCardSpot: { x: number; y: number; w: number; h: number; label: string };
  layoutPromptNote: string;
  promptBlock: string;
  annotatedImageDataUrl?: string;
}

// Precomputed IEEE 802.3 CRC32 lookup table for PNG chunk checksums
const CRC32_TABLE = (() => {
  const table = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    }
    table[n] = c >>> 0;
  }
  return table;
})();

function crc32(buf: Buffer): number {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    c = CRC32_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function makePngChunk(type: string, data: Buffer): Buffer {
  const len = data.length;
  const out = Buffer.alloc(4 + 4 + len + 4);
  out.writeUInt32BE(len, 0);
  out.write(type, 4, 4, "ascii");
  if (len > 0) {
    data.copy(out, 8);
  }
  const crcVal = crc32(out.subarray(4, 8 + len));
  out.writeUInt32BE(crcVal, 8 + len);
  return out;
}

function paethPredictor(a: number, b: number, c: number): number {
  const p = a + b - c;
  const pa = Math.abs(p - a);
  const pb = Math.abs(p - b);
  const pc = Math.abs(p - c);
  if (pa <= pb && pa <= pc) return a;
  if (pb <= pc) return b;
  return c;
}

/**
 * Pure-Node PNG decoder supporting 8-bit RGB, RGBA, Grayscale, Grayscale+Alpha, and Indexed-color PNGs.
 */
export function decodePngToRgba(png: Buffer): DecodedImage | null {
  try {
    if (
      png.length < 33 ||
      png[0] !== 0x89 ||
      png[1] !== 0x50 ||
      png[2] !== 0x4e ||
      png[3] !== 0x47 ||
      png[4] !== 0x0d ||
      png[5] !== 0x0a ||
      png[6] !== 0x1a ||
      png[7] !== 0x0a
    ) {
      return null;
    }

    let offset = 8;
    let width = 0;
    let height = 0;
    let bitDepth = 0;
    let colorType = 0;
    let interlace = 0;
    let palette: Buffer | null = null;
    let trns: Buffer | null = null;
    const idatParts: Buffer[] = [];

    while (offset + 8 <= png.length) {
      const length = png.readUInt32BE(offset);
      const type = png.toString("ascii", offset + 4, offset + 8);
      const dataStart = offset + 8;
      const dataEnd = dataStart + length;
      if (dataEnd + 4 > png.length) break;

      const chunkData = png.subarray(dataStart, dataEnd);
      if (type === "IHDR" && length >= 13) {
        width = chunkData.readUInt32BE(0);
        height = chunkData.readUInt32BE(4);
        bitDepth = chunkData[8];
        colorType = chunkData[9];
        interlace = chunkData[12];
      } else if (type === "PLTE") {
        palette = chunkData;
      } else if (type === "tRNS") {
        trns = chunkData;
      } else if (type === "IDAT") {
        idatParts.push(chunkData);
      } else if (type === "IEND") {
        break;
      }

      offset = dataEnd + 4;
    }

    if (
      width <= 0 ||
      height <= 0 ||
      width * height > 16_000_000 ||
      bitDepth !== 8 ||
      interlace !== 0 ||
      idatParts.length === 0
    ) {
      return null;
    }

    const bytesPerPixel =
      colorType === 6
        ? 4
        : colorType === 2
          ? 3
          : colorType === 4
            ? 2
            : colorType === 0 || colorType === 3
              ? 1
              : 0;
    if (bytesPerPixel === 0) return null;

    const raw = zlib.inflateSync(Buffer.concat(idatParts));
    const stride = width * bytesPerPixel;
    if (raw.length < height * (1 + stride)) return null;

    const recon = new Uint8Array(height * stride);
    for (let y = 0; y < height; y++) {
      const filter = raw[y * (1 + stride)];
      const rowInOffset = y * (1 + stride) + 1;
      const rowOutOffset = y * stride;
      const prevRowOutOffset = (y - 1) * stride;

      for (let i = 0; i < stride; i++) {
        const xByte = raw[rowInOffset + i];
        const a = i >= bytesPerPixel ? recon[rowOutOffset + i - bytesPerPixel] : 0;
        const b = y > 0 ? recon[prevRowOutOffset + i] : 0;
        const c =
          y > 0 && i >= bytesPerPixel
            ? recon[prevRowOutOffset + i - bytesPerPixel]
            : 0;

        let val = xByte;
        if (filter === 1) {
          val = (xByte + a) & 0xff;
        } else if (filter === 2) {
          val = (xByte + b) & 0xff;
        } else if (filter === 3) {
          val = (xByte + ((a + b) >> 1)) & 0xff;
        } else if (filter === 4) {
          val = (xByte + paethPredictor(a, b, c)) & 0xff;
        }
        recon[rowOutOffset + i] = val;
      }
    }

    const rgba = new Uint8Array(width * height * 4);
    if (colorType === 6) {
      rgba.set(recon);
    } else if (colorType === 2) {
      for (let p = 0, s = 0, d = 0; p < width * height; p++, s += 3, d += 4) {
        rgba[d] = recon[s];
        rgba[d + 1] = recon[s + 1];
        rgba[d + 2] = recon[s + 2];
        rgba[d + 3] = 255;
      }
    } else if (colorType === 4) {
      for (let p = 0, s = 0, d = 0; p < width * height; p++, s += 2, d += 4) {
        const g = recon[s];
        rgba[d] = g;
        rgba[d + 1] = g;
        rgba[d + 2] = g;
        rgba[d + 3] = recon[s + 1];
      }
    } else if (colorType === 0) {
      for (let p = 0, d = 0; p < width * height; p++, d += 4) {
        const g = recon[p];
        rgba[d] = g;
        rgba[d + 1] = g;
        rgba[d + 2] = g;
        rgba[d + 3] = 255;
      }
    } else if (colorType === 3 && palette) {
      for (let p = 0, d = 0; p < width * height; p++, d += 4) {
        const idx = recon[p];
        const pOff = idx * 3;
        rgba[d] = pOff + 2 < palette.length ? palette[pOff] : 0;
        rgba[d + 1] = pOff + 2 < palette.length ? palette[pOff + 1] : 0;
        rgba[d + 2] = pOff + 2 < palette.length ? palette[pOff + 2] : 0;
        rgba[d + 3] = trns && idx < trns.length ? trns[idx] : 255;
      }
    } else {
      return null;
    }

    return { width, height, data: rgba };
  } catch {
    return null;
  }
}

/**
 * Decodes either a PNG or JPEG image buffer into an RGBA `DecodedImage`.
 */
export function decodeImageToRgba(buf: Buffer): DecodedImage | null {
  if (!buf || buf.length < 8) return null;
  if (
    buf[0] === 0x89 &&
    buf[1] === 0x50 &&
    buf[2] === 0x4e &&
    buf[3] === 0x47
  ) {
    return decodePngToRgba(buf);
  }
  try {
    const decoded = jpeg.decode(buf, {
      useTArray: true,
      maxResolutionInMP: 16,
      maxMemoryUsageInMB: 128,
    });
    if (
      !decoded ||
      !decoded.width ||
      !decoded.height ||
      !decoded.data ||
      decoded.data.length < decoded.width * decoded.height * 4
    ) {
      return null;
    }
    return {
      width: decoded.width,
      height: decoded.height,
      data:
        decoded.data instanceof Uint8Array
          ? decoded.data
          : new Uint8Array(decoded.data),
    };
  } catch {
    return null;
  }
}

/**
 * Pure-Node PNG encoder (`IHDR`, `IDAT`, `IEND` with CRC32 and filter byte `0` (None) per scanline).
 */
export function encodeRgbaToPng(img: DecodedImage): Buffer {
  const { width, height, data } = img;
  const rowBytes = width * 4;
  const rawScanlines = Buffer.alloc((rowBytes + 1) * height);

  for (let y = 0; y < height; y++) {
    const dstOffset = y * (rowBytes + 1);
    rawScanlines[dstOffset] = 0; // Filter type 0 (None)
    const srcOffset = y * rowBytes;
    Buffer.from(data.buffer, data.byteOffset + srcOffset, rowBytes).copy(
      rawScanlines,
      dstOffset + 1
    );
  }

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; // Bit depth: 8 bits per channel
  ihdr[9] = 6; // Color type: 6 (RGBA)
  ihdr[10] = 0; // Compression method: 0 (deflate)
  ihdr[11] = 0; // Filter method: 0 (adaptive)
  ihdr[12] = 0; // Interlace method: 0 (no interlace)

  const compressed = zlib.deflateSync(rawScanlines, { level: 1 });
  const signature = Buffer.from([
    0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
  ]);

  return Buffer.concat([
    signature,
    makePngChunk("IHDR", ihdr),
    makePngChunk("IDAT", compressed),
    makePngChunk("IEND", Buffer.alloc(0)),
  ]);
}

/**
 * Fast JPEG encoder using `jpeg-js` (produces ~130 KB for 1280x720 vs ~1.1 MB for PNG).
 */
export function encodeRgbaToJpeg(img: DecodedImage, quality = 82): Buffer {
  const encoded = jpeg.encode(
    { data: img.data, width: img.width, height: img.height },
    quality
  );
  return Buffer.isBuffer(encoded.data)
    ? encoded.data
    : Buffer.from(encoded.data);
}

/**
 * Normalizes an RGBA `DecodedImage` into ClickyDraw's `1280x720` canvas coordinate space
 * whenever the input dimensions exceed `1280x720` (cloning if already `<= 1280x720`).
 */
export function downsampleDecodedImageToCanvas(
  decoded: DecodedImage,
  targetW = 1280,
  targetH = 720
): DecodedImage {
  const origW = decoded.width;
  const origH = decoded.height;
  if (origW <= targetW && origH <= targetH) {
    return {
      width: origW,
      height: origH,
      data: new Uint8Array(decoded.data),
    };
  }
  const out = new Uint8Array(targetW * targetH * 4);
  const src = decoded.data;
  for (let y = 0; y < targetH; y++) {
    const sy = Math.min(origH - 1, Math.floor((y * origH) / targetH));
    const srcRow = sy * origW;
    const dstRow = y * targetW;
    for (let x = 0; x < targetW; x++) {
      const sx = Math.min(origW - 1, Math.floor((x * origW) / targetW));
      const sIdx = (srcRow + sx) * 4;
      const dIdx = (dstRow + x) * 4;
      out[dIdx] = src[sIdx];
      out[dIdx + 1] = src[sIdx + 1];
      out[dIdx + 2] = src[sIdx + 2];
      out[dIdx + 3] = 255;
    }
  }
  return { width: targetW, height: targetH, data: out };
}

// Built-in 3x5 bitmap digit font for crisp numeric pixel labels along the top and left rulers
const DIGIT_FONT_3X5: Record<string, number[]> = {
  "0": [0b111, 0b101, 0b101, 0b101, 0b111],
  "1": [0b010, 0b110, 0b010, 0b010, 0b111],
  "2": [0b111, 0b001, 0b111, 0b100, 0b111],
  "3": [0b111, 0b001, 0b111, 0b001, 0b111],
  "4": [0b101, 0b101, 0b111, 0b001, 0b001],
  "5": [0b111, 0b100, 0b111, 0b001, 0b111],
  "6": [0b111, 0b100, 0b111, 0b101, 0b111],
  "7": [0b111, 0b001, 0b001, 0b001, 0b001],
  "8": [0b111, 0b101, 0b111, 0b101, 0b111],
  "9": [0b111, 0b101, 0b111, 0b001, 0b111],
};

function blendPixel(
  data: Uint8Array,
  width: number,
  height: number,
  x: number,
  y: number,
  r: number,
  g: number,
  b: number,
  alpha: number
): void {
  if (x < 0 || x >= width || y < 0 || y >= height) return;
  const idx = (y * width + x) * 4;
  const inv = 1 - alpha;
  data[idx] = Math.round(data[idx] * inv + r * alpha);
  data[idx + 1] = Math.round(data[idx + 1] * inv + g * alpha);
  data[idx + 2] = Math.round(data[idx + 2] * inv + b * alpha);
  data[idx + 3] = 255;
}

export function drawBitmapNumber(
  img: DecodedImage,
  text: string,
  startX: number,
  startY: number,
  r: number,
  g: number,
  b: number,
  alpha = 1,
  scale = 2
): void {
  const { data, width, height } = img;
  const s = Math.max(1, Math.floor(scale));
  let cursorX = startX;
  for (let i = 0; i < text.length; i++) {
    const glyph = DIGIT_FONT_3X5[text[i]];
    if (glyph) {
      for (let row = 0; row < 5; row++) {
        const bits = glyph[row];
        for (let col = 0; col < 3; col++) {
          if ((bits >> (2 - col)) & 1) {
            for (let dy = 0; dy < s; dy++) {
              for (let dx = 0; dx < s; dx++) {
                blendPixel(
                  data,
                  width,
                  height,
                  cursorX + col * s + dx,
                  startY + row * s + dy,
                  r,
                  g,
                  b,
                  alpha
                );
              }
            }
          }
        }
      }
    }
    cursorX += 4 * s;
  }
}

/**
 * Renders the pixel coordinate ruler (`topBarH = 22`, `leftBarW = 44`), `2x`-scaled (`6x10` px) numeric
 * labels every `100px`, `2px`-thick bright cyan major ticks, and `5x5` `+` grid crosshairs at `0.45` alpha
 * in-place onto an RGBA `DecodedImage`.
 */
export function renderRulerOnRgba(img: DecodedImage): void {
  const { width, height, data: rgba } = img;
  const scale = 2;
  const topBarH = Math.min(22, height); // y = 0..21
  const leftBarW = Math.min(44, width); // x = 0..43

  // 1. 5x5 `+` crosshair dots at every (100*m, 100*n) intersection across the canvas (45% alpha)
  for (let gy = 100; gy < height; gy += 100) {
    for (let gx = 100; gx < width; gx += 100) {
      if (gx <= leftBarW || gy <= topBarH) continue;
      const centerIdx = (gy * width + gx) * 4;
      const bgLuma =
        0.299 * rgba[centerIdx] +
        0.587 * rgba[centerIdx + 1] +
        0.114 * rgba[centerIdx + 2];
      const cr = bgLuma > 128 ? 16 : 0;
      const cg = bgLuma > 128 ? 90 : 225;
      const cb = bgLuma > 128 ? 150 : 255;
      for (let dx = -2; dx <= 2; dx++) {
        blendPixel(rgba, width, height, gx + dx, gy, cr, cg, cb, 0.45);
      }
      for (let dy = -2; dy <= 2; dy++) {
        if (dy === 0) continue;
        blendPixel(rgba, width, height, gx, gy + dy, cr, cg, cb, 0.45);
      }
    }
  }

  // 2. Semi-transparent dark ruler bar along the top edge (y = 0..21) and left edge (x = 0..43)
  for (let y = 0; y < topBarH; y++) {
    for (let x = 0; x < width; x++) {
      blendPixel(rgba, width, height, x, y, 12, 18, 28, 0.78);
    }
  }
  for (let y = topBarH; y < height; y++) {
    for (let x = 0; x < leftBarW; x++) {
      blendPixel(rgba, width, height, x, y, 12, 18, 28, 0.78);
    }
  }

  // Subtle 1px border line along the inside edge of the top and left rulers
  if (topBarH - 1 >= 0) {
    for (let x = 0; x < width; x++) {
      blendPixel(rgba, width, height, x, topBarH - 1, 0, 210, 255, 0.55);
    }
  }
  if (leftBarW - 1 >= 0) {
    for (let y = 0; y < height; y++) {
      blendPixel(rgba, width, height, leftBarW - 1, y, 0, 210, 255, 0.55);
    }
  }

  // 3. Top ruler ticks every 50px (2px-thick bright cyan for major 100px ticks) and 2x-scaled numeric labels every 100px
  for (let x = 50; x < width; x += 50) {
    const isMajor = x % 100 === 0;
    const tickLen = isMajor ? 6 : 4;
    const tickThickness = isMajor ? 2 : 1;
    for (let dx = 0; dx < tickThickness; dx++) {
      for (let ty = topBarH - tickLen; ty < topBarH; ty++) {
        blendPixel(
          rgba,
          width,
          height,
          x + dx,
          ty,
          isMajor ? 0 : 200,
          isMajor ? 240 : 220,
          255,
          1.0
        );
      }
    }
    if (isMajor) {
      const label = String(x);
      const textW = label.length * (4 * scale) - scale;
      const labelX = Math.max(
        leftBarW + 2,
        Math.min(width - textW - 2, x - Math.floor(textW / 2))
      );
      drawBitmapNumber(img, label, labelX, 3, 255, 255, 255, 1.0, scale);
    }
  }

  // 4. Left ruler ticks every 50px (2px-thick bright cyan for major 100px ticks) and 2x-scaled numeric labels every 100px
  for (let y = 50; y < height; y += 50) {
    const isMajor = y % 100 === 0;
    const tickLen = isMajor ? 6 : 4;
    const tickThickness = isMajor ? 2 : 1;
    for (let dy = 0; dy < tickThickness; dy++) {
      for (let tx = leftBarW - tickLen; tx < leftBarW; tx++) {
        blendPixel(
          rgba,
          width,
          height,
          tx,
          y + dy,
          isMajor ? 0 : 200,
          isMajor ? 240 : 220,
          255,
          1.0
        );
      }
    }
    if (isMajor) {
      const label = String(y);
      const textW = label.length * (4 * scale) - scale;
      const textH = 5 * scale;
      const labelX = Math.max(2, leftBarW - tickLen - textW - 3);
      const labelY = Math.max(
        topBarH + 2,
        Math.min(height - textH - 2, y - Math.floor(textH / 2))
      );
      drawBitmapNumber(img, label, labelX, labelY, 255, 255, 255, 1.0, scale);
    }
  }
}

/**
 * Normalizes `decoded` to ClickyDraw's `1280x720` coordinate canvas when larger than `1280x720`,
 * draws the visual coordinate ruler via `renderRulerOnRgba`,
 * and encodes the result as a compact `data:image/jpeg;base64,...` data URL.
 */
export function annotateDecodedImageWithRuler(
  decoded: DecodedImage
): string | null {
  try {
    if (
      !decoded ||
      !decoded.width ||
      !decoded.height ||
      !decoded.data ||
      decoded.data.length < decoded.width * decoded.height * 4
    ) {
      return null;
    }
    const imgCopy = downsampleDecodedImageToCanvas(decoded, 1280, 720);
    renderRulerOnRgba(imgCopy);
    const encodedJpeg = encodeRgbaToJpeg(imgCopy, 82);
    return `data:image/jpeg;base64,${encodedJpeg.toString("base64")}`;
  } catch {
    return null;
  }
}

/**
 * Accepts either a screenshot data URL / raw base64 string OR an already-decoded `DecodedImage`,
 * normalizes to `1280x720` if larger, draws the pixel ruler (`topBarH = 22`, `leftBarW = 44`, `2x`-scaled `6x10` digits, `5x5` `+` crosshairs),
 * and returns the re-encoded JPEG as a `data:image/jpeg;base64,...` data URL (or `null` if decoding fails).
 */
export function annotateScreenshotWithRuler(
  input: string | DecodedImage
): string | null {
  try {
    if (!input) return null;
    if (typeof input !== "string") {
      return annotateDecodedImageWithRuler(input);
    }
    const base64 = input
      .trim()
      .replace(/^data:image\/[a-zA-Z0-9.+-]+;base64,/, "");
    if (!base64) return null;
    const buf = Buffer.from(base64, "base64");
    if (buf.length === 0 || buf.length > 8 * 1024 * 1024) return null;

    const decoded = decodeImageToRgba(buf);
    if (!decoded) return null;

    return annotateDecodedImageWithRuler(decoded);
  } catch {
    return null;
  }
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

/**
 * Detects worksheet dotted (`............`), dashed (`-------`), and solid (`________`) answer lines
 * across the `1280x720` canvas while rejecting normal printed text rows, colored UI overlays, and table borders.
 */
function detectWorksheetAnswerLines(
  W: number,
  H: number,
  tables: DetectedTable[],
  lumaArr: Float64Array,
  chromaArr: Uint8Array
): ScreenBlankLine[] {
  interface RawLineSegment {
    y: number;
    x0: number;
    x1: number;
    hits: number;
    transitions: number;
    kind: "dotted" | "solid" | "dashed";
  }

  const rowCandidates: RawLineSegment[] = [];

  const isInsideTable = (y: number): boolean => {
    for (const t of tables) {
      if (y >= t.y - 8 && y <= t.y + t.h + 8) return true;
    }
    return false;
  };

  for (let y = 165; y <= H - 10; y++) {
    if (isInsideTable(y)) continue;

    // Collect contiguous horizontal dot/dash/line segments on row `y`
    const segmentsOnRow: Array<{
      x0: number;
      x1: number;
      hits: number;
      transitions: number;
      cleanCount: number;
    }> = [];

    let segStart = -1;
    let lastInkX = -1;
    let hits = 0;
    let transitions = 0;
    let cleanCount = 0;
    let nonCleanStreak = 0;
    let prevInk = false;

    const flushSegment = () => {
      if (segStart >= 0 && lastInkX > segStart) {
        const span = lastInkX - segStart + 1;
        if (span >= 45 && cleanCount >= span * 0.78) {
          segmentsOnRow.push({
            x0: segStart,
            x1: lastInkX,
            hits,
            transitions,
            cleanCount,
          });
        }
      }
      segStart = -1;
      lastInkX = -1;
      hits = 0;
      transitions = 0;
      cleanCount = 0;
      nonCleanStreak = 0;
      prevInk = false;
    };

    const rowY = y * W;
    const rowYm1 = clampCoord(y - 1, H) * W;
    const rowYp1 = clampCoord(y + 1, H) * W;
    const rowYm4 = clampCoord(y - 4, H) * W;
    const rowYp4 = clampCoord(y + 4, H) * W;
    const rowYm5 = clampCoord(y - 5, H) * W;
    const rowYp5 = clampCoord(y + 5, H) * W;
    const rowYm6 = clampCoord(y - 6, H) * W;
    const rowYp6 = clampCoord(y + 6, H) * W;

    for (let x = 120; x < W - 40; x++) {
      const lumYm4 = lumaArr[rowYm4 + x];
      const lumYm6 = lumaArr[rowYm6 + x];
      const lumYp4 = lumaArr[rowYp4 + x];
      const lumYp6 = lumaArr[rowYp6 + x];
      const lumYm5 = lumaArr[rowYm5 + x];
      const lumYp5 = lumaArr[rowYp5 + x];
      const bgAbove = lumYm4 < lumYm6 ? lumYm4 : lumYm6;
      const bgBelow = lumYp4 < lumYp6 ? lumYp4 : lumYp6;
      const cleanPaper =
        bgAbove > 182 &&
        bgBelow > 182 &&
        Math.abs(lumYm5 - lumYp5) <= 22;
      const bgLum = 0.5 * (lumYm5 + lumYp5);
      const lumYm1 = lumaArr[rowYm1 + x];
      const lumY = lumaArr[rowY + x];
      const lumYp1 = lumaArr[rowYp1 + x];
      const minMid =
        lumYm1 < lumY
          ? lumYm1 < lumYp1
            ? lumYm1
            : lumYp1
          : lumY < lumYp1
            ? lumY
            : lumYp1;
      const dotContrast = bgLum - minMid;
      const isNeutralInk =
        chromaArr[rowY + x] <= 38 &&
        chromaArr[rowYm1 + x] <= 42 &&
        chromaArr[rowYp1 + x] <= 42;
      const isDotInk = cleanPaper && dotContrast >= 18 && isNeutralInk;

      if (!cleanPaper) {
        nonCleanStreak++;
      } else {
        nonCleanStreak = 0;
      }

      if (isDotInk) {
        if (segStart < 0) {
          segStart = x;
          lastInkX = x;
          hits = 1;
          transitions = 1;
          cleanCount = 1;
          prevInk = true;
        } else {
          if (!prevInk) transitions++;
          prevInk = true;
          lastInkX = x;
          hits++;
          cleanCount++;
        }
      } else if (segStart >= 0) {
        if (prevInk) {
          transitions++;
          prevInk = false;
        }
        if (cleanPaper) cleanCount++;
        const gapFromLastInk = x - lastInkX;
        if (gapFromLastInk > 12 || nonCleanStreak > 3) {
          flushSegment();
        }
      }
    }
    flushSegment();

    if (segmentsOnRow.length === 0) continue;

    // Filter valid sub-segments (either dotted pattern or solid underline)
    const validSubSegments = segmentsOnRow.filter((s) => {
      const span = s.x1 - s.x0 + 1;
      const isDotted =
        s.transitions >= Math.max(6, Math.floor(span * 0.04)) &&
        s.hits >= span * 0.1 &&
        s.hits <= span * 0.72;
      const isSolid =
        span >= 140 && s.hits >= span * 0.75 && s.transitions <= 12;
      return isDotted || isSolid;
    });

    if (validSubSegments.length === 0) continue;

    // Merge collinear segments on the same row separated by <= 140px (e.g., interrupted by a watermark stamp)
    const mergedRowSegs = [{ ...validSubSegments[0] }];
    for (let i = 1; i < validSubSegments.length; i++) {
      const prev = mergedRowSegs[mergedRowSegs.length - 1];
      const cur = validSubSegments[i];
      if (cur.x0 - prev.x1 <= 140) {
        prev.x1 = cur.x1;
        prev.hits += cur.hits;
        prev.transitions += cur.transitions;
        prev.cleanCount += cur.cleanCount;
      } else {
        mergedRowSegs.push({ ...cur });
      }
    }

    for (const seg of mergedRowSegs) {
      const span = seg.x1 - seg.x0;
      if (span < 180 || seg.x0 >= 620) continue;
      const isDotted =
        seg.transitions >= 14 &&
        seg.hits >= span * 0.1 &&
        seg.hits <= span * 0.72;
      const isSolid =
        y < H - 25 && seg.hits >= span * 0.76 && seg.transitions <= 12;
      if (isDotted || isSolid) {
        rowCandidates.push({
          y,
          x0: seg.x0,
          x1: seg.x1,
          hits: seg.hits,
          transitions: seg.transitions,
          kind: isSolid ? "solid" : "dotted",
        });
      }
    }
  }

  if (rowCandidates.length === 0) return [];

  // Cluster detected line candidates vertically (within 14px of cluster start y and 9px of previous y)
  rowCandidates.sort((a, b) => a.y - b.y);
  const clusters: RawLineSegment[][] = [[rowCandidates[0]]];
  for (let i = 1; i < rowCandidates.length; i++) {
    const cur = rowCandidates[i];
    const lastCluster = clusters[clusters.length - 1];
    const firstInCluster = lastCluster[0];
    const prev = lastCluster[lastCluster.length - 1];
    const clusterMinX0 = Math.min(...lastCluster.map((c) => c.x0));
    const clusterMaxX1 = Math.max(...lastCluster.map((c) => c.x1));
    const overlapX =
      Math.min(cur.x1, clusterMaxX1) - Math.max(cur.x0, clusterMinX0);
    const gapX =
      Math.max(cur.x0, clusterMinX0) - Math.min(cur.x1, clusterMaxX1);
    if (
      cur.y - firstInCluster.y <= 14 &&
      cur.y - prev.y <= 9 &&
      (overlapX > 40 || gapX <= 260)
    ) {
      lastCluster.push(cur);
    } else {
      clusters.push([cur]);
    }
  }

  const prelimLines: Array<{
    x0: number;
    rawX0: number;
    x1: number;
    y: number;
    kind: "dotted" | "solid" | "dashed";
  }> = [];

  for (const cluster of clusters) {
    let bestRow = cluster[0];
    let bestScore = -1;
    for (const r of cluster) {
      const score = (r.x1 - r.x0) * r.hits;
      if (score > bestScore) {
        bestScore = score;
        bestRow = r;
      }
    }
    const minX0 = Math.min(...cluster.map((c) => c.x0));
    const maxX1 = Math.max(...cluster.map((c) => c.x1));
    if (maxX1 - minX0 >= 180 && minX0 < 620) {
      prelimLines.push({
        x0: minX0,
        rawX0: minX0,
        x1: maxX1,
        y: bestRow.y,
        kind: bestRow.kind,
      });
    }
  }

  // Deduplicate/consolidate any two prelimLines within 18px vertically (e.g. tilted dotted lines across a page scan)
  prelimLines.sort((a, b) => a.y - b.y);
  const consolidatedLines: typeof prelimLines = [];
  for (const line of prelimLines) {
    const prev = consolidatedLines[consolidatedLines.length - 1];
    if (prev && Math.abs(line.y - prev.y) <= 18) {
      const wA = Math.max(1, prev.x1 - prev.x0);
      const wB = Math.max(1, line.x1 - line.x0);
      prev.y = Math.round((prev.y * wA + line.y * wB) / (wA + wB));
      prev.x0 = Math.min(prev.x0, line.x0);
      prev.rawX0 = prev.x0;
      prev.x1 = Math.max(prev.x1, line.x1);
    } else {
      consolidatedLines.push({ ...line });
    }
  }
  prelimLines.length = 0;
  prelimLines.push(...consolidatedLines);

  // A single isolated solid line on a page (with 0 dotted/dashed lines) is ALWAYS a math fraction bar,
  // a page header/footer rule (like the rule under 9709/31/O/N/10), or a UI/card border — never a worksheet answer slot!
  if (prelimLines.length === 1 && prelimLines[0].kind === "solid") {
    return [];
  }

  // Left-Edge (`x0`) and Right-Edge (`x1`) Alignment Restoration Passes
  if (prelimLines.length >= 1) {
    const candidateRightEdges = prelimLines
      .map((l) => l.x1)
      .filter((x1) => x1 >= 850 && x1 <= 1180);
    for (const t of tables) {
      const tRight = t.x + t.w;
      if (tRight >= 850 && tRight <= 1180) {
        candidateRightEdges.push(tRight);
      }
    }
    candidateRightEdges.sort((a, b) => b - a);
    const sortedRightEdges = prelimLines.map((l) => l.x1).sort((a, b) => b - a);
    const topRightRef =
      candidateRightEdges.length > 0
        ? candidateRightEdges[0]
        : sortedRightEdges[Math.min(1, sortedRightEdges.length - 1)];

    if (prelimLines.length >= 2) {
      // Pre-OCR Left-Edge (`x0`) Alignment Restoration: restore genuinely mid-line broken segments
      // or clamp slight thumbprint/smudge left-overshoots without overwriting printed left-margin label offsets.
      const leftMarginCandidates = prelimLines
        .map((l) => l.rawX0)
        .filter((x0) => x0 >= 240 && x0 <= 440)
        .sort((a, b) => a - b);
      const sortedLeftEdges = prelimLines
        .map((l) => l.rawX0)
        .sort((a, b) => a - b);
      const leftRef =
        leftMarginCandidates.length > 0
          ? leftMarginCandidates[Math.min(1, leftMarginCandidates.length - 1)]
          : sortedLeftEdges[Math.min(1, sortedLeftEdges.length - 1)];
      const alignedLeftCount = prelimLines.filter(
        (l) => l.rawX0 <= leftRef + 28
      ).length;
      if (alignedLeftCount >= 1 && leftRef >= 240 && leftRef <= 440) {
        const brokenMidLineStart = Math.max(430, leftRef + 95);
        for (const l of prelimLines) {
          const effectiveX1 = Math.max(l.x1, topRightRef);
          if (
            (l.x0 > brokenMidLineStart ||
              (alignedLeftCount >= 2 &&
                l.x0 < leftRef - 6 &&
                leftRef - l.x0 <= 22)) &&
            effectiveX1 - leftRef >= 300
          ) {
            l.x0 = leftRef;
          }
        }
      }
    }

    // Right-Edge (`x1`) Alignment Restoration:
    // Whenever ANY worksheet line or table on the page reaches the right margin (`850..1180`),
    // restore faded right-hand dotted line endings in the same column (`x0 <= 520, x1 >= 650`).
    const alignedRightCount =
      prelimLines.filter((l) => Math.abs(l.x1 - topRightRef) <= 28).length +
      tables.filter((t) => Math.abs(t.x + t.w - topRightRef) <= 28).length;
    if (
      (alignedRightCount >= 1 && topRightRef >= 850 && topRightRef <= 1180) ||
      alignedRightCount >= 3
    ) {
      for (const l of prelimLines) {
        if (
          l.x0 <= 520 &&
          l.x1 >= 650 &&
          l.x1 < topRightRef &&
          topRightRef - l.x0 >= 300
        ) {
          l.x1 = topRightRef;
        }
      }
    }
  }

  prelimLines.sort((a, b) => a.y - b.y);

  return prelimLines.map((l, idx) => {
    const writeX = l.x0 + 6;
    const writeY = l.y - 4;
    const w = l.x1 - l.x0;
    const maxWidth = Math.max(140, l.x1 - writeX);
    return {
      lineIndex: idx,
      x0: l.x0,
      rawX0: l.rawX0,
      x1: l.x1,
      x: l.x0,
      y: l.y,
      w,
      writeX,
      writeY,
      maxWidth,
      kind: l.kind,
    };
  });
}

function buildLayoutPromptBlock(
  W: number,
  H: number,
  tables: DetectedTable[],
  allCells: TableCell[],
  answerLines: ScreenBlankLine[],
  bestSpot: { x: number; y: number; w: number; h: number; label: string },
  ocrLines?: OcrLineBox[]
): string {
  const noteLines: string[] = [
    `## Measured Screen Geometry (${W}x${H} exact canvas pixels)`,
    `- **Pixel Coordinate Ruler**: If the screenshot includes pixel ruler ticks (\`100, 200, ...\`) along the top (\`x\`) and left (\`y\`) edges (plus subtle \`100x100\` grid crosshairs), read exact \`(x, y)\` coordinates directly from those ruler ticks.`,
  ];

  const nonOrphanAnswerLines = answerLines.filter(
    (l) => !l.isScrolledOffTopOrphan
  );
  const activeAnswerLines =
    nonOrphanAnswerLines.length > 0 || tables.length > 0
      ? nonOrphanAnswerLines
      : answerLines;

  // Emit Multi-Part Question Summary when a question has BOTH a DetectedTable (e.g. Part (a)) AND answerLines (e.g. Part (b))
  if (tables.length > 0 && activeAnswerLines.length > 0) {
    const t0 = tables[0];
    const sharedQ =
      t0.questionNumber ||
      activeAnswerLines.find((l) => l.questionNumber)?.questionNumber;
    const matchingSlots = sharedQ
      ? activeAnswerLines.filter(
          (l) => !l.questionNumber || l.questionNumber === sharedQ
        )
      : activeAnswerLines;
    if (matchingSlots.length > 0) {
      const dataRowsCount = t0.rows.filter((r) => !r.isHeader).length;
      const ansCols = t0.cols.length >= 2 ? t0.cols.slice(1) : t0.cols;
      const ansColDesc = ansCols
        .map(
          (c) =>
            `Col ${c.colIndex + 1}${c.headerText ? ` "${c.headerText}"` : ""} cx=${c.cx}`
        )
        .join(", ");
      const partALabel = t0.subPart ? `Part ${t0.subPart}` : "Part (a)";
      const partBSub =
        matchingSlots.find((s) => s.subPart)?.subPart || "(b)";
      const partBLabel = `Part ${partBSub}`;
      const partBPrompt =
        matchingSlots.find((s) => s.subPartPrompt)?.subPartPrompt || "";
      const firstSlotIdx = activeAnswerLines.indexOf(matchingSlots[0]) + 1;
      const lastSlotIdx =
        activeAnswerLines.indexOf(matchingSlots[matchingSlots.length - 1]) + 1;
      const firstWriteY = matchingSlots[0].writeY;
      const lastWriteY = matchingSlots[matchingSlots.length - 1].writeY;
      noteLines.push(
        `- **MULTI-PART QUESTION STRUCTURE${sharedQ ? ` (Question ${sharedQ})` : ""}**:`
      );
      noteLines.push(
        `  - **${partALabel}** (y=${t0.y}..${t0.y + t0.h}): Tick-box / Table in **Detected Table/Grid 1** (${dataRowsCount} rows × ${ansCols.length} answer columns: ${ansColDesc}). Evaluate each row's statement carefully and place **EXACTLY ONE** \`check(cx, cy)\` per row in the single matching column (e.g. cx=${ansCols[0]?.cx} for "${ansCols[0]?.headerText || "Col 2"}" OR cx=${ansCols[1]?.cx} for "${ansCols[1]?.headerText || "Col 3"}" — NEVER tick both columns in the same row)!`
      );
      noteLines.push(
        `  - **${partBLabel}** (y=${matchingSlots[0].y}..${matchingSlots[matchingSlots.length - 1].y})${partBPrompt ? `: "${partBPrompt}"` : ""} -> Fill **SLOT_${firstSlotIdx}..SLOT_${lastSlotIdx}** (y=${firstWriteY}..${lastWriteY}) with \`write(writeX, writeY, "...")\` answering ${partBLabel}!`
      );
      noteLines.push(
        `  - **IMPORTANT**: When asked to solve Question ${sharedQ || "on screen"} (or the whole question), you MUST complete **BOTH ${partALabel}** (using \`check(cx, cy)\` in the table) **AND ${partBLabel}** (using \`write()\` on SLOT_${firstSlotIdx}..SLOT_${lastSlotIdx})! Do NOT write ${partALabel}'s table statements onto ${partBLabel}'s lines!`
      );
    }
  }

  if (allCells.length >= 9) {
    for (let ti = 0; ti < tables.length; ti++) {
      const t = tables[ti];
      if (t.cells.length === 0) continue;
      const hasRowHeaderCol =
        t.cols.length >= 3 && !(t.rows.length === 9 && t.cols.length === 9);
      const formatColHeader = (c: (typeof t.cols)[number]): string => {
        const hdrSuffix = c.headerText ? `: "${c.headerText}"` : "";
        if (!hasRowHeaderCol) {
          return `Col ${c.colIndex + 1}${c.headerText ? ` [${c.headerText}]` : ""} (x=${c.x0}..${c.x1}, **cx=${c.cx}**)`;
        }
        if (c.colIndex === 0) {
          const rowHdrLabel = c.headerText
            ? `Row Header: "${c.headerText}"`
            : `Row Labels / Stage Names`;
          return `Col 1 [${rowHdrLabel}] (x=${c.x0}..${c.x1}, cx=${c.cx})`;
        }
        return `Col ${c.colIndex + 1} [Data Col ${c.colIndex}${hdrSuffix}] (x=${c.x0}..${c.x1}, **cx=${c.cx}**)`;
      };
      const formatCellCoord = (
        c: (typeof t.cols)[number],
        cy: number
      ): string => {
        const hdrTag = c.headerText ? ` "${c.headerText}"` : "";
        if (!hasRowHeaderCol) {
          return `Col ${c.colIndex + 1}${hdrTag}: **(${c.cx}, ${cy})**`;
        }
        if (c.colIndex === 0) {
          return `Col 1 [Row Header]: (${c.cx}, ${cy})`;
        }
        return `Col ${c.colIndex + 1} [Data Col ${c.colIndex}${hdrTag}]: **(${c.cx}, ${cy})**`;
      };

      const qSubMeta =
        t.questionNumber || t.subPart
          ? ` [Q${t.questionNumber ?? ""}${t.subPart ?? ""}]`
          : "";
      noteLines.push(
        `- **Detected Table/Grid ${ti + 1}**${qSubMeta}: frame box \`[x=${t.x}, y=${t.y}, w=${t.w}, h=${t.h}]\` (${t.rows.length} rows × ${t.cols.length} columns, ${t.cells.length} cells)`
      );
      noteLines.push(
        `  - Column X centers: ${t.cols.map(formatColHeader).join(" | ")}`
      );
      let stmtCounter = 0;
      for (const r of t.rows) {
        const role = r.isHeader ? "Header Row" : `Row ${++stmtCounter}`;
        const rowLabelStr = !r.isHeader && r.label ? ` ["${r.label}"]` : "";
        const colCenters = t.cols
          .map((c) => formatCellCoord(c, r.cy))
          .join(" | ");
        noteLines.push(
          `  - **${role}** (y=${r.y0}..${r.y1}, **cy=${r.cy}**)${rowLabelStr}: ${colCenters}`
        );
      }
      if (hasRowHeaderCol) {
        noteLines.push(
          `  - **IMPORTANT**: \`Col 1 [Row Header]\` (cx=${t.cols[0].cx}) contains the pre-printed row labels/stage names — NEVER write cell values or place checks/crosses in \`Col 1\`! Data columns start at \`Col 2 [Data Col 1]\` (cx=${t.cols[1].cx}).`
        );
      }
      noteLines.push(
        `  - For table/grid markings (\`check(cx, cy)\`, \`cross(cx, cy)\`, \`write(cx, cy, ...)\`), use the exact \`(cx, cy)\` cell centers listed above.`
      );
    }
  }

  const hasShiftedDocColumn =
    tables.some((t) => t.x >= 260) ||
    answerLines.some((al) => (al.rawX0 ?? al.x0) >= 260) ||
    Boolean(
      ocrLines?.some(
        (l) =>
          l.x >= 240 &&
          /(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM)\s*\d{2}|\b\d{2}[\/1I|]P\d{2}[\/1I|]Q\d+/i.test(
            l.text
          )
      )
    );
  const minDocTextX = hasShiftedDocColumn ? 220 : 40;

  if (ocrLines && ocrLines.length > 0) {
    const hasMainDocLines = ocrLines.some((l) => l.y >= 40 && l.x < 450);
    const maxAnswerLineX1 =
      answerLines.length > 0
        ? Math.max(...answerLines.map((al) => al.x1))
        : 0;
    noteLines.push(`### [SCREEN OCR TEXT & HEADINGS (1280x720 Canvas)]`);
    for (const l of ocrLines) {
      if (l.y < 40 || l.x < minDocTextX) continue;
      // Exclude OCR noise from scrolled-off top orphan lines or drawn handwriting sitting on unlabeled blank answer lines
      const isOnOrphanOrUnlabeledSlot = answerLines.some(
        (al) =>
          (al.isScrolledOffTopOrphan && Math.abs(l.cy - al.y) <= 14) ||
          (!al.label && Math.abs(l.cy - al.y) <= 15 && l.x >= al.x0 - 5)
      );
      if (isOnOrphanOrUnlabeledSlot) continue;
      const isInsideOrNearTable = tables.some(
        (t) =>
          l.x >= t.x - 20 &&
          l.x <= t.x + t.w + 20 &&
          l.y >= t.y - 20 &&
          l.y <= t.y + t.h + 20
      );
      const isWithinAnswerLineSpan =
        maxAnswerLineX1 > 0 && l.x <= maxAnswerLineX1 + 20;
      // Only filter right-margin overlay card text when it is outside tables/answerLines and x >= 960 (or >= 720 when no wide table/answerLines exist)
      const rightEdgeLimit =
        tables.length > 0 || maxAnswerLineX1 >= 720 ? 960 : 720;
      if (
        hasMainDocLines &&
        !isInsideOrNearTable &&
        !isWithinAnswerLineSpan &&
        l.x >= rightEdgeLimit
      ) {
        continue;
      }
      noteLines.push(
        `- [x=${l.x}, y=${l.y}, w=${l.w}, h=${l.h}, bottomY=${l.bottomY}] "${l.text}"`
      );
    }
  }

  if (activeAnswerLines.length > 0) {
    noteLines.push(`### [FILLABLE ANSWER SLOTS (1280x720 Canvas)]`);
    const emittedMultiPartSummary =
      tables.length > 0 && activeAnswerLines.length > 0;
    const seenSubPartPrompts = new Set<string>();
    const isPairedNameLabel = (lbl?: string): boolean =>
      Boolean(
        lbl &&
          /^(?:Architecture|Category|Type|Term|Name|Component|Register|Bus|Stage|Layer|Protocol|Device|Sensor|Method|Principle|Factor|Feature|Model|Topology|Mode|Technique|Interrupt|Hazard|Threat|Attack|Error|Gate|State|Address|Rule|Unit)\s*\d*$/i.test(
            lbl.trim()
          )
      );
    const isPairedDescLabel = (lbl?: string): boolean =>
      Boolean(
        lbl &&
          /^(?:Description|Explanation|Purpose|Function|Role|Meaning|Definition|Use|Example|Reason|Effect)\s*\d*$/i.test(
            lbl.trim()
          )
      );
    for (let i = 0; i < activeAnswerLines.length; i++) {
      const line = activeAnswerLines[i];
      const nextLine = activeAnswerLines[i + 1];
      const prevLine = i > 0 ? activeAnswerLines[i - 1] : undefined;
      let pairHint = "";
      if (isPairedNameLabel(line.label) && isPairedDescLabel(nextLine?.label)) {
        pairHint = ` [NAME/TERM ONLY — write ONLY the concise name/identifier here, put its description on SLOT_${i + 2}!]`;
      } else if (
        isPairedDescLabel(line.label) &&
        isPairedNameLabel(prevLine?.label)
      ) {
        pairHint = ` [1-LINE DESCRIPTION FOR ${prevLine!.label} — <= 75 chars, do NOT repeat the name prefix!]`;
      }
      const labelTag = line.label
        ? `[label="${line.label}"]${pairHint}`
        : `[continuation line]`;
      const subKey = `${line.questionNumber ?? ""}:${line.subPart ?? ""}`;
      const showSubPrompt =
        !emittedMultiPartSummary &&
        Boolean(line.subPartPrompt) &&
        !seenSubPartPrompts.has(subKey);
      if (showSubPrompt) seenSubPartPrompts.add(subKey);
      const subPromptShort =
        showSubPrompt && line.subPartPrompt
          ? ` — "${line.subPartPrompt.slice(0, 72)}"`
          : "";
      const qTag = line.questionNumber
        ? ` (Q${line.questionNumber}${line.subPart ?? ""}${subPromptShort})`
        : line.subPart
          ? ` (${line.subPart}${subPromptShort})`
          : "";
      noteLines.push(
        `- SLOT_${i + 1} ${labelTag}${qTag}: write(${line.writeX}, ${line.writeY}, "...") (maxW=${line.maxWidth}px, baseline y=${line.y}, x=${line.writeX}..${line.x1})`
      );
    }
    noteLines.push(
      `- **CRITICAL WORKSHEET LINE RULE**: When writing answers on worksheet lines, use the exact \`write(writeX, writeY, "...")\` coordinates from \`[FILLABLE ANSWER SLOTS]\` above! Each \`SLOT_N\` is a **SINGLE HORIZONTAL LINE** (\`max 1 line\`, \`<= 75 characters\`) — NEVER write a multi-sentence or >80-character paragraph into a single \`write()\` call! When a slot labeled \`Architecture N\` / \`Category N\` / \`Term N\` / \`Component N\` / \`Method N\` is immediately followed by a slot labeled \`Description\` / \`Explanation\`, write **ONLY the concise name/identifier** on the first slot (e.g. \`"Multiple Instruction Multiple Data (MIMD)"\`) and write **ONLY the 1-line description** (without repeating the label or name prefix) on the \`Description\` slot below it! Emit ONE \`write()\` call per primary labeled field/item (using a \`(line 2)\` continuation slot only if a single explanation exceeds ~75 characters). Do NOT repeat the printed label or item number inside your text!`
    );
  }

  noteLines.push(
    `- **Optional Suggested Calm Region for \`paper()\` Whiteboard or Card**: \`x=${bestSpot.x}, y=${bestSpot.y}, w=${bestSpot.w}, h=${bestSpot.h}\` (${bestSpot.label}) — use this region ONLY if you choose to open a \`paper()\`, \`receipt()\`, or \`clipping()\` card.`
  );

  return noteLines.join("\n");
}

function clampCoord(v: number, maxExclusive: number): number {
  return v < 0 ? 0 : v >= maxExclusive ? maxExclusive - 1 : v;
}

function isHLinePixelAt(
  W: number,
  H: number,
  lumaArr: Float64Array,
  redArr: Uint8Array,
  x: number,
  y: number
): boolean {
  const cx = clampCoord(x, W);
  const redAbove = redArr[clampCoord(y - 5, H) * W + cx] === 1;
  const redBelow = redArr[clampCoord(y + 5, H) * W + cx] === 1;
  const bgAbove = lumaArr[clampCoord(redAbove ? y - 8 : y - 5, H) * W + cx];
  const bgBelow = lumaArr[clampCoord(redBelow ? y + 8 : y + 5, H) * W + cx];
  if (!redAbove && !redBelow && Math.abs(bgAbove - bgBelow) > 40) {
    return false;
  }
  const bgLum = 0.5 * (bgAbove + bgBelow);
  let maxDiff = 0;
  for (let dy = -2; dy <= 2; dy++) {
    const diff = Math.abs(lumaArr[clampCoord(y + dy, H) * W + cx] - bgLum);
    if (diff > maxDiff) maxDiff = diff;
  }
  return maxDiff >= 26;
}

function isVLineOrRedPixelAt(
  W: number,
  H: number,
  lumaArr: Float64Array,
  redArr: Uint8Array,
  vHitCache: Uint8Array,
  x: number,
  y: number
): boolean {
  const cx = clampCoord(x, W);
  const rowOff = clampCoord(y, H) * W;
  const pIdx = rowOff + cx;
  const cached = vHitCache[pIdx];
  if (cached !== 0) return cached === 2;
  if (redArr[pIdx] === 1) {
    vHitCache[pIdx] = 2;
    return true;
  }
  const redLeft = redArr[rowOff + clampCoord(x - 5, W)] === 1;
  const redRight = redArr[rowOff + clampCoord(x + 5, W)] === 1;
  const bgLeft = lumaArr[rowOff + clampCoord(redLeft ? x - 8 : x - 5, W)];
  const bgRight = lumaArr[rowOff + clampCoord(redRight ? x + 8 : x + 5, W)];
  if (!redLeft && !redRight && Math.abs(bgLeft - bgRight) > 40) {
    vHitCache[pIdx] = 1;
    return false;
  }
  const bgLum = 0.5 * (bgLeft + bgRight);
  let maxDiff = 0;
  for (let dx = -2; dx <= 2; dx++) {
    const diff = Math.abs(lumaArr[rowOff + clampCoord(x + dx, W)] - bgLum);
    if (diff > maxDiff) maxDiff = diff;
  }
  const hit = maxDiff >= 26;
  vHitCache[pIdx] = hit ? 2 : 1;
  return hit;
}

function countConnectingVLinesAt(
  W: number,
  H: number,
  lumaArr: Float64Array,
  redArr: Uint8Array,
  vHitCache: Uint8Array,
  yA: number,
  yB: number,
  x0: number,
  x1: number
): number {
  if (yB - yA < 7 || yB - yA > 140) return 0;
  const vXs: number[] = [];
  for (let x = Math.max(46, x0 - 6); x <= Math.min(W - 20, x1 + 6); x++) {
    let hits = 0;
    let total = 0;
    for (let y = yA + 1; y <= yB - 1; y++) {
      total++;
      if (isVLineOrRedPixelAt(W, H, lumaArr, redArr, vHitCache, x, y)) hits++;
    }
    if (total >= 4 && hits >= total * 0.6) {
      vXs.push(x);
    }
  }
  return clusterPositions(vXs, 4).filter(
    (x, idx, arr) => idx === 0 || x - arr[idx - 1] >= 12
  ).length;
}

export function analyzeScreenLayout(
  imageDataUrl: string
): ScreenLayoutAnalysis | null {
  try {
    const base64 = imageDataUrl.replace(/^data:image\/[a-zA-Z0-9.+-]+;base64,/, "");
    if (!base64) return null;
    const buf = Buffer.from(base64, "base64");
    if (buf.length > 8 * 1024 * 1024) return null;

    const decoded = decodeImageToRgba(buf);
    if (!decoded) return null;

    if (
      !decoded.width ||
      !decoded.height ||
      !decoded.data ||
      decoded.data.length < decoded.width * decoded.height * 4
    ) {
      return null;
    }

    // Normalize to ClickyDraw's 1280x720 coordinate canvas when input exceeds 1280x720
    const canvasImg = downsampleDecodedImageToCanvas(decoded, 1280, 720);
    const W = canvasImg.width;
    const H = canvasImg.height;
    const data = canvasImg.data;
    const totalPixels = W * H;

    // Precompute per-pixel luminance, chroma, and red-highlight masks once (O(W*H))
    const lumaArr = new Float64Array(totalPixels);
    const chromaArr = new Uint8Array(totalPixels);
    const redArr = new Uint8Array(totalPixels);
    for (let p = 0, idx = 0; p < totalPixels; p++, idx += 4) {
      const r = data[idx];
      const g = data[idx + 1];
      const b = data[idx + 2];
      lumaArr[p] = 0.299 * r + 0.587 * g + 0.114 * b;
      const maxC = r > g ? (r > b ? r : b) : g > b ? g : b;
      const minC = r < g ? (r < b ? r : b) : g < b ? g : b;
      chromaArr[p] = maxC - minC;
      const minGB = g < b ? g : b;
      redArr[p] = r > 175 && r - minGB > 45 ? 1 : 0;
    }

    const vHitCache = new Uint8Array(totalPixels); // 0 = uncomputed, 1 = false, 2 = true

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

      for (let x = 46; x < W - 20; x++) {
        if (runStart >= 0 && x % 8 === 0) {
          const bgRef =
            0.5 *
            (lumaArr[clampCoord(curY - 5, H) * W + x] +
              lumaArr[clampCoord(curY + 5, H) * W + x]);
          const diffCur = Math.abs(lumaArr[clampCoord(curY, H) * W + x] - bgRef);
          const diffDown = Math.abs(
            lumaArr[clampCoord(curY + 1, H) * W + x] - bgRef
          );
          const diffUp = Math.abs(
            lumaArr[clampCoord(curY - 1, H) * W + x] - bgRef
          );
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
          isHLinePixelAt(W, H, lumaArr, redArr, x, curY) ||
          (runStart >= 0 && redArr[clampCoord(curY, H) * W + x] === 1);
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
          // Do not bridge across whitespace from a short (<12px) isolated text glyph (e.g. rotated vertical label) into a table border
          if (gap <= 18 && !(hits < 12 && gap >= 4)) continue;
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
          const baseX0 = Math.min(...group.map((g) => g.x0));
          const baseX1 = group[group.length - 1].x1;
          const rightAligned = Math.abs(hLines[j].x1 - baseX1) <= 36;
          const leftAligned = Math.abs(hLines[j].x0 - baseX0) <= 36;
          // Support corner-less header top borders (group.length === 1) whose left edge starts at Col 1 instead of Col 0
          const maxCornerlessOffset = Math.max(
            260,
            Math.round((hLines[j].x1 - hLines[j].x0) * 0.6)
          );
          const isCornerlessHeader =
            group.length === 1 &&
            group[0].x0 > hLines[j].x0 + 20 &&
            group[0].x0 <= hLines[j].x0 + maxCornerlessOffset;
          const sameSpan = rightAligned && (leftAligned || isCornerlessHeader);
          const connectX0 = Math.max(prev.x0, hLines[j].x0);
          const connectX1 = Math.min(prev.x1, hLines[j].x1);
          if (
            sameSpan &&
            dy >= 7 &&
            dy <= 140 &&
            countConnectingVLinesAt(
              W,
              H,
              lumaArr,
              redArr,
              vHitCache,
              prev.y,
              hLines[j].y,
              connectX0,
              connectX1
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
          const bodyGroup =
            group.length >= 2 && group[0].x0 > group[1].x0 + 20
              ? group.slice(1)
              : group;
          const mainX0 = Math.min(...bodyGroup.map((g) => g.x0));
          const mainX1 = Math.round(
            bodyGroup.reduce((s, g) => s + g.x1, 0) / bodyGroup.length
          );
          const sortedBodyX0 = [...bodyGroup.map((g) => g.x0)].sort(
            (a, b) => a - b
          );
          const bodyMedianX0 =
            sortedBodyX0[Math.floor(sortedBodyX0.length / 2)];

          const vCandidates: number[] = [];
          for (
            let x = Math.max(46, mainX0 - 3);
            x <= Math.min(W - 20, mainX1 + 3);
            x++
          ) {
            let hits = 0;
            let total = 0;
            let transitions = 0;
            let prevHit = false;
            for (let y = yMin + 2; y <= yMax - 2; y += 2) {
              total++;
              const hit = isVLineOrRedPixelAt(
                W,
                H,
                lumaArr,
                redArr,
                vHitCache,
                x,
                y
              );
              if (hit !== prevHit) {
                transitions++;
                prevHit = hit;
              }
              if (hit) hits++;
            }
            if (
              total > 6 &&
              hits >= total * 0.72 &&
              transitions <= Math.max(14, Math.floor(total * 0.18))
            ) {
              vCandidates.push(x);
            }
          }

          const xLines = clusterPositions(vCandidates, 4).filter(
            (x, idx, arr) => idx === 0 || x - arr[idx - 1] >= 12
          );

          // Drop any narrow phantom column on the far left (e.g. rotated vertical text outside the table's left wall)
          while (
            xLines.length >= 4 &&
            (xLines[1] - xLines[0] < 24 || bodyMedianX0 >= xLines[1] - 5)
          ) {
            xLines.shift();
          }

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

    // 2. Detect worksheet dotted, dashed, and solid answer lines
    const answerLines: ScreenBlankLine[] = detectWorksheetAnswerLines(
      W,
      H,
      tables,
      lumaArr,
      chromaArr
    );

    // 3. Find the calmest spot for an optional explanation card (paper / clipping / receipt / figure)
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
        const rowOff = clampCoord(y, H) * W;
        for (let x = spot.x; x < Math.min(W - 2, spot.x + spot.w); x += 8) {
          const i1 = (rowOff + clampCoord(x, W)) * 4;
          const i2 = (rowOff + clampCoord(x + 2, W)) * 4;
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

    const clampedW = Math.max(
      1,
      Math.min(bestSpot.w, Math.max(1, W - 32))
    );
    const clampedH = Math.max(
      1,
      Math.min(bestSpot.h, Math.max(1, H - 32))
    );
    const clampedX =
      W >= clampedW + 32
        ? Math.max(16, Math.min(860, W - clampedW - 16, bestSpot.x))
        : Math.max(0, W - clampedW);
    const clampedY =
      H >= clampedH + 32
        ? Math.max(16, Math.min(H - clampedH - 16, bestSpot.y))
        : Math.max(0, H - clampedH);

    bestSpot = {
      x: clampedX,
      y: clampedY,
      w: clampedW,
      h: clampedH,
      label: bestSpot.label,
    };

    const allCells: TableCell[] = tables.flatMap((t) => t.cells);
    const promptBlock = buildLayoutPromptBlock(
      W,
      H,
      tables,
      allCells,
      answerLines,
      bestSpot
    );
    const annotatedImageDataUrl =
      annotateDecodedImageWithRuler(canvasImg) ?? undefined;

    return {
      width: W,
      height: H,
      tables,
      cells: allCells,
      answerLines,
      calmCardSpot: bestSpot,
      layoutPromptNote: promptBlock,
      promptBlock,
      annotatedImageDataUrl,
    };
  } catch {
    return null;
  }
}

/**
 * Enriches a pixel-analyzed `ScreenLayoutAnalysis` with Windows WinRT OCR lines (`OcrScreenResult`):
 * - Filters out top browser chrome (`y < 135`) and pure dot/punctuation noise
 * - Pairs printed field labels (`Description`, `Benefit 1`, `Benefit 2`, `Drawback 1`, `Drawback 2`, etc.) with `layout.answerLines`
 * - Labels unlabeled continuation lines (`"Description (line 2)"`)
 * - Tags each `answerLine` with its nearest preceding `questionNumber`
 * - Rebuilds `layout.layoutPromptNote` and `layout.promptBlock` with `[SCREEN OCR TEXT & HEADINGS]` and `[FILLABLE ANSWER SLOTS]`
 */
export function enrichScreenLayoutWithOcr(
  layout: ScreenLayoutAnalysis,
  ocr: OcrScreenResult | null
): ScreenLayoutAnalysis {
  if (!layout) {
    return layout;
  }
  if (!ocr || !Array.isArray(ocr.lines)) {
    layout.ocrLines = layout.ocrLines ?? [];
    return layout;
  }

  const examCodeRegex =
    /(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM)\s*\d{2}|\b\d{2}[\/1I|]P\d{2}[\/1I|]Q\d+/i;

  let toolbarBottomY = 0;
  let topBarChromeCount = 0;
  let topBarChromeMaxBottomY = 0;
  for (const l of ocr.lines) {
    if (
      l.y < 180 &&
      !examCodeRegex.test(l.text) &&
      /(?:\.pdf\b|\b\d+\s*\/\s*\d+\b|\b\d+%\s*\+?|\bhttps?:\/\/|\bfile:\/\/\/|\bVPN\b|\bExtensions?\b|\bupgrade\s+requires\b|\bInstall\s+Tailwind|\bUdemy\b|\bReact\s+JS\b|\bFile\b.*\bEdit\b)/i.test(
        l.text
      )
    ) {
      toolbarBottomY = Math.max(toolbarBottomY, l.bottomY);
    }
    if (
      l.y < 130 &&
      !examCodeRegex.test(l.text) &&
      !/^\s*\(([a-z]|[ivx]+)\)\s+(?:Use|Using|Find|Show|Hence|Evaluate|Prove|Describe|Explain|State|Outline|Complete|Give|Identify|Calculate|Solve|Express|Differentiate|Integrate)\b/i.test(
        l.text
      ) &&
      !/^\s*\d{1,2}\s*$/.test(l.text.trim()) &&
      (l.x < 210 || l.rightX > 980 || l.y < 85)
    ) {
      topBarChromeCount++;
      topBarChromeMaxBottomY = Math.max(topBarChromeMaxBottomY, l.bottomY);
    }
  }
  const hasBrowserChrome = toolbarBottomY > 0 || topBarChromeCount >= 2;
  const minDocY = hasBrowserChrome
    ? Math.max(155, toolbarBottomY + 4, topBarChromeMaxBottomY + 4)
    : 20;

  const rawDocOcrLines: OcrLineBox[] = ocr.lines
    .filter((l) => {
      const t = l.text.trim();
      if (!t || /^[\s.:…·\-_]+$/.test(t)) return false;
      if (/^(?:X\s*Clear|Clear|Go to Extensions)$/i.test(t)) return false;
      if (
        l.y < 180 &&
        /(?:\.pdf\b|\b\d+\s*\/\s*\d+\b|\b\d+%\s*\+?|\bhttps?:\/\/|\bfile:\/\/\/|\bVPN\b|\bExtensions?\b|\bupgrade\s+requires\b)/i.test(
          t
        )
      ) {
        return false;
      }
      if (
        l.y < minDocY &&
        !(l.y >= 40 && !hasBrowserChrome && examCodeRegex.test(t)) &&
        !(l.y >= 135 && examCodeRegex.test(t))
      ) {
        return false;
      }
      return true;
    })
    .map((l) => ({
      ...l,
      words: Array.isArray(l.words) ? l.words.map((w) => ({ ...w })) : [],
    }));

  // Merge tab-separated standalone left-margin question number boxes (e.g. "6" at x=95 + "The integral I is defined by..." at x=146,
  // or "7" at x=101 + "Let I =" at x=153, or "1" at x=95 + "(i) Use the substitution..." at x=162) into a single combined OcrLineBox.
  const mergedIndices = new Set<number>();
  const docOcrLines: OcrLineBox[] = [];
  for (let i = 0; i < rawDocOcrLines.length; i++) {
    if (mergedIndices.has(i)) continue;
    const l1 = rawDocOcrLines[i];
    const t1 = l1.text.trim();
    const isStandaloneQNum =
      l1.x >= 16 &&
      l1.x < 340 &&
      l1.y >= minDocY &&
      /^\s*(\d{1,2})\s*[.)]?\s*$/.test(t1) &&
      !layout.answerLines.some((al) => Math.abs(l1.cy - al.y) <= 18) &&
      !layout.tables.some(
        (t) =>
          l1.x >= t.x - 10 &&
          l1.x <= t.x + t.w + 10 &&
          l1.cy >= t.y - 6 &&
          l1.cy <= t.y + t.h + 6
      );

    if (isStandaloneQNum) {
      let bestJ = -1;
      let bestGapX = Infinity;
      const l1MidY = l1.y + l1.h / 2;
      for (let j = 0; j < rawDocOcrLines.length; j++) {
        if (j === i || mergedIndices.has(j)) continue;
        const l2 = rawDocOcrLines[j];
        const t2 = l2.text.trim();
        if (t2.length < 3 || /^\[\d+\]$/.test(t2)) continue;
        if (layout.answerLines.some((al) => Math.abs(l2.cy - al.y) <= 18)) {
          continue;
        }
        const gapX = l2.x - (l1.x + l1.w);
        const l2MidY = l2.y + l2.h / 2;
        if (
          l2.x > l1.x &&
          gapX <= 130 &&
          Math.abs(l2MidY - l1MidY) <= 16 &&
          gapX < bestGapX
        ) {
          bestGapX = gapX;
          bestJ = j;
        }
      }
      if (bestJ >= 0) {
        const l2 = rawDocOcrLines[bestJ];
        mergedIndices.add(bestJ);
        const qDigits = t1.replace(/[.)]+$/, "").trim();
        const newX = Math.min(l1.x, l2.x);
        const newY = Math.min(l1.y, l2.y);
        const newRightX = Math.max(l1.x + l1.w, l2.x + l2.w);
        const newBottomY = Math.max(l1.y + l1.h, l2.y + l2.h);
        docOcrLines.push({
          id: l1.id,
          text: `${qDigits}. ${l2.text.trim()}`,
          x: newX,
          y: newY,
          w: newRightX - newX,
          h: newBottomY - newY,
          rightX: newRightX,
          bottomY: newBottomY,
          cy: Math.round((newY + newBottomY) / 2),
          words: [...(l1.words ?? []), ...(l2.words ?? [])],
        });
        continue;
      }
    }
    docOcrLines.push(l1);
  }

  // Filter out OCR lines inside right-side Kursor overlay cards (`paper()`, `receipt()`, `clipping()` at `x >= 720`)
  const hasMainLeftDocOcr = docOcrLines.some((l) => l.x < 500 && l.y >= 110);
  const kursorCardPhraseRe =
    /\b(?:Decompose:|Solving gives|Thus integral|Question \d+ Solution|Partial Fractions|Worked Solution|Exact Value|Rewrite as|Kursor Notes|Step \d+)\b/i;
  const isRightSideOverlayCandidate = (l: OcrLineBox): boolean => {
    if (l.x < 720 || l.y < 100 || l.y > 660) return false;
    if (
      examCodeRegex.test(l.text) ||
      /\b9709\b|\bUCLES\b|\bTurn\s+over\b/i.test(l.text)
    ) {
      return false;
    }
    if (/^\s*\[\s*(?:Total:\s*)?\d+\s*\]\s*$/i.test(l.text.trim())) {
      return false;
    }
    if (
      layout.tables.some(
        (t) =>
          l.x >= t.x - 20 &&
          l.x <= t.x + t.w + 20 &&
          l.cy >= t.y - 20 &&
          l.cy <= t.y + t.h + 20
      )
    ) {
      return false;
    }
    if (
      layout.answerLines.some(
        (al) =>
          al.x0 < 620 &&
          Math.abs(l.cy - al.y) <= 16 &&
          l.x <= al.x1 + 20
      )
    ) {
      return false;
    }
    return true;
  };
  const rightSideCandidates = docOcrLines.filter(isRightSideOverlayCandidate);
  const hasRightSideAlignedStack = rightSideCandidates.some(
    (c1) =>
      rightSideCandidates.filter((c2) => Math.abs(c2.x - c1.x) <= 18).length >= 4
  );
  const hasRightSideKursorPhrase = rightSideCandidates.some((c) =>
    kursorCardPhraseRe.test(c.text)
  );
  if (
    (hasMainLeftDocOcr && rightSideCandidates.length >= 3) ||
    hasRightSideAlignedStack ||
    hasRightSideKursorPhrase
  ) {
    const rightCandidateSet = new Set(rightSideCandidates);
    for (let i = docOcrLines.length - 1; i >= 0; i--) {
      const l = docOcrLines[i];
      if (rightCandidateSet.has(l)) {
        docOcrLines.splice(i, 1);
      }
    }
  }

  layout.ocrLines = docOcrLines;

  // Reset table OCR metadata so enrichScreenLayoutWithOcr is 100% idempotent
  for (const table of layout.tables) {
    table.questionNumber = undefined;
    table.subPart = undefined;
    table.subPartPrompt = undefined;
    for (const col of table.cols) {
      col.headerText = undefined;
    }
    for (const row of table.rows) {
      row.label = undefined;
    }
    for (const cell of table.cells) {
      cell.text = undefined;
    }
  }

  // Reset slot metadata and x0 before Step 1 so enrichScreenLayoutWithOcr is 100% idempotent
  const leftMarginRawEdges = layout.answerLines
    .map((l) => l.rawX0 ?? l.x0)
    .filter((x0) => x0 >= 240 && x0 <= 440)
    .sort((a, b) => a - b);
  const sortedRawLeftEdges = layout.answerLines
    .map((l) => l.rawX0 ?? l.x0)
    .sort((a, b) => a - b);
  const leftRef =
    leftMarginRawEdges.length > 0
      ? leftMarginRawEdges[Math.min(1, leftMarginRawEdges.length - 1)]
      : sortedRawLeftEdges.length > 0
        ? sortedRawLeftEdges[Math.min(1, sortedRawLeftEdges.length - 1)]
        : 0;
  const alignedLeftCount = layout.answerLines.filter(
    (l) => (l.rawX0 ?? l.x0) <= leftRef + 28
  ).length;
  const canRestoreLeftRef =
    layout.answerLines.length >= 2 &&
    alignedLeftCount >= 1 &&
    leftRef >= 240 &&
    leftRef <= 440;
  const brokenMidLineStart = Math.max(430, leftRef + 95);

  const rightMarginEdges = layout.answerLines
    .map((l) => l.x1)
    .filter((x1) => x1 >= 850 && x1 <= 1180)
    .sort((a, b) => b - a);
  const topRightRef = rightMarginEdges[0] ?? 0;

  for (const line of layout.answerLines) {
    const rawX0 = line.rawX0 ?? line.x0;
    line.rawX0 = rawX0;
    if (
      canRestoreLeftRef &&
      (rawX0 > brokenMidLineStart ||
        (alignedLeftCount >= 2 &&
          rawX0 < leftRef - 6 &&
          leftRef - rawX0 <= 22)) &&
      line.x1 - leftRef >= 300
    ) {
      line.x0 = leftRef;
    } else {
      line.x0 = rawX0;
    }
    if (
      topRightRef >= 850 &&
      line.x0 <= 520 &&
      line.x1 >= 650 &&
      line.x1 < topRightRef &&
      topRightRef - line.x0 >= 300
    ) {
      line.x1 = topRightRef;
    }
    line.x = line.x0;
    line.w = line.x1 - line.x0;
    line.label = undefined;
    line.questionNumber = undefined;
    line.subPart = undefined;
    line.subPartPrompt = undefined;
    line.isScrolledOffTopOrphan = undefined;
    line.writeX = line.x0 + 6;
    line.writeY = line.y - 4;
    line.maxWidth = Math.max(140, line.x1 - line.writeX);
  }

  const normalizeFieldLabelText = (txt: string): string =>
    txt
      .replace(/\bArch[il1e]t[eo]ctur[eo]\b/gi, "Architecture")
      .replace(/\bDescr[il1]pt[il1]on\b/gi, "Description")
      .replace(
        /^((?:Architecture|Benefit|Drawback|Advantage|Disadvantage|Category|Feature|Method|Stage|Term|Component|Device|Reason|Effect))\s+[Il|]$/i,
        "$1 1"
      )
      .trim();

  // 1. Pair OCR labels with detected `answerLines`
  for (const line of layout.answerLines) {
    let bestLabel = "";
    let bestLabelRightX = -1;
    let bestDy = Infinity;

    for (const ocrLine of docOcrLines) {
      const rawStart = line.rawX0 ?? line.x0;
      const effectiveStart = Math.min(rawStart, line.x0);
      const refX0 = Math.max(rawStart, line.x0);
      if (ocrLine.y > line.y + 2 || ocrLine.cy > line.y + 8) continue;
      if (examCodeRegex.test(ocrLine.text)) {
        continue;
      }
      const dy = Math.abs(ocrLine.cy - line.y);
      if (dy > 16) continue;
      if (ocrLine.x >= refX0 + 60) continue;

      let candidateText = "";
      let candidateX = ocrLine.x;
      let candidateRightX = ocrLine.rightX;

      const leadingSubItemMatch =
        ocrLine.x >= effectiveStart - 30 && ocrLine.x <= refX0 + 18
          ? ocrLine.text.match(
              /^\s*(?:([1-9])(?:\s*[.:…·\-_)|\]]|\s*$)|\(([a-z1-9])\))/i
            ) ??
            (ocrLine.words?.[0] && ocrLine.words[0].x <= refX0 + 18
              ? ocrLine.words[0].text.match(/^([1-9])\./)
              : null)
          : null;

      if (leadingSubItemMatch) {
        const subToken = leadingSubItemMatch[1] || leadingSubItemMatch[2];
        candidateText = `Item ${subToken}`;
        candidateX = ocrLine.x;
        candidateRightX = line.x0 + 2;
      } else {
        // Universal Rule for Printed Worksheet Labels vs. Written Line Prose:
        // Extract words at the left of the dotted line first (excluding trailing dots, drawn ink along the line, and right-margin "[6]" marks)
        const labelWords =
          ocrLine.words && ocrLine.words.length > 0
            ? ocrLine.words.filter(
                (w) =>
                  w.x < refX0 + 15 &&
                  w.x + w.w <= refX0 + 35 &&
                  !/^[\s.:…·\-_]+$/.test(w.text) &&
                  !/^\[\d+\]$/.test(w.text.trim())
              )
            : [];

        const hasStrictLeftMarginWords =
          labelWords.length >= 1 &&
          labelWords.length <= 3 &&
          Math.min(...labelWords.map((w) => w.x)) <= refX0 - 25 &&
          Math.max(...labelWords.map((w) => w.x + w.w)) <= refX0 + 20;

        const alphaWords =
          ocrLine.words && ocrLine.words.length > 0
            ? ocrLine.words.filter(
                (w) =>
                  /[A-Za-z]{2,}/.test(w.text) &&
                  !/^\[\d+\]$/.test(w.text.trim())
              )
            : ocrLine.text
                .replace(/\[\d+\]/g, "")
                .split(/\s+/)
                .filter((t) => /[A-Za-z]{2,}/.test(t))
                .map((t) => ({
                  text: t,
                  x: ocrLine.x,
                  y: ocrLine.y,
                  w: ocrLine.w,
                  h: ocrLine.h,
                }));

        const wordsRightOfLabelZone =
          ocrLine.words && ocrLine.words.length > 0
            ? alphaWords.filter(
                (w) => w.x > refX0 + 65 || w.x + w.w > refX0 + 135
              )
            : [];

        if (
          !hasStrictLeftMarginWords &&
          (alphaWords.length >= 4 || wordsRightOfLabelZone.length >= 2)
        ) {
          continue;
        }

        if (labelWords.length > 0) {
          candidateText = labelWords
            .map((w) => w.text.replace(/[.:…·_]+$/g, "").trim())
            .filter(Boolean)
            .join(" ")
            .trim();
          candidateX = Math.min(...labelWords.map((w) => w.x));
          candidateRightX = Math.max(...labelWords.map((w) => w.x + w.w));
        } else if (ocrLine.w < 220 && ocrLine.rightX <= refX0 + 35) {
          candidateText = ocrLine.text
            .replace(/\[\d+\]/g, "")
            .replace(/^[.:…·\-_\s]+|[.:…·\-_:\s]+$/g, "")
            .trim();
          candidateX = ocrLine.x;
          candidateRightX = ocrLine.rightX;
        }

        candidateText = normalizeFieldLabelText(
          candidateText.replace(/^[.:…·\-_\s]+|[.:…·\-_:\s]+$/g, "").trim()
        );

        if (/^[1-9]$/.test(candidateText) || /^\([a-z1-9]\)$/i.test(candidateText)) {
          candidateText = `Item ${candidateText.replace(/^[(\s]+|[)\s.]+$/g, "")}`;
        } else {
          // Universal structural check for printed left-margin worksheet field labels vs. drawn ink on the dotted line:
          // 1. Any 1-3 word capitalized label positioned structurally to the LEFT of the line's start (`effectiveStart` or `rawStart`)
          //    whose right edge terminates near `effectiveStart` or `rawStart` is a genuine printed label
          //    regardless of vocabulary ("Primary Key", "Data Bus", "Continent", "CPU", "Stage A", "Benefit 1", etc.).
          // 2. If the dots start under the label (`candidateX > effectiveStart - 12`), require a known field title or trailing ':'
          //    at the left edge (`candidateX <= refX0 + 4`) with no prose words along the line.
          const alignsWithStart = (startX: number): boolean =>
            candidateX <= startX - 12 &&
            candidateRightX >= startX - 28 &&
            candidateRightX <= startX + 18;
          const isLeftMarginFieldLabel =
            (wordsRightOfLabelZone.length === 0 || hasStrictLeftMarginWords) &&
            candidateX <= refX0 + 4 &&
            (alignsWithStart(effectiveStart) || alignsWithStart(rawStart)) &&
            /^[A-Z][A-Za-z0-9/-]*(?:\s+[A-Za-z0-9/-]+){0,2}$/.test(
              candidateText
            ) &&
            !/^(?:Complete|Comploto|Describe|Explain|State|Outline|Give|Identify|Tick|Show|Find|Use|Calculate|Write|Draw|There|Each|When|Include|Reduced|Virtual)\b/i.test(
              candidateText
            );
          const hasExplicitColon =
            wordsRightOfLabelZone.length === 0 &&
            candidateX <= refX0 + 4 &&
            candidateRightX <= refX0 + 18 &&
            /:\s*$/.test(ocrLine.text.trim()) &&
            /^[A-Z][A-Za-z0-9/-]*(?:\s+[A-Za-z0-9/-]+){0,2}$/.test(
              candidateText
            );
          const isKnownWorksheetFieldTitle =
            (wordsRightOfLabelZone.length === 0 || hasStrictLeftMarginWords) &&
            candidateX <= refX0 + 4 &&
            candidateRightX <= refX0 + 18 &&
            /^(?:Description|Benefit|Drawback|Advantage|Disadvantage|Feature|Similarity|Difference|Reason|Effect|Cause|Purpose|Function|Example|Method|Stage|Step|Term|Definition|Input|Output|Device|Sensor|Component|Layer|Protocol|Address|Register|Bus|Gate|State|Type|Name|Category|Rule|Property|Characteristic|Use|Application|Issue|Problem|Solution|Risk|Impact|Limitation|Factor|Architecture|Model|Topology|Paradigm|Structure|Technique|Mechanism|Principle|Concept)(?:\s+[A-Za-z0-9]+)?$/i.test(
              candidateText
            );
          if (
            !isLeftMarginFieldLabel &&
            !hasExplicitColon &&
            !isKnownWorksheetFieldTitle
          ) {
            continue;
          }
        }
      }

      if (
        candidateX < effectiveStart - 180 ||
        candidateRightX < effectiveStart - 80 ||
        candidateRightX > refX0 + 35
      ) {
        continue;
      }

      const candidateWords = candidateText.split(/\s+/).filter(Boolean);
      if (
        candidateWords.length > 3 &&
        /\b(?:the|each|complete|table|moves)\b/i.test(candidateText)
      ) {
        continue;
      }

      if (
        candidateText &&
        candidateText.length >= 1 &&
        candidateText.length <= 36 &&
        !/^\[\d+\]$/.test(candidateText) &&
        dy < bestDy
      ) {
        bestDy = dy;
        bestLabel = candidateText;
        bestLabelRightX = candidateRightX;
      }
    }

    if (bestLabel) {
      const rawStart = line.rawX0 ?? line.x0;
      const isShortOrItemLabel =
        /^Item\s+[a-z0-9]+$/i.test(bestLabel) || bestLabel.length <= 2;
      if (
        !isShortOrItemLabel &&
        line.x0 < bestLabelRightX - 24 &&
        bestLabelRightX <= 460
      ) {
        const restoredX0 =
          rawStart >= bestLabelRightX - 18 && rawStart <= 460
            ? rawStart
            : bestLabelRightX;
        line.x0 = restoredX0;
        line.x = restoredX0;
        line.w = line.x1 - restoredX0;
      }
      line.label = bestLabel;
      line.writeX = isShortOrItemLabel
        ? Math.max(line.x0 + 8, Math.min(bestLabelRightX + 6, line.x0 + 16))
        : Math.max(line.x0 + 4, Math.min(bestLabelRightX + 10, line.x0 + 85));
      line.writeY = line.y - 4;
      line.maxWidth = Math.max(140, line.x1 - line.writeX);
    } else {
      line.writeX = line.x0 + 6;
      line.writeY = line.y - 4;
      line.maxWidth = Math.max(140, line.x1 - line.writeX);
    }
  }

  // Post-OCR Left-Edge Alignment Pass:
  // Restore degraded left starts on unlabeled lines when no OCR text blocks [leftRef - 10, line.x0 + 10]
  if (canRestoreLeftRef) {
    for (const line of layout.answerLines) {
      if (!line.label && line.x0 > leftRef + 25 && line.x1 - leftRef >= 300) {
        const hasBlockingOcr = docOcrLines.some(
          (ol) =>
            Math.abs(ol.cy - line.y) <= 14 &&
            ol.y <= line.y + 2 &&
            (ol.words && ol.words.length > 0
              ? ol.words.some(
                  (w) => w.x + w.w >= leftRef - 10 && w.x <= line.x0 + 10
                )
              : ol.rightX >= leftRef - 10 && ol.x <= line.x0 + 10)
        );
        if (!hasBlockingOcr) {
          line.x0 = leftRef;
          line.x = leftRef;
          line.w = line.x1 - leftRef;
          line.writeX = leftRef + 6;
          line.maxWidth = Math.max(140, line.x1 - line.writeX);
        }
      }
    }
  }

  // 2. Identify question headers in `docOcrLines` (including OCR leading digit confusions like "f2.WJ 221P32/Q5" -> Q12)
  const hasShiftedDocColumn =
    layout.tables.some((t) => t.x >= 260) ||
    layout.answerLines.some((al) => (al.rawX0 ?? al.x0) >= 260) ||
    docOcrLines.some((l) => l.x >= 240 && examCodeRegex.test(l.text));
  const minQuestionHeaderX = hasShiftedDocColumn ? 220 : 16;

  const questionHeaders: Array<{ qNum: string; y: number; line: OcrLineBox }> = [];
  for (const l of docOcrLines) {
    const hasExamCode = examCodeRegex.test(l.text);
    if (
      (l.y < minDocY && !(l.y >= 40 && hasExamCode)) ||
      l.x < minQuestionHeaderX ||
      l.x > 360
    ) {
      continue;
    }
    // Never classify an OCR line sitting on a worksheet answer line as a top-level question header
    if (
      !hasExamCode &&
      layout.answerLines.some((al) => Math.abs(l.cy - al.y) <= 18)
    ) {
      continue;
    }
    const normalizedText = hasExamCode
      ? l.text
          .replace(/^\s*[fIl|](\d)\b/, "1$1")
          .replace(/^\s*[Il|]\s*\./, "1.")
      : l.text;
    if (hasExamCode && normalizedText !== l.text) {
      l.text = normalizedText;
    }

    const strippedAfterNum = normalizedText
      .replace(/^\s*\d{1,2}\s*[.:…·\-_\s]*/, "")
      .replace(/\[\d+\]\s*$/, "")
      .trim();
    const alphaCount = (strippedAfterNum.match(/[A-Za-z]/g) || []).length;
    if (alphaCount < 3 && !hasExamCode) continue;

    const m = normalizedText.match(
      /^\s*(\d{1,2})\s*(?:[.)\]:-]\s*|\s*(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM)|\s+(?:\([a-zivx]+\)|The|Let|Given|A|An|In|For|Find|Use|Show|Hence|Evaluate|Prove|Differentiate|Integrate|Express|Sketch|Solve|Describe|Complete|Comploto|Outline|State|Explain|Calculate|Give|Identify)\b)/i
    );
    if (m) {
      questionHeaders.push({ qNum: m[1], y: l.y, line: l });
    } else if (hasExamCode && questionHeaders.length > 0) {
      const prevHdr = questionHeaders[questionHeaders.length - 1];
      const prevNum = parseInt(prevHdr.qNum, 10);
      if (!isNaN(prevNum) && l.y - prevHdr.y >= 60) {
        questionHeaders.push({
          qNum: String(prevNum + 1),
          y: l.y,
          line: l,
        });
      }
    }
  }

  // If Question N (e.g. 10 or 8) is clearly detected below a question block whose number (e.g. 9 or 7) was occluded by prior ink,
  // infer Question N-1 at the prompt line ("Describe what is meant...", "The processing of instructions is divided into...") above it.
  if (questionHeaders.length > 0) {
    questionHeaders.sort((a, b) => a.y - b.y);
    const firstHdr = questionHeaders[0];
    const firstNum = parseInt(firstHdr.qNum, 10);
    if (firstNum > 1 && firstHdr.y > 380) {
      const prevNumStr = String(firstNum - 1);
      const alreadyHasPrev = questionHeaders.some((h) => h.qNum === prevNumStr);
      if (!alreadyHasPrev) {
        const promptLine = docOcrLines.find(
          (l) =>
            l.y >= 260 &&
            l.y < firstHdr.y - 80 &&
            l.x >= minQuestionHeaderX &&
            l.x < 385 &&
            (new RegExp(`^\\s*${prevNumStr}\\b`).test(l.text) ||
              /\b(?:Describe|Explain|State|Outline|Complete|Comploto|Give|Identify|Include in your answer|divided into)\b/i.test(
                l.text
              ))
        );
        if (promptLine) {
          questionHeaders.unshift({
            qNum: prevNumStr,
            y: promptLine.y,
            line: promptLine,
          });
        }
      }
    }
  }

  questionHeaders.sort((a, b) => a.y - b.y);

  // Detect sub-part headers `(a)`, `(b)`, `(c)`, `(i)`, `(ii)` in `docOcrLines`
  const subPartHeaders: Array<{
    subPart: string;
    prompt: string;
    y: number;
    questionNumber?: string;
  }> = [];
  for (let idx = 0; idx < docOcrLines.length; idx++) {
    const l = docOcrLines[idx];
    if (l.y < 150 || l.x > 400) continue;
    // Ignore sub-item labels sitting directly on an answerLine
    if (layout.answerLines.some((al) => Math.abs(l.cy - al.y) <= 12)) continue;
    // Ignore text inside a table
    if (
      layout.tables.some(
        (t) =>
          l.x >= t.x - 10 &&
          l.x <= t.x + t.w + 10 &&
          l.cy >= t.y - 4 &&
          l.cy <= t.y + t.h + 4
      )
    ) {
      continue;
    }
    const subMatch = l.text.match(/^\s*\(([a-z]|[ivx]+)\)\s+(.+)$/i);
    if (subMatch) {
      const token = subMatch[1].toLowerCase();
      let promptText = subMatch[2].replace(/\[\d+\]\s*$/, "").trim();
      const nextLine = docOcrLines[idx + 1];
      if (
        nextLine &&
        nextLine.x < 450 &&
        nextLine.y > l.y &&
        nextLine.y - l.y <= 26 &&
        !/^\s*\(([a-z]|[ivx]+)\)\s+/i.test(nextLine.text) &&
        !examCodeRegex.test(nextLine.text) &&
        !layout.answerLines.some((al) => Math.abs(nextLine.cy - al.y) <= 12) &&
        !layout.tables.some(
          (t) => nextLine.cy >= t.y - 4 && nextLine.cy <= t.y + t.h + 4
        )
      ) {
        const nextCleaned = nextLine.text.replace(/\[\d+\]\s*$/, "").trim();
        if (nextCleaned) {
          promptText = `${promptText} ${nextCleaned}`.replace(/\s+/g, " ").trim();
        }
      }
      let ownerQ: string | undefined;
      for (const hdr of questionHeaders) {
        if (hdr.y <= l.y) {
          ownerQ = hdr.qNum;
        }
      }
      subPartHeaders.push({
        subPart: `(${token})`,
        prompt: promptText,
        y: l.y,
        questionNumber: ownerQ,
      });
    }
  }

  // Enrich each `DetectedTable` with `questionNumber`, `subPart`, `col.headerText`, `row.label`, and `cell.text`
  for (const table of layout.tables) {
    for (const hdr of questionHeaders) {
      if (hdr.y <= table.y) {
        table.questionNumber = hdr.qNum;
      }
    }
    for (const sh of subPartHeaders) {
      if (
        sh.y <= table.y &&
        (!table.questionNumber ||
          !sh.questionNumber ||
          sh.questionNumber === table.questionNumber)
      ) {
        table.subPart = sh.subPart;
        table.subPartPrompt = sh.prompt;
      }
    }

    const extractCellText = (
      rowY0: number,
      rowY1: number,
      colX0: number,
      colX1: number
    ): string => {
      const lineParts: Array<{ y: number; text: string }> = [];
      for (const ol of docOcrLines) {
        if (ol.bottomY < rowY0 - 4 || ol.y > rowY1 + 4) continue;
        if (ol.words && ol.words.length > 0) {
          const matchingWords = ol.words.filter((w) => {
            const wCy = w.y + w.h / 2;
            const wCx = w.x + w.w / 2;
            return (
              wCy >= rowY0 - 6 &&
              wCy <= rowY1 + 6 &&
              wCx >= colX0 - 6 &&
              wCx <= colX1 + 6
            );
          });
          if (matchingWords.length > 0) {
            const joined = matchingWords
              .map((w) => w.text.replace(/^[|[\]]+|[|[\]]+$/g, "").trim())
              .filter(Boolean)
              .join(" ")
              .trim();
            if (joined) {
              lineParts.push({ y: ol.y, text: joined });
            }
          }
        } else if (ol.cy >= rowY0 - 6 && ol.cy <= rowY1 + 6) {
          const olCx = ol.x + ol.w / 2;
          if (olCx >= colX0 - 6 && olCx <= colX1 + 6) {
            const cleaned = ol.text.replace(/^[|[\]]+|[|[\]]+$/g, "").trim();
            if (cleaned) {
              lineParts.push({ y: ol.y, text: cleaned });
            }
          }
        }
      }
      lineParts.sort((a, b) => a.y - b.y);
      return lineParts
        .map((p) => p.text)
        .join(" ")
        .replace(/\s+/g, " ")
        .trim();
    };

    const headerRow = table.rows.find((r) => r.isHeader);
    if (headerRow) {
      for (const col of table.cols) {
        const hText = extractCellText(
          headerRow.y0,
          headerRow.y1,
          col.x0,
          col.x1
        );
        if (hText) {
          col.headerText = hText;
        }
      }
    }

    // Smart Column Header Inference when a binary/choice column header (e.g. "CISC" in "RISC or CISC") was missed by OCR
    if (
      table.cols.length === 3 &&
      (!table.cols[1].headerText || !table.cols[2].headerText)
    ) {
      const promptLinesAbove = docOcrLines.filter(
        (l) => l.y >= table.y - 90 && l.y <= table.y + 10
      );
      const combinedPromptAbove = promptLinesAbove
        .map((l) => l.text)
        .join(" ");
      const binaryChoiceMatches = Array.from(
        combinedPromptAbove.matchAll(
          /\b([A-Za-z0-9]{2,12})\s+or\s+([A-Za-z0-9]{2,12})\b/gi
        )
      );
      for (const m of binaryChoiceMatches) {
        const choiceA = m[1];
        const choiceB = m[2];
        if (
          /^(?:box|row|one|two|tick|show|each)$/i.test(choiceA) ||
          /^(?:box|row|one|two|tick|show|each)$/i.test(choiceB)
        ) {
          continue;
        }
        const h1 = table.cols[1].headerText?.trim() ?? "";
        const h2 = table.cols[2].headerText?.trim() ?? "";
        if (h1 && !h2) {
          if (h1.toLowerCase() === choiceA.toLowerCase()) {
            table.cols[2].headerText = choiceB;
            break;
          } else if (h1.toLowerCase() === choiceB.toLowerCase()) {
            table.cols[2].headerText = choiceA;
            break;
          }
        } else if (!h1 && h2) {
          if (h2.toLowerCase() === choiceB.toLowerCase()) {
            table.cols[1].headerText = choiceA;
            break;
          } else if (h2.toLowerCase() === choiceA.toLowerCase()) {
            table.cols[1].headerText = choiceB;
            break;
          }
        } else if (!h1 && !h2) {
          table.cols[1].headerText = choiceA;
          table.cols[2].headerText = choiceB;
          break;
        }
      }
    }

    // Smart Sequential Numeric Column Header Inference (e.g. Clock cycles 1..12 where OCR read "2", "3", "10", "11", "12")
    if (table.cols.length >= 4) {
      const dataCols = table.cols.slice(1);
      const matchingSeqCount = dataCols.filter(
        (c) => c.headerText?.trim() === String(c.colIndex)
      ).length;
      if (matchingSeqCount >= 2) {
        for (const c of dataCols) {
          if (!c.headerText) {
            c.headerText = String(c.colIndex);
          }
        }
      }
    }

    const hasRowHeaderCol =
      table.cols.length >= 3 &&
      !(table.rows.length === 9 && table.cols.length === 9);
    for (const row of table.rows) {
      if (!row.isHeader && hasRowHeaderCol && table.cols[0]) {
        const rLabel = extractCellText(
          row.y0,
          row.y1,
          table.cols[0].x0,
          table.cols[0].x1
        );
        if (rLabel) {
          row.label = rLabel;
        }
      }
    }

    // Smart Stage / Abbreviated Row Label Inference when prompt lines above the table list (IF), (ID), (OF), (IE), (WB)
    const nonHeaderRows = table.rows.filter((r) => !r.isHeader);
    if (hasRowHeaderCol && nonHeaderRows.length >= 3) {
      const stageLinesAbove = docOcrLines.filter(
        (l) =>
          l.x >= minQuestionHeaderX &&
          l.y >= table.y - 220 &&
          l.y <= table.y + 10
      );
      const stageTokens: string[] = [];
      for (const sl of stageLinesAbove) {
        const m = sl.text.match(/\(([A-Z]{2,5})\)/);
        if (m && !stageTokens.includes(m[1])) {
          stageTokens.push(m[1]);
        }
      }
      if (stageTokens.length === nonHeaderRows.length) {
        const alignedStageCount = nonHeaderRows.filter((r, idx) =>
          new RegExp(`\\b${stageTokens[idx]}\\b`, "i").test(r.label ?? "")
        ).length;
        if (alignedStageCount >= 2) {
          for (let idx = 0; idx < nonHeaderRows.length; idx++) {
            nonHeaderRows[idx].label = stageTokens[idx];
          }
        }
      }
    }

    for (const row of table.rows) {
      for (const col of table.cols) {
        const cell = table.cells.find(
          (c) => c.row === row.rowIndex && c.col === col.colIndex
        );
        if (cell) {
          if (row.isHeader && col.headerText) {
            cell.text = col.headerText;
          } else if (!row.isHeader && col.colIndex === 0 && row.label) {
            cell.text = row.label;
          } else {
            const cText = extractCellText(row.y0, row.y1, col.x0, col.x1);
            if (cText) cell.text = cText;
          }
        }
      }
    }
  }

  // Tag scrolled-off top orphan answer lines (above `questionHeaders[0].y - 4`) and associate active answer lines with questions & sub-parts
  if (questionHeaders.length > 0) {
    const firstQNum = parseInt(questionHeaders[0].qNum, 10);
    const orphanPrevQ =
      !isNaN(firstQNum) && firstQNum > 1 ? String(firstQNum - 1) : "prev";
    for (const line of layout.answerLines) {
      if (line.y < questionHeaders[0].y - 4) {
        line.questionNumber = orphanPrevQ;
        line.isScrolledOffTopOrphan = true;
      }
    }
  }

  for (const line of layout.answerLines) {
    if (line.isScrolledOffTopOrphan) continue;
    let matchedQ: string | undefined;
    for (const hdr of questionHeaders) {
      if (hdr.y <= line.y - 6 && hdr.line.cy < line.y) {
        matchedQ = hdr.qNum;
      }
    }
    if (matchedQ) {
      line.questionNumber = matchedQ;
    }
  }

  for (let i = 1; i < layout.answerLines.length; i++) {
    const cur = layout.answerLines[i];
    const prev = layout.answerLines[i - 1];
    if (cur.isScrolledOffTopOrphan || prev.isScrolledOffTopOrphan) continue;
    if (
      !cur.questionNumber &&
      prev.questionNumber &&
      cur.y - prev.y >= 16 &&
      cur.y - prev.y <= 56
    ) {
      const hasHeaderBetween = questionHeaders.some(
        (h) => h.y > prev.y && h.y <= cur.y - 4 && h.line.cy < cur.y
      );
      if (!hasHeaderBetween) {
        cur.questionNumber = prev.questionNumber;
      }
    }
  }

  // Associate each non-orphan answerLine with its sub-part `(a)`, `(b)`, etc.
  for (const line of layout.answerLines) {
    if (line.isScrolledOffTopOrphan) continue;
    for (const sh of subPartHeaders) {
      if (
        sh.y <= line.y - 6 &&
        (!line.questionNumber ||
          !sh.questionNumber ||
          sh.questionNumber === line.questionNumber)
      ) {
        line.subPart = sh.subPart;
        line.subPartPrompt = sh.prompt;
      }
    }
  }

  // Partition answerLines into contiguous question groups for per-question sub-item grid inference
  const questionGroups: ScreenBlankLine[][] = [];
  for (const line of layout.answerLines) {
    const lastGroup = questionGroups[questionGroups.length - 1];
    const prev = lastGroup ? lastGroup[lastGroup.length - 1] : undefined;
    const hasHeaderBetween =
      prev !== undefined &&
      questionHeaders.some((h) => h.y > prev.y && h.y <= line.y - 4);
    if (
      prev &&
      (line.questionNumber ?? "") === (prev.questionNumber ?? "") &&
      !hasHeaderBetween &&
      line.y - prev.y >= 18 &&
      line.y - prev.y <= 56
    ) {
      lastGroup.push(line);
    } else {
      questionGroups.push([line]);
    }
  }

  // 1B. Regular 2-line numbered sub-item grid inference per question group
  // (e.g. 8 lines for Item 1..4 where a number like "4" was occluded by a thumbprint, or multi-question pages where Q1 and Q2 each have their own grid)
  for (const group of questionGroups) {
    if (group.length >= 4 && group.length % 2 === 0) {
      const pairCount = group.length / 2;
      const noConflictingEvenLabels = group.every(
        (l, idx) =>
          idx % 2 !== 0 ||
          !l.label ||
          l.label.toLowerCase() === `item ${idx / 2 + 1}`
      );
      const oddLinesUnlabeled = group.every(
        (l, idx) => idx % 2 === 0 || !l.label
      );
      const hasAlignedEvenItem = group.some(
        (l, idx) =>
          idx % 2 === 0 && l.label?.toLowerCase() === `item ${idx / 2 + 1}`
      );
      if (noConflictingEvenLabels && oddLinesUnlabeled && hasAlignedEvenItem) {
        const groupX0 = Math.min(...group.map((l) => l.x0));
        const groupWriteX = groupX0 + 14;
        for (let k = 0; k < pairCount; k++) {
          const evenLine = group[2 * k];
          evenLine.x0 = groupX0;
          evenLine.x = groupX0;
          evenLine.w = evenLine.x1 - groupX0;
          evenLine.label = `Item ${k + 1}`;
          evenLine.writeX = groupWriteX;
          evenLine.writeY = evenLine.y - 4;
          evenLine.maxWidth = Math.max(140, evenLine.x1 - evenLine.writeX);

          const oddLine = group[2 * k + 1];
          oddLine.x0 = groupX0;
          oddLine.x = groupX0;
          oddLine.w = oddLine.x1 - groupX0;
        }
      }

      // 1C. Paired Title + Description Sequence Normalization per question group
      // (e.g. 6 lines for Architecture 1 + Description, Architecture 2 + Description, Architecture 3 + Description where OCR dropped '2' or '3')
      if (group.length >= 4 && group.length % 2 === 0) {
        const pairCount = group.length / 2;
        const oddAreDesc = group.every(
          (l, idx) =>
            idx % 2 === 0 ||
            Boolean(
              l.label &&
                /^(?:Description|Explanation|Purpose|Function|Role|Meaning|Definition)\s*\d*$/i.test(
                  l.label
                )
            )
        );
        const evenBaseTitles = group
          .filter((_, idx) => idx % 2 === 0)
          .map((l) =>
            l.label ? l.label.replace(/\s+\d+$/, "").trim() : ""
          );
        const firstBaseTitle = evenBaseTitles.find(Boolean) ?? "";
        const allEvenMatchBase =
          Boolean(firstBaseTitle) &&
          evenBaseTitles.every(
            (b) => b.toLowerCase() === firstBaseTitle.toLowerCase()
          );
        if (oddAreDesc && allEvenMatchBase) {
          const maxEvenWriteX = Math.max(
            ...group
              .filter((_, idx) => idx % 2 === 0)
              .map((l) => l.writeX)
          );
          const maxOddWriteX = Math.max(
            ...group
              .filter((_, idx) => idx % 2 === 1)
              .map((l) => l.writeX)
          );
          for (let k = 0; k < pairCount; k++) {
            const evenLine = group[2 * k];
            evenLine.label = `${firstBaseTitle} ${k + 1}`;
            evenLine.writeX = maxEvenWriteX;
            evenLine.maxWidth = Math.max(140, evenLine.x1 - evenLine.writeX);

            const oddLine = group[2 * k + 1];
            oddLine.writeX = maxOddWriteX;
            oddLine.maxWidth = Math.max(140, oddLine.x1 - oddLine.writeX);
          }
        }
      }
    }
  }

  // 3. Automatically label unlabeled continuation lines within the same question group (e.g. "Description (line 2)", "Item 1 (line 2)")
  for (let i = 1; i < layout.answerLines.length; i++) {
    const cur = layout.answerLines[i];
    const prev = layout.answerLines[i - 1];
    const hasHeaderBetween = questionHeaders.some(
      (h) => h.y > prev.y && h.y <= cur.y - 4
    );
    if (
      !cur.label &&
      prev.label &&
      (cur.questionNumber ?? "") === (prev.questionNumber ?? "") &&
      !hasHeaderBetween &&
      cur.y - prev.y >= 18 &&
      cur.y - prev.y <= 56
    ) {
      const baseLabel = prev.label.replace(/\s*\(line\s*\d+\)\s*$/i, "");
      const prevLineMatch = prev.label.match(/\(line\s*(\d+)\)/i);
      const nextNum = prevLineMatch ? parseInt(prevLineMatch[1], 10) + 1 : 2;
      cur.label = `${baseLabel} (line ${nextNum})`;
      cur.writeX = cur.x0 + 4;
      cur.writeY = cur.y - 4;
      cur.maxWidth = Math.max(140, cur.x1 - cur.writeX);
    }
  }

  const promptBlock = buildLayoutPromptBlock(
    layout.width,
    layout.height,
    layout.tables,
    layout.cells,
    layout.answerLines,
    layout.calmCardSpot,
    docOcrLines
  );
  layout.layoutPromptNote = promptBlock;
  layout.promptBlock = promptBlock;

  return layout;
}

/**
 * Runs both pixel-level geometry analysis (`analyzeScreenLayout`) and local Windows WinRT OCR (`runWindowsOcr`),
 * returning the unified `ScreenLayoutAnalysis` enriched with OCR lines and labeled worksheet answer slots.
 */
export async function analyzeScreenLayoutWithOcr(
  imageDataUrl: string
): Promise<ScreenLayoutAnalysis | null> {
  const ocrPromise = runWindowsOcr(imageDataUrl);
  const layout = analyzeScreenLayout(imageDataUrl);
  if (!layout) return null;
  const ocr = await ocrPromise;
  return enrichScreenLayoutWithOcr(layout, ocr);
}

/**
 * Balanced-parenthesis statement stripper for `receipt(...)` and `clipping(...)` calls
 * that overlap a detected high-confidence multi-cell grid (`cells.length >= 9`).
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

const WORD_TO_NUM: Record<string, string> = {
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

function extractRequestedQuestionNumber(userText: string): string | null {
  if (!userText) return null;
  const normalized = userText
    .replace(/\b(question(?:\s+number)?|problem|number)\s+c\s*,?\s*x\b/gi, "$1 6")
    .replace(
      /\b(question(?:\s+number)?|problem)\s+by\s+(\d{1,2})(?:\s+and\s+[a-d]\b)?/gi,
      "$1 $2"
    );
  const digitMatch = normalized.match(
    /\b(?:question(?:\s+number)?|q|problem|number)\s*#?\s*(\d{1,2})\b/i
  );
  if (digitMatch) return digitMatch[1];

  const wordMatch = normalized.match(
    /\b(?:question(?:\s+number)?|problem|number)\s+(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)\b/i
  );
  if (wordMatch) {
    return WORD_TO_NUM[wordMatch[1].toLowerCase()] ?? null;
  }
  return null;
}

function splitTwoLineWorksheetText(text: string, splitTarget = 76): [string, string] {
  const trimmed = text.trim();
  if (trimmed.length <= 82 && trimmed.length <= splitTarget + 8) return [trimmed, ""];
  const idealIdx = Math.min(
    splitTarget,
    Math.max(64, Math.round(trimmed.length * 0.72))
  );
  let bestSpace = -1;
  let bestDist = Infinity;
  const minIdx = Math.min(48, Math.max(24, Math.floor(trimmed.length * 0.45)));
  const maxIdx = Math.min(trimmed.length - 10, splitTarget + 8);
  for (let i = minIdx; i < maxIdx; i++) {
    if (trimmed[i] === " ") {
      const dist = Math.abs(i - idealIdx);
      if (dist < bestDist) {
        bestDist = dist;
        bestSpace = i;
      }
    }
  }
  if (bestSpace < 0) {
    bestSpace = trimmed.lastIndexOf(" ", idealIdx);
  }
  if (bestSpace < 0) {
    return [trimmed.slice(0, idealIdx), trimmed.slice(idealIdx).trim()];
  }
  return [
    trimmed.slice(0, bestSpace).trim(),
    trimmed.slice(bestSpace + 1).trim(),
  ];
}

function buildWorksheetWriteOptions(
  maxWidth: number,
  existingOptsInner?: string
): string {
  const trimmed = (existingOptsInner ?? "").trim();
  if (!trimmed) {
    return `{ maxW: ${maxWidth} }`;
  }
  if (/\bmaxW\s*:/.test(trimmed)) {
    const updated = trimmed.replace(/\bmaxW\s*:\s*\d+(?:\.\d+)?/, `maxW: ${maxWidth}`);
    return `{ ${updated} }`;
  }
  return `{ ${trimmed}, maxW: ${maxWidth} }`;
}

/**
 * Safely snaps generated ClickyDraw JS coordinates to:
 * 1. Detected multi-cell tables/grids (`layout.cells.length >= 9`)
 * 2. OCR question headings (`underline(x, y, w)`)
 * 3. Detected worksheet answer slots (`layout.answerLines`, including semantic label prefix stripping and 2-line Description splitting)
 */
export function snapProgramToScreenLayout(
  program: string,
  layout: ScreenLayoutAnalysis | null,
  userText: string = ""
): string {
  if (!layout) {
    return program;
  }

  // Strip empty/whitespace-only or generic meta-placeholder write(x, y, "..."[, opts]) calls up front
  const genericPlaceholderWriteRe =
    /\b(?:answer\s+line\s+(?:one|two|three|four|five|six|\d+)|(?:first|second|third|fourth)\s+concise\s+response|concise\s+response\s+(?:addressing|completing|finalizing|that\s+follows))\b/i;
  let processed = program.replace(
    /[ \t]*\bwrite\s*\(\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?\s*,\s*(["'`])((?:\\.|(?!\1)[\s\S])*)\1(?:\s*,\s*\{[^{}]*\})?\s*\)\s*;?[ \t]*\r?\n?/g,
    (full, _q: string, body: string) => {
      if (!body.trim() || genericPlaceholderWriteRe.test(body)) {
        return "";
      }
      return full;
    }
  );

  const hasGridCells = Array.isArray(layout.cells) && layout.cells.length >= 9;
  const hasAnswerLines =
    Array.isArray(layout.answerLines) && layout.answerLines.length > 0;
  const hasOcrLines =
    Array.isArray(layout.ocrLines) && layout.ocrLines.length > 0;

  if (!hasGridCells && !hasAnswerLines && !hasOcrLines) {
    return processed;
  }

  const userWantsPageCard =
    /\b(?:on a page|on another page|open a page|in a new page|on a card|on a whiteboard|separate page|show working|step by step|explain\s+(?:it\s+|to\s+me\s+|me\s+)?(?:step\s+by\s+step|in\s+detail|how\s+did\s+you|how\s+you)|how\s+did\s+you\s+(?:solve|come\s+up\s+with|get))\b/i.test(
      userText
    ) && !/\bnot on (?:a |another )?page\b/i.test(userText);

  const writableCells: TableCell[] =
    hasGridCells && layout.tables.length > 0
      ? layout.tables.flatMap((t) => {
          const isSudoku9x9 = t.rows.length === 9 && t.cols.length === 9;
          const headerRows = new Set(
            isSudoku9x9
              ? []
              : t.rows.filter((r) => r.isHeader).map((r) => r.rowIndex)
          );
          const skipCol0 = !isSudoku9x9 && t.cols.length >= 2;
          return t.cells.filter(
            (c) => !headerRows.has(c.row) && !(skipCol0 && c.col === 0)
          );
        })
      : hasGridCells
        ? layout.cells
        : [];
  const writeCandidateCells =
    writableCells.length > 0 ? writableCells : hasGridCells ? layout.cells : [];

  const findNearestGridCell = (
    x: number,
    y: number,
    candidateCells: TableCell[] = writeCandidateCells
  ): TableCell | null => {
    if (!hasGridCells || candidateCells.length === 0) return null;
    let bestCell: TableCell | null = null;
    let bestDist = Infinity;
    for (const cell of candidateCells) {
      const insideOrNear =
        x >= cell.x - 36 &&
        x <= cell.x + cell.w + 36 &&
        y >= cell.y - 36 &&
        y <= cell.y + cell.h + 36;
      if (!insideOrNear) continue;
      const dist = Math.hypot(x - cell.cx, y - cell.cy);
      if (dist < bestDist) {
        bestDist = dist;
        bestCell = cell;
      }
    }
    return bestCell;
  };

  if (hasGridCells) {
    const strippedNow = stripCommentsAndStrings(processed);
    const hasTableMarks = /\b(check|cross)\s*\(/.test(strippedNow);
    if (!userWantsPageCard && hasTableMarks && layout.tables.length > 0) {
      processed = stripReceiptAndClippingStatements(processed);
    }

    // 1. Snap `check(x, y)` / `cross(x, y)` and `point(x, y, "✓/✗")` for detected tables
    if (layout.tables.length > 0) {
      const findNearestTable = (
        rawX: number,
        rawY: number
      ): DetectedTable | null => {
        let bestTable: DetectedTable | null = null;
        let bestDist = Infinity;
        for (const t of layout.tables) {
          const dx =
            rawX < t.x
              ? t.x - rawX
              : rawX > t.x + t.w
                ? rawX - (t.x + t.w)
                : 0;
          const dy =
            rawY < t.y
              ? t.y - rawY
              : rawY > t.y + t.h
                ? rawY - (t.y + t.h)
                : 0;
          if (dx <= 140 && dy <= 140) {
            const dist = Math.hypot(dx, dy);
            if (dist < bestDist) {
              bestDist = dist;
              bestTable = t;
            }
          }
        }
        return bestTable;
      };

      processed = processed.replace(
        /\bpoint\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])((?:\\.|(?!\3)[\s\S])*)\3(\s*(?:,\s*(?:\{[^{}]*\}|[^()]*))?)\)/g,
        (full, xStr: string, yStr: string, _q: string, label: string) => {
          const hasCheck = /(?:✓|✔|✅|\bcheck\b|\btick\b)/i.test(label);
          const hasCross = /(?:✗|✘|❌|\bcross\b)/i.test(label);
          if (hasCheck === hasCross) return full;

          let targetX = parseFloat(xStr);
          let targetY = parseFloat(yStr);

          const table =
            findNearestTable(targetX, targetY) ??
            findNearestTable(targetX * (1280 / 840), targetY * (720 / 565));
          if (!table) return full;
          const dataRows = table.rows.filter((r) => !r.isHeader);
          if (dataRows.length === 0) return full;

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

      // Also convert `write(x, y, "✓")` / `write(x, y, "✗")` near a table into `check(x, y)` / `cross(x, y)`
      // so table tick marks go through full table column/row snapping and never leak into worksheet answerLines.
      processed = processed.replace(
        /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])\s*([✓✔✅✗✘❌])\s*\3(?:\s*,\s*\{[^{}]*\})?\s*\)/g,
        (full, xStr: string, yStr: string, _q: string, sym: string) => {
          const targetX = parseFloat(xStr);
          const targetY = parseFloat(yStr);
          const table =
            findNearestTable(targetX, targetY) ??
            findNearestTable(targetX * (1280 / 840), targetY * (720 / 565));
          if (!table) return full;
          const fn = /[✗✘❌]/.test(sym) ? "cross" : "check";
          return `${fn}(${targetX}, ${targetY})`;
        }
      );

      for (const table of layout.tables) {
        const dataRows = table.rows.filter((r) => !r.isHeader);
        const answerCols =
          table.cols.length >= 2 ? table.cols.slice(1) : table.cols;
        if (dataRows.length === 0 || answerCols.length === 0) continue;

        const callRegex =
          /\b(check|cross)\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)(\s*(?:,\s*(?:\{[^{}]*\}|[^()]*))?)\)/g;
        const allMatches = Array.from(processed.matchAll(callRegex));
        if (allMatches.length === 0) continue;

        const minAnswerColX = answerCols[0].x0;
        const avgX =
          allMatches.reduce((s, m) => s + parseFloat(m[2]), 0) /
          allMatches.length;
        const maxX = Math.max(...allMatches.map((m) => parseFloat(m[2])));
        const maxY = Math.max(...allMatches.map((m) => parseFloat(m[3])));
        const needsXRescale =
          allMatches.length >= 2 &&
          avgX < minAnswerColX - 80 &&
          maxX < minAnswerColX - 40 &&
          avgX * (1280 / 840) >= minAnswerColX - 60 &&
          avgX * (1280 / 840) <= table.x + table.w + 60;
        const needsYRescale =
          needsXRescale &&
          maxY < table.y + table.h - 40 &&
          maxY * (720 / 565) <= table.y + table.h + 35;

        const isCoordNearTable = (rawX: number, rawY: number): boolean => {
          const rx = needsXRescale ? rawX * (1280 / 840) : rawX;
          const ry = needsYRescale ? rawY * (720 / 565) : rawY;
          const nearest = findNearestTable(rx, ry);
          return (
            nearest === table &&
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
        if (tableMatches.length === 0) continue;

        const checkMatches = tableMatches.filter((m) => m[1] === "check");
        const crossMatches = tableMatches.filter((m) => m[1] === "cross");
        const checksOnePerRow = checkMatches.length === dataRows.length;
        const crossesOnePerRow = crossMatches.length === dataRows.length;
        const totalOnePerRow = tableMatches.length === dataRows.length;
        const totalTwoPerRow = tableMatches.length === dataRows.length * 2;

        const isTopToBottom = (matches: RegExpMatchArray[]): boolean => {
          for (let i = 1; i < matches.length; i++) {
            const prevY =
              parseFloat(matches[i - 1][3]) * (needsYRescale ? 720 / 565 : 1);
            const curY =
              parseFloat(matches[i][3]) * (needsYRescale ? 720 / 565 : 1);
            if (curY < prevY - 8) return false;
          }
          return matches.length > 0;
        };

        const checksInOrder = isTopToBottom(checkMatches);
        const crossesInOrder = isTopToBottom(crossMatches);
        const allInOrder = isTopToBottom(tableMatches);

        let callIndex = 0;
        let checkIndex = 0;
        let crossIndex = 0;
        const seenCheckRowYs = new Set<number>();

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

            if (needsXRescale) {
              rawX = rawX * (1280 / 840);
            }
            if (needsYRescale) {
              rawY = rawY * (720 / 565);
            }

            let bestCol = answerCols[0];
            let bestDx = Math.abs(rawX - bestCol.cx);
            for (const col of answerCols) {
              const dx = Math.abs(rawX - col.cx);
              if (dx < bestDx) {
                bestDx = dx;
                bestCol = col;
              }
            }

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
            if (
              fnName === "check" &&
              checksOnePerRow &&
              (checksInOrder || bestDy > 45)
            ) {
              snappedY = dataRows[cIdx].cy;
            } else if (
              fnName === "cross" &&
              crossesOnePerRow &&
              (crossesInOrder || bestDy > 45)
            ) {
              snappedY = dataRows[cIdx].cy;
            } else if (
              totalOnePerRow &&
              idx < dataRows.length &&
              (allInOrder || bestDy > 45)
            ) {
              snappedY = dataRows[idx].cy;
            } else if (
              totalTwoPerRow &&
              Math.floor(idx / 2) < dataRows.length &&
              (allInOrder || bestDy > 45)
            ) {
              snappedY = dataRows[Math.floor(idx / 2)].cy;
            }

            // In a 2-column binary choice table where only check() is used (no cross()), never allow ticking both boxes in the same row
            if (
              fnName === "check" &&
              crossMatches.length === 0 &&
              answerCols.length === 2
            ) {
              if (seenCheckRowYs.has(snappedY)) {
                return "";
              }
              seenCheckRowYs.add(snappedY);
            }

            return `${fnName}(${bestCol.cx}, ${snappedY}${rest || ""})`;
          }
        );
        processed = processed.replace(/;\s*;/g, ";");
      }
    }

    // 2. Snap `write(x, y, ...)` and `spot(x, y, ...)` calls that fall inside or within 36px of a detected grid cell.
    const writeFullRegex =
      /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(?:(["'`])((?:\\.|(?!\3)[\s\S])*)\3|([^,()\s][^,()]*?))(\s*,\s*\{([^{}]*)\})?\s*\)/g;
    processed = processed.replace(
      writeFullRegex,
      (
        full,
        xStr: string,
        yStr: string,
        quote: string | undefined,
        textBody: string | undefined,
        rawExpr: string | undefined,
        optsFull: string | undefined,
        optsInner: string | undefined
      ) => {
        const rawX = parseFloat(xStr);
        const rawY = parseFloat(yStr);
        const cell = findNearestGridCell(rawX, rawY, writeCandidateCells);
        if (!cell) return full;
        const thirdArg =
          quote !== undefined
            ? `${quote}${textBody ?? ""}${quote}`
            : (rawExpr ?? "").trim();
        if (cell.w > 110) {
          const snappedX = Math.round(
            Math.max(
              cell.x + 8,
              Math.min(Math.max(cell.x + 8, cell.x + cell.w - 24), rawX)
            )
          );
          return `write(${snappedX}, ${cell.cy}, ${thirdArg}${optsFull ?? ""})`;
        }
        const snappedX = cell.cx;
        const snappedY = cell.cy;
        if (!optsFull) {
          return `write(${snappedX}, ${snappedY}, ${thirdArg}, { anchor: "middle" })`;
        }
        const trimmedOpts = (optsInner ?? "").trim();
        if (/\banchor\s*:/.test(trimmedOpts)) {
          const normalizedOpts = trimmedOpts.replace(
            /\banchor\s*:\s*(["'`])[^"'`]*\1/,
            'anchor: "middle"'
          );
          return `write(${snappedX}, ${snappedY}, ${thirdArg}, { ${normalizedOpts} })`;
        }
        const mergedOpts = trimmedOpts
          ? `{ anchor: "middle", ${trimmedOpts} }`
          : `{ anchor: "middle" }`;
        return `write(${snappedX}, ${snappedY}, ${thirdArg}, ${mergedOpts})`;
      }
    );

    const spotCallRegex =
      /\bspot\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,/g;
    processed = processed.replace(
      spotCallRegex,
      (full, xStr: string, yStr: string) => {
        const rawX = parseFloat(xStr);
        const rawY = parseFloat(yStr);
        const cell = findNearestGridCell(rawX, rawY, layout.cells);
        if (!cell) return full;
        return `spot(${cell.cx}, ${cell.cy},`;
      }
    );
  }

  // 3. Answer-Slot & Heading Anchor Resolver (when OCR lines or worksheet answerLines are present)
  let targetQ = extractRequestedQuestionNumber(userText);

  const isAnswerLineOcr = (l: OcrLineBox): boolean => {
    if (
      /(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM)\s*\d{2}|\b\d{2}[\/1I|]P\d{2}[\/1I|]Q\d+/i.test(
        l.text
      )
    ) {
      return false;
    }
    if (
      layout.answerLines.some(
        (al) => l.cy >= al.y - 15 && l.cy <= al.y + 8 && l.y <= al.y + 2
      )
    ) {
      return true;
    }
    const stripped = l.text
      .replace(/^\s*\d{1,2}\s*[.:…·\-_\s]*/, "")
      .replace(/\[\d+\]\s*$/, "")
      .trim();
    return (stripped.match(/[A-Za-z]/g) || []).length < 3;
  };

  const nonOrphanAnswerLinesForQ = layout.answerLines.filter(
    (s) => !s.isScrolledOffTopOrphan
  );
  const docLinesForQ =
    hasOcrLines && layout.ocrLines
      ? layout.ocrLines.filter(
          (l) =>
            l.y >= 20 &&
            l.x < 450 &&
            !/(?:\.pdf\b|\b\d+\s*\/\s*\d+\b|\b\d+%\s*\+?)/i.test(l.text)
        )
      : [];
  const docQuestionHeaders = docLinesForQ.filter((l) => {
    if (isAnswerLineOcr(l)) return false;
    const norm = l.text
      .replace(/^\s*[fIl|](\d)\b/, "1$1")
      .replace(/^\s*[Il|]\s*\./, "1.");
    return /^\s*(?:\d{1,2}|I|II|l)\s*(?:[.)\]:-]\s*|\s*(?:O\/N|0\/N|OIN|otN|M\/J|W\/J|WJ|F\/M|FIM)|\s+(?:\([a-zivx]+\)|The|Let|Given|A|An|In|For|Describe|Explain|Find|Use|Show|Hence|Evaluate|Prove|Differentiate|Integrate|Express|Sketch|Complete|Outline|State|Calculate|Solve|Give|Identify)\b)|^\s*:\s*-+use-integration/i.test(
      norm
    );
  });

  const findHeaderForQ = (qNum: string | null): OcrLineBox | null => {
    if (!qNum || docLinesForQ.length === 0) return null;
    const direct =
      docLinesForQ.find((l) => {
        if (l.x >= 360 || isAnswerLineOcr(l)) return false;
        const norm = l.text
          .replace(/^\s*[fIl|](\d)\b/, "1$1")
          .replace(/^\s*[Il|]\s*\./, "1.");
        return (
          new RegExp(`^\\s*(?:0\\s*)?${qNum}(?:\\.|\\s|$)`).test(norm) ||
          (qNum === "1" &&
            /^\s*(?:1|I|II|l)\s+(?:\([a-zivx]+\)|The|Let|Given|A|An|In|For|Find|Use|Show|Hence|Evaluate|Prove|Differentiate|Integrate|Express|Sketch|Describe|Explain|State|Calculate|Solve|Complete|Outline|Give|Identify)\b/i.test(
              l.text
            ))
        );
      }) ?? null;
    if (direct) return direct;

    // Fallback if the question number digit itself was occluded (e.g. Q9 above Q10)
    const qSlots = layout.answerLines.filter((s) => s.questionNumber === qNum);
    if (qSlots.length > 0) {
      const firstSlotY = qSlots[0].y;
      const promptCandidates = docLinesForQ.filter(
        (l) =>
          l.x < 360 &&
          l.y >= firstSlotY - 95 &&
          l.y <= firstSlotY - 12 &&
          /\b(?:Describe|Explain|State|Outline|Complete|Give|Identify|Include|Find|Use|Show)\b/i.test(
            l.text
          )
      );
      if (promptCandidates.length > 0) {
        return promptCandidates[0];
      }
    }
    return null;
  };

  // Locate target question header in OCR lines (if available), discarding any stale `targetQ` from an earlier page scroll
  let targetQHeader: OcrLineBox | null = findHeaderForQ(targetQ);
  if (targetQ) {
    const existsOnCurrentScreen =
      Boolean(targetQHeader) ||
      nonOrphanAnswerLinesForQ.some((s) => s.questionNumber === targetQ) ||
      layout.tables.some((t) => t.questionNumber === targetQ);
    if (
      !existsOnCurrentScreen &&
      (docQuestionHeaders.length > 0 ||
        nonOrphanAnswerLinesForQ.some((s) => Boolean(s.questionNumber)) ||
        layout.tables.some((t) => Boolean(t.questionNumber)))
    ) {
      targetQ = null;
      targetQHeader = null;
    }
  }

  // Auto-bind to the single active question on screen when `targetQ` is null
  if (!targetQ) {
    const activeScreenQNums = new Set<string>();
    for (const s of nonOrphanAnswerLinesForQ) {
      if (s.questionNumber && s.questionNumber !== "prev") {
        activeScreenQNums.add(s.questionNumber);
      }
    }
    for (const t of layout.tables) {
      if (t.questionNumber && t.questionNumber !== "prev") {
        activeScreenQNums.add(t.questionNumber);
      }
    }
    if (activeScreenQNums.size === 1) {
      targetQ = [...activeScreenQNums][0];
      targetQHeader = findHeaderForQ(targetQ) ?? docQuestionHeaders[0] ?? null;
    } else if (!targetQHeader && docQuestionHeaders.length === 1) {
      targetQHeader = docQuestionHeaders[0];
    }
  }

  // 3A. Snap `underline(x, y, w[, opts])` when targeting a requested question or when placed inside browser toolbar (`y < 165`) or right-margin card (`x >= 650`)
  if (hasOcrLines && layout.ocrLines) {
    const docLines = docLinesForQ;
    const evalCoordExpr = (expr: string): number => {
      const m = expr
        .trim()
        .match(/^(-?\d+(?:\.\d+)?)\s*(?:([+-])\s*(\d+(?:\.\d+)?))?$/);
      if (!m) return parseFloat(expr);
      const base = parseFloat(m[1]);
      if (!m[2] || !m[3]) return base;
      const delta = parseFloat(m[3]);
      return m[2] === "+" ? base + delta : base - delta;
    };
    const underlineRegex =
      /\bunderline\s*\(\s*(-?\d+(?:\.\d+)?(?:\s*[+-]\s*\d+(?:\.\d+)?)?)\s*,\s*(-?\d+(?:\.\d+)?(?:\s*[+-]\s*\d+(?:\.\d+)?)?)\s*,\s*(-?\d+(?:\.\d+)?(?:\s*[+-]\s*\d+(?:\.\d+)?)?)(\s*(?:,\s*\{[^{}]*\})?)\s*\)/g;
    let snappedPrimaryUnderline = false;
    let underlineCallIdx = 0;
    const activeHeader =
      targetQHeader ??
      (docQuestionHeaders.length === 1 ? docQuestionHeaders[0] : null);
    processed = processed.replace(
      underlineRegex,
      (full, xStr: string, yStr: string, wStr: string, optsRest: string) => {
        const rawX = evalCoordExpr(xStr);
        const rawY = evalCoordExpr(yStr);
        const rawW = evalCoordExpr(wStr);
        const curIdx = underlineCallIdx++;

        if (activeHeader && !snappedPrimaryUnderline && curIdx === 0) {
          if (
            Math.abs(rawY - activeHeader.bottomY) <= 80 ||
            Math.abs(rawY - activeHeader.bottomY) > 18 ||
            rawY < 165 ||
            rawX >= 450 ||
            rawW > 360
          ) {
            snappedPrimaryUnderline = true;
            const uW = Math.min(320, Math.max(140, activeHeader.w));
            return `underline(${activeHeader.x}, ${activeHeader.bottomY + 2}, ${uW}${optsRest || ""})`;
          }
        }

        if ((rawY < 165 || rawX >= 650) && docLines.length > 0) {
          const fallbackLine =
            docQuestionHeaders[curIdx] ??
            (curIdx === 0 ? activeHeader : null) ??
            docQuestionHeaders[0] ??
            docLines.find((l) =>
              /^\s*(?:\d{1,2}|I|II|l)\s+(?:Describe|Explain|Find|Use|Show|Complete|Outline|State|Calculate|Solve)\b|^\s*\d{1,2}\b|\b(?:Describe|Explain|Find|Use|Show|Complete|Outline)\b/i.test(
                l.text
              )
            ) ??
            docLines[0];
          const uW = Math.min(320, Math.max(140, fallbackLine.w || rawW));
          return `underline(${fallbackLine.x}, ${fallbackLine.bottomY + 2}, ${uW}${optsRest || ""})`;
        }

        return full;
      }
    );
  }

  // 3A-bis. Pure Question Papers with 0 Fillable Answer Slots & 0 Tables, OR Step-by-Step Card Requests,
  // OR Multi-Line Derivations Exceeding Available Slots / Extending Past y > 700:
  // Never allow floating `write(x, y, "...")` ink over printed exam/math text!
  {
    const strippedForSlotlessCheck = stripCommentsAndStrings(processed);
    if (/\bwrite\s*\(/.test(strippedForSlotlessCheck)) {
      const strayWriteRegex =
        /[ \t]*\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])((?:\\.|(?!\3)[\s\S])*)\3(?:\s*,\s*\{([^{}]*)\})?\s*\)\s*;?/g;
      const allWriteCalls = Array.from(processed.matchAll(strayWriteRegex));
      const maxWriteY =
        allWriteCalls.length > 0
          ? Math.max(...allWriteCalls.map((m) => parseFloat(m[2])))
          : 0;
      const hasTableMarksInCode =
        layout.tables.length > 0 &&
        /\b(?:check|cross)\s*\(/.test(strippedForSlotlessCheck);
      const writesOverflowSlotsOrScreen =
        !hasTableMarksInCode &&
        layout.tables.length === 0 &&
        allWriteCalls.length >= 4 &&
        (maxWriteY > 700 ||
          allWriteCalls.length > nonOrphanAnswerLinesForQ.length + 3);

      if (
        (nonOrphanAnswerLinesForQ.length === 0 && layout.tables.length === 0) ||
        (userWantsPageCard && !hasTableMarksInCode) ||
        writesOverflowSlotsOrScreen
      ) {
        const hasCardOrScene =
          /\b(?:paper|receipt|clipping|figure|scene3d|note|window)\s*\(/.test(
            strippedForSlotlessCheck
          );

        if (hasCardOrScene) {
          // Program already opened a `paper()` / `receipt()` / `clipping()` card: strip stray floating `write()` calls
          processed = processed.replace(strayWriteRegex, "");
        } else if (allWriteCalls.length > 0) {
          // Program has NO card at all: convert floating `write()` call(s) into a `paper()` card in `calmCardSpot`
          const calm = layout.calmCardSpot || {
            x: 16,
            y: 150,
            w: 290,
            h: 420,
          };
          const totalWrites = allWriteCalls.length;
          const isTallCard = totalWrites > 10;
          const cardY = isTallCard ? Math.min(calm.y, 90) : calm.y;
          const cardW = isTallCard ? Math.max(calm.w, 420) : calm.w;
          const cardX =
            calm.x + cardW > 1268 ? Math.max(16, 1268 - cardW) : calm.x;
          const cardH = isTallCard ? 580 : calm.h;
          const lineStep = isTallCard
            ? Math.max(
                17,
                Math.min(28, Math.floor((cardH - 55) / Math.max(1, totalWrites)))
              )
            : 30;
          const fontSize = isTallCard
            ? lineStep <= 19
              ? 13
              : lineStep <= 23
                ? 14
                : 15
            : 16;
          const maxCardLines = isTallCard
            ? Math.floor((cardH - 35) / lineStep)
            : Math.floor((cardH - 20) / lineStep);

          const cardTitle = targetQ ? `Question ${targetQ}` : "Solution";
          const activeHeader =
            targetQHeader ??
            (docQuestionHeaders.length === 1 ? docQuestionHeaders[0] : null);
          const needsUnderline =
            Boolean(activeHeader) &&
            !/\bunderline\s*\(/.test(strippedForSlotlessCheck);
          let writeIdx = 0;
          processed = processed.replace(
            strayWriteRegex,
            (_full, _xStr: string, _yStr: string, quote: string, body: string) => {
              const idx = writeIdx++;
              if (idx >= maxCardLines) {
                return "";
              }
              const yOff = (isTallCard ? 16 : 18) + idx * lineStep;
              const textStmt = `text(__p.x, __p.y + ${yOff}, ${quote}${body}${quote}, { size: ${fontSize} });`;
              if (idx === 0) {
                const uPrefix =
                  needsUnderline && activeHeader
                    ? `underline(${activeHeader.x}, ${activeHeader.bottomY + 2}, ${Math.min(320, Math.max(140, activeHeader.w))});\n`
                    : "";
                return `${uPrefix}const __p = paper(${cardX}, ${cardY}, ${cardW}, ${cardH}, { title: ${JSON.stringify(cardTitle)}, heading: "Worked Solution" });\n${textStmt}`;
              }
              return textStmt;
            }
          );
        }
      }
    }
  }

  // 3B. Snap non-table `write(x, y, "text"[, opts])` calls onto worksheet `answerLines`
  if (hasAnswerLines && !userWantsPageCard) {
    const nonOrphanLines = layout.answerLines.filter(
      (s) => !s.isScrolledOffTopOrphan
    );
    let candidateSlots =
      nonOrphanLines.length > 0 ? nonOrphanLines : layout.answerLines;
    if (targetQ) {
      const qFiltered = layout.answerLines.filter(
        (s) =>
          s.questionNumber === targetQ &&
          (!s.isScrolledOffTopOrphan || nonOrphanLines.length === 0)
      );
      if (qFiltered.length > 0) {
        candidateSlots = qFiltered;
      } else if (targetQHeader) {
        const belowHeader = nonOrphanLines.filter(
          (s) => s.y >= targetQHeader!.y
        );
        if (
          belowHeader.length > 0 &&
          belowHeader.every(
            (s) => Boolean(s.questionNumber) && s.questionNumber !== targetQ
          )
        ) {
          candidateSlots = [];
        } else {
          candidateSlots = belowHeader;
        }
      }
    }

    if (candidateSlots.length > 0) {
      const writeLiteralRegex =
        /\bwrite\s*\(\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(["'`])((?:\\.|(?!\3)[\s\S])*)\3(\s*,\s*\{([^{}]*)\})?\s*\)/g;

      interface WriteCallRecord {
        fullMatch: string;
        index: number;
        rawX: number;
        rawY: number;
        textBody: string;
        optsInner?: string;
        isTableCell: boolean;
        boundSlot?: ScreenBlankLine;
        boundSecondSlot?: ScreenBlankLine;
        secondSlotText?: string;
        cleanedText: string;
        mergedIntoPrev?: boolean;
      }

      const records: WriteCallRecord[] = [];
      for (const m of processed.matchAll(writeLiteralRegex)) {
        const rawX = parseFloat(m[1]);
        const rawY = parseFloat(m[2]);
        const quote = m[3];
        const rawBody = m[4];
        let unescaped = rawBody;
        try {
          if (quote === '"' || quote === "'") {
            unescaped = rawBody.replace(/\\(["'\\])/g, "$1");
          }
        } catch {
          // ignore
        }
        const inTable =
          findNearestGridCell(rawX, rawY, layout.cells) !== null ||
          layout.tables.some(
            (t) =>
              rawX >= t.x - 24 &&
              rawX <= t.x + t.w + 24 &&
              rawY >= t.y - 24 &&
              rawY <= t.y + t.h + 24
          ) ||
          /^[✓✔✅✗✘❌]$/.test(unescaped.trim());
        records.push({
          fullMatch: m[0],
          index: m.index ?? 0,
          rawX,
          rawY,
          textBody: unescaped,
          optsInner: m[6],
          isTableCell: inTable,
          cleanedText: unescaped.trim(),
        });
      }

      const nonTableRecords = records.filter((r) => !r.isTableCell);
      if (nonTableRecords.length > 0) {
        const claimedSlots = new Set<ScreenBlankLine>();
        const isContinuationSlot = (s: ScreenBlankLine): boolean =>
          Boolean(s.label && /\(line\s*\d+\)/i.test(s.label));
        const isPairedTitleSlot = (s?: ScreenBlankLine): boolean =>
          Boolean(
            s?.label &&
              /^(?:Architecture|Category|Type|Term|Name|Component|Register|Bus|Stage|Layer|Protocol|Device|Sensor|Method|Principle|Factor|Feature|Model|Topology|Mode|Technique|Interrupt|Hazard|Threat|Attack|Error|Gate|State|Address|Rule|Unit)\s*\d*$/i.test(
                s.label.trim()
              )
          );
        const isPairedDescSlot = (s?: ScreenBlankLine): boolean =>
          Boolean(
            s?.label &&
              /^(?:Description|Explanation|Purpose|Function|Role|Meaning|Definition|Use|Example|Reason|Effect)\s*\d*$/i.test(
                s.label.trim()
              )
          );
        const splitTitleAndDescription = (
          txt: string
        ): [string, string] | null => {
          const m = txt.match(
            /^([^–—:\n]{2,64}?)\s*(?:[–—]|\s-\s|:)\s*(.{10,})$/
          );
          if (!m) return null;
          const titlePart = m[1].trim();
          let descPart = m[2].trim();
          if (!titlePart || !descPart || /[.!?]$/.test(titlePart)) return null;
          if (/^[a-z]/.test(descPart)) {
            descPart = descPart[0].toUpperCase() + descPart.slice(1);
          }
          return [titlePart, descPart];
        };

        const buildSlotPrefixRegex = (slotLabel: string): RegExp => {
          const itemMatch = slotLabel.match(/^Item\s+(\d+)$/i);
          if (itemMatch) {
            return new RegExp(
              `^\\s*(?:Item\\s*)?${itemMatch[1]}\\s*[:\\-–—.)]\\s*(.+)$`,
              "i"
            );
          }
          const escapedLabel = slotLabel.replace(
            /[.*+?^${}()|[\]\\]/g,
            "\\$&"
          );
          return new RegExp(
            `^\\s*${escapedLabel}\\s*[:\\-–—.]\\s*(.+)$`,
            "i"
          );
        };

        const hasExplicitSlotPrefix = (text: string): boolean => {
          for (const slot of candidateSlots) {
            if (!slot.label) continue;
            if (buildSlotPrefixRegex(slot.label).test(text)) return true;
          }
          return false;
        };

        // Re-join consecutive write() calls where the LLM artificially split a single sentence mid-phrase
        // (e.g. Call i has no trailing sentence punctuation and Call i+1 starts with a lowercase letter),
        // ONLY when there are more write() calls than primary slots (excessCalls > 0).
        const primarySlots = candidateSlots.filter(
          (s) => !isContinuationSlot(s)
        );
        let excessCalls = nonTableRecords.length - primarySlots.length;
        const danglingConnectorRe =
          /\b(?:and|or|to|of|in|for|with|by|from|that|which|as|into|on|at|a|an|the|like|such|than|between|within|through|during|without|using|including|via|per|allowing|enabling|supporting|providing|improving|reducing)\s*$/i;

        for (let i = 0; i < nonTableRecords.length - 1; i++) {
          if (excessCalls <= 0) break;
          const recA = nonTableRecords[i];
          const recB = nonTableRecords[i + 1];
          if (
            !recA.mergedIntoPrev &&
            recA.cleanedText.length > 0 &&
            !/[.!?;:]$/.test(recA.cleanedText) &&
            /^[a-z]/.test(recB.cleanedText) &&
            !hasExplicitSlotPrefix(recB.cleanedText) &&
            recB.rawY >= recA.rawY + 10 &&
            recB.rawY - recA.rawY <= 60 &&
            (!/^[a-z]/.test(recA.cleanedText) ||
              danglingConnectorRe.test(recA.cleanedText))
          ) {
            recA.cleanedText = `${recA.cleanedText} ${recB.cleanedText}`
              .replace(/\s+/g, " ")
              .trim();
            recB.mergedIntoPrev = true;
            excessCalls--;
          }
        }

        const activeRecords = nonTableRecords.filter((r) => !r.mergedIntoPrev);

        // Immediately claim an adjacent continuation slot ("(line 2)") or paired "Description" slot
        // when the LLM emitted fewer write() calls than slots and combined both lines into one write() call.
        const tryClaimContinuationSlot = (
          rec: WriteCallRecord,
          slots: ScreenBlankLine[],
          claimed: Set<ScreenBlankLine>
        ): void => {
          if (!rec.boundSlot || rec.boundSecondSlot) return;

          let slotIdx = slots.indexOf(rec.boundSlot);
          let pool = slots;
          if (slotIdx < 0) {
            slotIdx = layout.answerLines.indexOf(rec.boundSlot);
            pool = layout.answerLines;
          }
          if (slotIdx < 0 || slotIdx + 1 >= pool.length) return;
          const nextSlot = pool[slotIdx + 1];
          if (claimed.has(nextSlot)) return;

          const remainingUnboundCount = activeRecords.filter(
            (r) => !r.boundSlot
          ).length;
          const remainingUnclaimedCount = slots.filter(
            (s) => !claimed.has(s)
          ).length;

          // Case A2: Paired Title (`Architecture N`) + `Description` slot where the LLM emitted 1 combined `Name – Description` write() call per pair
          if (
            isPairedTitleSlot(rec.boundSlot) &&
            isPairedDescSlot(nextSlot) &&
            remainingUnboundCount < remainingUnclaimedCount
          ) {
            const splitPair = splitTitleAndDescription(rec.cleanedText);
            if (splitPair) {
              rec.cleanedText = splitPair[0];
              rec.secondSlotText = splitPair[1];
              rec.boundSecondSlot = nextSlot;
              claimed.add(nextSlot);
              return;
            }
          }

          if (rec.cleanedText.length <= 82) return;
          if (activeRecords.length >= candidateSlots.length) {
            const hasAnotherUnprefixedRecordWaiting = activeRecords.some(
              (other) =>
                other !== rec &&
                !other.boundSlot &&
                !hasExplicitSlotPrefix(other.cleanedText)
            );
            if (hasAnotherUnprefixedRecordWaiting) return;
          }
          const remainingUnclaimedPrimaryCount = slots.filter(
            (s) => !claimed.has(s) && !isContinuationSlot(s)
          ).length;
          if (
            remainingUnboundCount >= remainingUnclaimedCount ||
            remainingUnboundCount > remainingUnclaimedPrimaryCount
          ) {
            return;
          }

          const isContinuation =
            !nextSlot.label || isContinuationSlot(nextSlot);
          if (isContinuation) {
            rec.boundSecondSlot = nextSlot;
            claimed.add(nextSlot);
          }
        };

        const tryMatchLabelPrefix = (
          rec: WriteCallRecord,
          pool: ScreenBlankLine[]
        ): boolean => {
          for (const slot of pool) {
            if (claimedSlots.has(slot) || !slot.label) continue;
            const prefixRegex = buildSlotPrefixRegex(slot.label);
            const match = rec.cleanedText.match(prefixRegex);
            if (match) {
              rec.boundSlot = slot;
              rec.cleanedText = match[1].trim();
              claimedSlots.add(slot);
              tryClaimContinuationSlot(rec, candidateSlots, claimedSlots);
              return true;
            }
          }
          return false;
        };

        // Pass 1: Semantic Label Prefix Binding (e.g., "Benefit 1: ...", "Drawback 2: ...", "Description: ...", "1. ...")
        for (const rec of activeRecords) {
          if (!tryMatchLabelPrefix(rec, candidateSlots)) {
            if (candidateSlots !== nonOrphanLines && nonOrphanLines.length > 0) {
              tryMatchLabelPrefix(rec, nonOrphanLines);
            }
          }
        }

        // Pass 2: Strictly Top-to-Bottom Sequential Slot Binding & Immediate 2-Line Continuation Reservation
        for (const rec of activeRecords) {
          if (rec.boundSlot) continue;

          const isSameTableSideAndNear = (s: ScreenBlankLine): boolean => {
            if (Math.abs(rec.rawY - s.writeY) > 160) return false;
            for (const t of layout.tables) {
              const recAbove = rec.rawY < t.y;
              const recBelow = rec.rawY > t.y + t.h;
              const slotAbove = s.y < t.y;
              const slotBelow = s.y > t.y + t.h;
              if ((recAbove && slotBelow) || (recBelow && slotAbove)) {
                return false;
              }
            }
            return true;
          };

          const nearbySameSidePool = candidateSlots.filter(
            (s) => !claimedSlots.has(s) && isSameTableSideAndNear(s)
          );
          const effectivePool =
            nearbySameSidePool.length > 0
              ? nearbySameSidePool
              : candidateSlots.filter((s) => !claimedSlots.has(s));

          const remainingUnbound = activeRecords.filter(
            (r) => !r.boundSlot
          ).length;
          const remainingUnclaimedPrimary = effectivePool.filter(
            (s) => !isContinuationSlot(s)
          ).length;
          const preferNonContinuation =
            remainingUnbound <= remainingUnclaimedPrimary &&
            remainingUnclaimedPrimary > 0;

          const chosenSlot = preferNonContinuation
            ? (effectivePool.find((s) => !isContinuationSlot(s)) ??
              effectivePool[0])
            : effectivePool[0];

          if (chosenSlot) {
            rec.boundSlot = chosenSlot;
            claimedSlots.add(chosenSlot);
            tryClaimContinuationSlot(rec, candidateSlots, claimedSlots);
          }
        }

        // Pass 3: Paired Title + Description Slot De-duplication (Case A1)
        // When `slot[i]` is a Title slot (e.g. `Architecture 1`) and `slot[i + 1]` is a `Description` slot with its own `write()` call:
        // 1. Strip any trailing ` – <description>` clause from `slot[i]`'s `write()` call so `slot[i]` has ONLY the concise Name/Title.
        // 2. Strip any redundant `Description: ` or repeated `<Acronym>: ` prefix (e.g. `MIMD: `, `SIMD: `, `MISD: `) from `slot[i + 1]`'s `write()` call.
        for (const rec of activeRecords) {
          if (!rec.boundSlot || !isPairedTitleSlot(rec.boundSlot)) continue;
          const sIdx = candidateSlots.indexOf(rec.boundSlot);
          if (sIdx < 0 || sIdx + 1 >= candidateSlots.length) continue;
          const nextSlot = candidateSlots[sIdx + 1];
          if (!isPairedDescSlot(nextSlot)) continue;

          const descRec = activeRecords.find((r) => r.boundSlot === nextSlot);
          if (descRec) {
            const splitPair = splitTitleAndDescription(rec.cleanedText);
            if (splitPair) {
              rec.cleanedText = splitPair[0];
            }

            descRec.cleanedText = descRec.cleanedText
              .replace(
                /^(?:Description|Explanation|Purpose|Function|Role|Meaning|Definition)\s*[:\-–—.]\s*/i,
                ""
              )
              .trim();

            const acronymTokens: string[] = [];
            const parenMatch = rec.cleanedText.match(
              /\(([A-Za-z0-9/-]{2,12})\)/
            );
            if (parenMatch) {
              acronymTokens.push(parenMatch[1]);
            }
            if (rec.cleanedText.length <= 16) {
              acronymTokens.push(rec.cleanedText.trim());
            }
            for (const tok of acronymTokens) {
              const escapedTok = tok.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
              const acrPrefixRe = new RegExp(
                `^${escapedTok}\\s*[:\\-–—]\\s*(.+)$`,
                "i"
              );
              const acrMatch = descRec.cleanedText.match(acrPrefixRe);
              if (acrMatch) {
                let rest = acrMatch[1].trim();
                if (/^[a-z]/.test(rest)) {
                  rest = rest[0].toUpperCase() + rest.slice(1);
                }
                descRec.cleanedText = rest;
                break;
              }
            }
          }
        }

        // Rebuild `processed` from back to front so string indices remain valid
        for (let i = nonTableRecords.length - 1; i >= 0; i--) {
          const rec = nonTableRecords[i];
          if (rec.mergedIntoPrev) {
            const afterIdx = rec.index + rec.fullMatch.length;
            const trailingSemi = processed.slice(afterIdx).match(/^[ \t]*;[ \t]*/);
            const removeLen =
              rec.fullMatch.length + (trailingSemi ? trailingSemi[0].length : 0);
            processed =
              processed.slice(0, rec.index) +
              processed.slice(rec.index + removeLen);
            continue;
          }
          if (!rec.boundSlot) continue;
          const slot = rec.boundSlot;
          let replacement: string;
          if (rec.boundSecondSlot) {
            const slot2 = rec.boundSecondSlot;
            const [part1, part2] = rec.secondSlotText
              ? [rec.cleanedText, rec.secondSlotText]
              : splitTwoLineWorksheetText(rec.cleanedText, 72);
            const opts1 = buildWorksheetWriteOptions(
              slot.maxWidth,
              rec.optsInner
            );
            const opts2 = buildWorksheetWriteOptions(
              slot2.maxWidth,
              rec.optsInner
            );
            if (part2) {
              replacement = `write(${slot.writeX}, ${slot.writeY}, ${JSON.stringify(part1)}, ${opts1}); write(${slot2.writeX}, ${slot2.writeY}, ${JSON.stringify(part2)}, ${opts2})`;
            } else {
              replacement = `write(${slot.writeX}, ${slot.writeY}, ${JSON.stringify(part1)}, ${opts1})`;
            }
          } else {
            const opts = buildWorksheetWriteOptions(
              slot.maxWidth,
              rec.optsInner
            );
            replacement = `write(${slot.writeX}, ${slot.writeY}, ${JSON.stringify(rec.cleanedText)}, ${opts})`;
          }

          processed =
            processed.slice(0, rec.index) +
            replacement +
            processed.slice(rec.index + rec.fullMatch.length);
        }
      }
    }
  }

  return processed;
}

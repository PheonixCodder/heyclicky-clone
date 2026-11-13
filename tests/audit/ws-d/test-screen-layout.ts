import fs from "node:fs";
import path from "node:path";
import zlib from "node:zlib";
import { performance } from "node:perf_hooks";
import { fileURLToPath } from "node:url";
import jpeg from "../../../src/backend/node_modules/jpeg-js/index.js";
import {
  analyzeScreenLayout,
  snapProgramToScreenLayout,
  type ScreenLayoutAnalysis,
} from "../../../src/backend/src/utils/screenLayout.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../..");
const CORPUS_DIR = path.join(REPO_ROOT, "tests/audit/ws-d/corpus");
const EVIDENCE_DIR = path.join(REPO_ROOT, "docs/audit/ws-d");
fs.mkdirSync(CORPUS_DIR, { recursive: true });
fs.mkdirSync(EVIDENCE_DIR, { recursive: true });

// ============================================================================
// Raster Canvas Helper for Generating Realistic & Edge-Case JPEGs
// ============================================================================
class RasterCanvas {
  width: number;
  height: number;
  data: Uint8Array;

  constructor(
    width: number,
    height: number,
    bg: [number, number, number] = [255, 255, 255]
  ) {
    this.width = width;
    this.height = height;
    this.data = new Uint8Array(width * height * 4);
    this.fillRect(0, 0, width, height, bg);
  }

  setPixel(x: number, y: number, rgb: [number, number, number]) {
    const ix = Math.round(x);
    const iy = Math.round(y);
    if (ix < 0 || ix >= this.width || iy < 0 || iy >= this.height) return;
    const idx = (iy * this.width + ix) * 4;
    this.data[idx] = rgb[0];
    this.data[idx + 1] = rgb[1];
    this.data[idx + 2] = rgb[2];
    this.data[idx + 3] = 255;
  }

  fillRect(
    x0: number,
    y0: number,
    w: number,
    h: number,
    rgb: [number, number, number]
  ) {
    const sx = Math.max(0, Math.floor(x0));
    const sy = Math.max(0, Math.floor(y0));
    const ex = Math.min(this.width, Math.ceil(x0 + w));
    const ey = Math.min(this.height, Math.ceil(y0 + h));
    for (let y = sy; y < ey; y++) {
      let idx = (y * this.width + sx) * 4;
      for (let x = sx; x < ex; x++) {
        this.data[idx] = rgb[0];
        this.data[idx + 1] = rgb[1];
        this.data[idx + 2] = rgb[2];
        this.data[idx + 3] = 255;
        idx += 4;
      }
    }
  }

  drawHLine(
    x0: number,
    x1: number,
    y: number,
    thickness = 2,
    rgb: [number, number, number] = [30, 30, 30],
    skewTotalY = 0
  ) {
    const span = Math.max(1, x1 - x0);
    for (let x = x0; x <= x1; x++) {
      const cy = Math.round(y + ((x - x0) / span) * skewTotalY);
      for (
        let dy = -Math.floor(thickness / 2);
        dy <= Math.floor((thickness - 1) / 2);
        dy++
      ) {
        this.setPixel(x, cy + dy, rgb);
      }
    }
  }

  drawVLine(
    x: number,
    y0: number,
    y1: number,
    thickness = 2,
    rgb: [number, number, number] = [30, 30, 30]
  ) {
    for (let y = y0; y <= y1; y++) {
      for (
        let dx = -Math.floor(thickness / 2);
        dx <= Math.floor((thickness - 1) / 2);
        dx++
      ) {
        this.setPixel(x + dx, y, rgb);
      }
    }
  }

  drawTable(opts: {
    xLines: number[];
    yLines: number[];
    borderRgb?: [number, number, number];
    thickness?: number;
    drawH?: boolean;
    drawV?: boolean;
    skewY?: number;
    skipVInRow?: number; // simulate merged row
    skipVCols?: number[]; // which internal V lines to skip in skipVInRow
  }) {
    const rgb = opts.borderRgb ?? [25, 25, 25];
    const t = opts.thickness ?? 2;
    const drawH = opts.drawH ?? true;
    const drawV = opts.drawV ?? true;
    const skewY = opts.skewY ?? 0;
    const x0 = opts.xLines[0];
    const x1 = opts.xLines[opts.xLines.length - 1];
    const y0 = opts.yLines[0];
    const y1 = opts.yLines[opts.yLines.length - 1];

    if (drawH) {
      for (const y of opts.yLines) {
        this.drawHLine(x0, x1, y, t, rgb, skewY);
      }
    }
    if (drawV) {
      for (let r = 0; r < opts.yLines.length - 1; r++) {
        const ry0 = opts.yLines[r];
        const ry1 = opts.yLines[r + 1];
        for (let c = 0; c < opts.xLines.length; c++) {
          if (
            opts.skipVInRow === r &&
            opts.skipVCols &&
            opts.skipVCols.includes(c)
          ) {
            continue;
          }
          this.drawVLine(opts.xLines[c], ry0, ry1, t, rgb);
        }
      }
    } else if (opts.drawV === false && opts.skipVInRow === undefined) {
      // no vertical lines at all
    }
    void y0;
    void y1;
  }

  drawDottedLine(opts: {
    x0: number;
    x1: number;
    y: number;
    dotW: number;
    gapW: number;
    thickness?: number;
    rgb?: [number, number, number];
  }) {
    const rgb = opts.rgb ?? [40, 40, 40];
    const t = opts.thickness ?? 2;
    let x = opts.x0;
    while (x + opts.dotW <= opts.x1) {
      this.fillRect(x, opts.y - Math.floor(t / 2), opts.dotW, t, rgb);
      x += opts.dotW + opts.gapW;
    }
  }

  drawPrintedTextLine(
    x0: number,
    x1: number,
    y: number,
    rgb: [number, number, number] = [35, 35, 35]
  ) {
    // Realistic 12px tall words separated by 14px spaces
    let x = x0;
    let seed = (x0 * 31 + y * 17) >>> 0;
    const rand = () => {
      seed = (seed * 1664525 + 1013904223) >>> 0;
      return seed / 4294967296;
    };
    while (x < x1 - 40) {
      const wordW = 24 + Math.floor(rand() * 55);
      for (let gx = x; gx < Math.min(x1, x + wordW); gx += 7) {
        this.fillRect(gx, y - 5, 5, 11, rgb);
      }
      x += wordW + 14 + Math.floor(rand() * 8);
    }
  }

  encodeJpeg(quality = 92): Buffer {
    return Buffer.from(
      jpeg.encode({ data: this.data, width: this.width, height: this.height }, quality).data
    );
  }

  toDataUrl(quality = 92): { buf: Buffer; dataUrl: string } {
    const buf = this.encodeJpeg(quality);
    return {
      buf,
      dataUrl: `data:image/jpeg;base64,${buf.toString("base64")}`,
    };
  }
}

// Create a valid 16x16 PNG buffer using node:zlib
function createValidPngBuffer(w = 16, h = 16): Buffer {
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  const crcTable = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    }
    crcTable[n] = c >>> 0;
  }
  const crc32 = (buf: Buffer) => {
    let c = 0xffffffff;
    for (let i = 0; i < buf.length; i++) {
      c = crcTable[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
    }
    return (c ^ 0xffffffff) >>> 0;
  };
  const chunk = (type: string, data: Buffer) => {
    const len = Buffer.alloc(4);
    len.writeUInt32BE(data.length, 0);
    const typeBuf = Buffer.from(type, "ascii");
    const crcBuf = Buffer.alloc(4);
    crcBuf.writeUInt32BE(crc32(Buffer.concat([typeBuf, data])), 0);
    return Buffer.concat([len, typeBuf, data, crcBuf]);
  };
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0);
  ihdr.writeUInt32BE(h, 4);
  ihdr[8] = 8; // bit depth
  ihdr[9] = 2; // RGB
  const rawScanlines = Buffer.alloc(h * (1 + w * 3), 255);
  for (let y = 0; y < h; y++) rawScanlines[y * (1 + w * 3)] = 0;
  const idat = zlib.deflateSync(rawScanlines);
  return Buffer.concat([
    signature,
    chunk("IHDR", ihdr),
    chunk("IDAT", idat),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

interface CorpusCase {
  id: string;
  category: "table" | "answer_lines" | "distractor" | "edge_resolution_or_corrupt";
  description: string;
  width: number;
  height: number;
  expectedTables: number;
  expectedTableRows?: number;
  expectedTableCols?: number;
  expectedHeaderFirstRow?: boolean;
  expectedAnswerLines: number;
  expectNull?: boolean;
  buildDataUrl: () => { dataUrl: string; savedFile?: string };
}

const corpusCases: CorpusCase[] = [];

function addCanvasCase(
  meta: Omit<CorpusCase, "buildDataUrl">,
  drawFn: (c: RasterCanvas) => void,
  bg: [number, number, number] = [255, 255, 255],
  quality = 92
) {
  corpusCases.push({
    ...meta,
    buildDataUrl: () => {
      const c = new RasterCanvas(meta.width, meta.height, bg);
      drawFn(c);
      const { buf, dataUrl } = c.toDataUrl(quality);
      const fileName = `${meta.id}.jpg`;
      fs.writeFileSync(path.join(CORPUS_DIR, fileName), buf);
      return { dataUrl, savedFile: `tests/audit/ws-d/corpus/${fileName}` };
    },
  });
}

// ============================================================================
// Category 1: Tables (24 cases)
// ============================================================================
addCanvasCase(
  {
    id: "01_table_3col_standard",
    category: "table",
    description: "Standard 3-col x 5-row (1 header + 4 data) worksheet table at y=200..460",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 5,
    expectedTableCols: 3,
    expectedHeaderFirstRow: true,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 245, 300, 355, 410, 465],
    });
  }
);

addCanvasCase(
  {
    id: "02_table_4col_standard",
    category: "table",
    description: "Standard 4-col x 6-row (1 header + 5 data) table at y=190..500",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 6,
    expectedTableCols: 4,
    expectedHeaderFirstRow: true,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [140, 480, 660, 840, 1020],
      yLines: [190, 235, 288, 341, 394, 447, 500],
    });
  }
);

addCanvasCase(
  {
    id: "03_table_2col_standard",
    category: "table",
    description: "Standard 2-col x 4-row table at y=220..420",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 2,
    expectedHeaderFirstRow: true,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [200, 620, 820],
      yLines: [220, 265, 315, 365, 415],
    });
  }
);

addCanvasCase(
  {
    id: "04_table_6col_8row_dense",
    category: "table",
    description: "Dense 6-col x 8-row spreadsheet grid at y=180..580",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 8,
    expectedTableCols: 6,
    expectedHeaderFirstRow: true,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [100, 300, 450, 600, 750, 900, 1050],
      yLines: [180, 220, 270, 320, 370, 420, 470, 520, 570],
    });
  }
);

addCanvasCase(
  {
    id: "05_table_borderless_whitespace",
    category: "table",
    description: "Borderless 3-col table aligned by whitespace only (no border lines)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    const rows = [220, 275, 330, 385, 440];
    const cols = [180, 520, 740];
    for (const ry of rows) {
      for (const cx of cols) {
        c.drawPrintedTextLine(cx, cx + 120, ry);
      }
    }
  }
);

addCanvasCase(
  {
    id: "06_table_horizontal_borders_only",
    category: "table",
    description: "Modern UI / Booktabs 3-col table with horizontal borders only (no vertical lines)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [210, 255, 310, 365, 420],
      drawH: true,
      drawV: false,
    });
  }
);

addCanvasCase(
  {
    id: "07_table_vertical_borders_only",
    category: "table",
    description: "Table with vertical column dividers only and no horizontal lines",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [210, 255, 310, 365, 420],
      drawH: false,
      drawV: true,
    });
  }
);

addCanvasCase(
  {
    id: "08_table_merged_middle_row",
    category: "table",
    description: "3-col table where row 2 has merged internal cells (only outer left/right V borders in row 2)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 5,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 250, 300, 350, 400, 450],
      skipVInRow: 2,
      skipVCols: [1, 2],
    });
  }
);

addCanvasCase(
  {
    id: "09_table_merged_title_banner_row",
    category: "table",
    description: "3-col table with merged top title banner row (row 0 has only 2 outer V-lines, rows 1..4 have 4 V-lines)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 5,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 245, 300, 355, 410, 465],
      skipVInRow: 0,
      skipVCols: [1, 2],
    });
  }
);

addCanvasCase(
  {
    id: "10_table_dark_mode_slate",
    category: "table",
    description: "Dark-mode 3-col table (bg rgb(30,34,42), light borders rgb(220,225,235))",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 250, 310, 370, 430],
      borderRgb: [220, 225, 235],
    });
  },
  [30, 34, 42]
);

addCanvasCase(
  {
    id: "11_table_dark_mode_oled",
    category: "table",
    description: "OLED dark-mode 4-col table (bg rgb(15,15,15), white borders rgb(245,245,245))",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 5,
    expectedTableCols: 4,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [160, 480, 640, 800, 960],
      yLines: [200, 245, 300, 355, 410, 465],
      borderRgb: [245, 245, 245],
    });
  },
  [15, 15, 15]
);

addCanvasCase(
  {
    id: "12_table_medium_gray_bg",
    category: "table",
    description: "3-col table on medium-gray shaded background (bg rgb(148,150,154) < 160 luma cutoff, black borders)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 250, 310, 370, 430],
      borderRgb: [10, 10, 10],
    });
  },
  [148, 150, 154]
);

addCanvasCase(
  {
    id: "13_table_faint_low_contrast_borders",
    category: "table",
    description: "Light UI table with subtle CSS borders (rgb(232,234,238) on white rgb(252,252,252), luma drop = 19 < 28)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 250, 310, 370, 430],
      borderRgb: [232, 234, 238],
    });
  },
  [252, 252, 252]
);

addCanvasCase(
  {
    id: "14_table_slight_skew_2px",
    category: "table",
    description: "Slightly skewed CamScanner 3-col table (+2px vertical drift across horizontal lines)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [210, 260, 315, 370, 425],
      skewY: 2,
      thickness: 3,
    });
  }
);

addCanvasCase(
  {
    id: "15_table_heavy_skew_12px",
    category: "table",
    description: "Skewed scan 3-col table (+12px vertical drift across 740px width, exceeding +-6px tracker)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [210, 265, 320, 375, 430],
      skewY: 12,
      thickness: 2,
    });
  }
);

addCanvasCase(
  {
    id: "16_table_near_top_y145_header_heuristic",
    category: "table",
    description: "Full 3-col table with visible 25px header row placed at y=145 (< 175 heuristic cutoff)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 5,
    expectedTableCols: 3,
    expectedHeaderFirstRow: true, // Real header is at y=145..170, but screenLayout.ts forces hasVisibleHeader=false when yMin < 175!
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [145, 170, 225, 280, 335, 390],
    });
  }
);

addCanvasCase(
  {
    id: "17_table_top_region_y70_to_210",
    category: "table",
    description: "3-col table in fullscreen PDF/app at y=70..210 (top 2 borders above y=135 scan start)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 3,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [70, 115, 165, 215],
    });
  }
);

addCanvasCase(
  {
    id: "18_table_bottom_region_y560_to_712",
    category: "table",
    description: "3-col table near bottom at y=560..712 (bottom border below H-20 = 700 scan limit)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 3,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [560, 610, 660, 712],
    });
  }
);

addCanvasCase(
  {
    id: "19_table_tall_rows_130px",
    category: "table",
    description: "3-col table with tall multi-paragraph rows (rowHeight = 130px > 115px limit)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 3,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [180, 310, 440, 570],
    });
  }
);

addCanvasCase(
  {
    id: "20_table_compact_rows_6px",
    category: "table",
    description: "Compact grid with 6px row spacing (< 7px dy minimum)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 206, 212, 218, 224],
      thickness: 1,
    });
  }
);

addCanvasCase(
  {
    id: "21_table_narrow_checkbox_col_15px",
    category: "table",
    description: "3-col table where Col 2 and Col 3 are narrow 15px checkbox columns (< 20px xLines filter)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 600, 615, 630],
      yLines: [200, 250, 300, 350, 400],
    });
  }
);

addCanvasCase(
  {
    id: "22_table_narrow_total_width_150px",
    category: "table",
    description: "Compact 2-col lookup table with total width 150px (< 170px bestLen threshold)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 2,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [200, 275, 350],
      yLines: [200, 245, 290, 335, 380],
    });
  }
);

addCanvasCase(
  {
    id: "23_table_two_tables_on_one_screen",
    category: "table",
    description: "Two separate 3-col tables on the same screen (Table 1 at y=180..320, Table 2 at y=400..580)",
    width: 1280,
    height: 720,
    expectedTables: 2,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 520, 680, 840],
      yLines: [180, 225, 270, 315],
    });
    c.drawTable({
      xLines: [180, 520, 680, 840],
      yLines: [400, 445, 490, 535, 580],
    });
  }
);

addCanvasCase(
  {
    id: "24_table_with_red_highlight_bridge",
    category: "table",
    description: "3-col table with a red user highlight loop crossing horizontal & vertical borders",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedHeaderFirstRow: true,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 245, 300, 355, 410],
    });
    // Red highlight ring across borders
    c.drawHLine(520, 780, 245, 4, [235, 45, 45]);
    c.drawVLine(560, 220, 290, 4, [235, 45, 45]);
  }
);

// ============================================================================
// Category 2: Worksheet Answer Lines (14 cases)
// ============================================================================
addCanvasCase(
  {
    id: "25_lines_dotted_4_standard",
    category: "answer_lines",
    description: "Worksheet with 4 standard dotted lines (dotW=3, gapW=4, span=620px, >85 dots)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 285, 330, 375]) {
      c.drawDottedLine({ x0: 160, x1: 780, y, dotW: 3, gapW: 4 });
    }
  }
);

addCanvasCase(
  {
    id: "26_lines_dotted_8_fullwidth",
    category: "answer_lines",
    description: "Worksheet with 8 full-width dotted lines (dotW=2, gapW=4, span=750px)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 8,
  },
  (c) => {
    for (let i = 0; i < 8; i++) {
      c.drawDottedLine({
        x0: 150,
        x1: 900,
        y: 210 + i * 42,
        dotW: 2,
        gapW: 4,
      });
    }
  }
);

addCanvasCase(
  {
    id: "27_combo_table_plus_4_dotted_below",
    category: "answer_lines",
    description: "Combined 3-col table (y=190..350) + 4 dotted answer lines below (y=420..540)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 3,
    expectedTableCols: 3,
    expectedAnswerLines: 4,
  },
  (c) => {
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [190, 235, 290, 345],
    });
    for (const y of [420, 460, 500, 540]) {
      c.drawDottedLine({ x0: 180, x1: 860, y, dotW: 3, gapW: 4 });
    }
  }
);

addCanvasCase(
  {
    id: "28_combo_dotted_above_and_below_table",
    category: "answer_lines",
    description: "2 dotted lines above (y=195, 230) + 3-col table (y=270..430) + 2 dotted lines below (y=480, 520)",
    width: 1280,
    height: 720,
    expectedTables: 1,
    expectedTableRows: 3,
    expectedTableCols: 3,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [195, 230]) {
      c.drawDottedLine({ x0: 180, x1: 860, y, dotW: 3, gapW: 4 });
    }
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [270, 315, 370, 425],
    });
    for (const y of [480, 520]) {
      c.drawDottedLine({ x0: 180, x1: 860, y, dotW: 3, gapW: 4 });
    }
  }
);

addCanvasCase(
  {
    id: "29_lines_solid_underline_blanks",
    category: "answer_lines",
    description: "Worksheet with 4 solid underline answer blanks (span=620px, 2px thick solid rule)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4, // Solid blank lines should be detected as answer lines, but screenLayout.ts only detects dot chains!
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawHLine(160, 780, y, 2, [40, 40, 40]);
    }
  }
);

addCanvasCase(
  {
    id: "30_lines_dashed_10px_blanks",
    category: "answer_lines",
    description: "Worksheet with 4 dashed answer lines (dashW=10px > 8px limit, gapW=6px, span=640px)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawDottedLine({ x0: 160, x1: 800, y, dotW: 10, gapW: 6 });
    }
  }
);

addCanvasCase(
  {
    id: "31_lines_wide_gap_14px_dotted",
    category: "answer_lines",
    description: "Worksheet with 4 wide-spaced dotted lines (dotW=3px, gapW=14px > 12px gap limit)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawDottedLine({ x0: 140, x1: 1050, y, dotW: 3, gapW: 14 });
    }
  }
);

addCanvasCase(
  {
    id: "32_lines_short_span_380px_dotted",
    category: "answer_lines",
    description: "Worksheet with 4 short fill-in-the-blank dotted lines (span=380px < W*0.35 = 448px)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawDottedLine({ x0: 200, x1: 580, y, dotW: 3, gapW: 3 });
    }
  }
);

addCanvasCase(
  {
    id: "33_lines_40_dots_under_50_threshold",
    category: "answer_lines",
    description: "Worksheet with 4 dotted lines having 42 dots each (span=504px >= 448px, but bestDots=42 < 50)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawDottedLine({ x0: 160, x1: 664, y, dotW: 4, gapW: 8 });
    }
  }
);

addCanvasCase(
  {
    id: "34_lines_top_of_page_y140_165",
    category: "answer_lines",
    description: "2 dotted answer lines near top of page (y=140, 165 < 180 scan start) + 2 at y=200, 235",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [140, 165, 200, 235]) {
      c.drawDottedLine({ x0: 160, x1: 780, y, dotW: 3, gapW: 4 });
    }
  }
);

addCanvasCase(
  {
    id: "35_lines_faint_gray_dots_luma175",
    category: "answer_lines",
    description: "4 light-gray printed dotted lines (rgb(175,175,175), luma=175 >= 162 threshold)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawDottedLine({
        x0: 160,
        x1: 780,
        y,
        dotW: 3,
        gapW: 4,
        rgb: [175, 175, 175],
      });
    }
  }
);

addCanvasCase(
  {
    id: "36_lines_cream_shaded_paper_luma180",
    category: "answer_lines",
    description: "4 dark dotted lines on warm cream/gray textbook box (paper rgb(180,178,172) <= 188 threshold)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 290, 340, 390]) {
      c.drawDottedLine({ x0: 160, x1: 780, y, dotW: 3, gapW: 4 });
    }
  },
  [180, 178, 172]
);

addCanvasCase(
  {
    id: "37_lines_tight_vertical_spacing_15px",
    category: "answer_lines",
    description: "4 dotted answer lines with tight 15px line spacing (< 20px deduplication gap)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [240, 255, 270, 285]) {
      c.drawDottedLine({ x0: 160, x1: 780, y, dotW: 3, gapW: 4 });
    }
  }
);

addCanvasCase(
  {
    id: "38_lines_right_aligned_form_blanks",
    category: "answer_lines",
    description: "4 right-half dotted answer lines at x=580..1120 (span=540px, bestDots=77)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [230, 280, 330, 380]) {
      c.drawDottedLine({ x0: 580, x1: 1120, y, dotW: 3, gapW: 4 });
    }
  }
);

// ============================================================================
// Category 3: False-Positive / Distractor Screens (12 cases)
// ============================================================================
addCanvasCase(
  {
    id: "39_distractor_printed_paragraphs",
    category: "distractor",
    description: "Dense printed textbook page (18 lines of body text, no tables or blank lines)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    for (let i = 0; i < 18; i++) {
      c.drawPrintedTextLine(140, 820, 160 + i * 26);
    }
  }
);

addCanvasCase(
  {
    id: "40_distractor_math_exam_paper",
    category: "distractor",
    description: "Math exam paper with printed problem statements and short fraction bars (no blank answer lines)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    for (const y of [170, 200, 280, 310, 410, 440]) {
      c.drawPrintedTextLine(140, 760, y);
    }
    // Short math fraction bars (60-90px wide, must not trigger table or answer line)
    c.drawHLine(220, 300, 240, 2, [20, 20, 20]);
    c.drawHLine(220, 310, 355, 2, [20, 20, 20]);
    c.drawHLine(220, 290, 485, 2, [20, 20, 20]);
  }
);

addCanvasCase(
  {
    id: "41_distractor_math_coordinate_axes",
    category: "distractor",
    description: "Math exam paper with 2D x/y coordinate axes and curve (single H-axis and single V-axis)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawHLine(180, 680, 420, 2, [20, 20, 20]);
    c.drawVLine(260, 180, 520, 2, [20, 20, 20]);
    c.drawPrintedTextLine(140, 720, 150);
  }
);

addCanvasCase(
  {
    id: "42_distractor_code_editor_dark",
    category: "distractor",
    description: "Dark-theme code editor (VS Code style with sidebar, gutter, and code lines)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.fillRect(0, 0, 240, 720, [37, 37, 38]);
    c.drawVLine(240, 0, 720, 2, [65, 65, 65]);
    c.drawHLine(0, 1280, 35, 2, [65, 65, 65]);
    c.drawHLine(0, 1280, 695, 2, [0, 122, 204]);
    for (let i = 0; i < 20; i++) {
      c.drawPrintedTextLine(280 + (i % 3) * 24, 780, 70 + i * 28, [
        180, 210, 240,
      ]);
    }
  },
  [30, 30, 30]
);

addCanvasCase(
  {
    id: "43_distractor_code_editor_light_split_panes",
    category: "distractor",
    description: "Light-theme IDE with 3 horizontal panel splitters and 3 vertical splitters (false-positive table trap!)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    // 3 horizontal splitters + 3 vertical splitters spanning x=100..1100, y=180..380
    c.drawHLine(100, 1100, 180, 2, [100, 100, 100]);
    c.drawHLine(100, 1100, 280, 2, [100, 100, 100]);
    c.drawHLine(100, 1100, 380, 2, [100, 100, 100]);
    c.drawVLine(100, 180, 380, 2, [100, 100, 100]);
    c.drawVLine(600, 180, 380, 2, [100, 100, 100]);
    c.drawVLine(1100, 180, 380, 2, [100, 100, 100]);
  },
  [250, 250, 250]
);

addCanvasCase(
  {
    id: "44_distractor_blank_light_desktop",
    category: "distractor",
    description: "Blank white canvas (rgb(250,250,250))",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  () => {},
  [250, 250, 250]
);

addCanvasCase(
  {
    id: "45_distractor_blank_dark_desktop",
    category: "distractor",
    description: "Blank dark desktop (rgb(20,24,30))",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  () => {},
  [20, 24, 30]
);

addCanvasCase(
  {
    id: "46_distractor_high_noise_jpeg",
    category: "distractor",
    description: "High-noise pseudo-random grain on light page (quality=60)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    let s = 1234567;
    for (let i = 0; i < c.data.length; i += 4) {
      s = (s * 1664525 + 1013904223) >>> 0;
      const v = 205 + ((s >>> 24) % 50);
      c.data[i] = v;
      c.data[i + 1] = v;
      c.data[i + 2] = v;
    }
  },
  [235, 235, 235],
  60
);

addCanvasCase(
  {
    id: "47_distractor_ruled_notebook_lines_only",
    category: "distractor",
    description: "Ruled notebook sheet with 10 solid horizontal lines and 1 left margin line (not a table or dotted line)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    for (let i = 0; i < 10; i++) {
      c.drawHLine(120, 1080, 180 + i * 38, 2, [90, 130, 200]);
    }
    c.drawVLine(200, 140, 600, 2, [220, 100, 100]);
  }
);

addCanvasCase(
  {
    id: "48_distractor_bar_chart_with_grid",
    category: "distractor",
    description: "Bar chart with 5 horizontal gridlines and 4 vertical bar outlines (false-positive table trap!)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    for (const y of [200, 260, 320, 380, 440]) {
      c.drawHLine(200, 800, y, 2, [40, 40, 40]);
    }
    for (const x of [200, 350, 500, 650, 800]) {
      c.drawVLine(x, 200, 440, 2, [40, 40, 40]);
    }
  }
);

addCanvasCase(
  {
    id: "49_distractor_flowchart_boxes",
    category: "distractor",
    description: "Flowchart with 3 separate small boxes connected by arrows",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    for (const bx of [200, 480, 760]) {
      c.drawHLine(bx, bx + 140, 260, 2, [30, 30, 30]);
      c.drawHLine(bx, bx + 140, 330, 2, [30, 30, 30]);
      c.drawVLine(bx, 260, 330, 2, [30, 30, 30]);
      c.drawVLine(bx + 140, 260, 330, 2, [30, 30, 30]);
    }
  }
);

addCanvasCase(
  {
    id: "50_distractor_busy_right_margin_calm_left",
    category: "distractor",
    description: "Page with dense content on the right (x=720..1240) and calm whitespace on the left (x=20..350)",
    width: 1280,
    height: 720,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  (c) => {
    for (let y = 150; y < 580; y += 16) {
      c.drawPrintedTextLine(740, 1230, y, [10, 10, 10]);
    }
  }
);

// ============================================================================
// Category 4: Non-1280x720 Resolutions & Corrupt/Edge Payloads (16 cases)
// ============================================================================
addCanvasCase(
  {
    id: "51_res_1920x1080_table_compact",
    category: "edge_resolution_or_corrupt",
    description: "1920x1080 (Full HD) 3-col table with 75px row height",
    width: 1920,
    height: 1080,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [260, 820, 1080, 1340],
      yLines: [260, 335, 410, 485, 560],
    });
  }
);

addCanvasCase(
  {
    id: "52_res_1920x1080_table_scaled_125px_rows",
    category: "edge_resolution_or_corrupt",
    description: "1920x1080 (Full HD) 3-col table with 1.5x scaled rows (rowHeight = 125px > 115px hardcoded limit)",
    width: 1920,
    height: 1080,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [260, 820, 1080, 1340],
      yLines: [240, 365, 490, 615, 740],
    });
  }
);

addCanvasCase(
  {
    id: "53_res_2560x1440_table_scaled_2x",
    category: "edge_resolution_or_corrupt",
    description: "2560x1440 (QHD 2x DPI) 3-col table with 150px row height (> 115px hardcoded limit)",
    width: 2560,
    height: 1440,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [360, 1120, 1480, 1840],
      yLines: [360, 510, 660, 810, 960],
      thickness: 3,
    });
  }
);

addCanvasCase(
  {
    id: "54_res_2560x1440_dotted_lines_2x_dpi",
    category: "edge_resolution_or_corrupt",
    description: "2560x1440 (QHD 2x DPI) worksheet with 4 dotted lines scaled 2x (dotW=10px > 8px limit, gapW=8px)",
    width: 2560,
    height: 1440,
    expectedTables: 0,
    expectedAnswerLines: 4,
  },
  (c) => {
    for (const y of [450, 540, 630, 720]) {
      c.drawDottedLine({
        x0: 320,
        x1: 1600,
        y,
        dotW: 10,
        gapW: 8,
        thickness: 3,
      });
    }
  }
);

addCanvasCase(
  {
    id: "55_res_640x480_table_and_offscreen_calmspot",
    category: "edge_resolution_or_corrupt",
    description: "640x480 (VGA) 3-col table (checks if calmCardSpot x=860 is off-screen on 640px width!)",
    width: 640,
    height: 480,
    expectedTables: 1,
    expectedTableRows: 4,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [50, 260, 380, 500],
      yLines: [180, 220, 260, 300, 340],
    });
  }
);

addCanvasCase(
  {
    id: "56_res_640x480_dotted_lines",
    category: "edge_resolution_or_corrupt",
    description: "640x480 (VGA) worksheet with 3 dotted lines at x=120..540 (span=420px, dotW=3, gapW=4)",
    width: 640,
    height: 480,
    expectedTables: 0,
    expectedAnswerLines: 3,
  },
  (c) => {
    for (const y of [210, 250, 290]) {
      c.drawDottedLine({ x0: 120, x1: 540, y, dotW: 3, gapW: 4 });
    }
  }
);

addCanvasCase(
  {
    id: "57_res_320x240_qvga",
    category: "edge_resolution_or_corrupt",
    description: "320x240 (QVGA) small screenshot with table at y=40..180",
    width: 320,
    height: 240,
    expectedTables: 1,
    expectedTableRows: 3,
    expectedTableCols: 3,
    expectedAnswerLines: 0,
  },
  (c) => {
    c.drawTable({
      xLines: [35, 130, 200, 270],
      yLines: [40, 85, 130, 175],
    });
  }
);

addCanvasCase(
  {
    id: "58_res_1x1_pixel_jpeg",
    category: "edge_resolution_or_corrupt",
    description: "1x1 pixel valid JPEG boundary check",
    width: 1,
    height: 1,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  () => {}
);

addCanvasCase(
  {
    id: "59_res_16x16_tiny_jpeg",
    category: "edge_resolution_or_corrupt",
    description: "16x16 pixel tiny JPEG boundary check",
    width: 16,
    height: 16,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  () => {}
);

// Non-canvas / special payload cases (60..66)
corpusCases.push({
  id: "60_payload_empty_base64",
  category: "edge_resolution_or_corrupt",
  description: "Empty data URL prefix 'data:image/jpeg;base64,'",
  width: 0,
  height: 0,
  expectedTables: 0,
  expectedAnswerLines: 0,
  expectNull: true,
  buildDataUrl: () => ({ dataUrl: "data:image/jpeg;base64," }),
});

corpusCases.push({
  id: "61_payload_corrupt_random_bytes",
  category: "edge_resolution_or_corrupt",
  description: "Corrupt non-JPEG base64 bytes",
  width: 0,
  height: 0,
  expectedTables: 0,
  expectedAnswerLines: 0,
  expectNull: true,
  buildDataUrl: () => {
    const buf = Buffer.from("NOT_A_VALID_JPEG_HEADER_AT_ALL_1234567890", "utf8");
    const fileName = "61_payload_corrupt_random_bytes.bin";
    fs.writeFileSync(path.join(CORPUS_DIR, fileName), buf);
    return {
      dataUrl: `data:image/jpeg;base64,${buf.toString("base64")}`,
      savedFile: `tests/audit/ws-d/corpus/${fileName}`,
    };
  },
});

corpusCases.push({
  id: "62_payload_truncated_jpeg",
  category: "edge_resolution_or_corrupt",
  description: "Truncated 1280x720 JPEG (sliced at 25% of byte stream)",
  width: 1280,
  height: 720,
  expectedTables: 0,
  expectedAnswerLines: 0,
  expectNull: true,
  buildDataUrl: () => {
    const c = new RasterCanvas(1280, 720);
    c.drawTable({
      xLines: [180, 560, 740, 920],
      yLines: [200, 250, 300, 350],
    });
    const full = c.encodeJpeg(85);
    const trunc = full.subarray(0, Math.floor(full.length * 0.25));
    const fileName = "62_payload_truncated_jpeg.jpg";
    fs.writeFileSync(path.join(CORPUS_DIR, fileName), trunc);
    return {
      dataUrl: `data:image/jpeg;base64,${trunc.toString("base64")}`,
      savedFile: `tests/audit/ws-d/corpus/${fileName}`,
    };
  },
});

corpusCases.push({
  id: "63_payload_png_bytes_with_jpeg_prefix",
  category: "edge_resolution_or_corrupt",
  description: "Valid PNG image bytes disguised with 'data:image/jpeg;base64,' prefix",
  width: 64,
  height: 64,
  expectedTables: 0,
  expectedAnswerLines: 0,
  expectNull: true,
  buildDataUrl: () => {
    const png = createValidPngBuffer(64, 64);
    const fileName = "63_payload_png_with_jpeg_prefix.png";
    fs.writeFileSync(path.join(CORPUS_DIR, fileName), png);
    return {
      dataUrl: `data:image/jpeg;base64,${png.toString("base64")}`,
      savedFile: `tests/audit/ws-d/corpus/${fileName}`,
    };
  },
});

corpusCases.push({
  id: "64_payload_png_bytes_with_png_prefix",
  category: "edge_resolution_or_corrupt",
  description: "Valid PNG image with standard 'data:image/png;base64,' prefix (unsupported by jpeg-only decoder!)",
  width: 64,
  height: 64,
  expectedTables: 0,
  expectedAnswerLines: 0,
  expectNull: true,
  buildDataUrl: () => {
    const png = createValidPngBuffer(64, 64);
    const fileName = "64_payload_valid_png.png";
    fs.writeFileSync(path.join(CORPUS_DIR, fileName), png);
    return {
      dataUrl: `data:image/png;base64,${png.toString("base64")}`,
      savedFile: `tests/audit/ws-d/corpus/${fileName}`,
    };
  },
});

addCanvasCase(
  {
    id: "65_res_4000x3000_12mp_sync_block",
    category: "edge_resolution_or_corrupt",
    description: "4000x3000 (12 MP) JPEG synchronous main-thread CPU block benchmark",
    width: 4000,
    height: 3000,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  () => {},
  [255, 255, 255],
  70
);

addCanvasCase(
  {
    id: "66_res_8000x8000_64mp_sync_block",
    category: "edge_resolution_or_corrupt",
    description: "8000x8000 (64 MP, 256MB raw RGBA) huge JPEG synchronous event-loop freeze check",
    width: 8000,
    height: 8000,
    expectedTables: 0,
    expectedAnswerLines: 0,
  },
  () => {},
  [255, 255, 255],
  60
);

// ============================================================================
// Execute Benchmark Across All 66 Corpus Cases
// ============================================================================
console.log(
  `[WS-D] Running analyzeScreenLayout benchmark across ${corpusCases.length} corpus images...`
);

interface BenchmarkRow {
  id: string;
  category: string;
  description: string;
  resolution: string;
  savedFile?: string;
  elapsedMs: number;
  returnedNull: boolean;
  expectedTables: number;
  detectedTables: number;
  expectedTableRows?: number;
  detectedTableRows?: number;
  expectedTableCols?: number;
  detectedTableCols?: number;
  expectedHeaderFirstRow?: boolean;
  detectedHeaderFirstRow?: boolean;
  expectedAnswerLines: number;
  detectedAnswerLines: number;
  calmCardSpot?: { x: number; y: number; w: number; h: number; label: string };
  calmCardOffScreen?: boolean;
  tableOutcome: "TP" | "TN" | "FP" | "FN" | "PARTIAL_FN";
  lineOutcome: "TP" | "TN" | "FP" | "FN" | "PARTIAL_FN";
  notes: string[];
}

const results: BenchmarkRow[] = [];

for (const tc of corpusCases) {
  const { dataUrl, savedFile } = tc.buildDataUrl();
  const t0 = performance.now();
  const layout = analyzeScreenLayout(dataUrl);
  const elapsedMs = Number((performance.now() - t0).toFixed(2));

  const detectedTables = layout ? layout.tables.length : 0;
  const detectedAnswerLines = layout ? layout.answerLines.length : 0;
  const t0Obj = layout?.tables[0];
  const detectedTableRows = t0Obj?.rows.length;
  const detectedTableCols = t0Obj?.cols.length;
  const detectedHeaderFirstRow = t0Obj?.rows[0]?.isHeader;

  const calmCardSpot = layout?.calmCardSpot;
  const calmCardOffScreen = Boolean(
    layout &&
      calmCardSpot &&
      (calmCardSpot.x + calmCardSpot.w > layout.width ||
        calmCardSpot.y + calmCardSpot.h > layout.height)
  );

  let tableOutcome: BenchmarkRow["tableOutcome"] = "TN";
  if (tc.expectedTables > 0) {
    if (detectedTables === 0) tableOutcome = "FN";
    else if (
      detectedTables < tc.expectedTables ||
      (tc.expectedTableRows && detectedTableRows !== tc.expectedTableRows) ||
      (tc.expectedTableCols && detectedTableCols !== tc.expectedTableCols) ||
      (tc.expectedHeaderFirstRow !== undefined &&
        detectedHeaderFirstRow !== tc.expectedHeaderFirstRow)
    ) {
      tableOutcome = "PARTIAL_FN";
    } else {
      tableOutcome = "TP";
    }
  } else {
    tableOutcome = detectedTables > 0 ? "FP" : "TN";
  }

  let lineOutcome: BenchmarkRow["lineOutcome"] = "TN";
  if (tc.expectedAnswerLines > 0) {
    if (detectedAnswerLines === 0) lineOutcome = "FN";
    else if (detectedAnswerLines < tc.expectedAnswerLines)
      lineOutcome = "PARTIAL_FN";
    else lineOutcome = "TP";
  } else {
    lineOutcome = detectedAnswerLines > 0 ? "FP" : "TN";
  }

  const notes: string[] = [];
  if (tableOutcome === "FN") notes.push("Missed expected table completely");
  if (tableOutcome === "PARTIAL_FN") {
    notes.push(
      `Table geometry mismatch: expected ${tc.expectedTables} table(s) [${tc.expectedTableRows ?? "*"}x${tc.expectedTableCols ?? "*"}, header=${tc.expectedHeaderFirstRow ?? "*"}], got ${detectedTables} table(s) [${detectedTableRows ?? 0}x${detectedTableCols ?? 0}, header=${detectedHeaderFirstRow}]`
    );
  }
  if (tableOutcome === "FP") notes.push("False-positive table detected on non-table image!");
  if (lineOutcome === "FN") notes.push("Missed all expected answer lines!");
  if (lineOutcome === "PARTIAL_FN") {
    notes.push(
      `Partial answer lines: expected ${tc.expectedAnswerLines}, got ${detectedAnswerLines}`
    );
  }
  if (lineOutcome === "FP") notes.push("False-positive answer lines detected!");
  if (calmCardOffScreen && calmCardSpot && layout) {
    notes.push(
      `calmCardSpot [${calmCardSpot.x},${calmCardSpot.y},${calmCardSpot.w}x${calmCardSpot.h}] overflows canvas ${layout.width}x${layout.height}!`
    );
  }
  if (elapsedMs > 100) {
    notes.push(`Synchronous CPU block: ${elapsedMs} ms on main event loop`);
  }

  results.push({
    id: tc.id,
    category: tc.category,
    description: tc.description,
    resolution: `${tc.width}x${tc.height}`,
    savedFile,
    elapsedMs,
    returnedNull: layout === null,
    expectedTables: tc.expectedTables,
    detectedTables,
    expectedTableRows: tc.expectedTableRows,
    detectedTableRows,
    expectedTableCols: tc.expectedTableCols,
    detectedTableCols,
    expectedHeaderFirstRow: tc.expectedHeaderFirstRow,
    detectedHeaderFirstRow,
    expectedAnswerLines: tc.expectedAnswerLines,
    detectedAnswerLines,
    calmCardSpot,
    calmCardOffScreen,
    tableOutcome,
    lineOutcome,
    notes,
  });
}

// Compute Confusion Matrix & Precision/Recall Metrics
const tableTP = results.filter((r) => r.tableOutcome === "TP").length;
const tablePartial = results.filter((r) => r.tableOutcome === "PARTIAL_FN").length;
const tableFN = results.filter((r) => r.tableOutcome === "FN").length;
const tableFP = results.filter((r) => r.tableOutcome === "FP").length;
const tableTN = results.filter((r) => r.tableOutcome === "TN").length;

const lineTP = results.filter((r) => r.lineOutcome === "TP").length;
const linePartial = results.filter((r) => r.lineOutcome === "PARTIAL_FN").length;
const lineFN = results.filter((r) => r.lineOutcome === "FN").length;
const lineFP = results.filter((r) => r.lineOutcome === "FP").length;
const lineTN = results.filter((r) => r.lineOutcome === "TN").length;

const std1280Rows = results.filter((r) => r.resolution === "1280x720" && !r.returnedNull);
const stdTimes = std1280Rows.map((r) => r.elapsedMs).sort((a, b) => a - b);
const p50_1280 = stdTimes[Math.floor(stdTimes.length * 0.5)] || 0;
const p95_1280 = stdTimes[Math.floor(stdTimes.length * 0.95)] || 0;
const mean_1280 = Number(
  (stdTimes.reduce((s, v) => s + v, 0) / Math.max(1, stdTimes.length)).toFixed(2)
);

const summaryMetrics = {
  totalCorpusImages: results.length,
  tableDetection: {
    strictTP: tableTP,
    partialGeometryOrCountMismatch: tablePartial,
    anyTableDetectedTP: tableTP + tablePartial,
    FN: tableFN,
    FP: tableFP,
    TN: tableTN,
    strictPrecision: Number(
      (tableTP / Math.max(1, tableTP + tableFP)).toFixed(4)
    ),
    anyDetectionPrecision: Number(
      ((tableTP + tablePartial) / Math.max(1, tableTP + tablePartial + tableFP)).toFixed(4)
    ),
    strictRecall: Number(
      (tableTP / Math.max(1, tableTP + tablePartial + tableFN)).toFixed(4)
    ),
    anyDetectionRecall: Number(
      ((tableTP + tablePartial) / Math.max(1, tableTP + tablePartial + tableFN)).toFixed(4)
    ),
  },
  answerLineDetection: {
    strictTP: lineTP,
    partialLineCountMismatch: linePartial,
    anyLineDetectedTP: lineTP + linePartial,
    FN: lineFN,
    FP: lineFP,
    TN: lineTN,
    strictPrecision: Number(
      (lineTP / Math.max(1, lineTP + lineFP)).toFixed(4)
    ),
    strictRecall: Number(
      (lineTP / Math.max(1, lineTP + linePartial + lineFN)).toFixed(4)
    ),
    anyDetectionRecall: Number(
      ((lineTP + linePartial) / Math.max(1, lineTP + linePartial + lineFN)).toFixed(4)
    ),
  },
  timingMs: {
    standard1280x720: {
      count: stdTimes.length,
      minMs: stdTimes[0] || 0,
      p50Ms: p50_1280,
      meanMs: mean_1280,
      p95Ms: p95_1280,
      maxMs: stdTimes[stdTimes.length - 1] || 0,
    },
    res1920x1080Ms:
      results.find((r) => r.id === "51_res_1920x1080_table_compact")?.elapsedMs ?? 0,
    res2560x1440Ms:
      results.find((r) => r.id === "53_res_2560x1440_table_scaled_2x")?.elapsedMs ?? 0,
    res4000x3000_12MP_Ms:
      results.find((r) => r.id === "65_res_4000x3000_12mp_sync_block")?.elapsedMs ?? 0,
    res8000x8000_64MP_Ms:
      results.find((r) => r.id === "66_res_8000x8000_64mp_sync_block")?.elapsedMs ?? 0,
  },
};

fs.writeFileSync(
  path.join(EVIDENCE_DIR, "layout-benchmark.json"),
  JSON.stringify({ summaryMetrics, results }, null, 2)
);

console.log("[WS-D] Screen Layout Benchmark Summary:", JSON.stringify(summaryMetrics, null, 2));

// ============================================================================
// Part 2: snapProgramToScreenLayout Adversarial Testing (>= 35 Cases)
// ============================================================================
const mockTableLayout: ScreenLayoutAnalysis = {
  width: 1280,
  height: 720,
  tables: [
    {
      x: 180,
      y: 200,
      w: 740,
      h: 265,
      xLines: [180, 560, 740, 920],
      yLines: [200, 245, 300, 355, 410, 465],
      rows: [
        { rowIndex: 0, y0: 200, y1: 245, cy: 223, isHeader: true },
        { rowIndex: 1, y0: 245, y1: 300, cy: 273, isHeader: false },
        { rowIndex: 2, y0: 300, y1: 355, cy: 328, isHeader: false },
        { rowIndex: 3, y0: 355, y1: 410, cy: 383, isHeader: false },
        { rowIndex: 4, y0: 410, y1: 465, cy: 438, isHeader: false },
      ],
      cols: [
        { colIndex: 0, x0: 180, x1: 560, cx: 370, w: 380 },
        { colIndex: 1, x0: 560, x1: 740, cx: 650, w: 180 },
        { colIndex: 2, x0: 740, x1: 920, cx: 830, w: 180 },
      ],
      cells: [],
    },
  ],
  answerLines: [],
  calmCardSpot: { x: 860, y: 150, w: 390, h: 420, label: "right margin" },
  layoutPromptNote: "",
};

const mockAnswerLineLayout: ScreenLayoutAnalysis = {
  width: 1280,
  height: 720,
  tables: [],
  answerLines: [
    { x: 166, y: 240, w: 608 },
    { x: 166, y: 285, w: 608 },
    { x: 166, y: 330, w: 608 },
    { x: 166, y: 375, w: 608 },
  ],
  calmCardSpot: { x: 860, y: 150, w: 390, h: 420, label: "right margin" },
  layoutPromptNote: "",
};

const mockBlankExamLayout: ScreenLayoutAnalysis = {
  width: 1280,
  height: 720,
  tables: [],
  answerLines: [],
  calmCardSpot: { x: 860, y: 150, w: 390, h: 420, label: "right margin" },
  layoutPromptNote: "",
};

const mockComboLayout: ScreenLayoutAnalysis = {
  ...mockTableLayout,
  answerLines: [
    { x: 186, y: 500, w: 660 },
    { x: 186, y: 540, w: 660 },
  ],
};

interface SnapAdversarialCase {
  id: string;
  title: string;
  inputCode: string;
  layout: ScreenLayoutAnalysis | null;
  userText: string;
  check: (output: string) => {
    status: "PASS" | "FAIL";
    issue?: string;
  };
}

const snapCases: SnapAdversarialCase[] = [
  {
    id: "SNAP-01",
    title: "Receipt stripping regex with trailing inline comment wipes out subsequent beats",
    inputCode: [
      "step(1);",
      'say("Let us solve the table.");',
      'receipt(860, 150, "Summary", ["Item 1", "Item 2"]); // floating summary card',
      "step(2);",
      'say("Marking all four rows.");',
      "check(640, 270);",
      "check(825, 330);",
      "check(640, 380);",
      "check(825, 440);",
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Tick the table",
    check: (out) => {
      const hasStep2 = out.includes("step(2)");
      const checkCount = (out.match(/\bcheck\(/g) || []).length;
      return hasStep2 && checkCount === 4
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `Greedy [\\s\\S]*? in receipt stripping regex matched past '// floating summary card' across newlines and deleted step(2), say(), and all ${4 - checkCount} check() calls!`,
          };
    },
  },
  {
    id: "SNAP-02",
    title: "Multi-line receipt() with function call at end of line leaves dangling syntax error",
    inputCode: [
      "step(1);",
      'say("Checking the table.");',
      "receipt(860, 150, String(123)",
      '  , ["Step 1", "Step 2"], { w: 390 });',
      "check(650, 273);",
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Tick the table",
    check: (out) => {
      let syntaxOk = true;
      try {
        new Function(out);
      } catch {
        syntaxOk = false;
      }
      return syntaxOk
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `Multi-line receipt() stripped only up to 'String(123)' at end of line 3, leaving dangling ', ["Step 1", "Step 2"], { w: 390 });' SyntaxError!`,
          };
    },
  },
  {
    id: "SNAP-03",
    title: "Two receipt() calls where first has trailing comment deletes code between them",
    inputCode: [
      "step(1);",
      'say("Beat one.");',
      'receipt(860, 150, "Card 1", ["A"]); // first card',
      "check(650, 273);",
      "check(830, 328);",
      "step(2);",
      'say("Beat two.");',
      'clipping(860, 400, 380, "Card 2", "Body", "Notes");',
      "check(650, 383);",
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Solve this table",
    check: (out) => {
      const checks = (out.match(/\bcheck\(/g) || []).length;
      return checks === 3 && out.includes("step(2)")
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `Deleted intermediate check(650, 273), check(830, 328), step(2), and say("Beat two.") between receipt() and clipping()! Remaining checks: ${checks}`,
          };
    },
  },
  {
    id: "SNAP-04",
    title: "Case A: write() assigned to const variable produces 'const w2 = ;' SyntaxError when converted to receipt()",
    inputCode: [
      "step(1);",
      'say("Here is the derivation.");',
      'const w1 = write(200, 220, "dy/dx = 2x + 3");',
      'const w2 = write(200, 260, "At x = 1, dy/dx = 5");',
    ].join("\n"),
    layout: mockBlankExamLayout,
    userText: "Differentiate y = x^2 + 3x",
    check: (out) => {
      let syntaxOk = true;
      let errMsg = "";
      try {
        new Function(out);
      } catch (e: any) {
        syntaxOk = false;
        errMsg = e.message;
      }
      return syntaxOk
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `Replacing 2nd write() with '' left invalid JS ('const w2 = ;'): ${errMsg}`,
          };
    },
  },
  {
    id: "SNAP-05",
    title: "Case A: write() with escaped quotes inside string truncates text and leaves invalid JS",
    inputCode: [
      "step(1);",
      'say("Solving for x.");',
      'write(200, 220, "The root \\"x\\" equals 42", { size: 17 });',
    ].join("\n"),
    layout: mockBlankExamLayout,
    userText: "Solve the equation",
    check: (out) => {
      const ok = out.includes("42") && out.includes("receipt(");
      return ok
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `writeFullRegex ([\\s\\S]*?)\\3 does not handle escaped quotes \\", failing to match or truncating: ${out}`,
          };
    },
  },
  {
    id: "SNAP-06",
    title: "Case A: write() with closing parenthesis inside options object fails regex match",
    inputCode: [
      "step(1);",
      'say("Writing part a.");',
      'write(200, 220, "x = pi / 6", { label: "a)" });',
    ].join("\n"),
    layout: mockBlankExamLayout,
    userText: "Solve part a",
    check: (out) => {
      return out.includes("receipt(")
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `Options regex \\{[^)]*\\} fails when options contain ')', leaving raw write() over printed text: ${out}`,
          };
    },
  },
  {
    id: "SNAP-07",
    title: "Case A: write() across multiple beats (step 1, step 2, step 3) collapses all into step 1 leaving empty beats",
    inputCode: [
      "step(1);",
      'say("First we differentiate.");',
      'write(200, 220, "dy/dx = 10 cos(2x)");',
      "step(2);",
      'say("Next we set derivative to zero.");',
      'write(200, 260, "10 cos(2x) = 0 => x = pi/4");',
    ].join("\n"),
    layout: mockBlankExamLayout,
    userText: "Show working step by step",
    check: (out) => {
      const step2Idx = out.indexOf("step(2)");
      const afterStep2 = step2Idx >= 0 ? out.slice(step2Idx) : "";
      const hasVisualAfterStep2 = /\b(receipt|write|point|ring|frame)\s*\(/.test(
        afterStep2
      );
      return hasVisualAfterStep2
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `All write() calls collapsed into step(1)'s receipt(), leaving step(2) with say() and ZERO visual drawing calls!`,
          };
    },
  },
  {
    id: "SNAP-08",
    title: "Case A: 20 write() calls push cardY calculation negative (clamped to y=56, overflowing 720px canvas height)",
    inputCode: Array.from(
      { length: 20 },
      (_, i) => `write(200, ${100 + i * 25}, "Step ${i + 1}: algebraic transformation line");`
    ).join("\n"),
    layout: mockBlankExamLayout,
    userText: "Solve on a page",
    check: (out) => {
      const m = out.match(/receipt\(\s*(-?\d+)\s*,\s*(-?\d+)/);
      const y = m ? parseInt(m[2], 10) : 0;
      const estimatedBottom = y + 60 + 20 * 32;
      return estimatedBottom <= 720
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `20 items in receipt() at y=${y} reaches y=${estimatedBottom}px (> 720px screen height), overflowing bottom of screen!`,
          };
    },
  },
  {
    id: "SNAP-09",
    title: "Off-screen extreme coordinates (-500, 9999) in check() unconditionally snapped into table cell",
    inputCode: `step(1);\nsay("Checking.");\ncheck(-500, 9999);`,
    layout: mockTableLayout,
    userText: "Check",
    check: (out) => {
      return out.includes("check(650, 438)")
        ? {
            status: "FAIL",
            issue: `Off-screen garbage coordinate check(-500, 9999) was silently snapped to table cell (650, 438) without bounds validation`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-10",
    title: "Checkmarks intended for non-table multiple-choice boxes outside table get hijacked into table cells",
    inputCode: [
      "step(1);",
      'say("Ticking box A at the top of the page.");',
      "check(120, 110);",
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Tick option A above the table",
    check: (out) => {
      return out.includes("check(650, 273)")
        ? {
            status: "FAIL",
            issue: `check(120, 110) outside the table (at y=110, x=120) was forciblyhijacked to table cell (650, 273)!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-11",
    title: "Hardcoded RISC/CISC column order assumption swaps columns when table has CISC in Col 2 and RISC in Col 3",
    inputCode: [
      "step(1);",
      'say("Marking RISC and CISC.");',
      'point(830, 273, "RISC ✓ Row 1");',
      'point(650, 328, "CISC ✓ Row 2");',
    ].join("\n"),
    layout: mockTableLayout, // Suppose Col 2 (cx=650) is CISC and Col 3 (cx=830) is RISC!
    userText: "Tick the table where Col 2 is CISC and Col 3 is RISC",
    check: (out) => {
      // Model passed x=830 for RISC and x=650 for CISC, but screenLayout.ts lines 721-724 override targetX to answerCols[0] (650) for RISC and answerCols[1] (830) for CISC!
      const swapped =
        out.includes("check(650, 273)") && out.includes("check(830, 328)");
      return swapped
        ? {
            status: "FAIL",
            issue: `screenLayout.ts:721-724 hardcodes 'risc' -> answerCols[0].cx (650) and 'cisc' -> answerCols[1].cx (830), overwriting the LLM's explicit x=830 and x=650 coordinates!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-12",
    title: "point() call pointing at 'Row 1' explanation outside table is converted into a table check()",
    inputCode: [
      "step(1);",
      'say("Look at the note about Row 1.");',
      'point(220, 120, "See Row 1 instructions");',
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Point to the instructions",
    check: (out) => {
      return !out.includes("point(") && out.includes("check(")
        ? {
            status: "FAIL",
            issue: `point(220, 120, "See Row 1 instructions") was falsely converted into check(650, 273) because label matched /\\brow\\s*(\\d+)/i!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-13",
    title: "point() with label 'Check or Cross' defaults to check() even when describing both",
    inputCode: `step(1);\nsay("Header.");\npoint(650, 223, "Check or Cross column");`,
    layout: mockTableLayout,
    userText: "Explain the columns",
    check: (out) => {
      return out.includes("check(650, 273)")
        ? {
            status: "FAIL",
            issue: `point(650, 223, "Check or Cross column") on the header row was converted to check(650, 273) on Data Row 1!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-14",
    title: "Bottom-up check() order (Row 4 -> Row 1) with >20px Y offset is overwritten to top-down (Row 1 -> Row 4) by checksOnePerRow heuristic",
    inputCode: [
      "step(1);",
      'say("Marking from bottom row to top row.");',
      "check(650, 460);", // intended for Row 4 (cy=438, dy=22 > 20)
      "check(830, 405);", // intended for Row 3 (cy=383, dy=22 > 20)
      "check(650, 350);", // intended for Row 2 (cy=328, dy=22 > 20)
      "check(830, 295);", // intended for Row 1 (cy=273, dy=22 > 20)
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Tick bottom to top",
    check: (out) => {
      // Because bestDy = 22 > 20 and checksOnePerRow is true, lines 796-801 force snappedY = dataRows[cIdx].cy (273, 328, 383, 438)!
      const firstIsRow1 = out.indexOf("check(650, 273)") !== -1;
      return firstIsRow1
        ? {
            status: "FAIL",
            issue: `checksOnePerRow heuristic (bestDy=22 > 20) ignored closest row and forced sequential top-down row assignment [273, 328, 383, 438], inverting all 4 answers!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-15",
    title: "ViT rescaling heuristic (avgX < minAnswerColX - 80) warps Y coordinates by 720/565 when 1st col is checked",
    inputCode: [
      "step(1);",
      'say("Checking.");',
      // minAnswerColX = 560. Suppose avgX = 450 (< 480), and 450 * (1280/840) = 685.7 (in 500..980).
      // If Y was already in 1280x720 space (e.g. y=383 for Row 3), rawY * (720/565) = 488 -> snaps to Row 4 (438) instead of Row 3 (383)!
      "check(450, 383);",
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Check row 3",
    check: (out) => {
      return out.includes("check(650, 438)")
        ? {
            status: "FAIL",
            issue: `needsVitRescale triggered on X=450 (1280x720 space) and multiplied Y=383 by 720/565 -> 488, snapping Row 3 to Row 4 (438)!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-16",
    title: "1-column table (cols.length === 1) snaps all checks to cols[0]",
    inputCode: `step(1);\nsay("Check.");\ncheck(300, 270);`,
    layout: {
      ...mockTableLayout,
      tables: [
        {
          ...mockTableLayout.tables[0],
          cols: [{ colIndex: 0, x0: 180, x1: 560, cx: 370, w: 380 }],
        },
      ],
    },
    userText: "Check",
    check: (out) =>
      out.includes("check(370, 273)")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-17",
    title: "Worksheet write() with long string wraps across consecutive dotted answer lines",
    inputCode: [
      "step(1);",
      'say("Writing answer.");',
      'write(200, 240, "RISC processors use a small set of simple fixed-length instructions that execute in a single clock cycle via pipelining.");',
    ].join("\n"),
    layout: mockAnswerLineLayout,
    userText: "Fill in the blank lines",
    check: (out) => {
      const writes = Array.from(out.matchAll(/write\((\d+),\s*(\d+)/g));
      return writes.length === 3 &&
        writes[0][1] === "166" &&
        writes[0][2] === "236" &&
        writes[1][2] === "281" &&
        writes[2][2] === "326"
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `Unexpected write wrap output: ${out}`,
          };
    },
  },
  {
    id: "SNAP-18",
    title: "Worksheet write() overflow when chunks exceed number of detected answerLines extrapolates Y by +28px",
    inputCode: [
      "step(1);",
      'say("Writing long paragraph.");',
      'write(200, 240, "' +
        "word ".repeat(80).trim() +
        '");',
    ].join("\n"),
    layout: mockAnswerLineLayout, // only 4 lines
    userText: "Fill in the lines",
    check: (out) => {
      const writes = Array.from(out.matchAll(/write\((\d+),\s*(\d+)/g));
      return writes.length >= 6 && writes[4][2] === String(375 - 4 + 28)
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected overflow extrapolation: ${out}` };
    },
  },
  {
    id: "SNAP-19",
    title: "Worksheet write() inside unbraced if-statement expands to multi-statement syntax/logic bug when wrapped",
    inputCode: [
      "step(1);",
      'say("Conditional write.");',
      'if (false) write(200, 240, "First long clause that exceeds fifty eight characters easily so it wraps onto a second dotted line immediately");',
    ].join("\n"),
    layout: mockAnswerLineLayout,
    userText: "Fill in",
    check: (out) => {
      // When `if (false) write(...)` expands into `if (false) write(...);\nwrite(...);`, the 2nd write executes unconditionally!
      const lines = out.split("\n");
      const unconditionalSecondWrite = lines.some((l) =>
        l.startsWith("write(")
      );
      return unconditionalSecondWrite
        ? {
            status: "FAIL",
            issue: `Expanding a single write() into multiple ';\\n'-separated statements outside braces turns conditional write into unconditional execution!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-20",
    title: "Combo layout (table + answer lines below): write() calls snap only to linesBelowTable when check() is present",
    inputCode: [
      "step(1);",
      'say("Checking table and writing below.");',
      "check(650, 273);",
      'write(200, 500, "Follow-up explanation on dotted line below table.");',
    ].join("\n"),
    layout: mockComboLayout,
    userText: "Complete the worksheet",
    check: (out) =>
      out.includes("write(186, 496,")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Did not snap to line below table: ${out}` },
  },
  {
    id: "SNAP-21",
    title: "Combo layout with answer lines ONLY above table: check() + write() snaps write() to line above table",
    inputCode: [
      "step(1);",
      'say("Answering part a above and table below.");',
      'write(200, 150, "Part A answer above the table.");',
      "check(650, 273);",
    ].join("\n"),
    layout: {
      ...mockTableLayout,
      answerLines: [{ x: 180, y: 150, w: 600 }],
    },
    userText: "Answer all parts",
    check: (out) =>
      out.includes("write(180, 146,")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-22",
    title: "Combo layout with answer lines BOTH above and below table: check() + write() for line ABOVE table gets forced BELOW table!",
    inputCode: [
      "step(1);",
      'say("Answering question 1a above the table and ticking 1b table.");',
      'write(180, 150, "Question 1a answer written above the table.");',
      "check(650, 273);",
    ].join("\n"),
    layout: {
      ...mockTableLayout,
      answerLines: [
        { x: 180, y: 150, w: 600 }, // above table (y=150 < 200)
        { x: 180, y: 510, w: 600 }, // below table (y=510 > 465)
      ],
    },
    userText: "Complete 1a and 1b",
    check: (out) => {
      // Look at screenLayout.ts:825-832: when hasTableChecks && linesBelowTable.length > 0, candidateLines = linesBelowTable!
      // It completely discards the answer line above the table (y=150) and forces the write(180, 150) down to y=506 (below the table)!
      return out.includes("write(180, 506,")
        ? {
            status: "FAIL",
            issue: `screenLayout.ts:829-832 discards all answerLines above the table whenever linesBelowTable.length > 0 and check() is present, moving write(180, 150) from y=150 to y=506!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-23",
    title: "User prompt 'solve this on a page, not on another page' contradiction check",
    inputCode: `step(1);\nsay("Solving.");\nwrite(200, 240, "Answer 1");`,
    layout: mockAnswerLineLayout,
    userText: "Solve this directly on my screen, not on a page",
    check: (out) =>
      out.includes("write(166, 236,") && !out.includes("receipt(")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-24",
    title: "User explicitly asks 'solve on a page' when table is present: keeps receipt() card and still snaps check()",
    inputCode: [
      "step(1);",
      'say("Solving on a page and ticking.");',
      "check(640, 270);",
      'receipt(860, 150, "Solution", ["Row 1 is RISC"]);',
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Solve this on a page",
    check: (out) =>
      out.includes("receipt(") && out.includes("check(650, 273)")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-25",
    title: "User forbids page card ('directly on my screen') when numTables === 0 && numAnswerLines === 0 preserves write()",
    inputCode: `step(1);\nsay("Writing on screen.");\nwrite(300, 300, "x = 5");`,
    layout: mockBlankExamLayout,
    userText: "Write directly on my screen",
    check: (out) =>
      out.includes('write(300, 300, "x = 5")') && !out.includes("receipt(")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-26",
    title: "Multiline template literal inside write() on blank exam layout converts newlines to spaces in receipt()",
    inputCode: "step(1);\nsay(\"Here.\");\nwrite(200, 200, `Line 1\nLine 2\nLine 3`);",
    layout: mockBlankExamLayout,
    userText: "Solve",
    check: (out) =>
      out.includes('receipt(860, 150, "Step-by-Step Solution", ["Line 1 Line 2 Line 3"]')
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-27",
    title: "Single-word 200-char string without spaces in splitTextForAnswerLines does not infinite-loop or split mid-word",
    inputCode: `step(1);\nsay("Long token.");\nwrite(200, 240, "${"A".repeat(200)}");`,
    layout: mockAnswerLineLayout,
    userText: "Write",
    check: (out) =>
      out.includes("A".repeat(200))
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-28",
    title: "Null layout with no tables/lines still converts loose write() to receipt() at default fallback spot (820, 150)",
    inputCode: `step(1);\nsay("Null layout test.");\nwrite(100, 200, "Step 1");`,
    layout: null,
    userText: "Solve",
    check: (out) =>
      out.includes('receipt(820, 150, "Step-by-Step Solution", ["Step 1"]')
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-29",
    title: "Two-per-row check+cross pattern (totalTwoPerRow) with >20px Y offset snaps pairs to consecutive rows",
    inputCode: [
      "step(1);",
      'say("Marking both columns per row.");',
      "check(650, 100); cross(830, 100);",
      "cross(650, 100); check(830, 100);",
      "check(650, 100); cross(830, 100);",
      "cross(650, 100); check(830, 100);",
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Mark check and cross for every row",
    check: (out) =>
      out.includes("check(650, 273); cross(830, 273);") &&
      out.includes("cross(650, 438); check(830, 438);")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-30",
    title: "Table with ALL rows marked isHeader=true (dataRows.length === 0) leaves check() unsnapped",
    inputCode: `step(1);\nsay("Test.");\ncheck(600, 220);`,
    layout: {
      ...mockTableLayout,
      tables: [
        {
          ...mockTableLayout.tables[0],
          rows: [{ rowIndex: 0, y0: 200, y1: 245, cy: 223, isHeader: true }],
        },
      ],
    },
    userText: "Check",
    check: (out) =>
      out.includes("check(600, 220)")
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-31",
    title: "Code comment mentioning 'check(1, 2)' triggers hasTableMarks and strips valid receipt() card!",
    inputCode: [
      "step(1);",
      'say("Here is the summary card.");',
      "// Note: do not use check(100, 200) here",
      'receipt(860, 150, "Summary", ["Point 1", "Point 2"]);',
    ].join("\n"),
    layout: mockTableLayout,
    userText: "Explain this table",
    check: (out) => {
      // hasTableMarks = /\b(check|cross)\s*\(/.test(processed) matches inside comments!
      // Then Case B strips the receipt() card, leaving the program with ZERO visual marks!
      return !out.includes("receipt(")
        ? {
            status: "FAIL",
            issue: `hasTableMarks regex matched 'check(100, 200)' inside a // comment, triggering Case B which stripped the only receipt() card in the program!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-32",
    title: "say() text mentioning 'paper(' prevents Case A from converting loose write() calls into a receipt() card!",
    inputCode: [
      "step(1);",
      'say("Let us solve this exam paper(1) problem.");',
      'write(200, 250, "Step 1: dy/dx = 3x^2");',
      'write(200, 290, "Step 2: x = 0");',
    ].join("\n"),
    layout: mockBlankExamLayout,
    userText: "Solve this problem",
    check: (out) => {
      // hasCardAlready = /\b(receipt|clipping|paper|sticky)\s*\(/.test(processed) matches 'paper(1)' inside say("...")!
      return !out.includes("receipt(")
        ? {
            status: "FAIL",
            issue: `hasCardAlready regex matched 'paper(1)' inside say("...") string literal, skipping Case A and leaving loose write() calls scribbled over printed exam text!`,
          }
        : { status: "PASS" };
    },
  },
  {
    id: "SNAP-33",
    title: "Floating point coordinates in check(649.75, 272.4) preserve third argument options",
    inputCode: `step(1);\nsay("Check.");\ncheck(649.75, 272.4, { tone: "leaf" });`,
    layout: mockTableLayout,
    userText: "Check",
    check: (out) =>
      out.includes('check(650, 273, { tone: "leaf" })')
        ? { status: "PASS" }
        : { status: "FAIL", issue: `Unexpected output: ${out}` },
  },
  {
    id: "SNAP-34",
    title: "point() with nested parens in 4th options arg e.g. point(650, 273, 'RISC ✓', { fn: a(1) }) corrupts syntax",
    inputCode: `step(1);\nsay("Point.");\npoint(650, 273, "RISC ✓", { fn: a(1) });`,
    layout: mockTableLayout,
    userText: "Tick",
    check: (out) => {
      let syntaxOk = true;
      try {
        new Function(out);
      } catch {
        syntaxOk = false;
      }
      return syntaxOk
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `point() regex (?:,[^)]*)?\\) stopped at inner ')' of a(1), leaving dangling '});' SyntaxError: ${out}`,
          };
    },
  },
  {
    id: "SNAP-35",
    title: "check() with nested parens in 3rd arg e.g. check(650, 273, { color: rgb(0, 255, 0) }) corrupts syntax",
    inputCode: `step(1);\nsay("Check.");\ncheck(650, 273, { color: rgb(0, 255, 0) });`,
    layout: mockTableLayout,
    userText: "Tick",
    check: (out) => {
      let syntaxOk = true;
      try {
        new Function(out);
      } catch {
        syntaxOk = false;
      }
      return syntaxOk
        ? { status: "PASS" }
        : {
            status: "FAIL",
            issue: `check() callRegex (\\s*(?:,[^)]*)?)\\) stopped at inner ')' of rgb(0, 255, 0), leaving dangling ' });' SyntaxError: ${out}`,
          };
    },
  },
];

const snapResults = snapCases.map((sc) => {
  const t0 = performance.now();
  const output = snapProgramToScreenLayout(sc.inputCode, sc.layout, sc.userText);
  const elapsedMs = Number((performance.now() - t0).toFixed(3));
  const res = sc.check(output);
  return {
    id: sc.id,
    title: sc.title,
    status: res.status,
    issue: res.issue || null,
    elapsedMs,
    inputCode: sc.inputCode,
    outputCode: output,
  };
});

const snapSummary = {
  totalCases: snapResults.length,
  passed: snapResults.filter((r) => r.status === "PASS").length,
  failed: snapResults.filter((r) => r.status === "FAIL").length,
  failures: snapResults
    .filter((r) => r.status === "FAIL")
    .map((r) => ({ id: r.id, title: r.title, issue: r.issue })),
};

fs.writeFileSync(
  path.join(EVIDENCE_DIR, "snap-adversarial-results.json"),
  JSON.stringify({ snapSummary, snapResults }, null, 2)
);

console.log(
  "[WS-D] snapProgramToScreenLayout Adversarial Summary:",
  JSON.stringify(snapSummary, null, 2)
);

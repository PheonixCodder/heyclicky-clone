import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';

const ROOT = path.resolve(import.meta.dirname, '../../..');
const ENGINE_DIR = path.join(ROOT, 'src/desktop/Kursor.Drawing/Assets/Engine');
const OUT_DIR = path.join(ROOT, 'docs/audit/ws-fg');

const nativeSrc = fs.readFileSync(path.join(ENGINE_DIR, 'clicky-draw-native.js'), 'utf8');
const engineSrc = fs.readFileSync(path.join(ENGINE_DIR, 'clicky-draw-engine.js'), 'utf8');
const piecesSrc = fs.readFileSync(path.join(ENGINE_DIR, 'clicky-draw-pieces.js'), 'utf8');

function createHarness() {
  let simTime = 1.0;
  let r0 = 0;
  let r1 = 0;
  let r2 = 0;
  let r3 = 0;
  let currentPathData = '';

  const hostCalls = {
    logs: [],
    schedules: [],
    cancels: [],
    captions: [],
    markSounds: [],
    buttonsReady: [],
    shapes3d: [],
    textWidthCalls: 0,
    textBoxCalls: 0,
  };

  // Matches HostBridge.cs (V8FastHostObject<HostBridge>) method getters 1:1
  const mockHost = {
    now() {
      return simTime;
    },
    schedule(id, seconds) {
      hostCalls.schedules.push({ id, seconds, dueAt: simTime + Math.max(0, Number(seconds) || 0) });
    },
    cancel(id) {
      hostCalls.cancels.push(id);
      const idx = hostCalls.schedules.findIndex((s) => s.id === id);
      if (idx >= 0) hostCalls.schedules.splice(idx, 1);
    },
    log(msg) {
      hostCalls.logs.push(String(msg));
    },
    textWidth(text, fontSize, weight, fontFamily) {
      hostCalls.textWidthCalls++;
      const str = String(text ?? '');
      const size = Number(fontSize) || 18;
      return Math.max(4, str.length * size * 0.52);
    },
    textBox(text) {
      hostCalls.textBoxCalls++;
      const str = String(text ?? '');
      const w = Math.max(8, str.length * 10);
      r0 = 0;
      r1 = -14;
      r2 = w;
      r3 = 18;
    },
    setPath(d) {
      currentPathData = String(d ?? '');
    },
    pathLength() {
      return Math.max(20, currentPathData.length * 1.5);
    },
    pathPoint(dist) {
      r0 = Number(dist) || 0;
      r1 = (Number(dist) || 0) * 0.5;
    },
    pathBox(d) {
      const nums = (String(d ?? '').match(/-?\d+(?:\.\d+)?/g) || []).map(Number).filter(Number.isFinite);
      if (nums.length >= 2) {
        const xs = nums.filter((_, i) => i % 2 === 0);
        const ys = nums.filter((_, i) => i % 2 === 1);
        const minX = Math.min(...xs);
        const maxX = Math.max(...xs);
        const minY = Math.min(...ys);
        const maxY = Math.max(...ys);
        r0 = minX;
        r1 = minY;
        r2 = Math.max(4, maxX - minX);
        r3 = Math.max(4, maxY - minY);
      } else {
        r0 = 0;
        r1 = 0;
        r2 = 100;
        r3 = 100;
      }
    },
    inkFor(x, y, w, h) {
      return 'ink';
    },
    busyFor(x, y, w, h) {
      return 0.12;
    },
    lumFor(x, y, w, h) {
      return 0.18;
    },
    figuresFloat() {
      return true;
    },
    caption(json) {
      hostCalls.captions.push(String(json ?? ''));
    },
    markSound(markIndex, soundName) {
      hostCalls.markSounds.push({ markIndex, soundName });
    },
    buttonReady(markIndex, delaySeconds) {
      hostCalls.buttonsReady.push({ markIndex, delaySeconds });
    },
    shape3d(kind, recipeJson) {
      hostCalls.shapes3d.push({ kind, recipe: JSON.parse(recipeJson || '{}') });
    },
    r0() {
      return r0;
    },
    r1() {
      return r1;
    },
    r2() {
      return r2;
    },
    r3() {
      return r3;
    },
  };

  const ctx = vm.createContext({
    __host: mockHost,
    Math,
    Date,
    JSON,
    Number,
    String,
    Boolean,
    Array,
    Object,
    Set,
    Map,
    WeakMap,
    RegExp,
    Error,
    TypeError,
    RangeError,
    SyntaxError,
    ReferenceError,
    parseFloat,
    parseInt,
    isNaN,
    isFinite,
    Uint8Array,
    Int32Array,
    Float32Array,
    Float64Array,
  });

  vm.runInContext(nativeSrc, ctx, { filename: 'clicky-draw-native.js' });
  vm.runInContext(engineSrc, ctx, { filename: 'clicky-draw-engine.js' });
  ctx.clickyCanvas.setPieces(piecesSrc);

  const drainedBatches = [];
  function drainRender() {
    const raw = ctx.__drainRender();
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed) && parsed.length > 0) {
      drainedBatches.push(...parsed);
    }
    return parsed;
  }

  function advanceTime(totalSec, stepSec = 0.2) {
    const steps = Math.max(1, Math.ceil(totalSec / stepSec));
    for (let s = 0; s < steps; s++) {
      simTime += totalSec / steps;
      // Fire due timers (up to 32 per step to avoid infinite interval loops)
      let guard = 0;
      while (guard++ < 32) {
        const idx = hostCalls.schedules.findIndex((item) => item.dueAt <= simTime);
        if (idx < 0) break;
        const [item] = hostCalls.schedules.splice(idx, 1);
        ctx.__fireTimer(item.id);
      }
      drainRender();
    }
  }

  return {
    ctx,
    hostCalls,
    advanceTime,
    drainRender,
    drainedBatches,
  };
}

function executeProgram(name, code, options = {}) {
  const harness = createHarness();
  const { ctx, hostCalls, advanceTime, drainRender, drainedBatches } = harness;
  const w = 1920;
  const h = 1080;

  let thrown = null;
  let feedAnalysis = null;
  let renderReport = null;

  try {
    if (options.mode === 'whole') {
      const raw = ctx.clickyCanvas.renderWhole({ w, h, width: w, code, ink: '#FF4C4C' });
      renderReport = raw ? JSON.parse(raw) : null;
      drainRender();
      advanceTime(1.0);
    } else {
      ctx.clickyCanvas.beginStream({ w, h, width: w, subtitles: true, ink: '#FF4C4C' });
      if (options.chunks) {
        for (let i = 0; i < options.chunks.length; i++) {
          const isLast = i === options.chunks.length - 1;
          const raw = ctx.clickyCanvas.feed({ code: options.chunks.slice(0, i + 1).join(''), done: isLast });
          if (raw) feedAnalysis = JSON.parse(raw);
        }
      } else {
        const raw = ctx.clickyCanvas.feed({ code, done: true });
        if (raw) feedAnalysis = JSON.parse(raw);
      }

      const lines = feedAnalysis?.lines || [];
      for (let k = 0; k < Math.max(1, lines.length); k++) {
        ctx.clickyCanvas.line(k);
        ctx.clickyCanvas.lineLength(k, 0.8);
        advanceTime(1.6);
      }
      ctx.clickyCanvas.finish();
      advanceTime(2.0);
    }
  } catch (err) {
    thrown = err ? String(err.stack || err.message || err) : 'Unknown error';
  }

  drainRender();
  const cardsRaw = ctx.clickyCanvas.cards();
  const cards = cardsRaw ? JSON.parse(cardsRaw) : [];
  const stateStr = ctx.clickyCanvas.state();
  const custom = options.custom ? options.custom(harness) : null;

  return {
    name,
    code,
    thrown,
    analysisError: feedAnalysis?.error ?? renderReport?.error ?? null,
    analysisWarnings: feedAnalysis?.warnings ?? renderReport?.warnings ?? [],
    markCount: feedAnalysis?.marks ?? renderReport?.marks ?? 0,
    inkedCount: feedAnalysis?.inked ?? 0,
    pictureFlag: feedAnalysis?.picture ?? false,
    lines: feedAnalysis?.lines ?? [],
    actions: feedAnalysis?.actions ?? [],
    cardCount: cards.length,
    cards: cards.map((c) => ({ box: c.box, markCount: c.marks?.length ?? 0, title: c.window?.title ?? null, has3D: c.window?.has3D ?? false })),
    drainedOpCount: drainedBatches.length,
    shapes3dBuiltCount: hostCalls.shapes3d.length,
    shapes3dBuilt: hostCalls.shapes3d,
    buttonsReadyCount: hostCalls.buttonsReady.length,
    markSoundsCount: hostCalls.markSounds.length,
    logs: hostCalls.logs,
    hasState: Boolean(stateStr && stateStr.length > 0),
    custom,
  };
}

// Inspect ClickyDraw & clickyCanvas surface and what primitives are defined by clicky-draw-engine.js + clicky-draw-pieces.js
const probe = createHarness();
const clickyCanvasMethods = Object.keys(probe.ctx.clickyCanvas);
const clickyDrawMethods = Object.keys(probe.ctx.ClickyDraw);

const primitiveNamesToCheck = [
  // Engine core API
  'say', 'step', 'erase', 'startOver', 'move', 'moveTo', 'scale', 'recolor', 'restyle',
  'text', 'line', 'arrow', 'curve', 'rect', 'circle', 'ellipse', 'poly', 'highlight', 'image',
  'figure', 'scene3d', 'graph', 'timeline', 'dot', 'path', 'label', 'molecule', 'seen', 'noted',
  // Pieces API + README/docs/brain.ts promised primitives
  'point', 'spot', 'ring', 'frame', 'check', 'cross', 'write', 'underline', 'strike', 'num',
  'connect', 'trace', 'sticky', 'clipping', 'receipt', 'legend', 'paper', 'openPage',
  'button', 'target', 'hover', 'typeText', 'copyText', 'handOff', 'sfx',
  'photo', 'tie', 'source', 'sources', 'scroll', 'cheer', 'spotlight', 'jobTicket', 'dreamMedal',
  'task', 'waitForUser', 'onScreen', 'tag', 'lessonTitle', 'plan', 'ink', 'inkCircle',
  'tourJob', 'tuckJobs', 'showNotch', 'fadeAway', 'nextQuestions',
];

const probeCode = `
  const __presence = {};
  for (const k of ${JSON.stringify(primitiveNamesToCheck)}) {
    try {
      __presence[k] = eval('typeof ' + k);
    } catch (_) {
      __presence[k] = 'undefined';
    }
  }
  say(JSON.stringify(__presence));
`;
const probeRun = probe.ctx.ClickyDraw.runCode(piecesSrc + '\n' + probeCode, 1920, 1080, {});
const primitivePresence = JSON.parse(probeRun.says[0].text);

// Define 56 ClickyDraw programs covering every primitive and edge case
const programs = [
  {
    name: '01_step_and_say_basic',
    code: `step("Inspect Header"); say("Look right here at the main header."); point(420, 200, "Header");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.lines.length === 1 && r.markCount >= 2,
  },
  {
    name: '02_point_with_direction_and_color',
    code: `say("Click the submit button."); point(500, 300, "Submit", { from: "tl", color: "coral" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'point' && a.x === 500 && a.y === 300),
  },
  {
    name: '03_spot_undefined_without_polyfill_AUD_F_002',
    code: `say("Spot check"); spot(500, 300, "Kursor Active");`,
    expectPass: true, // Remediated AUD-F-002: spot() is now natively defined in clicky-draw-pieces.js
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'point' && a.x === 500 && a.y === 300),
  },
  {
    name: '04_spot_with_brain_ts_polyfill',
    code: `const spot = typeof point === "function" ? point : () => {}; say("With polyfill"); spot(500, 300, "Kursor Active");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'point'),
  },
  {
    name: '05_ring_callout',
    code: `say("Circle the avatar."); ring(640, 360, 48, 48, "Avatar");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.inkedCount >= 1,
  },
  {
    name: '06_frame_callout',
    code: `say("Frame the login box."); frame(200, 150, 300, 120, "Login Box");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.inkedCount >= 1,
  },
  {
    name: '07_check_mark',
    code: `say("This field is valid."); check(400, 400);`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '08_cross_mark',
    code: `say("Remove this deprecated setting."); cross(450, 400);`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '09_write_handwritten_note',
    code: `say("Jot a quick margin note."); write(300, 220, "Remember to save!", { size: 24, tone: "ink" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '10_underline_emphasis',
    code: `say("Underline the key term."); underline(250, 300, 180, { tone: "red" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '11_strike_deletion',
    code: `say("Strike out the old price."); strike(250, 340, 160);`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '12_num_numbered_badges',
    code: `say("Follow steps one and two."); num(180, 180, 1); num(180, 260, 2);`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 2,
  },
  {
    name: '13_connect_two_points',
    code: `say("Connect client to server."); connect([200, 200], [500, 350], { label: "HTTPS", bend: 0.25 });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '14_trace_multi_waypoint_path',
    code: `say("Trace the packet route."); trace([[150, 150], [300, 220], [480, 180], [650, 300]], { straight: true, fill: true, label: "Cluster" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '15_highlight_region',
    code: `say("Highlight the warning banner."); highlight(300, 260, 220, 36, { color: "yellow" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '16_sticky_note_card',
    code: `say("Pin a sticky reminder."); sticky(1300, 140, "Check token expiry\\nVerify signature", "butter");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.pictureFlag === true,
  },
  {
    name: '17_clipping_quote_card',
    code: `say("Clip the RFC definition."); clipping(1250, 180, 320, "RFC 9110", "The GET method requests transfer of a current selected representation.", "ietf.org");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.pictureFlag === true,
  },
  {
    name: '18_receipt_breakdown_card',
    code: `say("Here is the latency breakdown."); receipt(1250, 160, "Latency Budget", ["DNS Lookup: 12ms", "TLS Handshake: 28ms", "TTFB: 45ms"]);`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.pictureFlag === true,
  },
  {
    name: '19_legend_key_card',
    code: `say("Color legend for the diagram."); legend(1300, 200, [["red", "Critical Path"], ["green", "Healthy"], ["amber", "Degraded"]]);`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1,
  },
  {
    name: '20_paper_study_card_window',
    code: `
      say("Let us work out the quadratic formula on paper.");
      const area = paper(260, 140, 520, 380, { title: "Algebra", heading: "Quadratic Roots", subtitle: "ax^2 + bx + c = 0" });
      write(area.x + 20, area.y + 40, "Discriminant: b^2 - 4ac");
      write(area.x + 20, area.y + 90, "If > 0: two distinct real roots");
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.pictureFlag === true,
  },
  {
    name: '21_openPage_url_action_AUD_F_001',
    code: `say("Opening documentation in browser."); openPage("https://example.com/docs");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'open' && a.text === 'https://example.com/docs') && r.cardCount === 0,
  },
  {
    name: '22_paper_and_write_study_card_remediated_AUD_F_001',
    code: `say("Spawning a study card with paper() and write()."); const p = paper(280, 160, 420, 260, { title: "Study Card", heading: "Key Concept" }); write(p.x + 16, p.y + 32, "Markdown-style study notes on paper()");`,
    expectPass: true, // Remediated AUD-F-001: README.md now accurately documents paper()/receipt()/clipping()/sticky() + write() for study cards and openPage(url) for browser URLs
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.markCount >= 2,
  },
  {
    name: '23_circle_engine_primitive',
    code: `say("Draw a core circle."); circle(500, 500, 60, { id: "c1", tone: "red", fill: "red", fillAlpha: 0.2, label: "Core" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '24_ellipse_engine_primitive',
    code: `say("Draw an orbit ellipse."); ellipse(600, 400, 90, 45, { id: "e1", tone: "blue", dash: "dashed" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '25_rect_engine_primitive',
    code: `say("Draw a service rectangle."); rect(300, 200, 180, 90, { id: "r1", tone: "green", fill: "green", label: "Service A" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '26_poly_engine_primitive',
    code: `say("Draw a closed polygon."); poly([[300, 300], [400, 250], [450, 350], [320, 380]], { id: "p1", tone: "purple", fill: "purple" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '27_line_engine_primitive',
    code: `say("Draw a dividing line."); line([[100, 100], [400, 200]], { id: "l1", tone: "ink", size: 3 });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '28_curve_engine_primitive',
    code: `say("Draw a smooth trajectory curve."); curve([[100, 400], [200, 320], [320, 440], [450, 350]], { id: "cv1", tone: "amber" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '29_arrow_engine_primitive',
    code: `say("Draw a request arrow."); arrow([[250, 250], [550, 350]], { id: "a1", curve: 25, tone: "red", label: "Request" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 1 && r.drainedOpCount >= 1,
  },
  {
    name: '30_text_engine_primitive_with_link',
    code: `say("Write a heading with link."); text(500, 120, "Architecture Overview https://example.com/arch", { id: "t1", size: 28, tone: "ink", anchor: "middle", box: true });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'link' && a.text.includes('https://example.com/arch')),
  },
  {
    name: '31_graph_axes_and_plot',
    code: `
      say("Plot sine and cosine waves.");
      const area = paper(180, 140, 560, 400, { title: "Trig Waves" });
      const g = graph(area.x + 40, area.y + 30, 440, 280, { x: [0, 6.28], y: [-1.2, 1.2], xLabel: "t", yLabel: "amp", grid: true, ticks: true });
      const s1 = g.plot(x => Math.sin(x), { tone: "blue", label: "sin(t)" });
      const s2 = g.plot(x => Math.cos(x), { tone: "red", dash: "dashed", label: "cos(t)" });
      const xing = g.cross(s1, s2);
      g.point(xing, { label: "Intersect", guides: true, tone: "amber" });
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.markCount >= 6,
  },
  {
    name: '32_timeline_events',
    code: `
      say("HTTP protocol timeline.");
      const area = paper(180, 180, 640, 280, { title: "HTTP Evolution" });
      timeline(area.x + 20, area.y + 110, 540, [["1991", "HTTP/0.9"], ["1997", "HTTP/1.1"], ["2015", "HTTP/2"], ["2022", "HTTP/3"]]);
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.markCount >= 5,
  },
  {
    name: '33_figure_3d_declarative_parts_and_labels',
    code: `
      say("Inspect the turbofan core in 3D.");
      const f = figure(300, 180, 480, 340, { title: "Turbofan", style: "davinci", spin: 8 });
      const core = f.cyl({ at: [0, 0, 0], r: 0.8, h: 2.4, axis: "x", mat: "steel", name: "Core" });
      f.cone({ at: [1.5, 0, 0], r: 0.7, h: 0.9, axis: "x", mat: "copper", name: "Exhaust" });
      f.label(core, "Compressor stage", { side: "left" });
      f.readout({ RPM: "12,400", Thrust: "98 kN" });
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.shapes3dBuiltCount >= 2 && r.markCount >= 4,
  },
  {
    name: '34_figure_materials_and_around_audit_AUD_F_003_004',
    code: `
      say("Audit cast/carbon materials and around.at vs around.center.");
      const f = figure(300, 180, 480, 340, { title: "Audit 3D", style: "shaded" });
      f.box({ at: [-1, 0, 0], size: [0.8, 0.8, 0.8], mat: "cast", name: "CastIron" });
      f.sphere({ at: [1, 0, 0], r: 0.5, mat: "carbon", name: "CarbonFiber" });
      f.torus({ at: [2, 1, 0], r: 0.3, tube: 0.08, mat: "gold", around: { count: 4, axis: "y", at: [2, 0, 0] } });
    `,
    options: {
      custom: ({ ctx }) => {
        const marks = ctx.ClickyDraw.runCode(
          piecesSrc +
            `\nconst f = figure(300, 180, 480, 340, { title: "Audit 3D", style: "shaded" });
             f.box({ at: [-1, 0, 0], size: [0.8, 0.8, 0.8], mat: "cast", name: "CastIron" });
             f.sphere({ at: [1, 0, 0], r: 0.5, mat: "carbon", name: "CarbonFiber" });
             f.torus({ at: [2, 1, 0], r: 0.3, tube: 0.08, mat: "gold", around: { count: 4, axis: "y", at: [2, 0, 0] } });`,
          1920,
          1080,
          {}
        );
        const parts = marks.filter((m) => m.do === 'part3d');
        return {
          castMaterial: parts[0]?.material,
          carbonMaterial: parts[1]?.material,
          aroundWithAtPositions: parts.slice(2).map((p) => p.pos),
        };
      },
    },
    expectPass: true,
    verify: (r) =>
      r.analysisError === null &&
      r.custom.castMaterial.mat === 'cast' &&
      r.custom.carbonMaterial.mat === 'carbon' &&
      // Remediated AUD-F-003: passing `at: [2, 0, 0]` inside `around` now revolves around [2,0,0]!
      r.custom.aroundWithAtPositions[2][0] === 2,
  },
  {
    name: '35_scene3d_and_figure_group_move_camera',
    code: `
      say("Group 3D parts, animate move(), and adjust camera view().");
      const s = scene3d(820, 180, 360, 260, { title: "Illustrated Scene3D" });
      s.sphere({ at: [0, 0, 0], r: 0.6, color: "blue" });
      const f = figure(300, 180, 460, 320, { title: "Mechanism", style: "shaded" });
      let piston = null;
      const grp = f.group((g) => {
        piston = g.cyl({ at: [0, 0, 0], r: 0.4, h: 1.2, mat: "alu", name: "Piston" });
        g.sphere({ at: [0, 0.8, 0], r: 0.3, mat: "brass", name: "Head" });
      }, { at: [0, -0.5, 0], name: "Assembly" });
      f.move(piston, { to: [0, 0.5, 0], rot: [0, 45, 0], secs: 0.6 });
      f.view({ turn: 35, tilt: 20, zoom: 1.25, spin: 4 });
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.shapes3dBuiltCount >= 2 && r.markCount >= 6,
  },
  {
    name: '36_button_interactive_prompt',
    code: `say("Offer a next-step button."); button(900, 800, "Next Step", { ask: "Continue to step 2" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'button') && r.buttonsReadyCount >= 1,
  },
  {
    name: '37_target_click_action',
    code: `say("Click the sign-in button."); target(600, 420, "Sign In");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'target' && a.x === 600 && a.y === 420),
  },
  {
    name: '38_hover_action',
    code: `say("Hover over settings."); hover(320, 80, "Settings Menu");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'hover' && a.x === 320 && a.y === 80),
  },
  {
    name: '39_typeText_action',
    code: `say("Type hello world into search."); typeText(500, 300, "hello world");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'type' && a.text === 'hello world'),
  },
  {
    name: '40_copyText_action',
    code: `say("Copy the build command."); copyText("npm run build");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'copy' && a.text === 'npm run build'),
  },
  {
    name: '41_handOff_wait_for_user',
    code: `say("Hand off to background agent."); handOff({ task: "Audit repository", label: "Run Audit", clicky: "Verifier" });`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'handoff'),
  },
  {
    name: '42_sfx_sound_effects',
    code: `say("Play sound effects."); sfx("ding"); sfx("tada"); sfx("whoosh");`,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 3,
  },
  {
    name: '43_erase_single_mark',
    code: `
      say("Draw two circles then erase one.");
      const c1 = circle(300, 300, 40, { id: "keep_me" });
      const c2 = circle(500, 300, 40, { id: "drop_me" });
      say("Erase the second circle.");
      erase(c2);
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.hasState,
  },
  {
    name: '44_move_and_moveTo_mutations',
    code: `
      say("Draw and move shapes.");
      const b1 = rect(100, 100, 80, 50, { id: "box1" });
      const c2 = circle(200, 200, 30, { id: "c2" });
      say("Now shift them.");
      move(b1, 50, 30);
      moveTo(c2, 400, 450);
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 4,
  },
  {
    name: '45_scale_recolor_restyle_mutations',
    code: `
      say("Draw a circle and restyle it.");
      const c = circle(400, 400, 50, { id: "sc1", tone: "red" });
      say("Scale, recolor, and restyle.");
      scale(c, 1.5);
      recolor(c, "green");
      restyle(c, { dash: "dashed", size: 5 });
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 4,
  },
  {
    name: '46_startOver_clears_scene',
    code: `
      say("First board.");
      sticky(200, 200, "Old note");
      circle(300, 300, 40, { id: "old_circle" });
      say("Start over with a clean board.");
      startOver();
      circle(600, 400, 50, { id: "new_circle" });
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 3,
  },
  {
    name: '47_photo_and_tie_connector',
    code: `
      say("Place a photo and tie it to a row.");
      const p = photo(250, 250, 200, 150, "golden gate bridge", "Golden Gate", { subtitle: "San Francisco" });
      tie([650, 320], p);
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.actions.some((a) => a.role === 'photo' && a.text === 'golden gate bridge'),
  },
  {
    name: '48_source_and_sources_citations',
    code: `
      say("Cite web sources.");
      source(300, 800, "developer.mozilla.org", "MDN Web Docs");
      sources(300, 860, [["httpwg.org", "RFC 9110"], ["w3.org", "W3C"]]);
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 4,
  },
  {
    name: '49_scroll_cheer_spotlight_pieces',
    code: `
      say("Spotlight the form, scroll down, and celebrate.");
      spotlight(400, 250, 500, 300);
      scroll(600, 500, "down", "Scroll for more");
      cheer(960, 540, "(^_^)v");
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 3,
  },
  {
    name: '50_jobTicket_and_dreamMedal_cards',
    code: `
      say("Show background job ticket and dream medal.");
      jobTicket(1200, 120, { title: "Deploy Worker", time: "Every 15m", steps: ["run migrations", "upload bundle", "verify health"], icon: "code", color: "mint" });
      dreamMedal(1200, 480, "100%", { caption: "Full audit coverage" });
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.markCount >= 8,
  },
  {
    name: '51_streaming_partial_chunks_scanner',
    code: '',
    options: {
      chunks: [
        'step("Streamed',
        ' Beat");\nsay("Streaming in chunks");\npoint(400, ',
        '300, "Streamed Target");\nsticky(1200, 200, "Streamed Sticky");',
      ],
    },
    expectPass: true,
    verify: (r) => r.analysisError === null && r.cardCount >= 1 && r.actions.some((a) => a.role === 'point'),
  },
  {
    name: '52_figure_tick3d_pose_packing_and_exception_watchdog',
    code: `
      say("Animate 3D rotor with tick().");
      const f = figure(300, 200, 380, 280, { id: "st1", title: "Rotor" });
      const rotor = f.box({ at: [0, 0, 0], size: [1, 1, 1], mat: "steel", name: "Rotor" });
      f.tick((t, dt) => {
        rotor.pos = [Math.sin(t), 0, 0];
        rotor.rot = [0, t * 90, 0];
        rotor.scale = 1.2;
        rotor.glow = 0.5;
        rotor.color = "#ff5500";
        if (t > 10) throw new Error("Simulated tick failure");
      });
    `,
    options: {
      custom: ({ ctx, hostCalls }) => {
        const poseOk = ctx.clickyCanvas.tick3d({ stage: 'st1', ticks: [['st1t1', 0.5]], dt: 0.016 });
        const poseCrash = ctx.clickyCanvas.tick3d({ stage: 'st1', ticks: [['st1t1', 20.0]], dt: 0.016 });
        return {
          poseLength: poseOk ? poseOk.length : 0,
          poseArray: poseOk ? Array.from(poseOk) : [],
          stoppedLogFound: hostCalls.logs.some((l) => l.includes('tick st1t1 stopped: Simulated tick failure')),
          poseCrashLength: poseCrash ? poseCrash.length : 0,
        };
      },
    },
    expectPass: true,
    verify: (r) =>
      r.analysisError === null &&
      r.custom.poseLength === 14 &&
      r.custom.stoppedLogFound === true,
  },
  {
    name: '53_card_lifecycle_move_zoom_close_keepNotesOnly_dropPointing',
    code: `
      say("Create a sticky and a screen pointer.");
      sticky(200, 200, "Persistent sticky note");
      point(600, 500, "Transient pointer");
    `,
    options: {
      custom: ({ ctx }) => {
        const cardsBefore = JSON.parse(ctx.clickyCanvas.cards() || '[]');
        const marks = cardsBefore[0]?.marks || [];
        const movedCount = ctx.clickyCanvas.moveCard({ marks, dx: 80, dy: 40 });
        const zoomedCount = ctx.clickyCanvas.zoomCard({ marks, to: [1.5, 100, 100] });
        const unzoomedCount = ctx.clickyCanvas.zoomCard({ marks, to: null });
        const dropRes = JSON.parse(ctx.clickyCanvas.dropPointing() || '{}');
        const keepRes = JSON.parse(ctx.clickyCanvas.keepNotesOnly() || '{}');
        const closeRes = JSON.parse(ctx.clickyCanvas.closeCard({ marks }) || '{}');
        return { movedCount, zoomedCount, unzoomedCount, dropRes, keepRes, closeRes };
      },
    },
    expectPass: true,
    verify: (r) =>
      r.analysisError === null &&
      r.custom.movedCount > 0 &&
      r.custom.zoomedCount > 0 &&
      r.custom.keepRes.kept >= 1 &&
      r.custom.closeRes.closed > 0,
  },
  {
    name: '54_syntax_error_graceful_reporting',
    code: `say("Valid line before syntax error");\nfunction broken( {`,
    expectPass: true, // Verifies graceful error reporting in analysis JSON without crashing host
    verify: (r) => r.thrown === null && r.analysisError !== null,
  },
  {
    name: '55_continueStream_multi_turn_session',
    code: `
      say("Turn 1 note and pointer.");
      sticky(220, 200, "Turn 1 Sticky");
      point(400, 300, "Turn 1 Pointer");
    `,
    options: {
      custom: ({ ctx, advanceTime }) => {
        const cont = JSON.parse(ctx.clickyCanvas.continueStream({ subtitles: true }) || '{}');
        const feed2 = JSON.parse(
          ctx.clickyCanvas.feed({
            code: 'say("Turn 2 pointer.");\npoint(800, 600, "Turn 2 Pointer");\n',
            done: true,
          }) || '{}'
        );
        ctx.clickyCanvas.line(cont.firstLine);
        ctx.clickyCanvas.lineLength(cont.firstLine, 0.5);
        advanceTime(1.0);
        return {
          firstLineTurn2: cont.firstLine,
          turn2Actions: feed2.actions,
        };
      },
    },
    expectPass: true,
    verify: (r) =>
      r.analysisError === null &&
      r.custom.firstLineTurn2 === 1 &&
      r.custom.turn2Actions.some((a) => a.label === 'Turn 2 Pointer'),
  },
  {
    name: '56_renderWhole_lab_harness_mode',
    code: `
      say("Whole render mode.");
      const area = paper(200, 150, 480, 320, { title: "Whole Card" });
      circle(area.x + 120, area.y + 120, 45, { tone: "blue", fill: "blue" });
      write(area.x + 190, area.y + 120, "Instant render");
    `,
    options: { mode: 'whole' },
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 3 && r.drainedOpCount >= 1,
  },
  {
    name: '57_molecule_smiles_and_tour_meta_primitives',
    code: `
      lessonTitle("Benzene Ring");
      plan("Draw benzene SMILES on paper");
      task("Understand aromatic ring");
      onScreen(0);
      seen("User viewing chemistry notes");
      noted("Student studies organic chemistry");
      say("Here is a benzene ring drawn via SMILES.");
      const area = paper(200, 150, 420, 320, { title: "Chemistry" });
      molecule("c1ccccc1", area.x + 60, area.y + 40, 220, 180, { tone: "ink" });
      tag(area.x + 120, area.y + 240, "C6H6");
    `,
    expectPass: true,
    verify: (r) => r.analysisError === null && r.markCount >= 3 && r.drainedOpCount >= 1,
  },
];

const results = [];
let passCount = 0;
let expectedFailCount = 0;
let unexpectedCount = 0;

for (const prog of programs) {
  const res = executeProgram(prog.name, prog.code, prog.options);
  const verified = Boolean(prog.verify(res));
  const status = verified ? (prog.expectPass ? 'PASS' : 'EXPECTED_FAIL_CONFIRMED') : 'UNEXPECTED';
  if (status === 'PASS') passCount++;
  else if (status === 'EXPECTED_FAIL_CONFIRMED') expectedFailCount++;
  else unexpectedCount++;

  results.push({
    ...res,
    expectPass: prog.expectPass,
    verified,
    status,
  });
}

const summary = {
  timestamp: new Date().toISOString(),
  totalPrograms: programs.length,
  passCount,
  failCount: expectedFailCount + unexpectedCount,
  expectedFailCount,
  unexpectedCount,
  clickyCanvasMethods,
  clickyDrawMethods,
  primitivePresence,
  missingPrimitives: Object.entries(primitivePresence)
    .filter(([, type]) => type === 'undefined')
    .map(([name]) => name),
  results,
};

fs.mkdirSync(OUT_DIR, { recursive: true });
const outPath = path.join(OUT_DIR, '2d-engine-results.json');
fs.writeFileSync(outPath, JSON.stringify(summary, null, 2), 'utf8');

console.log(`Executed ${programs.length} 2D engine programs:`);
console.log(`  PASS: ${passCount}`);
console.log(`  FAIL: ${summary.failCount}`);
console.log(`  Missing primitives from README/docs/brain.ts:`, summary.missingPrimitives);
console.log(`Evidence written to ${outPath}`);

if (unexpectedCount > 0) {
  for (const r of results.filter((x) => x.status === 'UNEXPECTED')) {
    console.error('UNEXPECTED:', r.name, {
      thrown: r.thrown,
      analysisError: r.analysisError,
      markCount: r.markCount,
      cardCount: r.cardCount,
      drainedOpCount: r.drainedOpCount,
      actions: r.actions,
      custom: r.custom,
    });
  }
  process.exitCode = 1;
}

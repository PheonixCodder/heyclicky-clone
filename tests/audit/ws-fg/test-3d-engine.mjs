import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';

const ROOT = path.resolve(import.meta.dirname, '../../..');
const ENGINE_DIR = path.join(ROOT, 'src/desktop/Kursor.Drawing/Assets/Engine');
const OUT_DIR = path.join(ROOT, 'docs/audit/ws-fg');

const threeSrc = fs.readFileSync(path.join(ENGINE_DIR, 'clicky-three.js'), 'utf8');
const partsSrc = fs.readFileSync(path.join(ENGINE_DIR, 'clicky-three-parts.js'), 'utf8');
const engineSrc = fs.readFileSync(path.join(ENGINE_DIR, 'clicky-draw-engine.js'), 'utf8');

function createThreeContext() {
  const ctx = vm.createContext({
    console,
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
    ArrayBuffer,
    DataView,
    Int8Array,
    Uint8Array,
    Uint8ClampedArray,
    Int16Array,
    Uint16Array,
    Int32Array,
    Uint32Array,
    Float32Array,
    Float64Array,
  });

  vm.runInContext(threeSrc, ctx, { filename: 'clicky-three.js' });
  vm.runInContext(partsSrc, ctx, { filename: 'clicky-three-parts.js' });
  vm.runInContext(engineSrc, ctx, { filename: 'clicky-draw-engine.js' });
  return ctx;
}

const ctx = createThreeContext();
const clickyThreeKinds = Array.from(ctx.clickyThreeKinds);
const clickyThreeBuild = ctx.clickyThreeBuild;

function runBuild(name, kind, recipe) {
  const t0 = performance.now();
  let thrown = null;
  let result = null;
  try {
    result = clickyThreeBuild(kind, JSON.stringify(recipe));
  } catch (err) {
    thrown = err ? String(err.message || err) : 'Unknown error';
  }
  const elapsedMs = +(performance.now() - t0).toFixed(3);

  if (!result) {
    return {
      name,
      kind,
      recipe,
      thrown,
      elapsedMs,
      valid: false,
      vertexCount: 0,
      indexCount: 0,
      sampleCount: 0,
      hasNaN: false,
      indexOutOfBounds: false,
    };
  }

  const posLen = result.positions?.length ?? 0;
  const normLen = result.normals?.length ?? 0;
  const idxLen = result.indices?.length ?? 0;
  const sampleLen = result.samples?.length ?? 0;
  const vertexCount = posLen / 3;
  const indexCount = idxLen;
  const sampleCount = sampleLen / 3;

  let hasNaN = false;
  for (let k = 0; k < posLen; k++) {
    if (!Number.isFinite(result.positions[k])) {
      hasNaN = true;
      break;
    }
  }
  if (!hasNaN) {
    for (let k = 0; k < normLen; k++) {
      if (!Number.isFinite(result.normals[k])) {
        hasNaN = true;
        break;
      }
    }
  }

  let indexOutOfBounds = false;
  for (let k = 0; k < idxLen; k++) {
    if (result.indices[k] < 0 || result.indices[k] >= vertexCount) {
      indexOutOfBounds = true;
      break;
    }
  }

  return {
    name,
    kind,
    recipe,
    thrown,
    elapsedMs,
    valid: vertexCount >= 3 && indexCount >= 3 && posLen === normLen && !hasNaN && !indexOutOfBounds,
    vertexCount,
    indexCount,
    sampleCount,
    hasUvs: Boolean(result.uvs && result.uvs.length === vertexCount * 2),
    hasNaN,
    indexOutOfBounds,
  };
}

// 1. All 16 procedural shape types (plus `cylinder` alias = 17 keys in `clickyThreeKinds`) with normal parameters
const normalShapeTests = [
  { name: 'shape_01_box_normal', kind: 'box', recipe: { size: [1.4, 0.9, 1.1], bevel: 0.08 } },
  { name: 'shape_02_cyl_normal', kind: 'cyl', recipe: { r: 0.7, top: 0.5, h: 1.6, seg: 32, bevel: 0.04 } },
  { name: 'shape_02b_cylinder_alias', kind: 'cylinder', recipe: { r: 0.6, h: 1.2 } },
  { name: 'shape_03_cone_normal', kind: 'cone', recipe: { r: 0.8, h: 1.5, seg: 32, bevel: 0.04 } },
  { name: 'shape_04_tube_normal', kind: 'tube', recipe: { r: 0.9, inner: 0.6, h: 1.4, arc: 270 } },
  { name: 'shape_05_arrow_normal', kind: 'arrow', recipe: { points: [[0, 0, 0], [0.8, 0.5, 0], [1.5, 1.0, 0.5]], r: 0.08, head: 0.25 } },
  {
    name: 'shape_06_lathe_normal',
    kind: 'lathe',
    recipe: {
      points: [
        [0, -1.0],
        [0.6, -0.8],
        [0.8, 0.0],
        [0.4, 0.7],
        [0.5, 1.0],
        [0, 1.0],
      ],
      seg: 36,
    },
  },
  { name: 'shape_07_sphere_normal', kind: 'sphere', recipe: { r: 0.85, seg: 32 } },
  { name: 'shape_08_torus_normal', kind: 'torus', recipe: { r: 1.0, tube: 0.25, arc: 300, seg: 36 } },
  { name: 'shape_09_capsule_normal', kind: 'capsule', recipe: { r: 0.45, h: 1.6, seg: 28 } },
  {
    name: 'shape_10_pipe_normal',
    kind: 'pipe',
    recipe: {
      points: [
        [-1, 0, 0],
        [-0.3, 0.8, 0.2],
        [0.5, 0.6, -0.3],
        [1.2, 0, 0],
      ],
      r: 0.14,
      seg: 48,
    },
  },
  { name: 'shape_11_spring_normal', kind: 'spring', recipe: { r: 0.7, wire: 0.08, h: 1.8, coils: 6 } },
  { name: 'shape_12_gear_normal', kind: 'gear', recipe: { r: 1.0, teeth: 18, w: 0.35, hole: 0.3, tooth: 0.16 } },
  {
    name: 'shape_13_extrude_svg_with_hole',
    kind: 'extrude',
    recipe: {
      d: 'M -80 -50 L 80 -50 L 50 60 L -50 60 Z',
      holes: [[[-0.2, -0.2], [0.2, -0.2], [0.2, 0.2], [-0.2, 0.2]]],
      depth: 0.5,
      bevel: 0.04,
    },
  },
  {
    name: 'shape_14_blob_metaballs',
    kind: 'blob',
    recipe: {
      balls: [
        [-0.45, 0, 0, 0.55],
        [0.45, 0, 0, 0.55],
        [0, 0.45, 0, 0.45],
      ],
      detail: 5,
    },
  },
  {
    name: 'shape_15_poly_platonic_and_subdivided',
    kind: 'poly',
    recipe: {
      faces: 12,
      r: 0.9,
      detail: 1,
    },
  },
  {
    name: 'shape_16_surface_parametric_grid',
    kind: 'surface',
    recipe: {
      grid: [
        [[-1, 0.0, -1], [0, 0.3, -1], [1, 0.0, -1]],
        [[-1, 0.3, 0], [0, 0.8, 0], [1, 0.3, 0]],
        [[-1, 0.0, 1], [0, 0.3, 1], [1, 0.0, 1]],
      ],
    },
  },
];

// 2. Modifiers tests (`thick` solidify, `stretch`, `mirror`, `cut`/`arc`/`slice`, `around`, `copies`)
const modifierTests = [
  {
    name: 'mod_01_thick_solidify_open_lathe',
    kind: 'lathe',
    recipe: {
      points: [
        [0.3, -0.8],
        [0.9, -0.2],
        [0.7, 0.8],
      ],
      thick: 0.12,
    },
  },
  {
    name: 'mod_02_thick_solidify_surface_sheet',
    kind: 'surface',
    recipe: {
      grid: [
        [[-1, 0.0, -1], [0, 0.3, -1], [1, 0.0, -1]],
        [[-1, 0.3, 0], [0, 0.7, 0], [1, 0.3, 0]],
        [[-1, 0.0, 1], [0, 0.3, 1], [1, 0.0, 1]],
      ],
      thick: 0.15,
    },
  },
  {
    name: 'mod_03_stretch_non_uniform_scale',
    kind: 'sphere',
    recipe: {
      r: 0.8,
      stretch: [1.8, 0.6, 1.1],
    },
  },
  {
    name: 'mod_04_mirror_negative_determinant_rewind',
    kind: 'extrude',
    recipe: {
      shape: [[0, 0], [1, 0], [0.3, 0.8]],
      depth: 0.4,
      mirror: [-1, 1, 1],
    },
  },
  {
    name: 'mod_05_cut_sector_and_dome_sphere',
    kind: 'sphere',
    recipe: {
      r: 0.9,
      arc: 270,
      dome: true,
      thick: 0.1,
    },
  },
  {
    name: 'mod_06_axis_orientation_vector_and_string',
    kind: 'cyl',
    recipe: {
      r: 0.5,
      h: 1.5,
      axis: [1, 1, 0],
      stretch: [1.2, 0.9, 1.4],
      mirror: [-1, -1, 1],
    },
  },
];

// Verify `around`, `copies`, `mirror`, `cut` expansion inside `figure()` in `clicky-draw-engine.js`
const dslModifierCode = `
  const f = figure(100, 100, 400, 300, { title: "DSL Modifiers" });
  f.box({ at: [1, 0, 0], size: [0.4, 0.4, 0.4], mirror: "x", name: "MirroredBox" });
  f.cyl({ at: [2, 0, 0], r: 0.2, h: 0.6, around: { count: 6, axis: "y", center: [0, 0, 0] }, name: "Bolt" });
  f.sphere({ at: [0, 0, 0], r: 0.3, copies: { count: 4, step: [0.8, 0, 0] }, cut: 90, name: "CutSphere" });
`;
const dslMarks = ctx.ClickyDraw.runCode(dslModifierCode, 1920, 1080, {});
const dslPartMarks = dslMarks.filter((m) => m.do === 'part3d');
const dslModifierExpansion = {
  totalExpandedParts: dslPartMarks.length, // 2 (mirror: "x") + 6 (around: 6) + 4 (copies: 4) = 12
  mirroredBoxCount: dslPartMarks.filter((m) => m.name?.startsWith('MirroredBox')).length,
  boltAroundCount: dslPartMarks.filter((m) => m.name?.startsWith('Bolt')).length,
  cutSphereCopiesCount: dslPartMarks.filter((m) => m.name?.startsWith('CutSphere')).length,
  cutSphereArcParam: JSON.parse(dslPartMarks.find((m) => m.name === 'CutSphere').recipe).arc,
};

// 3. Parameter extremes tests
const extremeTests = [
  { name: 'ext_01_box_zero_dimensions', kind: 'box', recipe: { size: [0, 0, 0] }, expectValid: true },
  { name: 'ext_02_box_negative_dimensions', kind: 'box', recipe: { size: [-2, -3, -1], bevel: -0.5 }, expectValid: true },
  {
    name: 'ext_03_cyl_nan_and_infinity',
    kind: 'cyl',
    recipe: { r: Number.NaN, top: Number.POSITIVE_INFINITY, h: Number.NEGATIVE_INFINITY, seg: Number.NaN },
    expectValid: true,
  },
  { name: 'ext_04_cone_with_top_param_AUD_G_001', kind: 'cone', recipe: { r: 0.8, top: 0.4, h: 1.2 }, expectValid: true },
  { name: 'ext_05_sphere_huge_segments_clamped', kind: 'sphere', recipe: { r: 1.0, seg: 999999 }, expectValid: true },
  {
    name: 'ext_06_spring_huge_coils_and_zero_wire_clamped',
    kind: 'spring',
    recipe: { r: 0, wire: 0, h: -5, coils: 100000 },
    expectValid: true,
  },
  { name: 'ext_07_gear_extreme_teeth_clamped', kind: 'gear', recipe: { r: 1.0, teeth: 5000, w: 0, hole: 5.0 }, expectValid: true },
  { name: 'ext_08_lathe_empty_points_fallback', kind: 'lathe', recipe: { points: [] }, expectValid: true },
  { name: 'ext_09_pipe_single_point_fallback', kind: 'pipe', recipe: { points: [[1, 2, 3]] }, expectValid: true },
  {
    name: 'ext_10_extrude_invalid_svg_path_fallback',
    kind: 'extrude',
    recipe: { d: 'INVALID SVG ??? M NaN Infinity Z', depth: -1 },
    expectValid: true,
  },
  {
    name: 'ext_11_extrude_collinear_degenerate_shape_AUD_G_004',
    kind: 'extrude',
    recipe: { shape: [[0, 0], [1, 0], [2, 0]], depth: 0.5 },
    // Collinear 2D shape points produce side bevel/extrude quads without NaN (valid vertex/index arrays, 0 cap faces)
    expectValid: true,
  },
  { name: 'ext_12_blob_zero_balls_fallback', kind: 'blob', recipe: { balls: [] }, expectValid: true },
  {
    name: 'ext_13_blob_500_balls_clamped_to_24',
    kind: 'blob',
    recipe: {
      balls: Array.from({ length: 500 }, (_, i) => [Math.sin(i) * 0.6, Math.cos(i) * 0.6, (i % 5) * 0.1, 0.35]),
      detail: 500,
    },
    expectValid: true,
  },
  {
    name: 'ext_14_poly_extreme_detail_and_invalid_faces_clamped',
    kind: 'poly',
    recipe: { faces: 999, r: -5, detail: 999 },
    expectValid: true,
  },
  { name: 'ext_15_surface_empty_grid_fallback', kind: 'surface', recipe: { grid: [] }, expectValid: true },
  {
    name: 'ext_16_vertex_cap_120k_guard_throws',
    kind: 'extrude',
    recipe: {
      // 2000-vertex star polygon with 20 holes + bevel + solidify exceeds MAX_VERTICES = 120,000 and triggers pack() guard
      shape: Array.from({ length: 2000 }, (_, i) => {
        const a = (i / 2000) * Math.PI * 2;
        const r = i % 2 === 0 ? 10.0 : 9.2;
        return [Math.cos(a) * r, Math.sin(a) * r];
      }),
      holes: Array.from({ length: 12 }, (_, hIdx) =>
        Array.from({ length: 1500 }, (_, i) => {
          const a = (i / 1500) * Math.PI * 2;
          const cx = Math.cos((hIdx / 12) * Math.PI * 2) * 5;
          const cy = Math.sin((hIdx / 12) * Math.PI * 2) * 5;
          return [cx + Math.cos(a) * 0.8, cy + Math.sin(a) * 0.8];
        })
      ),
      depth: 1.0,
      bevel: 0.1,
      thick: 0.05,
    },
    expectValid: false,
    expectThrowSubstring: 'part too detailed',
  },
  {
    name: 'ext_17_unknown_shape_kind_throws',
    kind: 'non_existent_shape',
    recipe: {},
    expectValid: false,
    expectThrowSubstring: 'unknown 3D part',
  },
];

const normalResults = normalShapeTests.map((t) => runBuild(t.name, t.kind, t.recipe));
const modifierResults = modifierTests.map((t) => runBuild(t.name, t.kind, t.recipe));
const extremeResults = extremeTests.map((t) => {
  const res = runBuild(t.name, t.kind, t.recipe);
  const passedExpectation = t.expectThrowSubstring
    ? Boolean(res.thrown && res.thrown.includes(t.expectThrowSubstring))
    : res.valid === t.expectValid;
  return { ...res, expectValid: t.expectValid, expectThrowSubstring: t.expectThrowSubstring || null, passedExpectation };
});

// Verify AUD-G-001 remediation directly: compare cone with top: 0.4 (frustum) vs cone without top (pointed)
const coneWithTop = clickyThreeBuild('cone', JSON.stringify({ r: 0.8, top: 0.4, h: 1.2, seg: 24 }));
const coneWithoutTop = clickyThreeBuild('cone', JSON.stringify({ r: 0.8, h: 1.2, seg: 24 }));
let coneTopIgnoredByteIdentical = coneWithTop.positions.length === coneWithoutTop.positions.length;
if (coneTopIgnoredByteIdentical) {
  for (let i = 0; i < coneWithTop.positions.length; i++) {
    if (coneWithTop.positions[i] !== coneWithoutTop.positions[i]) {
      coneTopIgnoredByteIdentical = false;
      break;
    }
  }
}
const coneTopParameterRespected = !coneTopIgnoredByteIdentical && coneWithTop.positions.length > 0;

const allNormalValid = normalResults.every((r) => r.valid);
const allModifiersValid = modifierResults.every((r) => r.valid) && dslModifierExpansion.totalExpandedParts === 12;
const allExtremesMetExpectation = extremeResults.every((r) => r.passedExpectation);
const totalTests = normalResults.length + modifierResults.length + extremeResults.length;
const passCount =
  normalResults.filter((r) => r.valid).length +
  modifierResults.filter((r) => r.valid).length +
  extremeResults.filter((r) => r.passedExpectation).length;
const failCount = totalTests - passCount + (coneTopParameterRespected ? 0 : 1);

const summary = {
  timestamp: new Date().toISOString(),
  clickyThreeKinds,
  totalTests,
  passCount,
  failCount,
  totalShapeTypesTested: normalResults.length,
  allNormalValid,
  allModifiersValid,
  allExtremesMetExpectation,
  coneTopParameterRespected,
  coneTopParameterIgnoredByteIdentical: coneTopIgnoredByteIdentical,
  dslModifierExpansion,
  normalResults,
  modifierResults,
  extremeResults,
};

fs.mkdirSync(OUT_DIR, { recursive: true });
const outPath = path.join(OUT_DIR, '3d-engine-results.json');
fs.writeFileSync(outPath, JSON.stringify(summary, null, 2), 'utf8');

console.log(`Executed 3D engine geometry & modifier suite:`);
console.log(`  clickyThreeKinds (${clickyThreeKinds.length}): ${clickyThreeKinds.join(', ')}`);
console.log(`  Normal shapes valid: ${normalResults.filter((r) => r.valid).length}/${normalResults.length}`);
console.log(`  Modifiers valid: ${modifierResults.filter((r) => r.valid).length}/${modifierResults.length} (DSL expansion: ${dslModifierExpansion.totalExpandedParts} parts)`);
console.log(`  Parameter extremes met expectations: ${extremeResults.filter((r) => r.passedExpectation).length}/${extremeResults.length}`);
console.log(`  AUD-G-001 remediated (cone respects 'top' frustum radius): ${coneTopParameterRespected}`);
console.log(`  PASS: ${passCount}/${totalTests}, FAIL: ${failCount}`);
console.log(`Evidence written to ${outPath}`);

if (!allNormalValid || !allModifiersValid || !allExtremesMetExpectation || !coneTopParameterRespected) {
  for (const r of normalResults.filter((x) => !x.valid)) console.error('Normal failed:', r);
  for (const r of modifierResults.filter((x) => !x.valid)) console.error('Modifier failed:', r);
  for (const r of extremeResults.filter((x) => !x.passedExpectation)) console.error('Extreme failed:', r);
  process.exitCode = 1;
}

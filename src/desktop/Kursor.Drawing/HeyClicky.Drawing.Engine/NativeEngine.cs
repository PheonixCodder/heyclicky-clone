using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HeyClicky.Drawing.Audio;
using HeyClicky.Drawing.Painter;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace HeyClicky.Drawing.Engine;

internal sealed class NativeEngine : IDisposable
{
	public sealed record DraggableCard(RectD Box, int[] Marks, int[] Units, CardWindow? Window);

	public sealed record CardWindow(RectD Box, double TitleBarHeight, double Scale, bool IsFullScreen, string Title, bool Holds3DFigure);

	private sealed record MarkSound(string? Texture, (string SoundId, double Offset, bool FromEnd)[] Events, double FallbackBeginTime, double FallbackDuration);

	private readonly DrawingThread thread;

	private readonly ScreenPainter painter;

	private readonly string engineDirectory;

	private readonly SoundPlayer? sounds;

	private V8ScriptEngine? engine;

	private ScriptObject? canvas;

	private ScriptObject? drain;

	private ScriptObject? fireTimer;

	private readonly Dictionary<string, ScriptObject> methods = new Dictionary<string, ScriptObject>(StringComparer.Ordinal);

	private readonly Dictionary<int, int> timerTokens = new Dictionary<int, int>();

	public static volatile bool FiguresFloat;

	public static string ClickyInkHex = "#3380FF";

	private const string HostShim = "(function (g) {\n  'use strict';\n  const raw = g.__hostRaw;\n  let lastPath = null;\n  const usePath = d => { d = d == null ? '' : String(d); if (d !== lastPath) { raw.setPath(d); lastPath = d; } };\n  g.__host = {\n    now: () => raw.now(),\n    schedule: (id, seconds) => raw.schedule(id | 0, +seconds || 0),\n    cancel: id => raw.cancel(id | 0),\n    log: m => raw.log(String(m)),\n    textWidth: (family, size, weight, text) => raw.textWidth(String(family), +size || 0, weight | 0, String(text)),\n    textBox: d => { raw.textBox(String(d)); return [raw.r0(), raw.r1(), raw.r2(), raw.r3()]; },\n    pathLength: d => { usePath(d); return raw.pathLength(); },\n    pathPoint: (d, length) => { usePath(d); raw.pathPoint(+length || 0); return [raw.r0(), raw.r1()]; },\n    pathBox: d => { raw.pathBox(d == null ? '' : String(d)); return [raw.r0(), raw.r1(), raw.r2(), raw.r3()]; },\n    inkFor: (x, y, w, h) => raw.inkFor(+x || 0, +y || 0, +w || 0, +h || 0),\n    busyFor: (x, y, w, h) => raw.busyFor(+x || 0, +y || 0, +w || 0, +h || 0),\n    lumFor: (x, y, w, h) => raw.lumFor(+x || 0, +y || 0, +w || 0, +h || 0),\n    // Stream mode: a 3D figure never stands on a card or in a window, it floats.\n    figuresFloat: () => raw.figuresFloat() === true,\n    caption: json => raw.caption(String(json)),\n    markSound: (mark, json) => raw.markSound(mark | 0, String(json)),\n    buttonReady: (mark, seconds) => raw.buttonReady(mark | 0, Number.isFinite(+seconds) ? +seconds : 0),\n    shape3d: (kind, recipe) => raw.shape3d(String(kind), String(recipe)),\n  };\n  // Calls from the app with plain arguments (no host objects cross into the engine).\n  g.__win = {\n    beginStream: (w, h, width, subtitles, ink) => clickyCanvas.beginStream({ w, h, width, subtitles, ink }),\n    feed: (code, done) => clickyCanvas.feed({ code, done }),\n    continueStream: subtitles => clickyCanvas.continueStream({ subtitles }),\n    closeCard: marksJson => clickyCanvas.closeCard({ marks: JSON.parse(marksJson) }),\n    zoomCard: (marksJson, toJson) => clickyCanvas.zoomCard({ marks: JSON.parse(marksJson), to: JSON.parse(toJson) }),\n    moveCard: (marksJson, dx, dy) => clickyCanvas.moveCard({ marks: JSON.parse(marksJson), dx, dy }),\n    markBox: i => clickyCanvas.markBox({ i }),\n    buttonSpot: json => clickyCanvas.buttonSpot(JSON.parse(json)),\n    renderWhole: (w, h, width, code, ink) => clickyCanvas.renderWhole({ w, h, width, code, ink }),\n    tick3d: (stage, ticksJson, dt) => {\n      const poses = clickyCanvas.tick3d({ stage, ticks: JSON.parse(ticksJson), dt });\n      return poses ? Array.prototype.join.call(poses, ',') : '';\n    },\n  };\n})(this);";

	private readonly Dictionary<int, MarkSound> pendingMarkSounds = new Dictionary<int, MarkSound>();

	private static readonly Dictionary<string, float> DelightfulSoundGains = new Dictionary<string, float>
	{
		["photo-drop"] = 0.6f,
		["paper-place"] = 0.45f,
		["sticky-press"] = 0.5f,
		["button-appear"] = 0.65f,
		["target-ping"] = 0.55f,
		["type-keys"] = 0.45f,
		["copy-tick"] = 0.55f,
		["page-clear"] = 0.55f,
		["build-3d"] = 0.5f,
		["boing"] = 0.7f,
		["tada"] = 0.8f,
		["ding"] = 0.8f,
		["nope"] = 0.75f,
		["stamp"] = 0.8f,
		["sparkle"] = 0.75f,
		["whoosh"] = 0.7f,
		["paper-unfold"] = 0.75f,
		["page-turn"] = 0.55f,
		["scissor-snip"] = 0.5f
	};

	private static readonly HashSet<string> AccentSoundIds = new HashSet<string> { "tada", "ding", "nope", "stamp", "sparkle", "whoosh", "boing" };

	private double lastAmbientSoundTime = double.NegativeInfinity;

	private double lastAccentSoundTime = double.NegativeInfinity;

	private readonly Dictionary<string, double> lastSoundTimeById = new Dictionary<string, double>();

	private const string WarmUpProgram = "step(1); say('Warming up.')\nring(120, 60, 300, 44, 'search', {r: 22})\ncircle(220, 300, 60, {fill: 'butter', tone: 'orange'})\nink([[350, 170], [300, 190]], {arrow: true})\nstep(2); say('A card.')\nconst a = paper(620, 80, 420, 300, {title: 'Warm', heading: 'Warming up', subtitle: 'a card'})\nconst g = graph(a.x + 30, a.y + 10, a.w - 60, a.h - 60, {x: [0, 10], y: [0, 10], ticks: true})\ng.plot([[0, 1], [5, 6], [10, 9]], {tone: 'sky'})\nsticky(100, 420, 'Warm up the engine', 'mint')\ntag(360, 150, 'warm')\nhighlight(120, 330, 260, 34)";

	public ScreenshotSampler Screenshot { get; } = new ScreenshotSampler();

	public bool PlaysSounds { get; set; } = true;

	public bool PlaysPenSounds { get; set; }

	public static bool DisableJit { get; set; }

	public double LoadMilliseconds { get; private set; }

	public long HeapBytes => (long)(engine?.GetRuntimeHeapInfo().TotalHeapSize ?? 0);

	public bool IsFinished
	{
		get
		{
			if (engine != null)
			{
				return AsBool(Call("isFinished"));
			}
			return true;
		}
	}

	internal Action<List<SoundItem>>? SoundsScheduled { get; set; }

	public event Action<string>? Caption;

	public event Action<int, double>? ButtonReady;

	public event Action<string>? RenderOps;

	public NativeEngine(DrawingThread thread, ScreenPainter painter, string engineDirectory, SoundPlayer? sounds)
	{
		this.thread = thread;
		this.painter = painter;
		this.engineDirectory = engineDirectory;
		this.sounds = sounds;
		painter.MarkTimingsReady += ScheduleSounds;
		Drawing3DAdapter figures = painter.Figures;
		if (figures != null)
		{
			figures.TickRunner = RunFigureTicks;
		}
	}

	public bool Load()
	{
		if (engine != null)
		{
			return true;
		}
		double now = DrawingThread.Now;
		try
		{
			if (DisableJit)
			{
				V8Settings.GlobalFlags |= V8GlobalFlags.DisableJITCompilation;
			}
			engine = new V8ScriptEngine("ClickyDraw", new V8RuntimeConstraints
			{
				MaxOldSpaceSize = 128,
				MaxNewSpaceSize = 32
			}, V8ScriptEngineFlags.None);
			engine.AddHostObject("__hostRaw", new HostBridge
			{
				Engine = this
			});
			engine.Execute(new DocumentInfo("clicky-host-shim.js"), "(function (g) {\n  'use strict';\n  const raw = g.__hostRaw;\n  let lastPath = null;\n  const usePath = d => { d = d == null ? '' : String(d); if (d !== lastPath) { raw.setPath(d); lastPath = d; } };\n  g.__host = {\n    now: () => raw.now(),\n    schedule: (id, seconds) => raw.schedule(id | 0, +seconds || 0),\n    cancel: id => raw.cancel(id | 0),\n    log: m => raw.log(String(m)),\n    textWidth: (family, size, weight, text) => raw.textWidth(String(family), +size || 0, weight | 0, String(text)),\n    textBox: d => { raw.textBox(String(d)); return [raw.r0(), raw.r1(), raw.r2(), raw.r3()]; },\n    pathLength: d => { usePath(d); return raw.pathLength(); },\n    pathPoint: (d, length) => { usePath(d); raw.pathPoint(+length || 0); return [raw.r0(), raw.r1()]; },\n    pathBox: d => { raw.pathBox(d == null ? '' : String(d)); return [raw.r0(), raw.r1(), raw.r2(), raw.r3()]; },\n    inkFor: (x, y, w, h) => raw.inkFor(+x || 0, +y || 0, +w || 0, +h || 0),\n    busyFor: (x, y, w, h) => raw.busyFor(+x || 0, +y || 0, +w || 0, +h || 0),\n    lumFor: (x, y, w, h) => raw.lumFor(+x || 0, +y || 0, +w || 0, +h || 0),\n    // Stream mode: a 3D figure never stands on a card or in a window, it floats.\n    figuresFloat: () => raw.figuresFloat() === true,\n    caption: json => raw.caption(String(json)),\n    markSound: (mark, json) => raw.markSound(mark | 0, String(json)),\n    buttonReady: (mark, seconds) => raw.buttonReady(mark | 0, Number.isFinite(+seconds) ? +seconds : 0),\n    shape3d: (kind, recipe) => raw.shape3d(String(kind), String(recipe)),\n  };\n  // Calls from the app with plain arguments (no host objects cross into the engine).\n  g.__win = {\n    beginStream: (w, h, width, subtitles, ink) => clickyCanvas.beginStream({ w, h, width, subtitles, ink }),\n    feed: (code, done) => clickyCanvas.feed({ code, done }),\n    continueStream: subtitles => clickyCanvas.continueStream({ subtitles }),\n    closeCard: marksJson => clickyCanvas.closeCard({ marks: JSON.parse(marksJson) }),\n    zoomCard: (marksJson, toJson) => clickyCanvas.zoomCard({ marks: JSON.parse(marksJson), to: JSON.parse(toJson) }),\n    moveCard: (marksJson, dx, dy) => clickyCanvas.moveCard({ marks: JSON.parse(marksJson), dx, dy }),\n    markBox: i => clickyCanvas.markBox({ i }),\n    buttonSpot: json => clickyCanvas.buttonSpot(JSON.parse(json)),\n    renderWhole: (w, h, width, code, ink) => clickyCanvas.renderWhole({ w, h, width, code, ink }),\n    tick3d: (stage, ticksJson, dt) => {\n      const poses = clickyCanvas.tick3d({ stage, ticks: JSON.parse(ticksJson), dt });\n      return poses ? Array.prototype.join.call(poses, ',') : '';\n    },\n  };\n})(this);");
			string[] array = new string[2] { "clicky-draw-native", "clicky-draw-engine" };
			foreach (string text in array)
			{
				string path = Path.Combine(engineDirectory, text + ".js");
				if (!File.Exists(path))
				{
					Log.Error("Drawing", "native engine: " + text + ".js is missing");
					return Fail();
				}
				RunCached(path);
			}
			string path2 = Path.Combine(engineDirectory, "clicky-draw-pieces.js");
			if (!File.Exists(path2))
			{
				Log.Error("Drawing", "native engine: clicky-draw-pieces.js is missing");
				return Fail();
			}
			canvas = (ScriptObject)engine.Script.clickyCanvas;
			drain = (ScriptObject)engine.Script.__drainRender;
			fireTimer = (ScriptObject)engine.Script.__fireTimer;
			Call("setPieces", File.ReadAllText(path2));
			object obj = engine.Evaluate("typeof ClickyDraw === 'object'");
			bool flag = obj is bool && (bool)obj;
			LoadMilliseconds = (DrawingThread.Now - now) * 1000.0;
			Log.Info("Drawing", $"native engine loaded={flag} in {LoadMilliseconds:F0} ms");
			return flag || Fail();
		}
		catch (Exception ex)
		{
			Log.Error("Drawing", "native engine failed to load", ex);
			return Fail();
		}
	}

	private bool Fail()
	{
		engine?.Dispose();
		engine = null;
		canvas = (drain = (fireTimer = null));
		methods.Clear();
		return false;
	}

	private void RunCached(string path)
	{
		string text = File.ReadAllText(path);
		string text2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text + typeof(V8ScriptEngine).Assembly.GetName().Version))).Substring(0, 16);
		string text3 = AppPaths.Combine("cache", "drawing-v8");
		string path2 = Path.Combine(text3, Path.GetFileNameWithoutExtension(path) + "-" + text2 + ".bin");
		byte[] array = null;
		try
		{
			if (File.Exists(path2))
			{
				array = File.ReadAllBytes(path2);
			}
		}
		catch
		{
		}
		DocumentInfo documentInfo = new DocumentInfo(Path.GetFileName(path));
		V8Script v8Script;
		if (array != null)
		{
			v8Script = engine.Compile(documentInfo, text, V8CacheKind.Code, array, out var cacheAccepted);
			if (!cacheAccepted)
			{
				array = null;
			}
		}
		else
		{
			v8Script = null;
		}
		if (array == null)
		{
			v8Script = engine.Compile(documentInfo, text, V8CacheKind.Code, out var cacheBytes);
			try
			{
				Directory.CreateDirectory(text3);
				if (cacheBytes != null && cacheBytes.Length > 0)
				{
					File.WriteAllBytes(path2, cacheBytes);
				}
			}
			catch
			{
			}
		}
		using (v8Script)
		{
			engine.Evaluate(v8Script);
		}
	}

	private void RunWithWatchdog(Action action, int timeoutMs)
	{
		RunWithWatchdog<object?>(delegate
		{
			action();
			return null;
		}, timeoutMs, null);
	}

	private T RunWithWatchdog<T>(Func<T> func, int timeoutMs, T fallback)
	{
		if (engine == null)
		{
			return fallback;
		}
		V8ScriptEngine v8 = engine;
		using (new System.Threading.Timer(delegate
		{
			try
			{
				v8?.Interrupt();
			}
			catch
			{
			}
		}, null, timeoutMs, System.Threading.Timeout.Infinite))
		{
			try
			{
				return func();
			}
			catch (ScriptInterruptedException ex)
			{
				Log.Warn("Drawing", $"V8 execution interrupted by watchdog ({timeoutMs} ms): {ex.Message}");
				return fallback;
			}
		}
	}

	private object? Call(string method, params object[] arguments)
	{
		if (engine == null || canvas == null)
		{
			return null;
		}
		object obj = null;
		try
		{
			if (!methods.TryGetValue(method, out ScriptObject value))
			{
				bool flag;
				switch (method)
				{
				case "isFinished":
				case "lineLength":
				case "cards":
				case "clear":
				case "state":
				case "setPieces":
				case "line":
				case "finish":
				case "timeline":
				case "keepNotesOnly":
				case "dropPointing":
				case "now":
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				value = (flag ? ((ScriptObject)engine.Evaluate("(function () { return clickyCanvas." + method + ".apply(clickyCanvas, arguments); })")) : ((ScriptObject)engine.Evaluate("__win." + method)));
				methods[method] = value;
			}
			obj = RunWithWatchdog(() => value.Invoke(asConstructor: false, arguments), 1500, null);
		}
		catch (ScriptInterruptedException ex)
		{
			Log.Warn("Drawing", "engine " + method + " interrupted by watchdog: " + ex.Message);
		}
		catch (ScriptEngineException ex)
		{
			Log.Warn("Drawing", "engine " + method + " threw: " + ex.ErrorDetails);
		}
		catch (Exception ex2)
		{
			Log.Error("Drawing", "engine " + method + " failed", ex2);
		}
		DrainRender();
		ScheduleSoundsOfUnpaintedMarks();
		if ((!(obj is Undefined) && !(obj is DBNull)) || 1 == 0)
		{
			return obj;
		}
		return null;
	}

	private static string? AsString(object? value)
	{
		return value as string;
	}

	private static bool AsBool(object? value)
	{
		if (value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	private void DrainRender()
	{
		if (drain != null)
		{
			string text;
			try
			{
				text = drain.Invoke(false) as string;
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "drain failed: " + ex.Message);
				return;
			}
			if (!string.IsNullOrEmpty(text))
			{
				painter.Apply(text);
				this.RenderOps?.Invoke(text);
			}
		}
	}

	public bool BeginStream(int canvasWidth, int canvasHeight, double pointWidth, bool subtitles)
	{
		StopSounds();
		sounds?.Prepare();
		CancelAllTimers();
		return AsBool(Call("beginStream", canvasWidth, canvasHeight, pointWidth, subtitles, ClickyInkHex));
	}

	public string? Feed(string code, bool done)
	{
		return AsString(Call("feed", code, done));
	}

	public bool Line(int index)
	{
		return AsBool(Call("line", index));
	}

	public void LineLength(int index, double seconds)
	{
		Call("lineLength", index, seconds);
	}

	public void Finish()
	{
		Call("finish");
	}

	public string? Timeline()
	{
		return AsString(Call("timeline"));
	}

	public string? DrawingState()
	{
		return AsString(Call("state"));
	}

	public (int FirstLine, int Marks, string Code)? ContinueStream(bool subtitles)
	{
		string text = AsString(Call("continueStream", subtitles));
		if (text == null)
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			JsonElement rootElement = jsonDocument.RootElement;
			painter.Figures?.HoldStill();
			return (rootElement.GetProperty("firstLine").GetInt32(), rootElement.GetProperty("marks").GetInt32(), rootElement.GetProperty("code").GetString() ?? "");
		}
		catch
		{
			return null;
		}
	}

	public (int Kept, int Dropped)? KeepNotesOnly()
	{
		return Pair(Call("keepNotesOnly"), "kept", "dropped");
	}

	public (int Dropped, int Left)? DropPointing()
	{
		return Pair(Call("dropPointing"), "dropped", "left");
	}

	public (int Closed, int Left)? CloseCard(IEnumerable<int> marks)
	{
		return Pair(Call("closeCard", JsonSerializer.Serialize(marks)), "closed", "left");
	}

	private static (int, int)? Pair(object? value, string first, string second)
	{
		if (!(value is string json))
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			return (jsonDocument.RootElement.GetProperty(first).GetInt32(), jsonDocument.RootElement.GetProperty(second).GetInt32());
		}
		catch
		{
			return null;
		}
	}

	public List<DraggableCard> DraggableCards()
	{
		List<DraggableCard> list = new List<DraggableCard>();
		string text = AsString(Call("cards"));
		if (text == null)
		{
			return list;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			foreach (JsonElement item in jsonDocument.RootElement.EnumerateArray())
			{
				RectD? rectD = Box(item, "box");
				if (!rectD.HasValue)
				{
					continue;
				}
				RectD valueOrDefault = rectD.GetValueOrDefault();
				CardWindow window = null;
				if (item.TryGetProperty("window", out var value) && value.ValueKind == JsonValueKind.Object)
				{
					rectD = Box(value, "box");
					if (rectD.HasValue)
					{
						RectD valueOrDefault2 = rectD.GetValueOrDefault();
						window = new CardWindow(valueOrDefault2, Number(value, "header"), Number(value, "scale", 1.0), value.TryGetProperty("zoomed", out var value2) && value2.ValueKind == JsonValueKind.True, value.TryGetProperty("title", out var value3) ? (value3.GetString() ?? "") : "", value.TryGetProperty("has3D", out var value4) && value4.ValueKind == JsonValueKind.True);
					}
				}
				list.Add(new DraggableCard(valueOrDefault, Ints(item, "marks"), Ints(item, "units"), window));
			}
		}
		catch
		{
		}
		return list;
	}

	private static RectD? Box(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 4)
		{
			return null;
		}
		double[] array = (from v in value.EnumerateArray()
			select (v.ValueKind != JsonValueKind.Number) ? 0.0 : v.GetDouble()).ToArray();
		if (!(array[2] > 0.0) || !(array[3] > 0.0))
		{
			return null;
		}
		return new RectD(array[0], array[1], array[2], array[3]);
	}

	private static double Number(JsonElement element, string name, double fallback = 0.0)
	{
		if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number)
		{
			return fallback;
		}
		return value.GetDouble();
	}

	private static int[] Ints(JsonElement element, string name)
	{
		if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
		{
			return Array.Empty<int>();
		}
		return (from v in value.EnumerateArray()
			where v.ValueKind == JsonValueKind.Number
			select v.GetInt32()).ToArray();
	}

	public int ZoomCard(IEnumerable<int> marks, double? scale, double offsetX = 0.0, double offsetY = 0.0)
	{
		object obj;
		if (scale.HasValue)
		{
			double valueOrDefault = scale.GetValueOrDefault();
			obj = JsonSerializer.Serialize(new double[3] { valueOrDefault, offsetX, offsetY });
		}
		else
		{
			obj = "null";
		}
		string text = (string)obj;
		object obj2 = Call("zoomCard", JsonSerializer.Serialize(marks), text);
		if (obj2 == null)
		{
			return 0;
		}
		return Convert.ToInt32(obj2);
	}

	public int MoveCard(IEnumerable<int> marks, double dx, double dy)
	{
		object obj = Call("moveCard", JsonSerializer.Serialize(marks), dx, dy);
		if (obj == null)
		{
			return 0;
		}
		return Convert.ToInt32(obj);
	}

	public RectD? DrawnBox(int mark)
	{
		string text = AsString(Call("markBox", mark));
		if (text == null)
		{
			return null;
		}
		try
		{
			double[] array = JsonSerializer.Deserialize<double[]>(text);
			return (array != null && array.Length == 4) ? new RectD?(new RectD(array[0], array[1], array[2], array[3])) : ((RectD?)null);
		}
		catch
		{
			return null;
		}
	}

	public RectD? ClearButtonSpot(RectD box, double gap, IEnumerable<RectD> avoiding)
	{
		string text = JsonSerializer.Serialize(new
		{
			box = new double[4] { box.X, box.Y, box.Width, box.Height },
			gap = gap,
			avoid = avoiding.Select((RectD r) => new double[4] { r.X, r.Y, r.Width, r.Height }).ToArray()
		});
		string text2 = AsString(Call("buttonSpot", text));
		if (text2 == null)
		{
			return null;
		}
		try
		{
			double[] array = JsonSerializer.Deserialize<double[]>(text2);
			return (array != null && array.Length == 4) ? new RectD?(new RectD(array[0], array[1], array[2], array[3])) : ((RectD?)null);
		}
		catch
		{
			return null;
		}
	}

	public void Clear()
	{
		StopSounds();
		Call("clear");
		CancelAllTimers();
		painter.ClearAll();
	}

	public string? RenderWhole(int canvasWidth, int canvasHeight, double pointWidth, string code)
	{
		CancelAllTimers();
		painter.ClearAll();
		return AsString(Call("renderWhole", canvasWidth, canvasHeight, pointWidth, code, ClickyInkHex));
	}

	public void SetGlobalFlag(string name, bool value)
	{
		try
		{
			if (engine != null)
			{
				engine.Script[name] = value;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "global " + name + " not set: " + ex.Message);
		}
	}

	public string? Evaluate(string script)
	{
		try
		{
			return RunWithWatchdog(() => engine?.Evaluate(script)?.ToString(), 1500, null);
		}
		catch (Exception ex)
		{
			return "error: " + ex.Message;
		}
	}

	private float[]? RunFigureTicks(string stageId, (string TickId, double Seconds)[] ticks, double frameSeconds)
	{
		try
		{
			return thread.Invoke(delegate
			{
				if (engine == null)
				{
					return (float[])null;
				}
				string text = JsonSerializer.Serialize(ticks.Select(((string TickId, double Seconds) t) => new object[2] { t.TickId, t.Seconds }));
				if (!methods.TryGetValue("tick3d", out ScriptObject value))
				{
					value = (methods["tick3d"] = (ScriptObject)engine.Evaluate("__win.tick3d"));
				}
				if (!(RunWithWatchdog(() => value.Invoke(false, stageId, text, frameSeconds), 100, null) is string { Length: not 0 } text2))
				{
					return (float[])null;
				}
				string[] array = text2.Split(',');
				float[] array2 = new float[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array2[i] = (float.TryParse(array[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0f);
				}
				return array2;
			});
		}
		catch
		{
			return null;
		}
	}

	internal void ScheduleTimer(int identifier, double seconds)
	{
		if (timerTokens.Remove(identifier, out var value))
		{
			thread.CancelTimer(value);
		}
		else if (timerTokens.Count >= 64)
		{
			return;
		}
		timerTokens[identifier] = thread.ScheduleTimer(DrawingThread.Now + Math.Max(0.016, seconds), delegate
		{
			FireTimer(identifier);
		});
	}

	internal void CancelTimer(int identifier)
	{
		if (timerTokens.Remove(identifier, out var value))
		{
			thread.CancelTimer(value);
		}
	}

	private void CancelAllTimers()
	{
		foreach (int value in timerTokens.Values)
		{
			thread.CancelTimer(value);
		}
		timerTokens.Clear();
	}

	private void FireTimer(int identifier)
	{
		if (timerTokens.Remove(identifier) && fireTimer != null)
		{
			try
			{
				fireTimer.Invoke(false, identifier);
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "engine timer threw: " + ex.Message);
			}
			DrainRender();
			ScheduleSoundsOfUnpaintedMarks();
		}
	}

	internal double TextWidth(string family, double size, int weight, string text)
	{
		return painter.TextWidth(family, size, weight, text);
	}

	internal (double, double, double, double) TextBox(string descriptionJson)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(descriptionJson);
			return painter.TextBox(jsonDocument.RootElement);
		}
		catch
		{
			return (0.0, 0.0, 0.0, 0.0);
		}
	}

	internal void RaiseCaption(string json)
	{
		this.Caption?.Invoke(json);
	}

	internal void RaiseButtonReady(int mark, double seconds)
	{
		this.ButtonReady?.Invoke(mark, double.IsFinite(seconds) ? Math.Max(0.0, seconds) : 0.0);
	}

	private List<SoundItem> Curated(List<SoundItem> items)
	{
		List<SoundItem> list = new List<SoundItem>();
		List<SoundItem> list2 = new List<SoundItem>();
		foreach (SoundItem item in items)
		{
			if (item.IsStroke)
			{
				if (PlaysPenSounds)
				{
					list.Add(item);
				}
			}
			else if (DelightfulSoundGains.ContainsKey(item.SoundId))
			{
				list2.Add(item);
			}
		}
		foreach (SoundItem item2 in list2.OrderBy((SoundItem e) => e.BeginTime))
		{
			if (lastSoundTimeById.TryGetValue(item2.SoundId, out var value) && Math.Abs(item2.BeginTime - value) < 2.0)
			{
				continue;
			}
			if (AccentSoundIds.Contains(item2.SoundId))
			{
				if (Math.Abs(item2.BeginTime - lastAccentSoundTime) < 1.0)
				{
					continue;
				}
				lastAccentSoundTime = item2.BeginTime;
			}
			else
			{
				if (Math.Abs(item2.BeginTime - lastAmbientSoundTime) < 0.8)
				{
					continue;
				}
				lastAmbientSoundTime = item2.BeginTime;
			}
			lastSoundTimeById[item2.SoundId] = item2.BeginTime;
			list.Add(SoundItem.Event(item2.SoundId, item2.BeginTime, DelightfulSoundGains[item2.SoundId]));
		}
		return list;
	}

	internal void NoteMarkSound(int mark, string json)
	{
		if (!PlaysSounds || sounds == null)
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			List<(string, double, bool)> list = new List<(string, double, bool)>();
			if (rootElement.TryGetProperty("events", out var value) && value.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value.EnumerateArray())
				{
					if (item.TryGetProperty("id", out var value2) && value2.ValueKind == JsonValueKind.String)
					{
						list.Add((value2.GetString(), Number(item, "at"), item.TryGetProperty("end", out var value3) && value3.ValueKind == JsonValueKind.True));
					}
				}
			}
			double num = Math.Max(0.0, Number(rootElement, "delay"));
			JsonElement value4;
			string texture = ((rootElement.TryGetProperty("texture", out value4) && value4.ValueKind == JsonValueKind.String) ? value4.GetString() : null);
			pendingMarkSounds[mark] = new MarkSound(texture, list.ToArray(), DrawingThread.Now + num, Math.Max(0.0, Number(rootElement, "dur")));
		}
		catch
		{
		}
	}

	private void ScheduleSounds(IReadOnlyList<MarkTiming> timings)
	{
		if (pendingMarkSounds.Count == 0 || sounds == null)
		{
			return;
		}
		List<SoundItem> list = new List<SoundItem>();
		foreach (MarkTiming timing in timings)
		{
			if (!pendingMarkSounds.Remove(timing.Mark, out MarkSound value))
			{
				continue;
			}
			string texture = value.Texture;
			if (texture != null)
			{
				bool flag = false;
				foreach (PenStroke penStroke in timing.PenStrokes)
				{
					if (penStroke.Points.Length > 1 && !(penStroke.Duration <= 0.02))
					{
						flag = true;
						list.Add(SoundItem.Stroke(texture, penStroke.BeginTime, penStroke.Duration, Length(penStroke.Points) * painter.PointsPerUnit, penStroke.Timing));
					}
				}
				if (!flag && timing.EndTime - timing.BeginTime > 0.1)
				{
					list.Add(SteadyStroke(texture, timing.BeginTime, timing.EndTime - timing.BeginTime));
				}
			}
			(string, double, bool)[] events = value.Events;
			for (int i = 0; i < events.Length; i++)
			{
				(string, double, bool) tuple = events[i];
				list.Add(SoundItem.Event(tuple.Item1, (tuple.Item3 ? timing.EndTime : timing.BeginTime) + tuple.Item2, 1f));
			}
		}
		ScheduleCurated(Curated(list));
	}

	private void ScheduleCurated(List<SoundItem> items)
	{
		if (items.Count != 0 && sounds != null)
		{
			SoundsScheduled?.Invoke(items);
			sounds.Schedule(items, this);
		}
	}

	private void ScheduleSoundsOfUnpaintedMarks()
	{
		if (pendingMarkSounds.Count == 0)
		{
			return;
		}
		List<SoundItem> list = new List<SoundItem>();
		foreach (MarkSound value in pendingMarkSounds.Values)
		{
			double fallbackBeginTime = value.FallbackBeginTime;
			string texture = value.Texture;
			if (texture != null && value.FallbackDuration > 0.1)
			{
				list.Add(SteadyStroke(texture, fallbackBeginTime, value.FallbackDuration));
			}
			(string, double, bool)[] events = value.Events;
			for (int i = 0; i < events.Length; i++)
			{
				(string, double, bool) tuple = events[i];
				list.Add(SoundItem.Event(tuple.Item1, (tuple.Item3 ? (fallbackBeginTime + value.FallbackDuration) : fallbackBeginTime) + tuple.Item2, 1f));
			}
		}
		pendingMarkSounds.Clear();
		if (PlaysSounds && sounds != null)
		{
			ScheduleCurated(Curated(list));
		}
	}

	public void PlayButtonAppearSound()
	{
		if (PlaysSounds && sounds != null)
		{
			ScheduleCurated(Curated(new List<SoundItem> { SoundItem.Event("button-appear", DrawingThread.Now, 1f) }));
		}
	}

	private static SoundItem SteadyStroke(string texture, double beginTime, double duration)
	{
		return SoundItem.Stroke(texture, beginTime, duration, 450.0 * duration, new Vector4(0f, 0f, 1f, 1f));
	}

	private static double Length(Vector2[] points)
	{
		double num = 0.0;
		for (int i = 1; i < points.Length; i++)
		{
			num += (double)Vector2.Distance(points[i - 1], points[i]);
		}
		return num;
	}

	public void StopSounds()
	{
		pendingMarkSounds.Clear();
		lastAmbientSoundTime = (lastAccentSoundTime = double.NegativeInfinity);
		lastSoundTimeById.Clear();
		sounds?.StopAll(this);
	}

	public void WarmUp(ScreenPainter painter)
	{
		bool playsSounds = PlaysSounds;
		PlaysSounds = false;
		double now = DrawingThread.Now;
		try
		{
			painter.Configure(new Vector2(1280f, 800f), 1440.0, 2.0);
			BeginStream(1280, 800, 1440.0, subtitles: false);
			Call("line", 0);
			Feed("step(1); say('Warming up.')\nring(120, 60, 300, 44, 'search', {r: 22})\ncircle(220, 300, 60, {fill: 'butter', tone: 'orange'})\nink([[350, 170], [300, 190]], {arrow: true})\nstep(2); say('A card.')\nconst a = paper(620, 80, 420, 300, {title: 'Warm', heading: 'Warming up', subtitle: 'a card'})\nconst g = graph(a.x + 30, a.y + 10, a.w - 60, a.h - 60, {x: [0, 10], y: [0, 10], ticks: true})\ng.plot([[0, 1], [5, 6], [10, 9]], {tone: 'sky'})\nsticky(100, 420, 'Warm up the engine', 'mint')\ntag(360, 150, 'warm')\nhighlight(120, 330, 260, 34)", done: true);
			Call("line", 1);
			Call("finish");
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "engine warm-up failed: " + ex.Message);
		}
		finally
		{
			Clear();
			PlaysSounds = playsSounds;
			Log.Info("Drawing", $"engine warmed up in {(DrawingThread.Now - now) * 1000.0:F0} ms");
		}
	}

	public void Dispose()
	{
		CancelAllTimers();
		foreach (ScriptObject value in methods.Values)
		{
			value.Dispose();
		}
		methods.Clear();
		engine?.Dispose();
		engine = null;
	}
}

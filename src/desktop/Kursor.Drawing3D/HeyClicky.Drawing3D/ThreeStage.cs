using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Direct2D1;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace HeyClicky.Drawing3D;

public sealed class ThreeStage : IDisposable
{
	private enum Slot
	{
		Left,
		Right,
		Top,
		Bottom
	}

	private struct Fitting
	{
		public string[] Lines;

		public double Size;

		public double Width;

		public readonly double LineAdvance => Size * 1.12;

		public readonly double Height => Size * 1.15 + (double)(Lines.Length - 1) * LineAdvance;
	}

	private sealed class StageLabel
	{
		public required string Id;

		public required string Text;

		public double Size;

		public double OneLineWidth;

		public required string[] OneLine;

		public string[]? TwoLines;

		public double TwoLinesWidth;

		public double Start;

		public double Duration;

		public Slot? RequestedSlot;

		public Slot? Slot;

		public Fitting? Fitting;

		public Rect2? WrittenBox;

		public Rect2? ShownBox;

		public int Unit;

		public string? Follow;
	}

	private struct DwmTimingInfo
	{
		public uint cbSize;

		public uint rateRefreshNumerator;

		public uint rateRefreshDenominator;

		public uint rateComposeNumerator;

		public uint rateComposeDenominator;

		public ulong qpcVBlank;

		public ulong cRefresh;

		public uint cDXRefresh;

		public ulong qpcCompose;

		public ulong cFrame;

		public uint cDXPresent;

		public ulong cRefreshFrame;

		public ulong cFrameSubmitted;

		public uint cDXPresentSubmitted;

		public ulong cFrameConfirmed;

		public uint cDXPresentConfirmed;

		public ulong cRefreshConfirmed;

		public uint cDXRefreshConfirmed;

		public ulong cFramesLate;

		public uint cFramesOutstanding;

		public ulong cFrameDisplayed;

		public ulong qpcFrameDisplayed;

		public ulong cRefreshFrameDisplayed;

		public ulong cFrameComplete;

		public ulong qpcFrameComplete;

		public ulong cFramePending;

		public ulong qpcFramePending;

		public ulong cFramesDisplayed;

		public ulong cFramesComplete;

		public ulong cFramesPending;

		public ulong cFramesAvailable;

		public ulong cFramesDropped;

		public ulong cFramesMissed;

		public ulong cRefreshNextDisplayed;

		public ulong cRefreshNextPresented;

		public ulong cRefreshesDisplayed;

		public ulong cRefreshesPresented;

		public ulong cRefreshStarted;

		public ulong cPixelsReceived;

		public ulong cPixelsDrawn;

		public ulong cBuffersEmpty;
	}

	private record struct LabelLayout(int Index, Slot Slot, Fitting Fitting, Rect2 Box, double PointX, double PointY);

	private struct LayoutItem
	{
		public int Index;

		public double PointX;

		public double PointY;

		public double DotX;

		public double DotY;

		public Slot Slot;

		public Fitting Fitting;

		public bool Movable;
	}

	internal readonly Scene3D Scene;

	private readonly string ink;

	private double devicePixelsPerUnit;

	private readonly object sync = new object();

	private static readonly long clockOrigin = Stopwatch.GetTimestamp();

	private static readonly bool DrawsIllustrated = Environment.GetEnvironmentVariable("CANVAS_3D_STYLE") != "physical";

	private const double OutlineWidth = 1.9;

	private static readonly (double width, double seamWidth, float seamStrength, float seamShade) StudioPens = (width: Scene3D.EnvFloat("CANVAS_3D_SILHOUETTE", 0.9f), seamWidth: Scene3D.EnvFloat("CANVAS_3D_SEAM", 0.6f), seamStrength: Scene3D.EnvFloat("CANVAS_3D_SEAM_STRENGTH", 1f), seamShade: Scene3D.EnvFloat("CANVAS_3D_SEAM_SHADE", 0.6f));

	private readonly Vector4 inkRGBA;

	private readonly Vector4 haloRGBA;

	private readonly WordsGlow? wordsGlow;

	private static readonly Vector4 PaperColor = new Vector4(50f / 51f, 49f / 51f, 78f / 85f, 1f);

	private static readonly Vector4 PaperShadow = new Vector4(0.10980392f, 0.11764706f, 12f / 85f, 1f) * 0.16f;

	private static readonly Vector4 TapeColor = new Vector4(0.99215686f, 0.99215686f, 0.99215686f, 1f) * 0.6f;

	private const double PaperShadowOffsetX = 3.0;

	private const double PaperShadowOffsetY = 5.0;

	private const double PaperPaddingX = 18.0;

	private const double PaperPaddingY = 20.0;

	private const double FlagPaddingX = 7.0;

	private const double FlagPaddingY = 4.0;

	private const double FlagTabWidth = 6.0;

	private const double FlagCornerRadius = 2.0;

	private const string FlagInk = "#30333A";

	private static readonly (Vector4 Strip, Vector4 Tab)[] FlagColors = new(Vector4, Vector4)[4]
	{
		(new Vector4(255f, 244f, 194f, 255f) / 255f, new Vector4(245f, 200f, 74f, 255f) / 255f),
		(new Vector4(255f, 224f, 235f, 255f) / 255f, new Vector4(242f, 138f, 176f, 255f) / 255f),
		(new Vector4(220f, 245f, 230f, 255f) / 255f, new Vector4(92f, 201f, 142f, 255f) / 255f),
		(new Vector4(223f, 236f, 255f, 255f) / 255f, new Vector4(111f, 163f, 242f, 255f) / 255f)
	};

	private static readonly Vector4 FlagShadow = new Vector4(0f, 0f, 0f, 1f);

	private readonly Vector4 wordsInkRGBA;

	private Rect2? cardShown;

	private double? cardShownTime;

	private Rect2? cardWanted;

	private int cardWantedRevision = -1;

	private readonly Dictionary<string, double> ticks = new Dictionary<string, double>();

	private string? partPrefixValue;

	private double? lastTickTime;

	private readonly List<TickDue> dueTicks = new List<TickDue>();

	private static readonly Comparison<TickDue> ByTickId = (TickDue a, TickDue b) => string.CompareOrdinal(a.TickId, b.TickId);

	private double? heldStillSince;

	private double turnRestartedAt = double.NegativeInfinity;

	private bool hasBegunRestingSwing;

	private float viewYaw = RestYaw;

	public static readonly float RestYaw = Scene3D.EnvFloat("CANVAS_3D_REST_YAW", -20f) * (float)Math.PI / 180f;

	public const double HoldSwingSeconds = 1.0;

	private static readonly float TurningFramesPerSecond = Scene3D.EnvFloat("CANVAS_3D_TURN_FPS", 60f);

	private static readonly bool KeepsTurning = Environment.GetEnvironmentVariable("CANVAS_3D_KEEP_TURNING") == "1";

	private const double TurnsAfterBuilt = 20.0;

	private const double ComesToRestOver = 2.5;

	private const double LongestCatchUp = 1.0;

	private double? simulatedTime;

	private const double SmallestLabelSize = 14.0;

	private readonly List<StageLabel> labels = new List<StageLabel>();

	private readonly Dictionary<string, (Vector3? point, string? follow)> labelAnchors = new Dictionary<string, (Vector3?, string)>();

	private int nextWordUnit = 1;

	private LabelWords? words;

	internal static int liveStages;

	internal static int liveRenderers;

	private ID3D11Texture2D? backBuffer;

	private ID3D11RenderTargetView? backBufferView;

	private ID2D1DeviceContext? d2dContext;

	private ID2D1Bitmap1? d2dTarget;

	private FigureRenderer? renderer;

	private Thread? frameThread;

	private readonly AutoResetEvent wake = new AutoResetEvent(initialState: false);

	private volatile bool stopping;

	private bool disposed;

	private volatile bool paused = true;

	private int targetWidth;

	private int targetHeight;

	private bool needsResize;

	private (int, int)? resizedTo;

	private bool presents;

	private const int LongestWaitForScreenMilliseconds = 250;

	private static bool hasCompositorClock = true;

	private bool labelsAreGliding;

	private readonly double[] cpuFrameMilliseconds = new double[256];

	private FigureRenderer.Capsule[] capsules = new FigureRenderer.Capsule[32];

	private readonly FigureRenderer.Sheet[] sheets = new FigureRenderer.Sheet[3];

	private FigureRenderer.Sheet[] flags = new FigureRenderer.Sheet[32];

	private double? lastBoxEaseTime;

	private readonly List<LayoutItem> items = new List<LayoutItem>();

	private static readonly Slot[] CrowdOrder = new Slot[4]
	{
		Slot.Bottom,
		Slot.Top,
		Slot.Left,
		Slot.Right
	};

	private readonly List<LabelLayout> layout = new List<LabelLayout>();

	private readonly List<LabelLayout> drawnLayout = new List<LabelLayout>();

	private readonly List<LabelLayout> lastLayout = new List<LabelLayout>();

	private readonly List<LabelPlacement3D> placements = new List<LabelPlacement3D>();

	private readonly double[] room = new double[4];

	private readonly List<int> candidates = new List<int>();

	private Rect2[] boxes = new Rect2[16];

	private bool[] hasBox = new bool[16];

	private readonly List<int> lineMembers = new List<int>();

	private double[] spreadPositions = new double[16];

	private int[] clusterFirst = new int[16];

	private int[] clusterCount = new int[16];

	private double[] clusterLength = new double[16];

	private double[] clusterStart = new double[16];

	private double[] offsets = new double[16];

	private readonly List<Vector2> samplePoints = new List<Vector2>();

	private ID3D11Texture2D? offscreen;

	private ID3D11Texture2D? offscreenStaging;

	private ID3D11RenderTargetView? offscreenView;

	private ID2D1Bitmap1? offscreenBitmap;

	private Rect2? interactiveRect;

	private bool interactiveRectChanged;

	private double? pressX;

	private double? pressY;

	private double? lastDragX;

	private double? lastDragY;

	private double dragTravel;

	private bool isDragging;

	private bool userTookControl;

	private double lastHandTime = double.NegativeInfinity;

	private bool relayoutAfterTurn;

	private Rect2? viewExtent;

	private static readonly (string, uint)[] ClickyTones = new(string, uint)[19]
	{
		("black", 1842724u),
		("white", 16777215u),
		("grey", 10133676u),
		("yellow", 16767053u),
		("golden", 16759088u),
		("orange", 16751164u),
		("peach", 16758170u),
		("coral", 15888491u),
		("red", 15026253u),
		("pink", 16095925u),
		("lilac", 11048949u),
		("violet", 7232472u),
		("light blue", 5941493u),
		("blue", 2056140u),
		("mint", 8312480u),
		("green", 4103772u),
		("brown", 10512189u),
		("tan", 14264439u),
		("cream", 16774102u)
	};

	public string Identifier { get; }

	public Rect2 Rect { get; }

	public Rect2 ObjectRect { get; }

	public Rect2 OuterRect { get; }

	public Rect2 LabelArea { get; }

	public Rect2? PaperArea { get; }

	public StageLook Look { get; }

	public Func<double> Now { get; set; } = DefaultClock;

	public TickProvider? TickProvider { get; set; }

	public bool LabelsWearFlags { get; }

	public static bool StreamMode { get; set; }

	private static bool StreamModeLabelFlagsEnabled => false;

	private string partPrefix => partPrefixValue ?? (partPrefixValue = Identifier + "p");

	public (int Width, int Height) PixelSize
	{
		get
		{
			lock (sync)
			{
				return DrawablePixelSize;
			}
		}
	}

	private (int, int) DrawablePixelSize => ((int)Math.Max(1.0, Math.Ceiling(OuterRect.Width * devicePixelsPerUnit)), (int)Math.Max(1.0, Math.Ceiling(OuterRect.Height * devicePixelsPerUnit)));

	public bool HasRunningTicks
	{
		get
		{
			lock (sync)
			{
				return ticks.Count > 0;
			}
		}
	}

	public bool IsSwingingToFront
	{
		get
		{
			lock (sync)
			{
				return Scene.IsSwingingToFront;
			}
		}
	}

	public static string? LabelFontPath
	{
		get
		{
			return LabelWords.FontPath;
		}
		set
		{
			LabelWords.FontPath = value;
		}
	}

	public static (int Stages, int Renderers) LiveCounts => (Stages: Volatile.Read(in liveStages), Renderers: Volatile.Read(in liveRenderers));

	public IStageSurface? Surface { get; set; }

	private bool IsBuilding => Now() < BuiltTime + 0.3;

	private bool IsHandActive
	{
		get
		{
			if (!isDragging && !relayoutAfterTurn && !labelsAreGliding && !(Now() < lastHandTime + 1.2))
			{
				return Scene.HasActiveHighlight;
			}
			return true;
		}
	}

	private double BuiltTime
	{
		get
		{
			double num = Scene.GrowthEndTime;
			foreach (StageLabel label in labels)
			{
				num = Math.Max(num, label.Start + label.Duration);
			}
			return num;
		}
	}

	public int FramesPresented { get; private set; }

	public double MedianCpuFrameMilliseconds
	{
		get
		{
			int num = Math.Min(FramesPresented, cpuFrameMilliseconds.Length);
			if (num == 0)
			{
				return 0.0;
			}
			double[] array = cpuFrameMilliseconds.AsSpan(0, num).ToArray();
			Array.Sort(array);
			return array[num / 2];
		}
	}

	public bool IsIdle => paused;

	public Matrix3x2 Placement { get; private set; } = Matrix3x2.Identity;

	public Rect2? InteractiveRect
	{
		get
		{
			lock (sync)
			{
				Rect2? rect = interactiveRect;
				Rect2? result;
				if (rect.HasValue)
				{
					Rect2 valueOrDefault = rect.GetValueOrDefault();
					result = Apply(Placement, valueOrDefault);
				}
				else
				{
					result = null;
				}
				return result;
			}
		}
	}

	public event Action<Rect2?>? InteractiveRectChanged;

	public event Action<StageClick>? PartClicked;

	public event Action<int, int>? SurfaceResized;

	private static double DefaultClock()
	{
		return Stopwatch.GetElapsedTime(clockOrigin).TotalSeconds;
	}

	public ThreeStage(string identifier, Rect2 rect, JsonElement camera, string ink, Rect2? card, double canvasWidth, double canvasHeight, double devicePixelsPerUnit, StageLook look = StageLook.Illustrated, bool presents = true)
	{
		Identifier = identifier;
		Rect = rect;
		this.ink = ink;
		Look = look;
		LabelsWearFlags = !card.HasValue && StreamModeLabelFlagsEnabled;
		this.devicePixelsPerUnit = devicePixelsPerUnit;
		double num = Math.Min(130.0, Math.Max(56.0, rect.Width * 0.2));
		ObjectRect = ((rect.Width > num * 2.0 + 80.0) ? rect.Inset(num, 6.0) : rect);
		Rect2 rect2 = card ?? rect;
		Rect2 other = new Rect2(0.0, 0.0, canvasWidth, canvasHeight).Inset(4.0, 4.0);
		Rect2 rect3 = ((canvasWidth > 0.0) ? rect2.Intersect(other).Union(ObjectRect) : rect2);
		Rect2 rect4 = (rect3.IsNull ? rect2 : rect3);
		PaperArea = ((!card.HasValue) ? ((Rect2?)null) : new Rect2?(rect4));
		double dx = 16.0;
		double dy = 8.0;
		LabelArea = (card.HasValue ? rect4.Inset(18.0, 20.0).Union(ObjectRect) : (LabelsWearFlags ? rect4.Inset(dx, dy).Union(ObjectRect) : rect4));
		Rect2? paperArea = PaperArea;
		if (paperArea.HasValue)
		{
			Rect2 valueOrDefault = paperArea.GetValueOrDefault();
			Rect2 rect5 = new Rect2(valueOrDefault.MinX - 2.0, valueOrDefault.MinY - 12.0, valueOrDefault.Width + 2.0 + 3.0 + 2.0, valueOrDefault.Height + 12.0 + 5.0 + 2.0);
			Rect2 other2 = new Rect2(0.0, 0.0, canvasWidth, canvasHeight);
			OuterRect = ((canvasWidth > 0.0) ? rect5.Intersect(other2).Union(valueOrDefault) : rect5);
		}
		else
		{
			OuterRect = LabelArea.Union(rect4);
		}
		Scene = new Scene3D(identifier, ObjectRect, camera)
		{
			RestingYaw = viewYaw
		};
		Vector4? vector = CssColor.Parse(ink);
		Vector4 vector2;
		if (vector.HasValue)
		{
			Vector4 valueOrDefault2 = vector.GetValueOrDefault();
			vector2 = new Vector4(valueOrDefault2.X, valueOrDefault2.Y, valueOrDefault2.Z, 1f) * valueOrDefault2.W;
		}
		else
		{
			vector2 = Vector4.One;
		}
		inkRGBA = vector2;
		string text = ink.ToUpperInvariant();
		haloRGBA = ((text == "#FFFFFF") ? (new Vector4(0.03137255f, 0.05490196f, 0.11764706f, 1f) * (LabelsWearFlags ? 0.8f : 0.55f)) : ((!(text == "#15171C")) ? (LabelsWearFlags ? (Vector4.One * 0.8f) : Vector4.Zero) : (Vector4.One * (LabelsWearFlags ? 0.8f : 0.6f))));
		string raw = (LabelsWearFlags ? "#30333A" : ink);
		Vector4? vector3 = CssColor.Parse(raw);
		Vector4 vector4;
		if (vector3.HasValue)
		{
			Vector4 valueOrDefault3 = vector3.GetValueOrDefault();
			vector4 = new Vector4(valueOrDefault3.X, valueOrDefault3.Y, valueOrDefault3.Z, 1f) * valueOrDefault3.W;
		}
		else
		{
			vector4 = inkRGBA;
		}
		wordsInkRGBA = vector4;
		wordsGlow = WordsGlow.ForInk(raw);
		if (presents)
		{
			StartFrameThread();
		}
		PrepareGpuAhead();
		Interlocked.Increment(ref liveStages);
	}

	public static ThreeStage FromMark(JsonElement mark, double canvasWidth, double canvasHeight, double devicePixelsPerUnit, bool presents = true)
	{
		string? identifier = mark.Str("id") ?? "st";
		double x;
		double y;
		if (mark.Has("at", out var value) && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() >= 2)
		{
			x = value[0].GetDouble();
			y = value[1].GetDouble();
		}
		else
		{
			x = mark.Double("x").GetValueOrDefault();
			y = mark.Double("y").GetValueOrDefault();
		}
		double width = Math.Max(24.0, mark.Double("w") ?? mark.Double("width") ?? 24.0);
		double height = Math.Max(24.0, mark.Double("h") ?? mark.Double("height") ?? 24.0);
		JsonElement camera = mark.Obj("camera");
		if (camera.ValueKind == JsonValueKind.Object && !camera.EnumerateObject().Any())
		{
			string text = mark.Str("data-camera");
			if (text != null)
			{
				camera = JsonDocument.Parse(text).RootElement.Clone();
			}
		}
		Rect2? card = null;
		if (mark.Has("card", out var value2) && value2.ValueKind == JsonValueKind.Array && value2.GetArrayLength() == 4)
		{
			card = new Rect2(value2[0].GetDouble(), value2[1].GetDouble(), value2[2].GetDouble(), value2[3].GetDouble());
		}
		else
		{
			string text2 = mark.Str("data-card");
			if (text2 != null)
			{
				string[] array = text2.Split(' ');
				if (array != null && array.Length == 4)
				{
					card = new Rect2(double.Parse(array[0], CultureInfo.InvariantCulture), double.Parse(array[1], CultureInfo.InvariantCulture), double.Parse(array[2], CultureInfo.InvariantCulture), double.Parse(array[3], CultureInfo.InvariantCulture));
				}
			}
		}
		return new ThreeStage(ink: mark.Str("ink") ?? mark.Str("data-ink") ?? "#FFFFFF", look: ((mark.Str("look") ?? mark.Str("data-look")) == "studio") ? StageLook.Studio : StageLook.Illustrated, identifier: identifier, rect: new Rect2(x, y, width, height), camera: camera, card: card, canvasWidth: canvasWidth, canvasHeight: canvasHeight, devicePixelsPerUnit: devicePixelsPerUnit, presents: presents);
	}

	public void AddPart(JsonElement op, double start, double duration = 0.65)
	{
		lock (sync)
		{
			double valueOrDefault = simulatedTime.GetValueOrDefault();
			if (!simulatedTime.HasValue)
			{
				valueOrDefault = Now();
				simulatedTime = valueOrDefault;
			}
			Scene.AddPart(op, start, duration, ClickyThreeKernel.Shared.Mesh);
		}
		StartFrames();
	}

	public void RemovePart(string partId)
	{
		lock (sync)
		{
			Scene.RemovePart(partId);
			foreach (StageLabel label in labels)
			{
				if (label.Follow == partId)
				{
					words?.Remove(label.Unit);
				}
			}
			labels.RemoveAll((StageLabel l) => l.Follow == partId);
			lastLayout.Clear();
		}
		StartFrames();
	}

	public void AddMotion(JsonElement op, double start)
	{
		if (op.Has("op", out var value) && value.ValueKind == JsonValueKind.Object)
		{
			op = value;
		}
		lock (sync)
		{
			Scene.AddMotion(op, start);
		}
		StartFrames();
	}

	public void AddTick(string tickId, double start)
	{
		if (!string.IsNullOrEmpty(tickId))
		{
			lock (sync)
			{
				ticks[tickId] = start;
			}
			StartFrames();
		}
	}

	public void RemoveTick(string tickId)
	{
		lock (sync)
		{
			if (!ticks.Remove(tickId))
			{
				return;
			}
			if (ticks.Count == 0)
			{
				Scene.ClearTickPoses();
			}
		}
		StartFrames();
	}

	public void ApplyCamera(JsonElement op, double? time = null)
	{
		if (op.Has("op", out var value) && value.ValueKind == JsonValueKind.Object)
		{
			op = value;
		}
		lock (sync)
		{
			Scene.ApplyCamera(op);
			float? num = op.Number("turn");
			if (num.HasValue)
			{
				float valueOrDefault = num.GetValueOrDefault();
				viewYaw = RestYaw + valueOrDefault * (float)Math.PI / 180f;
				Scene.RestingYaw = viewYaw;
				Scene.SwingToFront(time ?? Now(), viewYaw, 1.4, 0.6, 2.0);
			}
			num = op.Number("spin");
			if (num.HasValue)
			{
				float valueOrDefault2 = num.GetValueOrDefault();
				if (valueOrDefault2 != 0f)
				{
					heldStillSince = null;
					turnRestartedAt = time ?? Now();
					hasBegunRestingSwing = false;
				}
			}
		}
		StartFrames();
	}

	public void HoldStill()
	{
		lock (sync)
		{
			double? num = heldStillSince;
			if (num.HasValue)
			{
				return;
			}
			double num2 = Now();
			heldStillSince = num2;
			if (!userTookControl && Scene.TurnsOnItsOwn)
			{
				Scene.SwingToFront(num2, viewYaw, 1.6, 0.35, 1.0);
			}
		}
		StartFrames();
	}

	public static Task<Mesh3D?> PrefetchShape(string kind, string recipeJson)
	{
		return ClickyThreeKernel.Shared.Build(kind, recipeJson);
	}

	public static string? GpuProfileReport()
	{
		return GpuPassTimer.Current?.Report();
	}

	public static void WarmUp()
	{
		Pipelines.WarmUp();
	}

	public void AddLabelMark(JsonElement mark, double start, double duration)
	{
		Vector3? point = mark.Vec3("point");
		AddLabel(mark.Str("id") ?? "", point, mark.Str("follow"), mark.Str("text") ?? "", mark.Str("side"), mark.Number("size") ?? 19f, start, duration);
	}

	public void AddLabel(string id, Vector3? point, string? follow, string text, string? side, double size, double start, double duration)
	{
		lock (sync)
		{
			if (labels.Exists((StageLabel l) => l.Id == id))
			{
				RemoveLabelLocked(id);
			}
			List<string> list = new List<string>();
			foreach (var (text3, tuple2) in labelAnchors)
			{
				if (text3 == id)
				{
					continue;
				}
				if (follow != null)
				{
					if (tuple2.Item2 == follow)
					{
						list.Add(text3);
					}
				}
				else
				{
					if (tuple2.Item2 != null || !point.HasValue)
					{
						continue;
					}
					Vector3 valueOrDefault = point.GetValueOrDefault();
					var (vector, _) = tuple2;
					if (vector.HasValue)
					{
						Vector3 valueOrDefault2 = vector.GetValueOrDefault();
						if (Vector3.Distance(valueOrDefault, valueOrDefault2) < 0.02f)
						{
							list.Add(text3);
						}
					}
				}
			}
			foreach (string item in list)
			{
				RemoveLabelLocked(item);
			}
			labelAnchors[id] = (point, follow);
			Scene.AddLabel(id, text, point, follow, start);
			float size2 = (float)size;
			string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = null;
			double num = 0.0;
			if (array.Length >= 2)
			{
				for (int i = 1; i < array.Length; i++)
				{
					string text4 = string.Join(' ', array, 0, i);
					string text5 = string.Join(' ', array, i, array.Length - i);
					double num2 = Math.Max(LabelWords.Measure(text4, size2), LabelWords.Measure(text5, size2));
					if (array2 == null || num2 < num)
					{
						array2 = new string[2] { text4, text5 };
						num = num2;
					}
				}
			}
			Slot? slot;
			switch (side)
			{
			case "left":
				slot = Slot.Left;
				break;
			case "right":
				slot = Slot.Right;
				break;
			case "top":
			case "above":
				slot = Slot.Top;
				break;
			case "bottom":
			case "below":
				slot = Slot.Bottom;
				break;
			default:
				slot = null;
				break;
			}
			Slot? requestedSlot = slot;
			labels.Add(new StageLabel
			{
				Id = id,
				Text = text,
				Size = size,
				OneLineWidth = LabelWords.Measure(text, size2),
				OneLine = new string[1] { text },
				TwoLines = array2,
				TwoLinesWidth = num,
				Start = start,
				Duration = duration,
				RequestedSlot = requestedSlot,
				Unit = nextWordUnit++,
				Follow = follow
			});
		}
		StartFrames();
	}

	public void RemoveLabel(string labelId)
	{
		lock (sync)
		{
			RemoveLabelLocked(labelId);
		}
		StartFrames();
	}

	private void RemoveLabelLocked(string labelId)
	{
		Scene.RemoveLabel(labelId);
		foreach (StageLabel label in labels)
		{
			if (label.Id == labelId)
			{
				words?.Remove(label.Unit);
			}
		}
		labels.RemoveAll((StageLabel l) => l.Id == labelId);
		labelAnchors.Remove(labelId);
		lastLayout.Clear();
	}

	public IReadOnlyList<PointD> PenPoints(string partId)
	{
		return PenPointsForPart(partId);
	}

	public IReadOnlyList<PointD> PenPointsForPart(string partId)
	{
		lock (sync)
		{
			Vector3? vector = Scene.PartCenter(partId);
			if (vector.HasValue)
			{
				Vector3 valueOrDefault = vector.GetValueOrDefault();
				(double, double)? tuple = Scene.SettledProjection(valueOrDefault);
				if (tuple.HasValue)
				{
					(double, double) valueOrDefault2 = tuple.GetValueOrDefault();
					return new _003C_003Ez__ReadOnlySingleElementList<PointD>(new PointD(ObjectRect.MinX + valueOrDefault2.Item1, ObjectRect.MinY + valueOrDefault2.Item2));
				}
			}
			return Array.Empty<PointD>();
		}
	}

	public IReadOnlyList<PointD> PenPointsForLabel(string labelId)
	{
		lock (sync)
		{
			StageLabel stageLabel = labels.Find((StageLabel l) => l.Id == labelId);
			Vector3? vector = Scene.LabelPoint(labelId);
			if (vector.HasValue)
			{
				Vector3 valueOrDefault = vector.GetValueOrDefault();
				(double, double)? tuple = Scene.SettledProjection(valueOrDefault);
				if (tuple.HasValue)
				{
					(double, double) valueOrDefault2 = tuple.GetValueOrDefault();
					if (stageLabel != null)
					{
						PointD pointD = new PointD(ObjectRect.MinX + valueOrDefault2.Item1, ObjectRect.MinY + valueOrDefault2.Item2);
						Rect2? settledExtentTarget = Scene.SettledExtentTarget;
						if (!settledExtentTarget.HasValue)
						{
							return new _003C_003Ez__ReadOnlySingleElementList<PointD>(pointD);
						}
						Rect2 rect = settledExtentTarget.GetValueOrDefault().Offset(ObjectRect.MinX, ObjectRect.MinY);
						bool flag = rect.Width >= rect.Height * 1.5;
						double num = Math.Min(stageLabel.OneLineWidth, (stageLabel.TwoLines == null) ? stageLabel.OneLineWidth : stageLabel.TwoLinesWidth);
						Slot? requestedSlot = stageLabel.RequestedSlot;
						Slot slot;
						if (!requestedSlot.HasValue)
						{
							slot = ((!flag || !(pointD.X > rect.MinX + rect.Width * 0.14) || !(pointD.X < rect.MaxX - rect.Width * 0.14)) ? ((!(pointD.X < rect.MidX)) ? Slot.Right : Slot.Left) : ((pointD.Y < rect.MidY) ? Slot.Top : Slot.Bottom));
						}
						else
						{
							Slot valueOrDefault3 = requestedSlot.GetValueOrDefault();
							slot = valueOrDefault3;
						}
						double y = slot switch
						{
							Slot.Top => rect.MinY - 16.0 - stageLabel.Size * 0.5, 
							Slot.Bottom => rect.MaxY + 16.0 + stageLabel.Size * 0.6, 
							_ => pointD.Y, 
						};
						double num2 = slot switch
						{
							Slot.Right => Math.Min(rect.MaxX + 22.0, LabelArea.MaxX - num), 
							Slot.Left => Math.Max(rect.MinX - 22.0 - num, LabelArea.MinX), 
							_ => pointD.X - num / 2.0, 
						};
						return new _003C_003Ez__ReadOnlyArray<PointD>(new PointD[3]
						{
							pointD,
							new PointD(num2, y),
							new PointD(num2 + num, y)
						});
					}
				}
			}
			return Array.Empty<PointD>();
		}
	}

	public void Stop()
	{
		if (!stopping)
		{
			stopping = true;
			wake.Set();
		}
	}

	private int released;

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		disposed = true;
		Stop();
		Thread running = frameThread;
		if (running != null && running.IsAlive)
		{
			ThreadPool.UnsafeQueueUserWorkItem(delegate
			{
				if (running.Join(5000))
				{
					Release();
				}
				else
				{
					Log.Warn("3D", "stage " + Identifier + ": the frame thread was still running 5 s after teardown");
				}
			}, null);
		}
		else
		{
			Release();
		}
	}

	private void Release()
	{
		if (Interlocked.Exchange(ref released, 1) != 0)
		{
			return;
		}
		if (!GpuDevice.TryGetShared(out GpuDevice gpu))
		{
			wake.Dispose();
			Interlocked.Decrement(ref liveStages);
			return;
		}
		lock (sync)
		{
			lock (gpu.ContextLock)
			{
				words?.Dispose();
				words = null;
				d2dTarget?.Dispose();
				d2dTarget = null;
				d2dContext?.Dispose();
				d2dContext = null;
				backBufferView?.Dispose();
				backBufferView = null;
				backBuffer?.Dispose();
				backBuffer = null;
				renderer?.Dispose();
				renderer = null;
				offscreenView?.Dispose();
				offscreen?.Dispose();
				offscreenStaging?.Dispose();
				offscreenBitmap?.Dispose();
				offscreenView = null;
				offscreen = null;
				offscreenStaging = null;
				offscreenBitmap = null;
				gpu.Context.ClearState();
				gpu.Context.Flush();
			}
		}
		wake.Dispose();
		if (Interlocked.Decrement(ref liveStages) == 0)
		{
			gpu.Trim();
		}
	}

	private void PrepareGpuAhead()
	{
		(int, int) drawablePixelSize = DrawablePixelSize;
		int w = drawablePixelSize.Item1;
		int h = drawablePixelSize.Item2;
		bool studio = Look == StageLook.Studio;
		ThreadPool.UnsafeQueueUserWorkItem(delegate
		{
			FigureRenderer figureRenderer = null;
			try
			{
				if (stopping)
				{
					return;
				}
				Pipelines shared = Pipelines.Shared;
				if (shared == null)
				{
					return;
				}
				figureRenderer = new FigureRenderer(shared);
				figureRenderer.PrepareAhead(w, h, studio);
				lock (sync)
				{
					if (renderer == null && !stopping)
					{
						renderer = figureRenderer;
						figureRenderer = null;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn("3D", "stage " + Identifier + ": targets ahead failed: " + ex.Message);
			}
			if (figureRenderer != null)
			{
				lock (GpuDevice.Shared.ContextLock)
				{
					figureRenderer.Dispose();
				}
			}
		}, null);
	}

	private void StartFrameThread()
	{
		presents = true;
		(int, int) drawablePixelSize = DrawablePixelSize;
		targetWidth = drawablePixelSize.Item1;
		targetHeight = drawablePixelSize.Item2;
		frameThread = new Thread(FrameLoop)
		{
			IsBackground = true,
			Name = "3D stage " + Identifier,
			Priority = ThreadPriority.AboveNormal
		};
		frameThread.Start();
	}

	public void SetDevicePixelsPerUnit(double pixelsPerUnit)
	{
		lock (sync)
		{
			if (Math.Abs(pixelsPerUnit - devicePixelsPerUnit) < 1E-06)
			{
				return;
			}
			devicePixelsPerUnit = pixelsPerUnit;
			needsResize = true;
		}
		StartFrames();
	}

	private void ReleaseBackBuffer()
	{
		if ((object)d2dContext != null)
		{
			d2dContext.Target = null;
		}
		d2dTarget?.Dispose();
		d2dTarget = null;
		backBufferView?.Dispose();
		backBufferView = null;
		backBuffer?.Dispose();
		backBuffer = null;
	}

	private void StartFrames()
	{
		if (presents && !stopping && paused)
		{
			wake.Set();
		}
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetCompositionTimingInfo(nint hwnd, ref DwmTimingInfo info);

	[DllImport("dcomp.dll")]
	private static extern uint DCompositionWaitForCompositorClock(uint count, nint handles, uint timeoutInMs);

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();

	private static void WaitForCompositorFrame()
	{
		if (hasCompositorClock)
		{
			try
			{
				DCompositionWaitForCompositorClock(0u, IntPtr.Zero, 100u);
				return;
			}
			catch (EntryPointNotFoundException)
			{
				hasCompositorClock = false;
			}
		}
		if (DwmFlush() < 0)
		{
			Thread.Sleep(8);
		}
	}

	private static double DisplayRefreshRate()
	{
		DwmTimingInfo dwmTimingInfo = default(DwmTimingInfo);
		dwmTimingInfo.cbSize = (uint)Marshal.SizeOf<DwmTimingInfo>();
		DwmTimingInfo info = dwmTimingInfo;
		if (DwmGetCompositionTimingInfo(IntPtr.Zero, ref info) == 0 && info.rateRefreshDenominator != 0)
		{
			return (double)info.rateRefreshNumerator / (double)info.rateRefreshDenominator;
		}
		return 60.0;
	}

	private float WantedFramesPerSecond(double displayRate)
	{
		return (float)Math.Min((IsBuilding || IsHandActive) ? 60f : TurningFramesPerSecond, displayRate);
	}

	private void FrameLoop()
	{
		try
		{
			double num = DisplayRefreshRate();
			double num2 = 0.0;
			double num3 = 0.0;
			while (!stopping)
			{
				if (paused)
				{
					wake.WaitOne();
					if (stopping)
					{
						break;
					}
					paused = false;
					num2 = 0.0;
					num3 = 0.0;
					num = DisplayRefreshRate();
				}
				WaitForCompositorFrame();
				if (stopping)
				{
					break;
				}
				double num4 = (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
				float val;
				lock (sync)
				{
					val = WantedFramesPerSecond(num);
				}
				double num5 = 1.0 / (double)Math.Max(1f, val);
				double num6 = 0.5 / Math.Max(1.0, num);
				if (num3 > 0.0 && num4 < num3 - num6)
				{
					continue;
				}
				num3 = ((num3 > 0.0 && num4 - num3 < num5) ? (num3 + num5) : (num4 + num5));
				Surface?.WaitUntilReady(250);
				if (stopping)
				{
					break;
				}
				long timestamp = Stopwatch.GetTimestamp();
				double num7 = Now();
				bool isBackAfterLongGap = num2 > 0.0 && num7 - num2 > 1.0;
				num2 = num7;
				try
				{
					RenderFrame(num7, isBackAfterLongGap);
				}
				catch (Exception ex)
				{
					Log.Error("3D", "stage " + Identifier + " frame failed", ex);
					paused = true;
					continue;
				}
				cpuFrameMilliseconds[FramesPresented % cpuFrameMilliseconds.Length] = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
				FramesPresented++;
				lock (sync)
				{
					if (!IsBuilding && !IsHandActive && Scene.HasSettledMotion && ticks.Count == 0 && num7 > BuiltTime + 0.5)
					{
						LabelWords? labelWords = words;
						if (labelWords == null || !labelWords.IsWriting(num7))
						{
							paused = true;
						}
					}
				}
			}
		}
		finally
		{
			if (disposed)
			{
				Release();
			}
		}
	}

	private void RenderFrame(double time, bool isBackAfterLongGap)
	{
		RunTicks(time);
		GpuDevice shared = GpuDevice.Shared;
		lock (sync)
		{
			if (isBackAfterLongGap)
			{
				double? num = simulatedTime;
				if (num.HasValue)
				{
					double valueOrDefault = num.GetValueOrDefault();
					if (time - valueOrDefault > 1.0)
					{
						simulatedTime = time - 1.0;
					}
				}
			}
			Pipelines shared2 = Pipelines.Shared;
			if (shared2 == null)
			{
				return;
			}
			lock (shared.ContextLock)
			{
				if (!presents || stopping)
				{
					return;
				}
				if (needsResize || (object)backBuffer == null)
				{
					var (num2, num3) = DrawablePixelSize;
					ReleaseBackBuffer();
					if (needsResize && (num2 != targetWidth || num3 != targetHeight))
					{
						targetWidth = num2;
						targetHeight = num3;
						resizedTo = (num2, num3);
					}
					needsResize = false;
					backBuffer = shared.Device.CreateTexture2D(new Texture2DDescription
					{
						Width = (uint)targetWidth,
						Height = (uint)targetHeight,
						MipLevels = 1u,
						ArraySize = 1u,
						Format = Format.B8G8R8A8_UNorm,
						SampleDescription = new SampleDescription(1u, 0u),
						Usage = ResourceUsage.Default,
						BindFlags = (BindFlags.ShaderResource | BindFlags.RenderTarget)
					});
					backBufferView = shared.Device.CreateRenderTargetView(backBuffer, null);
					if ((object)d2dContext == null)
					{
						d2dContext = shared.D2DDevice.CreateDeviceContext(DeviceContextOptions.None);
					}
					using IDXGISurface surface = backBuffer.QueryInterface<IDXGISurface>();
					d2dTarget = d2dContext.CreateBitmapFromDxgiSurface(surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target | BitmapOptions.CannotDraw));
				}
				if (renderer == null)
				{
					renderer = new FigureRenderer(shared2);
				}
				Encode(time, backBuffer, backBufferView, targetWidth, targetHeight, d2dTarget);
				Surface?.Present(backBuffer, targetWidth, targetHeight);
			}
		}
		(int, int)? tuple2 = resizedTo;
		if (tuple2.HasValue)
		{
			(int, int) valueOrDefault2 = tuple2.GetValueOrDefault();
			resizedTo = null;
			this.SurfaceResized?.Invoke(valueOrDefault2.Item1, valueOrDefault2.Item2);
		}
		UpdateInteractiveRectOutsideLock();
	}

	private void RunTicks(double time)
	{
		TickProvider tickProvider = TickProvider;
		if (tickProvider == null)
		{
			return;
		}
		double frameSeconds;
		lock (sync)
		{
			if (ticks.Count == 0)
			{
				return;
			}
			dueTicks.Clear();
			foreach (var (tickId, num2) in ticks)
			{
				if (time >= num2)
				{
					dueTicks.Add(new TickDue(tickId, time - num2));
				}
			}
			if (dueTicks.Count == 0)
			{
				return;
			}
			dueTicks.Sort(ByTickId);
			frameSeconds = Math.Min(0.1, Math.Max(1.0 / 120.0, time - (lastTickTime ?? (time - 1.0 / 60.0))));
			lastTickTime = time;
		}
		ReadOnlySpan<float> packed;
		try
		{
			packed = tickProvider(Identifier, CollectionsMarshal.AsSpan(dueTicks), frameSeconds);
		}
		catch (Exception ex)
		{
			Log.Warn("3D", "tick provider failed: " + ex.Message);
			return;
		}
		if (packed.IsEmpty)
		{
			return;
		}
		lock (sync)
		{
			Scene.ApplyTickPoses(packed, partPrefix);
		}
	}

	private float SpinScale(double time)
	{
		if (userTookControl)
		{
			return 0f;
		}
		if (KeepsTurning)
		{
			return 1f;
		}
		double? num = heldStillSince;
		if (num.HasValue)
		{
			return 0f;
		}
		if (time - Math.Max(BuiltTime, turnRestartedAt) - 20.0 <= 0.0)
		{
			hasBegunRestingSwing = false;
			return 1f;
		}
		if (!hasBegunRestingSwing)
		{
			hasBegunRestingSwing = true;
			if (Scene.TurnsOnItsOwn)
			{
				Scene.SwingToFront(time, viewYaw, 0.6, 2.5, 6.0);
			}
		}
		return 0f;
	}

	private void AdvanceScene(double time)
	{
		double? num = simulatedTime;
		if (num.HasValue)
		{
			double num2 = num.GetValueOrDefault();
			if (!(time <= num2))
			{
				while (time - num2 > 1.0 / 60.0)
				{
					num2 += 1.0 / 60.0;
					Scene.SpinScale = SpinScale(num2);
					Scene.Update(num2);
				}
				Scene.SpinScale = SpinScale(time);
				Scene.Update(time);
				simulatedTime = time;
				return;
			}
		}
		Scene.SpinScale = SpinScale(time);
		Scene.Update(time);
		simulatedTime = time;
	}

	private void Encode(double time, ID3D11Texture2D target, ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, ID2D1Bitmap1 wordsTarget)
	{
		GpuPassTimer current = GpuPassTimer.Current;
		ID3D11DeviceContext1 context = GpuDevice.Shared.Context;
		current?.BeginFrame(context);
		AdvanceScene(time);
		double scale = (double)targetWidth / OuterRect.Width;
		Viewport viewport = new Viewport((float)((ObjectRect.MinX - OuterRect.MinX) * scale), (float)((ObjectRect.MinY - OuterRect.MinY) * scale), (float)(ObjectRect.Width * scale), (float)(ObjectRect.Height * scale), 0f, 1f);
		FigureRenderer figureRenderer = renderer;
		if (Look == StageLook.Studio)
		{
			InkOutline inkOutline = default(InkOutline);
			inkOutline.Color = inkRGBA;
			inkOutline.Width = (float)(StudioPens.width * scale);
			inkOutline.SeamWidth = (float)(StudioPens.seamWidth * scale);
			inkOutline.SeamStrength = StudioPens.seamStrength;
			inkOutline.SeamShade = ((StudioPens.seamShade > 0f) ? new float?(StudioPens.seamShade) : ((float?)null));
			InkOutline outline = inkOutline;
			figureRenderer.EncodeStudio(Scene, target, targetView, targetWidth, targetHeight, viewport, in outline);
		}
		else
		{
			InkOutline inkOutline = default(InkOutline);
			inkOutline.Color = inkRGBA;
			inkOutline.Width = (float)(1.9 * scale);
			inkOutline.SeamStrength = 1f;
			InkOutline outline2 = inkOutline;
			figureRenderer.EncodeMarker(Scene, target, targetView, targetWidth, targetHeight, viewport, in outline2, DrawsIllustrated);
		}
		if (relayoutAfterTurn && !isDragging && Scene.HasSettledDrag)
		{
			relayoutAfterTurn = false;
			viewExtent = Scene.CurrentExtent();
			foreach (StageLabel label in labels)
			{
				Rect2? writtenBox = label.WrittenBox;
				if (writtenBox.HasValue)
				{
					Slot? requestedSlot = label.RequestedSlot;
					if (!requestedSlot.HasValue)
					{
						label.Slot = null;
					}
				}
			}
		}
		LayOutLabels(time);
		if (PaperArea.HasValue)
		{
			Rect2? writtenBox = Card(time);
			if (writtenBox.HasValue)
			{
				Rect2 valueOrDefault = writtenBox.GetValueOrDefault();
				Vector2 halfSize = new Vector2((float)(valueOrDefault.Width * scale / 2.0), (float)(valueOrDefault.Height * scale / 2.0));
				double num = Math.Min(120.0, Math.Max(60.0, valueOrDefault.Width * 0.2));
				sheets[0] = new FigureRenderer.Sheet
				{
					Center = ToPixels(valueOrDefault.MidX, valueOrDefault.MinY + 1.0),
					HalfSize = new Vector2((float)(num * scale / 2.0), (float)(10.0 * scale)),
					Angle = -(float)Math.PI / 72f,
					Color = TapeColor
				};
				sheets[1] = new FigureRenderer.Sheet
				{
					Center = ToPixels(valueOrDefault.MidX, valueOrDefault.MidY),
					HalfSize = halfSize,
					Color = PaperColor
				};
				sheets[2] = new FigureRenderer.Sheet
				{
					Center = ToPixels(valueOrDefault.MidX + 3.0, valueOrDefault.MidY + 5.0),
					HalfSize = halfSize,
					Color = PaperShadow
				};
				figureRenderer.DrawSheetsUnder(targetView, targetWidth, targetHeight, sheets);
				current?.Mark(context, "paper");
			}
		}
		if (words == null)
		{
			words = new LabelWords();
		}
		int num2 = 0;
		int num3 = 0;
		float num4 = (float)(1.0 - Math.Pow(0.78, Math.Min(240.0, Math.Max(0.0, (time - (lastBoxEaseTime ?? time)) * 60.0))));
		lastBoxEaseTime = time;
		labelsAreGliding = false;
		drawnLayout.Clear();
		foreach (LabelLayout item in layout)
		{
			StageLabel stageLabel = labels[item.Index];
			if (time < stageLabel.Start)
			{
				continue;
			}
			Rect2? writtenBox = stageLabel.ShownBox;
			Rect2 rect;
			if (writtenBox.HasValue)
			{
				Rect2 valueOrDefault2 = writtenBox.GetValueOrDefault();
				writtenBox = stageLabel.WrittenBox;
				if (writtenBox.HasValue)
				{
					rect = new Rect2(valueOrDefault2.MinX + (item.Box.MinX - valueOrDefault2.MinX) * (double)num4, valueOrDefault2.MinY + (item.Box.MinY - valueOrDefault2.MinY) * (double)num4, item.Box.Width, item.Box.Height);
					goto IL_05e3;
				}
			}
			rect = item.Box;
			goto IL_05e3;
			IL_05e3:
			stageLabel.ShownBox = rect;
			if (Math.Abs(rect.MinX - item.Box.MinX) > 0.3 || Math.Abs(rect.MinY - item.Box.MinY) > 0.3)
			{
				labelsAreGliding = true;
			}
			Slot? requestedSlot = stageLabel.Slot;
			if (!requestedSlot.HasValue)
			{
				writtenBox = stageLabel.WrittenBox;
				if (writtenBox.HasValue)
				{
					stageLabel.Slot = item.Slot;
				}
			}
			drawnLayout.Add(item with
			{
				Box = rect
			});
			float num5 = (float)Math.Clamp((time - stageLabel.Start) / 0.25, 0.0, 1.0);
			var (x2, y2) = item.Slot switch
			{
				Slot.Right => (rect.MinX - 5.0, rect.MidY), 
				Slot.Left => (rect.MaxX + 5.0, rect.MidY), 
				Slot.Top => (rect.MidX, rect.MaxY + 3.0), 
				_ => (rect.MidX, rect.MinY - 3.0), 
			};
			if (num2 + 2 > capsules.Length)
			{
				Array.Resize(ref capsules, capsules.Length * 2);
			}
			capsules[num2++] = new FigureRenderer.Capsule
			{
				A = ToPixels(item.PointX, item.PointY),
				B = ToPixels(x2, y2),
				Radius = (float)(1.1 * scale),
				HaloWidth = (float)((LabelsWearFlags ? 2.2 : 1.4) * scale),
				Color = inkRGBA * num5,
				Halo = haloRGBA * num5
			};
			capsules[num2++] = new FigureRenderer.Capsule
			{
				A = ToPixels(item.PointX, item.PointY),
				B = ToPixels(item.PointX, item.PointY),
				Radius = (float)(3.6 * scale),
				HaloWidth = (float)((LabelsWearFlags ? 2.4 : 1.6) * scale),
				Color = inkRGBA * num5,
				Halo = haloRGBA * num5
			};
			if (LabelsWearFlags)
			{
				if (num3 + 4 > flags.Length)
				{
					Array.Resize(ref flags, flags.Length * 2);
				}
				FlagSheets(stageLabel.Id, rect, item.Slot, num5, scale, flags.AsSpan(num3, 4));
				num3 += 4;
			}
			writtenBox = stageLabel.WrittenBox;
			if (writtenBox.HasValue)
			{
				Rect2 valueOrDefault3 = writtenBox.GetValueOrDefault();
				double num6 = devicePixelsPerUnit;
				words.SetOffset(stageLabel.Unit, Math.Round((rect.MinX - valueOrDefault3.MinX) * num6) / num6, Math.Round((rect.MinY - valueOrDefault3.MinY) * num6) / num6);
				continue;
			}
			double originX = ((item.Slot == Slot.Right) ? rect.MinX : ((item.Slot == Slot.Left) ? rect.MaxX : rect.MidX));
			double originY = rect.MinY + item.Fitting.Size * 0.895;
			WordsAnchor anchor = ((item.Slot != Slot.Right) ? ((item.Slot == Slot.Left) ? WordsAnchor.End : WordsAnchor.Middle) : WordsAnchor.Start);
			words.Write(stageLabel.Unit, item.Fitting.Lines, (float)item.Fitting.Size, anchor, originX, originY, item.Fitting.LineAdvance, stageLabel.Start, stageLabel.Duration, wordsInkRGBA, wordsGlow);
			stageLabel.WrittenBox = rect;
			stageLabel.Slot = item.Slot;
			stageLabel.Fitting = item.Fitting;
		}
		lastLayout.Clear();
		lastLayout.AddRange(drawnLayout);
		figureRenderer.DrawCapsules(targetView, targetWidth, targetHeight, capsules.AsSpan(0, num2));
		if (num3 > 0)
		{
			figureRenderer.DrawSheetsOver(targetView, targetWidth, targetHeight, flags.AsSpan(0, num3));
		}
		current?.Mark(context, "dots and leaders");
		if (!words.IsEmpty)
		{
			ID2D1DeviceContext iD2D1DeviceContext = d2dContext ?? (d2dContext = GpuDevice.Shared.D2DDevice.CreateDeviceContext(DeviceContextOptions.None));
			iD2D1DeviceContext.Target = wordsTarget;
			iD2D1DeviceContext.BeginDraw();
			words.Draw(iD2D1DeviceContext, time, Matrix3x2.CreateTranslation((float)(0.0 - OuterRect.MinX), (float)(0.0 - OuterRect.MinY)) * Matrix3x2.CreateScale((float)scale));
			iD2D1DeviceContext.EndDraw();
			current?.Mark(context, "words");
		}
		current?.EndFrame(context);
		UpdateInteractiveRect(time);
		Vector2 ToPixels(double x, double y)
		{
			return new Vector2((float)((x - OuterRect.MinX) * scale), (float)((y - OuterRect.MinY) * scale));
		}
	}

	private void FlagSheets(string labelId, Rect2 box, Slot slot, float appear, double scale, Span<FigureRenderer.Sheet> into)
	{
		int num = 0;
		foreach (Rune item3 in labelId.EnumerateRunes())
		{
			num = (num * 31 + item3.Value) & 0xFFFF;
		}
		(Vector4 Strip, Vector4 Tab) tuple = FlagColors[num % FlagColors.Length];
		Vector4 item = tuple.Strip;
		Vector4 item2 = tuple.Tab;
		float angle = (float)(num / 4 % 5 - 2) * 0.8f * (float)Math.PI / 180f;
		bool flag = slot == Slot.Left;
		Rect2 rect = new Rect2(box.MinX - 7.0 - (flag ? 0.0 : 6.0), box.MinY - 4.0, box.Width + 14.0 + 6.0, box.Height + 8.0);
		float num2 = 0.86f + 0.14f * appear;
		Vector2 vector = new Vector2((float)(rect.Width * scale / 2.0), (float)(rect.Height * scale / 2.0)) * num2;
		Vector2 vector2 = new Vector2((float)((rect.MidX - OuterRect.MinX) * scale), (float)((rect.MidY - OuterRect.MinY) * scale));
		float corner = (float)(2.0 * scale);
		float num3 = (float)scale;
		Vector2 halfSize2 = new Vector2((float)(6.0 * scale / 2.0), vector.Y);
		float num4 = (vector.X - halfSize2.X) * (float)(flag ? 1 : (-1));
		Vector2 at2 = vector2 + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * num4;
		into[0] = Sheet(vector2 + new Vector2(1.6f, 3.2f) * num3, vector + new Vector2(0.8f * num3), FlagShadow * 0.12f);
		into[1] = Sheet(vector2 + new Vector2(0.8f, 1.6f) * num3, vector, FlagShadow * 0.22f);
		into[2] = Sheet(vector2, vector, item);
		into[3] = Sheet(at2, halfSize2, item2);
		FigureRenderer.Sheet Sheet(Vector2 at, Vector2 halfSize, Vector4 color)
		{
			FigureRenderer.Sheet result = default(FigureRenderer.Sheet);
			result.Center = at;
			result.HalfSize = halfSize;
			result.Angle = angle;
			result.CornerRadius = corner;
			result.Color = color * appear;
			return result;
		}
	}

	private Rect2? ObjectExtentOnCanvas()
	{
		Rect2? rect = viewExtent;
		if (rect.HasValue)
		{
			return rect.GetValueOrDefault().Offset(ObjectRect.MinX, ObjectRect.MinY);
		}
		rect = Scene.SettledExtent;
		if (rect.HasValue)
		{
			return rect.GetValueOrDefault().Offset(ObjectRect.MinX, ObjectRect.MinY);
		}
		Scene.ProjectedSamplePoints(samplePoints);
		if (samplePoints.Count == 0)
		{
			return null;
		}
		Rect2 rect2 = new Rect2(samplePoints[0].X, samplePoints[0].Y, 0.0, 0.0);
		foreach (Vector2 samplePoint in samplePoints)
		{
			rect2 = rect2.Union(samplePoint.X, samplePoint.Y);
		}
		return rect2.Offset(ObjectRect.MinX, ObjectRect.MinY);
	}

	private bool TryFit(StageLabel label, Slot slot, double rowWidth, out Fitting fitting)
	{
		Fitting? fitting2 = label.Fitting;
		if (fitting2.HasValue)
		{
			Fitting valueOrDefault = fitting2.GetValueOrDefault();
			fitting = valueOrDefault;
			return true;
		}
		bool flag = (uint)slot <= 1u;
		double num = (flag ? room[(int)slot] : rowWidth);
		fitting = default(Fitting);
		flag = (uint)(slot - 2) <= 1u;
		if (flag && room[(int)slot] < label.Size * 1.15)
		{
			return false;
		}
		if (label.OneLineWidth <= num)
		{
			fitting = new Fitting
			{
				Lines = label.OneLine,
				Size = label.Size,
				Width = label.OneLineWidth
			};
			return true;
		}
		string[] twoLines = label.TwoLines;
		flag = twoLines != null && label.TwoLinesWidth <= num;
		if (flag)
		{
			bool flag2 = (uint)slot <= 1u;
			flag = flag2 || room[(int)slot] >= label.Size * 2.3;
		}
		if (flag)
		{
			fitting = new Fitting
			{
				Lines = twoLines,
				Size = label.Size,
				Width = label.TwoLinesWidth
			};
			return true;
		}
		string[] lines = label.TwoLines ?? label.OneLine;
		double num2 = ((label.TwoLines == null) ? label.OneLineWidth : label.TwoLinesWidth);
		double num3 = label.Size * num / num2;
		if (num3 < 14.0)
		{
			return false;
		}
		fitting = new Fitting
		{
			Lines = lines,
			Size = num3,
			Width = num2 * num3 / label.Size
		};
		return true;
	}

	private static Slot Opposite(Slot s)
	{
		return s switch
		{
			Slot.Left => Slot.Right, 
			Slot.Right => Slot.Left, 
			Slot.Top => Slot.Bottom, 
			_ => Slot.Top, 
		};
	}

	private void LayOutLabels(double time)
	{
		layout.Clear();
		if (labels.Count == 0)
		{
			return;
		}
		Rect2? rect = ObjectExtentOnCanvas();
		if (!rect.HasValue)
		{
			return;
		}
		Rect2 extent = rect.GetValueOrDefault();
		Scene.LabelPlacements(time, placements);
		bool wide = extent.Width >= extent.Height * 1.5;
		room[1] = LabelArea.MaxX - (extent.MaxX + 22.0);
		room[0] = extent.MinX - 22.0 - LabelArea.MinX;
		room[2] = extent.MinY - 16.0 - LabelArea.MinY;
		room[3] = LabelArea.MaxY - (extent.MaxY + 16.0);
		double rowWidth = Math.Max(120.0, Math.Min(240.0, LabelArea.Width / 3.0));
		items.Clear();
		Span<Slot> span = stackalloc Slot[5];
		for (int i = 0; i < labels.Count; i++)
		{
			StageLabel stageLabel = labels[i];
			int num = -1;
			for (int j = 0; j < placements.Count; j++)
			{
				if (placements[j].Id == stageLabel.Id)
				{
					num = j;
					break;
				}
			}
			if (num < 0)
			{
				continue;
			}
			LabelPlacement3D labelPlacement3D = placements[num];
			double num2 = ObjectRect.MinX + labelPlacement3D.AnchorX;
			double num3 = ObjectRect.MinY + labelPlacement3D.AnchorY;
			double dotX = ObjectRect.MinX + labelPlacement3D.PointX;
			double dotY = ObjectRect.MinY + labelPlacement3D.PointY;
			Slot? slot2 = stageLabel.Slot;
			if (slot2.HasValue)
			{
				Slot valueOrDefault = slot2.GetValueOrDefault();
				Fitting? fitting = stageLabel.Fitting;
				if (fitting.HasValue)
				{
					Fitting valueOrDefault2 = fitting.GetValueOrDefault();
					items.Add(new LayoutItem
					{
						Index = i,
						PointX = num2,
						PointY = num3,
						DotX = dotX,
						DotY = dotY,
						Slot = valueOrDefault,
						Fitting = valueOrDefault2,
						Movable = false
					});
					continue;
				}
			}
			Slot slot3 = stageLabel.RequestedSlot ?? PreferredSlot(num2, num3);
			Slot slot4 = ((!(num2 < extent.MidX)) ? Slot.Right : Slot.Left);
			span[0] = slot3;
			span[1] = Opposite(slot3);
			span[2] = slot4;
			span[3] = Opposite(slot4);
			span[4] = ((num3 < extent.MidY) ? Slot.Top : Slot.Bottom);
			Slot slot5 = Slot.Left;
			Fitting fitting2 = default(Fitting);
			bool flag = false;
			Span<Slot> span2 = span;
			for (int k = 0; k < span2.Length; k++)
			{
				Slot slot6 = span2[k];
				if (TryFit(stageLabel, slot6, rowWidth, out var fitting3))
				{
					slot5 = slot6;
					fitting2 = fitting3;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				slot5 = ((room[1] >= room[0]) ? Slot.Right : Slot.Left);
				double num4 = ((stageLabel.TwoLines == null) ? stageLabel.OneLineWidth : stageLabel.TwoLinesWidth);
				Fitting fitting4 = default(Fitting);
				fitting4.Lines = stageLabel.TwoLines ?? stageLabel.OneLine;
				fitting4.Size = 14.0;
				fitting4.Width = num4 * 14.0 / stageLabel.Size;
				fitting2 = fitting4;
			}
			List<LayoutItem> list = items;
			LayoutItem item = new LayoutItem
			{
				Index = i,
				PointX = num2,
				PointY = num3,
				DotX = dotX,
				DotY = dotY,
				Slot = slot5,
				Fitting = fitting2
			};
			slot2 = stageLabel.RequestedSlot;
			item.Movable = !slot2.HasValue;
			list.Add(item);
		}
		if (wide)
		{
			Balance(Slot.Top, Slot.Bottom, 3, extent.Height * 0.25, byY: true);
		}
		else
		{
			Balance(Slot.Left, Slot.Right, 2, extent.Width * 0.2, byY: false);
		}
		Slot[] crowdOrder = CrowdOrder;
		Span<Slot> span3 = stackalloc Slot[3];
		Slot[] array;
		for (int l = 0; l < items.Count; l++)
		{
			Slot slot7 = Slot.Left;
			bool flag2 = false;
			array = crowdOrder;
			foreach (Slot slot8 in array)
			{
				if (!Fits(slot8))
				{
					slot7 = slot8;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				break;
			}
			bool flag3 = (uint)(slot7 - 2) <= 1u;
			bool flag4 = flag3;
			candidates.Clear();
			for (int m = 0; m < items.Count; m++)
			{
				if (items[m].Slot == slot7 && items[m].Movable)
				{
					candidates.Add(m);
				}
			}
			for (int n = 1; n < candidates.Count; n++)
			{
				int num5 = candidates[n];
				int num6 = n - 1;
				double num7 = (flag4 ? Math.Abs(items[num5].PointX - extent.MidX) : Math.Abs(items[num5].PointY - extent.MidY));
				while (num6 >= 0 && (flag4 ? Math.Abs(items[candidates[num6]].PointX - extent.MidX) : Math.Abs(items[candidates[num6]].PointY - extent.MidY)) < num7)
				{
					candidates[num6 + 1] = candidates[num6];
					num6--;
				}
				candidates[num6 + 1] = num5;
			}
			bool flag5 = false;
			foreach (int candidate in candidates)
			{
				LayoutItem layoutItem = items[candidate];
				if (flag4)
				{
					span3[0] = ((slot7 == Slot.Top) ? Slot.Bottom : Slot.Top);
					span3[1] = ((!(layoutItem.PointX < extent.MidX)) ? Slot.Right : Slot.Left);
					span3[2] = ((layoutItem.PointX < extent.MidX) ? Slot.Right : Slot.Left);
				}
				else
				{
					span3[0] = ((slot7 == Slot.Left) ? Slot.Right : Slot.Left);
					span3[1] = ((layoutItem.PointY < extent.MidY) ? Slot.Top : Slot.Bottom);
					span3[2] = ((layoutItem.PointY < extent.MidY) ? Slot.Bottom : Slot.Top);
				}
				Span<Slot> span2 = span3;
				for (int k = 0; k < span2.Length; k++)
				{
					Slot slot9 = span2[k];
					if (TryFit(labels[layoutItem.Index], slot9, rowWidth, out var fitting5))
					{
						LayoutItem layoutItem2 = items[candidate];
						LayoutItem value = layoutItem2;
						value.Slot = slot9;
						value.Fitting = fitting5;
						items[candidate] = value;
						if (Fits(slot9))
						{
							flag5 = true;
							break;
						}
						items[candidate] = layoutItem2;
					}
				}
				if (flag5)
				{
					break;
				}
			}
			if (!flag5)
			{
				break;
			}
		}
		array = crowdOrder;
		foreach (Slot slot10 in array)
		{
			if (Fits(slot10))
			{
				continue;
			}
			bool flag3 = (uint)(slot10 - 2) <= 1u;
			bool flag6 = flag3;
			int num8 = 0;
			double num9 = 0.0;
			double num10 = 0.0;
			foreach (LayoutItem item2 in items)
			{
				if (item2.Slot == slot10)
				{
					num8++;
					double num11 = (flag6 ? item2.Fitting.Width : item2.Fitting.Height);
					if (item2.Movable)
					{
						num10 += num11;
					}
					else
					{
						num9 += num11;
					}
				}
			}
			double num12 = (double)(num8 - 1) * (flag6 ? 14.0 : 6.0);
			if (num10 <= 0.0)
			{
				continue;
			}
			double num13 = Math.Max(0.4, Math.Min(1.0, (Capacity(slot10) - num12 - num9) / num10));
			for (int num14 = 0; num14 < items.Count; num14++)
			{
				LayoutItem value2 = items[num14];
				if (value2.Slot == slot10 && value2.Movable)
				{
					value2.Fitting = new Fitting
					{
						Lines = value2.Fitting.Lines,
						Size = value2.Fitting.Size * num13,
						Width = value2.Fitting.Width * num13
					};
					items[num14] = value2;
				}
			}
		}
		if (boxes.Length < items.Count)
		{
			boxes = new Rect2[items.Count * 2];
			hasBox = new bool[items.Count * 2];
		}
		Array.Clear(hasBox, 0, items.Count);
		array = crowdOrder;
		foreach (Slot slot11 in array)
		{
			bool flag3 = (uint)(slot11 - 2) <= 1u;
			bool flag7 = flag3;
			lineMembers.Clear();
			for (int num15 = 0; num15 < items.Count; num15++)
			{
				if (items[num15].Slot == slot11)
				{
					lineMembers.Add(num15);
				}
			}
			if (lineMembers.Count == 0)
			{
				continue;
			}
			for (int num16 = 1; num16 < lineMembers.Count; num16++)
			{
				int num17 = lineMembers[num16];
				int num18 = num16 - 1;
				double num19 = (flag7 ? items[num17].PointX : items[num17].PointY);
				while (num18 >= 0 && (flag7 ? items[lineMembers[num18]].PointX : items[lineMembers[num18]].PointY) > num19)
				{
					lineMembers[num18 + 1] = lineMembers[num18];
					num18--;
				}
				lineMembers[num18 + 1] = num17;
			}
			int count = lineMembers.Count;
			EnsureSpread(count);
			for (int num20 = 0; num20 < count; num20++)
			{
				LayoutItem layoutItem3 = items[lineMembers[num20]];
				double num21 = (flag7 ? layoutItem3.Fitting.Width : layoutItem3.Fitting.Height);
				offsets[num20] = num21;
				spreadPositions[num20] = (flag7 ? layoutItem3.PointX : layoutItem3.PointY) - num21 / 2.0;
			}
			Spread(count, flag7 ? 14 : 6, flag7 ? LabelArea.MinX : LabelArea.MinY, flag7 ? LabelArea.MaxX : LabelArea.MaxY);
			for (int num22 = 0; num22 < count; num22++)
			{
				int num23 = lineMembers[num22];
				LayoutItem layoutItem4 = items[num23];
				if (flag7)
				{
					double height = layoutItem4.Fitting.Height;
					double y2 = ((slot11 == Slot.Top) ? Math.Max(extent.MinY - 16.0 - height, LabelArea.MinY) : Math.Min(extent.MaxY + 16.0, LabelArea.MaxY - height));
					boxes[num23] = new Rect2(spreadPositions[num22], y2, layoutItem4.Fitting.Width, height);
				}
				else
				{
					double width = layoutItem4.Fitting.Width;
					double x2 = ((slot11 == Slot.Right) ? Math.Min(extent.MaxX + 22.0, LabelArea.MaxX - width) : Math.Max(extent.MinX - 22.0 - width, LabelArea.MinX));
					boxes[num23] = new Rect2(x2, spreadPositions[num22], width, layoutItem4.Fitting.Height);
				}
				hasBox[num23] = true;
			}
		}
		for (int num24 = 0; num24 < items.Count; num24++)
		{
			if (hasBox[num24])
			{
				layout.Add(new LabelLayout(items[num24].Index, items[num24].Slot, items[num24].Fitting, boxes[num24], items[num24].DotX, items[num24].DotY));
			}
		}
		void Balance(Slot a, Slot b, int limit, double nearMiddle, bool byY)
		{
			for (int num26 = 0; num26 < items.Count; num26++)
			{
				int num27 = 0;
				int num28 = 0;
				foreach (LayoutItem item3 in items)
				{
					if (item3.Slot == a)
					{
						num27++;
					}
					else if (item3.Slot == b)
					{
						num28++;
					}
				}
				if (Math.Abs(num27 - num28) < limit)
				{
					break;
				}
				Slot slot13;
				Slot slot14;
				if (num27 <= num28)
				{
					Slot slot12 = a;
					slot13 = slot12;
					slot14 = b;
				}
				else
				{
					Slot slot12 = b;
					slot13 = slot12;
					slot14 = a;
				}
				int num29 = -1;
				double num30 = double.PositiveInfinity;
				Fitting fitting6 = default(Fitting);
				for (int num31 = 0; num31 < items.Count; num31++)
				{
					LayoutItem layoutItem5 = items[num31];
					if (layoutItem5.Slot == slot14 && layoutItem5.Movable)
					{
						double num32 = (byY ? Math.Abs(layoutItem5.PointY - extent.MidY) : Math.Abs(layoutItem5.PointX - extent.MidX));
						if (!(num32 >= nearMiddle) && !(num32 >= num30) && TryFit(labels[layoutItem5.Index], slot13, rowWidth, out var fitting7))
						{
							num29 = num31;
							num30 = num32;
							fitting6 = fitting7;
						}
					}
				}
				if (num29 < 0)
				{
					break;
				}
				LayoutItem value3 = items[num29];
				value3.Slot = slot13;
				value3.Fitting = fitting6;
				items[num29] = value3;
			}
		}
		double Capacity(Slot slot)
		{
			if ((uint)(slot - 2) > 1u)
			{
				return LabelArea.Height;
			}
			return LabelArea.Width;
		}
		bool Fits(Slot slot)
		{
			return Length(slot) <= Capacity(slot) + 0.5;
		}
		double Length(Slot slot)
		{
			bool flag8 = (uint)(slot - 2) <= 1u;
			bool flag9 = flag8;
			double num33 = 0.0;
			int num34 = 0;
			foreach (LayoutItem item4 in items)
			{
				if (item4.Slot == slot)
				{
					num33 += (flag9 ? item4.Fitting.Width : item4.Fitting.Height);
					num34++;
				}
			}
			if (num34 != 0)
			{
				return num33 + (double)(num34 - 1) * (flag9 ? 14.0 : 6.0);
			}
			return 0.0;
		}
		Slot PreferredSlot(double x, double y)
		{
			if (wide)
			{
				double num25 = extent.Width * 0.14;
				if (x < extent.MinX + num25)
				{
					return Slot.Left;
				}
				if (x > extent.MaxX - num25)
				{
					return Slot.Right;
				}
				if (!(y < extent.MidY))
				{
					return Slot.Bottom;
				}
				return Slot.Top;
			}
			if (!(x < extent.MidX))
			{
				return Slot.Right;
			}
			return Slot.Left;
		}
	}

	private void EnsureSpread(int n)
	{
		if (spreadPositions.Length < n)
		{
			int num = n * 2;
			spreadPositions = new double[num];
			clusterFirst = new int[num];
			clusterCount = new int[num];
			clusterLength = new double[num];
			clusterStart = new double[num];
			offsets = new double[num];
		}
	}

	private void Spread(int n, double gap, double low, double high)
	{
		Span<double> w2 = stackalloc double[n];
		Span<double> l2 = stackalloc double[n];
		for (int i = 0; i < n; i++)
		{
			w2[i] = spreadPositions[i];
			l2[i] = offsets[i];
		}
		int num = 0;
		for (int j = 0; j < n; j++)
		{
			clusterFirst[num] = j;
			clusterCount[num] = 1;
			clusterLength[num] = l2[j];
			clusterStart[num] = BestStart(num, w2, l2);
			num++;
			while (num >= 2)
			{
				int num2 = num - 2;
				int num3 = num - 1;
				if (!(clusterStart[num2] + clusterLength[num2] + gap > clusterStart[num3]))
				{
					break;
				}
				clusterCount[num2] += clusterCount[num3];
				clusterLength[num2] = clusterLength[num2] + gap + clusterLength[num3];
				num--;
				clusterStart[num2] = BestStart(num2, w2, l2);
			}
		}
		for (int k = 0; k < num; k++)
		{
			double num4 = 0.0;
			for (int m = 0; m < clusterCount[k]; m++)
			{
				int num5 = clusterFirst[k] + m;
				spreadPositions[num5] = clusterStart[k] + num4;
				num4 += l2[num5] + gap;
			}
		}
		double BestStart(int c, Span<double> w, Span<double> l)
		{
			double num6 = 0.0;
			double num7 = 0.0;
			for (int num8 = 0; num8 < clusterCount[c]; num8++)
			{
				int index = clusterFirst[c] + num8;
				num6 += w[index] - num7;
				num7 += l[index] + gap;
			}
			return Math.Min(Math.Max(num6 / (double)clusterCount[c], low), Math.Max(low, high - clusterLength[c]));
		}
	}

	private Rect2? Card(double time)
	{
		Rect2? paperArea = PaperArea;
		if (paperArea.HasValue)
		{
			Rect2 valueOrDefault = paperArea.GetValueOrDefault();
			Rect2? rect = ObjectExtentOnCanvas();
			foreach (LabelLayout item in layout)
			{
				rect = rect?.Union(item.Box) ?? item.Box;
			}
			if (!rect.HasValue)
			{
				return null;
			}
			Rect2 value = rect.GetValueOrDefault().Inset(-18.0, -20.0).Intersect(valueOrDefault);
			if (value.IsNull)
			{
				return null;
			}
			paperArea = cardWanted;
			if (paperArea.HasValue)
			{
				Rect2 valueOrDefault2 = paperArea.GetValueOrDefault();
				if (cardWantedRevision == Scene.FramingRevision)
				{
					value = value.Union(valueOrDefault2);
				}
			}
			cardWanted = value;
			cardWantedRevision = Scene.FramingRevision;
			paperArea = cardShown;
			if (paperArea.HasValue)
			{
				Rect2 valueOrDefault3 = paperArea.GetValueOrDefault();
				double? num = cardShownTime;
				if (num.HasValue)
				{
					double valueOrDefault4 = num.GetValueOrDefault();
					if (!(time <= valueOrDefault4))
					{
						double num2 = 1.0 - Math.Pow(0.84, Math.Min(240.0, (time - valueOrDefault4) * 60.0));
						Rect2 value2 = new Rect2(valueOrDefault3.MinX + (value.MinX - valueOrDefault3.MinX) * num2, valueOrDefault3.MinY + (value.MinY - valueOrDefault3.MinY) * num2, valueOrDefault3.Width + (value.Width - valueOrDefault3.Width) * num2, valueOrDefault3.Height + (value.Height - valueOrDefault3.Height) * num2);
						cardShown = value2;
						cardShownTime = time;
						return value2;
					}
				}
			}
			cardShown = value;
			cardShownTime = time;
			return value;
		}
		return null;
	}

	private void PrepareOffscreen(int w, int h, GpuDevice gpu)
	{
		if ((object)offscreen != null && offscreen.Description.Width == w && offscreen.Description.Height == h)
		{
			return;
		}
		offscreenView?.Dispose();
		offscreen?.Dispose();
		offscreenStaging?.Dispose();
		offscreenBitmap?.Dispose();
		offscreen = gpu.Device.CreateTexture2D(new Texture2DDescription
		{
			Width = (uint)w,
			Height = (uint)h,
			MipLevels = 1u,
			ArraySize = 1u,
			Format = Format.B8G8R8A8_UNorm,
			SampleDescription = new SampleDescription(1u, 0u),
			Usage = ResourceUsage.Default,
			BindFlags = (BindFlags.ShaderResource | BindFlags.RenderTarget)
		});
		offscreenView = gpu.Device.CreateRenderTargetView(offscreen, null);
		offscreenStaging = gpu.Device.CreateTexture2D(new Texture2DDescription
		{
			Width = (uint)w,
			Height = (uint)h,
			MipLevels = 1u,
			ArraySize = 1u,
			Format = Format.B8G8R8A8_UNorm,
			SampleDescription = new SampleDescription(1u, 0u),
			Usage = ResourceUsage.Staging,
			CPUAccessFlags = CpuAccessFlags.Read
		});
		if ((object)d2dContext == null)
		{
			d2dContext = gpu.D2DDevice.CreateDeviceContext(DeviceContextOptions.None);
		}
		using IDXGISurface surface = offscreen.QueryInterface<IDXGISurface>();
		offscreenBitmap = d2dContext.CreateBitmapFromDxgiSurface(surface, new BitmapProperties1(new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied), 96f, 96f, BitmapOptions.Target | BitmapOptions.CannotDraw));
	}

	public unsafe byte[] Snapshot(double time, out int width, out int height)
	{
		RunTicks(time);
		GpuDevice shared = GpuDevice.Shared;
		lock (sync)
		{
			(int, int) drawablePixelSize = DrawablePixelSize;
			int item = drawablePixelSize.Item1;
			int item2 = drawablePixelSize.Item2;
			width = item;
			height = item2;
			Pipelines pipelines = Pipelines.Shared ?? throw new InvalidOperationException("3D pipelines unavailable: " + Pipelines.LastError);
			lock (shared.ContextLock)
			{
				if (stopping)
				{
					throw new ObjectDisposedException("3D stage " + Identifier);
				}
				PrepareOffscreen(item, item2, shared);
				if (renderer == null)
				{
					renderer = new FigureRenderer(pipelines);
				}
				double valueOrDefault = simulatedTime.GetValueOrDefault();
				if (!simulatedTime.HasValue)
				{
					valueOrDefault = time;
					simulatedTime = valueOrDefault;
				}
				Encode(time, offscreen, offscreenView, item, item2, offscreenBitmap);
				shared.Context.CopyResource(offscreenStaging, offscreen);
				MappedSubresource mappedSubresource = shared.Context.Map(offscreenStaging, 0u);
				byte[] array = new byte[item * item2 * 4];
				for (int i = 0; i < item2; i++)
				{
					new ReadOnlySpan<byte>((void*)(mappedSubresource.DataPointer + (nint)(i * mappedSubresource.RowPitch)), item * 4).CopyTo(array.AsSpan(i * item * 4));
				}
				shared.Context.Unmap(offscreenStaging, 0u);
				return array;
			}
		}
	}

	public (double gpuMedian, double gpuMean, double cpuMedian, double wall, long allocatedBytes, double gpuP10) BenchBurst(double time, int count, double step)
	{
		GpuDevice shared = GpuDevice.Shared;
		ID3D11Device1 device = shared.Device;
		ID3D11DeviceContext1 context = shared.Context;
		double[] array = new double[count];
		double[] array2 = new double[count];
		ID3D11Query[] array3 = new ID3D11Query[count];
		ID3D11Query[] array4 = new ID3D11Query[count];
		ID3D11Query[] array5 = new ID3D11Query[count];
		for (int i = 0; i < count; i++)
		{
			array3[i] = device.CreateQuery(new QueryDescription(QueryType.TimestampDisjoint));
			array4[i] = device.CreateQuery(new QueryDescription(QueryType.Timestamp));
			array5[i] = device.CreateQuery(new QueryDescription(QueryType.Timestamp));
		}
		var (w, h) = DrawablePixelSize;
		lock (shared.ContextLock)
		{
			PrepareOffscreen(w, h, shared);
			if (renderer == null)
			{
				renderer = new FigureRenderer(Pipelines.Shared);
			}
		}
		RunTicks(time);
		lock (sync)
		{
			lock (shared.ContextLock)
			{
				Encode(time, offscreen, offscreenView, w, h, offscreenBitmap);
			}
		}
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long timestamp = Stopwatch.GetTimestamp();
		for (int j = 0; j < count; j++)
		{
			double time2 = time + (double)(j + 1) * step;
			long timestamp2 = Stopwatch.GetTimestamp();
			RunTicks(time2);
			lock (sync)
			{
				lock (shared.ContextLock)
				{
					context.Begin(array3[j]);
					context.End(array4[j]);
					Encode(time2, offscreen, offscreenView, w, h, offscreenBitmap);
					context.End(array5[j]);
					context.End(array3[j]);
					context.Flush();
				}
			}
			array2[j] = Stopwatch.GetElapsedTime(timestamp2).TotalMilliseconds;
			if (j >= 2)
			{
				WaitQuery(context, array5[j - 2]);
			}
		}
		long item = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		WaitQuery(context, array5[count - 1]);
		double item2 = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds / (double)count;
		for (int k = 0; k < count; k++)
		{
			QueryDataTimestampDisjoint result;
			while (!context.GetData<QueryDataTimestampDisjoint>(array3[k], AsyncGetDataFlags.None, out result))
			{
				Thread.Yield();
			}
			ulong result2;
			while (!context.GetData<ulong>(array4[k], AsyncGetDataFlags.None, out result2))
			{
				Thread.Yield();
			}
			ulong result3;
			while (!context.GetData<ulong>(array5[k], AsyncGetDataFlags.None, out result3))
			{
				Thread.Yield();
			}
			array[k] = (result.Disjoint ? double.NaN : ((double)(result3 - result2) * 1000.0 / (double)result.Frequency));
			array3[k].Dispose();
			array4[k].Dispose();
			array5[k].Dispose();
		}
		double[] array6 = (from x in array.Where(double.IsFinite)
			orderby x
			select x).ToArray();
		Array.Sort(array2);
		return (gpuMedian: (array6.Length != 0) ? array6[array6.Length / 2] : double.NaN, gpuMean: (array6.Length != 0) ? array6.Average() : double.NaN, cpuMedian: array2[count / 2], wall: item2, allocatedBytes: item, gpuP10: (array6.Length != 0) ? array6[array6.Length / 10] : double.NaN);
	}

	private static void WaitQuery(ID3D11DeviceContext context, ID3D11Query query)
	{
		ulong result;
		while (!context.GetData<ulong>(query, AsyncGetDataFlags.None, out result))
		{
			Thread.SpinWait(50);
		}
	}

	private static Rect2 Apply(Matrix3x2 m, Rect2 r)
	{
		Vector2 vector = Vector2.Transform(new Vector2((float)r.MinX, (float)r.MinY), m);
		Vector2 vector2 = Vector2.Transform(new Vector2((float)r.MaxX, (float)r.MaxY), m);
		return new Rect2(Math.Min(vector.X, vector2.X), Math.Min(vector.Y, vector2.Y), Math.Abs(vector2.X - vector.X), Math.Abs(vector2.Y - vector.Y));
	}

	public void SetPlacement(Matrix3x2 placement)
	{
		Rect2? obj2;
		lock (sync)
		{
			if (placement == Placement)
			{
				return;
			}
			Placement = placement;
			Rect2? rect = interactiveRect;
			Rect2? obj;
			if (rect.HasValue)
			{
				Rect2 valueOrDefault = rect.GetValueOrDefault();
				obj = Apply(placement, valueOrDefault);
			}
			else
			{
				obj = null;
			}
			obj2 = obj;
		}
		this.InteractiveRectChanged?.Invoke(obj2);
	}

	private void UpdateInteractiveRect(double time)
	{
		Rect2? rect = null;
		Rect2? rect2;
		if (PaperArea.HasValue)
		{
			rect2 = cardShown;
			if (rect2.HasValue)
			{
				Rect2 valueOrDefault = rect2.GetValueOrDefault();
				rect = valueOrDefault;
				goto IL_0106;
			}
		}
		if (time >= BuiltTime + 0.3)
		{
			rect2 = ObjectExtentOnCanvas();
			if (rect2.HasValue)
			{
				Rect2 valueOrDefault2 = rect2.GetValueOrDefault();
				Rect2 rect3 = valueOrDefault2;
				foreach (LabelLayout item in layout)
				{
					rect3 = rect3.Union(item.Box);
				}
				rect = rect3.Inset(-10.0, -10.0);
				goto IL_0106;
			}
		}
		if (!PaperArea.HasValue)
		{
			rect = Rect.Inset(-10.0, -10.0);
		}
		goto IL_0106;
		IL_01d2:
		bool flag;
		if (flag)
		{
			interactiveRect = rect;
			interactiveRectChanged = true;
		}
		return;
		IL_01cf:
		flag = true;
		goto IL_01d2;
		IL_0106:
		rect2 = interactiveRect;
		if (!rect.HasValue)
		{
			if (rect2.HasValue)
			{
				goto IL_01cf;
			}
			flag = false;
		}
		else
		{
			Rect2 valueOrDefault3 = rect.GetValueOrDefault();
			if (!rect2.HasValue)
			{
				goto IL_01cf;
			}
			Rect2 valueOrDefault4 = rect2.GetValueOrDefault();
			flag = Math.Abs(valueOrDefault3.MinX - valueOrDefault4.MinX) > 1.0 || Math.Abs(valueOrDefault3.MinY - valueOrDefault4.MinY) > 1.0 || Math.Abs(valueOrDefault3.Width - valueOrDefault4.Width) > 1.0 || Math.Abs(valueOrDefault3.Height - valueOrDefault4.Height) > 1.0;
		}
		goto IL_01d2;
	}

	private void UpdateInteractiveRectOutsideLock()
	{
		Rect2? obj2;
		lock (sync)
		{
			if (!interactiveRectChanged)
			{
				return;
			}
			interactiveRectChanged = false;
			Rect2? rect = interactiveRect;
			Rect2? obj;
			if (rect.HasValue)
			{
				Rect2 valueOrDefault = rect.GetValueOrDefault();
				obj = Apply(Placement, valueOrDefault);
			}
			else
			{
				obj = null;
			}
			obj2 = obj;
		}
		this.InteractiveRectChanged?.Invoke(obj2);
	}

	public void MouseDown(double x, double y)
	{
		lock (sync)
		{
			pressX = x;
			pressY = y;
			lastDragX = x;
			lastDragY = y;
			dragTravel = 0.0;
		}
	}

	public void MouseDragged(double x, double y)
	{
		bool flag = false;
		lock (sync)
		{
			double? num = lastDragX;
			if (!num.HasValue)
			{
				return;
			}
			double valueOrDefault = num.GetValueOrDefault();
			num = lastDragY;
			if (!num.HasValue)
			{
				return;
			}
			double valueOrDefault2 = num.GetValueOrDefault();
			dragTravel += Math.Sqrt((x - valueOrDefault) * (x - valueOrDefault) + (y - valueOrDefault2) * (y - valueOrDefault2));
			if (!isDragging && dragTravel > 3.0)
			{
				isDragging = true;
				userTookControl = true;
				Scene.BeginDrag();
			}
			if (isDragging)
			{
				Scene.Drag(x - valueOrDefault, y - valueOrDefault2);
				lastHandTime = Now();
				flag = true;
			}
			lastDragX = x;
			lastDragY = y;
		}
		if (flag)
		{
			StartFrames();
		}
	}

	public void MouseUp(double x, double y)
	{
		StageClick? stageClick = null;
		lock (sync)
		{
			if (isDragging)
			{
				Scene.EndDrag();
				isDragging = false;
				lastHandTime = Now();
				relayoutAfterTurn = true;
			}
			else
			{
				double? num = pressX;
				if (num.HasValue)
				{
					StageClick? stageClick2 = PickLocked(x, y);
					if (stageClick2.HasValue)
					{
						StageClick valueOrDefault = stageClick2.GetValueOrDefault();
						string partId = valueOrDefault.PartId;
						if (partId != null)
						{
							Scene.Highlight(partId, Now());
						}
						stageClick = valueOrDefault;
					}
				}
			}
			pressX = null;
			pressY = null;
			lastDragX = null;
			lastDragY = null;
		}
		StartFrames();
		if (stageClick.HasValue)
		{
			StageClick valueOrDefault2 = stageClick.GetValueOrDefault();
			this.PartClicked?.Invoke(valueOrDefault2);
		}
	}

	public StageClick? Pick(double x, double y)
	{
		lock (sync)
		{
			return PickLocked(x, y);
		}
	}

	private StageClick? PickLocked(double x, double y)
	{
		foreach (LabelLayout item in lastLayout)
		{
			if (item.Box.Inset(-4.0, -4.0).Contains(x, y) || Distance(item.PointX - x, item.PointY - y) < 9.0)
			{
				StageLabel stageLabel = labels[item.Index];
				return new StageClick(stageLabel.Text, Scene.FollowedPartId(stageLabel.Id), IsLabelled: true);
			}
		}
		if (!stopping && ObjectRect.Contains(x, y))
		{
			Pipelines shared = Pipelines.Shared;
			if (shared != null)
			{
				(double, double)[] array = new(double, double)[1 + lastLayout.Count];
				array[0] = (x - ObjectRect.MinX, y - ObjectRect.MinY);
				for (int i = 0; i < lastLayout.Count; i++)
				{
					array[i + 1] = (lastLayout[i].PointX - ObjectRect.MinX, lastLayout[i].PointY - ObjectRect.MinY);
				}
				string[] array2;
				lock (GpuDevice.Shared.ContextLock)
				{
					if (stopping)
					{
						return null;
					}
					if (renderer == null)
					{
						renderer = new FigureRenderer(shared);
					}
					array2 = renderer.PartIdentifiers(Scene, array, devicePixelsPerUnit);
				}
				string text = array2[0];
				if (text == null)
				{
					return null;
				}
				LabelLayout? labelLayout = null;
				for (int j = 0; j < lastLayout.Count; j++)
				{
					LabelLayout labelLayout2 = lastLayout[j];
					if ((array2[j + 1] == text || Scene.FollowedPartId(labels[labelLayout2.Index].Id) == text) && (!labelLayout.HasValue || DistanceTo(labelLayout2) < DistanceTo(labelLayout.Value)))
					{
						labelLayout = labelLayout2;
					}
				}
				if (!labelLayout.HasValue)
				{
					foreach (LabelLayout item2 in lastLayout)
					{
						if (DistanceTo(item2) < 40.0 && (!labelLayout.HasValue || DistanceTo(item2) < DistanceTo(labelLayout.Value)))
						{
							labelLayout = item2;
						}
					}
				}
				if (labelLayout.HasValue)
				{
					LabelLayout valueOrDefault = labelLayout.GetValueOrDefault();
					return new StageClick(labels[valueOrDefault.Index].Text, text, IsLabelled: true);
				}
				return new StageClick(Describe(text), text, IsLabelled: false);
			}
		}
		return null;
		double DistanceTo(LabelLayout item)
		{
			return Distance(item.PointX - x, item.PointY - y);
		}
	}

	private static double Distance(double dx, double dy)
	{
		return Math.Sqrt(dx * dx + dy * dy);
	}

	private string Describe(string partId)
	{
		Node3D node3D = Scene.Node(partId);
		if (node3D == null)
		{
			return "this part";
		}
		string name = node3D.Name;
		if (name != null)
		{
			string text = SpokenName(name);
			if (text != null)
			{
				return "the " + text;
			}
		}
		string text2;
		switch (node3D.Kind)
		{
		case "sphere":
			text2 = "ball";
			break;
		case "box":
			text2 = "block";
			break;
		case "ring":
		case "torus":
			text2 = "ring";
			break;
		case "cone":
		case "tube":
		case "cylinder":
		case "capsule":
			text2 = node3D.Kind;
			break;
		default:
			text2 = "part";
			break;
		}
		string text3 = text2;
		if (node3D.Material.BandCount > 0)
		{
			return "the striped " + text3;
		}
		Vector4 color = node3D.Material.Color;
		Vector3 value = new Vector3(ToSRGB(color.X), ToSRGB(color.Y), ToSRGB(color.Z));
		string text4 = "";
		float num = float.PositiveInfinity;
		(string, uint)[] clickyTones = ClickyTones;
		for (int i = 0; i < clickyTones.Length; i++)
		{
			(string, uint) tuple = clickyTones[i];
			string item = tuple.Item1;
			uint item2 = tuple.Item2;
			float num2 = Vector3.DistanceSquared(new Vector3((float)((item2 >> 16) & 0xFF) / 255f, (float)((item2 >> 8) & 0xFF) / 255f, (float)(item2 & 0xFF) / 255f), value);
			if (num2 < num)
			{
				num = num2;
				text4 = item;
			}
		}
		return ("the " + text4 + " " + text3).Replace("  ", " ");
		static float ToSRGB(float v)
		{
			if (!(v <= 0.0031308f))
			{
				return 1.055f * MathF.Pow(v, 5f / 12f) - 0.055f;
			}
			return v * 12.92f;
		}
	}

	public static string? SpokenName(string name)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		while (num < name.Length)
		{
			char c = name[num];
			char? c2 = ((num > 0) ? new char?(name[num - 1]) : ((char?)null));
			char? c3 = ((num + 1 < name.Length) ? new char?(name[num + 1]) : ((char?)null));
			if (char.IsUpper(c) && c2.HasValue)
			{
				char valueOrDefault = c2.GetValueOrDefault();
				if (char.IsLower(valueOrDefault) || char.IsDigit(valueOrDefault))
				{
					goto IL_0120;
				}
			}
			if (char.IsUpper(c) && c2.HasValue)
			{
				char valueOrDefault2 = c2.GetValueOrDefault();
				if (char.IsUpper(valueOrDefault2) && c3.HasValue)
				{
					char valueOrDefault3 = c3.GetValueOrDefault();
					if (char.IsLower(valueOrDefault3))
					{
						goto IL_0120;
					}
				}
			}
			if (char.IsDigit(c) && c2.HasValue)
			{
				char valueOrDefault4 = c2.GetValueOrDefault();
				if (char.IsLetter(valueOrDefault4))
				{
					goto IL_0120;
				}
			}
			int num2;
			if (char.IsLetter(c) && c2.HasValue)
			{
				char valueOrDefault5 = c2.GetValueOrDefault();
				num2 = (char.IsDigit(valueOrDefault5) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			goto IL_0121;
			IL_0121:
			bool flag = (byte)num2 != 0;
			bool flag2;
			switch (c)
			{
			case ' ':
			case '$':
			case '-':
			case '_':
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			if (flag2)
			{
				if (stringBuilder.Length > 0)
				{
					list.Add(stringBuilder.ToString());
				}
				stringBuilder.Clear();
			}
			else
			{
				if (flag && stringBuilder.Length > 0)
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
				}
				stringBuilder.Append(c);
			}
			num++;
			continue;
			IL_0120:
			num2 = 1;
			goto IL_0121;
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		List<string> list2 = list.Select((string w) => (w.Length <= 1 || !(w == w.ToUpperInvariant())) ? w.ToLowerInvariant() : w).ToList();
		if (string.Concat(list2).Count(char.IsLetter) < 2 || (list2.Count <= 2 && new string[6] { "part", "p", "obj", "mesh", "shape", "item" }.Contains(list2[0])))
		{
			return null;
		}
		return string.Join(' ', list2);
	}
}

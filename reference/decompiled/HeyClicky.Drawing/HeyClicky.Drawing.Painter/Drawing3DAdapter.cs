using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using HeyClicky.Drawing.Engine;
using HeyClicky.Drawing.Overlay;
using HeyClicky.Drawing3D;
using Windows.Graphics;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed class Drawing3DAdapter
{
	private sealed class Figure
	{
		public required ThreeStage Stage;

		public required Node Node;

		public required SpriteVisual Visual;

		public IFigureSurface? Surface;

		public OverlayWindow? Handle;

		public Vector2? Pressed;
	}

	[CompilerGenerated]
	private sealed class _003CSnapshots_003Ed__30 : IEnumerable<(byte[] Pixels, int Width, int Height, (double X, double Y, double Width, double Height) Box, Node Node)>, IEnumerable, IEnumerator<(byte[] Pixels, int Width, int Height, (double X, double Y, double Width, double Height) Box, Node Node)>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private (byte[] Pixels, int Width, int Height, (double X, double Y, double Width, double Height) Box, Node Node) _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		public Drawing3DAdapter _003C_003E4__this;

		private double time;

		public double _003C_003E3__time;

		private Dictionary<string, Figure>.ValueCollection.Enumerator _003C_003E7__wrap1;

		(byte[] Pixels, int Width, int Height, (double X, double Y, double Width, double Height) Box, Node Node) IEnumerator<(byte[], int, int, (double, double, double, double), Node)>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CSnapshots_003Ed__30(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 1)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = default(Dictionary<string, Figure>.ValueCollection.Enumerator);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				Drawing3DAdapter drawing3DAdapter = _003C_003E4__this;
				switch (num)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = drawing3DAdapter.stages.Values.GetEnumerator();
					_003C_003E1__state = -3;
					break;
				case 1:
					_003C_003E1__state = -3;
					break;
				}
				while (_003C_003E7__wrap1.MoveNext())
				{
					Figure current = _003C_003E7__wrap1.Current;
					byte[] item;
					int width;
					int height;
					try
					{
						item = current.Stage.Snapshot(time, out width, out height);
					}
					catch
					{
						continue;
					}
					Rect2 outerRect = current.Stage.OuterRect;
					_003C_003E2__current = (Pixels: item, Width: width, Height: height, Box: (outerRect.X, outerRect.Y, outerRect.Width, outerRect.Height), Node: current.Node);
					_003C_003E1__state = 1;
					return true;
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap1 = default(Dictionary<string, Figure>.ValueCollection.Enumerator);
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003E7__wrap1/*cast due to .constrained prefix*/).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<(byte[] Pixels, int Width, int Height, (double X, double Y, double Width, double Height) Box, Node Node)> IEnumerable<(byte[], int, int, (double, double, double, double), Node)>.GetEnumerator()
		{
			_003CSnapshots_003Ed__30 _003CSnapshots_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CSnapshots_003Ed__ = this;
			}
			else
			{
				_003CSnapshots_003Ed__ = new _003CSnapshots_003Ed__30(0)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			_003CSnapshots_003Ed__.time = _003C_003E3__time;
			return _003CSnapshots_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<(byte[], int, int, (double, double, double, double), Node)>)this).GetEnumerator();
		}
	}

	public Func<string, (string TickId, double Seconds)[], double, float[]?>? TickRunner;

	public Func<(RectInt32 Bounds, double PixelsPerUnit)>? CanvasPlacement;

	public Func<nint>? CanvasWindow;

	public DrawingThread? Thread;

	public Compositor? Compositor;

	private readonly Dictionary<string, Figure> stages = new Dictionary<string, Figure>(StringComparer.Ordinal);

	private readonly Dictionary<string, int> memberOwners = new Dictionary<string, int>(StringComparer.Ordinal);

	private readonly Dictionary<int, (string Stage, string Kind, string Id)> members = new Dictionary<int, (string, string, string)>();

	private float[] tickResult = Array.Empty<float>();

	private bool takesNoMouse;

	public bool IsSwingingToFront => stages.Values.Any((Figure figure) => figure.Stage.IsSwingingToFront);

	public bool HasFigures => stages.Count > 0;

	public event Action<JsonElement>? PartClicked;

	public static void Prefetch(string kind, string recipe)
	{
		try
		{
			ThreeStage.PrefetchShape(kind, recipe);
			ThreeStage.WarmUp();
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "3D prefetch failed: " + ex.Message);
		}
	}

	public static void WarmUp()
	{
		try
		{
			ThreeStage.WarmUp();
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "3D warm-up failed: " + ex.Message);
		}
	}

	public static void WarmUpForTurn()
	{
		try
		{
			ThreeStage.WarmUp();
			ClickyThreeKernel.Shared.WarmUp();
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "3D warm-up failed: " + ex.Message);
		}
	}

	private static JsonElement Json(JsonElement attributes, string key)
	{
		string text = ScreenPainter.Attr(attributes, key);
		if (string.IsNullOrEmpty(text))
		{
			return default(JsonElement);
		}
		try
		{
			return JsonDocument.Parse(text).RootElement.Clone();
		}
		catch
		{
			return default(JsonElement);
		}
	}

	public PenStroke? Add(string tag, int unit, JsonElement attributes, AnimationSpec? animation, Node placeholder, ScreenPainter painter)
	{
		if (tag == "scene3d")
		{
			Rect2? rect = AddStage(unit, attributes, animation, placeholder, painter);
			if (rect.HasValue)
			{
				Rect2 valueOrDefault = rect.GetValueOrDefault();
				if (animation.HasValue)
				{
					AnimationSpec valueOrDefault2 = animation.GetValueOrDefault();
					return ScreenPainter.Visit(new Vector2((float)(valueOrDefault.X + valueOrDefault.Width / 2.0), (float)(valueOrDefault.Y + valueOrDefault.Height / 2.0)), valueOrDefault2);
				}
			}
			return null;
		}
		placeholder.Visual.IsVisible = false;
		string text = ScreenPainter.Attr(attributes, "data-stage");
		if (text == null || !stages.TryGetValue(text, out Figure value))
		{
			return null;
		}
		ThreeStage stage = value.Stage;
		double num = animation?.BeginTime ?? ScreenPainter.Now;
		double duration = animation?.Duration ?? 0.65;
		string text2 = "";
		switch (tag)
		{
		case "part3d":
		{
			JsonElement op = Json(attributes, "data-part");
			if (op.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			text2 = (op.TryGetProperty("id", out var value2) ? (value2.GetString() ?? "") : "");
			stage.AddPart(op, num, duration);
			break;
		}
		case "label3d":
		{
			JsonElement jsonElement = Json(attributes, "data-label");
			if (jsonElement.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			text2 = (jsonElement.TryGetProperty("id", out var value3) ? (value3.GetString() ?? "") : "");
			double num2 = SvgValues.Number(ScreenPainter.Attr(attributes, "font-size"));
			Dictionary<string, JsonElement> dictionary = new Dictionary<string, JsonElement>();
			foreach (JsonProperty item in jsonElement.EnumerateObject())
			{
				dictionary[item.Name] = item.Value;
			}
			if (num2 > 0.0 && !dictionary.ContainsKey("size"))
			{
				dictionary["size"] = JsonSerializer.SerializeToElement(num2);
			}
			stage.AddLabelMark(JsonSerializer.SerializeToElement(dictionary), num, duration);
			break;
		}
		case "motion3d":
			stage.AddMotion(Json(attributes, "data-op"), num);
			break;
		case "tick3d":
			text2 = ScreenPainter.Attr(attributes, "data-tick") ?? "";
			stage.AddTick(text2, num);
			break;
		default:
			stage.ApplyCamera(Json(attributes, "data-op"), num);
			break;
		}
		members[unit] = (text, tag, text2);
		bool flag;
		switch (tag)
		{
		case "part3d":
		case "label3d":
		case "tick3d":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			memberOwners[text + "/" + text2] = unit;
		}
		AnimationSpec valueOrDefault3 = default(AnimationSpec);
		int num3;
		if (animation.HasValue)
		{
			valueOrDefault3 = animation.GetValueOrDefault();
			num3 = ((1 == 0) ? 1 : 0);
		}
		else
		{
			num3 = 1;
		}
		flag = (byte)num3 != 0;
		if (!flag)
		{
			bool flag2 = ((tag == "part3d" || tag == "label3d") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			return null;
		}
		Rect2 rect2 = stage.Rect;
		bool isPart = tag == "part3d";
		string member = text2;
		PenStroke penStroke = new PenStroke();
		penStroke.Points = new Vector2[1]
		{
			new Vector2((float)(rect2.X + rect2.Width / 2.0), (float)(rect2.Y + rect2.Height / 2.0))
		};
		penStroke.BeginTime = valueOrDefault3.BeginTime;
		penStroke.Duration = valueOrDefault3.Duration;
		penStroke.Timing = valueOrDefault3.Timing;
		penStroke.LateResolver = () => (isPart ? stage.PenPointsForPart(member) : stage.PenPointsForLabel(member)).Select((PointD point) => new Vector2((float)point.X, (float)point.Y)).ToArray();
		return penStroke;
	}

	private Rect2? AddStage(int unit, JsonElement attributes, AnimationSpec? animation, Node placeholder, ScreenPainter painter)
	{
		string text = ScreenPainter.Attr(attributes, "id");
		if (text == null)
		{
			return null;
		}
		if (stages.Remove(text, out Figure value))
		{
			Retire(value);
		}
		double canvasWidth = painter.CanvasSize.X;
		double canvasHeight = painter.CanvasSize.Y;
		string[] array = (ScreenPainter.Attr(attributes, "data-canvas") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 2 && SvgValues.TryNumber(array[0], out var number) && SvgValues.TryNumber(array[1], out var number2))
		{
			double num = number;
			canvasHeight = number2;
			canvasWidth = num;
		}
		ThreeStage stage;
		try
		{
			stage = ThreeStage.FromMark(attributes, canvasWidth, canvasHeight, painter.DevicePixelsPerUnit);
		}
		catch (Exception ex)
		{
			Log.Error("Drawing", "3D stage failed to start", ex);
			return null;
		}
		stage.Now = () => DrawingThread.Now;
		Func<string, (string TickId, double Seconds)[], double, float[]?> runner = TickRunner;
		stage.TickProvider = delegate(string stageId, ReadOnlySpan<TickDue> ticks, double frameSeconds)
		{
			if (runner == null)
			{
				return ReadOnlySpan<float>.Empty;
			}
			(string, double)[] array2 = new(string, double)[ticks.Length];
			for (int i = 0; i < ticks.Length; i++)
			{
				array2[i] = (ticks[i].TickId, ticks[i].Seconds);
			}
			tickResult = runner(stageId, array2, frameSeconds) ?? Array.Empty<float>();
			return tickResult;
		};
		SpriteVisual visual = painter.Compositor.CreateSpriteVisual();
		Rect2 outerRect = stage.OuterRect;
		visual.Offset = new Vector3((float)outerRect.X, (float)outerRect.Y, 0f);
		var (num2, num3) = stage.PixelSize;
		FitToSurface(num2, num3);
		IFigureSurface figureSurface = null;
		try
		{
			IFigureSurface figureSurface2 = StageSwapChain.Create(painter.Compositor, num2, num3);
			figureSurface = figureSurface2 ?? new StageSurface(painter.Compositor, num2, num3);
			CompositionSurfaceBrush compositionSurfaceBrush = painter.Compositor.CreateSurfaceBrush(figureSurface.Surface);
			compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
			visual.Brush = compositionSurfaceBrush;
			stage.Surface = figureSurface;
		}
		catch (Exception ex2)
		{
			Log.Error("Drawing", "3D stage has no surface to show", ex2);
		}
		((ContainerVisual)placeholder.Visual).Children.InsertAtTop(visual);
		placeholder.Extent = (X: outerRect.X, Y: outerRect.Y, Width: outerRect.Width, Height: outerRect.Height);
		Figure figure = new Figure
		{
			Stage = stage,
			Node = placeholder,
			Visual = visual,
			Surface = figureSurface
		};
		stages[text] = figure;
		members[unit] = (text, "scene3d", text);
		if (animation.HasValue)
		{
			AnimationSpec valueOrDefault = animation.GetValueOrDefault();
			if (valueOrDefault.Name == "fadein")
			{
				painter.AnimateScalar(placeholder.Visual, "Opacity", 0f, 1f, valueOrDefault, fillBackwards: true, placeholder);
			}
		}
		stage.InteractiveRectChanged += delegate(Rect2? rect)
		{
			Thread?.Post(delegate
			{
				UpdateHandle(figure, rect);
			});
		};
		stage.PartClicked += delegate(StageClick click)
		{
			Log.Info("Drawing", $"3D figure: a click on {click.Name} ({click.PartId})");
			this.PartClicked?.Invoke(JsonSerializer.SerializeToElement(new
			{
				name = click.Name,
				partId = click.PartId,
				isLabelled = click.IsLabelled
			}));
		};
		stage.SurfaceResized += delegate(int width, int height)
		{
			Thread?.Post(delegate
			{
				FitToSurface(width, height);
			});
		};
		return stage.Rect;
		void FitToSurface(int pixelWidth, int pixelHeight)
		{
			Rect2 outerRect2 = stage.OuterRect;
			visual.Size = new Vector2(Math.Max(1, pixelWidth), Math.Max(1, pixelHeight));
			visual.Scale = new Vector3((float)(outerRect2.Width / (double)Math.Max(1, pixelWidth)), (float)(outerRect2.Height / (double)Math.Max(1, pixelHeight)), 1f);
		}
	}

	private void UpdateHandle(Figure figure, Rect2? rect)
	{
		if (!stages.ContainsValue(figure))
		{
			return;
		}
		OverlayWindow handle;
		if (rect.HasValue)
		{
			Rect2 valueOrDefault = rect.GetValueOrDefault();
			if (!(valueOrDefault.Width < 8.0) && !(valueOrDefault.Height < 8.0) && CanvasPlacement != null && (object)Compositor != null && !takesNoMouse)
			{
				(RectInt32, double) tuple = CanvasPlacement();
				RectInt32 item = tuple.Item1;
				double item2 = tuple.Item2;
				RectInt32 boundsPx = new RectInt32((int)((double)item.X + valueOrDefault.X * item2), (int)((double)item.Y + valueOrDefault.Y * item2), Math.Max(1, (int)(valueOrDefault.Width * item2)), Math.Max(1, (int)(valueOrDefault.Height * item2)));
				if (figure.Handle == null)
				{
					handle = new OverlayWindow(Compositor, "Clicky figure", interactive: true, win32Input: true);
					handle.CustomCursor = Hand(closed: false);
					handle.PointerPressed += delegate(int x, int y)
					{
						Vector2 value = ToStage(x, y);
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 3);
						defaultInterpolatedStringHandler.AppendLiteral("3D figure: pressed at ");
						defaultInterpolatedStringHandler.AppendFormatted(value.X, "0");
						defaultInterpolatedStringHandler.AppendLiteral(",");
						defaultInterpolatedStringHandler.AppendFormatted(value.Y, "0");
						Vector2? pressed = figure.Pressed;
						defaultInterpolatedStringHandler.AppendFormatted((!pressed.HasValue) ? "" : " (already pressed)");
						Log.Info("Drawing", defaultInterpolatedStringHandler.ToStringAndClear());
						figure.Pressed = value;
						handle.ApplyCustomCursor(Hand(closed: true));
						figure.Stage.MouseDown(value.X, value.Y);
					};
					handle.PointerMoved += delegate(int x, int y)
					{
						Vector2? pressed2 = figure.Pressed;
						if (pressed2.HasValue)
						{
							Vector2 vector = ToStage(x, y);
							figure.Stage.MouseDragged(vector.X, vector.Y);
						}
					};
					handle.PointerReleased += delegate(int x, int y)
					{
						Vector2 value2 = ToStage(x, y);
						Vector2? pressed3 = figure.Pressed;
						if (pressed3.HasValue)
						{
							Vector2 valueOrDefault2 = pressed3.GetValueOrDefault();
							Log.Info("Drawing", $"3D figure: the hand let go {Vector2.Distance(valueOrDefault2, value2):0} units from where it pressed");
						}
						figure.Pressed = null;
						handle.ApplyCustomCursor(Hand(closed: false));
						figure.Stage.MouseUp(value2.X, value2.Y);
					};
					figure.Handle = handle;
				}
				if (figure.Handle.IsShown)
				{
					figure.Handle.Reframe(boundsPx);
				}
				else
				{
					figure.Handle.ShowOver(boundsPx, OverlayWindow.TopOfRunAbove(CanvasWindow?.Invoke() ?? IntPtr.Zero, "Clicky card"));
				}
				return;
			}
		}
		figure.Handle?.Hide();
		nint Hand(bool closed)
		{
			return GrabCursors.Get(closed, GetDpiForWindow(handle.Handle));
		}
		Vector2 ToStage(int x, int y)
		{
			(RectInt32, double) tuple2 = CanvasPlacement();
			RectInt32 item3 = tuple2.Item1;
			double item4 = tuple2.Item2;
			Vector2 vector2 = new Vector2((float)((double)(handle.BoundsPx.X - item3.X + x) / item4), (float)((double)(handle.BoundsPx.Y - item3.Y + y) / item4));
			if (!Matrix3x2.Invert(figure.Stage.Placement, out var result))
			{
				return vector2;
			}
			return Vector2.Transform(vector2, result);
		}
	}

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint window);

	private void Retire(Figure figure)
	{
		try
		{
			figure.Handle?.Dispose();
		}
		catch
		{
		}
		figure.Handle = null;
		try
		{
			figure.Stage.Stop();
			figure.Stage.Dispose();
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "3D stage teardown: " + ex.Message);
		}
		try
		{
			figure.Surface?.Dispose();
		}
		catch
		{
		}
		figure.Surface = null;
	}

	public void RemoveUnit(int unit, Node node)
	{
		if (!members.Remove(unit, out (string, string, string) value))
		{
			return;
		}
		if (value.Item2 == "scene3d")
		{
			if (stages.Remove(value.Item1, out Figure value2))
			{
				Retire(value2);
			}
		}
		else
		{
			if (!stages.TryGetValue(value.Item1, out Figure value3))
			{
				return;
			}
			string key = value.Item1 + "/" + value.Item3;
			if (memberOwners.TryGetValue(key, out var value4) && value4 == unit)
			{
				memberOwners.Remove(key);
				switch (value.Item2)
				{
				case "part3d":
					value3.Stage.RemovePart(value.Item3);
					break;
				case "label3d":
					value3.Stage.RemoveLabel(value.Item3);
					break;
				case "tick3d":
					value3.Stage.RemoveTick(value.Item3);
					break;
				}
			}
		}
	}

	public void SyncPlacements(ScreenPainter painter)
	{
		foreach (Figure value in stages.Values)
		{
			Matrix3x2 identity = Matrix3x2.Identity;
			for (Node parent = value.Node.Parent; parent != null; parent = parent.Parent)
			{
				if (parent.Kind == NodeKind.Wrapper)
				{
					identity *= Matrix3x2.CreateScale(parent.WrapperScale.X) * Matrix3x2.CreateTranslation(parent.WrapperOffset.X, parent.WrapperOffset.Y);
				}
			}
			value.Stage.SetPlacement(identity);
		}
	}

	public void SetDevicePixelsPerUnit(double scale)
	{
		foreach (Figure value in stages.Values)
		{
			value.Stage.SetDevicePixelsPerUnit(scale);
		}
	}

	public void HoldStill()
	{
		foreach (Figure value in stages.Values)
		{
			value.Stage.HoldStill();
		}
	}

	[IteratorStateMachine(typeof(_003CSnapshots_003Ed__30))]
	public IEnumerable<(byte[] Pixels, int Width, int Height, (double X, double Y, double Width, double Height) Box, Node Node)> Snapshots(double time)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CSnapshots_003Ed__30(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__time = time
		};
	}

	public void ClearAll()
	{
		foreach (Figure value in stages.Values)
		{
			Retire(value);
		}
		stages.Clear();
		members.Clear();
		memberOwners.Clear();
		takesNoMouse = false;
	}

	public void StopTakingTheMouse()
	{
		takesNoMouse = true;
		foreach (Figure value in stages.Values)
		{
			try
			{
				value.Handle?.Dispose();
			}
			catch
			{
			}
			value.Handle = null;
		}
	}
}

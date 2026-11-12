using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using HeyClicky.Drawing.Engine;
using HeyClicky.Drawing.Painter;
using Microsoft.UI;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Overlay;

internal sealed class ActionButtons : IDisposable
{
	internal sealed record ButtonSpec(string Title, string Kind, string Icon, bool IsSingleUse, bool LeavesAfterPress, string DoneTitle, string? Value = null)
	{
		public bool IsHandOff => Kind == "handoff";
	}

	internal sealed class ButtonWindow
	{
		private sealed class UnderlineState(SpriteVisual line)
		{
			public void SetValue(bool on)
			{
				line.Opacity = (on ? 1 : 0);
			}
		}

		private readonly ActionButtons owner;

		private readonly ButtonSpec? spec;

		private readonly OverlayWindow window;

		private readonly JsonElement payload;

		private readonly ContainerVisual plate;

		private bool hovering;

		private bool pressed;

		private bool claimed;

		private bool closed;

		private bool hiddenForFullScreen;

		private bool coveredByNotch;

		private int stepAsideGeneration;

		public Action? OnPressed;

		public Action? OnAskPressed;

		private CompositionColorGradientStop? glossTop;

		private UnderlineState? underline;

		private const double TitleSize = 12.5;

		private const double IconSize = 10.5;

		private const double PaddingX = 15.0;

		private const double MinimumHeight = 34.0;

		private const double CornerRadius = 17.0;

		private const double Spacing = 6.0;

		public RectInt32 Rect { get; private set; }

		public bool IsLink { get; }

		public bool IsFinishedAsk { get; private set; }

		public int Mark { get; }

		public bool IsAsk => spec?.Kind == "ask";

		public ContainerVisual RootVisual => window.Root;

		public double Scale { get; }

		public bool IsRetired { get; private set; }

		private static Color? GelTint => NativeEngine.ClickyInkHex.ToUpperInvariant() switch
		{
			"#FF4C4C" => Color.FromArgb(byte.MaxValue, byte.MaxValue, 184, 176), 
			"#D4A017" => Color.FromArgb(byte.MaxValue, byte.MaxValue, 223, 138), 
			"#26C281" => Color.FromArgb(byte.MaxValue, 201, 240, 207), 
			_ => null, 
		};

		public void MoveTo(int x, int y)
		{
			Rect = new RectInt32(x, y, Rect.Width, Rect.Height);
			window.MoveTo(x, y);
		}

		public ButtonWindow(ActionButtons owner, ButtonSpec? spec, RectInt32 rect, double scale, bool isLink, JsonElement payload, int mark)
		{
			this.owner = owner;
			this.spec = spec;
			this.payload = payload;
			Rect = rect;
			Scale = scale;
			IsLink = isLink;
			Mark = mark;
			Compositor compositor = owner.compositor;
			window = new OverlayWindow(compositor, isLink ? "Clicky link" : "Clicky button", interactive: true);
			plate = compositor.CreateContainerVisual();
			Vector2 vector = new Vector2((float)((double)rect.Width / scale), (float)((double)rect.Height / scale));
			plate.Size = vector;
			plate.CenterPoint = new Vector3(vector / 2f, 0f);
			window.Root.Children.InsertAtTop(plate);
			if (isLink)
			{
				BuildLink(vector);
			}
			else
			{
				BuildGel(vector, owner.painter.TextShaper, spec, spec.Title, spec.Icon);
			}
			window.PointerMoved += delegate
			{
				SetHover(value: true);
			};
			window.PointerLeft += delegate
			{
				SetHover(value: false);
			};
			window.PointerPressed += delegate
			{
				pressed = true;
			};
			window.PointerReleased += delegate
			{
				if (pressed && hovering)
				{
					Press();
				}
				pressed = false;
			};
			plate.Opacity = 0f;
			window.ShowOver(rect);
			Fade(1f, 0.25);
		}

		private void Fade(float to, double seconds)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = owner.compositor.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
			scalarKeyFrameAnimation.InsertKeyFrame(1f, to);
			scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
			plate.StartAnimation("Opacity", scalarKeyFrameAnimation);
		}

		private void SetHover(bool value)
		{
			if (hovering == value || closed)
			{
				return;
			}
			hovering = value;
			if (IsLink)
			{
				underline?.SetValue(value);
				return;
			}
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = owner.compositor.CreateVector3KeyFrameAnimation();
			vector3KeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
			vector3KeyFrameAnimation.InsertKeyFrame(1f, value ? new Vector3(1.03f, 1.03f, 1f) : Vector3.One, owner.painter.Easing(new Vector4(0f, 0f, 0.58f, 1f)));
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.15);
			plate.StartAnimation("Scale", vector3KeyFrameAnimation);
			if ((object)glossTop != null)
			{
				glossTop.Color = Color.FromArgb((byte)((value ? 0.95 : 0.85) * 255.0), byte.MaxValue, byte.MaxValue, byte.MaxValue);
			}
		}

		private void Press()
		{
			if (closed)
			{
				return;
			}
			ButtonSpec buttonSpec = spec;
			if ((object)buttonSpec != null && buttonSpec.IsSingleUse)
			{
				if (claimed)
				{
					return;
				}
				claimed = true;
			}
			Action onPressed = OnPressed;
			if (onPressed != null)
			{
				onPressed();
			}
			else
			{
				owner.Pressed?.Invoke(IsLink ? "open" : "button", payload);
			}
			if ((object)spec == null)
			{
				return;
			}
			if (spec.LeavesAfterPress)
			{
				IsFinishedAsk = true;
				OnAskPressed?.Invoke();
				owner.thread.ScheduleTimer(DrawingThread.Now + 0.12, Retire);
			}
			else if (spec.IsSingleUse)
			{
				Relabel(spec.DoneTitle, "\ue73e");
			}
			else
			{
				if (!(spec.Kind == "copy"))
				{
					return;
				}
				Relabel("Copied", "\ue73e");
				owner.thread.ScheduleTimer(DrawingThread.Now + 1.6, delegate
				{
					if (!closed)
					{
						Relabel(spec.Title, spec.Icon);
					}
				});
			}
		}

		private void Relabel(string title, string icon)
		{
			plate.Children.RemoveAll();
			BuildGel(plate.Size, owner.painter.TextShaper, spec, title, icon);
		}

		public void SetHidden(bool hidden)
		{
			if (!closed && hiddenForFullScreen != hidden)
			{
				hiddenForFullScreen = hidden;
				ApplyStepAside();
			}
		}

		public void SetCoveredByNotch(bool covered)
		{
			if (!closed && coveredByNotch != covered)
			{
				coveredByNotch = covered;
				Log.Info("Drawing", "button \"" + spec?.Title + "\" " + (covered ? "steps aside for the open notch" : "comes back"));
				ApplyStepAside();
			}
		}

		private void ApplyStepAside()
		{
			int generationNow = ++stepAsideGeneration;
			if (coveredByNotch || hiddenForFullScreen)
			{
				Fade(0f, 0.2);
				owner.thread.ScheduleTimer(DrawingThread.Now + 0.2, delegate
				{
					if (!closed && stepAsideGeneration == generationNow)
					{
						window.Hide();
					}
				});
			}
			else
			{
				if (!window.IsShown)
				{
					window.ShowOver(Rect);
				}
				Fade(1f, 0.2);
			}
		}

		public void Close()
		{
			if (!closed)
			{
				closed = true;
				window.Dispose();
			}
		}

		public void Retire()
		{
			if (!IsRetired && !closed)
			{
				IsRetired = true;
				window.SetHandCursor(hand: false);
				double num = (PinnedCards.AnimationsAreReduced ? 0.0 : 0.3);
				Fade(0f, Math.Max(0.001, num));
				owner.thread.ScheduleTimer(DrawingThread.Now + num + 0.02, delegate
				{
					Close();
					owner.shown.Remove(this);
					owner.lingering.Remove(this);
					owner.standalone.Remove(this);
				});
			}
		}

		private void BuildLink(Vector2 size)
		{
			SpriteVisual spriteVisual = owner.compositor.CreateSpriteVisual();
			spriteVisual.Size = new Vector2(size.X - 4f, 1.5f);
			spriteVisual.Offset = new Vector3(2f, size.Y - 1.5f, 0f);
			spriteVisual.Brush = owner.compositor.CreateColorBrush(Color.FromArgb(230, 51, 128, byte.MaxValue));
			spriteVisual.Opacity = 0f;
			plate.Children.InsertAtTop(spriteVisual);
			underline = new UnderlineState(spriteVisual);
		}

		public static Vector2 PanelSize(TextShaper shaper, ButtonSpec spec)
		{
			TextShaper.Font font = shaper.GetFont("system-ui", 12.5, 600);
			double advance = shaper.Shape(spec.Title, font).Advance;
			TextShaper.Font font2 = shaper.GetFont("Segoe Fluent Icons, Segoe MDL2 Assets", 10.5, 700);
			double advance2 = shaper.Shape(spec.Icon, font2).Advance;
			return new Vector2((float)(30.0 + advance2 + 6.0 + advance + 12.0), 46f);
		}

		private static Color WithWhite(Color color, double fraction)
		{
			return Color.FromArgb(color.A, (byte)((double)(int)color.R + (double)(255 - color.R) * fraction), (byte)((double)(int)color.G + (double)(255 - color.G) * fraction), (byte)((double)(int)color.B + (double)(255 - color.B) * fraction));
		}

		private void BuildGel(Vector2 size, TextShaper shaper, ButtonSpec spec, string title, string icon)
		{
			Compositor c = owner.compositor;
			float num = 6f;
			Vector2 vector = new Vector2(size.X - 2f * num, size.Y - 2f * num);
			ShapeVisual shapeVisual = c.CreateShapeVisual();
			shapeVisual.Size = size;
			plate.Children.InsertAtTop(CompositionShadows.Cast(c, shapeVisual, (X: 0.0, Y: 0.0, Width: size.X, Height: size.Y), Color.FromArgb(28, 0, 0, 0), 10f, new Vector2(0f, 4f)));
			plate.Children.InsertAtTop(shapeVisual);
			Color? color = (spec.IsHandOff ? new Color?(Color.FromArgb(byte.MaxValue, byte.MaxValue, 197, 138)) : GelTint);
			CompositionLinearGradientBrush compositionLinearGradientBrush;
			if (color.HasValue)
			{
				Color valueOrDefault = color.GetValueOrDefault();
				compositionLinearGradientBrush = Vertical(new(float, Color)[5]
				{
					(0f, WithWhite(valueOrDefault, 0.7)),
					(0.18f, WithWhite(valueOrDefault, 0.3)),
					(0.5f, valueOrDefault),
					(0.82f, valueOrDefault),
					(1f, WithWhite(valueOrDefault, 0.1))
				});
			}
			else
			{
				compositionLinearGradientBrush = Vertical(new(float, Color)[5]
				{
					(0f, Color.FromArgb(byte.MaxValue, 237, 244, byte.MaxValue)),
					(0.18f, Color.FromArgb(byte.MaxValue, 214, 231, byte.MaxValue)),
					(0.5f, Color.FromArgb(byte.MaxValue, 162, 199, byte.MaxValue)),
					(0.8f, Color.FromArgb(byte.MaxValue, 153, 203, 248)),
					(1f, Color.FromArgb(byte.MaxValue, 162, 210, 247))
				});
			}
			CompositionLinearGradientBrush fillBrush = compositionLinearGradientBrush;
			CompositionSpriteShape compositionSpriteShape = c.CreateSpriteShape(Rounded(num, num, vector.X, vector.Y, 17f));
			compositionSpriteShape.FillBrush = fillBrush;
			shapeVisual.Shapes.Add(compositionSpriteShape);
			CompositionSpriteShape compositionSpriteShape2 = c.CreateSpriteShape(Rounded(num + 4f, num + vector.Y - 2f - 12f, vector.X - 8f, 12f, 6f));
			compositionSpriteShape2.FillBrush = Vertical(new(float, Color)[2]
			{
				(0f, Color.FromArgb(0, 67, 90, 135)),
				(1f, Color.FromArgb(66, 67, 90, 135))
			});
			shapeVisual.Shapes.Add(compositionSpriteShape2);
			CompositionSpriteShape compositionSpriteShape3 = c.CreateSpriteShape(Rounded(num + 10f, num + vector.Y - 3f - 3f, vector.X - 20f, 3f, 1.5f));
			compositionSpriteShape3.FillBrush = Vertical(new(float, Color)[3]
			{
				(0f, Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
				(0.5f, Color.FromArgb(81, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
				(1f, Color.FromArgb(0, byte.MaxValue, byte.MaxValue, byte.MaxValue))
			});
			shapeVisual.Shapes.Add(compositionSpriteShape3);
			CompositionLinearGradientBrush compositionLinearGradientBrush2 = c.CreateLinearGradientBrush();
			compositionLinearGradientBrush2.StartPoint = new Vector2(0.5f, 0f);
			compositionLinearGradientBrush2.EndPoint = new Vector2(0.5f, 1f);
			glossTop = c.CreateColorGradientStop(0f, Color.FromArgb(216, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			compositionLinearGradientBrush2.ColorStops.Add(glossTop);
			compositionLinearGradientBrush2.ColorStops.Add(c.CreateColorGradientStop(1f, Color.FromArgb(5, byte.MaxValue, byte.MaxValue, byte.MaxValue)));
			CompositionSpriteShape compositionSpriteShape4 = c.CreateSpriteShape(Rounded(num + 12f, num + 3f, vector.X - 24f, 7f, 3.5f));
			compositionSpriteShape4.FillBrush = compositionLinearGradientBrush2;
			shapeVisual.Shapes.Add(compositionSpriteShape4);
			CompositionSpriteShape compositionSpriteShape5 = c.CreateSpriteShape(Rounded(num + 0.5f, num + 0.5f, vector.X - 1f, vector.Y - 1f, 16.5f));
			compositionSpriteShape5.StrokeBrush = ((!color.HasValue) ? ((CompositionBrush)c.CreateColorBrush(Color.FromArgb(byte.MaxValue, 34, 24, 152))) : ((CompositionBrush)Vertical(new(float, Color)[2]
			{
				(0f, Color.FromArgb(229, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
				(1f, Color.FromArgb(56, 0, 0, 0))
			})));
			compositionSpriteShape5.StrokeThickness = 1f;
			shapeVisual.Shapes.Add(compositionSpriteShape5);
			TextShaper.Font font = shaper.GetFont("system-ui", 12.5, 600);
			TextShaper.Font font2 = shaper.GetFont("Segoe Fluent Icons, Segoe MDL2 Assets", 10.5, 700);
			TextShaper.ShapedLine shapedLine = shaper.Shape(title, font);
			TextShaper.ShapedLine shapedLine2 = shaper.Shape(icon, font2);
			double num2 = shapedLine2.Advance + 6.0 + shapedLine.Advance;
			float num3 = (float)((double)num + ((double)vector.X - num2) / 2.0);
			float y2 = (float)((double)(num + vector.Y / 2f) + (font.Metrics.Ascent - font.Metrics.Descent) / 2.0);
			CompositionContainerShape label = c.CreateContainerShape();
			CompositionContainerShape embossed = c.CreateContainerShape();
			embossed.Offset = new Vector2(0f, 1.5f);
			CompositionColorBrush ink = c.CreateColorBrush(Colors.Black);
			CompositionColorBrush emboss = c.CreateColorBrush(Color.FromArgb(204, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			float y3 = (float)((double)(num + vector.Y / 2f) + (font2.Metrics.Ascent - font2.Metrics.Descent) / 2.0);
			AddGlyphs(shapedLine2, new Vector2(num3, y3));
			AddGlyphs(shapedLine, new Vector2(num3 + (float)(shapedLine2.Advance + 6.0), y2));
			shapeVisual.Shapes.Add(embossed);
			shapeVisual.Shapes.Add(label);
			void AddGlyphs(TextShaper.ShapedLine line, Vector2 origin)
			{
				TextShaper.Glyph[] glyphs = line.Glyphs;
				foreach (TextShaper.Glyph glyph in glyphs)
				{
					if ((object)glyph.Outline != null)
					{
						CompositionPathGeometry geometry = c.CreatePathGeometry(owner.painter.CompositionPathFor(glyph.Outline));
						CompositionSpriteShape compositionSpriteShape6 = c.CreateSpriteShape(geometry);
						compositionSpriteShape6.FillBrush = ink;
						compositionSpriteShape6.Offset = origin;
						label.Shapes.Add(compositionSpriteShape6);
						CompositionSpriteShape compositionSpriteShape7 = c.CreateSpriteShape(geometry);
						compositionSpriteShape7.FillBrush = emboss;
						compositionSpriteShape7.Offset = origin;
						embossed.Shapes.Add(compositionSpriteShape7);
					}
				}
			}
			CompositionRoundedRectangleGeometry Rounded(float x, float y, float w, float h, float r)
			{
				CompositionRoundedRectangleGeometry compositionRoundedRectangleGeometry = c.CreateRoundedRectangleGeometry();
				compositionRoundedRectangleGeometry.Offset = new Vector2(x, y);
				compositionRoundedRectangleGeometry.Size = new Vector2(w, h);
				compositionRoundedRectangleGeometry.CornerRadius = new Vector2(r);
				return compositionRoundedRectangleGeometry;
			}
			CompositionLinearGradientBrush Vertical((float At, Color Color)[] stops)
			{
				CompositionLinearGradientBrush compositionLinearGradientBrush3 = c.CreateLinearGradientBrush();
				compositionLinearGradientBrush3.StartPoint = new Vector2(0.5f, 0f);
				compositionLinearGradientBrush3.EndPoint = new Vector2(0.5f, 1f);
				for (int i = 0; i < stops.Length; i++)
				{
					var (offset, color2) = stops[i];
					compositionLinearGradientBrush3.ColorStops.Add(c.CreateColorGradientStop(offset, color2));
				}
				return compositionLinearGradientBrush3;
			}
		}
	}

	private readonly DrawingThread thread;

	private readonly Compositor compositor;

	private readonly ScreenPainter painter;

	private readonly NativeEngine engine;

	private readonly OverlayWindow canvasWindow;

	private readonly List<ButtonWindow> shown = new List<ButtonWindow>();

	private readonly Dictionary<int, (string Role, JsonElement Payload)> waiting = new Dictionary<int, (string, JsonElement)>();

	private readonly Dictionary<int, double> readyTimes = new Dictionary<int, double>();

	private int generation;

	private const double Margin = 6.0;

	private readonly List<(ButtonWindow Window, RectInt32 Start)> riding = new List<(ButtonWindow, RectInt32)>();

	private readonly List<ButtonWindow> lingering = new List<ButtonWindow>();

	private int lingeringGeneration;

	private readonly List<ButtonWindow> standalone = new List<ButtonWindow>();

	public IEnumerable<(RectInt32 Rect, ContainerVisual Root, double Scale)> Windows => shown.Select((ButtonWindow w) => (Rect: w.Rect, RootVisual: w.RootVisual, Scale: w.Scale));

	public bool AnyShown => shown.Count > 0;

	public event Action<string, JsonElement>? Pressed;

	public ActionButtons(DrawingThread thread, Compositor compositor, ScreenPainter painter, NativeEngine engine, OverlayWindow canvasWindow)
	{
		this.thread = thread;
		this.compositor = compositor;
		this.painter = painter;
		this.engine = engine;
		this.canvasWindow = canvasWindow;
	}

	public void Queue(string role, int mark, JsonElement action)
	{
		JsonElement jsonElement = action.Clone();
		double value;
		if (mark < 0)
		{
			Show(role, mark, jsonElement);
		}
		else if (readyTimes.Remove(mark, out value))
		{
			ShowAfter(role, mark, jsonElement, value - DrawingThread.Now);
		}
		else
		{
			waiting[mark] = (role, jsonElement);
		}
	}

	public void BeatDrawn(int mark, double seconds)
	{
		if (waiting.Remove(mark, out (string, JsonElement) value))
		{
			ShowAfter(value.Item1, mark, value.Item2, seconds);
		}
		else
		{
			readyTimes[mark] = DrawingThread.Now + seconds;
		}
	}

	private void ShowAfter(string role, int mark, JsonElement payload, double seconds)
	{
		if (seconds <= 0.01)
		{
			Show(role, mark, payload);
			return;
		}
		int generationAtStart = generation;
		thread.ScheduleTimer(DrawingThread.Now + seconds, delegate
		{
			if (generation == generationAtStart)
			{
				Show(role, mark, payload);
			}
		});
	}

	internal static ButtonSpec? Parse(string payloadText)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(payloadText);
			JsonElement rootElement = jsonDocument.RootElement;
			string text = (rootElement.TryGetProperty("text", out var value) ? (value.GetString() ?? "").Trim() : "");
			JsonElement value2;
			string text2 = (rootElement.TryGetProperty("kind", out value2) ? (value2.GetString() ?? "") : "");
			if (text.Length == 0)
			{
				return null;
			}
			text = ButtonLabels.Label(text, ButtonLabels.LimitFor(text2));
			switch (text2)
			{
			case "open":
			{
				if (!Uri.TryCreate(rootElement.TryGetProperty("value", out var value5) ? (value5.GetString() ?? "").Trim() : "", UriKind.Absolute, out Uri result) || (result.Scheme != "https" && result.Scheme != "http"))
				{
					return null;
				}
				return new ButtonSpec(text, text2, "\ue8a7", IsSingleUse: false, LeavesAfterPress: false, text);
			}
			case "copy":
			{
				if (!rootElement.TryGetProperty("value", out var value7) || string.IsNullOrEmpty(value7.GetString()))
				{
					return null;
				}
				return new ButtonSpec(text, text2, "\ue8c8", IsSingleUse: false, LeavesAfterPress: false, text);
			}
			case "ask":
			{
				if (!rootElement.TryGetProperty("value", out var value6) || string.IsNullOrWhiteSpace(value6.GetString()))
				{
					return null;
				}
				return new ButtonSpec(text, text2, "\ue8bd", IsSingleUse: true, LeavesAfterPress: true, "Asked");
			}
			case "handoff":
			{
				if (!rootElement.TryGetProperty("value", out var value8) || value8.ValueKind != JsonValueKind.Object)
				{
					return null;
				}
				return new ButtonSpec(text, text2, "\ue72a", IsSingleUse: true, LeavesAfterPress: false, "On it");
			}
			case "card":
			{
				JsonElement value3;
				string key = ((rootElement.TryGetProperty("value", out value3) && value3.ValueKind == JsonValueKind.String) ? (value3.GetString() ?? "").Trim().ToLowerInvariant() : "");
				if (!DrawingSuggestionCards.CardIdsByKey.TryGetValue(key, out string value4))
				{
					return null;
				}
				return new ButtonSpec(text, text2, "\ue72a", IsSingleUse: true, LeavesAfterPress: false, "On it", value4);
			}
			default:
				return null;
			}
		}
		catch
		{
			return null;
		}
	}

	private void Show(string role, int mark, JsonElement payload)
	{
		if (!canvasWindow.IsShown)
		{
			Log.Info("Drawing", "button not shown: no drawing");
			return;
		}
		RectInt32 bounds = canvasWindow.BoundsPx;
		Vector2 canvasSize = painter.CanvasSize;
		if (canvasSize.X <= 0f)
		{
			return;
		}
		double pixelsPerUnit = (float)bounds.Width / canvasSize.X;
		double rasterizationScale = canvasWindow.RasterizationScale;
		double num = pixelsPerUnit / rasterizationScale;
		JsonElement value;
		string text = ((payload.TryGetProperty("text", out value) && value.ValueKind == JsonValueKind.String) ? (value.GetString() ?? "") : "");
		if (role == "link")
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out Uri result) || (result.Scheme != "https" && result.Scheme != "http"))
			{
				return;
			}
			RectD? rectD = engine.DrawnBox(mark);
			if (rectD.HasValue)
			{
				RectD valueOrDefault = rectD.GetValueOrDefault();
				if (valueOrDefault.Width > 1.0 && valueOrDefault.Height > 1.0)
				{
					RectInt32 rect = new RectInt32((int)((double)bounds.X + valueOrDefault.X * pixelsPerUnit - 2.0 * rasterizationScale), (int)((double)bounds.Y + valueOrDefault.Y * pixelsPerUnit - 2.0 * rasterizationScale), (int)Math.Max(12.0 * rasterizationScale, valueOrDefault.Width * pixelsPerUnit + 4.0 * rasterizationScale), (int)Math.Max(12.0 * rasterizationScale, valueOrDefault.Height * pixelsPerUnit + 4.0 * rasterizationScale));
					JsonElement payload2 = JsonSerializer.SerializeToElement(new
					{
						text = text,
						mark = mark,
						url = text
					});
					shown.Add(new ButtonWindow(this, null, rect, rasterizationScale, isLink: true, payload2, mark));
					return;
				}
			}
			Log.Info("Drawing", "link not clickable: its words aren't drawn");
			return;
		}
		ButtonSpec buttonSpec = Parse(text);
		if ((object)buttonSpec == null)
		{
			Log.Info("Drawing", "button skipped: unreadable " + text.Substring(0, Math.Min(text.Length, 120)));
		}
		else
		{
			if (!payload.TryGetProperty("box", out var value2) || value2.ValueKind != JsonValueKind.Array || value2.GetArrayLength() != 4)
			{
				return;
			}
			double[] array = (from e in value2.EnumerateArray()
				select e.GetDouble()).ToArray();
			foreach (ButtonWindow item in shown.Where((ButtonWindow w) => w.IsFinishedAsk).ToList())
			{
				item.Close();
				shown.Remove(item);
			}
			Vector2 vector = ButtonWindow.PanelSize(painter.TextShaper, buttonSpec);
			Vector2 vector2 = new Vector2((float)((double)vector.X / num), (float)((double)vector.Y / num));
			RectD centred = new RectD(array[0] + array[2] / 2.0 - (double)(vector2.X / 2f), array[1] + array[3] / 2.0 - (double)(vector2.Y / 2f), vector2.X, vector2.Y);
			List<RectD> avoiding = (from w in shown
				where !w.IsLink
				select new RectD((double)(w.Rect.X - bounds.X) / pixelsPerUnit, (double)(w.Rect.Y - bounds.Y) / pixelsPerUnit, (double)w.Rect.Width / pixelsPerUnit, (double)w.Rect.Height / pixelsPerUnit)).ToList();
			RectD rectD2 = centred;
			IEnumerable<RectD> source = from w in shown
				where w.IsAsk
				select new RectD((double)(w.Rect.X - bounds.X) / pixelsPerUnit, (double)(w.Rect.Y - bounds.Y) / pixelsPerUnit, (double)w.Rect.Width / pixelsPerUnit, (double)w.Rect.Height / pixelsPerUnit);
			RectD rectD3 = ((buttonSpec.Kind != "ask") ? default(RectD) : (from o in source
				where Math.Abs(o.Y + o.Height / 2.0 - (centred.Y + centred.Height / 2.0)) < centred.Height * 0.75 && o.Right <= centred.X + 1.0
				orderby o.Right descending
				select o).FirstOrDefault());
			if (rectD3.Width > 0.0)
			{
				RectD rectD4 = new RectD(rectD3.Right + 7.0 / num, rectD3.Y + (rectD3.Height - centred.Height) / 2.0, centred.Width, centred.Height);
				if (rectD4.Right <= (double)(canvasSize.X - 4f))
				{
					rectD2 = rectD4;
				}
			}
			RectD rectD5 = engine.ClearButtonSpot(rectD2, 6.0 / num, avoiding) ?? rectD2;
			int num2 = (int)Math.Round((double)vector.X * rasterizationScale);
			int num3 = (int)Math.Round((double)vector.Y * rasterizationScale);
			int value3 = (int)Math.Round((double)bounds.X + (rectD5.X + rectD5.Width / 2.0) * pixelsPerUnit - (double)num2 / 2.0);
			int value4 = (int)Math.Round((double)bounds.Y + (rectD5.Y + rectD5.Height / 2.0) * pixelsPerUnit - (double)num3 / 2.0);
			value3 = Math.Clamp(value3, bounds.X + (int)(4.0 * rasterizationScale), bounds.X + bounds.Width - num2 - (int)(4.0 * rasterizationScale));
			value4 = Math.Clamp(value4, bounds.Y + (int)(4.0 * rasterizationScale), bounds.Y + bounds.Height - num3 - (int)(4.0 * rasterizationScale));
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			string title = buttonSpec.Title;
			string kind = buttonSpec.Kind;
			string value5 = buttonSpec.Value;
			JsonElement value6;
			JsonElement payload3 = JsonSerializer.SerializeToElement(new
			{
				text = title,
				kind = kind,
				value = ((value5 != null) ? JsonSerializer.SerializeToElement(value5) : (jsonDocument.RootElement.TryGetProperty("value", out value6) ? value6.Clone() : default(JsonElement))),
				mark = mark
			});
			shown.Add(new ButtonWindow(this, buttonSpec, new RectInt32(value3, value4, num2, num3), rasterizationScale, isLink: false, payload3, mark));
			engine.PlayButtonAppearSound();
			Log.Info("Drawing", $"button \"{buttonSpec.Title}\" ({buttonSpec.Kind}) at {value3},{value4} {num2}x{num3}");
			RectInt32? openFrame = NotchVisibleSurfaces.OpenFrame;
			if (openFrame.HasValue)
			{
				RectInt32 valueOrDefault2 = openFrame.GetValueOrDefault();
				StepAroundNotch(valueOrDefault2);
			}
		}
	}

	public void BeginRide(IEnumerable<int> cardMarks, RectInt32 cardRect)
	{
		riding.Clear();
		HashSet<int> hashSet = cardMarks.ToHashSet();
		foreach (ButtonWindow item in shown)
		{
			int num = item.Rect.X + item.Rect.Width / 2;
			int num2 = item.Rect.Y + item.Rect.Height / 2;
			if (hashSet.Contains(item.Mark) || (num >= cardRect.X && num <= cardRect.X + cardRect.Width && num2 >= cardRect.Y && num2 <= cardRect.Y + cardRect.Height))
			{
				riding.Add((item, item.Rect));
			}
		}
	}

	public void Ride(int shiftX, int shiftY)
	{
		foreach (var (buttonWindow, rectInt) in riding)
		{
			buttonWindow.MoveTo(rectInt.X + shiftX, rectInt.Y + shiftY);
		}
	}

	public void EndRide(bool keep)
	{
		if (!keep)
		{
			foreach (var (buttonWindow, rectInt) in riding)
			{
				buttonWindow.MoveTo(rectInt.X, rectInt.Y);
			}
		}
		riding.Clear();
	}

	public void RetireRiding(IEnumerable<int> cardMarks, RectInt32 cardRect)
	{
		BeginRide(cardMarks, cardRect);
		foreach (var item2 in riding)
		{
			ButtonWindow item = item2.Window;
			item.Close();
			shown.Remove(item);
		}
		riding.Clear();
	}

	public void SetTemporarilyHidden(bool hidden)
	{
		foreach (ButtonWindow item in shown)
		{
			item.SetHidden(hidden);
		}
	}

	public void StepAroundNotch(RectInt32? openNotch)
	{
		foreach (ButtonWindow item in shown.Where((ButtonWindow w) => !w.IsLink))
		{
			int coveredByNotch;
			if (openNotch.HasValue)
			{
				RectInt32 valueOrDefault = openNotch.GetValueOrDefault();
				coveredByNotch = (NotchVisibleSurfaces.Intersects(valueOrDefault, item.Rect) ? 1 : 0);
			}
			else
			{
				coveredByNotch = 0;
			}
			item.SetCoveredByNotch((byte)coveredByNotch != 0);
		}
	}

	public void RetireAsks()
	{
		RetireLingeringAsks();
		foreach (ButtonWindow item in shown.Where((ButtonWindow w) => w.IsAsk).ToList())
		{
			item.Close();
			shown.Remove(item);
		}
	}

	public int KeepAsksAfterTheDrawing()
	{
		List<ButtonWindow> list = shown.Where((ButtonWindow w) => w.IsAsk && !w.IsRetired && !w.IsFinishedAsk).ToList();
		foreach (ButtonWindow item in list)
		{
			shown.Remove(item);
		}
		RetireLingeringAsks();
		lingering.AddRange(list);
		foreach (ButtonWindow item2 in list)
		{
			item2.OnAskPressed = delegate
			{
				RetireLingeringAsks(exceptPressed: true);
			};
		}
		if (list.Count == 0)
		{
			return 0;
		}
		int generationNow = ++lingeringGeneration;
		thread.ScheduleTimer(DrawingThread.Now + 120.0, delegate
		{
			if (generationNow == lingeringGeneration)
			{
				RetireLingeringAsks();
			}
		});
		return list.Count;
	}

	public void RetireLingeringAsks(bool exceptPressed = false)
	{
		foreach (ButtonWindow item in lingering.ToList())
		{
			if (!exceptPressed || !item.IsFinishedAsk)
			{
				item.Retire();
			}
		}
		if (!exceptPressed)
		{
			lingeringGeneration++;
		}
	}

	public Vector2 StandaloneSize(string title, string kind)
	{
		return ButtonWindow.PanelSize(painter.TextShaper, StandaloneSpec(title, kind));
	}

	private static ButtonSpec StandaloneSpec(string title, string kind)
	{
		if (!(kind == "dismiss"))
		{
			return new ButtonSpec(ButtonLabels.Label(title, 60), "ask", "\ue8bd", IsSingleUse: true, LeavesAfterPress: true, "Asked");
		}
		return new ButtonSpec(ButtonLabels.Label(title, 40), "dismiss", "\ue711", IsSingleUse: true, LeavesAfterPress: false, ButtonLabels.Label(title, 40));
	}

	public ButtonWindow ShowStandalone(string title, string kind, RectInt32 rectPx, double scale, Action pressed)
	{
		ButtonWindow buttonWindow = new ButtonWindow(this, StandaloneSpec(title, kind), rectPx, scale, isLink: false, default(JsonElement), -1)
		{
			OnPressed = pressed
		};
		standalone.Add(buttonWindow);
		return buttonWindow;
	}

	public void Clear()
	{
		generation++;
		foreach (ButtonWindow item in shown)
		{
			item.Close();
		}
		shown.Clear();
		waiting.Clear();
		readyTimes.Clear();
	}

	public void Dispose()
	{
		Clear();
		foreach (ButtonWindow item in lingering.Concat(standalone))
		{
			item.Close();
		}
		lingering.Clear();
		standalone.Clear();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Text;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Overlay;

internal sealed class DrawingCaption
{
	private const float MaximumWidthPoints = 900f;

	private const float PaddingX = 14f;

	private const float PaddingTop = 10f;

	private const float PaddingBottom = 11f;

	private const float BottomOffsetPoints = 84f;

	private readonly Compositor compositor;

	private readonly OverlayWindow window;

	private readonly CompositionGraphicsDevice graphicsDevice;

	private readonly SpriteVisual visual;

	private CompositionDrawingSurface? surface;

	private string lastJson = "";

	public string ShownText { get; private set; } = "";

	public DrawingCaption(Compositor compositor, OverlayWindow window)
	{
		this.compositor = compositor;
		this.window = window;
		graphicsDevice = SystemComposition.GraphicsDevice(compositor, CanvasDevice.GetSharedDevice());
		visual = compositor.CreateSpriteVisual();
		visual.IsVisible = false;
		window.Root.Children.InsertAtTop(visual);
	}

	public void Show(string json)
	{
		if (json == lastJson)
		{
			return;
		}
		lastJson = json;
		if (string.IsNullOrEmpty(json))
		{
			Hide();
			return;
		}
		List<(string, string, bool)> list = new List<(string, string, bool)>();
		string header;
		string text;
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			header = ((rootElement.TryGetProperty("header", out var value) && value.ValueKind == JsonValueKind.String) ? (value.GetString() ?? "") : "");
			text = ((rootElement.TryGetProperty("text", out var value2) && value2.ValueKind == JsonValueKind.String) ? (value2.GetString() ?? "") : "");
			if (rootElement.TryGetProperty("chips", out var value3) && value3.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value3.EnumerateArray())
				{
					list.Add((item.TryGetProperty("label", out var value4) ? (value4.GetString() ?? "") : "", item.TryGetProperty("state", out var value5) ? (value5.GetString() ?? "waiting") : "waiting", item.TryGetProperty("late", out var value6) && value6.ValueKind == JsonValueKind.True));
				}
			}
		}
		catch (JsonException)
		{
			Hide();
			return;
		}
		try
		{
			Render(header, text, list);
		}
		catch (Exception ex2)
		{
			Log.Warn("Drawing", "caption did not draw: " + ex2.Message);
			Hide();
		}
	}

	public void Hide()
	{
		lastJson = "";
		ShownText = "";
		visual.IsVisible = false;
	}

	private void Render(string header, string text, List<(string Label, string State, bool Late)> chips)
	{
		CanvasDevice device = CanvasDevice.GetSharedDevice();
		float num = (float)Math.Max(1.0, window.RasterizationScale);
		float num2 = (float)window.BoundsPx.Width / num;
		float num3 = (float)window.BoundsPx.Height / num;
		float num4 = Math.Min(900f, num2 * 0.8f);
		float num5 = Math.Max(40f, num4 - 28f);
		using CanvasTextFormat textFormat = new CanvasTextFormat
		{
			FontFamily = "Cascadia Mono, Consolas",
			FontSize = 11f,
			FontWeight = FontWeights.SemiBold,
			WordWrapping = CanvasWordWrapping.Wrap
		};
		using CanvasTextFormat textFormat2 = new CanvasTextFormat
		{
			FontFamily = "Segoe UI Variable Text, Segoe UI",
			FontSize = 15f,
			FontWeight = FontWeights.Medium,
			WordWrapping = CanvasWordWrapping.Wrap
		};
		CanvasTextFormat chipFormat = new CanvasTextFormat
		{
			FontFamily = "Cascadia Mono, Consolas",
			FontSize = 11.5f,
			FontWeight = FontWeights.Medium,
			WordWrapping = CanvasWordWrapping.NoWrap
		};
		try
		{
			using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(device, header, textFormat, num5, 10000f);
			using CanvasTextLayout canvasTextLayout2 = new CanvasTextLayout(device, text, textFormat2, num5, 10000f);
			List<(CanvasTextLayout, string, bool)> list = chips.Select<(string, string, bool), (CanvasTextLayout, string, bool)>(((string Label, string State, bool Late) c) => (Layout: new CanvasTextLayout(device, " " + c.Label + " ", chipFormat, 10000f, 100f), State: c.State, Late: c.Late)).ToList();
			try
			{
				List<Vector2> list2 = new List<Vector2>();
				float num6 = 0f;
				float num7 = 0f;
				float num8 = 0f;
				foreach (var item6 in list)
				{
					float num9 = (float)item6.Item1.LayoutBounds.Width;
					if (num6 > 0f && num6 + num9 > num5)
					{
						num6 = 0f;
						num7 += 23f;
					}
					list2.Add(new Vector2(num6, num7));
					num6 += num9 + 8f;
					num8 = Math.Max(num8, num6 - 8f);
				}
				float num10 = ((list.Count == 0) ? 0f : (num7 + 18f));
				float num11 = ((header.Length == 0) ? 0f : ((float)canvasTextLayout.LayoutBounds.Height + 2f));
				float num12 = ((text.Length == 0) ? 0f : ((float)canvasTextLayout2.LayoutBounds.Height));
				float num13 = Math.Max(Math.Max((float)canvasTextLayout.LayoutBounds.Width, (float)canvasTextLayout2.LayoutBounds.Width), num8);
				float num14 = MathF.Ceiling(Math.Min(num4, num13 + 28f));
				float num15 = MathF.Ceiling(10f + num11 + num12 + ((list.Count == 0) ? 0f : (6f + num10)) + 11f);
				Size size = new Size(Math.Ceiling(num14 * num), Math.Ceiling(num15 * num));
				if ((object)surface == null)
				{
					surface = graphicsDevice.CreateDrawingSurface(size, DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
				}
				else
				{
					SystemComposition.Resize(surface, size);
				}
				using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(surface))
				{
					CanvasDrawingSession session = surfaceDrawing.Session;
					session.Clear(Color.FromArgb(0, 0, 0, 0));
					session.Transform = Matrix3x2.CreateScale(num);
					session.FillRoundedRectangle(0f, 0f, num14, num15, 14f, 14f, Color.FromArgb(219, 12, 14, 20));
					float num16 = 10f;
					if (header.Length > 0)
					{
						session.DrawTextLayout(canvasTextLayout, 14f, num16, Color.FromArgb(byte.MaxValue, 154, 164, 184));
						num16 += num11;
					}
					if (text.Length > 0)
					{
						session.DrawTextLayout(canvasTextLayout2, 14f, num16, Colors.White);
						num16 += num12;
					}
					if (list.Count > 0)
					{
						num16 += 6f;
						for (int i = 0; i < list.Count; i++)
						{
							(CanvasTextLayout, string, bool) tuple = list[i];
							CanvasTextLayout item = tuple.Item1;
							string item2 = tuple.Item2;
							bool item3 = tuple.Item3;
							(Color, Color) tuple2 = ((item2 == "drawing") ? (Color.FromArgb(byte.MaxValue, byte.MaxValue, 214, 102), Color.FromArgb(56, byte.MaxValue, 214, 102)) : ((!(item2 == "done")) ? (Color.FromArgb(byte.MaxValue, 139, 147, 165), Color.FromArgb(20, byte.MaxValue, byte.MaxValue, byte.MaxValue)) : (Color.FromArgb(byte.MaxValue, 143, 227, 176), Color.FromArgb(46, 120, 219, 161))));
							(Color, Color) tuple3 = tuple2;
							Color item4 = tuple3.Item1;
							Color item5 = tuple3.Item2;
							Vector2 vector = new Vector2(14f + list2[i].X, num16 + list2[i].Y);
							float num17 = (float)item.LayoutBounds.Width;
							session.FillRoundedRectangle(vector.X, vector.Y, num17, 18f, 4f, 4f, item5);
							session.DrawTextLayout(item, vector.X, vector.Y + (18f - (float)item.LayoutBounds.Height) / 2f, item4);
							if (item3)
							{
								session.DrawLine(vector.X + 2f, vector.Y + 18f + 1.5f, vector.X + num17 - 2f, vector.Y + 18f + 1.5f, Color.FromArgb(204, byte.MaxValue, 140, 120), 1.2f);
							}
						}
					}
				}
				CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(surface);
				compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
				visual.Brush = compositionSurfaceBrush;
				visual.Size = new Vector2(num14, num15);
				visual.Offset = new Vector3(MathF.Round((num2 - num14) / 2f), MathF.Round(num3 - 84f - num15), 0f);
				visual.IsVisible = true;
				ShownText = text;
			}
			finally
			{
				foreach (var item7 in list)
				{
					item7.Item1.Dispose();
				}
			}
		}
		finally
		{
			if ((object)chipFormat != null)
			{
				((IDisposable)chipFormat).Dispose();
			}
		}
	}
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using HeyClicky.Drawing.Engine;
using HeyClicky.Drawing.Interop;
using HeyClicky.Drawing.Media;
using HeyClicky.Drawing.Painter;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Input;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Overlay;

internal sealed class CardHandles : IDisposable
{
	private sealed class Drag
	{
		public required NativeEngine.DraggableCard Card;

		public required Handle Handle;

		public required NativeMethods.POINT StartPointer;

		public required RectInt32 HandleStart;

		public Vector2 Offset;
	}

	private readonly record struct Rect(double X, double Y, double Width, double Height)
	{
		public bool Contains(Vector2 p, double grow = 0.0)
		{
			if ((double)p.X >= X - grow && (double)p.X <= X + Width + grow && (double)p.Y >= Y - grow)
			{
				return (double)p.Y <= Y + Height + grow;
			}
			return false;
		}
	}

	private sealed class Layout
	{
		public Rect CardRect;

		public List<(int Kind, Rect Rect)> Buttons = new List<(int, Rect)>();

		public double ButtonUnit = 1.0;

		public double CornerRadius;

		public Rect? TitleBar;

		public bool IsFullScreen;

		public bool CanGoFullScreen = true;

		public Rect? DownloadRect;

		public bool IsOnDownload(Vector2 p)
		{
			return DownloadRect?.Contains(p, 2.0) ?? false;
		}

		public bool DragsFrom(Vector2 p)
		{
			return TitleBar?.Contains(p, 3.0) ?? true;
		}

		public int? ButtonAt(Vector2 p)
		{
			foreach (var (value, rect) in Buttons)
			{
				if (rect.Contains(p))
				{
					return value;
				}
			}
			return null;
		}
	}

	private enum Press
	{
		None,
		Button,
		Download,
		Drag,
		OutsideFullScreenCard,
		TitleBarDoubleClick
	}

	private sealed class Handle
	{
		private readonly CardHandles owner;

		private readonly OverlayWindow window;

		private readonly ContainerVisual buttonsVisual;

		private readonly ContainerVisual downloadVisual;

		private Layout layout = new Layout();

		private double scale = 1.0;

		private bool revealed;

		private int? hoveredButton;

		private bool hoveringDownload;

		private bool downloadPressedUnderPointer;

		private bool savingDownload;

		private bool showsDownloadDone;

		private int? pressedButton;

		private Press press;

		private long lastPressTicks;

		public NativeEngine.DraggableCard? Card { get; private set; }

		public RectInt32 Frame => window.BoundsPx;

		public nint WindowHandle => window.Handle;

		public bool IsShown => window.IsShown;

		public Handle(CardHandles owner)
		{
			this.owner = owner;
			Compositor compositor = owner.compositor;
			window = new OverlayWindow(compositor, "Clicky card", interactive: true);
			SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.RelativeSizeAdjustment = Vector2.One;
			spriteVisual.Brush = compositor.CreateColorBrush(Color.FromArgb(1, 0, 0, 0));
			window.Root.Children.InsertAtTop(spriteVisual);
			buttonsVisual = compositor.CreateContainerVisual();
			buttonsVisual.Opacity = 0f;
			window.Root.Children.InsertAtTop(buttonsVisual);
			downloadVisual = compositor.CreateContainerVisual();
			window.Root.Children.InsertAtTop(downloadVisual);
			window.PointerMoved += delegate(int x, int y)
			{
				Moved(Point(x, y));
			};
			window.PointerLeft += Left;
			window.PointerPressed += delegate(int x, int y)
			{
				Pressed(Point(x, y));
			};
			window.PointerReleased += delegate(int x, int y)
			{
				Released(Point(x, y));
			};
		}

		private Vector2 Point(int x, int y)
		{
			return new Vector2((float)((double)x / scale), (float)((double)y / scale));
		}

		public void Place(NativeEngine.DraggableCard card, RectInt32 frame, Layout newLayout, double rasterization)
		{
			Card = card;
			layout = newLayout;
			scale = rasterization;
			if (window.IsShown)
			{
				window.Reframe(frame);
			}
			else
			{
				window.ShowOver(frame, owner.canvasWindow.Handle);
			}
			DrawButtons();
			DrawDownload();
		}

		public void ShowButtonsForHarness(int? hovered)
		{
			SetRevealed(value: true, animated: false);
			hoveredButton = hovered;
			DrawButtons();
		}

		public void MoveTo(int x, int y)
		{
			window.MoveTo(x, y);
		}

		public void BringToFront()
		{
			window.StackJustAbove(IntPtr.Zero);
		}

		public void StackJustAbove(nint below)
		{
			window.StackJustAbove(below);
		}

		public void Hide()
		{
			press = Press.None;
			SetRevealed(value: false, animated: false);
			window.Hide();
		}

		public void Close()
		{
			window.Dispose();
		}

		private void Moved(Vector2 p)
		{
			if (press == Press.Drag)
			{
				owner.ContinueDrag();
				return;
			}
			if (press == Press.Button)
			{
				int? num = pressedButton;
				if (num.HasValue)
				{
					int kind = num.GetValueOrDefault();
					if (layout.Buttons.Any(((int Kind, Rect Rect) b) => b.Kind == kind && b.Rect.Contains(p, 4.0)))
					{
						return;
					}
				}
				num = pressedButton;
				if (num.HasValue)
				{
					pressedButton = null;
					DrawButtons();
				}
				return;
			}
			if (press == Press.Download)
			{
				bool flag = layout.DownloadRect?.Contains(p, 4.0) ?? false;
				if (flag != downloadPressedUnderPointer)
				{
					downloadPressedUnderPointer = flag;
					DrawDownload();
				}
				return;
			}
			SetRevealed(layout.CardRect.Contains(p, 2.0), animated: true);
			int? num2 = layout.ButtonAt(p);
			if (num2 != hoveredButton)
			{
				hoveredButton = num2;
				DrawButtons();
			}
			bool flag2 = layout.IsOnDownload(p);
			if (flag2 != hoveringDownload)
			{
				hoveringDownload = flag2;
				DrawDownload();
			}
			if (flag2)
			{
				window.SetCursor(InputSystemCursorShape.Hand);
			}
			else if (num2.HasValue || layout.IsFullScreen || !layout.CardRect.Contains(p, 2.0) || !layout.DragsFrom(p))
			{
				window.SetCursor(InputSystemCursorShape.Arrow);
			}
			else
			{
				window.SetCursor(InputSystemCursorShape.SizeAll);
			}
		}

		private void Left()
		{
			if (press == Press.None)
			{
				int? num = hoveredButton;
				if (num.HasValue)
				{
					hoveredButton = null;
					DrawButtons();
				}
				if (hoveringDownload)
				{
					hoveringDownload = false;
					DrawDownload();
				}
				SetRevealed(value: false, animated: true);
			}
		}

		private void Pressed(Vector2 p)
		{
			long tickCount = Environment.TickCount64;
			bool flag = tickCount - lastPressTicks <= GetDoubleClickTime();
			lastPressTicks = tickCount;
			int? num = layout.ButtonAt(p);
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				press = Press.Button;
				pressedButton = valueOrDefault;
				DrawButtons();
				return;
			}
			if (layout.IsOnDownload(p))
			{
				press = Press.Download;
				downloadPressedUnderPointer = true;
				DrawDownload();
				return;
			}
			if (layout.IsFullScreen)
			{
				press = ((!layout.CardRect.Contains(p)) ? Press.OutsideFullScreenCard : Press.None);
				if (press == Press.None && flag)
				{
					Rect? titleBar = layout.TitleBar;
					if (titleBar.HasValue && titleBar.GetValueOrDefault().Contains(p))
					{
						press = Press.TitleBarDoubleClick;
					}
				}
				return;
			}
			if (flag && layout.CanGoFullScreen)
			{
				Rect? titleBar = layout.TitleBar;
				if (titleBar.HasValue && titleBar.GetValueOrDefault().Contains(p))
				{
					press = Press.TitleBarDoubleClick;
					return;
				}
			}
			if (!layout.DragsFrom(p))
			{
				press = Press.None;
				return;
			}
			press = Press.Drag;
			owner.BeginDrag(this);
		}

		private void Released(Vector2 p)
		{
			Press press = this.press;
			this.press = Press.None;
			switch (press)
			{
			case Press.Drag:
				owner.EndDrag();
				break;
			case Press.Button:
			{
				int? num = pressedButton;
				pressedButton = null;
				DrawButtons();
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					NativeEngine.DraggableCard card2 = Card;
					if ((object)card2 != null)
					{
						owner.PressWindowButton(valueOrDefault, card2);
					}
				}
				break;
			}
			case Press.Download:
			{
				bool num2 = downloadPressedUnderPointer;
				downloadPressedUnderPointer = false;
				DrawDownload();
				if (num2 && !savingDownload)
				{
					NativeEngine.DraggableCard card3 = Card;
					if ((object)card3 != null)
					{
						SaveDownload(card3);
					}
				}
				break;
			}
			case Press.OutsideFullScreenCard:
			case Press.TitleBarDoubleClick:
			{
				NativeEngine.DraggableCard card = Card;
				if ((object)card != null)
				{
					owner.PressWindowButton(2, card);
				}
				break;
			}
			}
		}

		private void SaveDownload(NativeEngine.DraggableCard card)
		{
			if (owner.drag != null)
			{
				return;
			}
			savingDownload = true;
			bool num = owner.Download(card);
			savingDownload = false;
			if (num)
			{
				showsDownloadDone = true;
				DrawDownload();
				owner.After?.Invoke(1.6, delegate
				{
					showsDownloadDone = false;
					DrawDownload();
				});
			}
		}

		private void DrawDownload()
		{
			downloadVisual.Children.RemoveAll();
			Rect? downloadRect = layout.DownloadRect;
			if (downloadRect.HasValue)
			{
				Rect valueOrDefault = downloadRect.GetValueOrDefault();
				Compositor compositor = owner.compositor;
				ShapeVisual shapeVisual = compositor.CreateShapeVisual();
				shapeVisual.Size = new Vector2((float)((double)window.BoundsPx.Width / scale), (float)((double)window.BoundsPx.Height / scale));
				WindowButtonShapes.AddDownloadGlyph(compositor, shapeVisual.Shapes, (X: valueOrDefault.X, Y: valueOrDefault.Y, Width: valueOrDefault.Width, Height: valueOrDefault.Height), hoveringDownload, downloadPressedUnderPointer, showsDownloadDone);
				downloadVisual.Children.InsertAtTop(shapeVisual);
			}
		}

		private void SetRevealed(bool value, bool animated)
		{
			if (value != revealed)
			{
				revealed = value;
				if (!animated)
				{
					buttonsVisual.StopAnimation("Opacity");
					buttonsVisual.Opacity = (value ? 1 : 0);
					return;
				}
				ScalarKeyFrameAnimation scalarKeyFrameAnimation = owner.compositor.CreateScalarKeyFrameAnimation();
				scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
				scalarKeyFrameAnimation.InsertKeyFrame(1f, value ? 1 : 0, owner.painter.Easing(value ? new Vector4(0f, 0f, 0.58f, 1f) : new Vector4(0.42f, 0f, 0.58f, 1f)));
				scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(value ? 0.16 : 0.26);
				buttonsVisual.StartAnimation("Opacity", scalarKeyFrameAnimation);
			}
		}

		private void DrawButtons()
		{
			Compositor compositor = owner.compositor;
			buttonsVisual.Children.RemoveAll();
			if (layout.Buttons.Count == 0)
			{
				return;
			}
			ShapeVisual shapeVisual = compositor.CreateShapeVisual();
			shapeVisual.Size = new Vector2((float)((double)window.BoundsPx.Width / scale), (float)((double)window.BoundsPx.Height / scale));
			foreach (var button in layout.Buttons)
			{
				int item = button.Kind;
				Rect item2 = button.Rect;
				CompositionShapeCollection shapes = shapeVisual.Shapes;
				(double, double, double, double) rect = (item2.X, item2.Y, item2.Width, item2.Height);
				double buttonUnit = layout.ButtonUnit;
				int? num = pressedButton;
				WindowButtonShapes.AddCaption(compositor, shapes, item, rect, buttonUnit, !num.HasValue && hoveredButton == item, pressedButton == item, layout.IsFullScreen, layout.CornerRadius, layout.ButtonUnit);
			}
			buttonsVisual.Children.InsertAtTop(shapeVisual);
		}

		[DllImport("user32.dll")]
		private static extern uint GetDoubleClickTime();
	}

	public const string WindowTitle = "Clicky card";

	private const double MinimumVisibleUnits = 40.0;

	private readonly Compositor compositor;

	private readonly ScreenPainter painter;

	private readonly NativeEngine engine;

	private readonly OverlayWindow canvasWindow;

	private readonly ActionButtons buttons;

	private readonly List<Handle> handles = new List<Handle>();

	private Drag? drag;

	public Action? NothingLeft;

	public Action<string, float>? PlaySound;

	public PinnedCards? Pinned;

	public Func<DisplayInfo?>? CurrentDisplay;

	public Action<double, Action>? After;

	public Func<string>? DownloadsFolder;

	public string? LastDownloadPath { get; private set; }

	private double PixelsPerUnit => (painter.CanvasSize.X > 0f) ? ((float)canvasWindow.BoundsPx.Width / painter.CanvasSize.X) : 1f;

	public CardHandles(Compositor compositor, ScreenPainter painter, NativeEngine engine, OverlayWindow canvasWindow, ActionButtons buttons)
	{
		this.compositor = compositor;
		this.painter = painter;
		this.engine = engine;
		this.canvasWindow = canvasWindow;
		this.buttons = buttons;
	}

	private RectInt32 ScreenRect(RectD box, double inset = 0.0)
	{
		RectInt32 boundsPx = canvasWindow.BoundsPx;
		double pixelsPerUnit = PixelsPerUnit;
		return new RectInt32((int)Math.Floor((double)boundsPx.X + box.X * pixelsPerUnit - inset), (int)Math.Floor((double)boundsPx.Y + box.Y * pixelsPerUnit - inset), (int)Math.Ceiling(box.Width * pixelsPerUnit + 2.0 * inset), (int)Math.Ceiling(box.Height * pixelsPerUnit + 2.0 * inset));
	}

	public void Refresh()
	{
		if (drag == null && canvasWindow.IsShown)
		{
			List<NativeEngine.DraggableCard> list = (from c in engine.DraggableCards()
				where c.Box.Width >= 8.0 && c.Box.Height >= 8.0 && c.Units.Length != 0
				select c).ToList();
			NativeEngine.DraggableCard draggableCard = list.FirstOrDefault((NativeEngine.DraggableCard c) => c.Window?.IsFullScreen ?? false);
			if ((object)draggableCard != null)
			{
				list = new List<NativeEngine.DraggableCard> { draggableCard };
			}
			while (handles.Count > list.Count)
			{
				List<Handle> list2 = handles;
				list2[list2.Count - 1].Close();
				handles.RemoveAt(handles.Count - 1);
			}
			while (handles.Count < list.Count)
			{
				handles.Add(new Handle(this));
			}
			for (int i = 0; i < list.Count; i++)
			{
				NativeEngine.DraggableCard draggableCard2 = list[i];
				double rasterizationScale = canvasWindow.RasterizationScale;
				RectInt32 cardRect = ScreenRect(draggableCard2.Box);
				NativeEngine.CardWindow? window = draggableCard2.Window;
				RectInt32 frame = (((object)window != null && window.IsFullScreen) ? canvasWindow.BoundsPx : ScreenRect(draggableCard2.Box, 2.0 * rasterizationScale));
				handles[i].Place(draggableCard2, frame, BuildLayout(draggableCard2, cardRect, frame, rasterizationScale), rasterizationScale);
			}
			StackInDrawOrder();
			if (list.Count > 0)
			{
				Log.Info("Drawing", $"card handles: {list.Count} card(s) can be dragged");
			}
		}
	}

	private void StackInDrawOrder()
	{
		nint below = canvasWindow.Handle;
		foreach (Handle handle in handles)
		{
			if (handle.IsShown)
			{
				handle.StackJustAbove(below);
				below = handle.WindowHandle;
			}
		}
	}

	private Layout BuildLayout(NativeEngine.DraggableCard card, RectInt32 cardRect, RectInt32 frame, double scale)
	{
		Layout layout = new Layout
		{
			CardRect = R(cardRect)
		};
		NativeEngine.CardWindow window = card.Window;
		if ((object)window == null)
		{
			return layout;
		}
		double val = PixelsPerUnit / scale;
		double num = window.Scale / Math.Max(val, 0.0001);
		Rect value = R(ScreenRect(new RectD(window.Box.X, window.Box.Y, window.Box.Width, window.TitleBarHeight)));
		double num2 = value.Height / 28.0;
		double num3 = 40.0 * num2;
		double num4 = value.X + value.Width;
		List<(int, Rect)> list = new List<(int, Rect)>();
		int[] captionsFromRight = WindowButtonShapes.CaptionsFromRight;
		foreach (int num5 in captionsFromRight)
		{
			if (num5 != 2 || !window.Holds3DFigure || window.IsFullScreen)
			{
				num4 -= num3;
				list.Add((num5, new Rect(num4, value.Y, num3, value.Height)));
			}
		}
		layout.Buttons = list;
		layout.ButtonUnit = num2;
		layout.CornerRadius = 10.0 * num2;
		if (WindowButtonShapes.ShowsDownloadButton)
		{
			double num6 = window.Box.Right - (18.0 + (double)list.Count * 40.0) * num;
			double num7 = window.Box.Y + window.TitleBarHeight / 2.0;
			double num8 = 20.0 * window.Scale;
			double num9 = ((double)canvasWindow.BoundsPx.X + num6 * PixelsPerUnit - (double)frame.X) / scale;
			double num10 = ((double)canvasWindow.BoundsPx.Y + num7 * PixelsPerUnit - (double)frame.Y) / scale;
			layout.DownloadRect = new Rect(num9 - num8 / 2.0, num10 - num8 / 2.0, num8, num8);
		}
		layout.TitleBar = value;
		layout.IsFullScreen = window.IsFullScreen;
		layout.CanGoFullScreen = !window.Holds3DFigure || window.IsFullScreen;
		return layout;
		Rect R(RectInt32 r)
		{
			return new Rect((double)(r.X - frame.X) / scale, (double)(r.Y - frame.Y) / scale, (double)r.Width / scale, (double)r.Height / scale);
		}
	}

	public void Hide()
	{
		CancelDrag();
		foreach (Handle handle in handles)
		{
			handle.Hide();
		}
	}

	public void Dispose()
	{
		CancelDrag();
		foreach (Handle handle in handles)
		{
			handle.Close();
		}
		handles.Clear();
	}

	private void BeginDrag(Handle handle)
	{
		CancelDrag();
		NativeMethods.GetCursorPos(out var point);
		RectInt32 boundsPx = canvasWindow.BoundsPx;
		Vector2 point2 = new Vector2((float)((double)(point.X - boundsPx.X) / PixelsPerUnit), (float)((double)(point.Y - boundsPx.Y) / PixelsPerUnit));
		NativeEngine.DraggableCard draggableCard = (from c in engine.DraggableCards()
			where c.Units.Length != 0 && (double)point2.X >= c.Box.X - 6.0 && (double)point2.X <= c.Box.Right + 6.0 && (double)point2.Y >= c.Box.Y - 6.0 && (double)point2.Y <= c.Box.Bottom + 6.0
			orderby c.Box.Width * c.Box.Height
			select c).FirstOrDefault() ?? handle.Card;
		if ((object)draggableCard != null)
		{
			drag = new Drag
			{
				Card = draggableCard,
				Handle = handle,
				StartPointer = point,
				HandleStart = handle.Frame
			};
			handle.BringToFront();
			painter.BeginCardDrag(draggableCard.Units);
			buttons.BeginRide(draggableCard.Marks, ScreenRect(draggableCard.Box));
		}
	}

	private void ContinueDrag()
	{
		if (drag != null)
		{
			NativeMethods.GetCursorPos(out var point);
			double pixelsPerUnit = PixelsPerUnit;
			double val = (double)(point.X - drag.StartPointer.X) / pixelsPerUnit;
			double val2 = (double)(point.Y - drag.StartPointer.Y) / pixelsPerUnit;
			RectD box = drag.Card.Box;
			Vector2 canvasSize = painter.CanvasSize;
			val = Math.Min(Math.Max(val, 40.0 - box.Right), (double)canvasSize.X - 40.0 - box.X);
			val2 = Math.Min(Math.Max(val2, 40.0 - box.Bottom), (double)canvasSize.Y - 40.0 - box.Y);
			drag.Offset = new Vector2((float)val, (float)val2);
			painter.MoveCardDrag(drag.Offset);
			int num = (int)Math.Round(val * pixelsPerUnit);
			int num2 = (int)Math.Round(val2 * pixelsPerUnit);
			drag.Handle.MoveTo(drag.HandleStart.X + num, drag.HandleStart.Y + num2);
			buttons.Ride(num, num2);
		}
	}

	private void EndDrag()
	{
		Drag drag = this.drag;
		if (drag != null)
		{
			this.drag = null;
			StackInDrawOrder();
			bool flag = Math.Abs(drag.Offset.X) >= 0.5f || Math.Abs(drag.Offset.Y) >= 0.5f;
			painter.EndCardDrag(flag);
			buttons.EndRide(flag);
			if (flag)
			{
				engine.MoveCard(drag.Card.Marks, drag.Offset.X, drag.Offset.Y);
				Log.Info("Drawing", $"the user moved a card ({drag.Card.Marks.Length} marks) by {drag.Offset.X:F0}, {drag.Offset.Y:F0}");
				Refresh();
			}
		}
	}

	private void CancelDrag()
	{
		Drag drag = this.drag;
		if (drag != null)
		{
			this.drag = null;
			painter.EndCardDrag();
			drag.Handle.MoveTo(drag.HandleStart.X, drag.HandleStart.Y);
			StackInDrawOrder();
			buttons.EndRide(keep: false);
		}
	}

	private void PressWindowButton(int kind, NativeEngine.DraggableCard card)
	{
		if (drag == null)
		{
			switch (kind)
			{
			case 0:
				CloseCard(card);
				break;
			case 1:
				PinCard(card);
				break;
			default:
				ToggleFullScreen(card);
				break;
			}
		}
	}

	private void PinCard(NativeEngine.DraggableCard card)
	{
		PinnedCards pinned = Pinned;
		if (pinned == null)
		{
			return;
		}
		DisplayInfo displayInfo = CurrentDisplay?.Invoke();
		if ((object)displayInfo == null)
		{
			return;
		}
		NativeEngine.CardWindow? window = card.Window;
		if ((object)window != null && window.IsFullScreen)
		{
			ToggleFullScreen(card);
			pinned.After(0.45, delegate
			{
				NativeEngine.DraggableCard draggableCard = engine.DraggableCards().FirstOrDefault((NativeEngine.DraggableCard c) => c.Marks.ToHashSet().SetEquals(card.Marks));
				if ((object)draggableCard != null)
				{
					PinCard(draggableCard);
				}
			});
			return;
		}
		double pixelsPerUnit = PixelsPerUnit;
		double num = canvasWindow.RasterizationScale / Math.Max(pixelsPerUnit, 0.0001);
		RectD rectD = card.Window?.Box ?? card.Box;
		RectD rectD2 = new RectD(rectD.X - num, rectD.Y - num, rectD.Width + 2.0 * num, rectD.Height + 2.0 * num);
		CanvasRenderTarget canvasRenderTarget;
		try
		{
			using CanvasRenderTarget image = painter.RenderSnapshot(DrawingThread.Now, (float)pixelsPerUnit);
			canvasRenderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), (float)Math.Ceiling(rectD2.Width * pixelsPerUnit), (float)Math.Ceiling(rectD2.Height * pixelsPerUnit), 96f);
			using CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession();
			canvasDrawingSession.Clear(Colors.Transparent);
			canvasDrawingSession.DrawImage(image, new Windows.Foundation.Rect(0.0, 0.0, canvasRenderTarget.Size.Width, canvasRenderTarget.Size.Height), new Windows.Foundation.Rect(rectD2.X * pixelsPerUnit, rectD2.Y * pixelsPerUnit, rectD2.Width * pixelsPerUnit, rectD2.Height * pixelsPerUnit));
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "pin: no picture of the card (" + ex.Message + ")");
			return;
		}
		RectInt32 sourceRectPx = ScreenRect(rectD2);
		painter.HideUnits(card.Units);
		buttons.RetireRiding(card.Marks, ScreenRect(card.Box));
		(int, int)? tuple = engine.CloseCard(card.Marks);
		NativeEngine.CardWindow window2 = card.Window;
		double titleBarFraction = (((object)window2 != null) ? (window2.TitleBarHeight / Math.Max(1.0, rectD2.Height)) : 0.0);
		pinned.Pin(canvasRenderTarget, sourceRectPx, card.Window?.Title ?? "", displayInfo, PinnedButtonsFor(card, rectD2), titleBarFraction);
		PlaySound?.Invoke("whoosh", 0.3f);
		Log.Info("Drawing", $"the user pinned a card to the edge of the screen ({tuple?.Item2 ?? (-1)} marks left on the drawing)");
		Refresh();
		if (tuple.HasValue && tuple.GetValueOrDefault().Item2 == 0 && !buttons.AnyShown)
		{
			NothingLeft?.Invoke();
		}
	}

	private List<(int Kind, RectD Fraction)> PinnedButtonsFor(NativeEngine.DraggableCard card, RectD pictureBox)
	{
		List<(int, RectD)> list = new List<(int, RectD)>();
		NativeEngine.CardWindow window = card.Window;
		if ((object)window == null)
		{
			return list;
		}
		double num = window.Scale / Math.Max(painter.PointsPerUnit, 0.0001);
		double num2 = 12.0 * num;
		int num3 = 0;
		int[] array = new int[3] { 0, 1, 2 };
		foreach (int item in array)
		{
			double num4 = window.Box.X + (16.0 + 20.0 * (double)num3) * num;
			double num5 = window.Box.Y + window.TitleBarHeight / 2.0;
			list.Add((item, new RectD((num4 - num2 / 2.0 - pictureBox.X) / pictureBox.Width, (num5 - num2 / 2.0 - pictureBox.Y) / pictureBox.Height, num2 / pictureBox.Width, num2 / pictureBox.Height)));
			num3++;
		}
		return list;
	}

	private void CloseCard(NativeEngine.DraggableCard card)
	{
		NativeEngine.CardWindow? window = card.Window;
		if ((object)window != null && window.IsFullScreen)
		{
			painter.EndIsolation(animated: false);
			buttons.SetTemporarilyHidden(hidden: false);
		}
		buttons.RetireRiding(card.Marks, ScreenRect(card.Box));
		(int, int)? tuple = engine.CloseCard(card.Marks);
		Log.Info("Drawing", $"the user closed a card ({tuple?.Item1 ?? 0} marks, {tuple?.Item2 ?? (-1)} left on the drawing)");
		PlaySound?.Invoke("page-clear", 0.35f);
		Refresh();
		if (tuple.HasValue && tuple.GetValueOrDefault().Item2 == 0 && !buttons.AnyShown)
		{
			NothingLeft?.Invoke();
		}
	}

	private void ToggleFullScreen(NativeEngine.DraggableCard card)
	{
		if ((object)card.Window == null)
		{
			return;
		}
		if (card.Window.IsFullScreen)
		{
			engine.ZoomCard(card.Marks, null);
			painter.EndIsolation();
			buttons.SetTemporarilyHidden(hidden: false);
			Log.Info("Drawing", "the user put a full-screen card back");
		}
		else
		{
			double num = Math.Max(painter.PointsPerUnit, 0.0001);
			double num2 = 64.0 / num;
			double num3 = 72.0 / num;
			double num4 = 64.0 / num;
			Vector2 canvasSize = painter.CanvasSize;
			RectD rectD = new RectD(num2, num3, (double)canvasSize.X - 2.0 * num2, (double)canvasSize.Y - num3 - num4);
			if (rectD.Width <= 0.0 || rectD.Height <= 0.0)
			{
				return;
			}
			double num5 = Math.Min(Math.Min(rectD.Width / card.Box.Width, rectD.Height / card.Box.Height), 3.0);
			double offsetX = rectD.X + rectD.Width / 2.0 - num5 * (card.Box.X + card.Box.Width / 2.0);
			double offsetY = rectD.Y + rectD.Height / 2.0 - num5 * (card.Box.Y + card.Box.Height / 2.0);
			painter.BeginIsolation(card.Units);
			engine.ZoomCard(card.Marks, num5, offsetX, offsetY);
			buttons.SetTemporarilyHidden(hidden: true);
			Log.Info("Drawing", $"the user showed a card full screen (x{num5:F2})");
		}
		PlaySound?.Invoke("slide", 0.3f);
		Refresh();
	}

	private bool Download(NativeEngine.DraggableCard card)
	{
		if (!canvasWindow.IsShown || painter.CanvasSize.X <= 0f)
		{
			return false;
		}
		double pixelsPerUnit = PixelsPerUnit;
		double num = Math.Max(painter.PointsPerUnit, 0.0001);
		RectD rectD = card.Window?.Box ?? card.Box;
		double num2 = card.Window?.Scale ?? 1.0;
		double num3 = 20.0 * num2 / num;
		double num4 = 12.0 * num2 / num;
		double num5 = 32.0 * num2 / num;
		Vector2 canvasSize = painter.CanvasSize;
		double num6 = Math.Max(0.0, rectD.X - num3);
		double num7 = Math.Max(0.0, rectD.Y - num4);
		double num8 = Math.Min(canvasSize.X, rectD.Right + num3);
		double num9 = Math.Min(canvasSize.Y, rectD.Bottom + num5);
		if (num8 - num6 < 1.0 || num9 - num7 < 1.0)
		{
			return false;
		}
		double rasterizationScale = canvasWindow.RasterizationScale;
		byte[] bytes;
		try
		{
			using CanvasRenderTarget image = painter.RenderUnitsSnapshot(card.Units, DrawingThread.Now, (float)pixelsPerUnit);
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			int num10 = (int)Math.Ceiling((num8 - num6) * pixelsPerUnit);
			int num11 = (int)Math.Ceiling((num9 - num7) * pixelsPerUnit);
			using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, num10, num11, 96f);
			using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
			{
				canvasDrawingSession.Clear(Colors.Transparent);
				canvasDrawingSession.DrawImage(image, new Windows.Foundation.Rect(0f, 0f, num10, num11), new Windows.Foundation.Rect(num6 * pixelsPerUnit, num7 * pixelsPerUnit, num10, num11));
			}
			using MemoryStream memoryStream = new MemoryStream();
			canvasRenderTarget.SaveAsync(memoryStream.AsRandomAccessStream(), CanvasBitmapFileFormat.Png).AsTask().GetAwaiter()
				.GetResult();
			bytes = PngWithResolution(memoryStream.ToArray(), 96.0 * rasterizationScale);
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "download: no picture of the card (" + ex.Message + ")");
			return false;
		}
		try
		{
			string obj = DownloadsFolder?.Invoke() ?? LessonVideoRecorder.DownloadsFolder();
			Directory.CreateDirectory(obj);
			string text = UniqueDownloadPath(obj, card.Window?.Title ?? "", DateTime.Now);
			File.WriteAllBytes(text, bytes);
			LastDownloadPath = text;
			Log.Info("Drawing", "download: saved " + Path.GetFileName(text));
			PlaySound?.Invoke("copy-tick", 0.35f);
			return true;
		}
		catch (Exception ex2)
		{
			Log.Warn("Drawing", "download: could not write the picture (" + ex2.Message + ")");
			return false;
		}
	}

	internal static string UniqueDownloadPath(string folder, string title, DateTime at)
	{
		HashSet<char> unsafeCharacters = Path.GetInvalidFileNameChars().Concat("/:\\?%*|\"<>").ToHashSet();
		string text = string.Join(" ", new string(title.Select((char ch) => (!unsafeCharacters.Contains(ch) && !char.IsControl(ch)) ? ch : ' ').ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries));
		if (text.Length > 60)
		{
			text = text.Substring(0, 60).TrimEnd();
		}
		string text2 = ((text.Length == 0) ? "HeyClicky drawing" : text) + " " + at.ToString("yyyy-MM-dd 'at' HH.mm.ss", CultureInfo.InvariantCulture);
		string text3 = Path.Combine(folder, text2 + ".png");
		int num = 2;
		while (File.Exists(text3))
		{
			text3 = Path.Combine(folder, $"{text2} {num}.png");
			num++;
		}
		return text3;
	}

	internal static byte[] PngWithResolution(byte[] png, double dotsPerInch)
	{
		if (png.Length < 33 || dotsPerInch <= 0.0)
		{
			return png;
		}
		uint value = (uint)Math.Round(dotsPerInch / 0.0254);
		byte[] array = new byte[9];
		BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(0), value);
		BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(4), value);
		array[8] = 1;
		int num;
		for (int i = 8; i + 12 <= png.Length; i += 12 + num)
		{
			num = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(i));
			Span<byte> span = png.AsSpan(i + 4, 4);
			if (span.SequenceEqual("IDAT"u8) || num < 0 || i + 12 + num > png.Length)
			{
				break;
			}
			if (span.SequenceEqual("pHYs"u8) && num == 9)
			{
				byte[] array2 = (byte[])png.Clone();
				array.CopyTo(array2, i + 8);
				BinaryPrimitives.WriteUInt32BigEndian(array2.AsSpan(i + 17), Crc32(array2.AsSpan(i + 4, 13)));
				return array2;
			}
		}
		byte[] array3 = new byte[8 + array.Length + 4];
		BinaryPrimitives.WriteUInt32BigEndian(array3.AsSpan(0), (uint)array.Length);
		"pHYs"u8.CopyTo(array3.AsSpan(4));
		array.CopyTo(array3, 8);
		BinaryPrimitives.WriteUInt32BigEndian(array3.AsSpan(8 + array.Length), Crc32(array3.AsSpan(4, 4 + array.Length)));
		byte[] array4 = new byte[png.Length + array3.Length];
		png.AsSpan(0, 33).CopyTo(array4);
		array3.CopyTo(array4, 33);
		png.AsSpan(33).CopyTo(array4.AsSpan(33 + array3.Length));
		return array4;
	}

	private static uint Crc32(ReadOnlySpan<byte> bytes)
	{
		uint num = uint.MaxValue;
		ReadOnlySpan<byte> readOnlySpan = bytes;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			num ^= b;
			for (int j = 0; j < 8; j++)
			{
				num = (((num & 1) != 0) ? (0xEDB88320u ^ (num >> 1)) : (num >> 1));
			}
		}
		return ~num;
	}

	internal void Simulate(int index, string what, Vector2 offset)
	{
		List<NativeEngine.DraggableCard> list = (from c in engine.DraggableCards()
			where c.Units.Length != 0
			select c).ToList();
		if (index >= list.Count || list.Count == 0)
		{
			Log.Warn("Drawing", $"simulate {what}: no card {index}");
			return;
		}
		NativeEngine.DraggableCard draggableCard = ((index < 0) ? (list.FirstOrDefault((NativeEngine.DraggableCard c) => (object)c.Window != null) ?? list[0]) : list[index]);
		switch (what)
		{
		case "drag":
			painter.BeginCardDrag(draggableCard.Units);
			painter.MoveCardDrag(offset);
			painter.EndCardDrag();
			engine.MoveCard(draggableCard.Marks, offset.X, offset.Y);
			break;
		case "close":
			CloseCard(draggableCard);
			break;
		case "zoom":
			ToggleFullScreen(draggableCard);
			break;
		case "pin":
			PinCard(draggableCard);
			break;
		case "download":
			Download(draggableCard);
			break;
		}
		Refresh();
	}

	internal List<RectInt32> ShowButtonsForHarness(int? hovered)
	{
		foreach (Handle handle in handles)
		{
			handle.ShowButtonsForHarness(hovered);
		}
		return handles.Select((Handle h) => h.Frame).ToList();
	}

	public void LeaveFullScreen()
	{
		if (!painter.IsIsolatingACard)
		{
			return;
		}
		foreach (NativeEngine.DraggableCard item in from c in engine.DraggableCards()
			where c.Window?.IsFullScreen ?? false
			select c)
		{
			engine.ZoomCard(item.Marks, null);
		}
		painter.EndIsolation();
		buttons.SetTemporarilyHidden(hidden: false);
	}
}

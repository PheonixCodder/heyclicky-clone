using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HeyClicky.Drawing.Interop;
using HeyClicky.Drawing.Painter;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Input;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Overlay;

internal sealed class PinnedCards : IDisposable
{
	private sealed class Card
	{
		public required CanvasRenderTarget Picture;

		public required CompositionSurfaceBrush Brush;

		public required Vector2 Size;

		public required string Title;

		public required List<(int Kind, RectD Fraction)> Buttons;

		public required double TitleBarFraction;

		public required OverlayWindow Window;

		public required ContainerVisual Body;

		public required ContainerVisual ButtonsVisual;

		public RectD Frame;

		public RectD From;

		public RectD To;

		public double MoveStart = -1.0;

		public bool IsExpanded;

		public bool IsHoveringButtons;

		public int? PressedButton;

		public bool PressStartedOnBody;

		public long LastPressTicks;
	}

	private sealed class Viewer
	{
		private readonly PinnedCards owner;

		private readonly Card card;

		private readonly OverlayWindow window;

		private readonly ContainerVisual body;

		private readonly ContainerVisual buttonsVisual;

		private readonly Vector2 bodySize;

		private bool hoveringButtons;

		private bool pressStartedBeside;

		private int? pressedButton;

		public Viewer(PinnedCards owner, Card card, DisplayInfo onDisplay)
		{
			this.owner = owner;
			this.card = card;
			Compositor compositor = owner.compositor;
			window = new OverlayWindow(compositor, "Clicky pinned card, big", interactive: true);
			RectInt32 boundsPx = new RectInt32((int)onDisplay.BoundsPx.X, (int)onDisplay.BoundsPx.Y, (int)onDisplay.BoundsPx.Width, (int)onDisplay.BoundsPx.Height);
			window.ShowOver(boundsPx);
			double rasterizationScale = window.RasterizationScale;
			SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.RelativeSizeAdjustment = Vector2.One;
			spriteVisual.Brush = compositor.CreateColorBrush(Color.FromArgb(107, 0, 0, 0));
			window.Root.Children.InsertAtTop(spriteVisual);
			double num = Math.Max(0.5, onDisplay.Scale);
			RectD rectD = new RectD((onDisplay.WorkAreaPx.X - onDisplay.BoundsPx.X) / rasterizationScale + 64.0, (onDisplay.WorkAreaPx.Y - onDisplay.BoundsPx.Y) / rasterizationScale + 56.0, onDisplay.WorkAreaPx.Width / rasterizationScale - 128.0, onDisplay.WorkAreaPx.Height / rasterizationScale - 112.0);
			double num2 = (double)card.Picture.SizeInPixels.Width / num;
			double num3 = Math.Min(Math.Min(rectD.Width / (double)card.Size.X, rectD.Height / (double)card.Size.Y), Math.Max(1.0, num2 / (double)card.Size.X));
			bodySize = new Vector2((float)((double)card.Size.X * num3), (float)((double)card.Size.Y * num3));
			body = compositor.CreateContainerVisual();
			body.Size = bodySize;
			body.Offset = new Vector3((float)(rectD.X + rectD.Width / 2.0 - (double)(bodySize.X / 2f)), (float)(rectD.Y + rectD.Height / 2.0 - (double)(bodySize.Y / 2f)), 0f);
			SpriteVisual spriteVisual2 = compositor.CreateSpriteVisual();
			spriteVisual2.RelativeSizeAdjustment = Vector2.One;
			spriteVisual2.Brush = card.Brush;
			DropShadow dropShadow = compositor.CreateDropShadow();
			dropShadow.BlurRadius = 24f;
			dropShadow.Offset = new Vector3(0f, 12f, 0f);
			dropShadow.Opacity = 0.3f;
			dropShadow.Mask = card.Brush;
			spriteVisual2.Shadow = dropShadow;
			body.Children.InsertAtTop(spriteVisual2);
			buttonsVisual = compositor.CreateContainerVisual();
			buttonsVisual.RelativeSizeAdjustment = Vector2.One;
			body.Children.InsertAtTop(buttonsVisual);
			window.Root.Children.InsertAtTop(body);
			window.Root.Opacity = 0f;
			owner.AnimateOpacity(window.Root, 1f, AnimationsAreReduced ? 0.0 : 0.22);
			window.PointerMoved += delegate(int x, int y)
			{
				Moved(Local(x, y));
			};
			window.PointerPressed += delegate(int x, int y)
			{
				Pressed(Local(x, y));
			};
			window.PointerReleased += delegate(int x, int y)
			{
				Released(Local(x, y));
			};
			DrawButtons();
			Log.Info("Drawing", "a pinned card is shown big");
		}

		private Vector2 Local(int x, int y)
		{
			double rasterizationScale = window.RasterizationScale;
			return new Vector2((float)((double)x / rasterizationScale - (double)body.Offset.X), (float)((double)y / rasterizationScale - (double)body.Offset.Y));
		}

		private bool IsOnCard(Vector2 point)
		{
			if (point.X >= 0f && point.Y >= 0f && point.X <= bodySize.X)
			{
				return point.Y <= bodySize.Y;
			}
			return false;
		}

		private void DrawButtons()
		{
			Compositor compositor = owner.compositor;
			buttonsVisual.Children.RemoveAll();
			ShapeVisual shapeVisual = compositor.CreateShapeVisual();
			shapeVisual.RelativeSizeAdjustment = Vector2.One;
			foreach (var item3 in ButtonRects(card.Buttons, bodySize))
			{
				int item = item3.Kind;
				(double, double, double, double) item2 = item3.Rect;
				CompositionShapeCollection shapes = shapeVisual.Shapes;
				bool pressed = pressedButton == item;
				int showsGlyph;
				if (!hoveringButtons)
				{
					int? num = pressedButton;
					showsGlyph = (num.HasValue ? 1 : 0);
				}
				else
				{
					showsGlyph = 1;
				}
				WindowButtonShapes.Add(compositor, shapes, item, item2, pressed, (byte)showsGlyph != 0, pointsInward: true, isPinned: true);
			}
			buttonsVisual.Children.InsertAtTop(shapeVisual);
		}

		private void Moved(Vector2 point)
		{
			bool flag = IsOverButtonGroup(card.Buttons, bodySize, point);
			if (flag != hoveringButtons)
			{
				hoveringButtons = flag;
				DrawButtons();
			}
			window.SetCursor(ButtonAt(card.Buttons, bodySize, point).HasValue ? InputSystemCursorShape.Hand : InputSystemCursorShape.Arrow);
		}

		private void Pressed(Vector2 point)
		{
			pressedButton = ButtonAt(card.Buttons, bodySize, point);
			int? num = pressedButton;
			pressStartedBeside = !num.HasValue && !IsOnCard(point);
			num = pressedButton;
			if (num.HasValue)
			{
				DrawButtons();
			}
		}

		private void Released(Vector2 point)
		{
			int? num = pressedButton;
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				pressedButton = null;
				DrawButtons();
				if (ButtonAt(card.Buttons, bodySize, point) == valueOrDefault)
				{
					Close(animated: true);
					if (valueOrDefault != 2)
					{
						owner.Unpin(card);
					}
				}
			}
			else
			{
				if (pressStartedBeside && !IsOnCard(point))
				{
					Close(animated: true);
				}
				pressStartedBeside = false;
			}
		}

		public void Close(bool animated)
		{
			if (owner.viewer == this)
			{
				owner.viewer = null;
			}
			double num = ((animated && !AnimationsAreReduced) ? 0.18 : 0.0);
			owner.AnimateOpacity(window.Root, 0f, num);
			owner.After(num + 0.02, window.Dispose);
		}
	}

	[CompilerGenerated]
	private sealed class _003CButtonRects_003Ed__53 : IEnumerable<(int Kind, (double X, double Y, double Width, double Height) Rect)>, IEnumerable, IEnumerator<(int Kind, (double X, double Y, double Width, double Height) Rect)>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private (int Kind, (double X, double Y, double Width, double Height) Rect) _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private List<(int Kind, RectD Fraction)> buttons;

		public List<(int Kind, RectD Fraction)> _003C_003E3__buttons;

		private Vector2 bodySize;

		public Vector2 _003C_003E3__bodySize;

		private int _003Cslot_003E5__2;

		private int[] _003C_003E7__wrap2;

		private int _003C_003E7__wrap3;

		(int Kind, (double X, double Y, double Width, double Height) Rect) IEnumerator<(int, (double, double, double, double))>.Current
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
		public _003CButtonRects_003Ed__53(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E7__wrap2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				goto IL_0110;
			}
			_003C_003E1__state = -1;
			_003Cslot_003E5__2 = 0;
			_003C_003E7__wrap2 = WindowButtonShapes.Shown;
			_003C_003E7__wrap3 = 0;
			goto IL_011e;
			IL_0110:
			_003C_003E7__wrap3++;
			goto IL_011e;
			IL_011e:
			if (_003C_003E7__wrap3 < _003C_003E7__wrap2.Length)
			{
				int item = _003C_003E7__wrap2[_003C_003E7__wrap3];
				RectD rectD = ((buttons.Count > _003Cslot_003E5__2) ? buttons[_003Cslot_003E5__2].Fraction : default(RectD));
				_003Cslot_003E5__2++;
				if (!(rectD.Width <= 0.0))
				{
					_003C_003E2__current = (Kind: item, Rect: (rectD.X * (double)bodySize.X, rectD.Y * (double)bodySize.Y, rectD.Width * (double)bodySize.X, rectD.Height * (double)bodySize.Y));
					_003C_003E1__state = 1;
					return true;
				}
				goto IL_0110;
			}
			_003C_003E7__wrap2 = null;
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<(int Kind, (double X, double Y, double Width, double Height) Rect)> IEnumerable<(int, (double, double, double, double))>.GetEnumerator()
		{
			_003CButtonRects_003Ed__53 _003CButtonRects_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CButtonRects_003Ed__ = this;
			}
			else
			{
				_003CButtonRects_003Ed__ = new _003CButtonRects_003Ed__53(0);
			}
			_003CButtonRects_003Ed__.buttons = _003C_003E3__buttons;
			_003CButtonRects_003Ed__.bodySize = _003C_003E3__bodySize;
			return _003CButtonRects_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<(int, (double, double, double, double))>)this).GetEnumerator();
		}
	}

	private const double PeekWidth = 30.0;

	private const double TuckedMaximumWidth = 150.0;

	private const double TuckedMaximumHeight = 110.0;

	private const double TuckedGap = 10.0;

	private const double StackTopInset = 56.0;

	private const double ExpandedMaximumWidth = 400.0;

	private const double ExpandedMaximumHeightFraction = 0.7;

	private const double ExpandedEdgeInset = 18.0;

	private const double ShadowRoom = 14.0;

	private const int MaximumPinnedCards = 8;

	private const double LayoutSeconds = 0.32;

	private static readonly Vector4 LayoutCurve = new Vector4(0.2f, 0.9f, 0.25f, 1f);

	private readonly DrawingThread thread;

	private readonly Compositor compositor;

	private readonly ScreenPainter painter;

	private readonly CompositionGraphicsDevice graphicsDevice;

	private readonly List<Card> cards = new List<Card>();

	private Card? expanded;

	private int pointerWatchTimer;

	private double pointerLeftExpandedCardAt = -1.0;

	private OverlayWindow? flightWindow;

	private Viewer? viewer;

	private DisplayInfo? display;

	internal bool IgnoresPointerForHarness;

	private readonly List<Card> departing = new List<Card>();

	public int Count => cards.Count;

	private double Scale => Math.Max(0.5, display?.Scale ?? 1.0);

	private RectD WorkArea
	{
		get
		{
			DisplayInfo displayInfo = display;
			if ((object)displayInfo == null)
			{
				return new RectD(0.0, 0.0, 1440.0, 900.0);
			}
			double scale = Scale;
			return new RectD((displayInfo.WorkAreaPx.X - displayInfo.BoundsPx.X) / scale, (displayInfo.WorkAreaPx.Y - displayInfo.BoundsPx.Y) / scale, displayInfo.WorkAreaPx.Width / scale, displayInfo.WorkAreaPx.Height / scale);
		}
	}

	internal static bool AnimationsAreReduced
	{
		get
		{
			if (SystemParametersInfo(4162u, 0u, out var value, 0u))
			{
				return value == 0;
			}
			return false;
		}
	}

	public PinnedCards(DrawingThread thread, Compositor compositor, ScreenPainter painter)
	{
		this.thread = thread;
		this.compositor = compositor;
		this.painter = painter;
		graphicsDevice = SystemComposition.GraphicsDevice(compositor, CanvasDevice.GetSharedDevice());
	}

	internal void Simulate(string what)
	{
		if (cards.Count == 0)
		{
			Log.Warn("Drawing", "simulate pinned " + what + ": nothing pinned");
			return;
		}
		Card card = cards[0];
		switch (what)
		{
		case "expand":
			Expand(card);
			break;
		case "tuck":
			Tuck(card);
			break;
		case "big":
			ShowBig(card);
			break;
		case "unbig":
			viewer?.Close(animated: true);
			break;
		case "unpin":
			Unpin(card);
			break;
		}
	}

	public void After(double seconds, Action action)
	{
		thread.ScheduleTimer(DrawingThread.Now + seconds, action);
	}

	public void Pin(CanvasRenderTarget picture, RectInt32 sourceRectPx, string title, DisplayInfo onDisplay, List<(int Kind, RectD Fraction)> buttons, double titleBarFraction)
	{
		double num = Math.Max(0.5, onDisplay.Scale);
		Vector2 size = new Vector2((float)((double)sourceRectPx.Width / num), (float)((double)sourceRectPx.Height / num));
		if (size.X <= 4f || size.Y <= 4f)
		{
			picture.Dispose();
			return;
		}
		display = onDisplay;
		Card card = CreateCard(picture, size, title, buttons, titleBarFraction);
		cards.Insert(0, card);
		while (cards.Count > 8)
		{
			List<Card> list = cards;
			RemoveCard(list[list.Count - 1]);
		}
		LayOut(animated: true, card);
		card.Frame = (card.From = (card.To = TuckedFrame(card, 0)));
		RectInt32 destinationPx = ScreenRect(card.Frame);
		Fly(card, sourceRectPx, destinationPx, delegate
		{
			if (cards.Contains(card))
			{
				Place(card);
				Settle(card);
			}
		});
	}

	private Card CreateCard(CanvasRenderTarget picture, Vector2 size, string title, List<(int, RectD)> buttons, double titleBarFraction)
	{
		CompositionDrawingSurface surface = graphicsDevice.CreateDrawingSurface(new Size(picture.SizeInPixels.Width, picture.SizeInPixels.Height), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
		using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(surface))
		{
			CanvasDrawingSession session = surfaceDrawing.Session;
			session.Clear(Colors.Transparent);
			session.DrawImage(picture);
		}
		CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(surface);
		compositionSurfaceBrush.Stretch = CompositionStretch.Fill;
		OverlayWindow overlayWindow = new OverlayWindow(compositor, "Clicky pinned card", interactive: true);
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.RelativeSizeAdjustment = Vector2.One;
		spriteVisual.Brush = compositionSurfaceBrush;
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.BlurRadius = 10f;
		dropShadow.Offset = new Vector3(0f, 6f, 0f);
		dropShadow.Opacity = 0.2f;
		dropShadow.Mask = compositionSurfaceBrush;
		spriteVisual.Shadow = dropShadow;
		containerVisual.Children.InsertAtTop(spriteVisual);
		ContainerVisual containerVisual2 = compositor.CreateContainerVisual();
		containerVisual2.RelativeSizeAdjustment = Vector2.One;
		containerVisual.Children.InsertAtTop(containerVisual2);
		overlayWindow.Root.Children.InsertAtTop(containerVisual);
		Card card = new Card
		{
			Picture = picture,
			Brush = compositionSurfaceBrush,
			Size = size,
			Title = title,
			Buttons = buttons,
			TitleBarFraction = titleBarFraction,
			Window = overlayWindow,
			Body = containerVisual,
			ButtonsVisual = containerVisual2
		};
		overlayWindow.PointerMoved += delegate(int x, int y)
		{
			Moved(card, LocalPoint(card, x, y));
		};
		overlayWindow.PointerLeft += delegate
		{
			Left(card);
		};
		overlayWindow.PointerPressed += delegate(int x, int y)
		{
			Pressed(card, LocalPoint(card, x, y));
		};
		overlayWindow.PointerReleased += delegate(int x, int y)
		{
			Released(card, LocalPoint(card, x, y));
		};
		return card;
	}

	private void Unpin(Card card)
	{
		if (cards.Contains(card))
		{
			if (expanded == card)
			{
				expanded = null;
				StopWatchingPointer();
			}
			card.From = card.Frame;
			card.To = new RectD(card.Frame.X + card.Frame.Width + 40.0, card.Frame.Y, card.Frame.Width, card.Frame.Height);
			AnimateOpacity(card.Body, 0f, AnimationsAreReduced ? 0.0 : 0.22);
			cards.Remove(card);
			card.MoveStart = (AnimationsAreReduced ? (-1.0) : DrawingThread.Now);
			departing.Add(card);
			LayOut(animated: true);
			After(AnimationsAreReduced ? 0.0 : 0.24, delegate
			{
				departing.Remove(card);
				DisposeCard(card);
			});
			Log.Info("Drawing", $"a pinned card was unpinned ({cards.Count} left)");
		}
	}

	private void RemoveCard(Card card)
	{
		cards.Remove(card);
		if (expanded == card)
		{
			expanded = null;
		}
		DisposeCard(card);
	}

	private static void DisposeCard(Card card)
	{
		card.Window.Dispose();
		card.Picture.Dispose();
	}

	private void Expand(Card card)
	{
		if (expanded == card)
		{
			pointerLeftExpandedCardAt = -1.0;
			return;
		}
		expanded = card;
		pointerLeftExpandedCardAt = -1.0;
		LayOut(animated: true);
		StartWatchingPointer();
	}

	private void Tuck(Card card)
	{
		if (expanded == card)
		{
			expanded = null;
			StopWatchingPointer();
			LayOut(animated: true);
		}
	}

	private void ShowBig(Card card)
	{
		DisplayInfo displayInfo = display;
		if ((object)displayInfo != null)
		{
			Tuck(card);
			viewer?.Close(animated: false);
			viewer = new Viewer(this, card, displayInfo);
		}
	}

	private void StartWatchingPointer()
	{
		if (pointerWatchTimer == 0)
		{
			pointerWatchTimer = thread.ScheduleTimer(DrawingThread.Now + 0.08, Check);
		}
		void Check()
		{
			pointerWatchTimer = 0;
			Card card = expanded;
			if (card != null)
			{
				pointerWatchTimer = thread.ScheduleTimer(DrawingThread.Now + 0.08, Check);
				int? pressedButton = card.PressedButton;
				if (pressedButton.HasValue || IgnoresPointerForHarness)
				{
					pointerLeftExpandedCardAt = -1.0;
				}
				else
				{
					NativeMethods.GetCursorPos(out var point);
					RectInt32 rectInt = ScreenRect(card.Frame);
					int num = (int)(24.0 * Scale);
					if (point.X >= rectInt.X - num && point.X <= rectInt.X + rectInt.Width + num && point.Y >= rectInt.Y - num && point.Y <= rectInt.Y + rectInt.Height + num)
					{
						pointerLeftExpandedCardAt = -1.0;
					}
					else
					{
						double now = DrawingThread.Now;
						if (pointerLeftExpandedCardAt < 0.0)
						{
							pointerLeftExpandedCardAt = now;
						}
						else if (now - pointerLeftExpandedCardAt > 0.35)
						{
							Tuck(card);
						}
					}
				}
			}
		}
	}

	private void StopWatchingPointer()
	{
		if (pointerWatchTimer != 0)
		{
			thread.CancelTimer(pointerWatchTimer);
		}
		pointerWatchTimer = 0;
		pointerLeftExpandedCardAt = -1.0;
	}

	private static double TuckedScale(Card card)
	{
		return Math.Min(Math.Min(150.0 / (double)card.Size.X, 110.0 / (double)card.Size.Y), 1.0);
	}

	private RectD TuckedFrame(Card card, int index)
	{
		RectD workArea = WorkArea;
		double num = workArea.Y + 56.0;
		foreach (Card item in cards.Take(index))
		{
			num += (double)item.Size.Y * TuckedScale(item) + 10.0;
		}
		double num2 = TuckedScale(card);
		return new RectD(workArea.Right - 30.0, num, (double)card.Size.X * num2, (double)card.Size.Y * num2);
	}

	private RectD ExpandedFrame(Card card, int index)
	{
		RectD workArea = WorkArea;
		double num = Math.Min(1.0, Math.Min(400.0 / (double)card.Size.X, workArea.Height * 0.7 / (double)card.Size.Y));
		double num2 = (double)card.Size.X * num;
		double num3 = (double)card.Size.Y * num;
		RectD rectD = TuckedFrame(card, index);
		double y = Math.Min(Math.Max(rectD.Y + rectD.Height / 2.0 - num3 / 2.0, workArea.Y + 8.0), Math.Max(workArea.Y + 8.0, workArea.Bottom - num3 - 8.0));
		return new RectD(workArea.Right - 18.0 - num2, y, num2, num3);
	}

	private void LayOut(bool animated, Card? except = null)
	{
		double now = DrawingThread.Now;
		for (int i = 0; i < cards.Count; i++)
		{
			Card card = cards[i];
			if (card == except)
			{
				continue;
			}
			bool flag = card == expanded;
			if (card.IsExpanded != flag)
			{
				card.IsExpanded = flag;
				if (!flag)
				{
					card.IsHoveringButtons = false;
					card.PressedButton = null;
				}
			}
			RectD rectD = (flag ? ExpandedFrame(card, i) : TuckedFrame(card, i));
			card.From = card.Frame;
			card.To = rectD;
			card.MoveStart = ((animated && !AnimationsAreReduced) ? now : (-1.0));
			if (card.MoveStart < 0.0)
			{
				card.Frame = rectD;
				Place(card);
			}
		}
		foreach (Card card2 in cards)
		{
			DrawButtons(card2);
		}
		if ((animated && !AnimationsAreReduced) || departing.Count > 0)
		{
			Step();
		}
	}

	private void Step()
	{
		double now = DrawingThread.Now;
		bool flag = false;
		foreach (Card item in cards.Concat(departing))
		{
			if (!(item.MoveStart < 0.0))
			{
				double num = Math.Clamp((now - item.MoveStart) / 0.32, 0.0, 1.0);
				double progress = CubicBezier(LayoutCurve, num);
				item.Frame = new RectD(Lerp(item.From.X, item.To.X, progress), Lerp(item.From.Y, item.To.Y, progress), Lerp(item.From.Width, item.To.Width, progress), Lerp(item.From.Height, item.To.Height, progress));
				if (num >= 1.0)
				{
					item.MoveStart = -1.0;
				}
				else
				{
					flag = true;
				}
				Place(item);
			}
		}
		if (flag)
		{
			thread.ScheduleTimer(now + 1.0 / 60.0, Step);
		}
	}

	private void Place(Card card)
	{
		DisplayInfo displayInfo = display;
		if ((object)displayInfo == null)
		{
			return;
		}
		double scale = Scale;
		RectInt32 rectInt = ScreenRect(card.Frame);
		int num = (int)Math.Ceiling(14.0 * scale);
		int num2 = Math.Max(rectInt.X - num, (int)displayInfo.BoundsPx.X);
		int num3 = Math.Max(rectInt.Y - num, (int)displayInfo.BoundsPx.Y);
		int num4 = Math.Min(rectInt.X + rectInt.Width + num, (int)(displayInfo.BoundsPx.X + displayInfo.BoundsPx.Width));
		int num5 = Math.Min(rectInt.Y + rectInt.Height + num, (int)(displayInfo.BoundsPx.Y + displayInfo.BoundsPx.Height));
		if (num4 - num2 < 2 || num5 - num3 < 2)
		{
			card.Window.Hide();
			return;
		}
		card.Window.ShowOver(new RectInt32(num2, num3, num4 - num2, num5 - num3));
		double rasterizationScale = card.Window.RasterizationScale;
		card.Body.Offset = new Vector3((float)((double)(rectInt.X - num2) / rasterizationScale), (float)((double)(rectInt.Y - num3) / rasterizationScale), 0f);
		card.Body.Size = new Vector2((float)((double)rectInt.Width / rasterizationScale), (float)((double)rectInt.Height / rasterizationScale));
		if (card.IsExpanded)
		{
			DrawButtons(card);
		}
	}

	private RectInt32 ScreenRect(RectD frame)
	{
		double scale = Scale;
		RectD rectD = display?.BoundsPx ?? default(RectD);
		return new RectInt32((int)Math.Round(rectD.X + frame.X * scale), (int)Math.Round(rectD.Y + frame.Y * scale), (int)Math.Round(frame.Width * scale), (int)Math.Round(frame.Height * scale));
	}

	private Vector2 LocalPoint(Card card, int x, int y)
	{
		double rasterizationScale = card.Window.RasterizationScale;
		return new Vector2((float)((double)x / rasterizationScale - (double)card.Body.Offset.X), (float)((double)y / rasterizationScale - (double)card.Body.Offset.Y));
	}

	[IteratorStateMachine(typeof(_003CButtonRects_003Ed__53))]
	private static IEnumerable<(int Kind, (double X, double Y, double Width, double Height) Rect)> ButtonRects(List<(int Kind, RectD Fraction)> buttons, Vector2 bodySize)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CButtonRects_003Ed__53(-2)
		{
			_003C_003E3__buttons = buttons,
			_003C_003E3__bodySize = bodySize
		};
	}

	private static int? ButtonAt(List<(int Kind, RectD Fraction)> buttons, Vector2 bodySize, Vector2 point)
	{
		foreach (var (value, tuple2) in ButtonRects(buttons, bodySize))
		{
			if ((double)point.X >= tuple2.Item1 - 5.0 && (double)point.X <= tuple2.Item1 + tuple2.Item3 + 5.0 && (double)point.Y >= tuple2.Item2 - 5.0 && (double)point.Y <= tuple2.Item2 + tuple2.Item4 + 5.0)
			{
				return value;
			}
		}
		return null;
	}

	private static bool IsOverButtonGroup(List<(int Kind, RectD Fraction)> buttons, Vector2 bodySize, Vector2 point)
	{
		List<(double, double, double, double)> list = (from b in ButtonRects(buttons, bodySize)
			select b.Rect).ToList();
		if (list.Count == 0)
		{
			return false;
		}
		double num = list.Min<(double, double, double, double)>(((double X, double Y, double Width, double Height) r) => r.X) - 4.0;
		double num2 = list.Min<(double, double, double, double)>(((double X, double Y, double Width, double Height) r) => r.Y) - 4.0;
		double num3 = list.Max<(double, double, double, double)>(((double X, double Y, double Width, double Height) r) => r.X + r.Width) + 4.0;
		double num4 = list.Max<(double, double, double, double)>(((double X, double Y, double Width, double Height) r) => r.Y + r.Height) + 4.0;
		if ((double)point.X >= num && (double)point.X <= num3 && (double)point.Y >= num2)
		{
			return (double)point.Y <= num4;
		}
		return false;
	}

	private void DrawButtons(Card card)
	{
		card.ButtonsVisual.Children.RemoveAll();
		if (!card.IsExpanded)
		{
			return;
		}
		ShapeVisual shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.RelativeSizeAdjustment = Vector2.One;
		foreach (var item3 in ButtonRects(card.Buttons, card.Body.Size))
		{
			int item = item3.Kind;
			(double, double, double, double) item2 = item3.Rect;
			Compositor obj = compositor;
			CompositionShapeCollection shapes = shapeVisual.Shapes;
			bool pressed = card.PressedButton == item;
			int showsGlyph;
			if (!card.IsHoveringButtons)
			{
				int? pressedButton = card.PressedButton;
				showsGlyph = (pressedButton.HasValue ? 1 : 0);
			}
			else
			{
				showsGlyph = 1;
			}
			WindowButtonShapes.Add(obj, shapes, item, item2, pressed, (byte)showsGlyph != 0, pointsInward: false, isPinned: true);
		}
		card.ButtonsVisual.Children.InsertAtTop(shapeVisual);
	}

	private bool IsOnCard(Card card, Vector2 point)
	{
		if (point.X >= 0f && point.Y >= 0f && point.X <= card.Body.Size.X)
		{
			return point.Y <= card.Body.Size.Y;
		}
		return false;
	}

	private void Moved(Card card, Vector2 point)
	{
		if (!IsOnCard(card, point))
		{
			Left(card);
			return;
		}
		if (!card.IsExpanded)
		{
			Expand(card);
		}
		bool flag = card.IsExpanded && IsOverButtonGroup(card.Buttons, card.Body.Size, point);
		if (flag != card.IsHoveringButtons)
		{
			card.IsHoveringButtons = flag;
			DrawButtons(card);
		}
		card.Window.SetCursor(InputSystemCursorShape.Hand);
	}

	private void Left(Card card)
	{
		if (card.IsHoveringButtons)
		{
			card.IsHoveringButtons = false;
			DrawButtons(card);
		}
	}

	private void Pressed(Card card, Vector2 point)
	{
		long tickCount = Environment.TickCount64;
		bool flag = tickCount - card.LastPressTicks <= GetDoubleClickTime();
		card.LastPressTicks = tickCount;
		if (card.IsExpanded)
		{
			int? num = ButtonAt(card.Buttons, card.Body.Size, point);
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				card.PressedButton = valueOrDefault;
				card.PressStartedOnBody = false;
				DrawButtons(card);
				return;
			}
		}
		card.PressStartedOnBody = true;
		if (flag && card.IsExpanded && (double)point.Y <= card.TitleBarFraction * (double)card.Body.Size.Y + 2.0)
		{
			card.PressStartedOnBody = false;
			ShowBig(card);
		}
	}

	private void Released(Card card, Vector2 point)
	{
		int? pressedButton = card.PressedButton;
		if (pressedButton.HasValue)
		{
			int valueOrDefault = pressedButton.GetValueOrDefault();
			card.PressedButton = null;
			DrawButtons(card);
			if (ButtonAt(card.Buttons, card.Body.Size, point) == valueOrDefault)
			{
				if (valueOrDefault == 2)
				{
					ShowBig(card);
				}
				else
				{
					Unpin(card);
				}
			}
		}
		else
		{
			if (card.PressStartedOnBody && IsOnCard(card, point) && !card.IsExpanded)
			{
				Expand(card);
			}
			card.PressStartedOnBody = false;
		}
	}

	private void Fly(Card card, RectInt32 sourcePx, RectInt32 destinationPx, Action landed)
	{
		if (!AnimationsAreReduced)
		{
			DisplayInfo displayInfo = display;
			if ((object)displayInfo != null)
			{
				Fly(card.Brush, displayInfo, sourcePx, destinationPx, 0.55, vanishes: false, landed);
				return;
			}
		}
		landed();
	}

	public void FlyIntoNotch(CanvasBitmap picture, RectInt32 sourcePx, DisplayInfo onDisplay, Action completion)
	{
		if (AnimationsAreReduced)
		{
			picture.Dispose();
			completion();
			return;
		}
		double num = Math.Max(0.5, onDisplay.Scale);
		double num2 = 84.0;
		double num3 = Math.Max(14.0, num2 * (double)sourcePx.Height / (double)Math.Max(sourcePx.Width, 1));
		RectD boundsPx = onDisplay.BoundsPx;
		RectInt32 destinationPx = new RectInt32((int)Math.Round(boundsPx.X + boundsPx.Width / 2.0 - num2 * num / 2.0), (int)Math.Round(boundsPx.Y + (18.0 - num3 / 2.0) * num), (int)Math.Round(num2 * num), (int)Math.Round(num3 * num));
		CompositionDrawingSurface surface = graphicsDevice.CreateDrawingSurface(new Size(picture.SizeInPixels.Width, picture.SizeInPixels.Height), DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
		using (SystemComposition.SurfaceDrawing surfaceDrawing = SystemComposition.Draw(surface))
		{
			CanvasDrawingSession session = surfaceDrawing.Session;
			session.Clear(Colors.Transparent);
			session.DrawImage(picture);
		}
		picture.Dispose();
		CompositionSurfaceBrush brush = compositor.CreateSurfaceBrush(surface);
		brush.Stretch = CompositionStretch.Fill;
		Fly(brush, onDisplay, sourcePx, destinationPx, 0.75, vanishes: true, delegate
		{
			brush.Dispose();
			surface.Dispose();
			completion();
		});
	}

	private void Fly(CompositionBrush pictureBrush, DisplayInfo d, RectInt32 sourcePx, RectInt32 destinationPx, double duration, bool vanishes, Action landed)
	{
		if (flightWindow == null)
		{
			flightWindow = new OverlayWindow(compositor, "Clicky pinned card flight");
		}
		RectInt32 area = new RectInt32((int)d.BoundsPx.X, (int)d.BoundsPx.Y, (int)d.BoundsPx.Width, (int)d.BoundsPx.Height);
		flightWindow.ShowOver(area);
		double rasterization = flightWindow.RasterizationScale;
		Vector2 vector = Local(sourcePx, center: true);
		Vector2 vector2 = Local(destinationPx, center: true);
		Vector2 value = new Vector2((float)((double)sourcePx.Width / rasterization), (float)((double)sourcePx.Height / rasterization));
		Vector2 vector3 = new Vector2((float)((double)destinationPx.Width / rasterization), (float)((double)destinationPx.Height / rasterization));
		SpriteVisual sprite = compositor.CreateSpriteVisual();
		sprite.Brush = pictureBrush;
		sprite.AnchorPoint = new Vector2(0.5f);
		sprite.Size = vector3;
		sprite.Offset = new Vector3(vector2, 0f);
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.BlurRadius = 14f;
		dropShadow.Offset = new Vector3(0f, 8f, 0f);
		dropShadow.Opacity = 0.25f;
		dropShadow.Mask = pictureBrush;
		sprite.Shadow = dropShadow;
		flightWindow.Root.Children.InsertAtTop(sprite);
		Vector4 curve = new Vector4(0.3f, 0f, 0.2f, 1f);
		float num = (float)Math.Min(80.0, Math.Max(30.0, (double)Vector2.Distance(vector, vector2) * 0.12));
		Vector2 vector4 = new Vector2((vector.X + vector2.X) / 2f, Math.Min(vector.Y, vector2.Y) - num);
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		Vector2KeyFrameAnimation vector2KeyFrameAnimation = compositor.CreateVector2KeyFrameAnimation();
		LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
		for (int i = 0; i <= 24; i++)
		{
			float num2 = (float)i / 24f;
			float num3 = (float)CubicBezier(curve, num2);
			Vector2 value2 = (1f - num3) * (1f - num3) * vector + 2f * (1f - num3) * num3 * vector4 + num3 * num3 * vector2;
			vector3KeyFrameAnimation.InsertKeyFrame(num2, new Vector3(value2, 0f), easingFunction);
			vector2KeyFrameAnimation.InsertKeyFrame(num2, Vector2.Lerp(value, vector3, num3), easingFunction);
		}
		TimeSpan duration2 = (vector2KeyFrameAnimation.Duration = TimeSpan.FromSeconds(duration));
		vector3KeyFrameAnimation.Duration = duration2;
		sprite.StartAnimation("Offset", vector3KeyFrameAnimation);
		sprite.StartAnimation("Size", vector2KeyFrameAnimation);
		if (vanishes)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
			ScalarKeyFrameAnimation scalarKeyFrameAnimation2 = compositor.CreateScalarKeyFrameAnimation();
			for (int j = 0; j <= 24; j++)
			{
				float num4 = (float)j / 24f;
				float num5 = (float)CubicBezier(curve, num4);
				scalarKeyFrameAnimation.InsertKeyFrame(num4, (num5 <= 0.7f) ? 1f : (1f - (num5 - 0.7f) / 0.3f), easingFunction);
				scalarKeyFrameAnimation2.InsertKeyFrame(num4, 0.25f * (1f - num5), easingFunction);
			}
			duration2 = (scalarKeyFrameAnimation2.Duration = TimeSpan.FromSeconds(duration));
			scalarKeyFrameAnimation.Duration = duration2;
			sprite.StartAnimation("Opacity", scalarKeyFrameAnimation);
			dropShadow.StartAnimation("Opacity", scalarKeyFrameAnimation2);
		}
		After(duration, delegate
		{
			try
			{
				flightWindow.Root.Children.Remove(sprite);
			}
			catch
			{
			}
			if (flightWindow.Root.Children.Count == 0)
			{
				flightWindow.Hide();
			}
			landed();
		});
		Vector2 Local(RectInt32 r, bool center)
		{
			return new Vector2((float)(((double)(r.X - area.X) + (center ? ((double)r.Width / 2.0) : 0.0)) / rasterization), (float)(((double)(r.Y - area.Y) + (center ? ((double)r.Height / 2.0) : 0.0)) / rasterization));
		}
	}

	private void Settle(Card card)
	{
		if (!AnimationsAreReduced)
		{
			ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
			scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
			scalarKeyFrameAnimation.InsertKeyFrame(0.4f, -9f, painter.Easing(new Vector4(0f, 0f, 0.58f, 1f)));
			scalarKeyFrameAnimation.InsertKeyFrame(1f, 0f, painter.Easing(new Vector4(0.42f, 0f, 1f, 1f)));
			scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.4);
			card.Body.StartAnimation("Translation.X", scalarKeyFrameAnimation);
		}
	}

	private void AnimateOpacity(Visual visual, float target, double seconds)
	{
		if (seconds <= 0.0)
		{
			visual.Opacity = target;
			return;
		}
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertExpressionKeyFrame(0f, "this.StartingValue");
		scalarKeyFrameAnimation.InsertKeyFrame(1f, target);
		scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(seconds);
		visual.StartAnimation("Opacity", scalarKeyFrameAnimation);
	}

	private static double Lerp(double from, double to, double progress)
	{
		return from + (to - from) * progress;
	}

	private static double CubicBezier(Vector4 curve, double x)
	{
		double num = 3f * curve.X;
		double num2 = (double)(3f * (curve.Z - curve.X)) - num;
		double num3 = 1.0 - num - num2;
		double num4 = 3f * curve.Y;
		double num5 = (double)(3f * (curve.W - curve.Y)) - num4;
		double num6 = 1.0 - num4 - num5;
		double num7 = x;
		for (int i = 0; i < 8; i++)
		{
			double num8 = ((num3 * num7 + num2) * num7 + num) * num7 - x;
			if (Math.Abs(num8) < 1E-06)
			{
				break;
			}
			double num9 = (3.0 * num3 * num7 + 2.0 * num2) * num7 + num;
			if (Math.Abs(num9) < 1E-06)
			{
				break;
			}
			num7 -= num8 / num9;
		}
		num7 = Math.Clamp(num7, 0.0, 1.0);
		return ((num6 * num7 + num5) * num7 + num4) * num7;
	}

	[DllImport("user32.dll")]
	private static extern bool SystemParametersInfo(uint action, uint parameter, out int value, uint winIni);

	[DllImport("user32.dll")]
	private static extern uint GetDoubleClickTime();

	public void Dispose()
	{
		StopWatchingPointer();
		viewer?.Close(animated: false);
		foreach (Card card in cards)
		{
			DisposeCard(card);
		}
		cards.Clear();
		flightWindow?.Dispose();
		graphicsDevice.Dispose();
	}
}

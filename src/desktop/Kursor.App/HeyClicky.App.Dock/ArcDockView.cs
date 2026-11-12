using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Interop;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace HeyClicky.App.Dock;

internal sealed class ArcDockView
{
	private sealed class Slot(string railId, string key, ArcCardView card, Grid holder, PoseMotion motion)
	{
		public bool Leaving;

		public bool IsNew = true;

		public DeckPose? Target;

		public string RailId { get; } = railId;

		public string Key { get; set; } = key;

		public ArcCardView Card { get; } = card;

		public Grid Holder { get; } = holder;

		public PoseMotion Motion { get; } = motion;
	}

	private sealed class MorePill
	{
		public readonly Grid Host = new Grid
		{
			Opacity = 0.0,
			IsHitTestVisible = false
		};

		public readonly PoseMotion Motion;

		public DeckMoreButton? Button;

		public int Count;

		public int Reveal;

		public DeckPose? Target;

		public bool Shows;

		public MorePill()
		{
			Motion = new PoseMotion(Host, null, Vector2.Zero);
		}
	}

	private readonly ArcDock dock;

	private readonly OverlayWindow backdropWindow;

	private readonly OverlayWindow deckWindow;

	private readonly Grid backdrop = new Grid
	{
		IsHitTestVisible = false
	};

	private readonly Canvas deck = new Canvas();

	private readonly Dictionary<string, Slot> slots = new Dictionary<string, Slot>();

	private readonly MorePill[] more;

	private readonly Pressable bringToFront = new Pressable
	{
		Visibility = Visibility.Collapsed
	};

	private int bringToFrontIndex = -1;

	private SpriteVisual? ellipse;

	private SpriteVisual? band;

	private List<DockWindowKey> items = new List<DockWindowKey>();

	private bool showing;

	private Displays.Monitor monitor;

	private double raster = 1.0;

	private LayoutReason? pendingReason;

	private bool motionQueued;

	private bool pendingJump;

	private readonly DispatcherQueue dq;

	private static nint aboveDeck;

	private bool? backdropShows;

	public bool IsShowing => showing;

	public Displays.Monitor Monitor => monitor;

	public IReadOnlyDictionary<string, ArcCardView> Cards => slots.Values.Where((Slot s) => !s.Leaving).ToDictionary((Slot s) => s.Key, (Slot s) => s.Card);

	public UIElement DeckRoot => deckWindow.Root;

	public nint DeckHwnd => deckWindow.Hwnd;

	public bool TakesPointer { get; private set; }

	public static Func<nint>? NotchHwnd { get; set; }

	public ArcDockView(ArcDock dock, bool excludeFromCapture)
	{
		ArcDockView arcDockView = this;
		this.dock = dock;
		dq = DispatcherQueue.GetForCurrentThread();
		backdropWindow = new OverlayWindow("HeyClicky Agents Arc", excludeFromCapture, clickThroughEverywhere: true);
		backdropWindow.Root.Children.Add(backdrop);
		backdropWindow.Root.IsHitTestVisible = false;
		backdrop.Opacity = 0.0;
		deckWindow = new OverlayWindow("HeyClicky Agents", excludeFromCapture);
		deckWindow.StayLayered();
		deckWindow.SetIgnoresMouseEvents(ignores: true);
		deckWindow.Root.RequestedTheme = ElementTheme.Light;
		deckWindow.Root.Children.Add(deck);
		more = new MorePill[2]
		{
			new MorePill(),
			new MorePill()
		};
		MorePill[] array = more;
		foreach (MorePill morePill in array)
		{
			deck.Children.Add(morePill.Host);
			Canvas.SetZIndex(morePill.Host, 200);
		}
		ToolTipService.SetToolTip(bringToFront, "Bring to the front");
		bringToFront.Click += delegate
		{
			if (arcDockView.bringToFrontIndex >= 0)
			{
				dock.ShowDeckWindow(arcDockView.bringToFrontIndex, "side_window_click");
			}
		};
		deck.Children.Add(bringToFront);
		Canvas.SetZIndex(bringToFront, 150);
		deckWindow.Root.PointerWheelChanged += delegate(object _, PointerRoutedEventArgs e)
		{
			PointerPoint currentPoint = e.GetCurrentPoint(arcDockView.deckWindow.Root);
			if (dock.HandleDeckScroll(currentPoint.Properties.MouseWheelDelta, currentPoint.Properties.IsHorizontalMouseWheel, currentPoint.Timestamp))
			{
				e.Handled = true;
			}
		};
		deckWindow.RawMessage += delegate(uint msg, nint _, nint _)
		{
			if ((msg == 26 || msg == 126 || msg == 736) ? true : false)
			{
				dock.NotchChanged();
			}
		};
		monitor = Displays.Primary();
		ScreenCaptureYield.Register(deckWindow.Hwnd);
		ScreenCaptureYield.Register(backdropWindow.Hwnd);
	}

	public void Show()
	{
		if (!showing)
		{
			showing = true;
			Place(dock.HostMonitor());
			backdropWindow.ShowNoActivate();
			backdropWindow.Root.IsHitTestVisible = false;
			deckWindow.ShowNoActivate();
			deckWindow.ClearRegion();
			deckWindow.SetIgnoresMouseEvents(!TakesPointer);
			KeepZOrder();
		}
	}

	public void Hide()
	{
		if (showing)
		{
			showing = false;
			SetTakesPointer(takes: false);
			backdropWindow.HideWindow();
			deckWindow.HideWindow();
		}
	}

	public void SetTakesPointer(bool takes)
	{
		TakesPointer = takes;
		deckWindow.SetIgnoresMouseEvents(!takes);
	}

	public void Place(Displays.Monitor on)
	{
		monitor = on;
		double width = (double)on.Bounds.Width / on.Scale;
		double height = (double)on.Bounds.Height / on.Scale;
		OverlayWindow[] array = new OverlayWindow[2] { backdropWindow, deckWindow };
		foreach (OverlayWindow obj in array)
		{
			obj.Root.Width = width;
			obj.Root.Height = height;
			obj.PlacePx(on.Bounds.Left, on.Bounds.Top, on.Bounds.Width, on.Bounds.Height);
		}
		deck.Width = width;
		deck.Height = height;
		KeepZOrder();
	}

	public void KeepZOrder()
	{
		if (showing)
		{
			nint num = NotchHwnd?.Invoke() ?? 0;
			Native.SetWindowPos(deckWindow.Hwnd, FullscreenGuard.TopmostInsertAfter(), 0, 0, 0, 0, 19u);
			if (num != 0)
			{
				Native.SetWindowPos(backdropWindow.Hwnd, num, 0, 0, 0, 0, 19u);
			}
			if (aboveDeck != 0)
			{
				Native.SetWindowPos(aboveDeck, FullscreenGuard.TopmostInsertAfter(), 0, 0, 0, 0, 19u);
			}
		}
	}

	public static void KeepAboveDeck(nint hwnd)
	{
		aboveDeck = hwnd;
		if (hwnd != 0)
		{
			Native.SetWindowPos(hwnd, FullscreenGuard.TopmostInsertAfter(), 0, 0, 0, 0, 19u);
		}
	}

	public void Sync(IReadOnlyList<DockWindowKey> next)
	{
		bool flag = !next.Select((DockWindowKey w) => w.RailId).SequenceEqual(items.Select((DockWindowKey w) => w.RailId));
		items = next.ToList();
		HashSet<string> ids = items.Select((DockWindowKey w) => w.RailId).ToHashSet();
		foreach (Slot item in slots.Values.Where((Slot s) => !ids.Contains(s.RailId) && !s.Leaving).ToList())
		{
			Remove(item);
		}
		foreach (DockWindowKey item2 in items)
		{
			ArcCardContent arcCardContent = dock.Content(item2);
			if (slots.TryGetValue(item2.RailId, out Slot value) && !value.Leaving)
			{
				value.Key = item2.Key;
				value.Card.Update(arcCardContent);
			}
			else
			{
				Add(item2, arcCardContent);
			}
		}
		Layout(flag ? LayoutReason.Items : LayoutReason.Hover);
	}

	public void RebuildCards()
	{
		foreach (Slot value in slots.Values)
		{
			deck.Children.Remove(value.Holder);
		}
		slots.Clear();
		foreach (DockWindowKey item in items)
		{
			Add(item, dock.Content(item));
		}
		Layout(LayoutReason.Items, animated: false);
	}

	private void Add(DockWindowKey window, ArcCardContent content)
	{
		if (slots.Remove(window.RailId, out Slot value))
		{
			deck.Children.Remove(value.Holder);
		}
		ArcCardView arcCardView = new ArcCardView(dock, content);
		Vector3 centerPoint = new Vector3(247f, 157f, 0f);
		Grid grid = new Grid
		{
			Width = 494.0,
			Height = 314.0,
			CenterPoint = centerPoint,
			Opacity = 0.0
		};
		Grid grid2 = new Grid();
		Visual elementVisual = ElementCompositionPreview.GetElementVisual(grid2);
		elementVisual.CenterPoint = centerPoint;
		grid2.Children.Add(arcCardView);
		grid.Children.Add(grid2);
		PoseMotion poseMotion = new PoseMotion(grid, elementVisual, new Vector2(centerPoint.X, centerPoint.Y));
		poseMotion.SetRaster(raster);
		slots[window.RailId] = new Slot(window.RailId, window.Key, arcCardView, grid, poseMotion);
		deck.Children.Add(grid);
	}

	private void Remove(Slot slot)
	{
		slot.Leaving = true;
		slot.Holder.IsHitTestVisible = false;
		DeckPose target = slot.Motion.Target;
		if (Anim.ReduceMotion || Anim.Instant)
		{
			slot.Motion.Jump(target with
			{
				Opacity = 0.0
			});
		}
		else
		{
			slot.Motion.SpringTo(target with
			{
				Scale = target.Scale * 0.82,
				Y = target.Y - 12.0,
				Opacity = 0.0
			}, new SwiftSpring(0.38, 0.86));
		}
		DockTimer.After(0.6, delegate
		{
			deck.Children.Remove(slot.Holder);
			if (slots.TryGetValue(slot.RailId, out Slot value) && value == slot)
			{
				slots.Remove(slot.RailId);
			}
		});
	}

	public void Layout(LayoutReason reason, bool animated = true)
	{
		ArcScreenMetrics? metrics = dock.Metrics;
		if (!metrics.HasValue)
		{
			return;
		}
		ArcScreenMetrics valueOrDefault = metrics.GetValueOrDefault();
		SetRaster(TopArcLayout.FrontCardSize(valueOrDefault.PanelWidth, valueOrDefault.PanelHeight).W / 494.0);
		List<DockWindowKey> list = LiveWindows();
		int count = list.Count;
		bool isOpenNow = dock.IsOpenNow;
		int num = DeckFocus.Clamped(dock.FocusIndex, count);
		int? obj;
		if (isOpenNow)
		{
			string key = dock.HoveredKey;
			if (key != null)
			{
				int num2 = list.FindIndex((DockWindowKey w) => w.Key == key);
				obj = ((num2 >= 0) ? new int?(num2) : ((int?)null));
				goto IL_00d2;
			}
		}
		obj = null;
		goto IL_00d2;
		IL_0278:
		int index;
		int? num3;
		PlaceBringToFront(index, num, num3, valueOrDefault);
		ArcRect rail;
		Dictionary<string, ArcRect> dictionary;
		ArcRect? card;
		dock.FramesChanged(rail, dictionary, card, LayoutMore(valueOrDefault, count, num, num3, isOpenNow));
		bool quick;
		LayoutBackdrop(valueOrDefault, isOpenNow, !animated, quick);
		if (!animated)
		{
			pendingJump = true;
		}
		QueueMotion(reason);
		KeepZOrder();
		return;
		IL_00d2:
		num3 = obj;
		quick = !isOpenNow && dock.ClosesInstantly;
		dictionary = new Dictionary<string, ArcRect>();
		rail = default(ArcRect);
		card = null;
		for (int i = 0; i < count; i++)
		{
			Slot slot = slots[list[i].RailId];
			ArcPlacement arcPlacement = TopArcLayout.Placement(i, num, num3, valueOrDefault, isOpenNow);
			ArcPlacement arcPlacement2 = TopArcLayout.Placement(i, num, num3, valueOrDefault, isOpen: true);
			bool flag = i == num3;
			bool flag2 = i == num;
			if (arcPlacement2.Opacity > 0.0)
			{
				rail = rail.Union(arcPlacement2.Frame);
				dictionary[slot.Key] = (flag ? arcPlacement2.Frame : TopArcLayout.VisibleFrame(i, num, num3, valueOrDefault));
				if (flag)
				{
					card = arcPlacement2.Frame;
				}
			}
			slot.Card.SetPresentation(!flag2 && !flag, flag && flag2, reason != LayoutReason.Open);
			slot.Card.IsHitTestVisible = flag && flag2;
			Canvas.SetZIndex(slot.Holder, flag ? 100 : ((int)(arcPlacement.Z * 10.0)));
			slot.Target = new DeckPose(arcPlacement.CenterX, arcPlacement.CenterY, arcPlacement.Scale, arcPlacement.Degrees, arcPlacement.Opacity);
		}
		if (num3.HasValue)
		{
			int valueOrDefault2 = num3.GetValueOrDefault();
			if (valueOrDefault2 != num)
			{
				index = valueOrDefault2;
				goto IL_0278;
			}
		}
		index = -1;
		goto IL_0278;
	}

	private List<DockWindowKey> LiveWindows()
	{
		Slot value;
		return items.Where((DockWindowKey w) => slots.TryGetValue(w.RailId, out value) && !value.Leaving).ToList();
	}

	private void SetRaster(double scale)
	{
		if (scale <= 0.0 || Math.Abs(scale - raster) < 1E-09)
		{
			return;
		}
		raster = scale;
		foreach (Slot value in slots.Values)
		{
			value.Motion.SetRaster(scale);
		}
	}

	private void QueueMotion(LayoutReason reason)
	{
		LayoutReason? layoutReason = pendingReason;
		if (layoutReason.HasValue)
		{
			LayoutReason valueOrDefault = layoutReason.GetValueOrDefault();
			if (Rank(reason) <= Rank(valueOrDefault))
			{
				goto IL_0032;
			}
		}
		pendingReason = reason;
		goto IL_0032;
		IL_0032:
		if (!motionQueued)
		{
			motionQueued = true;
			dq.TryEnqueue(DispatcherQueuePriority.High, MoveWindows);
		}
	}

	private static int Rank(LayoutReason reason)
	{
		return reason switch
		{
			LayoutReason.Open => 4, 
			LayoutReason.Focus => 3, 
			LayoutReason.Items => 2, 
			LayoutReason.Metrics => 1, 
			_ => 0, 
		};
	}

	private static SwiftSpring SpringFor(LayoutReason reason)
	{
		return reason switch
		{
			LayoutReason.Open => new SwiftSpring(0.42, 0.84), 
			LayoutReason.Focus => new SwiftSpring(0.36, 0.86), 
			LayoutReason.Metrics => new SwiftSpring(0.42, 0.86), 
			LayoutReason.Hover => new SwiftSpring(0.34, 0.84), 
			_ => new SwiftSpring(0.38, 0.86), 
		};
	}

	private void MoveWindows()
	{
		motionQueued = false;
		LayoutReason? layoutReason = pendingReason;
		if (!layoutReason.HasValue)
		{
			return;
		}
		LayoutReason valueOrDefault = layoutReason.GetValueOrDefault();
		pendingReason = null;
		bool num = pendingJump;
		pendingJump = false;
		bool isOpenNow = dock.IsOpenNow;
		bool flag = num || (!isOpenNow && dock.ClosesInstantly) || Anim.ReduceMotion || Anim.Instant;
		SwiftSpring next = SpringFor(valueOrDefault);
		List<DockWindowKey> list = LiveWindows();
		int num2 = DeckFocus.Clamped(dock.FocusIndex, list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			Slot slot = slots[list[i].RailId];
			DeckPose? target = slot.Target;
			if (!target.HasValue)
			{
				continue;
			}
			DeckPose valueOrDefault2 = target.GetValueOrDefault();
			if (slot.IsNew)
			{
				slot.IsNew = false;
				slot.Motion.Jump(valueOrDefault2 with
				{
					Opacity = 0.0
				});
				if (flag)
				{
					slot.Motion.Jump(valueOrDefault2);
				}
				else
				{
					slot.Motion.SpringTo(valueOrDefault2, new SwiftSpring(0.38, 0.86));
				}
				continue;
			}
			if (flag)
			{
				slot.Motion.Jump(valueOrDefault2);
				continue;
			}
			if (valueOrDefault == LayoutReason.Open && isOpenNow && !slot.Motion.IsMoving && slot.Motion.Target.Opacity == 0.0)
			{
				ArcScreenMetrics? metrics = dock.Metrics;
				if (metrics.HasValue)
				{
					ArcScreenMetrics valueOrDefault3 = metrics.GetValueOrDefault();
					ArcPlacement arcPlacement = TopArcLayout.Placement(i, num2, null, valueOrDefault3, isOpen: false);
					slot.Motion.Jump(new DeckPose(arcPlacement.CenterX, arcPlacement.CenterY, arcPlacement.Scale, arcPlacement.Degrees, arcPlacement.Opacity));
				}
			}
			double delay = ((valueOrDefault == LayoutReason.Open && isOpenNow) ? ((double)Math.Min(Math.Abs(i - num2), 3) * 0.04) : 0.0);
			slot.Motion.SpringTo(valueOrDefault2, next, delay);
		}
		SwiftSpring next2 = new SwiftSpring(0.36, 0.86);
		MorePill[] array = more;
		foreach (MorePill morePill in array)
		{
			DeckPose? target = morePill.Target;
			if (target.HasValue)
			{
				DeckPose valueOrDefault4 = target.GetValueOrDefault();
				if (!morePill.Motion.HasPose)
				{
					morePill.Motion.Jump(valueOrDefault4 with
					{
						Opacity = 0.0
					});
				}
				if (flag)
				{
					morePill.Motion.Jump(valueOrDefault4);
				}
				else
				{
					morePill.Motion.SpringTo(valueOrDefault4, next2);
				}
			}
		}
	}

	private void PlaceBringToFront(int index, int focus, int? hovered, ArcScreenMetrics arc)
	{
		bringToFrontIndex = index;
		if (index < 0)
		{
			bringToFront.Visibility = Visibility.Collapsed;
			return;
		}
		ArcRect frame = TopArcLayout.Placement(index, focus, hovered, arc, isOpen: true).Frame;
		Canvas.SetLeft(bringToFront, frame.X);
		Canvas.SetTop(bringToFront, frame.Y);
		bringToFront.Width = frame.W;
		bringToFront.Height = frame.H;
		bringToFront.Visibility = Visibility.Visible;
	}

	private List<ArcRect> LayoutMore(ArcScreenMetrics arc, int count, int focus, int? hovered, bool isOpen)
	{
		List<ArcRect> list = new List<ArcRect>();
		int num = 2;
		int num2 = Math.Max(0, focus - num);
		int num3 = Math.Max(0, count - 1 - (focus + num));
		for (int i = 0; i < 2; i++)
		{
			MorePill morePill = more[i];
			int num4 = ((i == 0) ? num2 : num3);
			int num5 = ((i != 0) ? 1 : (-1));
			if (num4 == 0)
			{
				morePill.Shows = false;
				morePill.Host.IsHitTestVisible = false;
				DeckPose? target = morePill.Target;
				if (target.HasValue)
				{
					DeckPose valueOrDefault = target.GetValueOrDefault();
					morePill.Target = valueOrDefault with
					{
						Opacity = 0.0
					};
				}
				continue;
			}
			morePill.Reveal = focus + num5;
			if ((object)morePill.Button == null || morePill.Count != num4)
			{
				int index = i;
				morePill.Count = num4;
				morePill.Button = new DeckMoreButton(num4, num5 < 0, (num5 < 0) ? "Show the windows on the left" : "Show the windows on the right", delegate
				{
					dock.ShowDeckWindow(more[index].Reveal, "more_pill");
				});
				morePill.Host.Children.Clear();
				morePill.Host.Children.Add(morePill.Button);
			}
			(double, double) tuple = TopArcLayout.MorePillCenter(TopArcLayout.Placement(focus + num5 * num, focus, hovered, arc, isOpen), num5, arc.PanelWidth);
			morePill.Button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			double num6 = Math.Max(60.0, morePill.Button.DesiredSize.Width);
			double num7 = 26.0;
			DeckPose deckPose = new DeckPose(tuple.Item1 - num6 / 2.0, tuple.Item2 - num7 / 2.0, 1.0, 0.0, isOpen ? 1 : 0);
			if (!morePill.Shows && morePill.Motion.HasPose)
			{
				morePill.Motion.Jump(deckPose with
				{
					Opacity = morePill.Motion.PoseAt(PoseMotion.Now).Opacity
				});
			}
			morePill.Shows = true;
			morePill.Target = deckPose;
			morePill.Host.IsHitTestVisible = isOpen;
			if (isOpen)
			{
				list.Add(new ArcRect(deckPose.X, deckPose.Y, num6, num7));
			}
		}
		return list;
	}

	private void LayoutBackdrop(ArcScreenMetrics arc, bool shows, bool instant, bool quick = false)
	{
		Compositor compositor = Ui.Compositor;
		double panelWidth = arc.PanelWidth;
		double num = TopArcLayout.ArcDepth(arc);
		if ((object)ellipse == null)
		{
			ContainerVisual containerVisual = compositor.CreateContainerVisual();
			ellipse = compositor.CreateSpriteVisual();
			CompositionRadialGradientBrush compositionRadialGradientBrush = compositor.CreateRadialGradientBrush();
			compositionRadialGradientBrush.EllipseCenter = new Vector2(0.5f, 0.5f);
			compositionRadialGradientBrush.EllipseRadius = new Vector2(0.5f, 0.5f);
			(float, double)[] array = new(float, double)[6]
			{
				(0f, 1.0),
				(0.38f, 0.96),
				(0.6f, 0.78),
				(0.8f, 0.42),
				(0.92f, 0.12),
				(1f, 0.0)
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (offset, alpha) = array[i];
				compositionRadialGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(offset, DS.Black(alpha)));
			}
			ellipse.Brush = compositionRadialGradientBrush;
			band = compositor.CreateSpriteVisual();
			CompositionLinearGradientBrush compositionLinearGradientBrush = compositor.CreateLinearGradientBrush();
			compositionLinearGradientBrush.StartPoint = new Vector2(0f, 0f);
			compositionLinearGradientBrush.EndPoint = new Vector2(0f, 1f);
			array = new(float, double)[3]
			{
				(0f, 1.0),
				(0.45f, 1.0),
				(1f, 0.0)
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (offset2, alpha2) = array[i];
				compositionLinearGradientBrush.ColorStops.Add(compositor.CreateColorGradientStop(offset2, DS.Black(alpha2)));
			}
			band.Brush = compositionLinearGradientBrush;
			containerVisual.Children.InsertAtTop(ellipse);
			containerVisual.Children.InsertAtTop(band);
			containerVisual.Clip = compositor.CreateInsetClip();
			ElementCompositionPreview.SetElementChildVisual(backdrop, containerVisual);
		}
		ElementCompositionPreview.GetElementChildVisual(backdrop).Size = new Vector2((float)panelWidth, (float)num);
		ellipse.Size = new Vector2((float)(panelWidth * 1.5), (float)(num * 2.0));
		ellipse.Offset = new Vector3((float)(panelWidth / 2.0 - panelWidth * 0.75), (float)(0.0 - num), 0f);
		band.Size = new Vector2((float)panelWidth, (float)(arc.MenuBarHeight + 56.0));
		backdrop.Width = panelWidth;
		backdrop.Height = num;
		backdrop.HorizontalAlignment = HorizontalAlignment.Left;
		backdrop.VerticalAlignment = VerticalAlignment.Top;
		if (backdropShows != shows || instant)
		{
			backdropShows = shows;
			if (instant)
			{
				Anim.Stop(backdrop, "Opacity");
				backdrop.Opacity = (shows ? 1 : 0);
			}
			else if (!shows && quick)
			{
				Anim.Opacity(backdrop, 0f, 0.2, Easing.EaseOut(Ui.Compositor));
			}
			else
			{
				Anim.Opacity(backdrop, shows ? 1 : 0, shows ? 0.32 : 0.26, Easing.EaseInOut(Ui.Compositor));
			}
		}
	}
}

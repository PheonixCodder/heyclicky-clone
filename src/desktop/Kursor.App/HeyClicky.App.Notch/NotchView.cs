using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.App.Services;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Telemetry;
using HeyClicky.Drawing;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.App.Notch;

public sealed class NotchView : INotchHost
{
	private sealed class Frame
	{
		public required NotchSurface Surface;

		public required Grid Root;

		public required Border Padded;

		public double W;

		public double H;

		public DispatcherQueueTimer? Removal;
	}

	private readonly Microsoft.UI.Composition.Compositor c;

	private readonly NotchSilhouette silhouette;

	private readonly NotchSystemLayer? systemLayer;

	private readonly NotchCornerGlow? glow;

	private readonly NotchWorkingSweep workingSweep;

	private readonly Canvas contentLayer;

	private readonly DispatcherQueue dq;

	private readonly DispatcherQueueTimer regionTimer;

	private readonly DispatcherQueueTimer glowQuiesceTimer;

	private readonly DispatcherQueueTimer rotationTimer;

	private readonly DispatcherQueueTimer zOrderTimer;

	private double envW = 820.0;

	private double envH = 820.0;

	private double contentScale = 1.0;

	private bool streamMode;

	private readonly ScaleTransform rootScale = new ScaleTransform();

	private Displays.Monitor monitor;

	private NotchPresentation current = new NotchPresentation.Idle();

	private (double W, double H) currentSize = (W: 60.0, H: 8.0);

	private Frame? active;

	private Frame? peekFrame;

	private nint previousForeground;

	private bool shown;

	private nint lastCoveringWindow;

	private bool ambientHidden;

	private readonly List<string> pendingFiles = new List<string>();

	private bool composerOpenedByDrag;

	private (double W, double H)? regionHold;

	private DateTimeOffset regionHoldUntil = DateTimeOffset.MinValue;

	private const double KeepOpenMarginDip = 24.0;

	private static readonly TimeSpan HoverExitCloseDelay = TimeSpan.FromSeconds(0.35);

	private DispatcherQueueTimer? hoverExitTimer;

	private DateTimeOffset? hoverExitOutsideSince;

	private (int X, int Y, int W, int H)? stickyRectPx;

	private (string? ExpandedReplyEntryId, double? FullReplyContentHeight) lastFullReply;

	private bool hasKeyboard;

	private long keyboardRequestedAt = -1500L;

	private const long KeyboardRequestGraceMs = 1500L;

	public AppServices Services { get; }

	public IAppActions Actions { get; }

	public NotchGeometry.SizeContext SizeContext { get; } = new NotchGeometry.SizeContext();

	public OverlayWindow Window { get; }

	public bool IsHovering { get; private set; }

	public nint WindowHandle => Window.Hwnd;

	public MultiAgentRotationCursor MultiAgentRotation { get; } = new MultiAgentRotationCursor();

	internal NotchWorkingSweep WorkingSweep => workingSweep;

	private double DrawnScale => monitor.Scale * contentScale;

	public double ContentScale => contentScale;

	public nint MonitorHandle => monitor.Handle;

	public Displays.Monitor Monitor => monitor;

	public bool IsAmbientHidden
	{
		get
		{
			if (ambientHidden)
			{
				return current is NotchPresentation.Idle;
			}
			return false;
		}
	}

	internal NotchSurface? ActiveSurfaceForTest => active?.Surface;

	internal (double W, double H) PanelSizeDip => (W: currentSize.W, H: currentSize.H);

	public NotchPresentation Current => current;

	public bool HasKeyboard => hasKeyboard;

	public Func<nint, bool> IsNonTypingWindow { get; set; } = (nint _) => false;

	public event Action<bool>? HoverChanged;

	public event Action? HoverDwellCompleted;

	public event Action? MultiAgentRotated;

	public event Action? FirstFrameShown;

	public event Action? IdleClicked;

	public event Action? RaisedToTop;

	public event Action<bool>? KeyboardChanged;

	public NotchView(AppServices services, IAppActions actions, bool excludeFromCapture)
	{
		Services = services;
		Actions = actions;
		dq = DispatcherQueue.GetForCurrentThread();
		Window = new OverlayWindow("Kursor Notch", excludeFromCapture);
		c = Ui.Compositor;
		monitor = Displays.UnderCursor();
		envH = NotchGeometry.EnvelopeHeight((double)monitor.Bounds.Height / monitor.Scale);
		SizeContext.ScreenHeightDip = (double)monitor.Bounds.Height / monitor.Scale;
		Grid root = Window.Root;
		root.Width = envW;
		root.Height = envH;
		root.HorizontalAlignment = HorizontalAlignment.Left;
		root.VerticalAlignment = VerticalAlignment.Top;
		silhouette = new NotchSilhouette(c, envW);
		silhouette.SetDeviceScale(monitor.Scale);
		Windows.UI.Composition.Compositor systemCompositor = TransparentBackdrop.SystemCompositor;
		NotchSystemShape notchSystemShape = new NotchSystemShape(systemCompositor, envW);
		silhouette.AttachTwin(notchSystemShape);
		try
		{
			glow = new NotchCornerGlow(systemCompositor, notchSystemShape.Props, notchSystemShape.ClipGeometry);
		}
		catch (Exception ex)
		{
			Log.Error("notch", "corner glow failed to build", ex);
			ClickyAnalytics.ReportInternalError("notch", "corner_glow", ex);
		}
		workingSweep = new NotchWorkingSweep(systemCompositor, notchSystemShape);
		systemLayer = NotchSystemLayer.Create(Window.Hwnd);
		if (systemLayer != null)
		{
			systemLayer.SetScale(monitor.Scale, envW, envH);
			systemLayer.Add(notchSystemShape.Root);
			systemLayer.Add(workingSweep.Root);
			if (glow != null)
			{
				systemLayer.Add(glow.Root);
			}
		}
		else
		{
			Grid grid = new Grid
			{
				IsHitTestVisible = false
			};
			Microsoft.UI.Composition.ContainerVisual containerVisual = c.CreateContainerVisual();
			containerVisual.RelativeSizeAdjustment = Vector2.One;
			containerVisual.Children.InsertAtTop(silhouette.Root);
			ElementCompositionPreview.SetElementChildVisual(grid, containerVisual);
			root.Children.Add(grid);
		}
		contentLayer = new Canvas
		{
			Background = Ui.Transparent,
			Width = envW,
			Height = envH
		};
		root.Children.Add(contentLayer);
		ElementCompositionPreview.GetElementVisual(contentLayer).Clip = c.CreateGeometricClip(silhouette.ClipGeometry);
		contentLayer.PointerEntered += delegate
		{
			StopHoverExitWatch();
			SetHover(hovering: true);
		};
		contentLayer.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				BeginHoverExit();
			}
		};
		contentLayer.PointerPressed += delegate
		{
			if (NotchGeometry.IsFloatingSliver(current))
			{
				this.IdleClicked?.Invoke();
			}
		};
		contentLayer.AllowDrop = true;
		contentLayer.DragEnter += OnDragEnter;
		contentLayer.DragOver += OnDragOver;
		contentLayer.DragLeave += delegate
		{
			active?.Surface.SetDropTargeting(on: false);
		};
		contentLayer.Drop += OnDrop;
		if (services.Presenter is NotchPresenter notchPresenter)
		{
			notchPresenter.HoverDwellCompleted += delegate
			{
				dq.TryEnqueue(delegate
				{
					if (IsHovering)
					{
						this.HoverDwellCompleted?.Invoke();
					}
				});
			};
		}
		rotationTimer = dq.CreateTimer();
		rotationTimer.IsRepeating = false;
		rotationTimer.Tick += delegate
		{
			RotateMultiAgentStrip();
		};
		regionTimer = dq.CreateTimer();
		regionTimer.IsRepeating = false;
		regionTimer.Tick += delegate
		{
			ApplyHeldRegion();
		};
		glowQuiesceTimer = dq.CreateTimer();
		glowQuiesceTimer.IsRepeating = false;
		glowQuiesceTimer.Interval = TimeSpan.FromSeconds(0.5);
		glowQuiesceTimer.Tick += delegate
		{
			glow?.Quiesce();
		};
		zOrderTimer = dq.CreateTimer();
		zOrderTimer.Interval = TimeSpan.FromSeconds(0.5);
		zOrderTimer.Tick += delegate
		{
			KeepOnTop("while shown");
		};
		HoverChanged += delegate(bool hovering)
		{
			if (hovering)
			{
				KeepOnTop("pointer in");
			}
		};
		Window.RawMessage += delegate(uint msg, nint w, nint l)
		{
			if (msg == 126 || msg == 736 || msg == 26)
			{
				dq.TryEnqueue(Reposition);
			}
			if (msg == 6)
			{
				bool activated = (((IntPtr)w).ToInt64() & 0xFFFF) != 0;
				dq.TryEnqueue(delegate
				{
					if (activated || !TakeKeyboardBackFrom(l))
					{
						SetHasKeyboard(activated);
					}
				});
			}
		};
		Services.Presenter.PresentationChanged += delegate
		{
			dq.TryEnqueue(delegate
			{
				Apply(Services.Presenter.Presentation);
			});
		};
		INotchPresenter presenter = Services.Presenter;
		NotchPresenter tabbed = presenter as NotchPresenter;
		if (tabbed != null)
		{
			SizeContext.PeekSize = () => NotchPeekLayout.PanelSize(tabbed.Tabs, NotchChat.HandOffs, NotchExtensions.HomeDock?.FrontCardSize);
			tabbed.Tabs.SelectionChanged += delegate(NotchTab tab)
			{
				dq.TryEnqueue(delegate
				{
					if (tab != NotchTab.Chat || tabbed.Tabs.ExpandedReplyEntryId == null || tabbed.Tabs.FullReplyContentHeight.HasValue)
					{
						RelayoutPeek();
					}
					else
					{
						DispatcherQueueTimer fallback = dq.CreateTimer().Rooted();
						fallback.Interval = TimeSpan.FromSeconds(0.2);
						fallback.IsRepeating = false;
						fallback.Tick += delegate
						{
							fallback.Stop();
							if (!tabbed.Tabs.FullReplyContentHeight.HasValue)
							{
								RelayoutPeek();
							}
						};
						fallback.Start();
					}
				});
			};
			tabbed.Tabs.Changed += delegate
			{
				dq.TryEnqueue(delegate
				{
					(string, double?) tuple = (tabbed.Tabs.ExpandedReplyEntryId, tabbed.Tabs.FullReplyContentHeight);
					(string, double?) tuple2 = tuple;
					(string, double?) tuple3 = lastFullReply;
					if (!(tuple2.Item1 == tuple3.Item1) || tuple2.Item2 != tuple3.Item2)
					{
						lastFullReply = tuple;
						if (tuple.Item1 != null)
						{
							double? item = tuple.Item2;
							if (!item.HasValue)
							{
								return;
							}
						}
						RelayoutPeek();
					}
				});
			};
		}
		root.Loaded += delegate
		{
			_ = shown;
		};
		Microsoft.UI.Xaml.Media.CompositionTarget.Rendered += OnFirstRender;
	}

	private void OnFirstRender(object? sender, RenderedEventArgs e)
	{
		if (shown)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendered -= OnFirstRender;
			this.FirstFrameShown?.Invoke();
		}
	}

	public void BindToEdges(UIElement element, double ax, double ay)
	{
		if (active != null)
		{
			Microsoft.UI.Composition.ExpressionAnimation expressionAnimation = c.CreateExpressionAnimation(FormattableString.Invariant($"Vector3({ax:0.###} * (p.Width - Wf) * 0.5, {ay:0.###} * (p.Height - Hf), 0)"));
			expressionAnimation.SetReferenceParameter("p", silhouette.Props);
			expressionAnimation.SetScalarParameter("Wf", (float)active.W);
			expressionAnimation.SetScalarParameter("Hf", (float)active.H);
			expressionAnimation.Target = "Translation";
			element.StartAnimation(expressionAnimation);
		}
	}

	public void Show()
	{
		Reposition();
		shown = true;
		Apply(Services.Presenter.Presentation, force: true);
		Window.ShowNoActivate();
		zOrderTimer.Start();
	}

	private void KeepOnTop(string why)
	{
		if (!shown || IsAmbientHidden)
		{
			return;
		}
		var (num, num2, item, item2) = SurfaceRectPx();
		if (OverlayWindow.Offscreen)
		{
			num += -24000;
			num2 += -24000;
		}
		nint num3 = ZOrder.ForeignWindowAbove(Window.Hwnd, (X: num, Y: num2, W: item, H: item2));
		if (num3 == 0)
		{
			lastCoveringWindow = 0;
			return;
		}
		if (num3 == FullscreenGuard.GameWindow())
		{
			lastCoveringWindow = num3;
			return;
		}
		ZOrder.RaiseToTop(Window.Hwnd);
		if (num3 != lastCoveringWindow)
		{
			Log.Info("notch", $"back on top of {ZOrder.Describe(num3)} ({why})");
		}
		lastCoveringWindow = num3;
		this.RaisedToTop?.Invoke();
	}

	private void SyncWorkingSweep()
	{
		workingSweep.Show(shown && NotchGeometry.IsWorkingBar(current) && !ambientHidden);
	}

	private void Reposition()
	{
		monitor = (shown ? Displays.ForWindow(Window.Hwnd) : monitor);
		contentScale = NotchGeometry.ContentScale(streamMode);
		double screenHeightDip = (double)monitor.Bounds.Height / monitor.Scale;
		SizeContext.ScreenHeightDip = screenHeightDip;
		envH = NotchGeometry.EnvelopeHeight(screenHeightDip, contentScale);
		Window.Root.Height = envH;
		contentLayer.Height = envH;
		ScaleTransform scaleTransform = rootScale;
		double scaleX = (rootScale.ScaleY = contentScale);
		scaleTransform.ScaleX = scaleX;
		Window.Root.RenderTransform = ((contentScale == 1.0) ? null : rootScale);
		double drawnScale = DrawnScale;
		int num2 = (int)Math.Round(envW * drawnScale);
		int h = (int)Math.Round(envH * drawnScale);
		int x = monitor.Bounds.Left + (monitor.Bounds.Width - num2) / 2;
		Window.PlacePx(x, monitor.Bounds.Top, num2, h);
		silhouette.SetDeviceScale(drawnScale);
		systemLayer?.SetScale(drawnScale, envW, envH);
		ApplyRegion(currentSize, null);
		SyncWorkingSweep();
	}

	public void SetStreamMode(bool on)
	{
		if (streamMode != on)
		{
			streamMode = on;
			Log.Info("notch", "stream mode " + (on ? "on: the notch draws 1.5x as big" : "off"));
			Reposition();
			PublishVisibleSurface();
			KeepOnTop("stream mode");
		}
	}

	public void FollowCursorDisplay()
	{
		Displays.Monitor monitor = Displays.UnderCursor();
		if (monitor.Handle != this.monitor.Handle)
		{
			this.monitor = monitor;
			Log.Info("notch", $"moved to the display under the cursor ({monitor.Bounds.Width}x{monitor.Bounds.Height} at {monitor.Bounds.Left},{monitor.Bounds.Top})");
			Reposition();
		}
	}

	public void SetAmbientHidden(bool hidden)
	{
		if (ambientHidden != hidden)
		{
			ambientHidden = hidden;
			ApplyAmbientVisibility();
		}
	}

	private void ApplyAmbientVisibility()
	{
		SyncWorkingSweep();
		bool flag = ambientHidden && current is NotchPresentation.Idle;
		Anim.Opacity(Window.Root, (!flag) ? 1 : 0, 0.22);
		if (!flag)
		{
			ApplyHeldRegion();
			return;
		}
		DispatcherQueueTimer t = dq.CreateTimer().Rooted();
		t.Interval = TimeSpan.FromSeconds(0.24);
		t.IsRepeating = false;
		t.Tick += delegate
		{
			t.Stop();
			if (IsAmbientHidden)
			{
				Window.SetRegionPx(0, 0, 0, 0);
			}
		};
		t.Start();
	}

	public IReadOnlyList<string> TakePendingFiles()
	{
		List<string> result = pendingFiles.ToList();
		pendingFiles.Clear();
		return result;
	}

	private static bool HasFiles(DragEventArgs e)
	{
		return e.DataView.Contains(StandardDataFormats.StorageItems);
	}

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		if (!HasFiles(e) || !Services.Auth.IsSignedIn)
		{
			return;
		}
		e.AcceptedOperation = DataPackageOperation.Copy;
		e.DragUIOverride.Caption = "Attach";
		NotchPresentation notchPresentation = current;
		if ((!(notchPresentation is NotchPresentation.TextInput) && !(notchPresentation is NotchPresentation.TextResponse)) || 1 == 0)
		{
			Services.Audio.PlayEarcon("textOpen");
			Services.Presenter.Present(new NotchPresentation.TextInput(""));
			if (!composerOpenedByDrag)
			{
				ClickyAnalytics.TrackNotchFileDragOpenedComposer();
			}
			composerOpenedByDrag = true;
		}
		dq.TryEnqueue(delegate
		{
			active?.Surface.SetDropTargeting(on: true);
		});
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		if (HasFiles(e) && Services.Auth.IsSignedIn)
		{
			e.AcceptedOperation = DataPackageOperation.Copy;
		}
	}

	private async void OnDrop(object sender, DragEventArgs e)
	{
		if (!HasFiles(e))
		{
			return;
		}
		DragOperationDeferral deferral = e.GetDeferral();
		try
		{
			List<string> list = (from i in await e.DataView.GetStorageItemsAsync()
				select i.Path into p
				where !string.IsNullOrEmpty(p)
				select p).ToList();
			Log.Info("notch", $"{list.Count} file(s) dropped on the notch");
			string composerOpenSource = ((composerOpenedByDrag && current is NotchPresentation.TextInput) ? "drag_opened" : ((current is NotchPresentation.TextInput) ? "user_opened" : "drop_opened"));
			composerOpenedByDrag = false;
			if (list.Count > 0)
			{
				ClickyAnalytics.TrackNotchFilesDropped(list.Count, composerOpenSource);
			}
			active?.Surface.SetDropTargeting(on: false);
			Frame? frame = active;
			if (frame == null || !frame.Surface.AcceptFiles(list))
			{
				pendingFiles.AddRange(list);
				Services.Presenter.Present(new NotchPresentation.TextInput(""));
			}
		}
		catch (Exception ex)
		{
			Log.Warn("notch", "file drop failed: " + ex.Message);
		}
		finally
		{
			deferral.Complete();
		}
	}

	private double Gap(NotchPresentation p)
	{
		if (!NotchGeometry.IsFloatingSliver(p))
		{
			return 0.0;
		}
		return 4.0;
	}

	private void ApplyHeldRegion()
	{
		regionTimer.Stop();
		TimeSpan timeSpan = regionHoldUntil - DateTimeOffset.Now;
		(double, double)? tuple = regionHold;
		if (tuple.HasValue)
		{
			(double, double) valueOrDefault = tuple.GetValueOrDefault();
			if (timeSpan > TimeSpan.Zero)
			{
				ApplyRegion(currentSize, valueOrDefault);
				regionTimer.Interval = timeSpan + TimeSpan.FromMilliseconds(20L, 0L);
				regionTimer.Start();
				return;
			}
		}
		regionHold = null;
		ApplyRegion(currentSize, null);
	}

	private void ApplyRegion((double W, double H) size, (double W, double H)? union)
	{
		if (IsAmbientHidden)
		{
			Window.SetRegionPx(0, 0, 0, 0);
			return;
		}
		var (num, num2) = size;
		if (union.HasValue)
		{
			(double, double) valueOrDefault = union.GetValueOrDefault();
			num = Math.Max(num, valueOrDefault.Item1);
			num2 = Math.Max(num2, valueOrDefault.Item2);
		}
		double num3 = Gap(current);
		double num4 = 0.0;
		double num5 = num2 + num3;
		double drawnScale = DrawnScale;
		int x = (int)Math.Floor((envW - num) / 2.0 * drawnScale);
		int w = (int)Math.Ceiling(num * drawnScale);
		Window.SetRegionPx(x, (int)(num4 * drawnScale), w, (int)Math.Ceiling(num5 * drawnScale));
	}

	public (int X, int Y, int W, int H) SurfaceRectPx()
	{
		if (!shown || IsAmbientHidden)
		{
			return (X: 0, Y: 0, W: 0, H: 0);
		}
		Displays.Monitor monitor = Displays.ForWindow(Window.Hwnd);
		double num = monitor.Scale * contentScale;
		return (X: monitor.Bounds.Left + (monitor.Bounds.Width - (int)Math.Round(envW * num)) / 2 + (int)Math.Floor((envW - currentSize.W) / 2.0 * num), Y: monitor.Bounds.Top, W: (int)Math.Ceiling(currentSize.W * num), H: (int)Math.Ceiling((currentSize.H + Gap(current)) * num));
	}

	private void PublishVisibleSurface()
	{
		(int X, int Y, int W, int H) tuple = SurfaceRectPx();
		int item = tuple.X;
		int item2 = tuple.Y;
		int item3 = tuple.W;
		int item4 = tuple.H;
		bool flag = item3 > 0 && !IsAmbient(current);
		bool flag2;
		if (flag)
		{
			if (current is NotchPresentation.LiveEvent liveEvent)
			{
				NotchLiveEvent @event = liveEvent.Event;
				if (@event is NotchLiveEvent.ActivityListening || @event is NotchLiveEvent.ActivityThinking || @event is NotchLiveEvent.ActivitySpeaking || @event is NotchLiveEvent.ActivityCustomStatus || @event is NotchLiveEvent.Dictation || @event is NotchLiveEvent.IntroHatchPeek)
				{
					flag2 = true;
					goto IL_0094;
				}
			}
			flag2 = false;
			goto IL_0094;
		}
		goto IL_009b;
		IL_0094:
		flag = !flag2;
		goto IL_009b;
		IL_009b:
		bool isOpen = flag;
		NotchVisibleSurfaces.Replace((item3 > 0) ? new RectInt32?(new RectInt32(item, item2, item3, item4)) : ((RectInt32?)null), isOpen);
	}

	public bool ContainsScreenPointPx(PointD p)
	{
		var (num, num2, num3, num4) = SurfaceRectPx();
		if (num3 <= 0 || num4 <= 0)
		{
			return false;
		}
		double num5 = 3.0 * Displays.ForWindow(Window.Hwnd).Scale;
		if (p.X >= (double)num - num5 && p.X <= (double)(num + num3) + num5 && p.Y >= (double)num2 - num5)
		{
			return p.Y <= (double)(num2 + num4) + num5;
		}
		return false;
	}

	private void BeginHoverExit()
	{
		if (!IsHovering || IsAmbient(current))
		{
			SetHover(hovering: false);
			return;
		}
		hoverExitOutsideSince = null;
		if ((object)hoverExitTimer == null)
		{
			hoverExitTimer = dq.CreateTimer();
			hoverExitTimer.Interval = TimeSpan.FromMilliseconds(50L, 0L);
			hoverExitTimer.Tick += delegate
			{
				TickHoverExit();
			};
		}
		hoverExitTimer.Start();
		TickHoverExit();
	}

	private void NoteStickyRect()
	{
		if (!IsHovering || IsAmbient(current))
		{
			return;
		}
		var (num, num2, num3, num4) = SurfaceRectPx();
		if (num3 > 0 && num4 > 0)
		{
			(int, int, int, int)? tuple2 = stickyRectPx;
			if (tuple2.HasValue)
			{
				(int, int, int, int) valueOrDefault = tuple2.GetValueOrDefault();
				int num5 = Math.Min(valueOrDefault.Item1, num);
				int num6 = Math.Min(valueOrDefault.Item2, num2);
				int num7 = Math.Max(valueOrDefault.Item1 + valueOrDefault.Item3, num + num3);
				int num8 = Math.Max(valueOrDefault.Item2 + valueOrDefault.Item4, num2 + num4);
				stickyRectPx = (num5, num6, num7 - num5, num8 - num6);
			}
			else
			{
				stickyRectPx = (num, num2, num3, num4);
			}
		}
	}

	private void TickHoverExit()
	{
		if (!IsHovering || IsAmbient(current))
		{
			StopHoverExitWatch();
			SetHover(hovering: false);
			return;
		}
		if ((Native.GetAsyncKeyState(1) & 0x8000) != 0 || (Native.GetAsyncKeyState(2) & 0x8000) != 0)
		{
			hoverExitOutsideSince = null;
			return;
		}
		XamlRoot xamlRoot = contentLayer.XamlRoot;
		if ((object)xamlRoot != null && VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot).Any((Popup p) => p.Child is MenuFlyoutPresenter))
		{
			hoverExitOutsideSince = null;
			return;
		}
		Native.GetCursorPos(out var p2);
		NoteStickyRect();
		(int X, int Y, int W, int H) obj = stickyRectPx ?? SurfaceRectPx();
		int item = obj.X;
		int item2 = obj.Y;
		int item3 = obj.W;
		int item4 = obj.H;
		double num = 24.0 * Displays.ForWindow(Window.Hwnd).Scale * contentScale;
		if (item3 > 0 && (double)p2.X >= (double)item - num && (double)p2.X <= (double)(item + item3) + num && p2.Y >= item2 && (double)p2.Y <= (double)(item2 + item4) + num)
		{
			hoverExitOutsideSince = null;
			return;
		}
		DateTimeOffset now = DateTimeOffset.Now;
		DateTimeOffset valueOrDefault = hoverExitOutsideSince.GetValueOrDefault();
		if (!hoverExitOutsideSince.HasValue)
		{
			valueOrDefault = now;
			hoverExitOutsideSince = valueOrDefault;
		}
		if (!(now - hoverExitOutsideSince.Value < HoverExitCloseDelay))
		{
			StopHoverExitWatch();
			SetHover(hovering: false);
		}
	}

	private void StopHoverExitWatch()
	{
		hoverExitTimer?.Stop();
		hoverExitOutsideSince = null;
	}

	private void SetHover(bool hovering)
	{
		if (IsHovering != hovering)
		{
			IsHovering = hovering;
			if (!hovering)
			{
				stickyRectPx = null;
			}
			Log.Info("notch", hovering ? "pointer in" : "pointer out");
			this.HoverChanged?.Invoke(hovering);
		}
	}

	public void Relayout()
	{
		Apply(current, force: false, relayoutOnly: true);
	}

	public void RelayoutPeek()
	{
		if (current is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentPeek)
		{
			Relayout();
		}
	}

	private void SetHasKeyboard(bool has)
	{
		has = has && Window.CanActivate;
		if (has != hasKeyboard)
		{
			hasKeyboard = has;
			this.KeyboardChanged?.Invoke(has);
		}
	}

	public void RebuildAfterLayoutCycle()
	{
		foreach (Frame item in new Frame[2] { active, peekFrame }.OfType<Frame>().Distinct())
		{
			item.Removal?.Stop();
			item.Removal = null;
			item.Surface.Detached();
			contentLayer.Children.Remove(item.Root);
		}
		active = null;
		peekFrame = null;
		Apply(Services.Presenter.Presentation, force: true);
	}

	public void ReleaseKeyboardIfUnheld()
	{
		Frame? frame = active;
		if (frame == null || !frame.Surface.HoldsKeyboard)
		{
			ReleaseKeyboard();
		}
	}

	public void Apply(NotchPresentation next, bool force = false, bool relayoutOnly = false)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		NoteStickyRect();
		NotchPresentation notchPresentation = current;
		bool flag = SameKind(notchPresentation, next);
		if (!relayoutOnly && IsAmbient(notchPresentation) && !IsAmbient(next) && shown && !IsHovering)
		{
			FollowCursorDisplay();
		}
		bool isAmbientHidden = IsAmbientHidden;
		current = next;
		if (ambientHidden && isAmbientHidden != IsAmbientHidden)
		{
			ApplyAmbientVisibility();
		}
		NotchAccessibility.Announce(Window.Root, next);
		NotchGeometry.SizeContext sizeContext = SizeContext;
		IAgentService agents = Services.Agents;
		sizeContext.AgentDriving = agents != null && AgentColors.RunningTasks(agents).Count > 0;
		(double, double) tuple = NotchGeometry.PanelSize(next, SizeContext);
		SwiftSpring? swiftSpring = NotchGeometry.SpringFor(next);
		if (NotchChatSize.IsResizing && next is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentPeek)
		{
			swiftSpring = null;
		}
		bool flag2 = NotchGeometry.IsFloatingSliver(next);
		if (!relayoutOnly)
		{
			if (active != null && flag && active.Surface.CanUpdate(next) && !force)
			{
				active.Surface.Update(next);
				tuple = NotchGeometry.PanelSize(next, SizeContext);
			}
			else
			{
				if (active != null)
				{
					Retire(active, swiftSpring ?? NotchSprings.Content);
				}
				active = null;
				if (!(next is NotchPresentation.Idle))
				{
					Frame frame = ((next is NotchPresentation.LiveEvent liveEvent2 && liveEvent2.Event is NotchLiveEvent.AgentPeek) ? peekFrame : null);
					NotchSurface notchSurface = frame?.Surface ?? NotchSurfaces.Create(next, this);
					if (notchSurface != null)
					{
						bool flag3 = (object)frame?.Removal != null;
						active = ((frame != null) ? Remount(frame, next, tuple) : Mount(notchSurface, next, tuple));
						if (notchSurface is NotchPeekSurface)
						{
							peekFrame = active;
						}
						notchSurface.Attach(this, flag3);
						tuple = NotchGeometry.PanelSize(next, SizeContext);
						SwiftSpring spring = ((next is NotchPresentation.TextInput) ? NotchSprings.TextInputOpen : NotchSprings.Content);
						if (!swiftSpring.HasValue)
						{
							active.Root.Opacity = 1.0;
						}
						else if (flag3)
						{
							Anim.ContentBack(active.Root, spring);
						}
						else
						{
							Anim.ContentIn(active.Root, spring);
						}
					}
				}
			}
		}
		if (active != null)
		{
			Resize(active, tuple);
		}
		silhouette.AnimateTo(tuple.Item1, tuple.Item2, NotchGeometry.BottomCornerRadius(next), Gap(next), (!flag2) ? 1 : 0, flag2 ? 1.0 : 1.0, flag2 ? 1 : 0, swiftSpring);
		UpdateMultiAgentRotation(notchPresentation, next);
		glow?.Update(NotchGeometry.Shown(next), AgentTint(NotchGeometry.Shown(next)));
		SyncWorkingSweep();
		if (next is NotchPresentation.LiveEvent)
		{
			glowQuiesceTimer.Stop();
		}
		else
		{
			glowQuiesceTimer.Start();
		}
		(double, double) tuple2 = currentSize;
		currentSize = tuple;
		if (swiftSpring.HasValue)
		{
			SwiftSpring valueOrDefault = swiftSpring.GetValueOrDefault();
			(double, double) tuple3 = tuple;
			(double, double) tuple4 = tuple2;
			if ((tuple3.Item1 != tuple4.Item1 || tuple3.Item2 != tuple4.Item2) && !Anim.Instant && !Anim.ReduceMotion)
			{
				(double, double) value = (Math.Max(tuple2.Item1, tuple.Item1 * 1.03 + 4.0), Math.Max(tuple2.Item2, tuple.Item2 * 1.03 + 4.0));
				(double, double)? tuple5 = regionHold;
				if (tuple5.HasValue)
				{
					(double, double) valueOrDefault2 = tuple5.GetValueOrDefault();
					if (DateTimeOffset.Now < regionHoldUntil)
					{
						value = (Math.Max(value.Item1, valueOrDefault2.Item1), Math.Max(value.Item2, valueOrDefault2.Item2));
					}
				}
				regionHold = value;
				DateTimeOffset dateTimeOffset = DateTimeOffset.Now + valueOrDefault.SettleTime;
				if (dateTimeOffset > regionHoldUntil)
				{
					regionHoldUntil = dateTimeOffset;
				}
			}
		}
		ApplyHeldRegion();
		UpdateKeyboardOwnership(notchPresentation, next);
		PublishVisibleSurface();
		if (!relayoutOnly)
		{
			KeepOnTop("present " + Describe(next));
		}
		if (!relayoutOnly)
		{
			Log.Info("notch", $"present {Describe(next)} {tuple.Item1:0}x{tuple.Item2:0} in {stopwatch.Elapsed.TotalMilliseconds:0.0} ms");
		}
	}

	private Color? AgentTint(NotchPresentation p)
	{
		if (p is NotchPresentation.LiveEvent liveEvent)
		{
			NotchLiveEvent @event = liveEvent.Event;
			if (@event is NotchLiveEvent.AgentTask agentTask)
			{
				return AgentColors.For(agentTask.TaskId, 0);
			}
			if (@event is NotchLiveEvent.AgentMultiActivity || @event is NotchLiveEvent.ActivityThinking || @event is NotchLiveEvent.ActivityCustomStatus)
			{
				AgentTaskSnapshot agentTaskSnapshot = AgentColors.DisplayedRunningTask(Services.Agents, MultiAgentRotation, liveEvent.Event is NotchLiveEvent.AgentMultiActivity);
				if ((object)agentTaskSnapshot != null)
				{
					return AgentColors.For(agentTaskSnapshot.TaskId, 0);
				}
				return null;
			}
		}
		return null;
	}

	private void UpdateMultiAgentRotation(NotchPresentation previous, NotchPresentation next)
	{
		bool flag = previous is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentMultiActivity;
		if (!(next is NotchPresentation.LiveEvent liveEvent2) || !(liveEvent2.Event is NotchLiveEvent.AgentMultiActivity))
		{
			rotationTimer.Stop();
			return;
		}
		MultiAgentRotation.Clamp(AgentColors.RunningTasks(Services.Agents).Count);
		if (!flag)
		{
			MultiAgentRotation.Restart(DateTimeOffset.UtcNow);
			ScheduleRotation();
		}
	}

	private void ScheduleRotation()
	{
		rotationTimer.Interval = MultiAgentRotation.UntilDue(DateTimeOffset.UtcNow) + TimeSpan.FromMilliseconds(20L, 0L);
		rotationTimer.Start();
	}

	private void RotateMultiAgentStrip()
	{
		if (current is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentMultiActivity)
		{
			int count = AgentColors.RunningTasks(Services.Agents).Count;
			MultiAgentRotation.Clamp(count);
			if (MultiAgentRotation.AdvanceIfHoldElapsed(DateTimeOffset.UtcNow, count))
			{
				this.MultiAgentRotated?.Invoke();
				glow?.Update(NotchGeometry.Shown(current), AgentTint(NotchGeometry.Shown(current)));
			}
			ScheduleRotation();
		}
	}

	public static bool IsAmbient(NotchPresentation p)
	{
		bool flag = p is NotchPresentation.Idle;
		bool flag2;
		if (!flag)
		{
			if (p is NotchPresentation.LiveEvent liveEvent)
			{
				NotchLiveEvent @event = liveEvent.Event;
				if (@event is NotchLiveEvent.RestingMark || @event is NotchLiveEvent.SkillPinned || @event is NotchLiveEvent.AgentMultiActivity || @event is NotchLiveEvent.AgentTask)
				{
					flag2 = true;
					goto IL_0044;
				}
			}
			flag2 = false;
			goto IL_0044;
		}
		goto IL_0046;
		IL_0044:
		flag = flag2;
		goto IL_0046;
		IL_0046:
		return flag;
	}

	private static bool SameKind(NotchPresentation a, NotchPresentation b)
	{
		if (a.GetType() != b.GetType())
		{
			return false;
		}
		if (a is NotchPresentation.LiveEvent liveEvent && b is NotchPresentation.LiveEvent liveEvent2)
		{
			return liveEvent.Event.GetType() == liveEvent2.Event.GetType();
		}
		return true;
	}

	public static string Describe(NotchPresentation p)
	{
		if (p is NotchPresentation.LiveEvent liveEvent)
		{
			return "live:" + liveEvent.Event.GetType().Name;
		}
		return p.GetType().Name;
	}

	private static Thickness ContentPadding(NotchPresentation p)
	{
		return new Thickness(18.0, NotchGeometry.ContentTopPadding(p), 18.0, NotchGeometry.ContentBottomPadding(p));
	}

	private Frame Mount(NotchSurface surface, NotchPresentation p, (double W, double H) size)
	{
		Border border = new Border
		{
			Padding = ContentPadding(p),
			Child = surface.View
		};
		surface.View.VerticalAlignment = surface.ContentAlignment;
		Grid grid = new Grid();
		grid.Children.Add(border);
		FrameworkElement overlay = surface.Overlay;
		if ((object)overlay != null)
		{
			grid.Children.Add(overlay);
		}
		contentLayer.Children.Add(grid);
		Frame frame = new Frame
		{
			Surface = surface,
			Root = grid,
			Padded = border
		};
		Resize(frame, size);
		return frame;
	}

	private Frame Remount(Frame f, NotchPresentation p, (double W, double H) size)
	{
		DispatcherQueueTimer removal = f.Removal;
		if ((object)removal != null)
		{
			TimerRoots.Release(removal);
			f.Removal = null;
		}
		if (!contentLayer.Children.Contains(f.Root))
		{
			contentLayer.Children.Add(f.Root);
		}
		f.Root.IsHitTestVisible = true;
		f.Padded.Padding = ContentPadding(p);
		Resize(f, size);
		return f;
	}

	private void Resize(Frame f, (double W, double H) size)
	{
		(f.W, f.H) = size;
		f.Root.Width = size.W;
		f.Root.Height = size.H;
		Canvas.SetLeft(f.Root, (envW - size.W) / 2.0);
		Canvas.SetTop(f.Root, Gap(current));
	}

	private void Retire(Frame f, SwiftSpring spring)
	{
		f.Surface.Detached();
		f.Root.IsHitTestVisible = false;
		Anim.ContentOut(f.Root, spring);
		DispatcherQueueTimer t = dq.CreateTimer().Rooted();
		t.Interval = TimeSpan.FromSeconds(Math.Max(0.25, spring.SettleTime.TotalSeconds * 0.6));
		t.IsRepeating = false;
		f.Removal = t;
		t.Tick += delegate
		{
			if (!(f.Removal != t))
			{
				f.Removal = null;
				contentLayer.Children.Remove(f.Root);
			}
		};
		t.Start();
	}

	private void UpdateKeyboardOwnership(NotchPresentation previous, NotchPresentation next)
	{
		if (next is NotchPresentation.LiveEvent liveEvent && liveEvent.Event is NotchLiveEvent.AgentPeek)
		{
			Frame? frame = active;
			if ((frame != null && frame.Surface.HoldsKeyboard) || Environment.TickCount64 - keyboardRequestedAt < 1500)
			{
				return;
			}
		}
		Frame? frame2 = active;
		bool flag = frame2 != null && frame2.Surface.WantsKeyboard && next is NotchPresentation.TextInput;
		bool flag2 = previous is NotchPresentation.TextInput;
		if (flag && !Window.CanActivate)
		{
			nint foregroundWindow = Native.GetForegroundWindow();
			if (foregroundWindow != Window.Hwnd)
			{
				previousForeground = foregroundWindow;
			}
			Window.CanActivate = true;
			Native.ForceForeground(Window.Hwnd);
			Window.Activate();
			active.Surface.FocusInput();
		}
		else if (!flag && Window.CanActivate)
		{
			Window.CanActivate = false;
			if (Native.GetForegroundWindow() == Window.Hwnd && previousForeground != 0 && Native.IsWindow(previousForeground))
			{
				Log.Info("notch", "keyboard handed back to the app behind (" + next.GetType().Name + ")");
				Native.SetForegroundWindow(previousForeground);
			}
			previousForeground = 0;
		}
		else if (flag && flag2)
		{
			active?.Surface.FocusInput();
		}
	}

	public void RequestKeyboard()
	{
		keyboardRequestedAt = Environment.TickCount64;
		nint foregroundWindow = Native.GetForegroundWindow();
		if (Window.CanActivate)
		{
			if (foregroundWindow != Window.Hwnd)
			{
				if (foregroundWindow != 0 && !Native.IsOwnWindow(foregroundWindow))
				{
					previousForeground = foregroundWindow;
				}
				TakeForeground();
			}
			SetHasKeyboard(Native.GetForegroundWindow() == Window.Hwnd);
		}
		else
		{
			if (foregroundWindow != Window.Hwnd && !Native.IsOwnWindow(foregroundWindow))
			{
				previousForeground = foregroundWindow;
			}
			Window.CanActivate = true;
			TakeForeground();
			SetHasKeyboard(Native.GetForegroundWindow() == Window.Hwnd);
		}
	}

	private void TakeForeground()
	{
		Native.ForceForeground(Window.Hwnd);
		if (Native.GetForegroundWindow() != Window.Hwnd)
		{
			Window.Activate();
			if (Native.GetForegroundWindow() != Window.Hwnd)
			{
				Log.Warn("notch", "couldn't take the keyboard (in front: " + Native.DescribeForeground(Native.GetForegroundWindow()) + ")");
			}
		}
	}

	private bool TakeKeyboardBackFrom(nint activated)
	{
		if (activated != 0 && activated != Window.Hwnd && Window.CanActivate)
		{
			Frame? frame = active;
			if (frame != null && frame.Surface.HoldsKeyboard)
			{
				if (!IsNonTypingWindow(activated))
				{
					return false;
				}
				Log.Info("notch", $"HeyClicky's non-typing window {activated} took activation from the chat; taking the keyboard back");
				TakeForeground();
				SetHasKeyboard(Native.GetForegroundWindow() == Window.Hwnd);
				return hasKeyboard;
			}
		}
		return false;
	}

	public void ReleaseKeyboard()
	{
		if (Window.CanActivate)
		{
			keyboardRequestedAt = -1500L;
			Window.CanActivate = false;
			if (Native.GetForegroundWindow() == Window.Hwnd && previousForeground != 0 && Native.IsWindow(previousForeground))
			{
				Native.SetForegroundWindow(previousForeground);
			}
			previousForeground = 0;
			SetHasKeyboard(has: false);
		}
	}
}

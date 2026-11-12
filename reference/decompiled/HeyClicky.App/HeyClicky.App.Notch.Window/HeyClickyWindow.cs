using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Billing;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Services;
using HeyClicky.App.Settings;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using WinRT.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using Windows.UI;

namespace HeyClicky.App.Notch.Window;

public sealed class HeyClickyWindow
{
	private sealed record SidebarEntry(string Key, string Name, string? JobId, string? ClickySlug, ArcCardState? State, DateTimeOffset LatestAt, bool OnDock);

	private sealed class DocumentResizeHandle : Grid
	{
		private readonly HeyClickyWindow owner;

		private readonly Border capsule;

		private double? startX;

		private double? startWidth;

		private bool hovered;

		public DocumentResizeHandle(HeyClickyWindow owner, MessagesPalette look)
		{
			DocumentResizeHandle documentResizeHandle = this;
			this.owner = owner;
			base.Background = Ui.Transparent;
			capsule = new Border
			{
				Width = 3.0,
				Height = 44.0,
				CornerRadius = new CornerRadius(1.5),
				Background = Ui.Brush(look.InkAt(0.3)),
				Opacity = 0.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsHitTestVisible = false
			};
			base.Children.Add(capsule);
			base.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
			ToolTipService.SetToolTip(this, "Drag to resize");
			base.PointerEntered += delegate
			{
				documentResizeHandle.hovered = true;
				documentResizeHandle.Show();
			};
			base.PointerExited += delegate
			{
				documentResizeHandle.hovered = false;
				documentResizeHandle.Show();
			};
			base.PointerPressed += delegate(object _, PointerRoutedEventArgs e)
			{
				documentResizeHandle.CapturePointer(e.Pointer);
				documentResizeHandle.startX = e.GetCurrentPoint(owner.root).Position.X;
				documentResizeHandle.startWidth = owner.documentDefinition?.Width.Value;
				documentResizeHandle.Show();
				e.Handled = true;
			};
			base.PointerMoved += delegate(object _, PointerRoutedEventArgs e)
			{
				double? num = documentResizeHandle.startX;
				if (num.HasValue)
				{
					double valueOrDefault = num.GetValueOrDefault();
					num = documentResizeHandle.startWidth;
					if (num.HasValue)
					{
						double valueOrDefault2 = num.GetValueOrDefault();
						owner.ResizeDocument(valueOrDefault2 - (e.GetCurrentPoint(owner.root).Position.X - valueOrDefault));
					}
				}
			};
			base.PointerReleased += delegate(object _, PointerRoutedEventArgs e)
			{
				documentResizeHandle.End(e.Pointer);
			};
			base.PointerCaptureLost += delegate(object _, PointerRoutedEventArgs e)
			{
				documentResizeHandle.End(e.Pointer);
			};
		}

		private void Show()
		{
			Border e = capsule;
			int num2;
			if (!hovered)
			{
				double? num = startX;
				if (!num.HasValue)
				{
					num2 = 0;
					goto IL_0022;
				}
			}
			num2 = 1;
			goto IL_0022;
			IL_0022:
			Anim.Opacity(e, num2, 0.15, Easing.EaseOut(Ui.Compositor));
		}

		private void End(Pointer pointer)
		{
			double? num = startX;
			if (num.HasValue)
			{
				startX = null;
				ReleasePointerCapture(pointer);
				Show();
				owner.SaveFrame();
			}
		}
	}

	private sealed record State(int X, int Y, int Width, int Height, bool SidebarShown, double DocumentWidth)
	{
		private static string FilePath => System.IO.Path.Combine(AppBundleConfiguration.AppDataDirectory, "heyclicky-window.json");

		public static State Load()
		{
			try
			{
				if (File.Exists(FilePath) && JsonNode.Parse(File.ReadAllText(FilePath), null) is JsonObject jsonObject)
				{
					return new State(((int?)jsonObject["x"]) ?? (-1), ((int?)jsonObject["y"]) ?? (-1), ((int?)jsonObject["w"]).GetValueOrDefault(), ((int?)jsonObject["h"]).GetValueOrDefault(), ((bool?)jsonObject["sidebar"]) ?? true, ((double?)jsonObject["document"]) ?? 520.0);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("window", "window frame unreadable: " + ex.Message);
			}
			return new State(-1, -1, 0, 0, SidebarShown: true, 520.0);
		}

		public void Save()
		{
			try
			{
				Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
				File.WriteAllText(FilePath, new JsonObject((JsonNodeOptions?)null)
				{
					["x"] = X,
					["y"] = Y,
					["w"] = Width,
					["h"] = Height,
					["sidebar"] = SidebarShown,
					["document"] = DocumentWidth
				}.ToJsonString());
			}
			catch (Exception ex)
			{
				Log.Warn("window", "window frame not saved: " + ex.Message);
			}
		}
	}

	private sealed class DispatcherQueueTimerHolder(Microsoft.UI.Dispatching.DispatcherQueue queue, Action tick)
	{
		private Microsoft.UI.Dispatching.DispatcherQueueTimer? timer;

		public void Restart(TimeSpan after)
		{
			if ((object)timer == null)
			{
				timer = queue.CreateTimer();
			}
			timer.Stop();
			timer.Interval = after;
			timer.IsRepeating = false;
			timer.Tick -= OnTick;
			timer.Tick += OnTick;
			timer.Start();
		}

		private void OnTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
		{
			tick();
		}
	}

	public const string MainThreadKey = "heyclicky";

	private INotchHost? notchHost;

	private ArcDock? dock;

	private Microsoft.UI.Xaml.Window? window;

	private AppWindow? appWindow;

	private nint hwnd;

	private HeyClickyWindowHost? host;

	private Grid? root;

	private Grid? sidebarColumn;

	private Grid? documentColumn;

	private Grid? ground;

	private Border? mainCard;

	private Border? columnHeader;

	private Border? mainColumn;

	private FrameworkElement? chatDots;

	private StackPanel? sidebarRows;

	private StackPanel? topLeft;

	private Pressable? sidebarButton;

	private Pressable? backButton;

	private Grid? dragBand;

	private readonly List<FrameworkElement> headerButtons = new List<FrameworkElement>();

	private ColumnDefinition? sidebarDefinition;

	private ColumnDefinition? documentDefinition;

	private NotchChatView? mainChat;

	private SettingsContent? settings;

	private NotchDocumentViewer? documentViewer;

	private Border? documentPlate;

	private Border? documentHeader;

	private string selectedKey = "heyclicky";

	private string? keyBeforeSettings;

	private bool showingSettings;

	private bool earlierUnfolded;

	private bool isActive;

	private string? documentPath;

	private string? documentThreadId;

	private volatile string? shownAgentKey;

	private State saved = State.Load();

	private HeyClickyWindowPalette palette = HeyClickyWindowPalette.Dark;

	private SystemLookWatcher? lookWatcher;

	private Border? accountSlot;

	private Grid? dropOverlay;

	private bool dropTargeted;

	private int dragLeaveCount;

	private const int SW_RESTORE = 9;

	private long lastDeactivatedTicks;

	private const double HeaderHeight = 44.0;

	private const double TitleBandHeight = 36.0;

	private const double CardTopInset = 36.0;

	private const double CardInset = 8.0;

	private const double CardCornerRadius = 12.0;

	private const double SidebarWidth = 244.0;

	private const double CardHeaderHeight = 38.0;

	private const double DocumentHeaderHeight = 32.0;

	private const double MinimumDocumentWidth = 340.0;

	private const double MinimumConversationWidth = 380.0;

	private const double TopLeftLeading = 14.0;

	private const string SidebarGlyph = "\ue90c";

	private static readonly SwiftSpring ColumnSpring = new SwiftSpring(0.32, 0.88);

	private SpringValue? sidebarShownness;

	private SpringValue? documentShownness;

	private bool columnFramesRunning;

	private bool documentViewerHolds;

	private string passthroughShape = "";

	private ContentControl? focusRest;

	private static readonly HashSet<string> LittleWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"a", "an", "the", "of", "for", "to", "and", "in", "on", "my",
		"your", "with", "about", "from", "at", "by", "me"
	};

	private readonly Dictionary<string, FrameworkElement> sidebarViews = new Dictionary<string, FrameworkElement>();

	private string sidebarShape = "";

	private readonly Dictionary<string, string> sidebarSections = new Dictionary<string, string>();

	private static readonly Dictionary<string, BitmapImage> AvatarImages = new Dictionary<string, BitmapImage>();

	private bool refreshQueued;

	private bool wasAnswering;

	private FrameworkElement? headerChild;

	private string? shownHeaderShape;

	private const double SettingsPageListLeading = 12.0;

	private const double SettingsPageListWidth = 224.0;

	private const double SettingsPageContentLeading = 261.0;

	private FrameworkElement? shownChild;

	private Grid? stage;

	private SettingsWindow.Page settingsPage;

	private double windowMoveToContentWidth;

	private RectInt32 windowMoveFrom;

	private RectInt32 windowMoveTo;

	private double windowMoveStartedAt;

	private double windowMoveSeconds;

	private bool windowMoving;

	private DispatcherQueueTimerHolder? saveTimer;

	public static HeyClickyWindow Shared { get; } = new HeyClickyWindow();

	private MessagesPalette Look => palette.Messages;

	public bool IsOpen => (object)window != null;

	internal nint Hwnd => hwnd;

	public bool IsInFront
	{
		get
		{
			if (hwnd != 0)
			{
				return Native.GetForegroundWindow() == hwnd;
			}
			return false;
		}
	}

	public static bool KeepsAgentLinesOffCursor => Shared.IsInFront;

	public bool IsOpenOnScreen
	{
		get
		{
			if (hwnd != 0 && Native.IsWindowVisible(hwnd))
			{
				return !Native.IsIconic(hwnd);
			}
			return false;
		}
	}

	public bool WasInFrontJustNow
	{
		get
		{
			if (!IsInFront)
			{
				if (hwnd != 0)
				{
					return Environment.TickCount64 - lastDeactivatedTicks < 1000;
				}
				return false;
			}
			return true;
		}
	}

	private bool SidebarVisible
	{
		get
		{
			if (saved.SidebarShown)
			{
				return !showingSettings;
			}
			return false;
		}
	}

	private string SidebarButtonTip
	{
		get
		{
			if (!saved.SidebarShown)
			{
				return "Show sidebar";
			}
			return "Hide sidebar";
		}
	}

	public void Configure(INotchHost notchHost, ArcDock? dock)
	{
		this.notchHost = notchHost;
		this.dock = dock;
	}

	public string? AgentKeyUnderPointer()
	{
		nint num = hwnd;
		if (num != 0)
		{
			string text = shownAgentKey;
			if (text != null && Native.IsWindowVisible(num) && !Native.IsIconic(num))
			{
				if (!Native.GetCursorPos(out var p))
				{
					return null;
				}
				nint num2 = Native.WindowFromPoint(p);
				if (num2 == 0 || Native.GetAncestor(num2, 2u) != num)
				{
					return null;
				}
				return text;
			}
		}
		return null;
	}

	public void Open(string? key = null, bool settingsPage = false, string? document = null, string? documentThreadId = null, string from = "notch", HeyClickyWindowOpeningFlight? flight = null, SettingsWindow.Page settingsSection = SettingsWindow.Page.Account)
	{
		if (notchHost == null)
		{
			flight?.Abandon();
			return;
		}
		bool flag = (object)window != null;
		if (flag && HeyClickyWindowOpeningFlight.IsFlying && flight == null)
		{
			return;
		}
		bool flag2 = flag && IsOpenOnScreen && VirtualDesktops.IsOnCurrentDesktop(hwnd);
		if (!flag)
		{
			Build();
		}
		if (flag)
		{
			flight?.Abandon();
			flight = null;
		}
		if (flight != null)
		{
			HeyClickyWindowOpeningFlight.Cloak(hwnd, cloaked: true);
		}
		if (settingsPage)
		{
			ShowSettings(settingsSection);
		}
		else
		{
			if (showingSettings)
			{
				LeaveSettings();
			}
			string text = key ?? selectedKey;
			Select(KeyStillExists(text) ? text : "heyclicky");
		}
		if (document != null)
		{
			ShowDocument(document, documentThreadId);
		}
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "window_opened",
			["from"] = from,
			["was_open"] = flag
		});
		if (flag)
		{
			if (Native.IsIconic(hwnd))
			{
				Native.ShowWindow(hwnd, 9);
			}
			VirtualDesktops.MoveToCurrentDesktop(hwnd);
		}
		window.Activate();
		Native.SetForegroundWindow(hwnd);
		ParkFocus(force: false);
		window.DispatcherQueue.TryEnqueue(delegate
		{
			ParkFocus(force: false);
		});
		if (flight != null)
		{
			try
			{
				Land(flight);
				return;
			}
			catch (Exception ex)
			{
				Log.Error("window", "flight: landing failed", ex);
				HeyClickyWindowOpeningFlight.Cloak(hwnd, cloaked: false);
				flight.Abandon();
				return;
			}
		}
		if (!flag2)
		{
			Arrive(flag);
		}
	}

	private void Land(HeyClickyWindowOpeningFlight flight)
	{
		Native.RECT windowPx = HeyClickyWindowOpeningFlight.VisibleFrame(hwnd);
		Displays.Monitor monitor = Displays.ForWindow(notchHost.WindowHandle);
		if (Displays.ForWindow(hwnd).Handle != monitor.Handle && !OverlayWindow.Offscreen)
		{
			HeyClickyWindowOpeningFlight.Cloak(hwnd, cloaked: false);
			flight.Abandon();
			Arrive(alreadyOpen: false);
			return;
		}
		nint landing = hwnd;
		flight.Bloom(windowPx, delegate
		{
			if (hwnd != landing || (object)root == null)
			{
				flight.Dispose();
			}
			else
			{
				HeyClickyWindowOpeningFlight.Cloak(hwnd, cloaked: false);
				HeyClickyWindowOpeningFlight.LastUncoveredAt = DateTime.UtcNow;
				if (root.ActualWidth > 0.0)
				{
					Anim.ScaleUpFrom(root, 0.955f, new SwiftSpring(0.45, 0.85), new Vector3((float)root.ActualWidth / 2f, (float)root.ActualHeight / 2f, 0f));
				}
				flight.Reveal();
			}
		});
	}

	public bool BringOpenWindowToFront()
	{
		if ((object)window == null || hwnd == 0)
		{
			return false;
		}
		if (Native.IsIconic(hwnd))
		{
			Native.ShowWindow(hwnd, 9);
		}
		window.Activate();
		Native.ForceForeground(hwnd);
		return true;
	}

	private void Arrive(bool alreadyOpen)
	{
		SwiftSpring spring;
		if ((object)root != null && !Anim.ReduceMotion)
		{
			root.Opacity = 0.0;
			spring = new SwiftSpring(alreadyOpen ? 0.3 : 0.42, alreadyOpen ? 0.9 : 0.82);
			Anim.OpacitySpring(root, 1f, spring);
			if (alreadyOpen)
			{
				root.Translation = new Vector3(0f, 14f, 0f);
				Anim.TranslationSpring(root, Vector3.Zero, spring);
			}
			else if (root.ActualWidth > 0.0)
			{
				Anim.ScaleUpFrom(root, 0.955f, spring, new Vector3((float)root.ActualWidth / 2f, 0f, 0f));
			}
			else
			{
				root.SizeChanged += Grow;
			}
		}
		void Grow(object sender, SizeChangedEventArgs e)
		{
			root.SizeChanged -= Grow;
			Anim.ScaleUpFrom(root, 0.955f, spring, new Vector3((float)e.NewSize.Width / 2f, 0f, 0f));
		}
	}

	private void Build()
	{
		window = new Microsoft.UI.Xaml.Window
		{
			Title = "HeyClicky"
		};
		hwnd = WindowNative.GetWindowHandle(window);
		appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
		if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.IsResizable = true;
			overlappedPresenter.IsMaximizable = true;
			overlappedPresenter.IsMinimizable = true;
			overlappedPresenter.PreferredMinimumWidth = 720;
			overlappedPresenter.PreferredMinimumHeight = 480;
		}
		window.ExtendsContentIntoTitleBar = true;
		appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
		appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		palette = HeyClickyWindowPalette.Current();
		PaintCaptionButtons();
		try
		{
			appWindow.SetIcon(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
		}
		catch
		{
		}
		host = new HeyClickyWindowHost(notchHost, this, hwnd);
		root = BuildContent();
		ground = new Grid
		{
			Background = Ui.Brush(palette.Ground),
			RequestedTheme = ((!palette.IsDark) ? ElementTheme.Light : ElementTheme.Dark)
		};
		ground.Children.Add(root);
		window.Content = ground;
		PlaceFromSavedFrame();
		Microsoft.UI.Dispatching.DispatcherQueue queue = window.DispatcherQueue;
		lookWatcher = new SystemLookWatcher(delegate
		{
			queue.TryEnqueue(ApplySystemLook);
		});
		BillingUi billing = notchHost.Services.Billing;
		if (billing != null)
		{
			billing.Changed += RebuildAccountRow;
		}
		if (notchHost.Services.Presenter is NotchPresenter notchPresenter)
		{
			notchPresenter.PresentationChanged += OnPresentationChanged;
		}
		if (notchHost.Services.Agents is AgentService agentService)
		{
			agentService.Jobs.Changed += OnAgentsChanged;
		}
		if (dock != null)
		{
			dock.ContentChanged += OnDockContentChanged;
		}
		window.Activated += delegate(object _, WindowActivatedEventArgs e)
		{
			bool flag = e.WindowActivationState != WindowActivationState.Deactivated;
			if (!flag)
			{
				lastDeactivatedTicks = Environment.TickCount64;
			}
			if (flag != isActive)
			{
				isActive = flag;
				host.NoteActive(flag);
			}
		};
		appWindow.Changed += delegate(AppWindow _, AppWindowChangedEventArgs e)
		{
			if (e.DidPositionChange || e.DidSizeChange)
			{
				SaveFrameSoon();
			}
		};
		window.Closed += delegate
		{
			OnClosed();
		};
	}

	private void OnClosed()
	{
		SaveFrame();
		mainChat = null;
		settings?.Hidden();
		settings = null;
		documentViewer = null;
		documentPath = null;
		documentThreadId = null;
		window = null;
		appWindow = null;
		hwnd = 0;
		root = null;
		ground = null;
		mainCard = null;
		columnHeader = null;
		dragBand = null;
		headerButtons.Clear();
		shownAgentKey = null;
		shownChild = null;
		stage = null;
		if (notchHost?.Services.Presenter is NotchPresenter notchPresenter)
		{
			notchPresenter.PresentationChanged -= OnPresentationChanged;
		}
		(notchHost?.Services.Presenter as NotchPresenter)?.SetWindowShowsMainThread(shows: false);
		ChatHandOffs handOffs = NotchChat.HandOffs;
		if (handOffs != null)
		{
			handOffs.Changed -= OnHandOffsChanged;
		}
		if (notchHost?.Services.Agents is AgentService agentService)
		{
			agentService.Jobs.Changed -= OnAgentsChanged;
		}
		if (dock != null)
		{
			dock.ContentChanged -= OnDockContentChanged;
		}
		CompositionTarget.Rendering -= OnColumnFrame;
		columnFramesRunning = false;
		CompositionTarget.Rendering -= OnWindowMoveFrame;
		windowMoving = false;
		focusRest = null;
		lookWatcher?.Dispose();
		lookWatcher = null;
		BillingUi billingUi = notchHost?.Services.Billing;
		if (billingUi != null)
		{
			billingUi.Changed -= RebuildAccountRow;
		}
		accountSlot = null;
		dropOverlay = null;
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "window_closed" });
	}

	public void Close()
	{
		window?.Close();
	}

	private Grid BuildContent()
	{
		using (MessagesPalette.Use(Look))
		{
			MessagesPalette look = Look;
			Grid obj = new Grid
			{
				Background = Ui.Brush(palette.Ground),
				RequestedTheme = ((!palette.IsDark) ? ElementTheme.Light : ElementTheme.Dark)
			};
			sidebarDefinition = new ColumnDefinition
			{
				Width = new GridLength(saved.SidebarShown ? 244.0 : 0.0)
			};
			documentDefinition = new ColumnDefinition
			{
				Width = new GridLength(0.0)
			};
			obj.ColumnDefinitions.Add(sidebarDefinition);
			obj.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star),
				MinWidth = 380.0
			});
			obj.ColumnDefinitions.Add(documentDefinition);
			focusRest = new ContentControl
			{
				Width = 1.0,
				Height = 1.0,
				IsTabStop = true,
				UseSystemFocusVisuals = false,
				Opacity = 0.0,
				IsHitTestVisible = false,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			AutomationProperties.SetName(focusRest, "HeyClicky");
			obj.Children.Add(focusRest);
			dragBand = new Grid
			{
				Height = 36.0,
				VerticalAlignment = VerticalAlignment.Top,
				Background = Ui.Transparent
			};
			Grid.SetColumnSpan(dragBand, 3);
			obj.Children.Add(dragBand);
			window.SetTitleBar(dragBand);
			headerButtons.Clear();
			sidebarColumn = BuildSidebar();
			sidebarColumn.Width = 244.0;
			sidebarColumn.HorizontalAlignment = HorizontalAlignment.Right;
			obj.Children.Add(sidebarColumn);
			sidebarShownness = new SpringValue(SidebarVisible ? 1 : 0, 0.0005);
			documentShownness = new SpringValue(0.0, 0.0005);
			obj.SizeChanged += delegate
			{
				if (!columnFramesRunning)
				{
					ApplyColumnFrame();
				}
			};
			Grid grid = new Grid
			{
				RowDefinitions = 
				{
					new RowDefinition
					{
						Height = GridLength.Auto
					},
					new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					}
				}
			};
			chatDots = new ClickyPaperBoardDots(look.ChatDots, followsResizing: true);
			Grid.SetRowSpan(chatDots, 2);
			grid.Children.Add(chatDots);
			columnHeader = new Border();
			grid.Children.Add(columnHeader);
			stage = new Grid();
			shownChild = null;
			mainColumn = new Border
			{
				Child = stage
			};
			Grid.SetRow(mainColumn, 1);
			grid.Children.Add(mainColumn);
			mainCard = new Border
			{
				Background = Ui.Brush(look.PaneFill),
				CornerRadius = new CornerRadius(12.0),
				BorderBrush = Ui.Brush(look.InkAt(0.08)),
				BorderThickness = new Thickness(1.0),
				Child = grid
			};
			Grid.SetColumn(mainCard, 1);
			obj.Children.Add(mainCard);
			Ui.ClipRounded(mainCard, 12.0);
			AcceptFileDrops(mainColumn);
			dropOverlay = DropTargetOverlay();
			dropTargeted = false;
			Grid.SetColumn(dropOverlay, 1);
			obj.Children.Add(dropOverlay);
			documentColumn = BuildDocumentColumn();
			Grid.SetColumn(documentColumn, 2);
			documentColumn.Visibility = Visibility.Collapsed;
			documentColumn.Opacity = 0.0;
			obj.Children.Add(documentColumn);
			sidebarButton = HeaderButton("\ue90c", SidebarButtonTip, ToggleSidebar);
			backButton = BackChip();
			backButton.Visibility = Visibility.Collapsed;
			topLeft = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0,
				Height = 36.0,
				Margin = new Thickness(14.0, 0.0, 0.0, 0.0),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Children = 
				{
					(UIElement)sidebarButton,
					(UIElement)backButton
				}
			};
			Grid.SetColumnSpan(topLeft, 3);
			obj.Children.Add(topLeft);
			LayOutCards(animated: false);
			obj.AddHandler(UIElement.PreviewKeyDownEvent, (KeyEventHandler)delegate(object _, KeyRoutedEventArgs e)
			{
				if (e.Key == VirtualKey.Escape && LeaveTextInput())
				{
					e.Handled = true;
				}
			}, handledEventsToo: true);
			KeyboardAccelerator keyboardAccelerator = new KeyboardAccelerator
			{
				Key = VirtualKey.Escape
			};
			keyboardAccelerator.Invoked += delegate(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs e)
			{
				if (documentPath != null)
				{
					e.Handled = true;
					CloseDocument();
				}
			};
			KeyboardAccelerator keyboardAccelerator2 = new KeyboardAccelerator
			{
				Key = VirtualKey.W,
				Modifiers = VirtualKeyModifiers.Control
			};
			keyboardAccelerator2.Invoked += delegate(KeyboardAccelerator _, KeyboardAcceleratorInvokedEventArgs e)
			{
				e.Handled = true;
				Close();
			};
			obj.KeyboardAccelerators.Add(keyboardAccelerator);
			obj.KeyboardAccelerators.Add(keyboardAccelerator2);
			obj.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
			obj.LayoutUpdated += delegate
			{
				LetHeaderButtonsThrough();
			};
			ChatHandOffs handOffs = NotchChat.HandOffs;
			if (handOffs != null)
			{
				handOffs.Changed -= OnHandOffsChanged;
				handOffs.Changed += OnHandOffsChanged;
			}
			return obj;
		}
		bool LeaveTextInput()
		{
			if (!TextInputHasFocus())
			{
				return false;
			}
			ParkFocus(force: true);
			return true;
		}
	}

	private void LayOutCards(bool animated = true)
	{
		bool flag = animated && !Anim.ReduceMotion && (root?.IsLoaded ?? false);
		sidebarShownness?.Move(SidebarVisible ? 1 : 0, flag ? new SwiftSpring?(ColumnSpring) : ((SwiftSpring?)null));
		documentShownness?.Move((documentPath != null) ? 1 : 0, flag ? new SwiftSpring?(ColumnSpring) : ((SwiftSpring?)null));
		if (!ApplyColumnFrame())
		{
			StartColumnFrames();
		}
		if ((object)sidebarButton != null)
		{
			sidebarButton.Visibility = (showingSettings ? Visibility.Collapsed : Visibility.Visible);
		}
		if ((object)backButton != null)
		{
			backButton.Visibility = ((!showingSettings) ? Visibility.Collapsed : Visibility.Visible);
		}
		if ((object)headerChild != null)
		{
			headerChild.Margin = ColumnHeaderInsets();
		}
	}

	private void StartColumnFrames()
	{
		if (!columnFramesRunning)
		{
			columnFramesRunning = true;
			CompositionTarget.Rendering += OnColumnFrame;
		}
	}

	private void OnColumnFrame(object? sender, object e)
	{
		if (ApplyColumnFrame() || (object)root == null)
		{
			CompositionTarget.Rendering -= OnColumnFrame;
			columnFramesRunning = false;
		}
	}

	private bool ApplyColumnFrame()
	{
		if ((object)sidebarDefinition == null || (object)documentDefinition == null || sidebarShownness == null || documentShownness == null)
		{
			return true;
		}
		double now = SpringCurve.Now;
		double num = Math.Clamp(sidebarShownness.ValueAt(now), 0.0, 1.0);
		double num2 = Math.Clamp(documentShownness.ValueAt(now), 0.0, 1.0);
		sidebarDefinition.Width = new GridLength(244.0 * num);
		if ((object)sidebarColumn != null)
		{
			sidebarColumn.Opacity = num;
			sidebarColumn.Visibility = ((!(num > 0.0)) ? Visibility.Collapsed : Visibility.Visible);
		}
		if ((object)mainCard != null)
		{
			mainCard.Margin = new Thickness(8.0 * (1.0 - num), 36.0, 8.0, 8.0);
		}
		if ((object)dropOverlay != null && (object)mainCard != null)
		{
			dropOverlay.Margin = mainCard.Margin;
		}
		double num3 = ClampDocumentWidth(saved.DocumentWidth);
		documentDefinition.Width = new GridLength(num3 * num2);
		if ((object)documentPlate != null)
		{
			documentPlate.Width = Math.Max(0.0, num3 - 8.0);
		}
		if ((object)documentColumn != null)
		{
			documentColumn.Opacity = num2;
			documentColumn.Visibility = ((!(num2 > 0.0) && documentPath == null) ? Visibility.Collapsed : Visibility.Visible);
		}
		int num4;
		if (num == sidebarShownness.Target)
		{
			num4 = ((num2 == documentShownness.Target) ? 1 : 0);
			if (num4 != 0 && documentPath == null && (object)documentViewer != null && documentViewerHolds)
			{
				documentViewerHolds = false;
				documentViewer.Show(null);
			}
		}
		else
		{
			num4 = 0;
		}
		return (byte)num4 != 0;
	}

	private Thickness ColumnHeaderInsets()
	{
		return new Thickness((!showingSettings) ? 16 : 0, 0.0, 12.0, 0.0);
	}

	private Pressable HeaderButton(string glyph, string tip, Action click)
	{
		MessagesPalette m = Look;
		Pressable p = new Pressable
		{
			Width = 26.0,
			Height = 24.0,
			CornerRadius = new CornerRadius(6.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock g = Icons.Glyph(glyph, 12.0, m.InkAt(0.6));
		g.HorizontalAlignment = HorizontalAlignment.Center;
		g.VerticalAlignment = VerticalAlignment.Center;
		p.Children.Add(g);
		p.HoverChanged += delegate(bool h)
		{
			p.Background = (h ? Ui.Brush(m.InkAt(0.1)) : null);
			g.Foreground = Ui.Brush(m.InkAt(h ? 0.95 : 0.6));
		};
		p.Click += click;
		p.Tip(tip);
		AutomationProperties.SetName(p, tip);
		headerButtons.Add(p);
		return p;
	}

	private Pressable BackChip()
	{
		MessagesPalette m = Look;
		Pressable chip = new Pressable
		{
			Height = 24.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			CornerRadius = new CornerRadius(12.0),
			VerticalAlignment = VerticalAlignment.Center,
			Background = Ui.Brush(m.InkAt(0.08)),
			BorderBrush = Ui.Brush(m.InkAt(0.1)),
			BorderThickness = new Thickness(1.0)
		};
		TextBlock arrow = Icons.Glyph("\ue72b", 10.5, m.InkAt(0.78));
		arrow.FontWeight = FontWeights.Bold;
		arrow.VerticalAlignment = VerticalAlignment.Center;
		TextBlock words = Ui.Text("Back", 12.0, FontWeights.SemiBold, m.InkAt(0.78), TextAlignment.Left, 1);
		words.VerticalAlignment = VerticalAlignment.Center;
		chip.Children.Add(new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)arrow,
				(UIElement)words
			}
		});
		chip.HoverChanged += delegate(bool h)
		{
			chip.Background = Ui.Brush(m.InkAt(h ? 0.14 : 0.08));
			TextBlock textBlock = arrow;
			Brush foreground = (words.Foreground = Ui.Brush(m.InkAt(h ? 0.95 : 0.78)));
			textBlock.Foreground = foreground;
		};
		chip.Click += LeaveSettings;
		chip.Tip("Back to chats");
		AutomationProperties.SetName(chip, "Back to chats");
		headerButtons.Add(chip);
		return chip;
	}

	private void LetHeaderButtonsThrough()
	{
		if ((object)appWindow == null || (object)root == null)
		{
			return;
		}
		XamlRoot xamlRoot = root.XamlRoot;
		if ((object)xamlRoot == null)
		{
			return;
		}
		double rasterizationScale = xamlRoot.RasterizationScale;
		List<RectInt32> list = new List<RectInt32>();
		foreach (FrameworkElement headerButton in headerButtons)
		{
			if (headerButton.Visibility == Visibility.Visible && !(headerButton.ActualWidth <= 0.0) && IsShown(headerButton))
			{
				Point point = headerButton.TransformToVisual(root).TransformPoint(new Point(0f, 0f));
				list.Add(new RectInt32((int)Math.Floor(point.X * rasterizationScale), (int)Math.Floor(point.Y * rasterizationScale), (int)Math.Ceiling(headerButton.ActualWidth * rasterizationScale), (int)Math.Ceiling(headerButton.ActualHeight * rasterizationScale)));
			}
		}
		string text = string.Join(";", list.Select((RectInt32 r) => $"{r.X},{r.Y},{r.Width},{r.Height}"));
		if (text == passthroughShape)
		{
			return;
		}
		passthroughShape = text;
		try
		{
			InputNonClientPointerSource.GetForWindowId(appWindow.Id).SetRegionRects(NonClientRegionKind.Passthrough, list.ToArray());
		}
		catch (Exception ex)
		{
			Log.Warn("window", "header buttons not let through: " + ex.Message);
		}
	}

	private bool IsShown(FrameworkElement element)
	{
		DependencyObject dependencyObject = element;
		while ((object)dependencyObject != null && dependencyObject != root)
		{
			if (dependencyObject is UIElement { Visibility: Visibility.Collapsed })
			{
				return false;
			}
			dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
		}
		return true;
	}

	private bool TextInputHasFocus()
	{
		XamlRoot xamlRoot = root?.XamlRoot;
		bool flag = (object)xamlRoot != null;
		if (flag)
		{
			object focusedElement = FocusManager.GetFocusedElement(xamlRoot);
			bool flag2 = ((focusedElement is TextBox || focusedElement is PasswordBox || focusedElement is RichEditBox || focusedElement is AutoSuggestBox) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private void ParkFocus(bool force)
	{
		if ((object)focusRest != null && (force || TextInputHasFocus()))
		{
			focusRest.Focus(FocusState.Programmatic);
		}
	}

	private bool FocusIsWithin(UIElement? element)
	{
		if ((object)element != null)
		{
			XamlRoot xamlRoot = root?.XamlRoot;
			if ((object)xamlRoot != null && FocusManager.GetFocusedElement(xamlRoot) is DependencyObject dependencyObject)
			{
				DependencyObject dependencyObject2 = dependencyObject;
				while ((object)dependencyObject2 != null)
				{
					if ((object)dependencyObject2 == element)
					{
						return true;
					}
					dependencyObject2 = VisualTreeHelper.GetParent(dependencyObject2);
				}
				return false;
			}
		}
		return false;
	}

	private void PaintCaptionButtons()
	{
		if ((object)appWindow != null)
		{
			appWindow.TitleBar.ButtonForegroundColor = palette.InkAt(0.85);
			appWindow.TitleBar.ButtonHoverForegroundColor = palette.InkAt(0.95);
			appWindow.TitleBar.ButtonHoverBackgroundColor = palette.InkAt(0.08);
			appWindow.TitleBar.ButtonPressedBackgroundColor = palette.InkAt(0.12);
			appWindow.TitleBar.ButtonInactiveForegroundColor = palette.InkAt(0.4);
		}
	}

	private void ApplySystemLook()
	{
		if ((object)root == null || (object)ground == null)
		{
			return;
		}
		HeyClickyWindowPalette heyClickyWindowPalette = HeyClickyWindowPalette.Current();
		if (heyClickyWindowPalette.IsDark != palette.IsDark)
		{
			palette = heyClickyWindowPalette;
			PaintCaptionButtons();
			mainChat = null;
			settings?.Hidden();
			settings = null;
			documentViewer = null;
			string text = documentPath;
			string threadId = documentThreadId;
			documentPath = (documentThreadId = null);
			bool num = showingSettings;
			showingSettings = false;
			ground.Background = Ui.Brush(palette.Ground);
			ground.RequestedTheme = ((!palette.IsDark) ? ElementTheme.Light : ElementTheme.Dark);
			ground.Children.Clear();
			passthroughShape = "";
			root = BuildContent();
			ground.Children.Add(root);
			if (num)
			{
				ShowSettings();
			}
			else
			{
				string key = selectedKey;
				selectedKey = "";
				Select(key);
			}
			if (text != null)
			{
				ShowDocument(text, threadId);
			}
			SettleColumns();
		}
	}

	private void SettleColumns()
	{
		sidebarShownness = new SpringValue(SidebarVisible ? 1 : 0, 0.0005);
		documentShownness = new SpringValue((documentPath != null) ? 1 : 0, 0.0005);
		ApplyColumnFrame();
	}

	private void AcceptFileDrops(Border column)
	{
		column.AllowDrop = true;
		column.AddHandler(UIElement.DragOverEvent, (DragEventHandler)delegate(object _, DragEventArgs e)
		{
			if (e.DataView.Contains(StandardDataFormats.StorageItems) && (object)DropTarget() != null)
			{
				e.AcceptedOperation = DataPackageOperation.Copy;
				e.DragUIOverride.Caption = "Attach";
				dragLeaveCount++;
				ShowDropTargeting(on: true);
			}
		}, handledEventsToo: true);
		column.AddHandler(UIElement.DragLeaveEvent, (DragEventHandler)delegate
		{
			int leave = ++dragLeaveCount;
			Microsoft.UI.Dispatching.DispatcherQueueTimer t = column.DispatcherQueue.CreateTimer().Rooted();
			t.Interval = TimeSpan.FromSeconds(0.12);
			t.IsRepeating = false;
			t.Tick += delegate
			{
				t.Stop();
				if (leave == dragLeaveCount)
				{
					ShowDropTargeting(on: false);
				}
			};
			t.Start();
		}, handledEventsToo: true);
		column.AddHandler(UIElement.DropEvent, (DragEventHandler)async delegate(object _, DragEventArgs e)
		{
			ShowDropTargeting(on: false);
			if (!e.DataView.Contains(StandardDataFormats.StorageItems))
			{
				return;
			}
			DragOperationDeferral deferral = e.GetDeferral();
			try
			{
				List<string> list = (from f in (await e.DataView.GetStorageItemsAsync()).OfType<StorageFile>()
					select f.Path into p
					where p.Length > 0
					select p).Distinct().ToList();
				if (list.Count != 0)
				{
					FrameworkElement frameworkElement = DropTarget();
					if (!(frameworkElement is NotchChatView notchChatView))
					{
						if (!(frameworkElement is HeyClickyWindowConversation heyClickyWindowConversation))
						{
							return;
						}
						heyClickyWindowConversation.AttachDroppedFiles(list);
					}
					else
					{
						notchChatView.AttachDroppedFiles(list);
					}
					ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null)
					{
						["action"] = "window_files_dropped",
						["file_count"] = list.Count
					});
				}
			}
			catch (Exception ex)
			{
				Log.Warn("window", "dropped files unreadable: " + ex.Message);
			}
			finally
			{
				deferral.Complete();
			}
		}, handledEventsToo: true);
	}

	private FrameworkElement? DropTarget()
	{
		object obj;
		if (!showingSettings)
		{
			obj = shownChild as NotchChatView;
			if (obj == null)
			{
				return shownChild as HeyClickyWindowConversation;
			}
		}
		else
		{
			obj = null;
		}
		return (FrameworkElement?)obj;
	}

	private void ShowDropTargeting(bool on)
	{
		Grid overlay = dropOverlay;
		if ((object)overlay != null && dropTargeted != on)
		{
			dropTargeted = on;
			if (on)
			{
				overlay.Visibility = Visibility.Visible;
			}
			if (Anim.ReduceMotion)
			{
				overlay.Opacity = (on ? 1 : 0);
				if (!on)
				{
					overlay.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				Anim.Opacity(overlay, on ? 1 : 0, 0.14, Easing.EaseOut(Ui.Compositor));
				if (!on)
				{
					Microsoft.UI.Dispatching.DispatcherQueueTimer t = overlay.DispatcherQueue.CreateTimer().Rooted();
					t.Interval = TimeSpan.FromSeconds(0.16);
					t.IsRepeating = false;
					t.Tick += delegate
					{
						t.Stop();
						if (!dropTargeted)
						{
							overlay.Visibility = Visibility.Collapsed;
						}
					};
					t.Start();
				}
			}
		}
		FrameworkElement frameworkElement = DropTarget();
		if (!(frameworkElement is NotchChatView notchChatView))
		{
			if (frameworkElement is HeyClickyWindowConversation heyClickyWindowConversation)
			{
				heyClickyWindowConversation.SetDropTargeting(on);
			}
		}
		else
		{
			notchChatView.SetDropTargeting(on);
		}
	}

	private Grid DropTargetOverlay()
	{
		MessagesPalette look = Look;
		Grid obj = new Grid
		{
			IsHitTestVisible = false,
			Visibility = Visibility.Collapsed,
			Opacity = 0.0,
			Children = 
			{
				(UIElement)new Border
				{
					CornerRadius = new CornerRadius(12.0),
					Background = Ui.Brush(look.PaneFill.WithAlpha(0.72))
				},
				(UIElement)new Rectangle
				{
					Margin = new Thickness(6.0),
					RadiusX = 11.0,
					RadiusY = 11.0,
					Stroke = Ui.Brush(DS.Hex(3112182u, 0.8)),
					StrokeThickness = 2.0,
					StrokeDashArray = new DoubleCollection { 4.0, 3.0 }
				}
			}
		};
		TextBlock textBlock = Icons.Glyph("\ue723", 22.0, look.PrimaryText);
		textBlock.FontWeight = FontWeights.SemiBold;
		textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		TextBlock textBlock2 = Ui.Text("Drop to attach", 14.0, FontWeights.SemiBold, look.PrimaryText);
		textBlock2.HorizontalAlignment = HorizontalAlignment.Center;
		obj.Children.Add(new StackPanel
		{
			Spacing = 8.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)textBlock,
				(UIElement)textBlock2
			}
		});
		return obj;
	}

	private void ToggleSidebar()
	{
		saved = saved with
		{
			SidebarShown = !saved.SidebarShown
		};
		if ((object)sidebarButton != null)
		{
			sidebarButton.Tip(SidebarButtonTip);
			AutomationProperties.SetName(sidebarButton, SidebarButtonTip);
		}
		LayOutCards();
		SaveFrame();
	}

	private Grid BuildSidebar()
	{
		Grid obj = new Grid
		{
			RowDefinitions = 
			{
				new RowDefinition
				{
					Height = new GridLength(44.0)
				},
				new RowDefinition
				{
					Height = new GridLength(1.0, GridUnitType.Star)
				},
				new RowDefinition
				{
					Height = GridLength.Auto
				}
			}
		};
		sidebarViews.Clear();
		sidebarSections.Clear();
		sidebarShape = "";
		sidebarRows = new StackPanel
		{
			Spacing = 3.0,
			Padding = new Thickness(8.0, 0.0, 8.0, 12.0)
		};
		ScrollViewer scrollViewer = new ScrollViewer
		{
			Content = sidebarRows,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		Grid.SetRow(scrollViewer, 1);
		obj.Children.Add(scrollViewer);
		accountSlot = new Border();
		Grid.SetRow(accountSlot, 2);
		obj.Children.Add(accountSlot);
		RebuildAccountRow();
		RebuildSidebar();
		return obj;
	}

	private List<SidebarEntry> SidebarEntries()
	{
		List<SidebarEntry> list = new List<SidebarEntry>();
		if (!(notchHost?.Services.Agents is AgentService agentService))
		{
			return list;
		}
		HashSet<string> hashSet = dock?.DockKeys().ToHashSet() ?? new HashSet<string>();
		foreach (ClickyJob job in agentService.Jobs.Jobs)
		{
			if (job.IsArchived != true && !job.IsWaitingToStart)
			{
				ArcCardState? state = dock?.ContentForKey(job.RailKey)?.State;
				DateTimeOffset latestAt = dock?.LatestActivity(job.RailKey) ?? job.FinishedAt ?? job.CreatedAt;
				list.Add(new SidebarEntry(job.RailKey, ShortName(job.DisplayName), job.Id, null, state, latestAt, hashSet.Contains(job.RailKey)));
			}
		}
		foreach (ClickyInfo clicky in agentService.Clickys)
		{
			if (clicky.ThreadId != null || (object)agentService.LiveStatusForClicky(clicky.Slug) != ClickyAppLiveStatus.Idle)
			{
				ArcCardState? state2 = dock?.ContentForKey(clicky.Slug)?.State;
				DateTimeOffset latestAt2 = dock?.LatestActivity(clicky.Slug) ?? DateTimeOffset.MinValue;
				list.Add(new SidebarEntry(clicky.Slug, ShortName(clicky.Name), null, clicky.Slug, state2, latestAt2, hashSet.Contains(clicky.Slug)));
			}
		}
		return list.OrderByDescending((SidebarEntry e) => e.LatestAt).ToList();
	}

	internal static string ShortName(string name)
	{
		string[] array = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length <= 3)
		{
			return name;
		}
		string[] array2 = array.Where((string w) => !LittleWords.Contains(w)).ToArray();
		if (array2.Length == 0)
		{
			array2 = array;
		}
		return string.Join(' ', array2.Take(3));
	}

	private void RebuildSidebar()
	{
		if ((object)sidebarRows == null)
		{
			return;
		}
		string selected;
		bool firstBuild;
		List<FrameworkElement> views;
		HashSet<string> used;
		Dictionary<string, string> sections;
		using (MessagesPalette.Use(Look))
		{
			bool answering = (notchHost?.Services.Presenter as NotchPresenter)?.IsTextResponseInProgress ?? false;
			List<SidebarEntry> list = SidebarEntries();
			List<SidebarEntry> list2 = list.Where(delegate(SidebarEntry e)
			{
				switch (e.State)
				{
				case ArcCardState.Queued:
				case ArcCardState.Working:
				case ArcCardState.NeedsYou:
				case ArcCardState.Pinning:
					return true;
				default:
					return false;
				}
			}).ToList();
			List<SidebarEntry> list3 = (from e in list.Except(list2)
				where e.OnDock
				select e).ToList();
			List<SidebarEntry> earlier = list.Except(list2).Except(list3).ToList();
			bool flag = earlierUnfolded || (list2.Count == 0 && list3.Count == 0);
			selected = (showingSettings ? "" : selectedKey);
			StringBuilder stringBuilder = new StringBuilder().Append(answering).Append('|').Append(selected)
				.Append('|')
				.Append(flag)
				.Append('|')
				.Append(earlier.Count);
			(string, List<SidebarEntry>)[] array = new(string, List<SidebarEntry>)[3]
			{
				("w", list2),
				("r", list3),
				("e", earlier)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(string, List<SidebarEntry>) tuple = array[i];
				var (value, _) = tuple;
				foreach (SidebarEntry item in tuple.Item2)
				{
					stringBuilder.Append('\u001e').Append(value).Append(item.Key)
						.Append('\u001f')
						.Append(item.Name)
						.Append('\u001f')
						.Append(item.State);
				}
			}
			if (stringBuilder.ToString() == sidebarShape && sidebarRows.Children.Count > 0)
			{
				return;
			}
			firstBuild = sidebarShape.Length == 0;
			sidebarShape = stringBuilder.ToString();
			views = new List<FrameworkElement>();
			used = new HashSet<string>();
			sections = new Dictionary<string, string>();
			views.Add(View($"main\u001f{answering}\u001f{selected == "heyclicky"}", delegate
			{
				FrameworkElement frameworkElement = SidebarRow("heyclicky", "HeyClicky", null, null, answering ? new ArcCardState?(ArcCardState.Working) : ((ArcCardState?)null), isMain: true);
				frameworkElement.Margin = new Thickness(0.0, 8.0, 0.0, 10.0);
				return frameworkElement;
			}));
			Section("Working", "\ue70f", 5, list2);
			Section("Recents", "\ue823", 11, list3, closesToEarlier: true);
			if (earlier.Count > 0)
			{
				views.Add(View($"earlier\u001f{earlier.Count}\u001f{earlierUnfolded}", () => EarlierHeader(earlier.Count)));
				if (flag)
				{
					foreach (SidebarEntry item2 in earlier)
					{
						Row(item2, "Earlier", closesToEarlier: false);
					}
				}
			}
			foreach (string item3 in sidebarViews.Keys.Where((string k) => !used.Contains(k)).ToList())
			{
				sidebarViews.Remove(item3);
			}
			sidebarSections.Clear();
			foreach (var (key, value2) in sections)
			{
				sidebarSections[key] = value2;
			}
			ReconcileChildren(sidebarRows.Children, views);
		}
		void Row(SidebarEntry e, string section, bool closesToEarlier)
		{
			bool closes = closesToEarlier && dock != null;
			string id2 = $"row\u001f{e.Key}\u001f{e.Name}\u001f{e.State}\u001f{e.Key == selected}\u001f{closes}";
			FrameworkElement frameworkElement2 = View(id2, () => SidebarRow(e.Key, e.Name, e.JobId, e.ClickySlug, e.State, isMain: false, closes ? ((Action)delegate
			{
				MoveToEarlier(e.Key);
			}) : null));
			if (!firstBuild && (!sidebarSections.TryGetValue(e.Key, out string value4) || value4 != section))
			{
				FadeInRow(frameworkElement2);
			}
			sections[e.Key] = section;
			views.Add(frameworkElement2);
		}
		void Section(string title, string symbol, int seed, List<SidebarEntry> rows, bool closesToEarlier = false)
		{
			if (rows.Count != 0)
			{
				views.Add(View("label\u001f" + title, () => SectionLabel(title, symbol, seed)));
				foreach (SidebarEntry row in rows)
				{
					Row(row, title, closesToEarlier);
				}
				views.Add(View("gap\u001f" + title, () => new Border
				{
					Height = 12.0
				}));
			}
		}
		FrameworkElement View(string id, Func<FrameworkElement> make)
		{
			used.Add(id);
			if (!sidebarViews.TryGetValue(id, out FrameworkElement value3))
			{
				value3 = make();
				sidebarViews[id] = value3;
			}
			return value3;
		}
	}

	private static void FadeInRow(FrameworkElement view)
	{
		if (!Anim.ReduceMotion)
		{
			view.Opacity = 0.0;
			Anim.Opacity(view, 1f, 0.22, Easing.EaseOut(Ui.Compositor));
		}
	}

	private static void ReconcileChildren(UIElementCollection children, List<FrameworkElement> views)
	{
		HashSet<UIElement> hashSet = new HashSet<UIElement>(views);
		for (int num = children.Count - 1; num >= 0; num--)
		{
			if (!hashSet.Contains(children[num]))
			{
				children.RemoveAt(num);
			}
		}
		for (int i = 0; i < views.Count; i++)
		{
			if (i >= children.Count || (object)children[i] != views[i])
			{
				int num2 = children.IndexOf(views[i]);
				if (num2 >= 0)
				{
					children.RemoveAt(num2);
				}
				children.Insert(i, views[i]);
			}
		}
	}

	private void MoveToEarlier(string key)
	{
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "window_sidebar_moved_to_earlier",
			["is_job"] = key.StartsWith("job:", StringComparison.Ordinal)
		});
		dock?.MoveToEarlier(key);
		RebuildSidebar();
	}

	private FrameworkElement SectionLabel(string title, string symbol, int seed)
	{
		MessagesPalette look = Look;
		TextBlock textBlock = Icons.Glyph(symbol, 9.0, look.TertiaryText);
		textBlock.FontWeight = FontWeights.Bold;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		TextBlock textBlock2 = Ui.Text(title.ToUpperInvariant(), 10.0, FontWeights.Black, look.TertiaryText, TextAlignment.Left, 1);
		textBlock2.CharacterSpacing = 130;
		textBlock2.VerticalAlignment = VerticalAlignment.Center;
		PaperDrawing paperDrawing = new PaperDrawing
		{
			Width = 66.0,
			Height = 5.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		paperDrawing.Stroke(ClickySketchPath.Line(new Point(0f, 2f), new Point(64.0, 2.5), seed, 0.8, 8.0), new PaperStroke(look.InkAt(0.22), 1.2, PenLineCap.Round));
		return new StackPanel
		{
			Spacing = 3.0,
			Padding = new Thickness(10.0, 8.0, 10.0, 4.0),
			Children = 
			{
				(UIElement)new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 5.0,
					Children = 
					{
						(UIElement)textBlock,
						(UIElement)textBlock2
					}
				},
				(UIElement)paperDrawing
			}
		};
	}

	private FrameworkElement EarlierHeader(int count)
	{
		MessagesPalette look = Look;
		Pressable obj = new Pressable
		{
			Background = Ui.Transparent
		};
		Grid grid = new Grid
		{
			ColumnSpacing = 6.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		FrameworkElement frameworkElement = SectionLabel("Earlier", "\ue7b8", 19);
		frameworkElement.Margin = new Thickness(0.0, 0.0, -10.0, 0.0);
		grid.Children.Add(frameworkElement);
		TextBlock textBlock = Ui.Text(count.ToString(CultureInfo.InvariantCulture), 10.0, FontWeights.Bold, look.TertiaryText, TextAlignment.Left, 1);
		textBlock.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		textBlock.VerticalAlignment = VerticalAlignment.Top;
		Grid.SetColumn(textBlock, 1);
		grid.Children.Add(textBlock);
		TextBlock textBlock2 = Icons.Glyph("\ue76c", 10.0, look.TertiaryText);
		textBlock2.FontWeight = FontWeights.Bold;
		textBlock2.Margin = new Thickness(0.0, 9.0, 12.0, 0.0);
		textBlock2.VerticalAlignment = VerticalAlignment.Top;
		textBlock2.RenderTransformOrigin = new Point(0.5, 0.5);
		textBlock2.RenderTransform = new RotateTransform
		{
			Angle = (earlierUnfolded ? 90 : 0)
		};
		Grid.SetColumn(textBlock2, 3);
		grid.Children.Add(textBlock2);
		obj.Children.Add(grid);
		obj.Click += delegate
		{
			earlierUnfolded = !earlierUnfolded;
			RebuildSidebar();
		};
		obj.Tip(earlierUnfolded ? "Hide earlier chats" : "Show earlier chats");
		return obj;
	}

	private FrameworkElement SidebarRow(string key, string name, string? jobId, string? clickySlug, ArcCardState? state, bool isMain = false, Action? moveToEarlier = null)
	{
		MessagesPalette m = Look;
		bool selected = !showingSettings && key == selectedKey;
		PaperStock stock = ((!isMain) ? NotePaperColors.Stock((jobId != null) ? ("job:" + jobId) : ("clicky:" + clickySlug)) : PaperStock.Sky);
		ArcPaper paper = new ArcPaper(stock);
		int seed = NotePaperColors.Seed(key);
		Pressable row = new Pressable
		{
			Height = 38.0,
			Background = Ui.Transparent
		};
		if (selected)
		{
			row.Children.Add(SelectedSlip(paper, seed));
		}
		PaperSurface wash = null;
		Grid grid = new Grid
		{
			ColumnSpacing = 10.0,
			Padding = new Thickness(8.0, 0.0, 9.0, 0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		FrameworkElement frameworkElement = (isMain ? ((Grid)new HandoffChipView(HandoffGlyph.HeyClicky, key, 24.0, PaperStock.Sky, !selected)) : ((Grid)new NoteChipView((jobId != null) ? ("job:" + jobId) : ("clicky:" + clickySlug), ClickyInkIcon.Inferred(name) ?? ((jobId != null) ? "checklist" : "sparkles"), seed, seed, 24.0, !selected)));
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
		CompositeTransform chipTurn = new CompositeTransform();
		frameworkElement.RenderTransform = chipTurn;
		grid.Children.Add(frameworkElement);
		TextBlock textBlock = Ui.Text(name, 13.0, selected ? FontWeights.SemiBold : FontWeights.Normal, selected ? paper.Ink : m.PrimaryText, TextAlignment.Left, 1);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		Grid.SetColumn(textBlock, 1);
		grid.Children.Add(textBlock);
		FrameworkElement frameworkElement2;
		switch (state)
		{
		case ArcCardState.Working:
		case ArcCardState.Pinning:
			frameworkElement2 = ChatStamp.Create(ChatStamp.Status.Working, "working", seed);
			break;
		case ArcCardState.Queued:
			frameworkElement2 = ChatStamp.Create(ChatStamp.Status.Working, "queued", seed);
			break;
		case ArcCardState.NeedsYou:
			frameworkElement2 = ChatStamp.Create(ChatStamp.Status.NeedsYou, "you", seed);
			break;
		case ArcCardState.Failed:
			frameworkElement2 = ChatStamp.Create(ChatStamp.Status.NeedsYou, "oops", seed);
			break;
		case ArcCardState.New:
			frameworkElement2 = ChatStamp.Create(ChatStamp.Status.Ready, "new", seed);
			break;
		default:
			frameworkElement2 = null;
			break;
		}
		FrameworkElement mark = frameworkElement2;
		if ((object)mark != null)
		{
			mark.VerticalAlignment = VerticalAlignment.Center;
			Grid.SetColumn(mark, 2);
			grid.Children.Add(mark);
		}
		Pressable close = null;
		if (moveToEarlier != null)
		{
			Pressable closeButton = new Pressable
			{
				Width = 18.0,
				Height = 18.0,
				CornerRadius = new CornerRadius(9.0),
				Visibility = Visibility.Collapsed,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock glyph = Icons.Glyph("\ue711", 9.0, m.TertiaryText);
			glyph.FontWeight = FontWeights.Bold;
			glyph.HorizontalAlignment = HorizontalAlignment.Center;
			glyph.VerticalAlignment = VerticalAlignment.Center;
			closeButton.Children.Add(glyph);
			closeButton.HoverChanged += delegate(bool h)
			{
				closeButton.Background = (h ? Ui.Brush(m.InkAt(0.12)) : Ui.Transparent);
				glyph.Foreground = Ui.Brush(h ? m.PrimaryText : m.TertiaryText);
			};
			closeButton.Click += moveToEarlier;
			AutomationProperties.SetName(closeButton, "Move to Earlier");
			Grid.SetColumn(closeButton, 2);
			grid.Children.Add(closeButton);
			close = closeButton;
		}
		row.Children.Add(grid);
		if (selected)
		{
			row.RenderTransformOrigin = new Point(0.5, 0.5);
			row.RenderTransform = new RotateTransform
			{
				Angle = -0.7
			};
		}
		double tilt = seed % 7 - 3;
		row.HoverChanged += delegate(bool h)
		{
			if (!selected)
			{
				if (h && (object)wash == null)
				{
					wash = new PaperSurface(new ClickySketchRect(7.0, seed + 2, 0.5), PaperFill.Of(paper.TitleWash.WithAlpha(0.14)), null).WithOutline(new PaperStroke(paper.TitleWash.WithAlpha(0.35)));
					row.Children.Insert(0, wash);
				}
				if ((object)wash != null)
				{
					wash.Visibility = ((!h) ? Visibility.Collapsed : Visibility.Visible);
				}
				chipTurn.Rotation = (h ? (tilt * -1.4) : 0.0);
				CompositeTransform compositeTransform = chipTurn;
				double scaleX = (chipTurn.ScaleY = (h ? 1.08 : 1.0));
				compositeTransform.ScaleX = scaleX;
			}
			if ((object)close != null)
			{
				close.Visibility = ((!h) ? Visibility.Collapsed : Visibility.Visible);
				if ((object)mark != null)
				{
					mark.Visibility = (h ? Visibility.Collapsed : Visibility.Visible);
				}
			}
		};
		row.Click += delegate
		{
			ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "window_sidebar_opened",
				["is_main"] = isMain,
				["is_job"] = jobId != null
			});
			if (showingSettings)
			{
				LeaveSettings();
			}
			Select(key);
		};
		AutomationProperties.SetName(row, name);
		return row;
	}

	private static FrameworkElement SelectedSlip(ArcPaper paper, int seed)
	{
		return new Grid
		{
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)new PaperSurface(new ClickySketchRect(7.0, seed + 2, 0.5), PaperFill.Of(paper.Fill), 0.5).WithOutline(new PaperStroke(paper.Deep.WithAlpha(0.4), 1.2, PenLineCap.Flat, PenLineJoin.Round)).WithShadow(DS.Black(0.45), 3.0, 0.0, 1.5),
				(UIElement)new Border
				{
					Width = 24.0,
					Height = 8.0,
					Background = Ui.Brush(paper.TitleWash.WithAlpha(0.9)),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(-6.0, -3.0, 0.0, 0.0),
					RenderTransformOrigin = new Point(0.5, 0.5),
					RenderTransform = new RotateTransform
					{
						Angle = -28.0
					}
				}
			}
		};
	}

	private void RebuildAccountRow()
	{
		if ((object)accountSlot == null || notchHost == null)
		{
			return;
		}
		using (MessagesPalette.Use(Look))
		{
			accountSlot.Child = AccountRow();
		}
	}

	private static BitmapImage AvatarImage(Uri uri)
	{
		if (!AvatarImages.TryGetValue(uri.AbsoluteUri, out BitmapImage value))
		{
			Dictionary<string, BitmapImage> avatarImages = AvatarImages;
			string absoluteUri = uri.AbsoluteUri;
			BitmapImage obj = new BitmapImage
			{
				DecodePixelType = DecodePixelType.Logical,
				DecodePixelWidth = 28,
				UriSource = uri
			};
			value = obj;
			avatarImages[absoluteUri] = obj;
		}
		return value;
	}

	private FrameworkElement AccountRow()
	{
		MessagesPalette m = Look;
		AppServices services = notchHost.Services;
		UserProfile user = services.Auth.User;
		string text = ((!string.IsNullOrWhiteSpace(user?.DisplayName)) ? user.DisplayName.Trim() : (user?.Email ?? "Your account"));
		char[] array = (from w in text.Split(new char[3] { ' ', '@', '.' }, StringSplitOptions.RemoveEmptyEntries).Take(2)
			select char.ToUpperInvariant(w[0])).ToArray();
		string text2 = ((array.Length == 0) ? "?" : new string(array));
		string text3 = (((object)user == null) ? "Signed out" : (services.Billing?.State.PaidPlanName?.Replace("HeyClicky ", "") ?? "Free"));
		Grid grid = new Grid
		{
			Margin = new Thickness(8.0, 0.0, 8.0, 10.0)
		};
		grid.Children.Add(new Border
		{
			Height = 1.0,
			Background = Ui.Brush(m.InkAt(0.07)),
			Margin = new Thickness(6.0, -6.0, 6.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top
		});
		Pressable row = new Pressable
		{
			CornerRadius = new CornerRadius(10.0),
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0)
		};
		Grid grid2 = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid grid3 = new Grid
		{
			ColumnSpacing = 9.0
		};
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Grid grid4 = new Grid
		{
			Width = 28.0,
			Height = 28.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		grid4.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(m.AvatarPlate)
		});
		grid4.Children.Add(Ui.Text(text2, 11.0, FontWeights.SemiBold, m.PrimaryText).Center());
		string text4 = user?.AvatarUrl;
		if (text4 != null && text4.Length > 0 && Uri.TryCreate(text4, UriKind.Absolute, out Uri result))
		{
			grid4.Children.Add(new Ellipse
			{
				Fill = new ImageBrush
				{
					ImageSource = AvatarImage(result),
					Stretch = Stretch.UniformToFill
				}
			});
		}
		grid3.Children.Add(grid4);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock textBlock = Ui.Text(text, 12.5, FontWeights.SemiBold, m.PrimaryText, TextAlignment.Left, 1);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		stackPanel.Children.Add(textBlock);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			Height = 13.0
		};
		TextBlock textBlock2 = Ui.Text(text3, 11.0, FontWeights.Normal, m.SecondaryText, TextAlignment.Left, 1);
		textBlock2.VerticalAlignment = VerticalAlignment.Center;
		stackPanel2.Children.Add(textBlock2);
		BillingStateService billingStateService = services.Billing?.State;
		if (billingStateService != null)
		{
			BillingUsageRing billingUsageRing = BillingUsageRing.From(billingStateService.Plan, billingStateService.UsageResetsPhrase);
			if ((object)billingUsageRing != null)
			{
				stackPanel2.Children.Add(new HeyClickyWindowUsageRing(billingUsageRing, palette)
				{
					Margin = new Thickness(-3.0)
				});
			}
		}
		stackPanel.Children.Add(stackPanel2);
		Grid.SetColumn(stackPanel, 1);
		grid3.Children.Add(stackPanel);
		grid2.Children.Add(grid3);
		Pressable gear = new Pressable
		{
			Width = 30.0,
			Height = 30.0,
			CornerRadius = new CornerRadius(15.0),
			Background = Ui.Brush(m.InkAt(showingSettings ? 0.12 : 0.05))
		};
		TextBlock gearGlyph = Icons.Glyph("\ue713", 13.0, m.InkAt(showingSettings ? 0.95 : 0.6));
		gearGlyph.HorizontalAlignment = HorizontalAlignment.Center;
		gearGlyph.VerticalAlignment = VerticalAlignment.Center;
		gear.Children.Add(gearGlyph);
		gear.HoverChanged += delegate(bool h)
		{
			gear.Background = Ui.Brush(m.InkAt((h || showingSettings) ? 0.12 : 0.05));
			gearGlyph.Foreground = Ui.Brush(m.InkAt((h || showingSettings) ? 0.95 : 0.6));
		};
		gear.Click += delegate
		{
			ShowSettings(settingsPage);
		};
		gear.Tip("Settings");
		AutomationProperties.SetName(gear, "Settings");
		Grid.SetColumn(gear, 1);
		grid2.Children.Add(gear);
		row.Children.Add(grid2);
		row.Background = (showingSettings ? Ui.Brush(m.InkAt(0.09)) : Ui.Transparent);
		row.HoverChanged += delegate(bool h)
		{
			row.Background = Ui.Brush(m.InkAt(showingSettings ? 0.09 : (h ? 0.05 : 0.0)));
		};
		row.Click += delegate
		{
			ShowSettings(SettingsWindow.Page.Account);
		};
		string text5 = user?.Email;
		if (text5 != null)
		{
			row.Tip(text5);
		}
		AutomationProperties.SetName(row, "Account and Settings");
		grid.Children.Add(row);
		return grid;
	}

	private void OnHandOffsChanged()
	{
		RefreshSoon();
	}

	private void OnAgentsChanged(object? sender, EventArgs e)
	{
		RefreshSoon();
	}

	private void OnDockContentChanged()
	{
		RefreshSoon();
	}

	private void RefreshSoon()
	{
		Microsoft.UI.Xaml.Window window = this.window;
		if ((object)window == null || refreshQueued)
		{
			return;
		}
		refreshQueued = true;
		window.DispatcherQueue.TryEnqueue(delegate
		{
			refreshQueued = false;
			if ((object)this.window != null)
			{
				RebuildSidebar();
				if (!showingSettings && !(selectedKey == "heyclicky"))
				{
					if (shownChild is HeyClickyWindowConversation heyClickyWindowConversation)
					{
						heyClickyWindowConversation.Refresh();
						SyncComposerStop(heyClickyWindowConversation);
					}
					ShowColumnHeader();
				}
			}
		});
	}

	private void OnPresentationChanged(object? sender, EventArgs e)
	{
		window?.DispatcherQueue.TryEnqueue(delegate
		{
			bool flag = (notchHost?.Services.Presenter as NotchPresenter)?.IsTextResponseInProgress ?? false;
			if (flag != wasAnswering)
			{
				wasAnswering = flag;
				RebuildSidebar();
				if (!showingSettings && selectedKey == "heyclicky")
				{
					ShowColumnHeader();
				}
			}
		});
	}

	private bool KeyStillExists(string? key)
	{
		if (key == null || key.Length == 0)
		{
			return false;
		}
		if (key == "heyclicky")
		{
			return true;
		}
		if (!(notchHost?.Services.Agents is AgentService agentService))
		{
			return true;
		}
		string text = ClickyJob.JobIdFromRailKey(key);
		if (text != null)
		{
			ClickyJob clickyJob = agentService.Jobs.Job(text);
			if ((object)clickyJob != null && ((!clickyJob.IsArchived) ?? true))
			{
				return true;
			}
			return false;
		}
		return (object)agentService.FindClicky(key) != null;
	}

	private void Select(string key)
	{
		if ((object)mainColumn == null || host == null)
		{
			return;
		}
		using (MessagesPalette.Use(Look))
		{
			bool flag = key != selectedKey || (object)shownChild == null;
			if (key != selectedKey && selectedKey.Length > 0)
			{
				CloseDocument();
			}
			selectedKey = key;
			NotchPresenter notchPresenter = notchHost?.Services.Presenter as NotchPresenter;
			if (key == "heyclicky")
			{
				shownAgentKey = null;
				if ((object)mainChat == null)
				{
					mainChat = new NotchChatView(host, isInWindow: true);
				}
				if (flag)
				{
					Swap(mainChat);
				}
				notchPresenter?.SetWindowShowsMainThread(shows: true);
			}
			else
			{
				shownAgentKey = key;
				notchPresenter?.SetWindowShowsMainThread(shows: false);
				if (flag)
				{
					HeyClickyWindowConversation heyClickyWindowConversation = new HeyClickyWindowConversation(host, key);
					Swap(heyClickyWindowConversation);
					SyncComposerStop(heyClickyWindowConversation);
					MarkRead(key);
				}
			}
			if ((object)chatDots != null)
			{
				chatDots.Visibility = Visibility.Visible;
			}
			LayOutCards();
			ShowColumnHeader();
			RebuildSidebar();
		}
	}

	private void MarkRead(string key)
	{
		if (notchHost?.Services.Agents is AgentService agentService)
		{
			string text = ClickyJob.JobIdFromRailKey(key);
			if (text != null)
			{
				agentService.Jobs.MarkSeen(text);
			}
			else if ((object)agentService.FindClicky(key) != null)
			{
				ClickyReadMarks.MarkRead(key);
			}
		}
	}

	private string AgentName(string key)
	{
		if (!(notchHost?.Services.Agents is AgentService agentService))
		{
			return key;
		}
		string text = ClickyJob.JobIdFromRailKey(key);
		if (text == null)
		{
			return agentService.FindClicky(key)?.Name ?? key;
		}
		return agentService.Jobs.Job(text)?.DisplayName ?? "Job";
	}

	private void ShowColumnHeader()
	{
		if ((object)columnHeader == null)
		{
			return;
		}
		using (MessagesPalette.Use(Look))
		{
			MessagesPalette look = Look;
			bool flag = (notchHost?.Services.Presenter as NotchPresenter)?.IsTextResponseInProgress ?? false;
			string text = ((showingSettings || selectedKey == "heyclicky") ? "" : AgentName(selectedKey));
			ArcCardState arcCardState = dock?.ContentForKey(selectedKey)?.State ?? ArcCardState.Idle;
			object obj;
			if (!showingSettings)
			{
				if (!(selectedKey == "heyclicky"))
				{
					global::_003C_003Ey__InlineArray4<object> buffer = default(global::_003C_003Ey__InlineArray4<object>);
					global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 0) = "agent";
					global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 1) = selectedKey;
					global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 2) = text;
					global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 3) = arcCardState;
					obj = string.Join("\u001f", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray4<object>, object>(in buffer, 4));
				}
				else
				{
					obj = "main\u001f" + flag;
				}
			}
			else
			{
				obj = "settings";
			}
			string text2 = (string)obj;
			if (text2 == shownHeaderShape && (object)columnHeader.Child != null)
			{
				return;
			}
			shownHeaderShape = text2;
			FrameworkElement frameworkElement;
			if (showingSettings)
			{
				frameworkElement = SettingsHeader();
			}
			else if (selectedKey == "heyclicky")
			{
				frameworkElement = ColumnHeader(new HandoffChipView(HandoffGlyph.HeyClicky, "heyclicky", 22.0, PaperStock.Sky), "HeyClicky", flag ? "Answering…" : "Your chat", look.SecondaryText);
			}
			else
			{
				(uint Rgb, double Opacity) tuple = ArcCardStates.Color(arcCardState);
				uint item = tuple.Rgb;
				double item2 = tuple.Opacity;
				bool flag2 = ArcCardStates.IsBusy(arcCardState);
				if (!flag2)
				{
					bool flag3 = (uint)(arcCardState - 2) <= 1u;
					flag2 = flag3;
				}
				bool flag4 = flag2;
				int num = NotePaperColors.Seed(selectedKey);
				frameworkElement = ColumnHeader(new NoteChipView(NotePaperColors.Key(selectedKey), ClickyInkIcon.Inferred(text) ?? "sparkles", num, num, 22.0), text, ArcCardStates.Title(arcCardState), flag4 ? DS.Hex(item, item2) : look.SecondaryText);
			}
			frameworkElement.Margin = ColumnHeaderInsets();
			Grid grid = new Grid
			{
				Height = 38.0
			};
			grid.Children.Add(frameworkElement);
			grid.Children.Add(new Border
			{
				Height = 1.0,
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = Ui.Brush(look.InkAt(0.07)),
				IsHitTestVisible = false
			});
			columnHeader.Child = grid;
			headerChild = frameworkElement;
		}
	}

	private void SyncComposerStop(HeyClickyWindowConversation conversation)
	{
		string agentKey = conversation.AgentKey;
		ArcCardContent arcCardContent = dock?.ContentForKey(agentKey);
		bool flag = (object)arcCardContent != null && !arcCardContent.IsFinalizing && arcCardContent.TaskIsRunning;
		conversation.SetStopAction(flag ? ((Action)delegate
		{
			ArcCardContent arcCardContent2 = dock?.ContentForKey(agentKey);
			if ((object)arcCardContent2 != null && arcCardContent2.TaskIsRunning)
			{
				ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "window_agent_stopped" });
				dock.StopWork(arcCardContent2);
			}
		}) : null);
	}

	private static FrameworkElement ColumnHeader(FrameworkElement chip, string title, string subtitle, Color subtitleColor)
	{
		MessagesPalette current = MessagesPalette.Current;
		chip.VerticalAlignment = VerticalAlignment.Center;
		TextBlock textBlock = Ui.Text(title, 13.0, FontWeights.SemiBold, current.PrimaryText, TextAlignment.Left, 1);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		TextBlock textBlock2 = Ui.Text(subtitle, 11.0, FontWeights.Medium, subtitleColor, TextAlignment.Left, 1);
		textBlock2.TextTrimming = TextTrimming.CharacterEllipsis;
		Grid obj = new Grid
		{
			ColumnSpacing = 10.0,
			IsHitTestVisible = false,
			ColumnDefinitions = 
			{
				new ColumnDefinition
				{
					Width = GridLength.Auto
				},
				new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				}
			},
			Children = { (UIElement)chip }
		};
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)textBlock,
				(UIElement)textBlock2
			}
		};
		Grid.SetColumn(stackPanel, 1);
		obj.Children.Add(stackPanel);
		return obj;
	}

	private static FrameworkElement SettingsHeader()
	{
		TextBlock textBlock = Ui.Text("Settings", 14.0, FontWeights.SemiBold, MessagesPalette.Current.PrimaryText, TextAlignment.Left, 1);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.Margin = new Thickness(261.0, 0.0, 0.0, 0.0);
		return new Grid
		{
			IsHitTestVisible = false,
			Children = { (UIElement)textBlock }
		};
	}

	private void Swap(FrameworkElement next)
	{
		if ((object)stage == null || (object)shownChild == next)
		{
			return;
		}
		FrameworkElement old = shownChild;
		if (old is SettingsContent settingsContent)
		{
			settingsContent.Hidden();
		}
		bool num = FocusIsWithin(old);
		shownChild = next;
		if (!stage.Children.Contains(next))
		{
			next.Opacity = (Anim.ReduceMotion ? 1 : 0);
			stage.Children.Add(next);
		}
		next.IsHitTestVisible = true;
		if (num)
		{
			ParkFocus(force: true);
		}
		if ((object)old != null)
		{
			old.IsHitTestVisible = false;
			if (Anim.ReduceMotion)
			{
				stage.Children.Remove(old);
			}
			else
			{
				Anim.Opacity(old, 0f, 0.16, Easing.EaseOut(Ui.Compositor));
				Grid leaving = stage;
				Microsoft.UI.Dispatching.DispatcherQueueTimer t = leaving.DispatcherQueue.CreateTimer().Rooted();
				t.Interval = TimeSpan.FromSeconds(0.18);
				t.IsRepeating = false;
				t.Tick += delegate
				{
					t.Stop();
					if ((object)old != shownChild)
					{
						leaving.Children.Remove(old);
					}
				};
				t.Start();
			}
		}
		if (!Anim.ReduceMotion)
		{
			Anim.Opacity(next, 1f, 0.16, Easing.EaseOut(Ui.Compositor));
		}
	}

	private void ShowSettings()
	{
		ShowSettings(SettingsWindow.Page.Account);
	}

	private void ShowSettings(SettingsWindow.Page page)
	{
		if (notchHost == null || (object)mainColumn == null)
		{
			return;
		}
		CloseDocument();
		settingsPage = page;
		using (MessagesPalette.Use(Look))
		{
			if (!showingSettings)
			{
				keyBeforeSettings = selectedKey;
			}
			showingSettings = true;
			shownAgentKey = null;
			(notchHost.Services.Presenter as NotchPresenter)?.SetWindowShowsMainThread(shows: false);
			if ((object)settings == null)
			{
				settings = new SettingsContent(notchHost.Services, notchHost.Actions)
				{
					Margin = new Thickness(0.0, 6.0, 12.0, 0.0)
				};
				settings.SidebarWidth = 224.0;
				settings.UseNotchGrid(12.0, 12.0);
				settings.PageChanged += delegate(SettingsWindow.Page p)
				{
					settingsPage = p;
				};
			}
			settings.IsSurfaceSettled = true;
			Swap(settings);
			settings.Show(page);
			if ((object)chatDots != null)
			{
				chatDots.Visibility = Visibility.Collapsed;
			}
			LayOutCards();
			ShowColumnHeader();
			RebuildAccountRow();
			ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "window_settings_opened" });
		}
	}

	private void LeaveSettings()
	{
		if (showingSettings)
		{
			showingSettings = false;
			settings?.Hidden();
			string key = (KeyStillExists(keyBeforeSettings) ? keyBeforeSettings : "heyclicky");
			selectedKey = "";
			Select(key);
			RebuildAccountRow();
		}
	}

	private Grid BuildDocumentColumn()
	{
		using (MessagesPalette.Use(Look))
		{
			MessagesPalette look = Look;
			Grid obj = new Grid
			{
				Margin = new Thickness(0.0, 36.0, 8.0, 8.0)
			};
			Grid grid = new Grid
			{
				RowDefinitions = 
				{
					new RowDefinition
					{
						Height = new GridLength(32.0)
					},
					new RowDefinition
					{
						Height = new GridLength(1.0, GridUnitType.Star)
					}
				}
			};
			documentHeader = new Border();
			grid.Children.Add(documentHeader);
			grid.Children.Add(new Border
			{
				Height = 1.0,
				VerticalAlignment = VerticalAlignment.Bottom,
				Background = Ui.Brush(look.InkAt(0.07)),
				IsHitTestVisible = false
			});
			documentViewer = new NotchDocumentViewer(host, showsFollowUp: false);
			Grid.SetRow(documentViewer, 1);
			grid.Children.Add(documentViewer);
			Border border = new Border
			{
				Background = Ui.Brush(look.SidebarFill),
				CornerRadius = new CornerRadius(12.0),
				BorderBrush = Ui.Brush(look.InkAt(0.08)),
				BorderThickness = new Thickness(1.0),
				Child = grid,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			Ui.ClipRounded(border, 12.0);
			obj.Children.Add(border);
			documentPlate = border;
			DocumentResizeHandle item = new DocumentResizeHandle(this, look)
			{
				Width = 12.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(-10.0, 0.0, 0.0, 0.0)
			};
			obj.Children.Add(item);
			return obj;
		}
	}

	private FrameworkElement DocumentHeader(string path)
	{
		using (MessagesPalette.Use(Look))
		{
			MessagesPalette look = Look;
			NotchDocumentStyle notchDocumentStyle = NotchDocumentStyle.For(path);
			Grid grid = new Grid
			{
				ColumnSpacing = 8.0,
				Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			TextBlock textBlock = Icons.Glyph(notchDocumentStyle.Glyph, 9.5, Colors.White);
			textBlock.FontWeight = FontWeights.SemiBold;
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(new Border
			{
				Width = 18.0,
				Height = 18.0,
				CornerRadius = new CornerRadius(5.0),
				Background = Ui.Brush(notchDocumentStyle.Tint),
				Child = textBlock,
				VerticalAlignment = VerticalAlignment.Center
			});
			TextBlock textBlock2 = Ui.Text(System.IO.Path.GetFileName(path), 12.5, FontWeights.SemiBold, look.PrimaryText, TextAlignment.Left, 1);
			textBlock2.TextTrimming = TextTrimming.CharacterEllipsis;
			textBlock2.VerticalAlignment = VerticalAlignment.Center;
			ToolTipService.SetToolTip(textBlock2, path);
			TextBlock textBlock3 = Ui.Text(notchDocumentStyle.KindLabel, 11.5, FontWeights.Normal, look.SecondaryText, TextAlignment.Left, 1);
			textBlock3.VerticalAlignment = VerticalAlignment.Center;
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0,
				Children = 
				{
					(UIElement)textBlock2,
					(UIElement)textBlock3
				}
			};
			Grid.SetColumn(stackPanel, 1);
			grid.Children.Add(stackPanel);
			headerButtons.RemoveAll((FrameworkElement b) => b.Tag as string == "document");
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			(string, string, Action)[] array = new(string, string, Action)[3]
			{
				("\ue8a7", "Open in its app", delegate
				{
					string text = documentPath;
					if (text != null)
					{
						(notchHost?.Actions as INotchActions)?.OpenInItsApp(text, documentThreadId);
					}
				}),
				("\ue8b7", "Show in Explorer", delegate
				{
					string text2 = documentPath;
					if (text2 != null)
					{
						(notchHost?.Actions as INotchActions)?.RevealInExplorer(text2);
					}
				}),
				("\ue711", "Close (esc)", CloseDocument)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(string, string, Action) tuple = array[i];
				string item = tuple.Item1;
				string item2 = tuple.Item2;
				Action item3 = tuple.Item3;
				Pressable pressable = HeaderButton(item, item2, item3);
				pressable.Tag = "document";
				stackPanel2.Children.Add(pressable);
			}
			Grid.SetColumn(stackPanel2, 2);
			grid.Children.Add(stackPanel2);
			return grid;
		}
	}

	public void ShowDocument(string path, string? threadId)
	{
		if ((object)documentColumn == null || (object)documentDefinition == null || (object)documentViewer == null)
		{
			return;
		}
		using (MessagesPalette.Use(Look))
		{
			bool num = documentPath != null;
			documentPath = path;
			documentThreadId = threadId;
			if ((object)documentHeader != null)
			{
				documentHeader.Child = DocumentHeader(path);
			}
			documentViewer.Show(null);
			documentViewer.Show(path, threadId);
			documentViewerHolds = true;
			LayOutCards();
			if (!num)
			{
				WidenForDocument();
			}
			ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "window_document_opened" });
		}
	}

	public void CloseDocument()
	{
		if (documentPath != null && (object)documentColumn != null && (object)documentDefinition != null)
		{
			documentPath = null;
			documentThreadId = null;
			LayOutCards();
		}
	}

	private double ClampDocumentWidth(double width)
	{
		double num;
		if (!windowMoving || !(windowMoveToContentWidth > 0.0))
		{
			Grid grid = root;
			num = (((object)grid != null && grid.ActualWidth > 0.0) ? grid.ActualWidth : 1100.0);
		}
		else
		{
			num = windowMoveToContentWidth;
		}
		double num2 = num;
		double num3 = (SidebarVisible ? 244.0 : 0.0);
		double val = Math.Max(340.0, num2 - num3 - 380.0 - 24.0);
		return Math.Min(Math.Max(width, 340.0), val);
	}

	private void ResizeDocument(double width)
	{
		if ((object)documentDefinition != null)
		{
			saved = saved with
			{
				DocumentWidth = ClampDocumentWidth(width)
			};
			if (!columnFramesRunning)
			{
				ApplyColumnFrame();
			}
		}
	}

	private void WidenForDocument()
	{
		if ((object)appWindow == null)
		{
			return;
		}
		Grid grid = root;
		if ((object)grid == null || !(grid.ActualWidth > 0.0))
		{
			return;
		}
		XamlRoot xamlRoot = grid.XamlRoot;
		if ((object)xamlRoot == null || HeyClickyWindowOpeningFlight.IsFlying || !(appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored }))
		{
			return;
		}
		double rasterizationScale = xamlRoot.RasterizationScale;
		double num = (SidebarVisible ? 244.0 : 0.0) + 380.0 + Math.Max(340.0, saved.DocumentWidth) + 24.0;
		RectInt32 rectInt = new RectInt32(appWindow.Position.X, appWindow.Position.Y, appWindow.Size.Width, appWindow.Size.Height);
		int num2 = Math.Max(0, rectInt.Width - (int)Math.Round(root.ActualWidth * rasterizationScale));
		int num3 = (int)Math.Ceiling(num * rasterizationScale) + num2;
		if (rectInt.Width < num3)
		{
			Native.RECT work = Displays.ForWindow(hwnd).Work;
			RectInt32 rectInt2 = rectInt;
			rectInt2.Width = Math.Min(num3, work.Width);
			RectInt32 to = rectInt2;
			if (!OverlayWindow.Offscreen && to.X + to.Width > work.Right)
			{
				to.X = Math.Max(work.Left, work.Right - to.Width);
			}
			windowMoveToContentWidth = (double)(to.Width - num2) / rasterizationScale;
			MoveWindow(to, 0.3);
		}
	}

	private void MoveWindow(RectInt32 to, double seconds)
	{
		if ((object)appWindow == null)
		{
			return;
		}
		if (Anim.ReduceMotion || seconds <= 0.0)
		{
			appWindow.MoveAndResize(to);
			return;
		}
		windowMoveFrom = new RectInt32(appWindow.Position.X, appWindow.Position.Y, appWindow.Size.Width, appWindow.Size.Height);
		windowMoveTo = to;
		windowMoveStartedAt = SpringCurve.Now;
		windowMoveSeconds = seconds;
		if (!windowMoving)
		{
			windowMoving = true;
			CompositionTarget.Rendering += OnWindowMoveFrame;
		}
	}

	private void OnWindowMoveFrame(object? sender, object e)
	{
		double num = (((object)appWindow == null) ? 1.0 : Math.Clamp((SpringCurve.Now - windowMoveStartedAt) / windowMoveSeconds, 0.0, 1.0));
		double p;
		if ((object)appWindow != null)
		{
			p = TimingCurve(0.2, 0.9, 0.25, 1.0, num);
			appWindow.MoveAndResize(new RectInt32(Lerp(windowMoveFrom.X, windowMoveTo.X), Lerp(windowMoveFrom.Y, windowMoveTo.Y), Lerp(windowMoveFrom.Width, windowMoveTo.Width), Lerp(windowMoveFrom.Height, windowMoveTo.Height)));
		}
		if (!(num < 1.0))
		{
			CompositionTarget.Rendering -= OnWindowMoveFrame;
			windowMoving = false;
			windowMoveToContentWidth = 0.0;
			if (!columnFramesRunning)
			{
				ApplyColumnFrame();
			}
		}
		int Lerp(int a, int b)
		{
			return (int)Math.Round((double)a + (double)(b - a) * p);
		}
	}

	internal static double TimingCurve(double x1, double y1, double x2, double y2, double t)
	{
		if (t <= 0.0)
		{
			return 0.0;
		}
		if (t >= 1.0)
		{
			return 1.0;
		}
		double num = 0.0;
		double num2 = 1.0;
		double num3 = t;
		for (int i = 0; i < 40; i++)
		{
			num3 = (num + num2) / 2.0;
			if (Bezier(x1, x2, num3) < t)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		return Bezier(y1, y2, num3);
		static double Bezier(double a, double b, double s)
		{
			return 3.0 * a * s * (1.0 - s) * (1.0 - s) + 3.0 * b * s * s * (1.0 - s) + s * s * s;
		}
	}

	public static string? AgentKeyForEntry(ChatHandOff entry)
	{
		string jobId = entry.JobId;
		if (jobId == null)
		{
			return entry.ClickySlug;
		}
		return "job:" + jobId;
	}

	internal void OpenAgentForEntry(string entryId)
	{
		ChatHandOff chatHandOff = NotchChat.HandOffs?.Entry(entryId);
		if ((object)chatHandOff == null)
		{
			return;
		}
		string text = AgentKeyForEntry(chatHandOff);
		if (text != null)
		{
			if (showingSettings)
			{
				LeaveSettings();
			}
			Select(text);
		}
	}

	private void PlaceFromSavedFrame()
	{
		if ((object)appWindow == null)
		{
			return;
		}
		Displays.Monitor monitor = Displays.ForWindow(notchHost.WindowHandle);
		Native.RECT work = monitor.Work;
		Displays.Monitor monitor2 = Displays.At(saved.X + 40, saved.Y + 20);
		Native.RECT work2 = monitor2.Work;
		if (!OverlayWindow.Offscreen && saved.Width > 0 && saved.Height > 0 && work2.Left <= saved.X + 40 && saved.X + 40 < work2.Left + work2.Width && work2.Top <= saved.Y + 20 && saved.Y + 20 < work2.Top + work2.Height)
		{
			if (monitor2.Handle == monitor.Handle)
			{
				appWindow.MoveAndResize(new RectInt32(saved.X, saved.Y, saved.Width, saved.Height));
				return;
			}
			double num = monitor.Scale / Math.Max(0.5, monitor2.Scale);
			int num2 = (int)Math.Min((double)saved.Width * num, work.Width);
			int num3 = (int)Math.Min((double)saved.Height * num, work.Height);
			double num4 = Math.Clamp((double)(saved.X - work2.Left) / (double)Math.Max(1, work2.Width - saved.Width), 0.0, 1.0);
			double num5 = Math.Clamp((double)(saved.Y - work2.Top) / (double)Math.Max(1, work2.Height - saved.Height), 0.0, 1.0);
			appWindow.MoveAndResize(new RectInt32(work.Left + (int)(num4 * (double)(work.Width - num2)), work.Top + (int)(num5 * (double)(work.Height - num3)), num2, num3));
		}
		else
		{
			int num6 = (int)Math.Min((double)work.Width * 0.9, 1100.0 * monitor.Scale);
			int height = (int)Math.Min((double)work.Height * 0.9, 760.0 * monitor.Scale);
			int num7 = (OverlayWindow.Offscreen ? (-24000) : 0);
			appWindow.MoveAndResize(new RectInt32(work.Left + (work.Width - num6) / 2 + num7, work.Top + (int)(40.0 * monitor.Scale) + num7, num6, height));
		}
	}

	private void SaveFrameSoon()
	{
		if ((object)window != null)
		{
			if (saveTimer == null)
			{
				saveTimer = new DispatcherQueueTimerHolder(window.DispatcherQueue, SaveFrame);
			}
			saveTimer.Restart(TimeSpan.FromSeconds(0.6));
		}
	}

	private void SaveFrame()
	{
		if ((object)appWindow != null && !OverlayWindow.Offscreen && appWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Restored })
		{
			saved = saved with
			{
				X = appWindow.Position.X,
				Y = appWindow.Position.Y,
				Width = appWindow.Size.Width,
				Height = appWindow.Size.Height
			};
		}
		saved.Save();
	}
}

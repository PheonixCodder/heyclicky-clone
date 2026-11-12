using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HeyClicky.Platform.Win32;
using Vortice.DirectComposition;
using Vortice.Mathematics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Overlay;

internal sealed class CursorLayer : IOverlayLayer, IDisposable
{
	private enum NavigationMode
	{
		FollowingCursor,
		NavigatingToTarget,
		PointingAtTarget,
		RidingPen,
		ParkedAtPen
	}

	private const double FollowOffsetX = 35.0;

	private const double FollowOffsetY = 25.0;

	private const double TargetOffsetX = 8.0;

	private const double TargetOffsetY = 12.0;

	private const double TargetEdgeMargin = 20.0;

	private const double RestingRotationDegrees = -35.0;

	private const double ReturnCancelDistancePoints = 100.0;

	private static readonly string[] PointingPhrases = new string[6] { "right here!", "this one!", "over here!", "click this!", "here it is!", "found it!" };

	private const double PenParkReleaseDistancePoints = 40.0;

	private readonly OverlayHost host;

	private readonly IDCompositionVisual2 clusterVisual;

	private readonly IDCompositionVisual3? clusterVisual3;

	private readonly CursorClusterRenderer renderer;

	private CursorClusterPose pose = CursorClusterPose.CreateDefault();

	private bool hasRendered;

	private bool showRequested;

	private bool hasShownBefore;

	private double opacity;

	private double opacityFrom;

	private double opacityTo;

	private double opacityStart;

	private double opacityDuration;

	private Func<double, double> opacityCurve = Easing.EaseInOut;

	private bool systemCursorVisible = true;

	private double lastVisibilityProbeTime;

	private double lastFullscreenProbeTime;

	private double lastMouseMoveTime;

	private bool foregroundIsFullscreen;

	private PointD pointerWhenHidden;

	private bool hiddenReportIsStale;

	private nint lastForegroundWindow;

	private bool layerVisibleRegistered;

	private SpringState2D spring;

	private PointD lastMouse;

	private DisplayInfo? currentDisplay;

	private int framesWithoutMovement;

	private NavigationMode mode;

	private OverlayCursorFlightPath flight;

	private double flightStartTime;

	private double flightScaleForPoints;

	private bool isReturningToCursor;

	private bool flightAwaitsLabel;

	private PointD returnStartMouse;

	private double rotationBackStartTime = -1.0;

	private double rotationBackFrom;

	private string pointingText = "";

	private int revealedCharacters;

	private double nextCharacterTime;

	private double holdUntilTime = -1.0;

	private double bubbleFadeStartTime = -1.0;

	private double returnAtTime = -1.0;

	private bool holdsForStreamedBubble;

	private double nextStreamedBubbleCheckTime;

	private SpringState1D bubbleScale;

	private TaskCompletionSource? pointCompletion;

	private readonly Random random = new Random();

	private readonly PenRideTrajectory penRide = new PenRideTrajectory();

	private DisplayInfo? penDisplay;

	private PointD penParkMouseBaseline;

	private readonly CursorMessageBubble message;

	private TaskCompletionSource? welcomeCompletion;

	private const double ShakeSampleWindowSeconds = 0.5;

	private const double ShakeCooldownSeconds = 1.0;

	private const double ShakeNoiseFloorPoints = 2.0;

	private const int ShakeReversalThreshold = 6;

	private const double ShakePathLengthThresholdPoints = 260.0;

	private const double DetachedHitRegionPoints = 44.0;

	private const double DetachReattachSeconds = 3.0;

	private const double DetachClickReattachSeconds = 5.0;

	private volatile bool shakeToDetachEnabled;

	private volatile bool detached;

	private volatile bool docked;

	private volatile bool sayingGoodbye;

	public Action? DetachedClicked;

	public Action? DetachBegan;

	public Action? DetachAutoReattached;

	private readonly List<(PointD Point, double Time)> shakeSamples = new List<(PointD, double)>();

	private double lastShakeSampleTime;

	private double lastShakeDetectedAt = -10.0;

	private double detachedAt;

	private double reattachAt = double.MaxValue;

	private bool detachHovered;

	private bool detachedVoiceSession;

	private bool stopPillShown;

	private double stopPillShownAt = -1.0;

	private SpringState1D detachedScale;

	private DetachedClickCatcher? catcher;

	public volatile VoiceState RequestedVoiceState;

	public volatile float LatestLevel;

	private VoiceState voiceState;

	private double displayedLevel;

	private double waveformOpacity;

	private double triangleVoiceOpacity = 1.0;

	private double thinkingStartTime = -1.0;

	private double mergeStartTime = -1.0;

	private double dotsFadeStartTime = -1.0;

	internal static bool IgnoreSystemVisibilityForTests;

	private double floatOffsetPoints;

	private const double GoodbyeHoldSeconds = 1.1;

	private const double GoodbyeFadeSeconds = 0.6;

	private double dwellStartTime = -1.0;

	private double dwellDurationSeconds = 0.45;

	public bool ShakeToDetachEnabled
	{
		get
		{
			return shakeToDetachEnabled;
		}
		set
		{
			shakeToDetachEnabled = value;
		}
	}

	public bool IsDetached => detached;

	public bool IsDocked => docked;

	public bool IsSayingGoodbye => sayingGoodbye;

	public bool DetachHovered => detachHovered;

	public long RenderCount => renderer.RenderCount;

	public PointD PositionPx => new PointD(spring.X, spring.Y);

	public string ModeName => mode.ToString();

	public string MessageText => message.Text;

	public double MessageOpacity => message.Opacity;

	public double CurrentOpacity => opacity * GoodbyeOpacityFactor();

	public int IdleWakeIntervalMs
	{
		get
		{
			if ((!showRequested || mode != 0 || voiceState != 0) && !message.IsShowing)
			{
				return -1;
			}
			return 100;
		}
	}

	public bool WantsMouseMoveWake
	{
		get
		{
			if (!showRequested)
			{
				return opacity > 0.0;
			}
			return true;
		}
	}

	public void SnapshotPng(string path, Color4 background)
	{
		renderer.SnapshotPng(path, background);
	}

	public CursorLayer(OverlayHost host)
	{
		this.host = host;
		clusterVisual = host.CompositionDevice.CreateVisual();
		clusterVisual3 = clusterVisual.QueryInterfaceOrNull<IDCompositionVisual3>();
		clusterVisual.SetBitmapInterpolationMode(BitmapInterpolationMode.NearestNeighbor);
		host.RootVisual.AddVisual(clusterVisual, true, null);
		clusterVisual3?.SetOpacity(0f);
		renderer = new CursorClusterRenderer(host, clusterVisual);
		renderer.SetTint(CursorOverlay.ParseHexColor("#3380FF"));
		message = new CursorMessageBubble(host);
		message.SetTint(CursorOverlay.ParseHexColor("#3380FF"));
		PointD pointD = (lastMouse = DisplayEnumeratorFast.CursorPositionPx());
		currentDisplay = DisplayEnumerator.DisplayContaining(pointD, (host.Displays.Count > 0) ? host.Displays : null);
		renderer.EnsureScale(currentDisplay.Scale);
		PointD pointD2 = FollowTarget(pointD);
		spring.X = pointD2.X;
		spring.Y = pointD2.Y;
	}

	public void Show()
	{
		if (!showRequested)
		{
			showRequested = true;
			RegisterVisible(visible: true);
			host.BringToTop();
			double now = Now();
			StartOpacityAnimation(1.0, hasShownBefore ? 0.22 : 2.0, hasShownBefore ? new Func<double, double>(Easing.EaseInOut) : new Func<double, double>(Easing.EaseIn), now);
			hasShownBefore = true;
			PointD mouse = DisplayEnumeratorFast.CursorPositionPx();
			if (mode == NavigationMode.FollowingCursor)
			{
				SnapToFollowTarget(mouse);
			}
			host.RequestFrames();
		}
	}

	public void Hide()
	{
		if (showRequested)
		{
			showRequested = false;
			StartOpacityAnimation(0.0, 0.4, Easing.EaseIn, Now());
			host.RequestFrames();
		}
	}

	public void SetTint(Color4 color)
	{
		renderer.SetTint(color);
		message.SetTint(color);
		hasRendered = false;
		host.RequestFrames();
	}

	public void SnapshotMessagePng(string path, Color4 background)
	{
		message.SnapshotPng(path, background);
	}

	public void ShowMessage(string text, bool streamed, double holdSeconds, TaskCompletionSource completion)
	{
		CancelWelcome();
		double now = Now();
		if (docked && mode == NavigationMode.FollowingCursor && !detached)
		{
			SnapToFollowTarget(DisplayEnumeratorFast.CursorPositionPx());
		}
		message.Show(text, streamed, holdSeconds, completion, now);
		if (!showRequested)
		{
			Show();
		}
		host.BringToTop();
		host.RequestFrames();
	}

	public void HideMessage()
	{
		message.Hide(Now());
		host.RequestFrames();
	}

	public void HideMessageIfOwnedBy(TaskCompletionSource completion)
	{
		if (!completion.Task.IsCompleted)
		{
			message.Hide(Now(), animated: false);
			completion.TrySetCanceled();
			host.RequestFrames();
		}
	}

	public void PlayWelcome(TaskCompletionSource completion)
	{
		CancelWelcome();
		welcomeCompletion = completion;
		if (!showRequested)
		{
			Show();
		}
		host.BringToTop();
		message.ShowWelcome(completion, Now());
		host.RequestFrames();
	}

	private void CancelWelcome()
	{
		if (welcomeCompletion != null)
		{
			welcomeCompletion.TrySetResult();
			welcomeCompletion = null;
		}
	}

	private bool DetectShake(PointD mouse, double now)
	{
		if (now - lastShakeSampleTime < 0.016)
		{
			return false;
		}
		lastShakeSampleTime = now;
		double num = Math.Max(0.5, currentDisplay?.Scale ?? 1.0);
		shakeSamples.Add((new PointD(mouse.X / num, mouse.Y / num), now));
		int num2 = shakeSamples.FindIndex(((PointD Point, double Time) s) => s.Time >= now - 0.5);
		if (num2 < 0)
		{
			shakeSamples.Clear();
		}
		else if (num2 > 0)
		{
			shakeSamples.RemoveRange(0, num2);
		}
		if (shakeSamples.Count < 4 || now - lastShakeDetectedAt < 1.0)
		{
			return false;
		}
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		double num6 = 0.0;
		for (int i = 1; i < shakeSamples.Count; i++)
		{
			double num7 = shakeSamples[i].Point.X - shakeSamples[i - 1].Point.X;
			double num8 = shakeSamples[i].Point.Y - shakeSamples[i - 1].Point.Y;
			num6 += Math.Sqrt(num7 * num7 + num8 * num8);
			if (Math.Abs(num7) > 2.0)
			{
				int num9 = ((num7 > 0.0) ? 1 : (-1));
				if (num4 != 0 && num9 != num4)
				{
					num3++;
				}
				num4 = num9;
			}
			if (Math.Abs(num8) > 2.0)
			{
				int num10 = ((num8 > 0.0) ? 1 : (-1));
				if (num5 != 0 && num10 != num5)
				{
					num3++;
				}
				num5 = num10;
			}
		}
		if (num3 >= 6)
		{
			return num6 >= 260.0;
		}
		return false;
	}

	private void BeginDetach(double now)
	{
		lastShakeDetectedAt = now;
		shakeSamples.Clear();
		if (detached || voiceState != 0)
		{
			return;
		}
		detached = true;
		detachedVoiceSession = false;
		detachHovered = false;
		stopPillShown = false;
		detachedAt = now;
		reattachAt = now + 3.0;
		detachedScale = new SpringState1D
		{
			Value = 1.0
		};
		spring.VelocityX = (spring.VelocityY = 0.0);
		double num = currentDisplay?.Scale ?? 1.0;
		double num2 = 44.0 * num;
		if (catcher == null)
		{
			catcher = new DetachedClickCatcher();
			catcher.Clicked += OnDetachedClicked;
			catcher.HoverChanged += delegate(bool hovered)
			{
				detachHovered = hovered;
				host.RequestFrames();
			};
		}
		catcher.Show(new RectD(spring.X - num2 / 2.0, spring.Y - num2 / 2.0, num2, num2));
		Log.Info("Overlay", "cursor detached (shake)");
		DetachBegan?.Invoke();
		host.RequestFrames();
	}

	private void OnDetachedClicked()
	{
		reattachAt = Now() + 5.0;
		if (voiceState == VoiceState.Idle)
		{
			detachedVoiceSession = true;
		}
		DetachedClicked?.Invoke();
		host.RequestFrames();
	}

	public void EndDetach()
	{
		if (detached)
		{
			detached = false;
			detachHovered = false;
			detachedVoiceSession = false;
			reattachAt = double.MaxValue;
			catcher?.Hide();
			pose.BubbleText = "";
			pose.BubbleOpacity = 0.0;
			pose.StopSquareOpacity = 0.0;
			pose.WaveformScale = 1.0;
			Log.Info("Overlay", "cursor reattached");
			host.RequestFrames();
		}
	}

	private bool UpdateDetached(double now, double dt)
	{
		if (!detached)
		{
			if (mode != 0)
			{
				if (detachedScale.Value != 0.0)
				{
					detachedScale = new SpringState1D
					{
						Value = 1.0
					};
				}
				return false;
			}
			if (detachedScale.Value != 0.0 && Math.Abs(detachedScale.Value - 1.0) > 0.001)
			{
				if (dt > 0.0)
				{
					detachedScale.Advance(1.0, dt, in OverlayCursorSpring.BubbleScale);
				}
				pose.TriangleScale = detachedScale.Value;
				pose.TriangleGlowRadius = 8.0;
				return true;
			}
			return false;
		}
		if (voiceState == VoiceState.Idle && now >= reattachAt)
		{
			EndDetach();
			DetachAutoReattached?.Invoke();
			return true;
		}
		double target = (detachHovered ? 1.7 : 1.55);
		if (dt > 0.0)
		{
			detachedScale.Advance(target, dt, in OverlayCursorSpring.BubbleScale);
		}
		bool flag = voiceState == VoiceState.Idle;
		double num = 0.5 + 0.5 * Math.Sin((now - detachedAt) * 2.0 * Math.PI / 1.6);
		pose.TriangleScale = detachedScale.Value;
		pose.TriangleGlowRadius = (double)(detachHovered ? 12 : 8) + (flag ? (num * 5.0) : 0.0);
		floatOffsetPoints = (flag ? (-4.0 * (0.5 + 0.5 * Math.Sin((now - detachedAt) * 2.0 * Math.PI / 2.4))) : 0.0);
		bool flag2 = detachedVoiceSession && voiceState == VoiceState.Listening;
		pose.WaveformScale = (flag2 ? 1.45 : 1.0);
		pose.StopSquareOpacity = ((flag2 && detachHovered) ? 1 : 0);
		if (flag2)
		{
			if (!stopPillShown)
			{
				stopPillShown = true;
				stopPillShownAt = now;
			}
			double num2 = now - stopPillShownAt;
			double bubbleOpacity = (detachHovered ? 1.0 : ((num2 < 0.2) ? (num2 / 0.2) : ((num2 < 3.0) ? 1.0 : Math.Max(0.0, 1.0 - (num2 - 3.0) / 0.4))));
			pose.BubbleText = (pose.BubbleLabel = (detachHovered ? "click to stop" : "click again to stop recording"));
			pose.BubbleScale = 1.0;
			pose.BubbleOpacity = bubbleOpacity;
		}
		else if (mode == NavigationMode.FollowingCursor)
		{
			pose.BubbleOpacity = 0.0;
			pose.BubbleText = "";
		}
		return true;
	}

	public void Dock(TaskCompletionSource completion)
	{
		if (!docked && !sayingGoodbye)
		{
			EndDetach();
			docked = true;
			host.RequestFrames();
		}
		completion.TrySetResult();
	}

	public void SayGoodbyeThenDock(string text, TaskCompletionSource completion)
	{
		if (docked || sayingGoodbye)
		{
			completion.TrySetResult();
			return;
		}
		EndDetach();
		if (!showRequested)
		{
			docked = true;
			completion.TrySetResult();
			host.RequestFrames();
			return;
		}
		sayingGoodbye = true;
		TaskCompletionSource taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		CancelWelcome();
		message.Show(text, streamed: true, 1.1, taskCompletionSource, Now(), 0.6);
		host.BringToTop();
		host.RequestFrames();
		taskCompletionSource.Task.ContinueWith(delegate
		{
			host.Post(delegate
			{
				FinishGoodbye();
				completion.TrySetResult();
			});
		}, TaskScheduler.Default);
	}

	private void FinishGoodbye()
	{
		if (sayingGoodbye)
		{
			sayingGoodbye = false;
			docked = true;
			if (voiceState == VoiceState.Idle && !message.IsShowing && mode == NavigationMode.FollowingCursor && !detached)
			{
				opacity = (opacityFrom = (opacityTo = 0.0));
				clusterVisual3?.SetOpacity(0f);
				host.RequestCommit();
			}
			host.RequestFrames();
		}
	}

	private double GoodbyeOpacityFactor()
	{
		if (!sayingGoodbye || voiceState != 0 || !message.IsFadingOut)
		{
			return 1.0;
		}
		return Math.Clamp(message.Opacity, 0.0, 1.0);
	}

	public void Undock(PointD anchorPx, TaskCompletionSource completion)
	{
		bool num = docked && opacity <= 0.01 && voiceState == VoiceState.Idle && !message.IsShowing;
		docked = false;
		if (num && showRequested && mode == NavigationMode.FollowingCursor && !detached)
		{
			SnapToFollowTarget(DisplayEnumeratorFast.CursorPositionPx());
		}
		completion.TrySetResult();
		host.RequestFrames();
	}

	public void ApplyRequestedVoiceState()
	{
		VoiceState requestedVoiceState = RequestedVoiceState;
		if (requestedVoiceState == voiceState)
		{
			return;
		}
		double num = Now();
		if (docked && voiceState == VoiceState.Idle && mode == NavigationMode.FollowingCursor && !detached)
		{
			SnapToFollowTarget(DisplayEnumeratorFast.CursorPositionPx());
		}
		if (detached && voiceState == VoiceState.Listening && requestedVoiceState != VoiceState.Listening)
		{
			EndDetach();
		}
		if (voiceState == VoiceState.Processing && requestedVoiceState != VoiceState.Processing)
		{
			if (mode == NavigationMode.NavigatingToTarget || thinkingStartTime < 0.0)
			{
				dotsFadeStartTime = num;
				mergeStartTime = -1.0;
			}
			else
			{
				mergeStartTime = num;
			}
			thinkingStartTime = -1.0;
		}
		if (requestedVoiceState == VoiceState.Processing)
		{
			thinkingStartTime = num;
			mergeStartTime = -1.0;
			dotsFadeStartTime = -1.0;
		}
		voiceState = requestedVoiceState;
		host.RequestFrames();
	}

	public void PointAt(PointD screenPx, string? label, TaskCompletionSource completion)
	{
		pointCompletion?.TrySetResult();
		pointCompletion = completion;
		DisplayInfo displayInfo = DisplayEnumerator.DisplayContaining(screenPx, host.Displays);
		double scale = displayInfo.Scale;
		double value = screenPx.X + 8.0 * scale;
		double value2 = screenPx.Y + 12.0 * scale;
		RectD boundsPx = displayInfo.BoundsPx;
		value = Math.Clamp(value, boundsPx.X + 20.0 * scale, boundsPx.Right - 20.0 * scale);
		value2 = Math.Clamp(value2, boundsPx.Y + 20.0 * scale, boundsPx.Bottom - 20.0 * scale);
		pointingText = (string.IsNullOrWhiteSpace(label) ? PointingPhrases[random.Next(PointingPhrases.Length)] : label);
		EndDetach();
		CancelWelcome();
		if (!showRequested)
		{
			Show();
		}
		host.BringToTop();
		StartFlight(value, value2, returning: false, 0.6, pointingText);
	}

	public void RidePen(IReadOnlyList<PenStrokePath> strokes)
	{
		PointD? pointD = strokes.FirstOrDefault((PenStrokePath stroke) => stroke.PointsPx.Count > 0)?.PointsPx[0];
		if (!pointD.HasValue)
		{
			return;
		}
		PointD valueOrDefault = pointD.GetValueOrDefault();
		if (!showRequested)
		{
			return;
		}
		EndDetach();
		DisplayInfo displayInfo = DisplayEnumerator.DisplayContaining(valueOrDefault, host.Displays);
		double scale = Math.Max(0.5, displayInfo.Scale);
		RectD bounds = displayInfo.BoundsPx;
		double now = Now();
		host.BringToTop();
		if (mode != NavigationMode.RidingPen || !(penDisplay?.DeviceName == displayInfo.DeviceName))
		{
			pointCompletion?.TrySetResult();
			pointCompletion = null;
			penDisplay = displayInfo;
			if ((object)currentDisplay == null || currentDisplay.DeviceName != displayInfo.DeviceName)
			{
				currentDisplay = displayInfo;
				renderer.EnsureScale(displayInfo.Scale);
				hasRendered = false;
			}
			mode = NavigationMode.RidingPen;
			isReturningToCursor = false;
			rotationBackStartTime = -1.0;
			pose.BubbleText = "";
			pose.BubbleOpacity = 0.0;
			pose.TriangleScale = 1.0;
			pose.TriangleGlowRadius = 8.0;
			spring.VelocityX = (spring.VelocityY = 0.0);
			penRide.Begin(ToPoints(new PointD(spring.X, spring.Y)), now, bounds.Width / scale, bounds.Height / scale);
		}
		penRide.Add(strokes.Select((PenStrokePath stroke) => (stroke.PointsPx.Select(ToPoints).ToArray(), BeginTime: stroke.BeginTime, Duration: stroke.Duration, X1: stroke.X1, Y1: stroke.Y1, X2: stroke.X2, Y2: stroke.Y2)), now);
		if (PenRideTrajectory.AnimationsAreReduced)
		{
			ParkAtPen(now, penRide.Sample(double.MaxValue).Position);
		}
		host.RequestFrames();
		PointD ToPoints(PointD px)
		{
			return new PointD((px.X - bounds.X) / scale, (px.Y - bounds.Y) / scale);
		}
	}

	public void EndPenRide()
	{
		NavigationMode navigationMode = mode;
		if ((uint)(navigationMode - 3) <= 1u)
		{
			FinishNavigationAndResumeFollowing(Now(), keepPosition: true);
			host.RequestFrames();
		}
	}

	private void ParkAtPen(double now, PointD positionPoints)
	{
		DisplayInfo displayInfo = penDisplay;
		if ((object)displayInfo != null)
		{
			spring.X = displayInfo.BoundsPx.X + positionPoints.X * displayInfo.Scale;
			spring.Y = displayInfo.BoundsPx.Y + positionPoints.Y * displayInfo.Scale;
		}
		spring.VelocityX = (spring.VelocityY = 0.0);
		mode = NavigationMode.ParkedAtPen;
		pose.TriangleScale = 1.0;
		pose.TriangleGlowRadius = 8.0;
		BeginRotationBack(now, pose.TriangleRotationDegrees);
		penParkMouseBaseline = DisplayEnumeratorFast.CursorPositionPx();
	}

	public void ReturnToCursor()
	{
		pointCompletion?.TrySetResult();
		pointCompletion = null;
		if (mode != 0)
		{
			BeginReturnFlight();
		}
	}

	public bool OnIdleWake(in OverlayFrame frame)
	{
		bool num = frame.MousePx != lastMouse;
		if (num)
		{
			lastMouseMoveTime = frame.TimeSeconds;
		}
		bool flag = ProbeSystemCursorVisibility(frame.TimeSeconds);
		if (!(num || flag) && !message.NeedsFrame(frame.TimeSeconds))
		{
			return detached;
		}
		return true;
	}

	public bool OnFrame(in OverlayFrame frame)
	{
		double timeSeconds = frame.TimeSeconds;
		double deltaSeconds = frame.DeltaSeconds;
		PointD mousePx = frame.MousePx;
		bool flag = mousePx != lastMouse;
		if (flag)
		{
			lastMouseMoveTime = timeSeconds;
			framesWithoutMovement = 0;
		}
		else
		{
			framesWithoutMovement++;
		}
		lastMouse = mousePx;
		ProbeSystemCursorVisibility(timeSeconds);
		bool num = UpdateOpacity(timeSeconds) | UpdateNavigation(timeSeconds, deltaSeconds, mousePx, flag) | UpdateDetached(timeSeconds, deltaSeconds) | UpdateVoiceVisuals(timeSeconds, deltaSeconds);
		UpdatePose();
		bool num2 = !hasRendered || renderer.PiecesNeedDrawing;
		bool flag2 = false;
		if (num2 && opacity > 0.0)
		{
			if (host.GpuCaughtUp)
			{
				renderer.Render(in pose);
				hasRendered = true;
			}
			else
			{
				flag2 = true;
			}
		}
		double num3 = currentDisplay?.Scale ?? 1.0;
		RectD workAreaPx = currentDisplay?.WorkAreaPx ?? host.VirtualScreenPx;
		double num4 = spring.Y + (detached ? (floatOffsetPoints * num3) : 0.0);
		renderer.SetScreenPlacement(new PointD(spring.X, num4), workAreaPx);
		renderer.PlacePieces(in pose);
		Vector2 anchorPx = renderer.AnchorPx;
		clusterVisual.SetOffsetX((float)Math.Round(spring.X - host.VirtualScreenPx.X - (double)anchorPx.X));
		clusterVisual.SetOffsetY((float)Math.Round(num4 - host.VirtualScreenPx.Y - (double)anchorPx.Y));
		host.RequestCommit();
		bool num5 = num | message.Update(timeSeconds, deltaSeconds);
		if (message.IsShowing)
		{
			message.Present(detached ? FollowTarget(mousePx) : new PointD(spring.X, spring.Y), num3, host.VirtualScreenPx, workAreaPx, timeSeconds);
		}
		TaskCompletionSource taskCompletionSource = welcomeCompletion;
		if (taskCompletionSource != null)
		{
			Task task = taskCompletionSource.Task;
			if (task != null && task.IsCompleted)
			{
				welcomeCompletion = null;
			}
		}
		if (opacity <= 0.0 && !showRequested && !message.IsShowing)
		{
			RegisterVisible(visible: false);
		}
		return num5 || flag2;
	}

	private bool UpdateNavigation(double now, double dt, PointD mouse, bool mouseMoved)
	{
		switch (mode)
		{
		case NavigationMode.FollowingCursor:
		{
			if (detached)
			{
				pose.TriangleRotationDegrees = RotationTowardRest(now);
				return true;
			}
			if (docked && voiceState == VoiceState.Idle && !message.IsShowing)
			{
				pose.TriangleRotationDegrees = RotationTowardRest(now);
				return rotationBackStartTime >= 0.0;
			}
			if (shakeToDetachEnabled && mouseMoved && voiceState == VoiceState.Idle && !docked && DetectShake(mouse, now))
			{
				BeginDetach(now);
				return true;
			}
			DisplayInfo displayInfo = DisplayEnumerator.DisplayContaining(mouse, host.Displays);
			if ((object)displayInfo != currentDisplay && ((object)currentDisplay == null || displayInfo.DeviceName != currentDisplay.DeviceName))
			{
				currentDisplay = displayInfo;
				renderer.EnsureScale(displayInfo.Scale);
				hasRendered = false;
				SnapToFollowTarget(mouse);
				return true;
			}
			PointD pointD = FollowTarget(mouse);
			if (dt > 0.0)
			{
				spring.Advance(pointD.X, pointD.Y, dt, in OverlayCursorSpring.CursorFollow);
			}
			double num = currentDisplay?.Scale ?? 1.0;
			bool num2 = spring.IsSettled(pointD.X, pointD.Y, 0.3 * num, 4.0 * num);
			if (num2)
			{
				spring.X = pointD.X;
				spring.Y = pointD.Y;
				spring.VelocityX = (spring.VelocityY = 0.0);
			}
			pose.TriangleRotationDegrees = RotationTowardRest(now);
			if (num2 && framesWithoutMovement >= 2)
			{
				return rotationBackStartTime >= 0.0;
			}
			return true;
		}
		case NavigationMode.NavigatingToTarget:
		{
			if (flightAwaitsLabel)
			{
				if (!host.GpuCaughtUp && now - flightStartTime < 0.25)
				{
					return true;
				}
				flightAwaitsLabel = false;
				flightStartTime = now;
			}
			if (isReturningToCursor && Distance(mouse, returnStartMouse) / flightScaleForPoints > 100.0)
			{
				FinishNavigationAndResumeFollowing(now, keepPosition: true);
				return true;
			}
			double num3 = now - flightStartTime;
			double linearProgress = num3 / flight.DurationSeconds;
			(double, double, double, double) tuple = flight.Pose(linearProgress);
			spring.X = tuple.Item1 * flightScaleForPoints;
			spring.Y = tuple.Item2 * flightScaleForPoints;
			spring.VelocityX = (spring.VelocityY = 0.0);
			pose.TriangleRotationDegrees = ((num3 < 1.0 / 60.0) ? (-35.0) : tuple.Item3);
			pose.TriangleScale = tuple.Item4;
			pose.TriangleGlowRadius = 8.0 + (tuple.Item4 - 1.0) * 20.0;
			if (num3 >= flight.CompletionDelaySeconds)
			{
				pose.TriangleScale = 1.0;
				pose.TriangleGlowRadius = 8.0;
				(double, double, double, double) tuple2 = flight.Pose(1.0);
				spring.X = tuple2.Item1 * flightScaleForPoints;
				spring.Y = tuple2.Item2 * flightScaleForPoints;
				if (isReturningToCursor)
				{
					FinishNavigationAndResumeFollowing(now, keepPosition: false, tuple2.Item3);
				}
				else
				{
					StartPointing(now, tuple2.Item3);
				}
			}
			return true;
		}
		case NavigationMode.RidingPen:
		{
			(PointD, PointD, double, double) tuple3 = penRide.Sample(now);
			DisplayInfo displayInfo2 = penDisplay;
			if ((object)displayInfo2 != null)
			{
				spring.X = displayInfo2.BoundsPx.X + tuple3.Item1.X * displayInfo2.Scale;
				spring.Y = displayInfo2.BoundsPx.Y + tuple3.Item1.Y * displayInfo2.Scale;
			}
			pose.TriangleRotationDegrees = -35.0 + tuple3.Item3;
			pose.TriangleScale = tuple3.Item4;
			pose.TriangleGlowRadius = 8.0 * tuple3.Item4;
			if (now >= penRide.EndTime)
			{
				ParkAtPen(now, tuple3.Item1);
			}
			return true;
		}
		case NavigationMode.ParkedAtPen:
		{
			pose.TriangleRotationDegrees = RotationTowardRest(now);
			double num4 = currentDisplay?.Scale ?? 1.0;
			if (Distance(mouse, penParkMouseBaseline) / num4 > 40.0)
			{
				BeginReturnFlight();
				return true;
			}
			return rotationBackStartTime >= 0.0;
		}
		case NavigationMode.PointingAtTarget:
			pose.TriangleRotationDegrees = RotationTowardRest(now);
			if (holdsForStreamedBubble)
			{
				pose.BubbleText = "";
				pose.BubbleOpacity = 0.0;
				if (now >= nextStreamedBubbleCheckTime)
				{
					if (message.IsShowing)
					{
						nextStreamedBubbleCheckTime = now + 0.5;
					}
					else
					{
						BeginReturnFlight();
					}
				}
				return true;
			}
			if (revealedCharacters < pointingText.Length && now >= nextCharacterTime)
			{
				revealedCharacters++;
				nextCharacterTime = now + 0.03 + random.NextDouble() * 0.03;
				if (revealedCharacters == pointingText.Length)
				{
					holdUntilTime = now + 3.0;
					pointCompletion?.TrySetResult();
					pointCompletion = null;
				}
			}
			if (revealedCharacters > 0 && dt > 0.0)
			{
				bubbleScale.Advance(1.0, dt, in OverlayCursorSpring.BubbleScale);
			}
			pose.BubbleScale = ((revealedCharacters > 0) ? bubbleScale.Value : 0.5);
			pose.BubbleText = pointingText.Substring(0, revealedCharacters);
			pose.BubbleLabel = pointingText;
			if (holdUntilTime >= 0.0 && now >= holdUntilTime && bubbleFadeStartTime < 0.0)
			{
				bubbleFadeStartTime = now;
				returnAtTime = now + 0.5;
			}
			pose.BubbleOpacity = ((bubbleFadeStartTime < 0.0) ? 1.0 : (1.0 - Easing.EaseOut((now - bubbleFadeStartTime) / 0.5)));
			if (returnAtTime >= 0.0 && now >= returnAtTime)
			{
				BeginReturnFlight();
			}
			return true;
		default:
			return false;
		}
	}

	private void StartFlight(double targetX, double targetY, bool returning, double minimumDuration, string? label = null)
	{
		double now = Now();
		DisplayInfo displayInfo = DisplayEnumerator.DisplayContaining(new PointD(spring.X, spring.Y), host.Displays);
		flightScaleForPoints = displayInfo.Scale;
		if ((object)currentDisplay == null || currentDisplay.DeviceName != displayInfo.DeviceName)
		{
			currentDisplay = displayInfo;
			renderer.EnsureScale(displayInfo.Scale);
			hasRendered = false;
		}
		flightAwaitsLabel = label != null && renderer.PrepareLabel(label);
		flight = new OverlayCursorFlightPath(spring.X / flightScaleForPoints, spring.Y / flightScaleForPoints, targetX / flightScaleForPoints, targetY / flightScaleForPoints, minimumDuration);
		flightStartTime = now;
		mode = NavigationMode.NavigatingToTarget;
		isReturningToCursor = returning;
		rotationBackStartTime = -1.0;
		pose.BubbleText = "";
		pose.BubbleOpacity = 0.0;
		if (returning)
		{
			returnStartMouse = DisplayEnumeratorFast.CursorPositionPx();
		}
		if (voiceState != VoiceState.Processing && thinkingStartTime < 0.0 && mergeStartTime >= 0.0)
		{
			mergeStartTime = -1.0;
			dotsFadeStartTime = now;
		}
		if (PenRideTrajectory.AnimationsAreReduced)
		{
			spring.X = targetX;
			spring.Y = targetY;
			spring.VelocityX = (spring.VelocityY = 0.0);
			if (returning)
			{
				FinishNavigationAndResumeFollowing(now, keepPosition: true);
			}
			else
			{
				StartPointing(now, -35.0);
			}
		}
		host.RequestFrames();
	}

	private void StartPointing(double now, double flightEndRotation)
	{
		mode = NavigationMode.PointingAtTarget;
		BeginRotationBack(now, flightEndRotation);
		revealedCharacters = 0;
		nextCharacterTime = now;
		bubbleScale = new SpringState1D
		{
			Value = 0.5
		};
		holdUntilTime = (bubbleFadeStartTime = (returnAtTime = -1.0));
		pose.BubbleOpacity = 1.0;
		holdsForStreamedBubble = message.IsShowing;
		if (holdsForStreamedBubble)
		{
			nextStreamedBubbleCheckTime = now + 0.5;
			pose.BubbleOpacity = 0.0;
			pointCompletion?.TrySetResult();
			pointCompletion = null;
		}
	}

	private void BeginReturnFlight()
	{
		PointD mouse = DisplayEnumeratorFast.CursorPositionPx();
		PointD pointD = FollowTarget(mouse);
		holdUntilTime = (bubbleFadeStartTime = (returnAtTime = -1.0));
		holdsForStreamedBubble = false;
		StartFlight(pointD.X, pointD.Y, returning: true, 0.6);
	}

	private void FinishNavigationAndResumeFollowing(double now, bool keepPosition, double flightEndRotation = -35.0)
	{
		mode = NavigationMode.FollowingCursor;
		isReturningToCursor = false;
		pose.TriangleScale = 1.0;
		pose.TriangleGlowRadius = 8.0;
		pose.BubbleText = "";
		pose.BubbleOpacity = 0.0;
		BeginRotationBack(now, keepPosition ? pose.TriangleRotationDegrees : flightEndRotation);
		spring.VelocityX = (spring.VelocityY = 0.0);
		currentDisplay = DisplayEnumerator.DisplayContaining(DisplayEnumeratorFast.CursorPositionPx(), host.Displays);
	}

	private void BeginRotationBack(double now, double fromDegrees)
	{
		double num;
		for (num = fromDegrees; num > 270.0; num -= 360.0)
		{
		}
		for (; num <= -90.0; num += 360.0)
		{
		}
		rotationBackFrom = num;
		rotationBackStartTime = now;
		pose.TriangleRotationDegrees = num;
	}

	private double RotationTowardRest(double now)
	{
		if (rotationBackStartTime < 0.0)
		{
			return -35.0;
		}
		double num = (now - rotationBackStartTime) / 0.3;
		if (num >= 1.0)
		{
			rotationBackStartTime = -1.0;
			return -35.0;
		}
		return Easing.Lerp(rotationBackFrom, -35.0, Easing.EaseInOut(num));
	}

	private bool UpdateVoiceVisuals(double now, double dt)
	{
		bool flag = false;
		double num = ((voiceState == VoiceState.Listening) ? 1 : 0);
		waveformOpacity = MoveToward(waveformOpacity, num, dt / 0.25);
		flag |= waveformOpacity != num;
		if (waveformOpacity > 0.0)
		{
			displayedLevel = MoveToward(displayedLevel, LatestLevel, dt / 0.08 * Math.Max(0.05, Math.Abs((double)LatestLevel - displayedLevel)));
			double num2 = Math.Pow(Math.Min(Math.Max(displayedLevel - 0.008, 0.0) * 2.85, 1.0), 0.76);
			ReadOnlySpan<double> readOnlySpan = RuntimeHelpers.CreateSpan<double>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			for (int i = 0; i < 5; i++)
			{
				pose.WaveformBarHeights[i] = (float)(3.0 + num2 * 10.0 * readOnlySpan[i] + 0.75 + 0.75 * Math.Sin(now * 3.6 + (double)i * 0.35));
			}
			flag = true;
		}
		double num3 = 0.0;
		double val = 1.0;
		pose.DotCount = 0;
		if (thinkingStartTime >= 0.0)
		{
			double num4 = now - thinkingStartTime;
			if (num4 < 0.31)
			{
				double num5 = Easing.EaseOutCubic(num4 / 0.31);
				SetCornerDots(Easing.Lerp(ThinkingDots.TriangleCircleDistance, 7.544, num5), Easing.Lerp(1.44, 2.116, num5), 0);
				val = 1.0 - num5;
				num3 = 1.0;
			}
			else if (num4 < 0.5)
			{
				double num6 = 0.92 + 0.08 * Easing.SpringOut((num4 - 0.31) / 0.19);
				SetCornerDots(8.2 * num6, 2.3 * num6, 0);
				val = 0.0;
				num3 = 1.0;
			}
			else
			{
				ThinkingDots.LoopPose(num4 - 0.5, ref pose);
				val = 0.0;
				num3 = 1.0;
			}
			flag = true;
		}
		else if (mergeStartTime >= 0.0)
		{
			double num7 = now - mergeStartTime;
			if (num7 < 0.16)
			{
				double t = Easing.EaseInOut(num7 / 0.16);
				SetCornerDots(Easing.Lerp(8.2, 7.544, t), Easing.Lerp(2.3, 2.116, t), 0);
				val = 0.0;
				num3 = 1.0;
				flag = true;
			}
			else if (num7 < 0.47)
			{
				double num8 = Easing.EaseOutCubic(1.0 - (num7 - 0.16) / 0.31);
				SetCornerDots(Easing.Lerp(ThinkingDots.TriangleCircleDistance, 7.544, num8), Easing.Lerp(1.44, 2.116, num8), 0);
				val = 1.0 - num8;
				num3 = 1.0;
				flag = true;
			}
			else
			{
				mergeStartTime = -1.0;
			}
		}
		else if (dotsFadeStartTime >= 0.0)
		{
			double num9 = now - dotsFadeStartTime;
			if (num9 < 0.15)
			{
				ThinkingDots.LoopPose(0.0, ref pose);
				num3 = 1.0 - num9 / 0.15;
				val = num9 / 0.15;
				flag = true;
			}
			else
			{
				dotsFadeStartTime = -1.0;
			}
		}
		pose.DotsOpacity = num3;
		if (num3 <= 0.0)
		{
			pose.DotCount = 0;
		}
		double num10 = ((voiceState != VoiceState.Listening) ? 1 : 0);
		triangleVoiceOpacity = MoveToward(triangleVoiceOpacity, num10, dt / 0.25);
		flag |= triangleVoiceOpacity != num10;
		pose.TriangleOpacity = Math.Min(triangleVoiceOpacity, val);
		pose.WaveformOpacity = waveformOpacity;
		if (dwellStartTime >= 0.0 && mode == NavigationMode.FollowingCursor && !detached && !docked && voiceState == VoiceState.Idle && num3 <= 0.0)
		{
			double num11 = Math.Clamp((Now() - dwellStartTime) / dwellDurationSeconds, 0.0, 1.0);
			pose.DwellRingOpacity = 1.0;
			pose.DwellRingProgress = num11;
			pose.TriangleOpacity = 0.0;
			flag = flag || num11 < 1.0;
		}
		else
		{
			pose.DwellRingOpacity = 0.0;
			pose.DwellRingProgress = 0.0;
		}
		return flag;
	}

	public void SetHoverDwell(double? seconds)
	{
		if (seconds.HasValue)
		{
			double valueOrDefault = seconds.GetValueOrDefault();
			if (valueOrDefault > 0.0)
			{
				dwellStartTime = Now();
				dwellDurationSeconds = valueOrDefault;
				goto IL_0040;
			}
		}
		dwellStartTime = -1.0;
		goto IL_0040;
		IL_0040:
		host.RequestFrames();
	}

	private void SetCornerDots(double distance, double radius, int cornerOffset)
	{
		pose.DotCount = 3;
		for (int i = 0; i < 3; i++)
		{
			Vector2 vector = ThinkingDots.CornerPoint((i + cornerOffset) % 3, distance);
			pose.DotCenters[i] = vector;
			pose.DotRadii[i] = radius;
			pose.DotAcrossScale[i] = 1.0;
			pose.DotAngles[i] = 0.0;
		}
	}

	private static double MoveToward(double current, double target, double maxStep)
	{
		if (maxStep <= 0.0)
		{
			return current;
		}
		if (!(current < target))
		{
			return Math.Max(target, current - maxStep);
		}
		return Math.Min(target, current + maxStep);
	}

	private void StartOpacityAnimation(double target, double duration, Func<double, double> curve, double now)
	{
		opacityFrom = opacity;
		opacityTo = target;
		opacityStart = now;
		opacityDuration = duration;
		opacityCurve = curve;
	}

	private double WantedOpacity(double now)
	{
		if (docked && mode == NavigationMode.FollowingCursor && voiceState == VoiceState.Idle && !detached && !message.IsShowing)
		{
			return 0.0;
		}
		bool num = mode != 0 || voiceState != 0 || detached || message.IsShowing;
		bool flag = systemCursorVisible || hiddenReportIsStale;
		bool flag2 = num || IgnoreSystemVisibilityForTests || (flag && (!foregroundIsFullscreen || !(now - lastMouseMoveTime >= 3.0)));
		return (showRequested && flag2) ? 1 : 0;
	}

	private bool UpdateOpacity(double now)
	{
		double num = WantedOpacity(now);
		if (num > 0.0 && opacityTo == 0.0 && hasShownBefore)
		{
			host.BringToTop();
		}
		if (num != opacityTo)
		{
			StartOpacityAnimation(num, (num > 0.0 && opacity == 0.0 && !hasShownBefore) ? 2.0 : (showRequested ? 0.22 : 0.4), Easing.EaseInOut, now);
		}
		double num2 = ((opacityDuration <= 0.0) ? 1.0 : ((now - opacityStart) / opacityDuration));
		opacity = ((num2 >= 1.0) ? opacityTo : Easing.Lerp(opacityFrom, opacityTo, opacityCurve(num2)));
		clusterVisual3?.SetOpacity((float)(opacity * GoodbyeOpacityFactor()));
		if (opacity == opacityTo)
		{
			return sayingGoodbye;
		}
		return true;
	}

	private bool ProbeSystemCursorVisibility(double now)
	{
		if (!showRequested || now - lastVisibilityProbeTime < 0.1)
		{
			return false;
		}
		lastVisibilityProbeTime = now;
		CURSORINFO cURSORINFO = default(CURSORINFO);
		cURSORINFO.cbSize = (uint)Marshal.SizeOf<CURSORINFO>();
		CURSORINFO pci = cURSORINFO;
		if ((bool)PInvoke.GetCursorInfo(ref pci))
		{
			bool flag = (pci.flags & CURSORINFO_FLAGS.CURSOR_SHOWING) != 0;
			if (flag != systemCursorVisible)
			{
				systemCursorVisible = flag;
				hiddenReportIsStale = false;
				if (!flag)
				{
					pointerWhenHidden = DisplayEnumeratorFast.CursorPositionPx();
				}
			}
			else if (!flag && !hiddenReportIsStale)
			{
				PointD pointD = DisplayEnumeratorFast.CursorPositionPx();
				if (Math.Abs(pointD.X - pointerWhenHidden.X) + Math.Abs(pointD.Y - pointerWhenHidden.Y) > 6.0 && !foregroundIsFullscreen && PointerFreedom.MovesFreely(host.VirtualScreenPx))
				{
					hiddenReportIsStale = true;
					Log.Info("Overlay", "the system pointer reads hidden while the mouse moves freely: the buddy shows again");
				}
			}
		}
		if (now - lastFullscreenProbeTime >= 1.0)
		{
			lastFullscreenProbeTime = now;
			bool flag2 = ForegroundWindowIsFullscreen();
			if (flag2 != foregroundIsFullscreen)
			{
				foregroundIsFullscreen = flag2;
				lastMouseMoveTime = now;
			}
			nint num = PointerFreedom.ForegroundWindow();
			if (num != lastForegroundWindow)
			{
				lastForegroundWindow = num;
				if (opacity > 0.0)
				{
					host.BringToTop();
				}
			}
		}
		return WantedOpacity(now) != opacityTo;
	}

	private unsafe static bool ForegroundWindowIsFullscreen()
	{
		HWND foregroundWindow = PInvoke.GetForegroundWindow();
		if (foregroundWindow.IsNull || foregroundWindow == PInvoke.GetShellWindow())
		{
			return false;
		}
		Span<char> span = stackalloc char[32];
		int className;
		fixed (char* ptr = span)
		{
			className = PInvoke.GetClassName(foregroundWindow, ptr, span.Length);
		}
		Span<char> span2 = span.Slice(0, Math.Max(0, className));
		if (span2.SequenceEqual("Progman") || span2.SequenceEqual("WorkerW") || span2.SequenceEqual("Shell_TrayWnd"))
		{
			return false;
		}
		if (!PInvoke.GetWindowRect(foregroundWindow, out var lpRect))
		{
			return false;
		}
		HMONITOR hMonitor = PInvoke.MonitorFromWindow(foregroundWindow, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
		};
		if (!PInvoke.GetMonitorInfo(hMonitor, ref lpmi))
		{
			return false;
		}
		RECT rcMonitor = lpmi.rcMonitor;
		if (lpRect.left <= rcMonitor.left && lpRect.top <= rcMonitor.top && lpRect.right >= rcMonitor.right)
		{
			return lpRect.bottom >= rcMonitor.bottom;
		}
		return false;
	}

	private void RegisterVisible(bool visible)
	{
		if (visible != layerVisibleRegistered)
		{
			layerVisibleRegistered = visible;
			host.SetLayerVisible(visible);
		}
	}

	private void UpdatePose()
	{
	}

	private PointD FollowTarget(PointD mouse)
	{
		double scale = (currentDisplay ?? DisplayEnumerator.DisplayContaining(mouse, host.Displays)).Scale;
		return new PointD(Math.Round(mouse.X + 35.0 * scale), Math.Round(mouse.Y + 25.0 * scale));
	}

	private void SnapToFollowTarget(PointD mouse)
	{
		PointD pointD = FollowTarget(mouse);
		spring.X = pointD.X;
		spring.Y = pointD.Y;
		spring.VelocityX = (spring.VelocityY = 0.0);
	}

	private static double Distance(PointD a, PointD b)
	{
		return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
	}

	private static double Now()
	{
		return (double)Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
	}

	public void OnDisplaysChanged(IReadOnlyList<DisplayInfo> displays, RectD virtualScreenPx)
	{
		currentDisplay = ((displays.Count > 0) ? DisplayEnumerator.DisplayContaining(DisplayEnumeratorFast.CursorPositionPx(), displays) : null);
		if ((object)currentDisplay != null)
		{
			renderer.EnsureScale(currentDisplay.Scale);
		}
		hasRendered = false;
		host.RequestFrames();
	}

	public void Dispose()
	{
		pointCompletion?.TrySetResult();
		message.Dispose();
		catcher?.Dispose();
		RegisterVisible(visible: false);
		host.RootVisual.RemoveVisual(clusterVisual);
		renderer.Dispose();
		clusterVisual3?.Dispose();
		clusterVisual.Dispose();
		host.RequestCommit();
	}

	bool IOverlayLayer.OnFrame(in OverlayFrame frame)
	{
		return OnFrame(in frame);
	}

	bool IOverlayLayer.OnIdleWake(in OverlayFrame frame)
	{
		return OnIdleWake(in frame);
	}
}

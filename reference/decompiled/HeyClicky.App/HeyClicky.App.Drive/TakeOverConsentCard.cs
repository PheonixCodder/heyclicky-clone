using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Drive;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Drive;

public sealed class TakeOverConsentCard
{
	private sealed class DispatcherQueueTimerHandle(DispatcherQueueTimer timer)
	{
		public void Stop()
		{
			timer.Stop();
		}
	}

	public const string DrivingNote = "Hands off the mouse while I drive. Esc stops me.";

	private OverlayWindow? window;

	private DispatcherQueueTimerHandle? timeout;

	private int generation;

	private Action? acceptForTest;

	public bool IsShown => window?.IsShown ?? false;

	internal void AcceptForTest()
	{
		if (IsShown)
		{
			acceptForTest?.Invoke();
		}
	}

	public void Present(string goal, string? line, Color accent, Action onAccept, Action onDecline, Action<string, double>? playChime = null, Action? onTimedOut = null, string note = "Hands off the mouse while I drive. Esc stops me.", string acceptTitle = "Go ahead", bool asksTheUser = false, int timeoutSeconds = 45, string? alwaysTitle = null, Action? onAlways = null)
	{
		Dismiss(animated: false);
		DriveNotchOwnership.SetConsentShown(shown: true);
		int presentGeneration = ++generation;
		Displays.Monitor monitor = Displays.UnderCursor();
		double scale = monitor.Scale;
		double num = ((alwaysTitle == null) ? Math.Max(360.0, 500.0) : Math.Max(460.0, 600.0));
		double num2 = 8.0;
		double num3 = num - 60.0 - 42.0;
		object obj;
		if (line != null)
		{
			string text = ClickyDriveLines.Sentence(line);
			obj = ((text != null && text.Length > 0) ? text : null);
		}
		else
		{
			obj = null;
		}
		string text2 = (string)obj;
		TextBlock textBlock = Ui.Text(text2 ?? ClickyDriveLines.Brief(goal), (text2 == null) ? 11 : 14, (text2 == null) ? FontWeights.Normal : FontWeights.SemiBold, Colors.White, TextAlignment.Left, 0, wrap: true);
		textBlock.Measure(new Size((text2 == null) ? num3 : (num3 - 48.0), double.PositiveInfinity));
		double height = textBlock.DesiredSize.Height;
		TextBlock textBlock2 = Ui.Text(note, 11.0, FontWeights.Normal, Colors.White);
		textBlock2.TextWrapping = TextWrapping.WrapWholeWords;
		textBlock2.Measure(new Size(num3, double.PositiveInfinity));
		double num4 = ((textBlock2.DesiredSize.Height > 16.0) ? 14 : 0);
		double num5 = num2 + num4 + (double)((text2 != null) ? ((height > 20.0) ? 128 : 110) : ((height > 16.0) ? 138 : 124));
		if ((object)window == null)
		{
			window = new OverlayWindow("HeyClicky drive", excludeFromCapture: true, clickThroughEverywhere: false, activatable: false, topmost: true, showInTaskbar: false, hiddenFromOwnCapture: true);
		}
		int x = monitor.Bounds.Left + (int)Math.Round(((double)monitor.Bounds.Width - num * scale) / 2.0);
		window.PlacePx(x, monitor.Bounds.Top, (int)Math.Ceiling(num * scale), (int)Math.Ceiling((num5 + 30.0) * scale));
		window.SetRegionPx(0, 0, (int)Math.Ceiling(num * scale), (int)Math.Ceiling((num5 + 30.0) * scale));
		Grid grid = new Grid
		{
			Width = num,
			Height = num5 + 30.0,
			VerticalAlignment = VerticalAlignment.Top
		};
		Grid grid2 = new Grid
		{
			Width = num,
			Height = num5,
			VerticalAlignment = VerticalAlignment.Top
		};
		grid.Children.Add(grid2);
		Compositor compositor = Ui.Compositor;
		CompositionPath path = new CompositionPath(DriveArt.NotchAttachedPath(CanvasDevice.GetSharedDevice(), num, num5, 26.0));
		ShapeVisual shapeVisual = compositor.CreateShapeVisual();
		shapeVisual.Size = new Vector2((float)num, (float)num5);
		CompositionSpriteShape compositionSpriteShape = compositor.CreateSpriteShape(compositor.CreatePathGeometry(path));
		compositionSpriteShape.FillBrush = compositor.CreateColorBrush(Colors.Black);
		shapeVisual.Shapes.Add(compositionSpriteShape);
		ShapeVisual shapeVisual2 = compositor.CreateShapeVisual();
		shapeVisual2.Size = shapeVisual.Size;
		CompositionSpriteShape compositionSpriteShape2 = compositor.CreateSpriteShape(compositor.CreatePathGeometry(path));
		compositionSpriteShape2.FillBrush = compositor.CreateColorBrush(Colors.Black);
		shapeVisual2.Shapes.Add(compositionSpriteShape2);
		CompositionVisualSurface compositionVisualSurface = compositor.CreateVisualSurface();
		compositionVisualSurface.SourceVisual = shapeVisual2;
		compositionVisualSurface.SourceSize = shapeVisual.Size;
		CompositionSurfaceBrush compositionSurfaceBrush = compositor.CreateSurfaceBrush(compositionVisualSurface);
		compositionSurfaceBrush.Stretch = CompositionStretch.None;
		DropShadow dropShadow = compositor.CreateDropShadow();
		dropShadow.Color = DS.Black(0.45);
		dropShadow.BlurRadius = 36f;
		dropShadow.Offset = new Vector3(0f, 8f, 0f);
		dropShadow.Mask = compositionSurfaceBrush;
		SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
		spriteVisual.Size = shapeVisual.Size;
		spriteVisual.Shadow = dropShadow;
		ContainerVisual containerVisual = compositor.CreateContainerVisual();
		containerVisual.Size = shapeVisual.Size;
		containerVisual.Children.InsertAtTop(spriteVisual);
		containerVisual.Children.InsertAtTop(shapeVisual);
		Grid grid3 = new Grid();
		ElementCompositionPreview.SetElementChildVisual(grid3, containerVisual);
		grid2.Children.Add(grid3);
		Image item = new Image
		{
			Width = 44.0,
			Height = 44.0,
			Margin = new Thickness(-6.0),
			Source = (asksTheUser ? DriveArt.YourTurnChip(32.0, 44.0, scale) : DriveArt.DriveChip(32.0, 44.0, scale)),
			VerticalAlignment = VerticalAlignment.Top
		};
		TextBlock textBlock3 = Ui.Text(text2 ?? "Want me to do it?", 14.0, FontWeights.SemiBold, DS.White(0.95), TextAlignment.Left, 2);
		textBlock3.TextWrapping = TextWrapping.WrapWholeWords;
		(WriteableBitmap? Image, double Width, double Height) tuple = DriveArt.Stamp("YOU", DriveArt.NeedsYouOnBlack, 7.5, scale);
		WriteableBitmap item2 = tuple.Image;
		double item3 = tuple.Width;
		double item4 = tuple.Height;
		Image image = new Image
		{
			Source = item2,
			Width = item3,
			Height = item4,
			Opacity = 0.92,
			VerticalAlignment = VerticalAlignment.Top,
			Rotation = -5f,
			CenterPoint = new Vector3((float)item3 / 2f, (float)item4 / 2f, 0f)
		};
		Grid grid4 = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid4.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Auto)
		});
		grid4.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid4.Children.Add(textBlock3);
		Grid.SetColumn(image, 1);
		image.HorizontalAlignment = HorizontalAlignment.Left;
		grid4.Children.Add(image);
		textBlock3.MaxWidth = num3 - item3 - 8.0;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0
		};
		stackPanel.Children.Add(grid4);
		if (text2 == null)
		{
			TextBlock item5 = Ui.Text(ClickyDriveLines.Brief(goal), 11.0, FontWeights.Normal, DS.White(0.62), TextAlignment.Left, 2);
			stackPanel.Children.Add(item5);
		}
		TextBlock textBlock4 = Ui.Text(note, 11.0, FontWeights.Normal, DS.White(0.42), TextAlignment.Left, 2);
		textBlock4.TextWrapping = TextWrapping.WrapWholeWords;
		stackPanel.Children.Add(textBlock4);
		Grid grid5 = new Grid
		{
			ColumnSpacing = 10.0
		};
		grid5.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid5.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid5.Children.Add(item);
		Grid.SetColumn(stackPanel, 1);
		grid5.Children.Add(stackPanel);
		GelButton gelButton = ((alwaysTitle == null) ? null : new GelButton(alwaysTitle, GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, "infinity", 13.0, 34.0));
		GelButton gelButton2 = new GelButton(acceptTitle, ((object)gelButton != null) ? GelAppearance.SecondaryWhite : GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, null, 13.0, 34.0);
		GelButton gelButton3 = new GelButton("Not now", ((object)gelButton == null) ? GelAppearance.SecondaryWhite : GelAppearance.SecondaryOutline, GelButton.Sizing.Normal, 100.0, null, 13.0, 34.0);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			Margin = new Thickness(42.0, 0.0, 0.0, 0.0)
		};
		if ((object)gelButton != null)
		{
			stackPanel2.Children.Add(gelButton);
		}
		stackPanel2.Children.Add(gelButton2);
		stackPanel2.Children.Add(gelButton3);
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 9.0,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(30.0, num2 + 10.0, 30.0, 0.0)
		};
		stackPanel3.Children.Add(grid5);
		stackPanel3.Children.Add(stackPanel2);
		grid2.Children.Add(stackPanel3);
		bool answered = false;
		gelButton2.Click += Accept;
		acceptForTest = Accept;
		if ((object)gelButton != null)
		{
			gelButton.Click += delegate
			{
				if (!answered)
				{
					answered = true;
					Dismiss(animated: false);
					onAlways?.Invoke();
				}
			};
		}
		gelButton3.Click += delegate
		{
			if (!answered)
			{
				answered = true;
				Dismiss();
				onDecline();
			}
		};
		window.Root.Children.Clear();
		window.Root.Children.Add(grid);
		window.ShowNoActivate();
		playChime?.Invoke("agent-needs-you", 0.5);
		SwiftSpring spring = new SwiftSpring(0.42, 0.78);
		grid2.CenterPoint = new Vector3((float)num / 2f, 0f, 0f);
		stackPanel3.Opacity = 0.0;
		if (Anim.ReduceMotion)
		{
			Anim.OpacitySpring(stackPanel3, 1f, spring);
		}
		else
		{
			grid2.Scale = Vector3.One;
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = SpringCurve.Scale(compositor, spring, new Vector3(0.4f, 0.2f, 1f), Vector3.One);
			vector3KeyFrameAnimation.Target = "Scale";
			grid2.StartAnimation(vector3KeyFrameAnimation);
			Anim.OpacitySpring(stackPanel3, 1f, spring);
		}
		DispatcherQueueTimer dispatcherQueueTimer = window.DispatcherQueue.CreateTimer();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(timeoutSeconds);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (!(generation != presentGeneration || answered))
			{
				OverlayWindow? overlayWindow = window;
				if ((object)overlayWindow != null && overlayWindow.IsShown)
				{
					answered = true;
					Dismiss();
					(onTimedOut ?? onDecline)();
				}
			}
		};
		dispatcherQueueTimer.Start();
		timeout = new DispatcherQueueTimerHandle(dispatcherQueueTimer);
		void Accept()
		{
			if (!answered)
			{
				answered = true;
				Dismiss(animated: false);
				onAccept();
			}
		}
	}

	public void Dismiss(bool animated = true)
	{
		timeout?.Stop();
		timeout = null;
		DriveNotchOwnership.SetConsentShown(shown: false);
		if ((object)window == null || !window.IsShown)
		{
			return;
		}
		int dismissGeneration = ++generation;
		OverlayWindow closing = window;
		if (!animated)
		{
			closing.HideWindow();
			closing.Root.Children.Clear();
			return;
		}
		UIElement uIElement = closing.Root.Children.FirstOrDefault();
		if ((object)uIElement != null)
		{
			Anim.Opacity(uIElement, 0f, 0.18);
		}
		DispatcherQueueTimer dispatcherQueueTimer = closing.DispatcherQueue.CreateTimer();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(0.18);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if (generation == dismissGeneration)
			{
				closing.HideWindow();
				closing.Root.Children.Clear();
			}
		};
		dispatcherQueueTimer.Start();
	}
}

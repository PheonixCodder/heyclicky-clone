using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Dock;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class ClickyDrawingLaptopScreen : Grid
{
	public static readonly ClickyHomeSpreadPaper Paper = ClickyHomeSpreadPaper.Of(ClickyPaperStock.Blush);

	private const double TitleBarHeight = 34.0;

	private readonly Border bezel;

	private readonly Border wash = new Border();

	private readonly Border hoverShade;

	private readonly Grid playButton;

	private readonly GelSurface playGel;

	private readonly List<CaptionButton> captionButtons = new List<CaptionButton>();

	private bool isHovered;

	private static readonly Dictionary<string, Color?> TintCache = new Dictionary<string, Color?>(StringComparer.OrdinalIgnoreCase);

	public ClickyDrawingLaptopScreen(StoredLesson stored, Action<bool> play, (string Help, string Label, Action Perform)? remove = null, bool showsCaptionButtons = true, double? outerCornerRadius = null)
	{
		ClickyDrawingLaptopScreen clickyDrawingLaptopScreen = this;
		(double, double) tuple = StickyPreviewLayout.CornerRadii(outerCornerRadius);
		base.Width = 494.0;
		base.Height = 314.0;
		base.Background = Ui.Transparent;
		base.ProtectedCursor = Pressable.ClickableCursor;
		base.Children.Add(new ShadowLayer(tuple.Item2, DS.Black(0.35), 20.0, 10.0));
		bezel = new Border
		{
			CornerRadius = new CornerRadius(tuple.Item2),
			Background = Ui.Vertical((0.0, DS.Rgb(0.16, 0.16, 0.16)), (1.0, DS.Rgb(0.08, 0.08, 0.08))),
			BorderBrush = Ui.Brush(DS.White(0.16)),
			BorderThickness = new Thickness(1.0)
		};
		base.Children.Add(bezel);
		Grid grid = new Grid
		{
			Margin = new Thickness(7.0),
			Width = 480.0,
			Height = 300.0
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(34.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		ClickyEdgeStackInkScreenPaper clickyEdgeStackInkScreenPaper = new ClickyEdgeStackInkScreenPaper(Paper);
		Grid.SetRowSpan(clickyEdgeStackInkScreenPaper, 2);
		grid.Children.Add(clickyEdgeStackInkScreenPaper);
		Grid grid2 = new Grid
		{
			Background = Ui.Transparent
		};
		grid2.Children.Add(new Border
		{
			Background = Ui.Horizontal((0.0, DS.Hex(14079447u)), (0.5, DS.Hex(15790320u)), (1.0, DS.Hex(14079447u)))
		});
		wash.Background = Ui.Brush(Paper.TitleWash.WithAlpha(0.62));
		grid2.Children.Add(wash);
		grid2.Children.Add(new Border
		{
			Height = 0.5,
			VerticalAlignment = VerticalAlignment.Bottom,
			Background = Ui.Brush(DS.Black(0.08))
		});
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top
		};
		if (showsCaptionButtons)
		{
			CaptionButton captionButton = new CaptionButton(CaptionButton.Kind.Maximize, 34.0, Paper.Ink.WithAlpha(0.8), "Play it in a window");
			captionButton.Click += delegate
			{
				play(obj: false);
			};
			stackPanel.Children.Add(captionButton);
			if (remove.HasValue)
			{
				(string, string, Action) valueOrDefault = remove.GetValueOrDefault();
				CaptionButton captionButton2 = new CaptionButton(CaptionButton.Kind.Close, 34.0, Paper.Ink.WithAlpha(0.8), valueOrDefault.Item1);
				captionButton2.Click += valueOrDefault.Item3;
				stackPanel.Children.Add(captionButton2);
			}
		}
		grid2.Children.Add(stackPanel);
		captionButtons.AddRange(stackPanel.Children.OfType<CaptionButton>());
		double num = (double)stackPanel.Children.Count * 46.0;
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, num + (double)((num > 0.0) ? 10 : 15), 0.0)
		};
		if (stored.Data.HasNarration)
		{
			TextBlock textBlock = Icons.Glyph("\ue767", 9.5, Paper.Ink.WithAlpha(0.55));
			textBlock.FontWeight = FontWeights.SemiBold;
			stackPanel2.Children.Add(textBlock);
		}
		TextBlock textBlock2 = Ui.Text(LengthText(stored.Data.Duration), 11.0, FontWeights.SemiBold, Paper.Ink.WithAlpha(0.55));
		Typography.SetNumeralAlignment(textBlock2, FontNumeralAlignment.Tabular);
		stackPanel2.Children.Add(textBlock2);
		grid2.Children.Add(stackPanel2);
		TextBlock textBlock3 = Ui.Text(stored.Data.Title, 11.5, FontWeights.SemiBold, Paper.Ink.WithAlpha(0.85), TextAlignment.Center, 1);
		double num2 = Math.Max(90.0, num + 60.0);
		textBlock3.Margin = new Thickness(num2, 0.0, num2, 0.0);
		textBlock3.VerticalAlignment = VerticalAlignment.Center;
		textBlock3.IsHitTestVisible = false;
		grid2.Children.Add(textBlock3);
		grid.Children.Add(grid2);
		Pressable pressable = new Pressable
		{
			Background = Ui.Transparent
		};
		if (File.Exists(stored.PosterPath))
		{
			pressable.Children.Add(new Image
			{
				Source = new BitmapImage(new Uri(stored.PosterPath))
				{
					DecodePixelWidth = 960
				},
				Stretch = Stretch.UniformToFill
			});
		}
		else
		{
			TextBlock textBlock4 = Icons.Glyph("\ue70f", 34.0, Paper.Deep.WithAlpha(0.5));
			textBlock4.FontWeight = FontWeights.Medium;
			pressable.Children.Add(textBlock4);
		}
		hoverShade = new Border
		{
			Background = Ui.Brush(DS.Black(0.18)),
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		pressable.Children.Add(hoverShade);
		playButton = new Grid
		{
			Width = 62.0,
			Height = 62.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0.88,
			IsHitTestVisible = false
		};
		playButton.Children.Add(new ShadowLayer(31.0, DS.Black(0.35), 8.0, 3.0));
		playGel = new GelSurface(GelAppearance.PrimaryBlue, 31.0);
		playButton.Children.Add(playGel);
		TextBlock textBlock5 = Icons.Glyph("\uf5b0", 22.0, DS.Black(0.82));
		textBlock5.FontWeight = FontWeights.Bold;
		textBlock5.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
		playButton.Children.Add(textBlock5);
		playButton.CenterPoint = new Vector3(31f, 31f, 0f);
		pressable.Children.Add(playButton);
		Ui.ClipRounded(pressable, 0.0);
		pressable.Click += delegate
		{
			play(obj: false);
		};
		Grid.SetRow(pressable, 1);
		grid.Children.Add(pressable);
		Ui.ClipRounded(grid, tuple.Item1);
		base.Children.Add(grid);
		base.PointerEntered += delegate
		{
			clickyDrawingLaptopScreen.SetHovered(hovered: true);
		};
		base.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				clickyDrawingLaptopScreen.SetHovered(hovered: false);
			}
		};
		ToolTipService.SetToolTip(this, "Play “" + stored.Data.Title + "” again in a window");
		AutomationProperties.SetName(this, "Drawing: " + stored.Data.Title);
		base.ContextFlyout = Menu(stored, play, remove);
		LoadTintAsync(stored.PosterPath);
	}

	private void SetHovered(bool hovered)
	{
		if (isHovered != hovered)
		{
			isHovered = hovered;
			CubicBezierEasingFunction easing = Easing.EaseOut(Ui.Compositor);
			bezel.BorderBrush = Ui.Brush(DS.White(hovered ? 0.26 : 0.16));
			Anim.Opacity(hoverShade, hovered ? 1 : 0, 0.16, easing);
			Anim.Opacity(playButton, hovered ? 1f : 0.88f, 0.16, easing);
			Anim.Scale(playButton, (hovered && !Anim.ReduceMotion) ? 1.08f : 1f, 0.16, easing, new Vector3(31f, 31f, 0f));
			playGel.Hovered = hovered;
		}
	}

	private static MenuFlyout Menu(StoredLesson stored, Action<bool> play, (string Help, string Label, Action Perform)? remove)
	{
		MenuFlyout menu = new MenuFlyout();
		Item("Play again", delegate
		{
			play(obj: false);
		});
		Item("Play on a white board", delegate
		{
			play(obj: true);
		});
		Item("Show in Explorer", delegate
		{
			ChatBlockViews.RevealPath(stored.Folder);
		});
		if (remove.HasValue)
		{
			(string, string, Action) valueOrDefault = remove.GetValueOrDefault();
			menu.Items.Add(new MenuFlyoutSeparator());
			Item(valueOrDefault.Item2, valueOrDefault.Item3);
		}
		return menu;
		void Item(string text, Action action)
		{
			MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
			{
				Text = text
			};
			menuFlyoutItem.Click += delegate
			{
				action();
			};
			menu.Items.Add(menuFlyoutItem);
		}
	}

	public static string LengthText(double duration)
	{
		int num = (int)Math.Round(duration, MidpointRounding.AwayFromZero);
		return $"{num / 60}:{num % 60:00}";
	}

	private async Task LoadTintAsync(string posterPath)
	{
		Color? color = await Tint(posterPath);
		if (color.HasValue)
		{
			Color valueOrDefault = color.GetValueOrDefault();
			wash.Background = Ui.Brush(valueOrDefault.WithAlpha(0.62));
		}
	}

	public static async Task<Color?> Tint(string posterPath)
	{
		if (TintCache.TryGetValue(posterPath, out var value))
		{
			return value;
		}
		Color? tint = null;
		try
		{
			if (File.Exists(posterPath))
			{
				byte[] bytes = await File.ReadAllBytesAsync(posterPath);
				tint = await Task.Run(async delegate
				{
					using IRandomAccessStream stream = new MemoryStream(bytes).AsRandomAccessStream();
					BitmapDecoder obj = await BitmapDecoder.CreateAsync(stream);
					BitmapTransform transform = new BitmapTransform
					{
						ScaledWidth = 32u,
						ScaledHeight = 32u,
						InterpolationMode = BitmapInterpolationMode.Linear
					};
					(double, double, double)? tuple = ClickyDrawingTint.DominantColor((await obj.GetPixelDataAsync(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData());
					if (!tuple.HasValue)
					{
						return (Color?)null;
					}
					(double, double, double) valueOrDefault = tuple.GetValueOrDefault();
					var (r, g, b) = ClickyDrawingTint.Pastel(valueOrDefault);
					return Color.FromArgb(byte.MaxValue, r, g, b);
				});
			}
		}
		catch (Exception ex)
		{
			Log.Warn("notch-chat", "drawing tint: " + ex.Message);
		}
		TintCache[posterPath] = tint;
		return tint;
	}
}

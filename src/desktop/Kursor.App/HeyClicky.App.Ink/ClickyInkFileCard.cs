using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkFileCard
{
	public const double CardWidth = 196.0;

	private const double PreviewHeight = 124.0;

	private static readonly Color CardInk = Color.FromArgb(byte.MaxValue, 28, 30, 36);

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkFileEntry entry = ClickyInkFileEntry.Of(block, actions);
		FrameworkElement frameworkElement = Card(block, entry, actions);
		InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(16.0), pencil: true, style.Quiet, null).With(delegate(InkTextView t)
		{
			t.LineLimit = 5;
		});
		inkTextView.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		return ClickyInkObjectKit.Leading(new ClickyInkHStack(14.0, InkVerticalAlignment.Top, frameworkElement, ClickyInkHStack.Flexible(inkTextView)), contentWidth);
	}

	private static FrameworkElement Card(ClickyInkLiveNote.Block block, ClickyInkFileEntry entry, ClickyInkActions actions)
	{
		Grid grid = new Grid
		{
			Width = 182.0,
			Height = 124.0,
			CornerRadius = new CornerRadius(6.0)
		};
		grid.Children.Add(Preview(block, entry));
		Grid grid2 = new Grid
		{
			Width = 182.0,
			Height = 124.0,
			Children = { (UIElement)grid }
		};
		grid2.Children.Add(ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(6.0), new PaperStroke(PaperColor.Black.Opacity(0.22))));
		PaperFont font = ClickyPaperFonts.Typed(12.5, FontWeights.Bold);
		InkTextView item = InkTextView.Plain(ClickyInkMiddleTruncation.Fit(entry.Name, font, 176.0), font, CardInk).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0
		};
		TextBlock textBlock = Icons.Glyph(entry.Style.Glyph, 9.5, entry.Style.Tint);
		textBlock.FontWeight = FontWeights.SemiBold;
		stackPanel.Children.Add(entry.IsWebLink ? WithCorners(ClickyInkObjectKit.Favicon(entry.RawPath, 11.0, textBlock), 2.5) : textBlock);
		stackPanel.Children.Add(InkTextView.Plain(entry.Style.KindLabel + " · Open", ClickyPaperFonts.Typed(10.5, FontWeights.Medium), CardInk.Opacity(0.5)).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		}));
		StackPanel item2 = new StackPanel
		{
			Spacing = 3.0,
			Margin = new Thickness(3.0, 4.0, 3.0, 6.0),
			Children = 
			{
				(UIElement)item,
				(UIElement)stackPanel
			}
		};
		StackPanel content = new StackPanel
		{
			Spacing = 7.0,
			Padding = new Thickness(7.0),
			Width = 196.0,
			Children = 
			{
				(UIElement)grid2,
				(UIElement)item2
			}
		};
		IClickyShape shape = ClickyShape.RoundedRectangle(10.0);
		ClickyInkShadowedShape restingShadow = new ClickyInkShadowedShape(shape, "rr10", PaperColor.White, new PaperShadow(PaperColor.Black.Opacity(0.16), 5.0, 0.0, 3.0));
		ClickyInkShadowedShape hoverShadow = new ClickyInkShadowedShape(shape, "rr10", PaperColor.White, new PaperShadow(PaperColor.Black.Opacity(0.24), 10.0, 0.0, 6.0))
		{
			Opacity = 0.0
		};
		ClickyInkOverlay item3 = new ClickyInkOverlay(content, ClickyShapeView.StrokeBorder(shape, new PaperStroke(PaperColor.Black.Opacity(0.07), 0.5))).Behind(hoverShadow).Behind(restingShadow);
		Pressable button = new Pressable
		{
			Children = { (UIElement)item3 },
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		button.Enabled = entry.RawPath.Length > 0;
		button.Click += delegate
		{
			if (entry.RawPath.Length > 0)
			{
				actions.Open(entry.RawPath);
			}
		};
		ToolTipService.SetToolTip(button, "Open " + entry.Name);
		AutomationProperties.SetName(button, "Open " + entry.Name + ", " + entry.Style.KindLabel);
		ClickyInkObjectKit.FileMenu(button, entry.RawPath, entry.ResolvedPath, entry.IsWebLink, delegate
		{
			actions.Open(entry.RawPath);
		});
		button.Rotation = -1.5f;
		button.SizeChanged += delegate
		{
			button.CenterPoint = new Vector3((float)button.ActualWidth / 2f, (float)button.ActualHeight / 2f, 0f);
		};
		SwiftSpring spring = new SwiftSpring(0.28, 0.8);
		button.HoverChanged += delegate(bool hovering)
		{
			Anim.ScaleSpring(button, hovering ? 1.03f : 1f, spring, null);
			Anim.Rotate(button, hovering ? 0f : (-1.5f), 0.28);
			Anim.Opacity(hoverShadow, hovering ? 1 : 0, 0.28);
			Anim.Opacity(restingShadow, (!hovering) ? 1 : 0, 0.28);
		};
		return button;
	}

	private static FrameworkElement WithCorners(FrameworkElement view, double radius)
	{
		return new Grid
		{
			CornerRadius = new CornerRadius(radius),
			Children = { (UIElement)view }
		};
	}

	private static FrameworkElement Preview(ClickyInkLiveNote.Block block, ClickyInkFileEntry entry)
	{
		if (entry.IsWebLink)
		{
			return ClickyInkLinkPreviewPlate.Make(entry.RawPath, 40.0, null);
		}
		Grid host = new Grid();
		ClickyInkPicture clickyInkPicture = ClickyInkImages.Image(block.Source["thumb"]);
		if ((object)clickyInkPicture != null)
		{
			ShowImage(clickyInkPicture.Color);
			return host;
		}
		ImageSource imageSource = ArtifactThumbnails.Cached(entry.ResolvedPath);
		if ((object)imageSource != null)
		{
			ShowImage(imageSource);
			return host;
		}
		host.Children.Add(new Border
		{
			Background = Ui.Brush(entry.Style.Tint.Opacity(0.14))
		});
		TextBlock textBlock = Icons.Glyph(entry.Style.Glyph, 30.0, entry.Style.Tint);
		textBlock.FontWeight = FontWeights.SemiBold;
		host.Children.Add(textBlock);
		DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();
		string text = block.Source["thumb"];
		if (text != null)
		{
			ClickyInkImages.LoadAsync(text).ContinueWith(delegate(Task<ClickyInkPicture?> t)
			{
				if (t.IsCompletedSuccessfully)
				{
					ClickyInkPicture p = t.Result;
					if ((object)p != null)
					{
						dispatcher.TryEnqueue(delegate
						{
							ShowImage(p.Color);
						});
					}
				}
			}, TaskScheduler.Default);
		}
		else if (entry.ResolvedPath.Length > 0)
		{
			LoadThumbnail(entry.ResolvedPath, (int)Math.Ceiling(364.0), ShowImage);
		}
		return host;
		void ShowImage(ImageSource source)
		{
			host.Children.Clear();
			host.Children.Add(new Image
			{
				Source = source,
				Stretch = Stretch.UniformToFill,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			});
		}
	}

	internal static async Task LoadThumbnail(string path, int pixelWidth, Action<ImageSource> show)
	{
		try
		{
			ImageSource imageSource = await ArtifactThumbnails.GetAsync(path, pixelWidth);
			if ((object)imageSource != null)
			{
				show(imageSource);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "file thumbnail " + Path.GetFileName(path) + ": " + ex.Message);
		}
	}
}

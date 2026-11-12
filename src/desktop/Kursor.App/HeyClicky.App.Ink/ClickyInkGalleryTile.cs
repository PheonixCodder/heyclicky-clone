using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.App.Home;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkGalleryTile
{
	private sealed class State : InkObjectState
	{
		public bool IsHovered;

		public double HoverChangedAt = -1000.0;

		public ImageSource? Thumbnail;

		public string? LoadingPath;

		public bool Subscribed;
	}

	private static readonly Color Ink = PaperColor.Hex("#2B2620");

	public static FrameworkElement Make(ClickyInkLiveNote.Block block, ClickyInkLiveNote.Item item, int index, double width, ClickyInkStyle style, ClickyInkActions actions, bool isPicked, bool canPick, Action onPick)
	{
		State state = InkObjectState.For<State>(block, "gallery-tile|" + item.Id);
		if (!state.Subscribed)
		{
			state.Subscribed = true;
			ClickyInkAudioPlayer.Shared.Changed += state.Changed;
		}
		return new InkObjectHost(state, () => Build(item, index, width, style, actions, isPicked, canPick, onPick, state));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Item item, int index, double width, ClickyInkStyle style, ClickyInkActions actions, bool isPicked, bool canPick, Action onPick, State state)
	{
		string rawPath = item.Source.Url ?? "";
		bool flag = ClickyInkGalleryRules.IsWebLink(rawPath);
		string resolvedPath = (flag ? rawPath : actions.ResolvePath(rawPath));
		bool flag2 = ClickyInkGalleryRules.IsAudio(resolvedPath);
		double tilt = ClickyInkGalleryRules.Tilt(index);
		double num = width - 14.0;
		bool isOut = item.Source.Mark == ClickyInkDocument.Mark.Gone;
		if (!flag && resolvedPath.Length > 0 && !flag2 && state.LoadingPath != resolvedPath)
		{
			state.LoadingPath = resolvedPath;
			state.Thumbnail = ArtifactThumbnails.Cached(resolvedPath);
			string path = resolvedPath;
			int pixels = (int)Math.Ceiling(num * 2.0);
			LoadThumbnail(path, pixels, state);
		}
		Grid grid = new Grid
		{
			Width = num,
			Height = num * 0.78
		};
		grid.Clip = new RectangleGeometry
		{
			Rect = new Rect(0.0, 0.0, num, num * 0.78)
		};
		grid.Children.Add(Preview(rawPath, resolvedPath, flag, flag2, state));
		if (flag2)
		{
			bool flag3 = ClickyInkAudioPlayer.Shared.PlayingPath == resolvedPath;
			FrameworkElement item2 = InkViews.Symbol(flag3 ? "pause.fill" : "play.fill", 16.0, PaperColor.White, bold: true) ?? new Grid();
			Pressable pressable = InkViews.PlainButton(new Grid
			{
				Width = 40.0,
				Height = 40.0,
				Children = 
				{
					(UIElement)new Ellipse
					{
						Fill = Ui.Brush(PaperColor.Black.Opacity(0.55))
					},
					(UIElement)item2
				}
			}, delegate
			{
				if (ClickyInkAudioPlayer.Shared.PlayingPath != resolvedPath)
				{
					actions.Track("gallery_audio_played", ClickyInkDocument.Kind.Gallery, new JsonObject((JsonNodeOptions?)null));
				}
				ClickyInkAudioPlayer.Shared.Toggle(resolvedPath);
			}, flag3 ? "Pause" : "Play");
			pressable.HorizontalAlignment = HorizontalAlignment.Center;
			pressable.VerticalAlignment = VerticalAlignment.Center;
			grid.Children.Add(pressable);
		}
		InkTextView inkTextView = InkViews.Plain((item.Source.Text.Length == 0) ? " " : ClickyInkNoteActions.Plain(item.Source.Text), ClickyPaperFonts.Hand((width < 150.0) ? 13.5 : 15.5), Ink.Opacity(isOut ? 0.4 : 1.0), delegate(InkTextView t)
		{
			t.TextAlignment = TextAlignment.Center;
			t.LineLimit = 2;
			if (isOut)
			{
				t.Strikethrough = Ink.Opacity(0.4);
			}
		});
		inkTextView.HorizontalAlignment = HorizontalAlignment.Center;
		inkTextView.VerticalAlignment = VerticalAlignment.Center;
		Grid grid2 = new Grid
		{
			Width = num,
			Height = 38.0,
			Children = { (UIElement)inkTextView }
		};
		StackPanel stackPanel = InkViews.VStack(0.0, grid, grid2);
		stackPanel.Margin = new Thickness(7.0, 7.0, 7.0, 0.0);
		InkPaperSheet paper = new InkPaperSheet("gallery-polaroid", ClickyShape.Rectangle, PaperFill.Of(PaperColor.Hex("#FEFDF9")), 0.2, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.08), 2.0, 0.0, 1.0)
		});
		InkPaperSheet lifted = new InkPaperSheet("gallery-polaroid-up", ClickyShape.Rectangle, PaperFill.Of(PaperColor.Hex("#FEFDF9")), 0.2, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.15), 5.0, 0.0, 3.0)
		})
		{
			Opacity = 0.0
		};
		List<UIElement> list = new List<UIElement>();
		FrameworkElement frameworkElement = ClickyInkMaskingTape.Make(Math.Min(54.0, width * 0.4), (0.0 - tilt) * 1.6, null);
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Top;
		frameworkElement.Transformed(0.0, -8.0, 0.0, 1.0, null);
		list.Add(frameworkElement);
		if (isPicked)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyWobblyRect(index + 9, 2.2), new PaperStroke(style.Fresh.Opacity(0.7), 2.0, PenLineCap.Round, PenLineJoin.Round));
			clickyShapeView.Margin = new Thickness(-6.0);
			list.Add(clickyShapeView);
		}
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel, list.ToArray()).Behind(lifted).Behind(paper);
		if (isOut)
		{
			clickyInkOverlay.Opacity = 0.7;
			grid.Children.Add(new Border
			{
				Background = Ui.Brush(PaperColor.Hex("#8A8A8A").Opacity(0.55)),
				IsHitTestVisible = false
			});
		}
		RotateTransform rotate = new RotateTransform();
		ScaleTransform scale = new ScaleTransform();
		clickyInkOverlay.RenderTransform = new TransformGroup
		{
			Children = 
			{
				(Transform)scale,
				(Transform)rotate
			}
		};
		clickyInkOverlay.RenderTransformOrigin = new Point(0.5, 0.5);
		ApplyHover();
		Pressable pressable2 = InkViews.PlainButton(clickyInkOverlay, delegate
		{
			actions.Open(rawPath);
		}, ClickyInkNoteActions.Plain(item.Source.Text));
		pressable2.HorizontalAlignment = HorizontalAlignment.Center;
		pressable2.HoverChanged += delegate(bool h)
		{
			state.IsHovered = h;
			state.HoverChangedAt = InkObjectState.Now;
			double until = InkObjectState.Now + InkSpring.SettleSeconds(0.3, 0.75);
			EventHandler<object> tick = null;
			tick = delegate
			{
				ApplyHover();
				if (InkObjectState.Now >= until)
				{
					CompositionTarget.Rendering -= tick;
				}
			};
			CompositionTarget.Rendering += tick;
		};
		if (rawPath.Length == 0)
		{
			pressable2.IsHitTestVisible = false;
		}
		FrameworkElement frameworkElement2 = null;
		if (isPicked)
		{
			frameworkElement2 = ClickyInkRubberStamp.Make("picked", style.Fresh, -3.0);
		}
		else if (canPick)
		{
			frameworkElement2 = new ClickyPaperGelButton("Pick this", onPick, ClickyPaperGel.Blue, 24.0, 11.0, 11.5);
		}
		if ((object)frameworkElement2 != null)
		{
			frameworkElement2.HorizontalAlignment = HorizontalAlignment.Center;
		}
		Grid grid3 = new Grid
		{
			Width = width
		};
		if ((object)frameworkElement2 != null)
		{
			grid3.Children.Add(frameworkElement2);
		}
		StackPanel stackPanel2 = InkViews.VStack(9.0, pressable2, grid3);
		stackPanel2.Width = width;
		return stackPanel2;
		void ApplyHover()
		{
			double num2 = InkSpring.Value(0.3, 0.75, InkObjectState.Now - state.HoverChangedAt);
			double num3 = ((state.HoverChangedAt == -1000.0) ? ((double)(state.IsHovered ? 1 : 0)) : (state.IsHovered ? num2 : (1.0 - num2)));
			rotate.Angle = tilt * (1.0 - num3);
			ScaleTransform scaleTransform = scale;
			double scaleX = (scale.ScaleY = 1.0 + 0.03 * num3);
			scaleTransform.ScaleX = scaleX;
			lifted.Opacity = Math.Clamp(num3, 0.0, 1.0);
			paper.Opacity = 1.0 - Math.Clamp(num3, 0.0, 1.0);
		}
	}

	private static async Task LoadThumbnail(string path, int pixels, State state)
	{
		ImageSource thumbnail = await ArtifactThumbnails.GetAsync(path, pixels);
		if (!(state.LoadingPath != path))
		{
			state.Thumbnail = thumbnail;
			state.Changed();
		}
	}

	private static FrameworkElement Preview(string rawPath, string resolvedPath, bool isWebLink, bool isAudio, State state)
	{
		if (ClickyInkGalleryRules.IsWebImage(rawPath))
		{
			Grid grid = new Grid();
			Grid placeholder = new Grid
			{
				Background = Ui.Brush(PaperColor.Black.Opacity(0.06)),
				Children = { (UIElement)InkViews.Spinner(16.0) }
			};
			((FrameworkElement)placeholder.Children[0]).HorizontalAlignment = HorizontalAlignment.Center;
			grid.Children.Add(placeholder);
			BitmapImage bitmapImage = new BitmapImage(new Uri(rawPath));
			Image image = new Image
			{
				Source = bitmapImage,
				Stretch = Stretch.UniformToFill,
				Opacity = 0.0
			};
			bitmapImage.ImageOpened += delegate
			{
				image.Opacity = 1.0;
				placeholder.Visibility = Visibility.Collapsed;
			};
			grid.Children.Add(image);
			return grid;
		}
		if (isWebLink)
		{
			return ClickyInkLinkPreviewPlate.Make(rawPath, 32.0, null);
		}
		ImageSource thumbnail = state.Thumbnail;
		if ((object)thumbnail != null)
		{
			return new Image
			{
				Source = thumbnail,
				Stretch = Stretch.UniformToFill,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
		}
		NotchDocumentStyle notchDocumentStyle = NotchDocumentStyle.For(resolvedPath);
		FrameworkElement item;
		if (isAudio)
		{
			item = InkViews.Symbol("waveform", 26.0, notchDocumentStyle.Tint, bold: true) ?? new Grid();
		}
		else
		{
			TextBlock textBlock = Icons.Glyph(notchDocumentStyle.Glyph, 26.0, notchDocumentStyle.Tint);
			textBlock.FontWeight = FontWeights.Bold;
			item = textBlock;
		}
		return new Grid
		{
			Background = Ui.Brush(notchDocumentStyle.Tint.Opacity(0.14)),
			Children = { (UIElement)item }
		};
	}
}

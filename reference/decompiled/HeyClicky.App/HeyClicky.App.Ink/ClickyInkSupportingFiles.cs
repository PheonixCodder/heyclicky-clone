using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Ink;

public static class ClickyInkSupportingFiles
{
	private const int CollapsedRowCount = 6;

	private static readonly HashSet<string> ShowingAll = new HashSet<string>();

	private const double ThumbnailSide = 26.0;

	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double inkWidth = ClickyInkEnvironment.Current.ContentWidth;
		Grid host = new Grid
		{
			Width = inkWidth,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		string key = ((blocks.Count > 0) ? blocks[0].Id : "");
		Build();
		return host;
		void Build()
		{
			host.Children.Clear();
			List<ClickyInkLiveNote.Block> list = blocks.Where((ClickyInkLiveNote.Block b) => clock >= b.LandsAt).ToList();
			List<ClickyInkLiveNote.Block> list2 = (ShowingAll.Contains(key) ? list : list.Take(6).ToList());
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 4.0
			};
			InkTextView inkTextView = InkTextView.Plain((list.Count == 1) ? "also made" : $"also made · {list.Count} files", ClickyPaperFonts.Hand(15.0), style.Quiet);
			inkTextView.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
			stackPanel.Children.Add(inkTextView);
			foreach (ClickyInkLiveNote.Block item in list2)
			{
				stackPanel.Children.Add(Row(item, clock, style, actions, inkWidth));
			}
			if (list.Count > list2.Count)
			{
				Pressable pressable = new Pressable
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Margin = new Thickness(34.0, 2.0, 0.0, 0.0),
					Children = { (UIElement)InkTextView.Plain($"{list.Count - list2.Count} more", ClickyPaperFonts.Typed(11.5, FontWeights.SemiBold), style.Fresh) }
				};
				pressable.Click += delegate
				{
					ShowingAll.Add(key);
					Build();
				};
				stackPanel.Children.Add(pressable);
			}
			host.Children.Add(stackPanel);
		}
	}

	private static FrameworkElement Row(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, double width)
	{
		ClickyInkFileEntry entry = ClickyInkFileEntry.Of(block, actions);
		bool flag = block.IsStruck(clock);
		Grid thumbnail = new Grid
		{
			Width = 26.0,
			Height = 26.0,
			CornerRadius = new CornerRadius(4.0)
		};
		if (!entry.IsWebLink)
		{
			ImageSource imageSource = ArtifactThumbnails.Cached(entry.ResolvedPath);
			if ((object)imageSource != null)
			{
				ShowImage(imageSource);
				goto IL_015c;
			}
		}
		thumbnail.Children.Add(new Border
		{
			Background = Ui.Brush(entry.Style.Tint.Opacity(0.14))
		});
		TextBlock textBlock = Icons.Glyph(entry.Style.Glyph, 11.0, entry.Style.Tint);
		textBlock.FontWeight = FontWeights.SemiBold;
		thumbnail.Children.Add(textBlock);
		if (!entry.IsWebLink && entry.ResolvedPath.Length > 0)
		{
			ClickyInkFileCard.LoadThumbnail(entry.ResolvedPath, 52, ShowImage);
		}
		goto IL_015c;
		IL_015c:
		Grid grid = new Grid
		{
			Width = 26.0,
			Height = 26.0,
			Children = { (UIElement)thumbnail }
		};
		grid.Children.Add(ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(4.0), new PaperStroke(PaperColor.Black.Opacity(0.14), 0.5)));
		PaperFont font = ClickyPaperFonts.Typed(12.5, FontWeights.Medium);
		InkTextView inkTextView = InkTextView.Plain(entry.Style.KindLabel, ClickyPaperFonts.Typed(11.0, null), style.Quiet).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
		double width2 = Math.Max(0.0, width - 8.0 - 26.0 - 8.0 - 8.0);
		InkTextView inkTextView2 = InkTextView.Plain(ClickyInkMiddleTruncation.Fit(entry.Name, font, width2), font, flag ? style.Quiet : style.Ink).With(delegate(InkTextView t)
		{
			t.LineLimit = 1;
		});
		if (flag)
		{
			inkTextView2.Strikethrough = style.Fresh;
		}
		ClickyInkHStack item = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, grid, inkTextView2, inkTextView, new ClickyInkSpacer())
		{
			Margin = new Thickness(4.0, 2.0, 4.0, 2.0)
		};
		ClickyShapeView hover = ClickyShapeView.Fill(ClickyShape.RoundedRectangle(6.0), style.Ink.Opacity(0.06));
		hover.Opacity = 0.0;
		Pressable pressable = new Pressable();
		pressable.Children.Add(hover);
		pressable.Children.Add(item);
		pressable.Enabled = entry.RawPath.Length > 0;
		pressable.HoverChanged += delegate(bool hovering)
		{
			hover.Opacity = (hovering ? 1 : 0);
		};
		pressable.Click += delegate
		{
			if (entry.RawPath.Length > 0)
			{
				actions.Open(entry.RawPath);
			}
		};
		ToolTipService.SetToolTip(pressable, "Open " + entry.Name);
		AutomationProperties.SetName(pressable, "Open " + entry.Name + ", " + entry.Style.KindLabel);
		ClickyInkObjectKit.FileMenu(pressable, entry.RawPath, entry.ResolvedPath, entry.IsWebLink, delegate
		{
			actions.Open(entry.RawPath);
		});
		return pressable;
		void ShowImage(ImageSource source)
		{
			thumbnail.Children.Clear();
			thumbnail.Children.Add(new Image
			{
				Source = source,
				Stretch = Stretch.UniformToFill,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			});
		}
	}
}

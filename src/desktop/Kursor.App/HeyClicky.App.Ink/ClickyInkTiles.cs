using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkTiles
{
	public enum Look
	{
		Ruled,
		Ink,
		Tape
	}

	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style, double width, double? tileHeight = null, Look look = Look.Ruled)
	{
		bool isWindowPreview = ClickyInkEnvironment.Current.IsWindowPreview;
		int num = ClickyInkDashboardViewMath.TileColumns(blocks.Count, width, isWindowPreview);
		double num2 = ((blocks.Count == 0) ? 0.0 : blocks.Min((ClickyInkLiveNote.Block b) => b.LandsAt));
		double num3 = ClickyInkClock.Progress(clock, num2, num2 + 0.55);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 0.0,
			Width = Math.Max(0.0, width),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		int num4 = 0;
		int num5 = 0;
		while (num4 < blocks.Count)
		{
			List<ClickyInkLiveNote.Block> list = blocks.Skip(num4).Take(num).ToList();
			double cellWidth = width / (double)list.Count;
			double val = tileHeight ?? list.Max((ClickyInkLiveNote.Block b) => ClickyInkTile.Height(b.Source, cellWidth, isWindowPreview));
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 0.0
			};
			for (int i = 0; i < list.Count; i++)
			{
				FrameworkElement item = ClickyInkTile.Create(list[i], clock, style, num5 * num + i, look, cellWidth);
				Grid grid = new Grid
				{
					Width = Math.Max(0.0, cellWidth),
					Height = Math.Max(0.0, val),
					Children = { (UIElement)item }
				};
				if (i > 0 && look != Look.Tape)
				{
					double num6 = ((look == Look.Ruled) ? (-0.04) : 0.14);
					ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkRuledLine(new Point(0.0, num6), new Point(0.0, 1.0 - num6), num5 * 7 + i).Trim(0.0, num3), new PaperStroke(ClickyInkDashboardPalette.Pencil.Opacity((look == Look.Ruled) ? 0.4 : 0.5), (look == Look.Ruled) ? 1.0 : 1.4, PenLineCap.Round));
					clickyShapeView.Width = 1.0;
					clickyShapeView.HorizontalAlignment = HorizontalAlignment.Left;
					grid.Children.Add(clickyShapeView);
				}
				stackPanel2.Children.Add(grid);
			}
			Grid grid2 = new Grid
			{
				Children = { (UIElement)stackPanel2 }
			};
			if (num5 > 0 && look == Look.Ruled)
			{
				ClickyShapeView clickyShapeView2 = ClickyShapeView.Stroke(new ClickyInkRuledLine(new Point(-0.01, 0.0), new Point(1.01, 0.0), num5).Trim(0.0, num3), new PaperStroke(ClickyInkDashboardPalette.Pencil.Opacity(0.4), 1.0, PenLineCap.Round));
				clickyShapeView2.Height = 1.0;
				clickyShapeView2.VerticalAlignment = VerticalAlignment.Top;
				grid2.Children.Add(clickyShapeView2);
			}
			stackPanel.Children.Add(grid2);
			num4 += num;
			num5++;
		}
		ClickyShapeView clickyShapeView3 = ClickyShapeView.Stroke(new ClickyInkRuledBox((blocks.Count <= 0) ? 1 : blocks[0].Id.CharacterCount(), 5.0).Trim(0.0, (look == Look.Ruled) ? num3 : 0.0), new PaperStroke(ClickyInkDashboardPalette.Pencil.Opacity(0.55), 1.15, PenLineCap.Round));
		return new ClickyInkOverlay(stackPanel, clickyShapeView3)
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(0.0, 4.0, 0.0, 4.0)
		};
	}
}

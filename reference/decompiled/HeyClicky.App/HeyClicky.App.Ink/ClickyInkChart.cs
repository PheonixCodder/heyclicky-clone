using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkChart
{
	public static string ValueWithUnit(string value, string unit)
	{
		return ClickyInkChartRules.ValueWithUnit(value, unit);
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		List<double> list = ClickyInkChartRules.Values(block.Source);
		double num = Math.Max((list.Count == 0) ? 1.0 : ((IEnumerable<double>)list).Max((Func<double, double>)Math.Abs), 0.0001);
		string unit = block.Source["unit"] ?? "";
		double num2 = ClickyInkChartRules.LabelWidth(block.Source);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 7.0
		};
		if (block.Words.Current.Length > 0)
		{
			stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(18.0), pencil: true, null, null));
		}
		ClickyInkLiveNote.Item item;
		double num3;
		bool flag;
		bool flag2;
		InkTextView inkTextView;
		Grid grid;
		double width;
		Grid grid2;
		InkTextView inkTextView2;
		ClickyInkHStack clickyInkHStack;
		for (int i = 0; i < block.Items.Count; flag2 = flag, inkTextView = InkTextView.Plain((item.Source.Cells.Count > 0) ? item.Source.Cells[0] : item.Source.Text, ClickyPaperFonts.Hand(15.0), style.Ink).With(delegate(InkTextView t)
		{
			t.LineLimit = 2;
			t.MinimumScale = 0.85;
			t.TextAlignment = TextAlignment.Right;
		}), inkTextView.HorizontalAlignment = HorizontalAlignment.Right, grid = new Grid
		{
			Width = num2,
			Children = { (UIElement)inkTextView }
		}, width = Math.Max(2.0, (contentWidth - num2 - 70.0) * (Math.Abs((i < list.Count) ? list[i] : 0.0) / num) * num3), grid2 = new Grid
		{
			Width = width,
			Height = 15.0
		}, grid2.Children.Add(ClickyShapeView.Fill(new ClickyInkSketchBar(i), flag2 ? style.Fresh.Opacity(0.75) : style.Ink.Opacity(0.22))), grid2.Children.Add(ClickyShapeView.Stroke(new ClickyInkSketchBar(i), flag2 ? style.Fresh : style.Ink.Opacity(0.5), 1.1)), inkTextView2 = InkTextView.Plain(ValueWithUnit((item.Source.Cells.Count > 1) ? item.Source.Cells[1] : "", unit), ClickyInkObjectFonts.Rounded(11.5, flag2 ? FontWeights.Bold : FontWeights.Medium), flag2 ? style.Fresh : style.Ink.Opacity(0.7)), inkTextView2.Opacity = num3, clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, grid, grid2, inkTextView2), clickyInkHStack.Opacity = ((clock >= item.LandsAt) ? 1 : 0), stackPanel.Children.Add(clickyInkHStack), i++)
		{
			item = block.Items[i];
			num3 = ClickyInkClock.Progress(clock, item.LandsAt, item.LandsAt + 0.45);
			ClickyInkDocument.Mark? mark = item.Source.Mark;
			if (mark.HasValue)
			{
				ClickyInkDocument.Mark valueOrDefault = mark.GetValueOrDefault();
				if ((uint)(valueOrDefault - 3) <= 1u)
				{
					flag = true;
					continue;
				}
			}
			flag = false;
		}
		return ClickyInkObjectKit.Leading(stackPanel, contentWidth);
	}
}

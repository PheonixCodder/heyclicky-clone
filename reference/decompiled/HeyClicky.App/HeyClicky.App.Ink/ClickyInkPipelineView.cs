using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkPipelineView
{
	private sealed class SketchBar(int seed) : IClickyShape
	{
		public PaperPath PathIn(Rect rect)
		{
			ClickyPaperRandom generator = new ClickyPaperRandom((ulong)(seed + 17));
			PaperPath paperPath = new PaperPath();
			double x = rect.X + J();
			paperPath.MoveTo(x, rect.Y + J());
			x = rect.X + rect.Width + J();
			paperPath.LineTo(x, rect.Y + J());
			x = rect.X + rect.Width + J();
			paperPath.LineTo(x, rect.Y + rect.Height + J());
			x = rect.X + J();
			paperPath.LineTo(x, rect.Y + rect.Height + J());
			return paperPath.Close();
			double J()
			{
				return ((double)(generator.Next() % 100) / 100.0 - 0.5) * 1.6;
			}
		}
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width)
	{
		ClickyInkDocument.Block source = block.Source;
		List<ClickyInkPipeline.Stage> list = ClickyInkPipeline.Stages(source["steps"] ?? source["stages"] ?? source["values"]);
		string text = ClickyInkChange.Previous(source, "steps");
		List<ClickyInkPipeline.Stage> source2 = ((text != null) ? ClickyInkPipeline.Stages(text) : new List<ClickyInkPipeline.Stage>());
		List<double> list2 = list.Select((ClickyInkPipeline.Stage s) => ClickyInkNumber.Parse(s.Value)?.Value ?? 0.0).ToList();
		double top = Math.Max((list2.Count == 0) ? 1.0 : list2.Max(), 1.0);
		double num = ((list.Count == 0) ? width : ((width - 26.0 * (double)(list.Count - 1)) / (double)list.Count));
		StackPanel stackPanel = InkDashboardLayout.Width(new StackPanel
		{
			Spacing = 8.0
		}, width);
		if (block.Words.Current.Length > 0)
		{
			stackPanel.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), pencil: true, null, null));
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 0.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		for (int i = 0; i < list.Count; i++)
		{
			ClickyInkPipeline.Stage stage = list[i];
			double num2 = ClickyInkClock.Progress(clock, block.LandsAt + 0.15 + (double)i * 0.18, block.LandsAt + 0.5 + (double)i * 0.18);
			string label = stage.Label.ToLowerInvariant();
			string previous = (from p in source2
				where p.Label.ToLowerInvariant() == label
				select p.Value).FirstOrDefault();
			FrameworkElement item = ClickyInkTickingNumber.Create(source, "steps", clock, style, (num < 80.0) ? 22 : 26, 0.2 + (double)i * 0.15, stage.Value, previous, showsWas: false);
			InkTextView item2 = InkTextView.Plain(stage.Label, ClickyPaperFonts.Typed(9.5, FontWeights.SemiBold), style.Quiet).With(delegate(InkTextView t)
			{
				t.Uppercase = true;
				t.Tracking = 0.5;
				t.LineLimit = 1;
				t.MinimumScale = 0.75;
			});
			double width2 = ClickyInkDashboardViewMath.PipelineBarWidth(num, list2[i], top, num2);
			Grid grid = new Grid
			{
				Width = width2,
				Height = 8.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			};
			grid.Children.Add(ClickyShapeView.Fill(new SketchBar(i + 5), (i == list.Count - 1) ? style.Fresh.Opacity(0.55) : ClickyInkDashboardPalette.Pencil.Opacity(0.2)));
			grid.Children.Add(ClickyShapeView.Stroke(new SketchBar(i + 5), ClickyInkDashboardPalette.Pencil.Opacity(0.55), 0.9));
			StackPanel stackPanel3 = new StackPanel
			{
				Spacing = 2.0,
				Width = Math.Max(0.0, num),
				Children = 
				{
					(UIElement)item,
					(UIElement)item2,
					(UIElement)grid
				}
			};
			stackPanel3.Opacity = ((num2 > 0.0) ? 1 : 0);
			stackPanel2.Children.Add(stackPanel3);
			if (i < list.Count - 1)
			{
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkArrowStroke(i).Trim(0.0, num2), new PaperStroke(ClickyInkDashboardPalette.Pencil.Opacity(0.6), 1.3, PenLineCap.Round, PenLineJoin.Round));
				clickyShapeView.Width = 20.0;
				clickyShapeView.Height = 10.0;
				clickyShapeView.HorizontalAlignment = HorizontalAlignment.Center;
				StackPanel stackPanel4 = new StackPanel
				{
					Spacing = 1.0,
					Width = 26.0,
					Margin = new Thickness(0.0, 10.0, 0.0, 0.0),
					Children = { (UIElement)clickyShapeView }
				};
				string text2 = ClickyInkDashboardViewMath.PipelinePercent(list2[i], list2[i + 1]);
				if (text2 != null)
				{
					TextBlock item3 = ClickyPaperFonts.Rounded(9.0, FontWeights.Medium).Apply(new TextBlock
					{
						Text = text2,
						Foreground = Ui.Brush(style.Quiet),
						IsTextScaleFactorEnabled = false,
						HorizontalAlignment = HorizontalAlignment.Center
					});
					stackPanel4.Children.Add(item3);
				}
				stackPanel2.Children.Add(stackPanel4);
			}
		}
		stackPanel.Children.Add(stackPanel2);
		return stackPanel;
	}
}

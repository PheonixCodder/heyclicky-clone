using System;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkTopOfNote : Panel
{
	private const double LargestScale = 0.95;

	private const double GlanceColumnWidth = 380.0;

	private readonly ClickyHomeInkNote ink;

	private readonly ClickyHomeSpreadPaper paper;

	private readonly ClickyInkActions actions;

	private readonly bool isWorking;

	private readonly bool animates;

	private readonly bool isGlancePreview;

	private readonly ScaleTransform scale = new ScaleTransform();

	private FrameworkElement? sheet;

	private ClickyInkSheetView? sheetView;

	private ClickyInkTimeline? timeline;

	private double columnWidth = double.NaN;

	private double Margins => 44.0;

	public ClickyInkTopOfNote(ClickyHomeInkNote ink, ClickyHomeSpreadPaper paper, ClickyInkActions actions, bool isWorking, bool animates, bool isGlancePreview = false)
	{
		this.ink = ink;
		this.paper = paper;
		this.actions = actions;
		this.isWorking = isWorking;
		this.animates = animates;
		this.isGlancePreview = isGlancePreview;
		base.IsHitTestVisible = true;
	}

	private double ColumnWidthFor(double width)
	{
		if (!isGlancePreview)
		{
			return Math.Max(378.0, width / 0.95 - Margins);
		}
		return 380.0;
	}

	private bool IsWriting(DateTimeOffset now)
	{
		return (now - ink.StartedAt).TotalSeconds < ink.Note.FinishedClock;
	}

	private void Build(double width)
	{
		columnWidth = ColumnWidthFor(width);
		base.Children.Clear();
		timeline = null;
		ClickyInkEnvironment environment = ClickyInkEnvironment.Default with
		{
			ContentWidth = columnWidth,
			IsWindowPreview = true,
			IsGlancePreview = isGlancePreview
		};
		ClickyInkLiveNote note = ink.Note;
		DateTimeOffset startedAt = ink.StartedAt;
		using (ClickyInkEnvironment.Use(environment))
		{
			DateTimeOffset now = DateTimeOffset.Now;
			bool flag = IsWriting(now);
			if (animates && (flag || isWorking) && !Anim.ReduceMotion)
			{
				bool wasWriting = flag;
				double totalSeconds = (now - startedAt).TotalSeconds;
				ClickyHomeSpreadPaper? clickyHomeSpreadPaper = paper;
				ClickyInkActions clickyInkActions = actions;
				bool? flag2 = isWorking;
				sheetView = new ClickyInkSheetView(note, totalSeconds, clickyHomeSpreadPaper, clickyInkActions, isEmbedded: false, null, null, flag2, null, drawsCard: false, delegate(DateTimeOffset at)
				{
					bool stillWriting = IsWriting(at);
					if (stillWriting != wasWriting)
					{
						wasWriting = stillWriting;
						base.DispatcherQueue.TryEnqueue(delegate
						{
							ClickyInkTimeline clickyInkTimeline = timeline;
							if ((object)clickyInkTimeline != null)
							{
								clickyInkTimeline.MinimumInterval = ClickyInkTimeline.Interval(stillWriting ? (1.0 / 30.0) : 1.0);
							}
						});
					}
					return (at - startedAt).TotalSeconds;
				});
				ClickyInkSheetView view = sheetView;
				timeline = new ClickyInkTimeline((DateTimeOffset _) => view, ClickyInkTimeline.Interval(flag ? (1.0 / 30.0) : 1.0));
				sheet = timeline;
			}
			else
			{
				double clock = note.FinishedClock + 30.0;
				ClickyHomeSpreadPaper? clickyHomeSpreadPaper2 = paper;
				ClickyInkActions clickyInkActions2 = actions;
				bool? flag2 = isWorking;
				sheetView = new ClickyInkSheetView(note, clock, clickyHomeSpreadPaper2, clickyInkActions2, isEmbedded: false, null, null, flag2, null, drawsCard: false);
				sheet = sheetView;
			}
		}
		sheet.RenderTransform = scale;
		base.Children.Add(sheet);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		double num = (double.IsInfinity(availableSize.Width) ? (378.0 + Margins) : availableSize.Width);
		if ((object)sheet == null || ColumnWidthFor(num) != columnWidth)
		{
			Build(num);
		}
		double num2 = columnWidth + Margins;
		sheet.Measure(new Size(num2, double.PositiveInfinity));
		double height = (double.IsInfinity(availableSize.Height) ? (sheet.DesiredSize.Height * num / num2) : availableSize.Height);
		return new Size(num, height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if ((object)sheet == null)
		{
			return finalSize;
		}
		double num = columnWidth + Margins;
		double num2 = finalSize.Width / num;
		ScaleTransform scaleTransform = scale;
		double scaleX = (scale.ScaleY = num2);
		scaleTransform.ScaleX = scaleX;
		sheet.Arrange(new Rect(0.0, 0.0, num, sheet.DesiredSize.Height));
		base.Clip = new RectangleGeometry
		{
			Rect = new Rect(0.0, 0.0, finalSize.Width, finalSize.Height)
		};
		return finalSize;
	}
}

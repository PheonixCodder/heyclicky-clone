using System;
using System.Collections.Generic;
using System.Text;
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

public static class ClickyInkSketch
{
	private static readonly Dictionary<string, DateTimeOffset> ShownAt = new Dictionary<string, DateTimeOffset>();

	public static FrameworkElement Make(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style)
	{
		double width = ClickyInkEnvironment.Current.ContentWidth;
		string value;
		string text = (block.Source.Attributes.TryGetValue("sketch", out value) ? value : "");
		ClickyInkSketchModel model = ClickyInkSketchModel.Cached(text);
		double val = InkText.ParseDouble(block.Source["h"]) ?? 160.0;
		double canvasHeight = Math.Min(320.0, Math.Max(50.0, val));
		double scale = width / 380.0;
		string text2 = block.Source.Text.TrimWsNl();
		int seedBase = 0;
		foreach (Rune item in block.Id.EnumerateRunes())
		{
			seedBase = (seedBase * 31 + item.Value) & 0xFFFFFF;
		}
		double step = ClickyInkSketchModel.Step(model.Pieces.Count);
		double totalSeconds = ClickyInkSketchModel.DrawingSeconds(text);
		Color ink = style.Ink;
		string key = block.Id + "\u001f" + text;
		DateTimeOffset shownAt;
		lock (ShownAt)
		{
			if (!ShownAt.TryGetValue(key, out shownAt))
			{
				ShownAt[key] = (shownAt = DateTimeOffset.Now);
			}
			if (ShownAt.Count > 200)
			{
				ShownAt.Clear();
			}
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 4.0,
			Width = width,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		if (text2.Length > 0)
		{
			stackPanel.Children.Add(InkTextView.Plain(text2, ClickyPaperFonts.Typed(9.5, FontWeights.Bold), ink.Opacity(0.45)).With(delegate(InkTextView v)
			{
				v.Tracking = 0.7;
				v.Uppercase = true;
			}));
		}
		ClickyInkTimeline timeline = new ClickyInkTimeline(Drawing, (Anim.ReduceMotion || isDrawnIn() || ClickyHomeRendering.IsOffscreen) ? ((TimeSpan?)null) : new TimeSpan?(TimeSpan.FromSeconds(1.0 / 30.0)));
		if (timeline.MinimumInterval.HasValue)
		{
			DispatcherQueue forCurrentThread = DispatcherQueue.GetForCurrentThread();
			Once.After(Math.Max(0.0, totalSeconds + 0.2 - (DateTimeOffset.Now - shownAt).TotalSeconds), delegate
			{
				timeline.MinimumInterval = null;
			}, forCurrentThread);
		}
		stackPanel.Children.Add(timeline);
		AutomationProperties.SetName(stackPanel, (text2.Length == 0) ? "Sketch" : text2);
		return stackPanel;
		FrameworkElement Drawing(DateTimeOffset now)
		{
			double num = ((Anim.ReduceMotion || isDrawnIn()) ? 1000.0 : Math.Max(clock - block.LandsAt, (now - shownAt).TotalSeconds));
			Canvas canvas = new Canvas
			{
				Width = width,
				Height = canvasHeight * scale,
				IsHitTestVisible = false
			};
			Canvas canvas2 = new Canvas
			{
				RenderTransform = new ScaleTransform
				{
					ScaleX = scale,
					ScaleY = scale
				}
			};
			canvas.Children.Add(canvas2);
			for (int i = 0; i < model.Pieces.Count; i++)
			{
				double num2 = 0.15 + (double)i * step;
				double num3 = Math.Max(0.0, Math.Min(1.0, (num - num2) / 0.45));
				if (!(num3 <= 0.0))
				{
					ClickyInkSketchPainter.Draw(model.Pieces[i], num3, seedBase + i * 7919, ink, canvas2);
				}
			}
			return canvas;
		}
		bool isDrawnIn()
		{
			return (DateTimeOffset.Now - shownAt).TotalSeconds > totalSeconds + 0.2;
		}
	}
}

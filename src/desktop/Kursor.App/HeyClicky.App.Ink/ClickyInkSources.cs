using System.Collections.Generic;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkSources
{
	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 5.0
		};
		if (blocks.Count > 0 && blocks[0].Source["_labelled"] == null)
		{
			stackPanel.Children.Add(InkTextView.Plain("sources", ClickyPaperFonts.Hand(15.0), style.Quiet));
		}
		foreach (ClickyInkLiveNote.Block block in blocks)
		{
			if (clock < block.LandsAt)
			{
				continue;
			}
			bool flag = block.IsStruck(clock);
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.Center, InkTextView.Plain("↗", ClickyPaperFonts.Typed(11.0, FontWeights.Bold), style.Fresh));
			InkTextView inkTextView = ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Typed(12.5, FontWeights.Medium), pencil: false, flag ? style.Quiet : style.Ink, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			});
			if (flag)
			{
				inkTextView.Strikethrough = style.Fresh;
			}
			clickyInkHStack.Children.Add(inkTextView);
			string text = block.Source["url"];
			if (text != null)
			{
				clickyInkHStack.Children.Add(InkTextView.Plain(ClickyInkClipping.Domain(text), ClickyPaperFonts.Typed(11.0, null), style.Quiet).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
			Pressable pressable = new Pressable
			{
				Children = { (UIElement)clickyInkHStack },
				HorizontalAlignment = HorizontalAlignment.Left
			};
			pressable.Click += delegate
			{
				string text2 = block.Source["url"];
				if (text2 != null)
				{
					actions.Open(text2);
				}
			};
			AutomationProperties.SetName(pressable, block.Words.Current);
			stackPanel.Children.Add(pressable);
		}
		return ClickyInkObjectKit.Leading(stackPanel, contentWidth);
	}
}

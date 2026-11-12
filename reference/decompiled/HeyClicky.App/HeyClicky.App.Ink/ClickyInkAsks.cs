using System.Collections.Generic;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Ink;

public static class ClickyInkAsks
{
	public static FrameworkElement Create(List<ClickyInkLiveNote.Block> blocks, double clock, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
		{
			Spacing = 8.0,
			LineSpacing = 8.0
		};
		for (int i = 0; i < blocks.Count; i++)
		{
			ClickyInkLiveNote.Block block = blocks[i];
			if (!(clock < block.LandsAt))
			{
				string say = block.Source["say"] ?? block.Source["ask"] ?? block.Words.Current;
				clickyNotebookFlow.Children.Add(new ClickyPaperGelButton(block.Words.Current, delegate
				{
					actions.Send(say);
				}, (i != 0) ? ClickyPaperGel.White : ClickyPaperGel.Blue, 30.0, 14.0, 12.5));
			}
		}
		return ClickyInkObjectKit.Leading(clickyNotebookFlow, contentWidth);
	}
}

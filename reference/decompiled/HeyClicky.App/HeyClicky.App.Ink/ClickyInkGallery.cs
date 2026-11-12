using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkGallery
{
	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkSentAction clickyInkSentAction = actions.Feedback.AnswerFor(block.Source);
		int num = ClickyInkGalleryRules.Columns(block.Items.Count);
		double num2 = ClickyInkGalleryRules.TileWidth(contentWidth, num);
		StackPanel stackPanel = InkViews.VStack(12.0);
		if (block.Words.Current.Length > 0)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(9.0, InkVerticalAlignment.Center, ClickyInkHStack.Flexible(InkViews.Words(block.Words, clock, style, ClickyPaperFonts.Hand(19.0), null)));
			if ((object)clickyInkSentAction == null && !block.Items.Any((ClickyInkLiveNote.Item i) => i.Source.Mark == ClickyInkDocument.Mark.Pick))
			{
				clickyInkHStack.Children.Add(ClickyInkRubberStamp.Make("pick one", PaperColor.Hex("#2D5BE3")));
			}
			stackPanel.Children.Add(clickyInkHStack);
		}
		Grid grid = new Grid
		{
			ColumnSpacing = 16.0,
			RowSpacing = 20.0,
			Margin = new Thickness(4.0, 6.0, 0.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		for (int j = 0; j < num; j++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(num2)
			});
		}
		for (int k = 0; k < block.Items.Count; k++)
		{
			if (k % num == 0)
			{
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = GridLength.Auto
				});
			}
			ClickyInkLiveNote.Item item = block.Items[k];
			FrameworkElement frameworkElement = ClickyInkGalleryTile.Make(block, item, k, num2, style, actions, item.Source.Mark == ClickyInkDocument.Mark.Pick, (object)clickyInkSentAction == null && item.Source.Mark != ClickyInkDocument.Mark.Gone, delegate
			{
				ClickyInkNoteAction clickyInkNoteAction = ClickyInkNoteActions.Pick(block.Source, ClickyInkGalleryRules.PickCaption(item.Source));
				if ((object)clickyInkNoteAction != null)
				{
					actions.Perform(clickyInkNoteAction);
				}
			});
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			Grid.SetRow(frameworkElement, k / num);
			Grid.SetColumn(frameworkElement, k % num);
			grid.Children.Add(frameworkElement);
		}
		stackPanel.Children.Add(grid);
		stackPanel.Width = contentWidth;
		stackPanel.HorizontalAlignment = HorizontalAlignment.Left;
		return stackPanel;
	}
}

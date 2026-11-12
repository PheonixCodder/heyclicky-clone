using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkQuestion
{
	private static readonly Color Amber = PaperColor.Hex("#8A5300");

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Padding = new Thickness(13.0)
		};
		stackPanel.Children.Add(new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, InkTextView.Plain("needs you", ClickyPaperFonts.Hand(17.0), Amber), ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Typed(14.0, FontWeights.SemiBold), pencil: true, null, null)));
		string text = block.Source["since"] ?? block.Source["asked"];
		if (text != null && text.Length > 0)
		{
			stackPanel.Children.Add(InkTextView.Plain("waiting since " + text, ClickyPaperFonts.Hand(14.0), Amber.Opacity(0.75)));
		}
		if (block.Source["yes"] != null || block.Source["no"] != null)
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0
			};
			string yes = block.Source["yes"];
			if (yes != null)
			{
				stackPanel2.Children.Add(new ClickyPaperGelButton(block.Source["yes_label"] ?? "Yes", delegate
				{
					actions.Send(yes);
				}, ClickyPaperGel.Blue, 28.0, 13.0, 12.5));
			}
			string no = block.Source["no"];
			if (no != null)
			{
				stackPanel2.Children.Add(new ClickyPaperGelButton(block.Source["no_label"] ?? "No", delegate
				{
					actions.Send(no);
				}, ClickyPaperGel.White, 28.0, 13.0, 12.5));
			}
			stackPanel.Children.Add(stackPanel2);
		}
		if (ClickyInkQuestionAnswers.HasAnswers(block.Source))
		{
			stackPanel.Children.Add(ClickyInkQuestionAnswers.Make(block, actions));
		}
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = 
			{
				(UIElement)ClickyShapeView.Fill(ClickyShape.RoundedRectangle(10.0), PaperColor.Hex("#FFE9B0").Opacity(0.7)),
				(UIElement)stackPanel,
				(UIElement)ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(10.0), new PaperStroke(PaperColor.Hex("#E0A030").Opacity(0.6)))
			}
		};
	}
}

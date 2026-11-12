using System;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Notch.Peek;

public sealed class ChatDrawingRow : StackPanel
{
	public ChatDrawingRow(INotchHost host, ChatHandOff entry)
	{
		MessagesPalette current = MessagesPalette.Current;
		base.Spacing = 6.0;
		StoredLesson stored = NotchChat.Lessons?.Store.List().FirstOrDefault((StoredLesson l) => l.Data.Id == entry.LessonId);
		if (stored != null)
		{
			ClickyHomeSpreadPaper paper = ClickyDrawingLaptopScreen.Paper;
			Grid grid = new Grid
			{
				ColumnSpacing = 8.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			Border border = new Border
			{
				Width = 22.0,
				Height = 22.0,
				CornerRadius = new CornerRadius(Math.Round(5.720000000000001)),
				Background = Ui.Brush(paper.TitleWash)
			};
			TextBlock textBlock = Icons.Glyph("\ue70f", 10.5, paper.Deep);
			textBlock.FontWeight = FontWeights.SemiBold;
			border.Child = textBlock;
			grid.Children.Add(border);
			TextBlock textBlock2 = new TextBlock
			{
				FontSize = 11.5,
				FontWeight = FontWeights.Medium,
				FontFamily = DS.Fonts.Small,
				IsTextScaleFactorEnabled = false,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center,
				MaxLines = 1
			};
			textBlock2.Inlines.Add(new Run
			{
				Text = "Drew ",
				Foreground = Ui.Brush(current.SecondaryText)
			});
			textBlock2.Inlines.Add(new Run
			{
				Text = stored.Data.Title,
				Foreground = Ui.Brush(current.PrimaryText),
				FontWeight = FontWeights.Bold
			});
			textBlock2.Inlines.Add(new Run
			{
				Text = "  ·  " + ChatBubbles.ShortTime(entry.CreatedAt),
				Foreground = Ui.Brush(current.TertiaryText)
			});
			Grid.SetColumn(textBlock2, 1);
			grid.Children.Add(textBlock2);
			base.Children.Add(grid);
			double num = 0.19433198380566802;
			ClickyDrawingLaptopScreen child = new ClickyDrawingLaptopScreen(stored, (Action<bool>)Play, ((string Help, string Label, Action Perform)?)("Remove from the chat (it stays in Drawings)", "Remove from chat", RemoveFromChat), showsCaptionButtons: false, (double?)(6.0 / num));
			base.Children.Add(new Viewbox
			{
				Width = 96.0,
				Height = ChatRowLayout.CardHeight,
				Stretch = Stretch.Uniform,
				Child = child,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(30.0, 0.0, 0.0, 0.0)
			});
		}
		void Play(bool onWhiteBoard)
		{
			NotchPeekSurface.PeekAction("drawing_played");
			(host.Actions as INotchActions)?.PlayDrawing(stored.Data.Id, onWhiteBoard);
		}
		void RemoveFromChat()
		{
			NotchPeekSurface.PeekAction("chat_drawing_removed");
			NotchChat.HandOffs?.Remove(entry.Id);
		}
	}
}

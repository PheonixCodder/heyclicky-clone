using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkFaceView : Grid
{
	public ClickyInkFaceView(ClickyInkLiveNote note, double clock, IReadOnlyList<ClickyInkActivity>? activity = null, bool isWorking = true, ClickyHomeSpreadPaper? paper = null)
	{
		ClickyHomeSpreadPaper paper2 = paper ?? ClickyHomeSpreadPaper.Note;
		ClickyInkStyle clickyInkStyle = new ClickyInkStyle(paper2.Ink, paper2.Quiet, paper2.Deep, note.LatestEdit(clock));
		base.Width = 330.0;
		base.Height = 210.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		base.Children.Add(new PaperSurface(ClickyShape.RoundedRectangle(10.0), paper2.Fill, 0.45).WithWash(PaperFill.Vertical((0.0, PaperColor.Clear), (1.0, PaperColor.Black.Opacity(0.06))), (Size s) => new Rect(0.0, Math.Max(0.0, s.Height - 10.0), s.Width, Math.Min(10.0, s.Height))).WithShadow(PaperColor.Black.Opacity(0.3), 12.0, 0.0, 5.0));
		Grid grid = new Grid
		{
			Padding = new Thickness(16.0)
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		bool flag = false;
		ClickyInkLiveNote.Words title = note.Title;
		if ((object)title != null)
		{
			grid.Children.Add(ClickyInkWordsView.Make(title, clock, clickyInkStyle, ClickyPaperFonts.Hand(23.0), pencil: true, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
			flag = true;
		}
		ClickyInkLiveNote.Block block = Lead(note, clock)?.Resolved(clock);
		if (block != null)
		{
			FrameworkElement frameworkElement = LeadView(block, clock, clickyInkStyle);
			if (flag)
			{
				frameworkElement.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
			}
			Grid.SetRow(frameworkElement, 1);
			grid.Children.Add(frameworkElement);
			flag = true;
		}
		if (flag)
		{
			grid.RowDefinitions[2].MinHeight = 8.0;
		}
		if (isWorking)
		{
			ClickyInkActivity clickyInkActivity = activity?.LastOrDefault((ClickyInkActivity a) => a.At <= clock);
			if ((object)clickyInkActivity != null)
			{
				ClickyNotebookWritingPencil content = new ClickyNotebookWritingPencil(paper2).Transformed(0.0, 0.0, 0.0, 0.7, new Point(0.0, 0.5));
				ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.Center, new ClickyInkCenteredOverflow(content, 18.0), ClickyInkHStack.Flexible(InkTextView.Plain(clickyInkActivity.Text, ClickyPaperFonts.Hand(14.5), clickyInkStyle.Fresh).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				})))
				{
					Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
					HorizontalAlignment = HorizontalAlignment.Left
				};
				Grid.SetRow(clickyInkHStack, 3);
				grid.Children.Add(clickyInkHStack);
			}
		}
		base.Children.Add(grid);
	}

	private static ClickyInkLiveNote.Block? Lead(ClickyInkLiveNote note, double clock)
	{
		List<ClickyInkLiveNote.Block> source = ((note.Pages.Count > 0) ? note.Pages[0].Blocks.Where((ClickyInkLiveNote.Block b) => b.IsPresent(clock) && !b.IsStruck(clock)).ToList() : new List<ClickyInkLiveNote.Block>());
		return source.FirstOrDefault((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Answer) ?? source.FirstOrDefault(delegate(ClickyInkLiveNote.Block b)
		{
			ClickyInkDocument.Kind kind = b.Source.Kind;
			return (kind == ClickyInkDocument.Kind.Paragraph || (uint)(kind - 4) <= 1u) ? true : false;
		});
	}

	private static FrameworkElement LeadView(ClickyInkLiveNote.Block lead, double clock, ClickyInkStyle style)
	{
		if (lead.Source.Kind == ClickyInkDocument.Kind.Answer)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(10.0, InkVerticalAlignment.Center);
			string text = lead.Source["big"];
			if (text != null && text.Length > 0)
			{
				InkTextView inkTextView = InkTextView.Plain(text, ClickyPaperFonts.BigNumber(26.0), style.Ink).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.MinimumScale = 0.6;
				});
				inkTextView.Margin = new Thickness(6.0, 0.0, 6.0, 0.0);
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(3), style.Fresh.Opacity(0.75), 1.8);
				clickyShapeView.Margin = new Thickness(-3.0);
				clickyInkHStack.Children.Add(new ClickyInkOverlay(inkTextView, clickyShapeView));
			}
			clickyInkHStack.Children.Add(ClickyInkHStack.Flexible(ClickyInkWordsView.Make(lead.Words, clock, style, ClickyPaperFonts.Hand(16.0), pencil: false, null, null).With(delegate(InkTextView t)
			{
				t.LineLimit = 3;
			})));
			clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
			return clickyInkHStack;
		}
		if (lead.Items.Count > 0)
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 3.0
			};
			{
				foreach (ClickyInkLiveNote.Item item in lead.Items.Take(3))
				{
					bool valueOrDefault = item.Source.IsChecked == true;
					FrameworkElement frameworkElement = (FrameworkElement)(((object)ClickyInkIcon.Glyph(valueOrDefault ? "checkmark.square" : "square", 11.0, valueOrDefault ? ClickyPaperInk.DoneGreen : style.Quiet)) ?? ((object)new Grid()));
					ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(6.0, InkVerticalAlignment.Center, frameworkElement, ClickyInkHStack.Flexible(ClickyInkWordsView.Make(item.Words, clock, style, ClickyPaperFonts.Hand(15.0), pencil: false, null, null).With(delegate(InkTextView t)
					{
						t.LineLimit = 1;
					})));
					clickyInkHStack2.HorizontalAlignment = HorizontalAlignment.Left;
					stackPanel.Children.Add(clickyInkHStack2);
				}
				return stackPanel;
			}
		}
		return ClickyInkWordsView.Make(lead.Words, clock, style, ClickyPaperFonts.Hand(16.0), pencil: false, null, null).With(delegate(InkTextView t)
		{
			t.LineLimit = 3;
		});
	}
}

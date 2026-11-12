using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkCriteria
{
	private sealed class State : InkObjectState
	{
		public bool IsEditing;

		public double EditingChangedAt = -1000.0;

		public readonly Dictionary<string, string> Edits = new Dictionary<string, string>();
	}

	private static readonly Color Ink = PaperColor.Hex("#33291A");

	private static readonly Color StampBlue = PaperColor.Hex("#2F4FA0");

	public static void SetEditingForGallery(ClickyInkLiveNote.Block block)
	{
		State state = InkObjectState.For<State>(block, "criteria");
		StartEditing(block, state);
		state.EditingChangedAt = -1000.0;
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		State state = InkObjectState.For<State>(block, "criteria");
		return new InkObjectHost(state, () => Build(block, actions, state));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Block block, ClickyInkActions actions, State state)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		InkTextView e = InkViews.Plain((block.Words.Current.Length == 0) ? "What I look for" : ClickyInkNoteActions.Plain(block.Words.Current), InkObjectFonts.Mono(14.0, FontWeights.Bold), Ink);
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.FirstBaseline, ClickyInkHStack.Flexible(e), new ClickyInkSpacer());
		if (!state.IsEditing)
		{
			InkTextView content = InkViews.Plain("edit", InkObjectFonts.Mono(11.5, FontWeights.SemiBold), StampBlue);
			Border border = new Border
			{
				Height = 1.0,
				Background = Ui.Brush(StampBlue),
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0.0, 0.0, 0.0, 1.0)
			};
			clickyInkHStack.Children.Add(InkViews.PlainButton(new ClickyInkOverlay(content, border), delegate
			{
				StartEditing(block, state);
				actions.Track("criteria_edit_started", ClickyInkDocument.Kind.Criteria, new JsonObject((JsonNodeOptions?)null) { ["fields"] = block.Items.Count });
				state.Changed();
				state.Animate(0.2);
			}, "edit"));
		}
		double opacity = ClickyInkClock.EaseOut(ClickyInkClock.Progress(InkObjectState.Now, state.EditingChangedAt, state.EditingChangedAt + 0.2));
		StackPanel stackPanel = InkViews.VStack(10.0, clickyInkHStack, new Border
		{
			Height = 1.2,
			Background = Ui.Brush(PaperColor.Hex("#D9453B").Opacity(0.4))
		});
		for (int i = 0; i < block.Items.Count; i++)
		{
			ClickyInkLiveNote.Item item = block.Items[i];
			InkTextView item2 = InkViews.Plain(ClickyInkNoteActions.Plain(item.Source.Text).ToUpperInvariant() + ":", InkObjectFonts.Mono(10.5, FontWeights.Bold), Ink.Opacity(0.55));
			Grid grid = new Grid
			{
				Width = 96.0,
				Children = { (UIElement)item2 }
			};
			FrameworkElement frameworkElement;
			if (state.IsEditing)
			{
				ClickyInkBlankLine clickyInkBlankLine2 = state.Field(item.Id, delegate
				{
					ClickyInkBlankLine clickyInkBlankLine = new ClickyInkBlankLine(item.Source.Text, Ink, InkObjectFonts.Mono(12.5, null));
					clickyInkBlankLine.TextChanged += delegate(string text)
					{
						state.Edits[item.Id] = text;
					};
					return clickyInkBlankLine;
				});
				string valueOrDefault = state.Edits.GetValueOrDefault(item.Id, "");
				if (clickyInkBlankLine2.Text != valueOrDefault)
				{
					clickyInkBlankLine2.Text = valueOrDefault;
				}
				frameworkElement = clickyInkBlankLine2;
				frameworkElement.Opacity = opacity;
			}
			else
			{
				ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
				{
					Spacing = 7.0,
					LineSpacing = 7.0
				};
				List<string> list = ClickyInkCriteriaRules.Values(item.Source.Detail);
				for (int j = 0; j < list.Count; j++)
				{
					InkTextView inkTextView = InkViews.Plain(list[j], InkObjectFonts.Mono(11.5, FontWeights.SemiBold), StampBlue.Opacity(0.75));
					inkTextView.Margin = new Thickness(6.0, 2.0, 6.0, 2.0);
					ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(inkTextView, ClickyShapeView.Stroke(ClickyShape.RoundedRectangle(2.5), new PaperStroke(StampBlue.Opacity(0.4), 1.1)));
					clickyInkOverlay.Transformed(0.0, 0.0, (double)((i * 3 + j) % 5) * 0.35 - 0.7, 1.0, null);
					clickyNotebookFlow.Children.Add(clickyInkOverlay);
				}
				frameworkElement = clickyNotebookFlow;
			}
			ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(10.0, state.IsEditing ? InkVerticalAlignment.Center : InkVerticalAlignment.FirstBaseline, grid);
			InkViews.Hold(clickyInkHStack2, ClickyInkHStack.Flexible(frameworkElement));
			stackPanel.Children.Add(clickyInkHStack2);
		}
		if (state.IsEditing)
		{
			StackPanel item3 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0,
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
				Opacity = opacity,
				Children = 
				{
					(UIElement)new ClickyPaperGelButton("Save changes", delegate
					{
						Save(block, actions, state);
					}, ClickyPaperGel.Blue, 27.0, 13.0, 12.5),
					(UIElement)new ClickyPaperGelButton("Cancel", delegate
					{
						actions.Track("criteria_edit_cancelled", ClickyInkDocument.Kind.Criteria, new JsonObject((JsonNodeOptions?)null));
						state.IsEditing = false;
						state.EditingChangedAt = InkObjectState.Now;
						state.Changed();
						state.Animate(0.2);
					}, ClickyPaperGel.White, 27.0, 13.0, 12.5)
				}
			};
			stackPanel.Children.Add(item3);
		}
		stackPanel.Margin = new Thickness(18.0, 15.0, 18.0, 30.0);
		InkPaperSheet background = new InkPaperSheet("criteria-card", ClickyShape.RoundedRectangle(5.0), PaperFill.Vertical((0.0, PaperColor.Hex("#FDF9EF")), (1.0, PaperColor.Hex("#F9F1DF"))), 0.25, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.07), 2.0, 0.0, 1.0)
		});
		Grid grid2 = new Grid
		{
			Width = 11.0,
			Height = 11.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 0.0, 9.0),
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)new Ellipse
				{
					Fill = Ui.Brush(PaperColor.Hex("#6B5634").Opacity(0.2))
				},
				(UIElement)new Ellipse
				{
					Stroke = Ui.Brush(PaperColor.Hex("#A88D5C").Opacity(0.6)),
					StrokeThickness = 0.8
				}
			}
		};
		ClickyInkOverlay clickyInkOverlay2 = new ClickyInkOverlay(stackPanel, grid2).Behind(background);
		clickyInkOverlay2.Width = contentWidth - 10.0;
		clickyInkOverlay2.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay2.Transformed(0.0, 0.0, 0.2, 1.0, null);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(4.0, 0.0, 0.0, 0.0),
			Children = { (UIElement)clickyInkOverlay2 }
		};
	}

	private static void StartEditing(ClickyInkLiveNote.Block block, State state)
	{
		state.Edits.Clear();
		foreach (ClickyInkLiveNote.Item item in block.Items)
		{
			state.Edits[item.Id] = ClickyInkNoteActions.Plain(item.Source.Detail ?? "");
		}
		state.IsEditing = true;
		state.EditingChangedAt = InkObjectState.Now;
	}

	private static void Save(ClickyInkLiveNote.Block block, ClickyInkActions actions, State state)
	{
		List<(string, string, string)> changes = block.Items.Select((ClickyInkLiveNote.Item item) => (Text: item.Source.Text, item.Source.Detail ?? "", state.Edits.GetValueOrDefault(item.Id, ""))).ToList();
		ClickyInkNoteAction clickyInkNoteAction = ClickyInkNoteActions.Criteria(block.Source, changes);
		if ((object)clickyInkNoteAction != null)
		{
			actions.Perform(clickyInkNoteAction);
		}
		else
		{
			actions.Track("criteria_saved_unchanged", ClickyInkDocument.Kind.Criteria, new JsonObject((JsonNodeOptions?)null));
		}
		state.IsEditing = false;
		state.EditingChangedAt = InkObjectState.Now;
		state.Changed();
		state.Animate(0.2);
	}
}

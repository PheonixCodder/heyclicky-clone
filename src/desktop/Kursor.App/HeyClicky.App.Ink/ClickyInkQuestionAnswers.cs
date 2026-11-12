using System.Collections.Generic;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkQuestionAnswers
{
	private sealed class State : InkObjectState
	{
	}

	public static bool HasAnswers(ClickyInkDocument.Block block)
	{
		return ClickyInkQuestionAnswerRules.HasAnswers(block);
	}

	public static FrameworkElement Make(ClickyInkLiveNote.Block block, ClickyInkActions actions)
	{
		State state = InkObjectState.For<State>(block, "question-answers");
		ClickyInkDocument.Block source = block.Source;
		StackPanel stackPanel = InkViews.VStack(8.0);
		List<string> list = ClickyInkQuestionAnswerRules.Options(source);
		if (list.Count > 0)
		{
			ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
			{
				Spacing = 8.0,
				LineSpacing = 8.0
			};
			for (int i = 0; i < list.Count; i++)
			{
				string option = list[i];
				clickyNotebookFlow.Children.Add(new ClickyPaperGelButton(option, delegate
				{
					ClickyInkNoteAction clickyInkNoteAction = ClickyInkNoteActions.Answer(source, option);
					if ((object)clickyInkNoteAction != null)
					{
						actions.Perform(clickyInkNoteAction);
					}
				}, (i != 0) ? ClickyPaperGel.White : ClickyPaperGel.Blue, 28.0, 13.0, 12.5));
			}
			stackPanel.Children.Add(clickyNotebookFlow);
		}
		string placeholder = ClickyInkQuestionAnswerRules.TypedPlaceholder(source);
		if (placeholder != null)
		{
			ClickyInkInlineAnswerField clickyInkInlineAnswerField = state.Field("typed", () => new ClickyInkInlineAnswerField(placeholder));
			clickyInkInlineAnswerField.OnSend = delegate(string words)
			{
				ClickyInkNoteAction clickyInkNoteAction2 = ClickyInkNoteActions.Answer(source, words, wasTyped: true);
				if ((object)clickyInkNoteAction2 != null)
				{
					actions.Perform(clickyInkNoteAction2);
				}
			};
			InkViews.Hold(stackPanel, clickyInkInlineAnswerField);
		}
		string text = source["file"];
		if (text != null)
		{
			ClickyPaperGelButton button = null;
			button = new ClickyPaperGelButton((text.Length == 0) ? "Choose file…" : text, async delegate
			{
				string text2 = await ClickyInkFilePicker.ChooseAsync(button, ClickyInkNoteActions.Plain(source.Text));
				if (text2 != null)
				{
					ClickyInkNoteAction clickyInkNoteAction3 = ClickyInkNoteActions.Answer(source, text2, wasTyped: true);
					if ((object)clickyInkNoteAction3 != null)
					{
						actions.Perform(clickyInkNoteAction3.ForAnalytics("question_answered", "file"));
					}
				}
			}, ClickyPaperGel.White, 28.0, 13.0, 12.5, "paperclip");
			stackPanel.Children.Add(button);
		}
		return stackPanel;
	}
}

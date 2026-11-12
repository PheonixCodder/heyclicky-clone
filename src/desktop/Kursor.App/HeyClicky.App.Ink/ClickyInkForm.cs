using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkForm
{
	private sealed class State : InkObjectState
	{
		public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

		public readonly Dictionary<string, (string Previous, double At)> ChoiceChanges = new Dictionary<string, (string, double)>();

		public ClickyPaperGelButton? Send;

		public bool HasAnything => Values.Values.Any((string v) => v.TrimWs().Length > 0);

		public void RefreshSend()
		{
			if ((object)Send != null)
			{
				Send.Enabled = HasAnything;
				Send.Opacity = (HasAnything ? 1.0 : 0.5);
			}
		}
	}

	private static readonly Color Ink = PaperColor.Hex("#2E2A24");

	public static void SetForGallery(ClickyInkLiveNote.Block block, IReadOnlyDictionary<string, string> valuesByLabel)
	{
		State state = InkObjectState.For<State>(block, "form");
		foreach (ClickyInkLiveNote.Item item in block.Items)
		{
			if (valuesByLabel.TryGetValue(ClickyInkNoteActions.Plain(item.Source.Text), out string value))
			{
				state.Values[item.Id] = value;
			}
		}
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions)
	{
		State state = InkObjectState.For<State>(block, "form");
		return new InkObjectHost(state, () => Build(block, clock, style, actions, state));
	}

	private static FrameworkElement Build(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, ClickyInkActions actions, State state)
	{
		double contentWidth = ClickyInkEnvironment.Current.ContentWidth;
		ClickyInkDocument.Block source = block.Source;
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(9.0, InkVerticalAlignment.Center, ClickyInkRubberStamp.Make("needs you", null, -5.0));
		if (block.Words.Current.Length > 0)
		{
			clickyInkHStack.Children.Add(InkViews.Words(block.Words, clock, style, ClickyPaperFonts.Typed(14.5, FontWeights.SemiBold), Ink));
		}
		StackPanel stackPanel = InkViews.VStack(13.0, clickyInkHStack);
		foreach (ClickyInkLiveNote.Item item4 in block.Items)
		{
			stackPanel.Children.Add(Field(item4, style, state));
		}
		ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(ClickyInkNoteActions.Plain(source["send_label"] ?? "Send"), delegate
		{
			Submit(usedDefaults: false);
		}, ClickyPaperGel.Blue, 28.0, 13.0, 12.5);
		state.Send = clickyPaperGelButton;
		state.RefreshSend();
		ClickyPaperGelButton item2 = new ClickyPaperGelButton(ClickyInkNoteActions.Plain(source["defaults_label"] ?? "Use your best guess"), delegate
		{
			Submit(usedDefaults: true);
		}, ClickyPaperGel.White, 28.0, 13.0, 12.5);
		StackPanel item3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0),
			Children = 
			{
				(UIElement)clickyPaperGelButton,
				(UIElement)item2
			}
		};
		stackPanel.Children.Add(item3);
		stackPanel.Margin = new Thickness(18.0, 26.0, 18.0, 16.0);
		InkPaperSheet background = new InkPaperSheet("form-sheet", ClickyShape.Rectangle, PaperFill.Of(PaperColor.Hex("#FFFDF8")), 0.2, new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.1), 3.0, 0.0, 1.5)
		});
		FrameworkElement frameworkElement = Clip();
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Top;
		frameworkElement.Transformed(0.0, -8.0, 0.0, 1.0, null);
		ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(stackPanel, frameworkElement).Behind(background);
		clickyInkOverlay.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		clickyInkOverlay.Width = contentWidth - 6.0;
		clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
		clickyInkOverlay.Transformed(0.0, 0.0, -0.2, 1.0, null);
		return new Grid
		{
			Width = contentWidth,
			HorizontalAlignment = HorizontalAlignment.Left,
			Padding = new Thickness(0.0, 6.0, 0.0, 0.0),
			Children = { (UIElement)clickyInkOverlay }
		};
		void Submit(bool usedDefaults)
		{
			List<(string, string)> answers = block.Items.Select((ClickyInkLiveNote.Item item) => (Text: item.Source.Text, state.Values.GetValueOrDefault(item.Id, ""))).ToList();
			ClickyInkNoteAction clickyInkNoteAction = ClickyInkNoteActions.Form(source, answers, usedDefaults);
			if ((object)clickyInkNoteAction != null)
			{
				actions.Perform(clickyInkNoteAction);
			}
		}
	}

	private static FrameworkElement Clip()
	{
		IClickyShape shape = ClickyShape.RoundedRectangle(5.0);
		PaperFill fill = PaperFill.Vertical((0.0, PaperColor.Hex("#F2F3F6")), (1.0, PaperColor.Hex("#A6ABB5")));
		PaperShadow[] shadows = new PaperShadow[1]
		{
			new PaperShadow(PaperColor.Black.Opacity(0.12), 1.0, 0.0, 0.8)
		};
		PaperStroke outline = new PaperStroke(PaperColor.Black.Opacity(0.18), 0.8);
		InkPaperSheet item = new InkPaperSheet("form-clip", shape, fill, null, shadows, outline)
		{
			Width = 68.0,
			Height = 17.0
		};
		Border border = new Border
		{
			Width = 24.0,
			Height = 5.0,
			CornerRadius = new CornerRadius(2.5),
			Background = Ui.Brush(PaperColor.Black.Opacity(0.22)),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		border.Transformed(0.0, -2.0, 0.0, 1.0, null);
		Grid obj = new Grid
		{
			Width = 68.0,
			Height = 17.0,
			IsHitTestVisible = false,
			Children = 
			{
				(UIElement)item,
				(UIElement)border
			}
		};
		AutomationProperties.SetAccessibilityView(obj, AccessibilityView.Raw);
		return obj;
	}

	private static FrameworkElement Field(ClickyInkLiveNote.Item item, ClickyInkStyle style, State state)
	{
		string label = ClickyInkNoteActions.Plain(item.Source.Text);
		string hint = item.Source.Detail ?? "";
		List<string> list = ClickyInkChoices.Of(hint);
		StackPanel stackPanel = InkViews.VStack(5.0, ClickyInkRubberStamp.TypedLabel(label, Ink.Opacity(0.55)));
		switch (ClickyInkFormRules.Kind(hint))
		{
		case ClickyInkFormRules.FieldKind.Choices:
		{
			ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
			{
				Spacing = 14.0,
				LineSpacing = 6.0
			};
			for (int i = 0; i < list.Count; i++)
			{
				clickyNotebookFlow.Children.Add(Choice(item, list[i], i, style, state));
			}
			stackPanel.Children.Add(clickyNotebookFlow);
			break;
		}
		case ClickyInkFormRules.FieldKind.File:
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center);
			ClickyPaperGelButton choose = null;
			choose = new ClickyPaperGelButton("Choose " + hint.Lowered() + "…", async delegate
			{
				string text2 = await ClickyInkFilePicker.ChooseAsync(choose, label, hint.Lowered() == "folder");
				if (text2 != null)
				{
					state.Values[item.Id] = text2;
					state.RefreshSend();
					state.Changed();
				}
			}, ClickyPaperGel.White, 25.0, 11.0, 12.0, "paperclip");
			clickyInkHStack.Children.Add(choose);
			string valueOrDefault = state.Values.GetValueOrDefault(item.Id);
			if (valueOrDefault != null && valueOrDefault.Length > 0)
			{
				clickyInkHStack.Children.Add(ClickyInkHStack.Flexible(new InkMiddleTruncatedText(System.IO.Path.GetFileName(valueOrDefault.TrimEnd('\\', '/')), InkObjectFonts.Mono(12.0, null), Ink)));
			}
			stackPanel.Children.Add(clickyInkHStack);
			break;
		}
		default:
		{
			ClickyInkBlankLine element = state.Field(item.Id, delegate
			{
				ClickyInkBlankLine clickyInkBlankLine = new ClickyInkBlankLine(hint, Ink);
				clickyInkBlankLine.Text = state.Values.GetValueOrDefault(item.Id, "");
				clickyInkBlankLine.TextChanged += delegate(string text)
				{
					state.Values[item.Id] = text;
					state.RefreshSend();
				};
				return clickyInkBlankLine;
			});
			InkViews.Hold(stackPanel, element);
			break;
		}
		}
		return stackPanel;
	}

	private static FrameworkElement Choice(ClickyInkLiveNote.Item item, string choice, int index, ClickyInkStyle style, State state)
	{
		bool isChosen = state.Values.GetValueOrDefault(item.Id) == choice;
		double num = (isChosen ? 1 : 0);
		if (state.ChoiceChanges.TryGetValue(item.Id, out (string, double) value))
		{
			double num2 = InkSpring.Value(0.25, 0.75, InkObjectState.Now - value.Item2);
			if (isChosen)
			{
				num = num2;
			}
			else if (value.Item1 == choice)
			{
				num = 1.0 - num2;
			}
		}
		ClickyShapeView item2 = ClickyShapeView.Stroke(new ClickyHandRing(index + 2, 0.08), new PaperStroke(Ink.Opacity(0.55), 1.2));
		Grid grid = new Grid
		{
			Width = 14.0,
			Height = 14.0,
			Children = { (UIElement)item2 }
		};
		if (num > 0.001)
		{
			Ellipse ellipse = new Ellipse
			{
				Width = 8.0,
				Height = 8.0,
				Fill = Ui.Brush(style.Fresh),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			ellipse.Transformed(0.0, 0.0, 0.0, Math.Max(0.0, num), null);
			grid.Children.Add(ellipse);
		}
		InkTextView inkTextView = InkViews.Plain(choice, ClickyPaperFonts.Typed(13.0, isChosen ? FontWeights.SemiBold : FontWeights.Normal), Ink);
		Pressable pressable = InkViews.PlainButton(new ClickyInkHStack(6.0, InkVerticalAlignment.Center, grid, inkTextView), delegate
		{
			string valueOrDefault = state.Values.GetValueOrDefault(item.Id, "");
			state.Values[item.Id] = (isChosen ? "" : choice);
			state.ChoiceChanges[item.Id] = (valueOrDefault, InkObjectState.Now);
			state.RefreshSend();
			state.Animate(InkSpring.SettleSeconds(0.25, 0.75));
		}, choice);
		if (isChosen)
		{
			AutomationProperties.SetItemStatus(pressable, "selected");
		}
		return pressable;
	}
}

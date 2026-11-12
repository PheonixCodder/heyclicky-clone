using System;
using System.Text.Json.Nodes;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Dock;

internal static class ArcNextStep
{
	public static FrameworkElement? Button(ArcDock dock, ArcCardContent content, bool isDraftCopied, Action onCopied, Action<string, JsonObject> track, bool canShowFullResponse = true, double height = 32.0, double fontSize = 13.5)
	{
		ClickyAnswer clickyAnswer = (content.IsDone ? content.Answer : null);
		if (clickyAnswer?.Ask != null)
		{
			return ArcButtons.Gel("Type an answer", "keyboard", GelAppearance.SecondaryWhite, height, fontSize, GelButton.Sizing.Normal, delegate
			{
				track("next_step", new JsonObject((JsonNodeOptions?)null) { ["kind"] = "type_answer" });
				dock.OpenInChat(content);
				dock.Host.RequestComposerFocus();
			});
		}
		ClickyAnswer.Draft draft = clickyAnswer?.DraftText;
		if ((object)draft != null)
		{
			return ArcButtons.Gel(isDraftCopied ? "Copy again" : "Copy", "doc.on.doc", GelAppearance.PrimaryBlue, height, fontSize, GelButton.Sizing.Normal, delegate
			{
				track("next_step", new JsonObject((JsonNodeOptions?)null)
				{
					["kind"] = "copy",
					["is_repeat"] = isDraftCopied
				});
				dock.CopyDraft(content, draft.Text);
				onCopied();
			});
		}
		ClickyAnswer.NextStep next = clickyAnswer?.Next;
		if ((object)next != null)
		{
			bool isLink = next.Open.StartsWith("http", StringComparison.OrdinalIgnoreCase);
			return ArcButtons.Gel(next.Label, isLink ? "arrow.up.right" : null, GelAppearance.PrimaryBlue, height, fontSize, GelButton.Sizing.Normal, delegate
			{
				track("next_step", new JsonObject((JsonNodeOptions?)null) { ["kind"] = (isLink ? "next_link" : "next_file") });
				dock.OpenResultCardTarget(next.Open, content);
			});
		}
		CardReadyOutput output = content.ReadyOutput;
		if ((object)output != null && !(output.OpensFullReplyOnly && canShowFullResponse))
		{
			string buttonTitle = output.ButtonTitle;
			string text = output.ButtonSymbol;
			if (text == null)
			{
				CardReadyOutput.Kinds kind = output.Kind;
				bool flag = ((kind == CardReadyOutput.Kinds.File || kind == CardReadyOutput.Kinds.Several) ? true : false);
				text = (flag ? output.Symbol : null);
			}
			return ArcButtons.Gel(buttonTitle, text, GelAppearance.PrimaryBlue, height, fontSize, GelButton.Sizing.Normal, delegate
			{
				track("next_step", new JsonObject((JsonNodeOptions?)null)
				{
					["kind"] = "open_output",
					["output_kind"] = ArcDockAnalytics.OutputKind(output.Kind)
				});
				dock.OpenReadyOutput(content.RailKey, output);
			});
		}
		if ((object)clickyAnswer != null && clickyAnswer.IsQuiet)
		{
			string routineId = dock.ResultCardRoutineId(content);
			if (routineId != null)
			{
				return ArcButtons.Gel("Run it now", "arrow.clockwise", GelAppearance.SecondaryWhite, height, fontSize, GelButton.Sizing.Normal, delegate
				{
					track("next_step", new JsonObject((JsonNodeOptions?)null) { ["kind"] = "run_now" });
					dock.RunRoutineNow(routineId);
				});
			}
		}
		return null;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkNoteActions
{
	private const string RewriteInPlace = "Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.";

	private const string EditInPlace = "Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.";

	public static ClickyInkNoteAction Ask(ClickyInkDocument.Block block)
	{
		string text = Plain(block.Text);
		string text2 = Plain(block["say"] ?? block["ask"] ?? block.Text);
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Ask, ClickyInkObjectKey.Of(block), (text2.Length == 0) ? text : text2, "The user tapped \"" + ((text.Length == 0) ? text2 : text) + "\" on this job's note (note.md). Do what it asks. Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is. Put a ::note[…] beside the change saying what they asked, in a few words.", "you asked: " + ((text.Length == 0) ? text2 : text), "On it.").ForAnalytics("ask_tapped");
	}

	public static ClickyInkNoteAction? Answer(ClickyInkDocument.Block block, bool saidYes)
	{
		string text = (saidYes ? block["yes"] : block["no"]);
		if (text == null)
		{
			return null;
		}
		string text2 = Plain(text);
		if (text2.Length == 0)
		{
			return null;
		}
		string text3 = Plain(saidYes ? (block["yes_label"] ?? "Yes") : (block["no_label"] ?? "No"));
		string value = Plain(block.Text);
		string text4 = text3.Lowered();
		bool flag = ((text4 == "yes" || text4 == "no") ? true : false);
		bool flag2 = flag;
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Answer, ClickyInkObjectKey.Of(block), text2, $"The user answered the question on this job's note, \"{value}\": they tapped \"{text3}\", meaning: {text2} Act on that answer. Take that ::question off note.md (it's answered), " + "Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is. Put a ::note[…] beside the change saying what they chose.", "you said: " + (flag2 ? Short(text2) : text3), "Got it.").ForAnalytics("question_answered", saidYes ? "yes" : "no");
	}

	public static ClickyInkNoteAction? Send(ClickyInkDocument.Block block)
	{
		string text = block["send"] ?? block["ask"];
		if (text == null)
		{
			return null;
		}
		string text2 = Plain(text);
		if (text2.Length == 0)
		{
			return null;
		}
		string text3 = "";
		string text4 = block["to"];
		if (text4 != null && text4.Length > 0)
		{
			text3 = text3 + " to " + Plain(text4);
		}
		string text5 = block["subject"];
		if (text5 != null && text5.Length > 0)
		{
			text3 = text3 + " (subject \"" + Plain(text5) + "\")";
		}
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Send, ClickyInkObjectKey.Of(block), text2, "The user tapped Send on the draft" + text3 + " in this job's note. That tap is their go-ahead to send this one message: send it exactly as note.md has it now, without rewording it. Then rewrite note.md in place: the letter becomes one short line saying it was sent (to whom, when), or plainly why it couldn't be sent and what they can do. Keep every other line as it is.", "you said: send it", "Okay, I'll send it.").ForAnalytics("letter_send");
	}

	public static ClickyInkNoteAction? Review(ClickyInkDocument.Block block, IReadOnlyList<string> ticked, int total)
	{
		List<string> list = (from i in ticked.Select(Plain)
			where i.Length > 0
			select i).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		string text = Plain(ClickyInkReviewRules.Label(block));
		string text2 = ClickyInkReviewRules.GoInstruction(block);
		int num = Math.Max(0, total - list.Count);
		string text3 = text2 + ": " + string.Join("; ", list) + ".";
		if (num > 0)
		{
			text3 += $" Leave the other {num} as {((num == 1) ? "it is" : "they are")}.";
		}
		string value = ((text.Length == 0) ? "the list" : ("the list \"" + text + "\""));
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Review, ClickyInkObjectKey.Of(block), text3, $"The user reviewed {value} on this job's note and ticked {list.Count} of {total} to go ahead with. Do it for exactly those {list.Count}, nothing else; leave the unticked ones alone. Then rewrite that :::review " + "list in note.md in place: mark each one you did {done} (or {out} with the reason on the line under it when it couldn't be done), keep every unticked line exactly as it is (each still its own line, still theirs to decide), and put a ::note[…] beside it saying what they approved.", "you asked: " + ClickyInkReviewRules.GoLabel(block, list.Count).Lowered() + ((num > 0) ? $" of {total}" : ""), (list.Count == 1) ? "Okay, that one." : $"Okay, those {list.Count}.", list).ForAnalytics("review_go_ahead", null, new Dictionary<string, int>
		{
			["ticked"] = list.Count,
			["total"] = total
		});
	}

	public static ClickyInkNoteAction? SkipReview(ClickyInkDocument.Block block)
	{
		string text = block["skip"];
		if (text == null)
		{
			return null;
		}
		string text2 = Plain(text);
		if (text2.Length == 0)
		{
			return null;
		}
		string text3 = Plain(ClickyInkReviewRules.Label(block));
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Review, ClickyInkObjectKey.Of(block), text2, "The user looked at " + ((text3.Length == 0) ? "the list" : ("the list \"" + text3 + "\"")) + " on this job's note and said not now to all of it: do none of them. Then rewrite note.md in place: that :::review list becomes one short line saying they passed for now. Keep every other line as it is.", "you said: " + Plain(block["skip_label"] ?? "not now").Lowered(), "Okay, none of them.", Array.Empty<string>()).ForAnalytics("review_skipped");
	}

	public static ClickyInkNoteAction ActionForSentWords(string words, ClickyInkLiveNote note)
	{
		string text = Plain(words);
		foreach (ClickyInkLiveNote.Page page in note.Pages)
		{
			foreach (ClickyInkLiveNote.Block block in page.Blocks)
			{
				if (block.GoneAt.HasValue)
				{
					continue;
				}
				ClickyInkDocument.Block source = block.Source;
				switch (source.Kind)
				{
				case ClickyInkDocument.Kind.Ask:
					if (Plain(source["say"] ?? source["ask"] ?? source.Text) == text)
					{
						return Ask(source);
					}
					break;
				case ClickyInkDocument.Kind.Question:
				{
					string text3 = source["yes"];
					if (text3 != null && Plain(text3) == text)
					{
						ClickyInkNoteAction clickyInkNoteAction3 = Answer(source, saidYes: true);
						if ((object)clickyInkNoteAction3 != null)
						{
							return clickyInkNoteAction3;
						}
						break;
					}
					string text4 = source["no"];
					if (text4 != null && Plain(text4) == text)
					{
						ClickyInkNoteAction clickyInkNoteAction4 = Answer(source, saidYes: false);
						if ((object)clickyInkNoteAction4 != null)
						{
							return clickyInkNoteAction4;
						}
					}
					break;
				}
				case ClickyInkDocument.Kind.Letter:
				{
					string text5 = source["send"] ?? source["ask"];
					if (text5 != null && Plain(text5) == text)
					{
						ClickyInkNoteAction clickyInkNoteAction5 = Send(source);
						if ((object)clickyInkNoteAction5 != null)
						{
							return clickyInkNoteAction5;
						}
					}
					break;
				}
				case ClickyInkDocument.Kind.Review:
				{
					string text2 = source["skip"];
					if (text2 != null && Plain(text2) == text)
					{
						ClickyInkNoteAction clickyInkNoteAction = SkipReview(source);
						if ((object)clickyInkNoteAction != null)
						{
							return clickyInkNoteAction;
						}
					}
					else if (text.HasPrefix(ClickyInkReviewRules.GoInstruction(source) + ":"))
					{
						List<string> ticked = ClickyInkReviewRules.ItemsToSend(source, Array.Empty<string>());
						int total = source.Items.Count(ClickyInkReviewRules.IsOpen);
						ClickyInkNoteAction clickyInkNoteAction2 = Review(source, ticked, total);
						if ((object)clickyInkNoteAction2 != null)
						{
							return clickyInkNoteAction2;
						}
					}
					break;
				}
				}
			}
		}
		return Ask(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Ask, text, new Dictionary<string, string> { ["say"] = text })).ForAnalytics("ask_tapped", "unmatched_words");
	}

	public static string Plain(string text)
	{
		return ClickyInkParser.Emphasis.Strip(text).TrimWsNl();
	}

	private static string Short(string text)
	{
		if (text.CharacterCount() <= 64)
		{
			return text;
		}
		return text.PrefixCharacters(61).TrimWs() + "…";
	}

	public static ClickyInkNoteAction? Answer(ClickyInkDocument.Block block, string answer, bool wasTyped = false)
	{
		string text = Plain(answer);
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = Plain(block.Text);
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Answer, ClickyInkObjectKey.Of(block), text, "The user answered the question on this job's note, \"" + text2 + "\": " + (wasTyped ? ("they typed \"" + text + "\".") : ("they chose \"" + text + "\".")) + " Act on that answer. Take that ::question off note.md (it's answered), Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is. Put a ::note[…] beside the change saying what they chose.", "you said: " + Shortened(text), "Got it.").ForAnalytics("question_answered", wasTyped ? "typed" : "option");
	}

	public static ClickyInkNoteAction? Form(ClickyInkDocument.Block block, IReadOnlyList<(string Label, string Value)> answers, bool usedDefaults)
	{
		List<(string, string)> list = (from a in answers
			select (Label: Plain(a.Label), Value: Plain(a.Value)) into a
			where a.Value.Length > 0
			select a).ToList();
		string text = Plain(block.Text);
		string text2 = Plain(block["submit"] ?? "Here are my answers");
		string text3;
		if (usedDefaults)
		{
			text3 = Plain(block["defaults"] ?? "Use your best guess for anything I haven't told you.");
			if (list.Count > 0)
			{
				text3 = text2 + ":\n" + string.Join("\n", list.Select<(string, string), string>(((string Label, string Value) f) => "- " + f.Label + ": " + f.Value)) + "\n" + text3;
			}
		}
		else
		{
			if (list.Count == 0)
			{
				return null;
			}
			text3 = text2 + ":\n" + string.Join("\n", list.Select<(string, string), string>(((string Label, string Value) f) => "- " + f.Label + ": " + f.Value));
		}
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Answer, ClickyInkObjectKey.Of(block), text3, "The user filled in the form" + ((text.Length == 0) ? "" : (" \"" + text + "\"")) + " on this job's note. Use their answers and carry on with the work. Take the :::form off note.md (it's answered), Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.", (usedDefaults && list.Count == 0) ? "you said: use your best guess" : $"you sent {list.Count} answer{((list.Count == 1) ? "" : "s")}", "Thanks, carrying on.").ForAnalytics("form_sent", usedDefaults ? "best_guess" : "filled", new Dictionary<string, int>
		{
			["fields_filled"] = list.Count,
			["fields_total"] = answers.Count
		});
	}

	public static ClickyInkNoteAction Connected(ClickyInkDocument.Block block)
	{
		string text = Plain((block.Text.Length == 0) ? (block["app"] ?? "the app") : block.Text);
		string message = Plain(block["retry"] ?? ("I connected " + text + ". Try again."));
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Ask, ClickyInkObjectKey.Of(block), message, "The user connected " + text + " from the note, as it asked. Try again with it. If it works, take the ::connect off note.md; if it still isn't connected, say so plainly. Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.", "you said: " + text + " is connected", "Trying again.").ForAnalytics("connect_confirmed");
	}

	public static ClickyInkNoteAction? ItemAction(ClickyInkDocument.Block block, string item, string action)
	{
		string text = ClickyInkReviewRules.Action(item);
		string text2 = Plain(action);
		if (text.Length == 0 || text2.Length == 0)
		{
			return null;
		}
		string text3 = Plain(block.Text);
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Answer, ItemKey(block, item), text2 + ": " + text, $"The user tapped \"{text2}\" on \"{text}\" in the list{((text3.Length == 0) ? "" : (" \"" + text3 + "\""))} on this job's note. " + "Do that for that one row only. Then rewrite that row in note.md in place to show what happened (mark it {done}, or {out} with why on the line under it, and put any draft it needs as a :::letter under the list); keep every other line exactly as it is.", "you said: " + text2.Lowered(), "On it.").ForAnalytics("item_action");
	}

	public static string ItemKey(ClickyInkDocument.Block block, string item)
	{
		return ClickyInkObjectKey.Of(block) + "|" + ClickyInkObjectKey.Normalized(ClickyInkReviewRules.Action(item));
	}

	public static ClickyInkNoteAction QuizResults(ClickyInkDocument.Block block, int right, int total, IReadOnlyList<string> missed)
	{
		string text = Plain(block.Text);
		string text2 = $"I got {right} of {total} right";
		if (missed.Count > 0)
		{
			text2 = text2 + ". I missed: " + string.Join("; ", missed.Select(Plain));
		}
		text2 += ".";
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Ask, ClickyInkObjectKey.Of(block), text2, "The user worked through the quiz" + ((text.Length == 0) ? "" : (" \"" + text + "\"")) + " on this job's note and sent their score. Explain what they missed in a few words each, then give them a new :::quiz that practises exactly those. Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.", $"you sent your score: {right}/{total}", "Nice work.").ForAnalytics("quiz_score_sent", null, new Dictionary<string, int>
		{
			["right"] = right,
			["total"] = total
		});
	}

	public static ClickyInkNoteAction? Pick(ClickyInkDocument.Block block, string option)
	{
		string text = Plain(option);
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = Plain(block["pick"] ?? "Go with");
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Answer, ClickyInkObjectKey.Of(block), text2 + ": " + text, $"The user picked \"{text}\" from the options{((block.Text.Length == 0) ? "" : (" \"" + Plain(block.Text) + "\""))} on this job's note. " + "Carry on with that one. Mark it {pick} in the :::gallery and the rest {out}, Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.", "you picked: " + Shortened(text), "Good pick.").ForAnalytics("gallery_pick");
	}

	public static ClickyInkNoteAction? Criteria(ClickyInkDocument.Block block, IReadOnlyList<(string Name, string From, string To)> changes)
	{
		List<(string, string, string)> list = changes.Where<(string, string, string)>(((string Name, string From, string To) c) => Plain(c.From) != Plain(c.To)).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		IEnumerable<string> values = list.Select<(string, string, string), string>(((string Name, string From, string To) c) => $"- {Plain(c.Name)}: {((Plain(c.To).Length == 0) ? "(none)" : Plain(c.To))} (was {((Plain(c.From).Length == 0) ? "none" : Plain(c.From))})");
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Ask, ClickyInkObjectKey.Of(block), "Change what you look for:\n" + string.Join("\n", values), "The user edited the criteria on this job's note. Use the new values from now on: if this is a routine, update the routine's task to match (a <ROUTINE> update block). Update the :::criteria in note.md to the new values, Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.", "you changed " + ((list.Count == 1) ? Plain(list[0].Item1).Lowered() : $"{list.Count} criteria"), "Updated.").ForAnalytics("criteria_saved", null, new Dictionary<string, int> { ["changed"] = list.Count });
	}

	public static ClickyInkNoteAction Retone(ClickyInkDocument.Block block, string tone)
	{
		string text = Plain(block["to"] ?? "");
		return new ClickyInkNoteAction(ClickyInkNoteAction.ActionKind.Ask, ClickyInkObjectKey.Of(block), $"Make the draft{((text.Length == 0) ? "" : (" to " + text))} {tone.Lowered()}.", $"The user asked to make the draft{((text.Length == 0) ? "" : (" to " + text))} on this job's note {tone.Lowered()}. " + "Rewrite only that :::letter's text, keeping what it says; do not send it. Then rewrite note.md in place for it: change only what changes and keep every other line exactly as it is.", "you asked: " + tone.Lowered(), "Rewriting it.").ForAnalytics("letter_retone");
	}

	private static string Shortened(string text)
	{
		if (text.CharacterCount() <= 64)
		{
			return text;
		}
		return text.PrefixCharacters(61).TrimWs() + "…";
	}
}

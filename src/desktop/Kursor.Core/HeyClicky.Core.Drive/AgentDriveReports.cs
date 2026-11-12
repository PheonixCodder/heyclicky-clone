using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Drive;

public static class AgentDriveReports
{
	public const string Declined = "Not now. Finish without controlling my PC, and tell me what you did get done. Don't ask to do this step on my PC again unless I tell you to go ahead with it.";

	public const string DeclinedCursorLine = "Okay, I'll leave that step to you. The job finishes without it.";

	public static readonly TimeSpan MacStepOpenCall = TimeSpan.FromSeconds(240L);

	private static readonly HashSet<string> CommonWords = new HashSet<string>
	{
		"this", "that", "with", "from", "into", "your", "their", "then", "when", "save",
		"file", "open", "the", "and", "for", "step", "mac", "screen", "folder", "user",
		"users"
	};

	public static string Report(AgentDriveOutcome outcome, string request)
	{
		string text = outcome.Ending switch
		{
			AgentDriveEnding.Finished => (outcome.Actions.Count > 0) ? "It finished." : (HasSummary(outcome) ? "It looked at the screen and didn't need to click or type anything. Trust what it found below; don't ask for this same step again unless that says something is still left to do on the PC." : "It ended without acting on the screen, so this step may not be done."), 
			AgentDriveEnding.UserTookOver => "The user took the screen back partway through, so this step may not be finished.", 
			AgentDriveEnding.SafetyStop => "It stopped for a safety check, so the user has to decide that part themselves.", 
			AgentDriveEnding.CouldNotSee => "It couldn't see the screen, so nothing was done.", 
			AgentDriveEnding.Failed => "It lost its connection partway, so this step may not be finished.", 
			_ => "It stopped partway: the user is out of agent messages for now, so this step may not be finished.", 
		};
		if (HasSummary(outcome))
		{
			text = text + " Its summary: " + outcome.Summary.Trim();
		}
		string text2 = string.Join("; ", outcome.Actions.TakeLast(40));
		string value = ((text2.Length == 0) ? "" : (" What it did on screen: " + text2 + "."));
		return $"{"[Clicky drove your screen for this step]"} HeyClicky drove the user's screen for the step you asked for: {request} {text}{value} " + "Carry on with the rest of the task from here. Check the result yourself where you can (open the file, read the page) instead of assuming it worked, don't redo a step that worked, and if something is still left on the PC, ask again the same way.";
	}

	private static bool HasSummary(AgentDriveOutcome outcome)
	{
		string text = outcome.Summary?.Trim();
		if (text != null && text.Length > 0)
		{
			return text != "done";
		}
		return false;
	}

	public static string Unavailable(string request)
	{
		return "[Clicky drove your screen for this step] HeyClicky can't drive the screen right now, so nothing was done on the PC for: " + request + " Finish what you can without it, and tell me exactly what to click or type for that step so I can do it myself.";
	}

	public static string Busy(string request)
	{
		return "[Clicky drove your screen for this step] HeyClicky couldn't drive the screen just now (it was already busy driving). Nothing was done on the PC for: " + request + " Finish what you can without it and tell me which step is left for me.";
	}

	public static JsonObject MacStepDeclinedBefore()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "declined",
			["message"] = "The user already said not now to this step. Don't ask for it again unless they tell you to go ahead with it: finish what you can without it, and in your reply say exactly what's left for them to do there (the clicks, in a short numbered list)."
		};
	}

	public static string? MacStepCardLine(string ask, string why)
	{
		string text = ask.Trim(' ', '.');
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = why.Trim(' ', '.');
		string text3 = ((text2.Length == 0) ? text : (text + ", " + text2));
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text3[0]));
		string text4 = text3;
		return string.Concat(readOnlySpan, text4.Substring(1, text4.Length - 1), ".");
	}

	public static string ShownRequest(string step, string why)
	{
		string text = why.Trim(' ', '.');
		if (text.Length != 0)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
			defaultInterpolatedStringHandler.AppendFormatted(char.ToUpperInvariant(text[0]));
			string text2 = text;
			defaultInterpolatedStringHandler.AppendFormatted(text2.Substring(1, text2.Length - 1));
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(step);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
		return step;
	}

	public static JsonObject MacStepBusy()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "busy",
			["message"] = "A step on the PC is already waiting for the user's answer. Wait for it before asking for another."
		};
	}

	public static JsonObject MacStepUnavailable()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "unavailable",
			["message"] = "HeyClicky can't do steps on the screen right now. Finish what you can, and in your reply tell the user exactly what to click or type for this step, as a short numbered list."
		};
	}

	public static JsonObject MacStepChoosingChrome()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "unavailable",
			["message"] = "Not now: they're choosing whether to connect Chrome for this job. Don't do the website part another way. End your turn; you'll be told to carry on once they've chosen."
		};
	}

	public static JsonObject MacStepChromeAskedAtTurnEnd()
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "unavailable",
			["message"] = "Not yet: Chrome isn't connected, and HeyClicky asks them about it once your turn ends. Don't do a website step another way. Finish everything else, then end your turn with a line saying the rest needs Chrome connected (name this step, even if it isn't on a website); you'll be told to carry on once they've chosen."
		};
	}

	public static JsonObject MacStepToolResult(MacStepAnswer answer, string step)
	{
		if (!(answer is MacStepAnswer.Drove drove))
		{
			if (!(answer is MacStepAnswer.Declined))
			{
				if (!(answer is MacStepAnswer.NoAnswer))
				{
					if (!(answer is MacStepAnswer.NotYet))
					{
						if (answer is MacStepAnswer.BeingDriven)
						{
							return new JsonObject((JsonNodeOptions?)null)
							{
								["status"] = "in_progress",
								["message"] = "HeyClicky is doing this step on their screen right now. End your turn now with one line saying you're waiting for it (no request block). When it's done, HeyClicky continues this conversation with what happened as your next message."
							};
						}
						return new JsonObject((JsonNodeOptions?)null)
						{
							["status"] = "busy",
							["message"] = "HeyClicky was already driving the screen, so nothing was done for this step. Finish what you can and say what's left."
						};
					}
					return new JsonObject((JsonNodeOptions?)null)
					{
						["status"] = "waiting",
						["message"] = "The user hasn't answered yet, and the request stays on your card. End your turn now: say in one line that you're waiting for this step on their PC, and end your final message with <COMPUTER_USE_REQUEST>" + step + "</COMPUTER_USE_REQUEST>. When it's done, HeyClicky continues this conversation with what happened as your next message. Don't ask for it again."
					};
				}
				return new JsonObject((JsonNodeOptions?)null)
				{
					["status"] = "no_answer",
					["message"] = "The user didn't answer. Finish what you can without this step, and say in your reply exactly what's left for them to do there."
				};
			}
			return new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "declined",
				["message"] = "The user said not now. Finish what you can without this step, and say in your reply exactly what's left for them to do there (the clicks, in a short numbered list). Don't ask for this step again unless they tell you to go ahead with it."
			};
		}
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "done",
			["result"] = drove.Report
		};
	}

	public static bool DriveGoalNamesJob(string goal, string threadId, string title)
	{
		string text = title.Trim();
		if (!goal.Contains(threadId, StringComparison.Ordinal))
		{
			if (text.Length >= 4)
			{
				return CultureInfo.InvariantCulture.CompareInfo.IndexOf(goal, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
			}
			return false;
		}
		return true;
	}

	public static bool DriveGoalSharesWordsWithStep(string goal, string step)
	{
		return Words(goal).Intersect(Words(step)).Count() >= 2;
		static HashSet<string> Words(string text)
		{
			HashSet<string> hashSet = new HashSet<string>();
			StringBuilder stringBuilder = new StringBuilder();
			string text2 = text.ToLowerInvariant() + " ";
			foreach (char c in text2)
			{
				if (char.IsLetter(c))
				{
					stringBuilder.Append(c);
				}
				else
				{
					if (stringBuilder.Length >= 4 && !CommonWords.Contains(stringBuilder.ToString()))
					{
						hashSet.Add(stringBuilder.ToString());
					}
					stringBuilder.Clear();
				}
			}
			return hashSet;
		}
	}
}

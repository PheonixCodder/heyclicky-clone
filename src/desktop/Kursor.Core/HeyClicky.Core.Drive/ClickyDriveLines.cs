using System;
using System.Collections.Generic;
using HeyClicky.Core.Tools;

namespace HeyClicky.Core.Drive;

public static class ClickyDriveLines
{
	public const string FixedOfferLine = "Want me to just do it? I'll drive, you watch.";

	public const string HandedBack = "Okay, it's all yours.";

	public const string CouldNotSee = "I can't see the screen right now.";

	public const string LostConnection = "I lost the connection partway, so I stopped there.";

	public const string OutOfAgentMessages = "You're out of agent messages for now, so I stopped here.";

	public const string Declined = "[App] The user chose to do it themselves (they declined the take-over). Keep guiding them.";

	public const string StartingStatus = "Getting started";

	public const int ShortRequestCharacters = 110;

	public static string DriveFact(string goal, string? clickyName)
	{
		string value = ((clickyName == null) ? "" : (" \"" + clickyName + "\", a job working in the background, asked for this one step on their PC; do only this step, then end the take-over so it can carry on. In end_screen_takeover's summary say what you made and where it is (the full path of any file you saved), so the job can pick it up."));
		return $"[Take-over] They said yes: you are driving their screen now, toward: {goal}.{value} Use the computer tool, one short spoken sentence before each group of actions; the first one also asks them to keep their hands off the mouse while you work (pressing the escape key stops you; say it as \"the escape key\", never \"esc\"). When it is done, say so in one line and stop acting.";
	}

	public static string SafetyStop(string message)
	{
		return "I stopped there: " + ((message.Length > 140) ? message.Substring(0, 140) : message) + ". It's yours.";
	}

	public static string ThenFact(string? summary, string then)
	{
		string text = (summary ?? "").Trim();
		string value = ((text.Length == 0) ? "" : (" What you said at the end: \"" + text + "\"."));
		return $"[App] The screen step is done.{value} The rest of what they asked: {then}. Start it now as background work (sessions_spawn) with what the drive produced (the file's name and where it was saved, the names, the link), in one short line, without asking again.";
	}

	public static string ActionsNote(IReadOnlyList<string> actions)
	{
		return "[App] While driving their screen you did: " + string.Join("; ", actions) + ".";
	}

	public static string SummaryFirstSentence(string summary)
	{
		string text = summary.TrimStart('.');
		string text2 = ((text.Length == 0) ? summary : (text.Split('.', 2)[0] + "."));
		if (text2.Length <= 160)
		{
			return text2;
		}
		return text2.Substring(0, 160);
	}

	public static string Brief(string goal)
	{
		return Sentence(ClickyNotes.SpeakableSentence(goal, 110));
	}

	public static string AsksToUsePc(string? clickyName, string request)
	{
		string text = ClickyNotes.SpeakableSentence(request, 110);
		string text3;
		if (text.Length != 0)
		{
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToLowerInvariant(text[0]));
			string text2 = text;
			text3 = string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
		}
		else
		{
			text3 = text;
		}
		string text4 = text3;
		return (string.IsNullOrWhiteSpace(clickyName) ? "Your job" : clickyName.Trim()) + " needs one step on your PC: " + text4 + " Want me to do it?";
	}

	public static string Sentence(string goal)
	{
		string text = goal.Trim();
		if (text.Length == 0)
		{
			return text;
		}
		char reference = char.ToUpperInvariant(text[0]);
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(in reference);
		string text2 = text;
		string text3 = string.Concat(readOnlySpan, text2.Substring(1, text2.Length - 1));
		reference = text3[text3.Length - 1];
		if ((reference != '!' && reference != '.' && reference != '?') || 1 == 0)
		{
			return text3 + ".";
		}
		return text3;
	}
}

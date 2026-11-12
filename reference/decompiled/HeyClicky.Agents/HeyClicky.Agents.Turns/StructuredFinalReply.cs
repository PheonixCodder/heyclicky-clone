using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.Agents.Turns;

public sealed record StructuredFinalReply(string Text, IReadOnlyList<string> NextActions, string? DoneTitle, IReadOnlyList<string> Artifacts, string? Summary, AgentRoutineRequest? Routine, string? ComputerUseRequest, ClickyJobUserStep? UserStep = null, IReadOnlyList<string>? LearnedHowTos = null, ClickyAnswer? Answer = null)
{
	public string SummaryOrFallback
	{
		get
		{
			object obj = Summary;
			if (obj == null)
			{
				if (Text.Length != 0)
				{
					return Cap(Text, 320);
				}
				obj = "Codex child worker completed successfully.";
			}
			return (string)obj;
		}
	}

	private static readonly Regex StructuredBlockTag = new Regex("<\\s*(/?)\\s*(SUMMARY|NEXT_ACTIONS|DONE_TITLE|ARTIFACTS|ROUTINE|COMPUTER_USE_REQUEST|WAIT_FOR|LEARNED|ANSWER)\\s*>", RegexOptions.IgnoreCase);

	public static StructuredFinalReply Parse(string raw)
	{
		string text = NormalizingStructuredBlockTags(CitationMarkers.Strip(raw));
		List<string> nextActions = ExtractList(ref text, "NEXT_ACTIONS", keepPathCharacters: false, 4);
		string text2 = ExtractBlock(ref text, "DONE_TITLE");
		string text3 = ((text2 != null) ? Cap(text2.Trim().Trim('"', '\'', '`', '.').Trim(), 60) : null);
		List<string> artifacts = ExtractList(ref text, "ARTIFACTS", keepPathCharacters: true, 8);
		ClickyAnswer clickyAnswer = ClickyAnswer.Parse(ExtractTaggedBlock(ref text, "ANSWER"));
		string text4 = ExtractBlock(ref text, "SUMMARY")?.Trim();
		string summary = ((text4 != null && text4.Length > 0) ? Cap(text4, 320) : clickyAnswer?.Line);
		string text5 = ExtractBlock(ref text, "ROUTINE");
		AgentRoutineRequest routine = ((text5 != null) ? AgentRoutineRequest.Parse(text5) : null);
		string text6 = ExtractBlock(ref text, "COMPUTER_USE_REQUEST")?.Trim();
		string computerUseRequest = ((text6 != null && text6.Length > 0) ? Cap(text6, 400) : null);
		string text7 = ExtractTaggedBlock(ref text, "WAIT_FOR");
		ClickyJobUserStep userStep = ((text7 != null) ? ClickyJobUserStep.Parse(text7, null) : null);
		IReadOnlyList<string> learnedHowTos = ClickyLearnedHowTos.Lines(ExtractTaggedBlock(ref text, "LEARNED"));
		return new StructuredFinalReply(text.Trim(), nextActions, string.IsNullOrEmpty(text3) ? null : text3, artifacts, summary, routine, computerUseRequest, userStep, learnedHowTos, clickyAnswer);
	}

	private static string? ExtractTaggedBlock(ref string text, string name)
	{
		string text2 = ExtractBlock(ref text, name)?.Trim();
		if (text2 == null || text2.Length <= 0)
		{
			return null;
		}
		return text2;
	}

	public static string NormalizingStructuredBlockTags(string text)
	{
		if (!text.Contains('<'))
		{
			return text;
		}
		string text2 = StructuredBlockTag.Replace(text, (Match m) => "<" + m.Groups[1].Value + m.Groups[2].Value + ">");
		int num = text2.LastIndexOf("<SUMMARY>", StringComparison.OrdinalIgnoreCase);
		if (num >= 0 && text2.IndexOf("</SUMMARY>", num + "<SUMMARY>".Length, StringComparison.OrdinalIgnoreCase) < 0)
		{
			text2 += "</SUMMARY>";
		}
		return text2;
	}

	private static string Cap(string s, int n)
	{
		if (s.Length <= n)
		{
			return s;
		}
		return s.Substring(0, n);
	}

	private static string? ExtractBlock(ref string text, string tag)
	{
		int num = text.IndexOf("<" + tag + ">", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return null;
		}
		int num2 = num + tag.Length + 2;
		int num3 = text.IndexOf("</" + tag + ">", num2, StringComparison.OrdinalIgnoreCase);
		if (num3 < 0)
		{
			return null;
		}
		string obj = text;
		int num4 = num2;
		string result = obj.Substring(num4, num3 - num4);
		text = text.Remove(num, num3 + tag.Length + 3 - num);
		return result;
	}

	private static List<string> ExtractList(ref string text, string tag, bool keepPathCharacters, int max)
	{
		string text2 = ExtractBlock(ref text, tag);
		if (text2 == null)
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		string[] array = text2.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i].Trim();
			while (text3.StartsWith('-') || text3.StartsWith('*') || text3.StartsWith('•'))
			{
				string text4 = text3;
				text3 = text4.Substring(1, text4.Length - 1).Trim();
			}
			text3 = (keepPathCharacters ? text3.Trim('"', '\'', '`').Trim() : text3.Trim('"', '\'', '`', '.', ',', ';', ':').Trim());
			if (text3.Length > 0)
			{
				list.Add(text3);
			}
			if (list.Count == max)
			{
				break;
			}
		}
		return list;
	}

	[CompilerGenerated]
	private StructuredFinalReply(StructuredFinalReply original)
	{
		Text = original.Text;
		NextActions = original.NextActions;
		DoneTitle = original.DoneTitle;
		Artifacts = original.Artifacts;
		Summary = original.Summary;
		Routine = original.Routine;
		ComputerUseRequest = original.ComputerUseRequest;
		UserStep = original.UserStep;
		LearnedHowTos = original.LearnedHowTos;
		Answer = original.Answer;
	}
}

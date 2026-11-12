using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyWorkShelf
{
	public enum Kind
	{
		Made,
		QuickAnswer,
		Question
	}

	public const int FirstAnswerLength = 160;

	public const int FullFollowUpAnswerLength = 1600;

	public const int QuestionMaximumLength = 700;

	public ClickyWorkPiece? OpenQuestion { get; }

	public IReadOnlyList<ClickyWorkPiece> Made { get; }

	public IReadOnlyList<ClickyWorkPiece> QuickAnswers { get; }

	public IReadOnlyDictionary<string, int> RoutineRunCounts { get; }

	public ClickyWorkShelf(IReadOnlyList<ClickyWorkPiece> works)
	{
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (ClickyWorkPiece item2 in works.OrderBy((ClickyWorkPiece p) => p.FinishedAt))
		{
			string item = item2.ThreadId ?? item2.Id;
			if (!hashSet.Add(item))
			{
				hashSet2.Add(item2.Id);
			}
		}
		List<ClickyWorkPiece> list = new List<ClickyWorkPiece>();
		List<ClickyWorkPiece> list2 = new List<ClickyWorkPiece>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		ClickyWorkPiece clickyWorkPiece = null;
		for (int i = 0; i < works.Count; i++)
		{
			ClickyWorkPiece clickyWorkPiece2 = works[i];
			switch (KindOf(clickyWorkPiece2, hashSet2.Contains(clickyWorkPiece2.Id)))
			{
			case Kind.Question:
				if (i == 0)
				{
					clickyWorkPiece = clickyWorkPiece2;
				}
				break;
			case Kind.QuickAnswer:
				list2.Add(clickyWorkPiece2);
				break;
			case Kind.Made:
				if (clickyWorkPiece2.IsRoutineRun)
				{
					string key = RoutineKey(clickyWorkPiece2);
					if (dictionary2.TryGetValue(key, out var value))
					{
						dictionary[value] = dictionary.GetValueOrDefault(value, 1) + 1;
						break;
					}
					dictionary2[key] = clickyWorkPiece2.Id;
					dictionary[clickyWorkPiece2.Id] = 1;
				}
				list.Add(clickyWorkPiece2);
				break;
			}
		}
		OpenQuestion = clickyWorkPiece;
		Made = list;
		QuickAnswers = list2;
		RoutineRunCounts = dictionary.Where((KeyValuePair<string, int> kv) => kv.Value > 1).ToDictionary((KeyValuePair<string, int> kv) => kv.Key, (KeyValuePair<string, int> kv) => kv.Value);
	}

	public static Kind KindOf(ClickyWorkPiece piece, bool isFollowUp)
	{
		if (piece.Failed)
		{
			return Kind.QuickAnswer;
		}
		if (piece.ArtifactPaths.Count > 0 || piece.IsRoutineRun)
		{
			return Kind.Made;
		}
		int length = piece.Reply.Length;
		if (EndsWithQuestionToUser(piece.Reply) && (length < 700 || QuestionLineCount(piece.Reply) >= 2))
		{
			return Kind.Question;
		}
		if (length >= (isFollowUp ? 1600 : 160))
		{
			return Kind.Made;
		}
		return Kind.QuickAnswer;
	}

	public static string RoutineKey(ClickyWorkPiece piece)
	{
		return Regex.Replace(piece.Ask, "\\s*\\u00B7\\s*run \\d+$", "").ToLowerInvariant();
	}

	private static List<string> LastLines(string text, int count)
	{
		return (from l in text.Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/))
			select l.Trim().Trim('*', '_', ' ') into l
			where l.Length > 0
			select l).TakeLast(count).ToList();
	}

	private static bool IsQuestionLine(string line)
	{
		if (!line.EndsWith('?') && !line.EndsWith('？'))
		{
			return line.EndsWith('؟');
		}
		return true;
	}

	private static bool EndsWithQuestionToUser(string text)
	{
		string text2 = LastLines(text, 1).FirstOrDefault();
		if (text2 != null)
		{
			return IsQuestionLine(text2);
		}
		return false;
	}

	private static int QuestionLineCount(string text)
	{
		return LastLines(text, 6).Count(IsQuestionLine);
	}

	public static string QuestionText(ClickyWorkPiece piece)
	{
		IEnumerable<string> values = from line in LastLines(piece.Reply, 6).Where(IsQuestionLine).Take(3)
			select Regex.Replace(Regex.Replace(line, "^([-*•]|\\d+[.)])\\s*", ""), "[*_`]", "");
		string text = string.Join("\n", values);
		if (text.Length != 0)
		{
			return text;
		}
		return ClickyWorkReplyParts.FirstSentence(piece.Reply);
	}
}

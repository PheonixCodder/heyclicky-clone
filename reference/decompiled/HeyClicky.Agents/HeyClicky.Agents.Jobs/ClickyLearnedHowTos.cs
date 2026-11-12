using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyLearnedHowTos
{
	private const string DefaultsKey = "clicky.jobs.learnedHowTos.v1";

	public const int MaximumKept = 60;

	public const int NewestCarried = 10;

	public const int RelatedCarried = 5;

	public const int MaximumLineLength = 240;

	private readonly object gate = new object();

	private List<ClickyLearnedHowTo> howTos = AgentDefaults.Get<List<ClickyLearnedHowTo>>("clicky.jobs.learnedHowTos.v1") ?? new List<ClickyLearnedHowTo>();

	private static readonly HashSet<string> IgnoredWords = new HashSet<string>
	{
		"the", "and", "for", "with", "from", "that", "this", "what", "when", "where",
		"which", "your", "their", "have", "has", "was", "are", "can", "you", "my",
		"me", "it", "is", "in", "on", "of", "to", "a", "an", "or",
		"at", "by", "use", "used", "search", "find", "get", "next", "latest"
	};

	public IReadOnlyList<ClickyLearnedHowTo> HowTos
	{
		get
		{
			lock (gate)
			{
				return howTos.ToList();
			}
		}
	}

	public static IReadOnlyList<string> Lines(string? block)
	{
		if (block == null)
		{
			return Array.Empty<string>();
		}
		return (from l in (from l in block.Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/))
				select l.Trim().Trim('-', '*', '•', ' ', '\t').Trim() into l
				where l.Length > 0
				select l).Take(3)
			select (l.Length <= 240) ? l : l.Substring(0, 240)).ToList();
	}

	public void Record(IReadOnlyList<string> lines, string? jobId)
	{
		lock (gate)
		{
			List<string> list = lines.Where((string line) => !howTos.Any((ClickyLearnedHowTo h) => SameHowTo(h.Text, line))).ToList();
			if (list.Count != 0)
			{
				howTos.AddRange(list.Select((string t) => new ClickyLearnedHowTo(t, DateTimeOffset.Now, jobId)));
				if (howTos.Count > 60)
				{
					howTos.RemoveRange(0, howTos.Count - 60);
				}
				AgentDefaults.Set("clicky.jobs.learnedHowTos.v1", howTos);
			}
		}
	}

	public string PromptSection(string ask)
	{
		IReadOnlyList<string> readOnlyList = Choose(HowTos.Select((ClickyLearnedHowTo h) => h.Text).ToList(), ask);
		if (readOnlyList.Count == 0)
		{
			return "";
		}
		return "\nWhat earlier jobs learned about this user's setup (use what fits; it may be out of date):\n" + string.Join("\n", readOnlyList.Select((string c) => "- " + c));
	}

	public void RemoveAllForSignOut()
	{
		lock (gate)
		{
			howTos = new List<ClickyLearnedHowTo>();
		}
		AgentDefaults.Remove("clicky.jobs.learnedHowTos.v1");
	}

	public static IReadOnlyList<string> Choose(IReadOnlyList<string> texts, string ask)
	{
		List<string> second = texts.Skip(Math.Max(0, texts.Count - 10)).ToList();
		List<string> source = texts.Take(Math.Max(0, texts.Count - 10)).ToList();
		HashSet<string> askWords = Words(ask);
		return (from x in (from t in Enumerable.Reverse(source)
				select (Text: t, Shared: Words(t).Intersect(askWords).Count()) into x
				where x.Shared > 0
				orderby x.Shared descending
				select x).Take(5)
			select x.Text).Reverse().ToList().Concat(second)
			.ToList();
	}

	private static HashSet<string> Words(string text)
	{
		HashSet<string> words = new HashSet<string>();
		StringBuilder current = new StringBuilder();
		string text2 = text.ToLowerInvariant();
		foreach (char c in text2)
		{
			if (char.IsLetterOrDigit(c))
			{
				current.Append(c);
			}
			else
			{
				Flush();
			}
		}
		Flush();
		return words;
		void Flush()
		{
			if (current.Length >= 3 && !IgnoredWords.Contains(current.ToString()))
			{
				words.Add(current.ToString());
			}
			current.Clear();
		}
	}

	private static bool SameHowTo(string a, string b)
	{
		return Normalize(a) == Normalize(b);
		static string Normalize(string s)
		{
			return new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
		}
	}
}

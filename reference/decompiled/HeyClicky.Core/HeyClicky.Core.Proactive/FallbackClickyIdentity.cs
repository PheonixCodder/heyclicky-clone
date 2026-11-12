using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HeyClicky.Core.Proactive;

public static class FallbackClickyIdentity
{
	private static readonly string[] Family = new string[6] { "Buddy", "Scout", "Desk", "Keeper", "Pal", "Lab" };

	private static readonly HashSet<string> TaskVerbs = new HashSet<string>
	{
		"run", "check", "draft", "write", "build", "make", "create", "update", "send", "analyze",
		"analyse", "review", "summarize", "summarise", "find", "prepare", "plan", "turn", "set", "pull",
		"list", "sync", "clean", "fix", "monitor", "track", "watch", "scan", "audit", "compile",
		"collect", "gather", "organize", "organise", "research", "look", "dig", "map", "triage", "reply",
		"answer", "follow", "generate", "produce", "put", "get", "do"
	};

	private static readonly HashSet<string> Filler = new HashSet<string>
	{
		"the", "a", "an", "my", "your", "our", "for", "of", "to", "in",
		"on", "with", "and", "into", "from", "this", "that", "up", "out", "me",
		"you", "please", "agent", "clicky", "assistant", "can", "could", "would", "want", "like",
		"help"
	};

	private static bool IsTaskVerb(string word)
	{
		if (TaskVerbs.Contains(word))
		{
			return true;
		}
		if (!word.EndsWith("ing", StringComparison.Ordinal) || word.Length <= 4)
		{
			return false;
		}
		string text = word;
		string text2 = text.Substring(0, text.Length - 3);
		if (TaskVerbs.Contains(text2) || TaskVerbs.Contains(text2 + "e"))
		{
			return true;
		}
		if (text2.Length >= 2)
		{
			if (text2[text2.Length - 1] == text2[text2.Length - 2])
			{
				HashSet<string> taskVerbs = TaskVerbs;
				text = text2;
				return taskVerbs.Contains(text.Substring(0, text.Length - 1));
			}
		}
		return false;
	}

	public static (string Name, string Role) For(string title)
	{
		List<string> list = (from w in (from w in Regex.Split(title, "[^A-Za-z0-9]+")
				where w.Length > 0
				select w).ToList()
			where !IsTaskVerb(w.ToLowerInvariant()) && !Filler.Contains(w.ToLowerInvariant())
			select w).ToList();
		string obj = ((list.Count > 0) ? TitleCased(list[0]) : "Task");
		int num = obj.ToLowerInvariant().Sum((char ch) => ch);
		string item = obj + " " + Family[num % Family.Length];
		List<string> list2 = list.Take(3).Select(TitleCased).ToList();
		return (Name: item, Role: (list2.Count == 0) ? "Agent" : string.Join(" ", list2));
		static string TitleCased(string w)
		{
			bool flag;
			switch (w.ToLowerInvariant())
			{
			case "pdf":
			case "csv":
			case "seo":
			case "ads":
			case "api":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				if (!(w == w.ToLowerInvariant()))
				{
					return w;
				}
				return char.ToUpperInvariant(w[0]) + w.Substring(1, w.Length - 1);
			}
			return w.ToUpperInvariant();
		}
	}

	public static string NormalizedName(string name)
	{
		return new string(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
	}
}

using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Settings;

namespace HeyClicky.App.Wiring;

public static class DeveloperChatModels
{
	private static readonly string[] ModelIds = new string[7] { "gpt-6.1-sol", "gpt-6-sol", "gpt-6-luna", "gpt-5.6-luna", "gpt-5.6-terra", "gpt-5.6-sol", "gpt-6-astra" };

	private static readonly string[] AllEfforts = new string[6] { "none", "low", "medium", "high", "xhigh", "max" };

	private static IReadOnlyList<string> EffortsFor(string modelId)
	{
		if ((!(modelId == "gpt-6-astra") && !(modelId == "gpt-6.1-sol")) || 1 == 0)
		{
			return AllEfforts;
		}
		return AllEfforts.Where((string e) => e != "none").ToArray();
	}

	private static string Key(string email, string slug)
	{
		return email.Trim().ToLowerInvariant() + "|" + slug;
	}

	public static (string Model, string Effort)? Load(SettingsStore store, string email, string slug)
	{
		string text = store.Read((AppSettings s) => s.DeveloperChatModels?.GetValueOrDefault(Key(email, slug)));
		if (text == null)
		{
			return null;
		}
		int num = text.IndexOf('|');
		string text5;
		string text6;
		if (num >= 0)
		{
			string text2 = text.Substring(0, num);
			string text3 = text;
			int num2 = num + 1;
			string text4 = text3.Substring(num2, text3.Length - num2);
			text5 = text4;
			text6 = text2;
		}
		else
		{
			text5 = "high";
			text6 = text;
		}
		if (!ModelIds.Contains(text6))
		{
			return null;
		}
		return (text6, EffortsFor(text6).Contains(text5) ? text5 : "low");
	}
}

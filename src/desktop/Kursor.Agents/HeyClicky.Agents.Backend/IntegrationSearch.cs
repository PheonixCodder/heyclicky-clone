using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Backend;

public static class IntegrationSearch
{
	private static readonly string[] PreferredOrder = new string[11]
	{
		"notion", "linear", "github", "googledocs", "googlecalendar", "linkedin", "slack", "gmail", "googlesheets", "googledrive",
		"obsidian"
	};

	public static int? RecommendedRank(AgentIntegration integration)
	{
		for (int i = 0; i < PreferredOrder.Length; i++)
		{
			if (string.Equals(integration.Id, PreferredOrder[i], StringComparison.OrdinalIgnoreCase) || string.Equals(integration.PrimaryToolkitSlug, PreferredOrder[i], StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return null;
	}

	public static int Compare(AgentIntegration left, AgentIntegration right)
	{
		int? num = RecommendedRank(left);
		int? num2 = RecommendedRank(right);
		int? num3 = num;
		if (num3.HasValue)
		{
			int valueOrDefault = num3.GetValueOrDefault();
			if (num2.HasValue)
			{
				int valueOrDefault2 = num2.GetValueOrDefault();
				if (valueOrDefault != valueOrDefault2)
				{
					return valueOrDefault.CompareTo(valueOrDefault2);
				}
			}
		}
		if (num3.HasValue && !num2.HasValue)
		{
			return -1;
		}
		if (!num3.HasValue && num2.HasValue)
		{
			return 1;
		}
		if (left.IsActiveConnection != right.IsActiveConnection)
		{
			if (!left.IsActiveConnection)
			{
				return 1;
			}
			return -1;
		}
		return string.Compare(left.DisplayName, right.DisplayName, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
	}

	public static List<AgentIntegration> Sorted(IEnumerable<AgentIntegration> integrations)
	{
		List<AgentIntegration> list = integrations.ToList();
		list.Sort(Compare);
		return list;
	}

	public static bool Matches(AgentIntegration integration, string query)
	{
		string text = Normalize(query);
		string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return true;
		}
		string value = text.Replace(" ", "");
		foreach (string item in new string[4] { integration.DisplayName, integration.Description, integration.Id, integration.PrimaryToolkitSlug }.Concat(integration.UseCases).Concat(integration.Toolkits.SelectMany((AgentIntegrationToolkit t) => new string[2] { t.Slug, t.Name })))
		{
			string text2 = Normalize(item);
			if (text2.Length != 0)
			{
				if (text2.Contains(text, StringComparison.Ordinal))
				{
					return true;
				}
				if (text2.Replace(" ", "").Contains(value, StringComparison.Ordinal))
				{
					return true;
				}
				string[] textTokens = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (array.All((string queryToken) => textTokens.Any((string textToken) => textToken.StartsWith(queryToken, StringComparison.Ordinal) || (queryToken.Length >= 3 && Similarity(textToken, queryToken) >= ((queryToken.Length <= 4) ? 0.8 : 0.72)))))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static string Normalize(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		string text2 = text.Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder(text2.Length);
		string text3 = text2;
		foreach (char c in text3)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				stringBuilder.Append(c);
			}
		}
		return Regex.Replace(stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
	}

	public static double Similarity(string a, string b)
	{
		int num = Math.Max(a.Length, b.Length);
		if (num != 0)
		{
			return 1.0 - (double)LevenshteinDistance(a, b) / (double)num;
		}
		return 1.0;
	}

	public static int LevenshteinDistance(string source, string target)
	{
		if (source.Length == 0)
		{
			return target.Length;
		}
		if (target.Length == 0)
		{
			return source.Length;
		}
		int[] array = new int[target.Length + 1];
		int[] array2 = new int[target.Length + 1];
		for (int i = 0; i <= target.Length; i++)
		{
			array[i] = i;
		}
		for (int j = 1; j <= source.Length; j++)
		{
			array2[0] = j;
			for (int k = 1; k <= target.Length; k++)
			{
				int val = array[k] + 1;
				int val2 = array2[k - 1] + 1;
				int val3 = array[k - 1] + ((source[j - 1] != target[k - 1]) ? 1 : 0);
				array2[k] = Math.Min(Math.Min(val, val2), val3);
			}
			int[] array3 = array2;
			array2 = array;
			array = array3;
		}
		return array[target.Length];
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using HeyClicky.Agents;

namespace HeyClicky.App.Ink;

public static class ClickyNotePaperColors
{
	public static readonly ClickyPaperStock[] Choices = new ClickyPaperStock[7]
	{
		ClickyPaperStock.Sky,
		ClickyPaperStock.Blush,
		ClickyPaperStock.Butter,
		ClickyPaperStock.Mint,
		ClickyPaperStock.Periwinkle,
		ClickyPaperStock.Peach,
		ClickyPaperStock.Kraft
	};

	private const string DefaultsKey = "clicky.notePaperColors.v1";

	private static readonly string LegacyFilePath = AppPaths.Combine("note-paper-colors.json");

	private static readonly Lock Gate = new Lock();

	private static bool legacyFolded;

	private static Dictionary<string, string> Load()
	{
		Dictionary<string, string> dictionary = AgentDefaults.Get<Dictionary<string, string>>("clicky.notePaperColors.v1") ?? new Dictionary<string, string>();
		if (legacyFolded)
		{
			return dictionary;
		}
		legacyFolded = true;
		try
		{
			if (!File.Exists(LegacyFilePath))
			{
				return dictionary;
			}
			Dictionary<string, string>? obj = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(LegacyFilePath)) ?? new Dictionary<string, string>();
			int count = dictionary.Count;
			foreach (var (key, value) in obj)
			{
				dictionary.TryAdd(key, value);
			}
			if (dictionary.Count != count)
			{
				Save(dictionary);
			}
			File.Delete(LegacyFilePath);
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "old note paper colours not folded in: " + ex.Message);
		}
		return dictionary;
	}

	private static void Save(Dictionary<string, string> assignments)
	{
		AgentDefaults.Set("clicky.notePaperColors.v1", assignments);
	}

	public static void Adopt(string key, string sourceKey)
	{
		using (Gate.EnterScope())
		{
			Dictionary<string, string> dictionary = Load();
			if (dictionary.TryGetValue(sourceKey, out var value))
			{
				dictionary[key] = value;
				Save(dictionary);
			}
		}
	}

	public static ClickyPaperStock Stock(string key)
	{
		using (Gate.EnterScope())
		{
			Dictionary<string, string> dictionary = Load();
			if (dictionary.TryGetValue(key, out var value))
			{
				ClickyPaperStock? clickyPaperStock = ClickyPaperStockStyle.FromRawValue(value);
				if (clickyPaperStock.HasValue)
				{
					return clickyPaperStock.GetValueOrDefault();
				}
			}
			Dictionary<ClickyPaperStock, int> useCounts = Choices.ToDictionary((ClickyPaperStock c) => c, (ClickyPaperStock _) => 0);
			foreach (string value2 in dictionary.Values)
			{
				ClickyPaperStock? clickyPaperStock = ClickyPaperStockStyle.FromRawValue(value2);
				if (clickyPaperStock.HasValue)
				{
					ClickyPaperStock valueOrDefault = clickyPaperStock.GetValueOrDefault();
					if (useCounts.ContainsKey(valueOrDefault))
					{
						useCounts[valueOrDefault]++;
					}
				}
			}
			int fewest = useCounts.Values.DefaultIfEmpty(0).Min();
			ClickyPaperStock[] array = Choices.Where((ClickyPaperStock c) => useCounts[c] == fewest).ToArray();
			ClickyPaperStock clickyPaperStock2 = ((array.Length != 0) ? array[Random.Shared.Next(array.Length)] : ClickyPaperStock.Sky);
			dictionary[key] = clickyPaperStock2.RawValue();
			Save(dictionary);
			return clickyPaperStock2;
		}
	}
}

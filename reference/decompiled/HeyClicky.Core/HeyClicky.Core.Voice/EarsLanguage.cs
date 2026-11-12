using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HeyClicky.Core.Configuration;

namespace HeyClicky.Core.Voice;

public static class EarsLanguage
{
	public enum Script
	{
		Latin,
		Cyrillic,
		Greek,
		Arabic,
		Hebrew,
		Thaana,
		Devanagari,
		Bengali,
		Gurmukhi,
		Gujarati,
		Oriya,
		Tamil,
		Telugu,
		Kannada,
		Malayalam,
		Sinhala,
		Thai,
		Lao,
		Khmer,
		Myanmar,
		Georgian,
		Armenian,
		Ethiopic,
		Hangul,
		Kana,
		Han
	}

	public const int HeardScriptsKept = 30;

	public const int HistoryNeededForNoiseGuard = 10;

	public const int TurnsNeededToAcceptAScript = 2;

	private static readonly object storeGate = new object();

	private static List<List<string>>? cachedHistory;

	private static readonly HashSet<Script> ScriptsWrittenWithoutSpaces = new HashSet<Script>
	{
		Script.Han,
		Script.Kana,
		Script.Thai,
		Script.Lao,
		Script.Khmer,
		Script.Myanmar
	};

	private static readonly object droppedGate = new object();

	private static (HashSet<Script> Scripts, DateTime At)? lastDroppedPress;

	public static readonly TimeSpan RepeatWindow = TimeSpan.FromSeconds(180L);

	internal static string StorePath { get; set; } = Path.Combine(AppBundleConfiguration.AppDataDirectory, "OneBrainHeardScripts.json");

	public static string Code()
	{
		return "multi";
	}

	public static void RecordHeard(string words)
	{
		RecordHeardScripts(ScriptsIn(words));
	}

	private static void RecordHeardScripts(IReadOnlySet<Script> scripts)
	{
		if (scripts.Count == 0)
		{
			return;
		}
		lock (storeGate)
		{
			List<List<string>> list = LoadLocked();
			list.Add(scripts.Select((Script s) => s.ToString()).OrderBy<string, string>((string s) => s, StringComparer.Ordinal).ToList());
			if (list.Count > 30)
			{
				list.RemoveRange(0, list.Count - 30);
			}
			cachedHistory = list;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(StorePath));
				File.WriteAllText(StorePath, JsonSerializer.Serialize(list, EarsLanguageJson.Default.ListListString));
			}
			catch (Exception ex)
			{
				Log.Warn("Companion", "ears: the heard-scripts history wasn't saved: " + ex.Message);
			}
		}
	}

	public static IReadOnlyList<IReadOnlyList<string>> StoredHeardScripts()
	{
		lock (storeGate)
		{
			return ((IEnumerable<List<string>>)LoadLocked()).Select((Func<List<string>, IReadOnlyList<string>>)((List<string> t) => t.ToList())).ToList();
		}
	}

	internal static void ResetCacheForTests()
	{
		lock (storeGate)
		{
			cachedHistory = null;
		}
	}

	private static List<List<string>> LoadLocked()
	{
		List<List<string>> list = cachedHistory;
		if (list != null)
		{
			return list;
		}
		List<List<string>> result = new List<List<string>>();
		try
		{
			if (File.Exists(StorePath))
			{
				result = JsonSerializer.Deserialize(File.ReadAllText(StorePath), EarsLanguageJson.Default.ListListString) ?? new List<List<string>>();
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is JsonException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			result = new List<List<string>>();
		}
		cachedHistory = result;
		return result;
	}

	public static IReadOnlySet<Script> AcceptedScripts(IEnumerable<string> voiceLanguageCodes, string? dictationLanguageCode, IEnumerable<string> systemLanguageTags, IReadOnlyList<IReadOnlyList<string>>? heardScripts = null)
	{
		HashSet<Script> hashSet = new HashSet<Script> { Script.Latin };
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (IReadOnlyList<string> item in heardScripts ?? StoredHeardScripts())
		{
			foreach (string item2 in item.Distinct())
			{
				dictionary[item2] = dictionary.GetValueOrDefault(item2) + 1;
			}
		}
		foreach (var (value, num2) in dictionary)
		{
			if (num2 >= 2 && Enum.TryParse<Script>(value, out var result))
			{
				hashSet.Add(result);
			}
		}
		List<string> list = voiceLanguageCodes.Concat(systemLanguageTags).ToList();
		if (dictationLanguageCode != null && dictationLanguageCode.Length > 0)
		{
			if (dictationLanguageCode != "multi")
			{
				list.Add(dictationLanguageCode);
			}
		}
		foreach (string item3 in list)
		{
			hashSet.UnionWith(Scripts(BaseCode(item3)));
		}
		return hashSet;
	}

	private static string BaseCode(string code)
	{
		string text = code.Replace('_', '-');
		if (text.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return "zh";
		}
		return text.Split('-')[0].ToLowerInvariant();
	}

	public static IReadOnlySet<Script> Scripts(string code)
	{
		switch (code)
		{
		case "ar":
		case "fa":
		case "ur":
		case "ps":
		case "sd":
		case "ug":
		case "ku":
			return new HashSet<Script> { Script.Arabic };
		case "he":
		case "yi":
		case "iw":
			return new HashSet<Script> { Script.Hebrew };
		case "dv":
			return new HashSet<Script> { Script.Thaana };
		case "hi":
		case "mr":
		case "ne":
		case "sa":
			return new HashSet<Script> { Script.Devanagari };
		case "bn":
		case "as":
			return new HashSet<Script> { Script.Bengali };
		case "pa":
			return new HashSet<Script>
			{
				Script.Gurmukhi,
				Script.Arabic
			};
		case "gu":
			return new HashSet<Script> { Script.Gujarati };
		case "or":
			return new HashSet<Script> { Script.Oriya };
		case "ta":
			return new HashSet<Script> { Script.Tamil };
		case "te":
			return new HashSet<Script> { Script.Telugu };
		case "kn":
			return new HashSet<Script> { Script.Kannada };
		case "ml":
			return new HashSet<Script> { Script.Malayalam };
		case "si":
			return new HashSet<Script> { Script.Sinhala };
		case "th":
			return new HashSet<Script> { Script.Thai };
		case "lo":
			return new HashSet<Script> { Script.Lao };
		case "km":
			return new HashSet<Script> { Script.Khmer };
		case "my":
			return new HashSet<Script> { Script.Myanmar };
		case "ka":
			return new HashSet<Script> { Script.Georgian };
		case "hy":
			return new HashSet<Script> { Script.Armenian };
		case "am":
		case "ti":
			return new HashSet<Script> { Script.Ethiopic };
		case "ko":
			return new HashSet<Script>
			{
				Script.Hangul,
				Script.Han
			};
		case "ja":
			return new HashSet<Script>
			{
				Script.Kana,
				Script.Han
			};
		case "zh":
		case "yue":
			return new HashSet<Script> { Script.Han };
		case "ru":
		case "uk":
		case "be":
		case "bg":
		case "mk":
		case "kk":
		case "ky":
		case "mn":
		case "tg":
		case "sr":
			return new HashSet<Script> { Script.Cyrillic };
		case "el":
			return new HashSet<Script> { Script.Greek };
		default:
			return new HashSet<Script>();
		}
	}

	public static Script? ScriptOf(Rune rune)
	{
		UnicodeCategory unicodeCategory = Rune.GetUnicodeCategory(rune);
		bool flag = Rune.IsLetter(rune);
		if (!flag)
		{
			bool flag2 = (uint)(unicodeCategory - 5) <= 1u;
			flag = flag2;
		}
		if (!flag)
		{
			return null;
		}
		int value = rune.Value;
		if (value <= 2815)
		{
			if (value >= 1872)
			{
				if (value <= 2303)
				{
					if (value <= 1983)
					{
						if (value > 1919)
						{
							return Script.Thaana;
						}
					}
					else if (value < 2208)
					{
						goto IL_03d0;
					}
					goto IL_02e8;
				}
				if (value <= 2559)
				{
					if (value <= 2431)
					{
						return Script.Devanagari;
					}
					return Script.Bengali;
				}
				if (value <= 2687)
				{
					return Script.Gurmukhi;
				}
				return Script.Gujarati;
			}
			if (value <= 1327)
			{
				if (value >= 880)
				{
					if (value <= 1023)
					{
						return Script.Greek;
					}
					return Script.Cyrillic;
				}
				if (value >= 65 && value <= 591)
				{
					goto IL_02ab;
				}
			}
			else
			{
				if (value <= 1535)
				{
					if (value <= 1423)
					{
						return Script.Armenian;
					}
					return Script.Hebrew;
				}
				if (value <= 1791)
				{
					goto IL_02e8;
				}
			}
		}
		else if (value >= 7680)
		{
			if (value >= 19968)
			{
				if (value >= 64336)
				{
					if (value >= 65136)
					{
						if (value <= 65279)
						{
							goto IL_02e8;
						}
					}
					else if (value <= 65023)
					{
						goto IL_02e8;
					}
				}
				else if (value >= 44032)
				{
					if (value <= 55215)
					{
						goto IL_03b2;
					}
				}
				else if (value <= 40959)
				{
					goto IL_03c6;
				}
			}
			else if (value >= 12592)
			{
				if (value >= 13312)
				{
					if (value <= 19903)
					{
						goto IL_03c6;
					}
				}
				else if (value <= 12687)
				{
					goto IL_03b2;
				}
			}
			else if (value >= 12352)
			{
				if (value <= 12543)
				{
					return Script.Kana;
				}
			}
			else if (value <= 7935)
			{
				goto IL_02ab;
			}
		}
		else
		{
			if (value <= 3839)
			{
				if (value <= 3327)
				{
					if (value <= 3071)
					{
						if (value <= 2943)
						{
							return Script.Oriya;
						}
						return Script.Tamil;
					}
					if (value <= 3199)
					{
						return Script.Telugu;
					}
					return Script.Kannada;
				}
				if (value <= 3583)
				{
					if (value <= 3455)
					{
						return Script.Malayalam;
					}
					return Script.Sinhala;
				}
				if (value <= 3711)
				{
					return Script.Thai;
				}
				return Script.Lao;
			}
			if (value >= 4608)
			{
				if (value >= 6016)
				{
					if (value <= 6143)
					{
						return Script.Khmer;
					}
				}
				else if (value <= 5023)
				{
					return Script.Ethiopic;
				}
			}
			else
			{
				if (value > 4255)
				{
					if (value > 4351)
					{
						goto IL_03b2;
					}
					return Script.Georgian;
				}
				if (value >= 4096)
				{
					return Script.Myanmar;
				}
			}
		}
		goto IL_03d0;
		IL_03c6:
		return Script.Han;
		IL_03d0:
		return null;
		IL_03b2:
		return Script.Hangul;
		IL_02ab:
		return Script.Latin;
		IL_02e8:
		return Script.Arabic;
	}

	private static string[] Tokens(string words)
	{
		return words.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
	}

	public static HashSet<Script> ScriptsIn(string token)
	{
		HashSet<Script> hashSet = new HashSet<Script>();
		foreach (Rune item in token.EnumerateRunes())
		{
			Script? script = ScriptOf(item);
			if (script.HasValue)
			{
				Script valueOrDefault = script.GetValueOrDefault();
				hashSet.Add(valueOrDefault);
			}
		}
		return hashSet;
	}

	public static int WordCount(string token)
	{
		int num = 0;
		foreach (Rune item in token.EnumerateRunes())
		{
			Script? script = ScriptOf(item);
			if (script.HasValue)
			{
				Script valueOrDefault = script.GetValueOrDefault();
				if (ScriptsWrittenWithoutSpaces.Contains(valueOrDefault))
				{
					num++;
				}
			}
		}
		if (num != 0)
		{
			return Math.Max(1, (num + 1) / 2);
		}
		return 1;
	}

	public static string? GuardingNoise(string words, IReadOnlySet<Script> acceptedScripts, DateTime? now = null)
	{
		DateTime dateTime = now ?? DateTime.UtcNow;
		string text = RemovingStrayScript(words, acceptedScripts);
		HashSet<Script> scripts = null;
		lock (droppedGate)
		{
			if (text != null)
			{
				lastDroppedPress = null;
				return text;
			}
			HashSet<Script> hashSet = ScriptsIn(words);
			hashSet.ExceptWith(acceptedScripts);
			hashSet.Remove(Script.Latin);
			(HashSet<Script>, DateTime)? tuple = lastDroppedPress;
			if (!tuple.HasValue)
			{
				goto IL_00bd;
			}
			(HashSet<Script>, DateTime) valueOrDefault = tuple.GetValueOrDefault();
			if (!(dateTime - valueOrDefault.Item2 <= RepeatWindow) || !valueOrDefault.Item1.Overlaps(hashSet))
			{
				goto IL_00bd;
			}
			lastDroppedPress = null;
			(scripts, _) = valueOrDefault;
			goto end_IL_002e;
			IL_00bd:
			lastDroppedPress = (hashSet, dateTime);
			return null;
			end_IL_002e:;
		}
		RecordHeardScripts(scripts);
		return words;
	}

	internal static void ForgetDroppedPress()
	{
		lock (droppedGate)
		{
			lastDroppedPress = null;
		}
	}

	public static string? RemovingStrayScript(string words, IEnumerable<string> knownLanguageCodes)
	{
		HashSet<Script> hashSet = new HashSet<Script>();
		foreach (string knownLanguageCode in knownLanguageCodes)
		{
			hashSet.UnionWith(Scripts(knownLanguageCode));
		}
		return RemovingStrayScript(words, hashSet);
	}

	public static string? RemovingStrayScript(string words, IReadOnlySet<Script> acceptedScripts)
	{
		HashSet<Script> accepted = new HashSet<Script>(acceptedScripts) { Script.Latin };
		List<string> list = Tokens(words).ToList();
		int num = list.Count(IsStray);
		if (num == 0)
		{
			return words;
		}
		if (!list.Any((string t) => HasLetters(t) && !IsStray(t)))
		{
			if (list.Where(IsStray).Sum((Func<string, int>)WordCount) > 6)
			{
				return words;
			}
			return null;
		}
		int num2 = 0;
		while (list.Count > 0 && IsStray(list[0]))
		{
			list.RemoveAt(0);
			num2++;
		}
		while (list.Count > 0)
		{
			if (!IsStray(list[list.Count - 1]))
			{
				break;
			}
			list.RemoveAt(list.Count - 1);
			num2++;
		}
		if (num2 != num || num2 > 2)
		{
			return words;
		}
		return string.Join(" ", list);
		static bool HasLetters(string token)
		{
			return ScriptsIn(token).Count > 0;
		}
		bool IsStray(string token)
		{
			HashSet<Script> hashSet = ScriptsIn(token);
			if (hashSet.Count > 0)
			{
				return !hashSet.Overlaps(accepted);
			}
			return false;
		}
	}
}

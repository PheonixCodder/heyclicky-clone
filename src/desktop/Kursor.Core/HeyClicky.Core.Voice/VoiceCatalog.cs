using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace HeyClicky.Core.Voice;

public static class VoiceCatalog
{
	public const string GeminiPrefix = "gemini:";

	public static readonly IReadOnlyList<ExpressiveVoice> ExpressiveVoices = new global::_003C_003Ez__ReadOnlyArray<ExpressiveVoice>(new ExpressiveVoice[30]
	{
		new ExpressiveVoice("gemini:Puck", "Puck", "Upbeat", "male"),
		new ExpressiveVoice("gemini:Zephyr", "Zephyr", "Bright", "female"),
		new ExpressiveVoice("gemini:Aoede", "Aoede", "Breezy", "female"),
		new ExpressiveVoice("gemini:Fenrir", "Fenrir", "Excitable", "male"),
		new ExpressiveVoice("gemini:Leda", "Leda", "Youthful", "female"),
		new ExpressiveVoice("gemini:Sulafat", "Sulafat", "Warm", "female"),
		new ExpressiveVoice("gemini:Achird", "Achird", "Friendly", "male"),
		new ExpressiveVoice("gemini:Sadachbia", "Sadachbia", "Lively", "male"),
		new ExpressiveVoice("gemini:Kore", "Kore", "Firm", "female"),
		new ExpressiveVoice("gemini:Charon", "Charon", "Informative", "male"),
		new ExpressiveVoice("gemini:Orus", "Orus", "Firm", "male"),
		new ExpressiveVoice("gemini:Callirrhoe", "Callirrhoe", "Easy-going", "female"),
		new ExpressiveVoice("gemini:Autonoe", "Autonoe", "Bright", "female"),
		new ExpressiveVoice("gemini:Enceladus", "Enceladus", "Breathy", "male"),
		new ExpressiveVoice("gemini:Iapetus", "Iapetus", "Clear", "male"),
		new ExpressiveVoice("gemini:Umbriel", "Umbriel", "Easy-going", "male"),
		new ExpressiveVoice("gemini:Algieba", "Algieba", "Smooth", "male"),
		new ExpressiveVoice("gemini:Despina", "Despina", "Smooth", "female"),
		new ExpressiveVoice("gemini:Erinome", "Erinome", "Clear", "female"),
		new ExpressiveVoice("gemini:Algenib", "Algenib", "Gravelly", "male"),
		new ExpressiveVoice("gemini:Rasalgethi", "Rasalgethi", "Informative", "male"),
		new ExpressiveVoice("gemini:Laomedeia", "Laomedeia", "Upbeat", "female"),
		new ExpressiveVoice("gemini:Achernar", "Achernar", "Soft", "female"),
		new ExpressiveVoice("gemini:Alnilam", "Alnilam", "Firm", "male"),
		new ExpressiveVoice("gemini:Schedar", "Schedar", "Even", "male"),
		new ExpressiveVoice("gemini:Gacrux", "Gacrux", "Mature", "female"),
		new ExpressiveVoice("gemini:Pulcherrima", "Pulcherrima", "Forward", "female"),
		new ExpressiveVoice("gemini:Zubenelgenubi", "Zubenelgenubi", "Casual", "male"),
		new ExpressiveVoice("gemini:Vindemiatrix", "Vindemiatrix", "Gentle", "female"),
		new ExpressiveVoice("gemini:Sadaltager", "Sadaltager", "Knowledgeable", "male")
	});

	public static readonly IReadOnlyList<VoiceLanguage> Languages = new global::_003C_003Ez__ReadOnlyArray<VoiceLanguage>(new VoiceLanguage[133]
	{
		new VoiceLanguage("ace-Arab", "Acehnese (Arabic script)"),
		new VoiceLanguage("af", "Afrikaans"),
		new VoiceLanguage("ak", "Akan"),
		new VoiceLanguage("sq", "Albanian"),
		new VoiceLanguage("am", "Amharic"),
		new VoiceLanguage("ar", "Arabic"),
		new VoiceLanguage("ar-Latn", "Arabic (Latin script)"),
		new VoiceLanguage("hy", "Armenian"),
		new VoiceLanguage("as", "Assamese"),
		new VoiceLanguage("awa", "Awadhi"),
		new VoiceLanguage("az", "Azerbaijani"),
		new VoiceLanguage("ban", "Balinese"),
		new VoiceLanguage("bjn-Latn", "Banjar"),
		new VoiceLanguage("bjn-Arab", "Banjar (Arabic script)"),
		new VoiceLanguage("ba", "Bashkir"),
		new VoiceLanguage("eu", "Basque"),
		new VoiceLanguage("be", "Belarusian"),
		new VoiceLanguage("bem", "Bemba"),
		new VoiceLanguage("bn", "Bengali"),
		new VoiceLanguage("bho", "Bhojpuri"),
		new VoiceLanguage("bs", "Bosnian"),
		new VoiceLanguage("bug", "Buginese"),
		new VoiceLanguage("bg", "Bulgarian"),
		new VoiceLanguage("my", "Burmese"),
		new VoiceLanguage("yue", "Cantonese"),
		new VoiceLanguage("ca", "Catalan"),
		new VoiceLanguage("ceb", "Cebuano"),
		new VoiceLanguage("ckb", "Central Kurdish"),
		new VoiceLanguage("hne", "Chhattisgarhi"),
		new VoiceLanguage("zh-Hans", "Chinese (Simplified)"),
		new VoiceLanguage("zh-Hant", "Chinese (Traditional)"),
		new VoiceLanguage("crh", "Crimean Tatar"),
		new VoiceLanguage("hr", "Croatian"),
		new VoiceLanguage("cs", "Czech"),
		new VoiceLanguage("da", "Danish"),
		new VoiceLanguage("prs", "Dari"),
		new VoiceLanguage("nl", "Dutch"),
		new VoiceLanguage("dyu", "Dyula"),
		new VoiceLanguage("dz", "Dzongkha"),
		new VoiceLanguage("arz", "Egyptian Arabic"),
		new VoiceLanguage("en", "English"),
		new VoiceLanguage("et", "Estonian"),
		new VoiceLanguage("fil", "Filipino"),
		new VoiceLanguage("fi", "Finnish"),
		new VoiceLanguage("fr", "French"),
		new VoiceLanguage("fuv", "Fulfulde"),
		new VoiceLanguage("gl", "Galician"),
		new VoiceLanguage("lg", "Ganda"),
		new VoiceLanguage("ka", "Georgian"),
		new VoiceLanguage("de", "German"),
		new VoiceLanguage("el", "Greek"),
		new VoiceLanguage("gn", "Guarani"),
		new VoiceLanguage("gu", "Gujarati"),
		new VoiceLanguage("ht", "Haitian Creole"),
		new VoiceLanguage("ha", "Hausa"),
		new VoiceLanguage("he", "Hebrew"),
		new VoiceLanguage("hi", "Hindi"),
		new VoiceLanguage("hu", "Hungarian"),
		new VoiceLanguage("is", "Icelandic"),
		new VoiceLanguage("ig", "Igbo"),
		new VoiceLanguage("ilo", "Ilocano"),
		new VoiceLanguage("id", "Indonesian"),
		new VoiceLanguage("it", "Italian"),
		new VoiceLanguage("ja", "Japanese"),
		new VoiceLanguage("jv", "Javanese"),
		new VoiceLanguage("kab", "Kabyle"),
		new VoiceLanguage("kam", "Kamba"),
		new VoiceLanguage("kn", "Kannada"),
		new VoiceLanguage("ks-Arab", "Kashmiri (Arabic script)"),
		new VoiceLanguage("ks-Deva", "Kashmiri (Devanagari)"),
		new VoiceLanguage("kk", "Kazakh"),
		new VoiceLanguage("km", "Khmer"),
		new VoiceLanguage("ki", "Kikuyu"),
		new VoiceLanguage("rw", "Kinyarwanda"),
		new VoiceLanguage("kg", "Kongo"),
		new VoiceLanguage("ko", "Korean"),
		new VoiceLanguage("ky", "Kyrgyz"),
		new VoiceLanguage("lo", "Lao"),
		new VoiceLanguage("ltg", "Latgalian"),
		new VoiceLanguage("lv", "Latvian"),
		new VoiceLanguage("ln", "Lingala"),
		new VoiceLanguage("lt", "Lithuanian"),
		new VoiceLanguage("lb", "Luxembourgish"),
		new VoiceLanguage("mk", "Macedonian"),
		new VoiceLanguage("mag", "Magahi"),
		new VoiceLanguage("mai", "Maithili"),
		new VoiceLanguage("ms", "Malay"),
		new VoiceLanguage("ml", "Malayalam"),
		new VoiceLanguage("mt", "Maltese"),
		new VoiceLanguage("mni", "Manipuri"),
		new VoiceLanguage("mr", "Marathi"),
		new VoiceLanguage("min", "Minangkabau"),
		new VoiceLanguage("min-Arab", "Minangkabau (Arabic script)"),
		new VoiceLanguage("lus", "Mizo"),
		new VoiceLanguage("mn", "Mongolian"),
		new VoiceLanguage("ne", "Nepali"),
		new VoiceLanguage("nso", "Northern Sotho"),
		new VoiceLanguage("nb", "Norwegian Bokmål"),
		new VoiceLanguage("nn", "Norwegian Nynorsk"),
		new VoiceLanguage("ny", "Nyanja"),
		new VoiceLanguage("oc", "Occitan"),
		new VoiceLanguage("or", "Odia"),
		new VoiceLanguage("pag", "Pangasinan"),
		new VoiceLanguage("ps", "Pashto"),
		new VoiceLanguage("fa", "Persian"),
		new VoiceLanguage("pl", "Polish"),
		new VoiceLanguage("pt", "Portuguese"),
		new VoiceLanguage("pa", "Punjabi"),
		new VoiceLanguage("ro", "Romanian"),
		new VoiceLanguage("ru", "Russian"),
		new VoiceLanguage("sat", "Santali"),
		new VoiceLanguage("sr", "Serbian"),
		new VoiceLanguage("sd", "Sindhi"),
		new VoiceLanguage("si", "Sinhala"),
		new VoiceLanguage("sk", "Slovak"),
		new VoiceLanguage("sl", "Slovenian"),
		new VoiceLanguage("so", "Somali"),
		new VoiceLanguage("azb", "South Azerbaijani"),
		new VoiceLanguage("st", "Southern Sotho"),
		new VoiceLanguage("es", "Spanish"),
		new VoiceLanguage("sw", "Swahili"),
		new VoiceLanguage("ss", "Swati"),
		new VoiceLanguage("sv", "Swedish"),
		new VoiceLanguage("tg", "Tajik"),
		new VoiceLanguage("ta", "Tamil"),
		new VoiceLanguage("te", "Telugu"),
		new VoiceLanguage("th", "Thai"),
		new VoiceLanguage("ti", "Tigrinya"),
		new VoiceLanguage("tr", "Turkish"),
		new VoiceLanguage("ur", "Urdu"),
		new VoiceLanguage("ug", "Uyghur"),
		new VoiceLanguage("uz", "Uzbek"),
		new VoiceLanguage("vi", "Vietnamese")
	});

	public static readonly IReadOnlyList<string> PopularCodes = new global::_003C_003Ez__ReadOnlyArray<string>(new string[12]
	{
		"en", "es", "fr", "de", "pt", "it", "hi", "ja", "ko", "zh-Hans",
		"ar", "ru"
	});

	private static readonly Dictionary<string, string> NativeNames = new Dictionary<string, string>
	{
		["sq"] = "Shqip",
		["am"] = "አማርኛ",
		["ar"] = "العربية",
		["hy"] = "Հայերեն",
		["as"] = "অসম\u09c0য\u09bc\u09be",
		["az"] = "Azərbaycan",
		["ba"] = "Башҡорт",
		["eu"] = "Euskara",
		["be"] = "Беларуская",
		["bem"] = "Ichibemba",
		["bn"] = "ব\u09be\u0982ল\u09be",
		["bho"] = "भ\u094bजप\u0941र\u0940",
		["bs"] = "Bosanski",
		["bg"] = "Български",
		["my"] = "မ\u103cန\u103aမ\u102c",
		["yue"] = "廣東話",
		["ca"] = "Català",
		["ckb"] = "کوردیی ناوەندی",
		["zh-Hans"] = "简体中文",
		["zh-Hant"] = "繁體中文",
		["hr"] = "Hrvatski",
		["cs"] = "Čeština",
		["da"] = "Dansk",
		["nl"] = "Nederlands",
		["dz"] = "ར\u0fab\u0f7cང་ཁ་",
		["et"] = "Eesti",
		["fi"] = "Suomi",
		["fr"] = "Français",
		["gl"] = "Galego",
		["lg"] = "Luganda",
		["ka"] = "ქართული",
		["de"] = "Deutsch",
		["el"] = "Ελληνικά",
		["gn"] = "Avañe’ẽ",
		["gu"] = "ગ\u0ac1જર\u0abeત\u0ac0",
		["ht"] = "Kreyòl Ayisyen",
		["he"] = "עברית",
		["hi"] = "ह\u093fन\u094dद\u0940",
		["hu"] = "Magyar",
		["is"] = "Íslenska",
		["id"] = "Indonesia",
		["it"] = "Italiano",
		["ja"] = "日本語",
		["jv"] = "Jawa",
		["kab"] = "Taqbaylit",
		["kam"] = "Kikamba",
		["kn"] = "ಕನ\u0ccdನಡ",
		["ks-Arab"] = "کٲش\u064fر (نسخ)",
		["ks-Deva"] = "क\u0949श\u0941र (द\u0947वन\u093eगर\u0940)",
		["kk"] = "Қазақ тілі",
		["km"] = "ខ\u17d2ម\u17c2រ",
		["ki"] = "Gikuyu",
		["rw"] = "Ikinyarwanda",
		["ko"] = "한국어",
		["ky"] = "Кыргызча",
		["lo"] = "ລາວ",
		["lv"] = "Latviešu",
		["ln"] = "Lingála",
		["lt"] = "Lietuvių",
		["lb"] = "Lëtzebuergesch",
		["mk"] = "Македонски",
		["mai"] = "म\u0948थ\u093fल\u0940",
		["ms"] = "Bahasa Melayu",
		["ml"] = "മലയ\u0d3eള\u0d02",
		["mt"] = "Malti",
		["mni"] = "ম\u09c8ত\u09c8ল\u09cbন\u09cd",
		["mr"] = "मर\u093eठ\u0940",
		["mn"] = "Монгол",
		["ne"] = "न\u0947प\u093eल\u0940",
		["nso"] = "Sesotho sa Leboa",
		["nb"] = "Norsk bokmål",
		["nn"] = "Norsk nynorsk",
		["or"] = "ଓଡ\u0b3c\u0b3fଆ",
		["ps"] = "پښتو",
		["fa"] = "فارسی",
		["pl"] = "Polski",
		["pt"] = "Português",
		["pa"] = "ਪ\u0a70ਜ\u0a3eਬ\u0a40",
		["ro"] = "Română",
		["ru"] = "Русский",
		["sat"] = "ᱥᱟᱱᱛᱟᱲᱤ",
		["sr"] = "Српски",
		["sd"] = "سنڌي",
		["si"] = "ස\u0dd2\u0d82හල",
		["sk"] = "Slovenčina",
		["sl"] = "Slovenščina",
		["so"] = "Soomaali",
		["st"] = "Sesotho",
		["es"] = "Español",
		["sw"] = "Kiswahili",
		["ss"] = "siSwati",
		["sv"] = "Svenska",
		["tg"] = "Тоҷикӣ",
		["ta"] = "தம\u0bbfழ\u0bcd",
		["te"] = "త\u0c46ల\u0c41గ\u0c41",
		["th"] = "ไทย",
		["ti"] = "ትግርኛ",
		["tr"] = "Türkçe",
		["ur"] = "اردو",
		["ug"] = "ئۇيغۇرچە",
		["uz"] = "O‘zbek",
		["vi"] = "Tiếng Việt"
	};

	public const int MaxPickedLanguages = 5;

	private static readonly Dictionary<string, string> TagAliases = new Dictionary<string, string>
	{
		["no"] = "nb",
		["tl"] = "fil",
		["iw"] = "he",
		["in"] = "id",
		["zh"] = "zh-Hans"
	};

	public static bool IsGemini(string? voiceId)
	{
		return voiceId?.StartsWith("gemini:", StringComparison.Ordinal) ?? false;
	}

	public static string GeminiName(string voiceId)
	{
		if (!IsGemini(voiceId))
		{
			return voiceId;
		}
		int length = "gemini:".Length;
		return voiceId.Substring(length, voiceId.Length - length);
	}

	public static ExpressiveVoice? Expressive(string? voiceId)
	{
		return ExpressiveVoices.FirstOrDefault((ExpressiveVoice v) => v.Id == voiceId);
	}

	public static string OpenAIVoice(string voiceId)
	{
		if (IsGemini(voiceId))
		{
			if (!(Expressive(voiceId)?.Gender == "female"))
			{
				return "cedar";
			}
			return "marin";
		}
		return voiceId;
	}

	public static VoiceLanguage? Language(string? code)
	{
		if (code != null)
		{
			return Languages.FirstOrDefault((VoiceLanguage l) => l.Code == code);
		}
		return null;
	}

	public static string? NativeName(string code)
	{
		return NativeNames.GetValueOrDefault(code);
	}

	public static IReadOnlyList<VoiceLanguage> PickerList(string? search = null)
	{
		IEnumerable<VoiceLanguage> source = PopularCodes.Select(Language).OfType<VoiceLanguage>().Concat(Languages.Where((VoiceLanguage l) => !PopularCodes.Contains(l.Code)));
		string query = Fold(search?.Trim() ?? "");
		if (query.Length != 0)
		{
			return source.Where(delegate(VoiceLanguage l)
			{
				if (!Fold(l.Name).Contains(query, StringComparison.Ordinal) && !Fold(l.Code).StartsWith(query, StringComparison.Ordinal))
				{
					string nativeName = l.NativeName;
					if (nativeName != null)
					{
						return Fold(nativeName).Contains(query, StringComparison.Ordinal);
					}
					return false;
				}
				return true;
			}).ToList();
		}
		return source.ToList();
	}

	private static string Fold(string text)
	{
		return new string((from c in text.ToLowerInvariant().Normalize(NormalizationForm.FormD)
			where CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
			select c).ToArray());
	}

	public static bool IsEnglish(string? code)
	{
		return code == "en";
	}

	public static IReadOnlyList<string> PickedLanguages(string? stored)
	{
		List<string> list = new List<string>();
		string[] array = (stored ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (string text in array)
		{
			if ((object)Language(text) != null && !list.Contains(text))
			{
				list.Add(text);
				if (list.Count == 5)
				{
					break;
				}
			}
		}
		return list;
	}

	public static string? StoredLanguages(IEnumerable<string> codes)
	{
		IReadOnlyList<string> readOnlyList = PickedLanguages(string.Join(",", codes));
		if (readOnlyList.Count != 0)
		{
			return string.Join(",", readOnlyList);
		}
		return null;
	}

	public static string SpokenList(IReadOnlyList<string> codes)
	{
		List<string> list = codes.Select((string c) => Language(c)?.Name).OfType<string>().ToList();
		if (list.Count > 1)
		{
			return string.Join(", ", list.SkipLast(1)) + " and " + list[list.Count - 1];
		}
		return list.FirstOrDefault() ?? "";
	}

	public static VoiceLanguage? LanguageForPreviews(string? stored)
	{
		return (from c in PickedLanguages(stored)
			where !IsEnglish(c)
			select c).Select(Language).FirstOrDefault();
	}

	public static string? CatalogCode(string? tag)
	{
		string[] array = (tag ?? "").Replace('_', '-').Split('-', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			return null;
		}
		string text = array[0].ToLowerInvariant();
		string text2 = array.Skip(1).FirstOrDefault((string p) => p.Length == 4);
		if (text2 != null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 3);
			defaultInterpolatedStringHandler.AppendFormatted(text);
			defaultInterpolatedStringHandler.AppendLiteral("-");
			defaultInterpolatedStringHandler.AppendFormatted(char.ToUpperInvariant(text2[0]));
			string text3 = text2;
			defaultInterpolatedStringHandler.AppendFormatted(text3.Substring(1, text3.Length - 1).ToLowerInvariant());
			string text4 = defaultInterpolatedStringHandler.ToStringAndClear();
			if ((object)Language(text4) != null)
			{
				return text4;
			}
		}
		if (text == "zh" && array.Skip(1).Any(delegate(string p)
		{
			switch (p.ToUpperInvariant())
			{
			case "TW":
			case "HK":
			case "MO":
				return true;
			default:
				return false;
			}
		}))
		{
			return "zh-Hant";
		}
		if ((object)Language(text) != null)
		{
			return text;
		}
		if (!TagAliases.TryGetValue(text, out string value) || (object)Language(value) == null)
		{
			return null;
		}
		return value;
	}

	public static IReadOnlyList<string> SystemLanguageCodes(IEnumerable<string> tags)
	{
		return tags.Select(CatalogCode).OfType<string>().Distinct()
			.ToList();
	}

	public static string? SpokenLanguageNote(string? stored)
	{
		IReadOnlyList<string> readOnlyList = PickedLanguages(stored);
		if (readOnlyList.Count != 0)
		{
			VoiceLanguage voiceLanguage = Language(readOnlyList[0]);
			if ((object)voiceLanguage != null)
			{
				if (readOnlyList.Count != 1)
				{
					return $"[Language] They picked {SpokenList(readOnlyList)} in Settings → Voice: write every say() line and every word you draw in whichever of these they are using with you, mixing them the way they do. If they use another language, answer in {voiceLanguage.Name}.";
				}
				return $"[Language] They picked {voiceLanguage.Name} in Settings → Voice: write every say() line and every word you draw in {voiceLanguage.Name}, whatever language they speak or type to you in.";
			}
		}
		return null;
	}
}

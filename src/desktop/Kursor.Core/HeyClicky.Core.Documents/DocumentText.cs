using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using HeyClicky.Core.Voice;

namespace HeyClicky.Core.Documents;

public static class DocumentText
{
	public const int MaximumCharacters = 60000;

	private const int MaximumTextFileBytes = 2000000;

	private const long MaximumDocumentBytes = 67108864L;

	private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"txt", "text", "md", "markdown", "mdx", "csv", "tsv", "log", "env", "conf",
		"cfg", "ini", "toml", "yaml", "yml", "json", "jsonl", "xml", "html", "htm",
		"xhtml", "svg", "css", "scss", "sass", "less", "js", "mjs", "cjs", "jsx",
		"ts", "tsx", "py", "rb", "php", "java", "kt", "kts", "swift", "m",
		"mm", "c", "h", "cc", "cpp", "cxx", "hpp", "hh", "cs", "fs",
		"vb", "go", "rs", "lua", "pl", "r", "scala", "sql", "sh", "bash",
		"zsh", "fish", "ps1", "psm1", "bat", "cmd", "tex", "bib", "rst", "adoc",
		"org", "srt", "vtt", "properties", "gradle", "dart", "vue", "svelte", "graphql", "proto",
		"dockerfile", "makefile", "gitignore", "editorconfig", "csproj", "sln", "props"
	};

	private static readonly HashSet<string> SystemDocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "docx", "docm", "dotx", "odt", "rtf", "pptx", "xlsx" };

	public static bool LikelyYieldsText(string path)
	{
		string text = Path.GetExtension(path).TrimStart('.');
		if (!text.Equals("pdf", StringComparison.OrdinalIgnoreCase) && !SystemDocumentExtensions.Contains(text))
		{
			return TextExtensions.Contains(text);
		}
		return true;
	}

	public static PageContext Extract(string path, int maximumCharacters = 60000)
	{
		string text = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
		string kind = ((text == "pdf") ? "pdf" : "file");
		try
		{
			using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
			}
		}
		catch (UnauthorizedAccessException)
		{
			return new PageContext(kind, path, null, IsTruncated: false, null, AccessDenied: true);
		}
		catch (Exception)
		{
			return new PageContext(kind, path, null, IsTruncated: false, null, AccessDenied: false);
		}
		try
		{
			if (text == "pdf")
			{
				PdfText.Result result = PdfText.Extract(path, maximumCharacters);
				var (text2, flag) = Cap(result.Text, maximumCharacters);
				return new PageContext("pdf", path, text2, flag || result.StoppedEarly, result.PageCount, AccessDenied: false);
			}
			if (SystemDocumentExtensions.Contains(text))
			{
				string text3 = ReadSystemDocument(path, text);
				if (text3 != null)
				{
					var (text4, isTruncated) = Cap(text3, maximumCharacters);
					if (text4 != null)
					{
						return new PageContext("text_file", path, text4, isTruncated, null, AccessDenied: false);
					}
				}
			}
			if (TextExtensions.Contains(text) || (text.Length == 0 && LooksLikeText(path)))
			{
				long length = new FileInfo(path).Length;
				string text5 = Decode(ReadLeading(path, 2000000));
				if (text5 != null)
				{
					var (text6, flag2) = Cap(text5, maximumCharacters);
					if (text6 != null)
					{
						return new PageContext("text_file", path, text6, flag2 || length > 2000000, null, AccessDenied: false);
					}
				}
			}
		}
		catch (Exception ex3)
		{
			Log.Warn("DocumentText", Path.GetFileName(path) + ": " + ex3.Message);
		}
		return new PageContext(kind, path, null, IsTruncated: false, null, AccessDenied: false);
	}

	internal static (string? Text, bool Cut) Cap(string? raw, int maximum)
	{
		string text = raw?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return (Text: null, Cut: false);
		}
		if (text.Length <= maximum)
		{
			return (Text: text, Cut: false);
		}
		return (Text: text.Substring(0, maximum), Cut: true);
	}

	private static byte[] ReadLeading(string path, int maximum)
	{
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		int num = (int)Math.Min(maximum, fileStream.Length);
		byte[] array = new byte[num];
		int i;
		int num2;
		for (i = 0; i < num; i += num2)
		{
			num2 = fileStream.Read(array, i, num - i);
			if (num2 <= 0)
			{
				break;
			}
		}
		return (i == num) ? array : array[..i];
	}

	private static bool LooksLikeText(string path)
	{
		byte[] array = ReadLeading(path, 4096);
		if (array.Length != 0)
		{
			return Array.IndexOf(array, (byte)0) < 0;
		}
		return false;
	}

	internal static string? Decode(byte[] bytes)
	{
		if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
		{
			return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
		}
		if (bytes.Length >= 2 && bytes[0] == byte.MaxValue && bytes[1] == 254)
		{
			return Encoding.Unicode.GetString(bytes, 2, (bytes.Length - 2) & -2);
		}
		if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == byte.MaxValue)
		{
			return Encoding.BigEndianUnicode.GetString(bytes, 2, (bytes.Length - 2) & -2);
		}
		UTF8Encoding uTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
		for (int i = 0; i <= 3 && i < bytes.Length; i++)
		{
			try
			{
				return uTF8Encoding.GetString(bytes, 0, bytes.Length - i);
			}
			catch (DecoderFallbackException)
			{
			}
		}
		if (Array.IndexOf(bytes, (byte)0) >= 0)
		{
			return null;
		}
		return Encoding.Latin1.GetString(bytes);
	}

	private static string? ReadSystemDocument(string path, string extension)
	{
		if (extension == "rtf")
		{
			return RtfToText(Decode(ReadLeading(path, 16000000)) ?? "");
		}
		if (new FileInfo(path).Length > 67108864)
		{
			return null;
		}
		using ZipArchive zipArchive = ZipFile.OpenRead(path);
		switch (extension)
		{
		case "docx":
		case "docm":
		case "dotx":
		{
			ZipArchiveEntry entry2 = zipArchive.GetEntry("word/document.xml");
			return (entry2 != null) ? WordXmlToText(ReadEntry(entry2)) : null;
		}
		case "odt":
		{
			ZipArchiveEntry entry3 = zipArchive.GetEntry("content.xml");
			return (entry3 != null) ? OpenDocumentXmlToText(ReadEntry(entry3)) : null;
		}
		case "pptx":
		{
			List<ZipArchiveEntry> list = (from e in zipArchive.Entries
				where SlidePath().IsMatch(e.FullName)
				orderby int.Parse(SlidePath().Match(e.FullName).Groups[1].Value)
				select e).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ZipArchiveEntry item in list)
			{
				IEnumerable<string> values = from m in DrawingTextRun().Matches(ReadEntry(item))
					select XmlUnescape(m.Groups[1].Value);
				stringBuilder.AppendLine(string.Join(" ", values));
			}
			return stringBuilder.ToString();
		}
		case "xlsx":
		{
			ZipArchiveEntry entry = zipArchive.GetEntry("xl/sharedStrings.xml");
			if (entry == null)
			{
				return null;
			}
			return string.Join("\n", from m in SharedStringItem().Matches(ReadEntry(entry))
				select XmlUnescape(string.Concat(from t in PlainTextRun().Matches(m.Value)
					select t.Groups[1].Value)));
		}
		default:
			return null;
		}
	}

	private static string ReadEntry(ZipArchiveEntry entry)
	{
		using Stream stream = entry.Open();
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	internal static string WordXmlToText(string xml)
	{
		string input = WordParagraphEnd().Replace(xml, "\n");
		input = WordTab().Replace(input, "\t");
		input = WordBreak().Replace(input, "\n");
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Match item in WordTextOrBreak().Matches(input))
		{
			stringBuilder.Append(item.Groups[1].Success ? XmlUnescape(item.Groups[1].Value) : item.Value);
		}
		return stringBuilder.ToString();
	}

	internal static string OpenDocumentXmlToText(string xml)
	{
		string input = OdtBlockEnd().Replace(xml, "\n");
		input = OdtTab().Replace(input, "\t");
		input = AnyTag().Replace(input, "");
		return XmlUnescape(input);
	}

	internal static string RtfToText(string rtf)
	{
		StringBuilder stringBuilder = new StringBuilder(rtf.Length / 2);
		Stack<bool> stack = new Stack<bool>();
		bool flag = false;
		int num = 1;
		int num2 = 0;
		for (int i = 0; i < rtf.Length; i++)
		{
			char c = rtf[i];
			if (c == '{')
			{
				stack.Push(flag);
			}
			else if (c == '}')
			{
				flag = stack.Count > 0 && stack.Pop();
			}
			else if (c == '\\')
			{
				if (i + 1 >= rtf.Length)
				{
					break;
				}
				char c2 = rtf[i + 1];
				if ((c2 == '\\' || c2 == '{' || c2 == '}') ? true : false)
				{
					if (!flag)
					{
						stringBuilder.Append(c2);
					}
					i++;
					continue;
				}
				bool flag2;
				switch (c2)
				{
				case '\'':
				{
					if (i + 3 < rtf.Length && int.TryParse(rtf.AsSpan(i + 2, 2), NumberStyles.HexNumber, null, out var result))
					{
						if (num2 > 0)
						{
							num2--;
						}
						else if (!flag)
						{
							Encoding latin = Encoding.Latin1;
							byte reference = (byte)result;
							stringBuilder.Append(latin.GetString(new ReadOnlySpan<byte>(in reference)));
						}
					}
					i += 3;
					continue;
				}
				case '*':
					flag = true;
					i++;
					continue;
				case '\n':
				case '\r':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				if (flag2)
				{
					if (!flag)
					{
						stringBuilder.Append('\n');
					}
					i++;
					continue;
				}
				int j;
				for (j = i + 1; j < rtf.Length && char.IsAsciiLetter(rtf[j]); j++)
				{
				}
				int num3 = i + 1;
				string text = rtf.Substring(num3, j - num3);
				int k = j;
				if (k < rtf.Length && (rtf[k] == '-' || char.IsAsciiDigit(rtf[k])))
				{
					for (k++; k < rtf.Length && char.IsAsciiDigit(rtf[k]); k++)
					{
					}
				}
				num3 = j;
				string s = rtf.Substring(num3, k - num3);
				if (k < rtf.Length && rtf[k] == ' ')
				{
					k++;
				}
				i = k - 1;
				switch (text)
				{
				case "par":
				case "row":
				case "line":
					if (!flag)
					{
						stringBuilder.Append('\n');
					}
					break;
				case "tab":
				case "cell":
					if (!flag)
					{
						stringBuilder.Append('\t');
					}
					break;
				case "uc":
				{
					if (int.TryParse(s, out var result3))
					{
						num = result3;
					}
					break;
				}
				case "u":
				{
					if (int.TryParse(s, out var result2) && !flag)
					{
						stringBuilder.Append((char)((result2 < 0) ? (result2 + 65536) : result2));
					}
					num2 = num;
					break;
				}
				case "pict":
				case "info":
				case "fonttbl":
				case "rsidtbl":
				case "colortbl":
				case "xmlnstbl":
				case "footer":
				case "header":
				case "listtable":
				case "themedata":
				case "datastore":
				case "generator":
				case "stylesheet":
				case "listoverridetable":
				case "colorschememapping":
				case "latentstyles":
					flag = true;
					break;
				}
			}
			else if ((c != '\n' && c != '\r') || 1 == 0)
			{
				if (num2 > 0)
				{
					num2--;
				}
				else if (!flag)
				{
					stringBuilder.Append(c);
				}
			}
		}
		return stringBuilder.ToString();
	}

	public static string TextWithoutMarkup(string html)
	{
		string input = ScriptBlock().Replace(html, " ");
		input = StyleBlock().Replace(input, " ");
		input = AnyTag().Replace(input, " ");
		input = input.Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<")
			.Replace("&gt;", ">")
			.Replace("&quot;", "\"")
			.Replace("&#39;", "'");
		return Whitespace().Replace(input, " ").Trim();
	}

	private static string XmlUnescape(string text)
	{
		return WebUtility.HtmlDecode(text);
	}

	// [GeneratedRegex("</w:p>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex WordParagraphEnd()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordParagraphEnd_4.Instance;
	}

	// [GeneratedRegex("<w:tab\\s*/>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex WordTab()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordTab_5.Instance;
	}

	// [GeneratedRegex("<w:(br|cr)\\b[^>]*/>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex WordBreak()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordBreak_6.Instance;
	}

	// [GeneratedRegex("<w:t(?:\\s[^>]*)?>([^<]*)</w:t>|\\n|\\t")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex WordTextOrBreak()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordTextOrBreak_7.Instance;
	}

	// [GeneratedRegex("</text:(p|h)>|<text:line-break\\s*/>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex OdtBlockEnd()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__OdtBlockEnd_8.Instance;
	}

	// [GeneratedRegex("<text:tab\\s*/>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex OdtTab()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__OdtTab_9.Instance;
	}

	// [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex AnyTag()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__AnyTag_10.Instance;
	}

	// [GeneratedRegex("^ppt/slides/slide(\\d+)\\.xml$")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex SlidePath()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__SlidePath_11.Instance;
	}

	// [GeneratedRegex("<a:t>([^<]*)</a:t>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex DrawingTextRun()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__DrawingTextRun_12.Instance;
	}

	// [GeneratedRegex("<si>.*?</si>", RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex SharedStringItem()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__SharedStringItem_13.Instance;
	}

	// [GeneratedRegex("<t(?:\\s[^>]*)?>([^<]*)</t>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex PlainTextRun()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__PlainTextRun_14.Instance;
	}

	// [GeneratedRegex("<script.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex ScriptBlock()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ScriptBlock_15.Instance;
	}

	// [GeneratedRegex("<style.*?</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex StyleBlock()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__StyleBlock_16.Instance;
	}

	// [GeneratedRegex("\\s+")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex Whitespace()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Whitespace_17.Instance;
	}
}

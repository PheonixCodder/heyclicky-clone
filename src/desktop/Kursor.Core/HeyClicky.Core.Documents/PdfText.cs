using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Documents;

public static class PdfText
{
	public sealed record Result(string Text, int? PageCount, bool StoppedEarly);

	private sealed class PdfObject(string dictionary, byte[]? stream)
	{
		public string dictionary = dictionary;

		public byte[]? stream = stream;

		private byte[]? decoded;

		public byte[]? Stream => decoded ?? (decoded = Decode(dictionary, stream));
	}

	private sealed class FontDecoder
	{
		public readonly Dictionary<int, string> Map = new Dictionary<int, string>();

		public int CodeBytes = 1;

		public bool HasMap => Map.Count > 0;

		public string Decode(byte[] bytes)
		{
			StringBuilder stringBuilder = new StringBuilder(bytes.Length);
			if (!HasMap)
			{
				if (CodeBytes == 2)
				{
					for (int i = 0; i + 1 < bytes.Length; i += 2)
					{
						stringBuilder.Append((char)((bytes[i] << 8) | bytes[i + 1]));
					}
					return stringBuilder.ToString();
				}
				foreach (byte b in bytes)
				{
					stringBuilder.Append(PdfDocEncoding(b));
				}
				return stringBuilder.ToString();
			}
			int num = 0;
			while (num < bytes.Length)
			{
				if (CodeBytes == 2 && num + 1 < bytes.Length)
				{
					int key = (bytes[num] << 8) | bytes[num + 1];
					if (Map.TryGetValue(key, out string value))
					{
						stringBuilder.Append(value);
					}
					num += 2;
				}
				else
				{
					stringBuilder.Append(Map.TryGetValue(bytes[num], out string value2) ? value2 : PdfDocEncoding(bytes[num]).ToString());
					num++;
				}
			}
			return stringBuilder.ToString();
		}
	}

	public static Result Extract(string path, int maximumCharacters = 60000)
	{
		return Extract(File.ReadAllBytes(path), maximumCharacters);
	}

	public static Result Extract(byte[] bytes, int maximumCharacters = 60000)
	{
		string @string = Encoding.Latin1.GetString(bytes);
		if (@string.Contains("/Encrypt", StringComparison.Ordinal) && EncryptReference().IsMatch(@string))
		{
			return new Result("", null, StoppedEarly: false);
		}
		Dictionary<int, PdfObject> dictionary = ParseObjects(bytes, @string);
		List<int> list = PageOrder(dictionary, @string);
		StringBuilder stringBuilder = new StringBuilder();
		Dictionary<string, FontDecoder> cache = new Dictionary<string, FontDecoder>();
		bool stoppedEarly = false;
		foreach (int item in list)
		{
			if (stringBuilder.Length >= maximumCharacters)
			{
				stoppedEarly = true;
				break;
			}
			if (!dictionary.TryGetValue(item, out var value))
			{
				continue;
			}
			Dictionary<string, FontDecoder> fonts = PageFonts(dictionary, item, value, cache);
			foreach (int item2 in ReferencesOf(value.dictionary, "Contents", dictionary))
			{
				if (dictionary.TryGetValue(item2, out var value2))
				{
					byte[] stream = value2.Stream;
					if (stream != null)
					{
						ReadContent(stream, fonts, stringBuilder);
					}
				}
			}
			stringBuilder.Append('\n');
		}
		return new Result(CleanUp(stringBuilder.ToString()), (list.Count > 0) ? new int?(list.Count) : ((int?)null), stoppedEarly);
	}

	private static string CleanUp(string text)
	{
		IEnumerable<string> values = from line in text.Split('\n')
			select MultipleSpaces().Replace(line, " ").TrimEnd();
		return BlankLines().Replace(string.Join("\n", values), "\n\n").Trim();
	}

	private static Dictionary<int, PdfObject> ParseObjects(byte[] bytes, string latin)
	{
		Dictionary<int, PdfObject> dictionary = new Dictionary<int, PdfObject>();
		IEnumerator enumerator = ObjectHeader().Matches(latin).GetEnumerator();
		try
		{
			int key;
			byte[] stream;
			string dictionary2;
			for (; enumerator.MoveNext(); dictionary[key] = new PdfObject(dictionary2, stream))
			{
				Match match = (Match)enumerator.Current;
				key = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				int num = match.Index + match.Length;
				int num2 = latin.IndexOf("endobj", num, StringComparison.Ordinal);
				if (num2 < 0)
				{
					num2 = latin.Length;
				}
				int num3 = latin.IndexOf("stream", num, StringComparison.Ordinal);
				stream = null;
				int num5;
				int num7;
				int num4;
				if (num3 >= 0 && num3 < num2 && !IsEndStream(latin, num3))
				{
					num4 = num;
					dictionary2 = latin.Substring(num4, num3 - num4);
					num5 = num3 + 6;
					if (num5 < latin.Length && latin[num5] == '\r')
					{
						num5++;
					}
					if (num5 < latin.Length && latin[num5] == '\n')
					{
						num5++;
					}
					int? num6 = DirectLength(dictionary2);
					if (num6.HasValue)
					{
						int valueOrDefault = num6.GetValueOrDefault();
						if (num5 + valueOrDefault <= bytes.Length && LooksLikeEndStream(latin, num5 + valueOrDefault))
						{
							num7 = num5 + valueOrDefault;
							goto IL_0146;
						}
					}
					num7 = latin.IndexOf("endstream", num5, StringComparison.Ordinal);
					if (num7 < 0)
					{
						num7 = num2;
					}
					goto IL_0146;
				}
				num4 = num;
				dictionary2 = latin.Substring(num4, num2 - num4);
				continue;
				IL_0146:
				stream = bytes[num5..Math.Max(num5, num7)];
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
		foreach (PdfObject item2 in dictionary.Values.Where((PdfObject o) => o.dictionary.Contains("/ObjStm", StringComparison.Ordinal)).ToList())
		{
			byte[] stream2 = item2.Stream;
			if (stream2 == null)
			{
				continue;
			}
			string @string = Encoding.Latin1.GetString(stream2);
			int? num8 = DirectInt(item2.dictionary, "N");
			if (!num8.HasValue)
			{
				continue;
			}
			int valueOrDefault2 = num8.GetValueOrDefault();
			num8 = DirectInt(item2.dictionary, "First");
			if (!num8.HasValue)
			{
				continue;
			}
			int valueOrDefault3 = num8.GetValueOrDefault();
			string[] array = @string.Substring(0, Math.Min(valueOrDefault3, @string.Length)).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			List<(int, int)> list = new List<(int, int)>();
			for (int i = 0; i + 1 < array.Length; i += 2)
			{
				if (list.Count >= valueOrDefault2)
				{
					break;
				}
				if (int.TryParse(array[i], out var result) && int.TryParse(array[i + 1], out var result2))
				{
					list.Add((result, result2));
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				int num9 = valueOrDefault3 + list[j].Item2;
				int num10 = ((j + 1 < list.Count) ? (valueOrDefault3 + list[j + 1].Item2) : @string.Length);
				if (num9 >= 0 && num9 < @string.Length && num10 > num9)
				{
					int item = list[j].Item1;
					int num4 = num9;
					dictionary.TryAdd(item, new PdfObject(@string.Substring(num4, Math.Min(num10, @string.Length) - num4), null));
				}
			}
		}
		return dictionary;
	}

	private static bool IsEndStream(string latin, int at)
	{
		if (at >= 3)
		{
			return string.CompareOrdinal(latin, at - 3, "end", 0, 3) == 0;
		}
		return false;
	}

	private static bool LooksLikeEndStream(string latin, int at)
	{
		for (int i = at; i < Math.Min(latin.Length, at + 4); i++)
		{
			if (latin[i] == 'e')
			{
				return string.CompareOrdinal(latin, i, "endstream", 0, 9) == 0;
			}
		}
		return false;
	}

	private static int? DirectLength(string dictionary)
	{
		Match match = DirectLengthPattern().Match(dictionary);
		if (!match.Success)
		{
			return null;
		}
		return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
	}

	private static int? DirectInt(string dictionary, string key)
	{
		Match match = Regex.Match(dictionary, "/" + key + "\\s+(\\d+)(?!\\s+\\d+\\s+R)");
		if (!match.Success)
		{
			return null;
		}
		return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
	}

	private static byte[]? Decode(string dictionary, byte[]? raw)
	{
		if (raw == null)
		{
			return null;
		}
		if (!dictionary.Contains("/Filter", StringComparison.Ordinal))
		{
			return raw;
		}
		if (!dictionary.Contains("FlateDecode", StringComparison.Ordinal) && !dictionary.Contains("/Fl ", StringComparison.Ordinal))
		{
			return null;
		}
		byte[] array = Inflate(raw);
		if (array == null)
		{
			return null;
		}
		int? num = DirectInt(dictionary, "Predictor");
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault >= 10)
			{
				array = Unpredict(array, DirectInt(dictionary, "Columns") ?? 1);
			}
		}
		return array;
	}

	private static byte[]? Inflate(byte[] raw)
	{
		bool[] array = new bool[2] { false, true };
		foreach (bool flag in array)
		{
			try
			{
				using MemoryStream stream = new MemoryStream(raw, flag ? 2 : 0, raw.Length - (flag ? 2 : 0));
				using Stream stream2 = (flag ? ((Stream)new DeflateStream(stream, CompressionMode.Decompress)) : ((Stream)new ZLibStream(stream, CompressionMode.Decompress)));
				using MemoryStream memoryStream = new MemoryStream();
				byte[] array2 = new byte[16384];
				try
				{
					int count;
					while ((count = stream2.Read(array2, 0, array2.Length)) > 0)
					{
						memoryStream.Write(array2, 0, count);
					}
				}
				catch (InvalidDataException) when (memoryStream.Length > 0)
				{
				}
				if (memoryStream.Length > 0)
				{
					return memoryStream.ToArray();
				}
			}
			catch (Exception)
			{
			}
		}
		return null;
	}

	private static byte[] Unpredict(byte[] data, int columns)
	{
		int num = columns + 1;
		int num2 = data.Length / num;
		byte[] array = new byte[num2 * columns];
		byte[] array2 = new byte[columns];
		for (int i = 0; i < num2; i++)
		{
			byte b = data[i * num];
			for (int j = 0; j < columns; j++)
			{
				byte b2 = data[i * num + 1 + j];
				byte b3 = (byte)((j > 0) ? array[i * columns + j - 1] : 0);
				byte b4 = array2[j];
				byte c = (byte)((j > 0) ? array2[j - 1] : 0);
				array[i * columns + j] = (byte)(b switch
				{
					1 => (uint)(b2 + b3), 
					2 => (uint)(b2 + b4), 
					3 => (uint)(b2 + (b3 + b4) / 2), 
					4 => (uint)(b2 + Paeth(b3, b4, c)), 
					_ => b2, 
				});
			}
			Array.Copy(array, i * columns, array2, 0, columns);
		}
		return array;
	}

	private static int Paeth(int a, int b, int c)
	{
		int num = a + b - c;
		int num2 = Math.Abs(num - a);
		int num3 = Math.Abs(num - b);
		int num4 = Math.Abs(num - c);
		if (num2 > num3 || num2 > num4)
		{
			if (num3 > num4)
			{
				return c;
			}
			return b;
		}
		return a;
	}

	private static List<int> PageOrder(Dictionary<int, PdfObject> objects, string latin)
	{
		List<int> pages = new List<int>();
		Match match = RootReference().Matches(latin).LastOrDefault();
		HashSet<int> visited;
		if (match != null && objects.TryGetValue(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), out PdfObject value))
		{
			int? num = ReferenceOf(value.dictionary, "Pages");
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				visited = new HashSet<int>();
				Walk(valueOrDefault, 0);
			}
		}
		if (pages.Count == 0)
		{
			pages.AddRange(from o in objects
				where PageType().IsMatch(o.Value.dictionary)
				select o.Key into n
				orderby n
				select n);
		}
		return pages;
		void Walk(int number, int depth)
		{
			if (depth <= 64 && visited.Add(number) && objects.TryGetValue(number, out PdfObject value2))
			{
				if (!PageType().IsMatch(value2.dictionary))
				{
					foreach (int item in ReferencesOf(value2.dictionary, "Kids", objects))
					{
						Walk(item, depth + 1);
					}
					return;
				}
				pages.Add(number);
			}
		}
	}

	private static int? ReferenceOf(string dictionary, string key)
	{
		Match match = Regex.Match(dictionary, "/" + key + "\\s+(\\d+)\\s+\\d+\\s+R");
		if (!match.Success)
		{
			return null;
		}
		return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
	}

	private static IEnumerable<int> ReferencesOf(string dictionary, string key, Dictionary<int, PdfObject> objects)
	{
		Match match = Regex.Match(dictionary, "/" + key + "\\s*\\[([^\\]]*)\\]");
		if (match.Success)
		{
			return ArrayReferences(match.Groups[1].Value);
		}
		int? num = ReferenceOf(dictionary, key);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (objects.TryGetValue(valueOrDefault, out PdfObject value) && value.stream == null && value.dictionary.TrimStart().StartsWith('['))
			{
				return ArrayReferences(value.dictionary);
			}
			return new _003C_003Ez__ReadOnlySingleElementList<int>(valueOrDefault);
		}
		return Array.Empty<int>();
	}

	private static IEnumerable<int> ArrayReferences(string text)
	{
		return (from m in IndirectReference().Matches(text)
			select int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)).ToList();
	}

	private static char PdfDocEncoding(byte b)
	{
		byte b2 = b;
		if (b2 >= 32)
		{
			switch (b2)
			{
			case 145:
				return '‘';
			case 146:
				return '’';
			case 147:
				return '“';
			case 148:
				return '”';
			case 150:
				return '–';
			case 151:
				return '—';
			case 149:
				return '•';
			case 133:
				return '…';
			}
		}
		else if (((uint)(b - 9) > 1u && b != 13) || 1 == 0)
		{
			return ' ';
		}
		return (char)b;
	}

	private static Dictionary<string, FontDecoder> PageFonts(Dictionary<int, PdfObject> objects, int page, PdfObject pageObject, Dictionary<string, FontDecoder> cache)
	{
		Dictionary<string, FontDecoder> dictionary = new Dictionary<string, FontDecoder>();
		string text = null;
		PdfObject pdfObject = pageObject;
		object obj;
		for (int i = 0; i < 32 && pdfObject != null && text == null; pdfObject = (PdfObject)obj, i++)
		{
			text = InlineOrReferencedDictionary(pdfObject.dictionary, "Resources", objects);
			int? num = ReferenceOf(pdfObject.dictionary, "Parent");
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				if (objects.TryGetValue(valueOrDefault, out PdfObject value))
				{
					obj = value;
					continue;
				}
			}
			obj = null;
		}
		if (text == null)
		{
			return dictionary;
		}
		string text2 = InlineOrReferencedDictionary(text, "Font", objects);
		if (text2 == null)
		{
			return dictionary;
		}
		foreach (Match item in FontEntry().Matches(text2))
		{
			string value2 = item.Groups[1].Value;
			string value3 = item.Groups[2].Value;
			if (!cache.TryGetValue(value3, out FontDecoder value4))
			{
				value4 = new FontDecoder();
				if (objects.TryGetValue(int.Parse(value3, CultureInfo.InvariantCulture), out PdfObject value5))
				{
					if (value5.dictionary.Contains("/Type0", StringComparison.Ordinal))
					{
						value4.CodeBytes = 2;
					}
					int? num = ReferenceOf(value5.dictionary, "ToUnicode");
					if (num.HasValue)
					{
						int valueOrDefault2 = num.GetValueOrDefault();
						if (objects.TryGetValue(valueOrDefault2, out PdfObject value6))
						{
							byte[] stream = value6.Stream;
							if (stream != null)
							{
								ReadCMap(Encoding.Latin1.GetString(stream), value4);
							}
						}
					}
				}
				cache[value3] = value4;
			}
			dictionary[value2] = value4;
		}
		return dictionary;
	}

	private static string? InlineOrReferencedDictionary(string dictionary, string key, Dictionary<int, PdfObject> objects)
	{
		int? num = ReferenceOf(dictionary, key);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (!objects.TryGetValue(valueOrDefault, out PdfObject value))
			{
				return null;
			}
			return value.dictionary;
		}
		int num2 = dictionary.IndexOf("/" + key, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return null;
		}
		int num3 = dictionary.IndexOf("<<", num2, StringComparison.Ordinal);
		if (num3 < 0)
		{
			return null;
		}
		int num4 = 0;
		for (int i = num3; i < dictionary.Length - 1; i++)
		{
			if (dictionary[i] == '<' && dictionary[i + 1] == '<')
			{
				num4++;
				i++;
			}
			else if (dictionary[i] == '>' && dictionary[i + 1] == '>')
			{
				num4--;
				i++;
				if (num4 == 0)
				{
					int num5 = num3;
					return dictionary.Substring(num5, i + 1 - num5);
				}
			}
		}
		return null;
	}

	private static void ReadCMap(string cmap, FontDecoder decoder)
	{
		Match match = CodespaceRange().Match(cmap);
		if (match.Success)
		{
			decoder.CodeBytes = Math.Max(1, match.Groups[1].Value.Length / 2);
		}
		foreach (Match item in BfCharBlock().Matches(cmap))
		{
			foreach (Match item2 in HexPair().Matches(item.Groups[1].Value))
			{
				decoder.Map[ParseHex(item2.Groups[1].Value)] = HexToUnicode(item2.Groups[2].Value);
			}
		}
		foreach (Match item3 in BfRangeBlock().Matches(cmap))
		{
			foreach (Match item4 in BfRangeLine().Matches(item3.Groups[1].Value))
			{
				int num = ParseHex(item4.Groups[1].Value);
				int num2 = ParseHex(item4.Groups[2].Value);
				if (num2 - num > 65536 || num2 < num)
				{
					continue;
				}
				if (item4.Groups[3].Success)
				{
					string text = HexToUnicode(item4.Groups[3].Value);
					if (text.Length != 0)
					{
						char c = text[text.Length - 1];
						for (int i = num; i <= num2; i++)
						{
							Dictionary<int, string> map = decoder.Map;
							int key = i;
							string text2 = text;
							map[key] = text2.Substring(0, text2.Length - 1) + (char)(c + (i - num));
						}
					}
				}
				else
				{
					List<string> list = (from m in HexString().Matches(item4.Groups[4].Value)
						select HexToUnicode(m.Groups[1].Value)).ToList();
					for (int j = num; j <= num2 && j - num < list.Count; j++)
					{
						decoder.Map[j] = list[j - num];
					}
				}
			}
		}
	}

	private static int ParseHex(string hex)
	{
		if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			return 0;
		}
		return result;
	}

	private static string HexToUnicode(string hex)
	{
		hex = hex.Trim();
		if (hex.Length % 4 != 0)
		{
			hex = hex.PadRight(hex.Length + 4 - hex.Length % 4, '0');
		}
		StringBuilder stringBuilder = new StringBuilder(hex.Length / 4);
		for (int i = 0; i + 3 < hex.Length; i += 4)
		{
			stringBuilder.Append((char)ParseHex(hex.Substring(i, 4)));
		}
		return stringBuilder.ToString();
	}

	private static void ReadContent(byte[] content, Dictionary<string, FontDecoder> fonts, StringBuilder output)
	{
		FontDecoder fontDecoder = new FontDecoder();
		FontDecoder fontDecoder2 = fontDecoder;
		List<object> list = new List<object>();
		int lineStart = output.Length;
		int i = 0;
		while (i < content.Length)
		{
			char c = (char)content[i];
			if (char.IsWhiteSpace(c) || c == '\0')
			{
				i++;
				continue;
			}
			if (c == '%')
			{
				for (; i < content.Length && content[i] != 10 && content[i] != 13; i++)
				{
				}
				continue;
			}
			switch (c)
			{
			case '(':
				list.Add(ReadLiteral(content, ref i));
				continue;
			case '<':
				if (i + 1 < content.Length && content[i + 1] == 60)
				{
					i += 2;
					SkipDictionary(content, ref i);
					continue;
				}
				break;
			}
			switch (c)
			{
			case '<':
				list.Add(ReadHex(content, ref i));
				continue;
			case '[':
				list.Add(ReadArray(content, ref i));
				continue;
			case '/':
				list.Add("/" + ReadToken(content, ref i, 1));
				continue;
			}
			bool flag = char.IsAsciiDigit(c);
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '+':
				case '-':
				case '.':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				string s = ReadToken(content, ref i, 0);
				list.Add(double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.0);
				continue;
			}
			string text = ReadToken(content, ref i, 0);
			if (text.Length == 0)
			{
				i++;
				continue;
			}
			if (text != null)
			{
				switch (text.Length)
				{
				case 2:
				{
					char c2 = text[1];
					if ((uint)c2 <= 74u)
					{
						if ((uint)c2 > 68u)
						{
							switch (c2)
							{
							case 'I':
								if (text == "BI")
								{
									for (; i + 1 < content.Length && (content[i] != 69 || content[i + 1] != 73 || (i != 0 && !char.IsWhiteSpace((char)content[i - 1]))); i++)
									{
									}
									i += 2;
								}
								break;
							case 'J':
								if (!(text == "TJ") || !(list.LastOrDefault() is List<object> list2))
								{
									break;
								}
								foreach (object item in list2)
								{
									if (item is byte[] bytes2)
									{
										output.Append(fontDecoder2.Decode(bytes2));
									}
									else if (item is double num && num < -180.0 && output.Length > lineStart)
									{
										StringBuilder stringBuilder = output;
										if (stringBuilder[stringBuilder.Length - 1] != ' ')
										{
											output.Append(' ');
										}
									}
								}
								break;
							}
							break;
						}
						if (c2 == '*')
						{
							if (text == "T*")
							{
								NewLine();
							}
							break;
						}
						if (c2 != 'D' || !(text == "TD"))
						{
							break;
						}
					}
					else
					{
						if ((uint)c2 > 100u)
						{
							switch (c2)
							{
							case 'f':
								if (text == "Tf" && list.Count >= 2)
								{
									if (list[list.Count - 2] is string text2 && text2.StartsWith('/'))
									{
										string text3 = text2;
										fontDecoder2 = (fonts.TryGetValue(text3.Substring(1, text3.Length - 1), out FontDecoder value) ? value : fontDecoder);
									}
								}
								break;
							case 'j':
								if (text == "Tj" && list.LastOrDefault() is byte[] bytes3)
								{
									output.Append(fontDecoder2.Decode(bytes3));
								}
								break;
							case 'm':
								if (text == "Tm")
								{
									NewLine();
								}
								break;
							}
							break;
						}
						if (c2 == 'T')
						{
							if (text == "ET" && output.Length > lineStart)
							{
								StringBuilder stringBuilder2 = output;
								if (stringBuilder2[stringBuilder2.Length - 1] != ' ')
								{
									output.Append(' ');
								}
							}
							break;
						}
						if (c2 != 'd' || !(text == "Td"))
						{
							break;
						}
					}
					if (list.Count >= 2)
					{
						if (list[list.Count - 1] is double value2 && Math.Abs(value2) > 0.01)
						{
							NewLine();
							break;
						}
					}
					if (output.Length > lineStart)
					{
						StringBuilder stringBuilder3 = output;
						if (stringBuilder3[stringBuilder3.Length - 1] != ' ')
						{
							output.Append(' ');
						}
					}
					break;
				}
				case 1:
				{
					char c2 = text[0];
					if (c2 == '"' || c2 == '\'')
					{
						NewLine();
						if (list.LastOrDefault() is byte[] bytes)
						{
							output.Append(fontDecoder2.Decode(bytes));
						}
					}
					break;
				}
				}
			}
			list.Clear();
		}
		NewLine();
		void NewLine()
		{
			if (output.Length > lineStart)
			{
				output.Append('\n');
				lineStart = output.Length;
			}
		}
	}

	private static string ReadToken(byte[] content, ref int i, int skip)
	{
		i += skip;
		int num = i;
		while (i < content.Length)
		{
			char c = (char)content[i];
			bool flag = char.IsWhiteSpace(c);
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '%':
				case '(':
				case ')':
				case '/':
				case '<':
				case '>':
				case '[':
				case ']':
				case '{':
				case '}':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				break;
			}
			i++;
		}
		return Encoding.Latin1.GetString(content, num, i - num);
	}

	private static byte[] ReadLiteral(byte[] content, ref int i)
	{
		i++;
		int num = 1;
		List<byte> list = new List<byte>();
		while (i < content.Length)
		{
			byte b = content[i++];
			if (b == 92 && i < content.Length)
			{
				byte b2 = content[i++];
				switch ((char)b2)
				{
				case 'n':
					list.Add(10);
					break;
				case 'r':
					list.Add(13);
					break;
				case 't':
					list.Add(9);
					break;
				case 'b':
					list.Add(8);
					break;
				case 'f':
					list.Add(12);
					break;
				case '\r':
					if (i < content.Length && content[i] == 10)
					{
						i++;
					}
					break;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				{
					int num2 = b2 - 48;
					for (int j = 0; j < 2; j++)
					{
						if (i >= content.Length)
						{
							break;
						}
						byte b3 = content[i];
						if (b3 < 48 || b3 > 55)
						{
							break;
						}
						num2 = num2 * 8 + (content[i++] - 48);
					}
					list.Add((byte)num2);
					break;
				}
				default:
					list.Add(b2);
					break;
				case '\n':
					break;
				}
			}
			else
			{
				if (b == 40)
				{
					num++;
				}
				else if (b == 41 && --num == 0)
				{
					break;
				}
				list.Add(b);
			}
		}
		return list.ToArray();
	}

	private static byte[] ReadHex(byte[] content, ref int i)
	{
		i++;
		StringBuilder stringBuilder = new StringBuilder();
		while (i < content.Length && content[i] != 62)
		{
			char c = (char)content[i++];
			if (Uri.IsHexDigit(c))
			{
				stringBuilder.Append(c);
			}
		}
		i++;
		if (stringBuilder.Length % 2 == 1)
		{
			stringBuilder.Append('0');
		}
		return Convert.FromHexString(stringBuilder.ToString());
	}

	private static List<object> ReadArray(byte[] content, ref int i)
	{
		i++;
		List<object> list = new List<object>();
		while (i < content.Length && content[i] != 93)
		{
			char c = (char)content[i];
			if (char.IsWhiteSpace(c))
			{
				i++;
				continue;
			}
			switch (c)
			{
			case '(':
				list.Add(ReadLiteral(content, ref i));
				continue;
			case '<':
				list.Add(ReadHex(content, ref i));
				continue;
			}
			string text = ReadToken(content, ref i, 0);
			if (text.Length == 0)
			{
				i++;
			}
			else
			{
				list.Add(double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0.0);
			}
		}
		i++;
		return list;
	}

	private static void SkipDictionary(byte[] content, ref int i)
	{
		int num = 1;
		while (i + 1 < content.Length && num > 0)
		{
			if (content[i] == 60 && content[i + 1] == 60)
			{
				num++;
				i += 2;
			}
			else if (content[i] == 62 && content[i + 1] == 62)
			{
				num--;
				i += 2;
			}
			else
			{
				i++;
			}
		}
	}

	// [GeneratedRegex("(?<![\\d])(\\d+)\\s+(\\d+)\\s+obj\\b")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex ObjectHeader()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ObjectHeader_20.Instance;
	}

	// [GeneratedRegex("/Length\\s+(\\d+)(?!\\s+\\d+\\s+R)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex DirectLengthPattern()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__DirectLengthPattern_21.Instance;
	}

	// [GeneratedRegex("/Root\\s+(\\d+)\\s+\\d+\\s+R")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex RootReference()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__RootReference_22.Instance;
	}

	// [GeneratedRegex("/Encrypt\\s+(\\d+\\s+\\d+\\s+R|<<)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex EncryptReference()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__EncryptReference_23.Instance;
	}

	// [GeneratedRegex("/Type\\s*/Page(?![s\\w])")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex PageType()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__PageType_24.Instance;
	}

	// [GeneratedRegex("(\\d+)\\s+\\d+\\s+R")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex IndirectReference()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__IndirectReference_25.Instance;
	}

	// [GeneratedRegex("/([^\\s/<>\\[\\]()]+)\\s+(\\d+)\\s+\\d+\\s+R")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex FontEntry()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__FontEntry_26.Instance;
	}

	// [GeneratedRegex("begincodespacerange\\s*<([0-9A-Fa-f]+)>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex CodespaceRange()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__CodespaceRange_27.Instance;
	}

	// [GeneratedRegex("beginbfchar(.*?)endbfchar", RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BfCharBlock()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__BfCharBlock_28.Instance;
	}

	// [GeneratedRegex("beginbfrange(.*?)endbfrange", RegexOptions.Singleline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BfRangeBlock()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__BfRangeBlock_29.Instance;
	}

	// [GeneratedRegex("<([0-9A-Fa-f]+)>\\s*<([0-9A-Fa-f]*)>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex HexPair()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__HexPair_30.Instance;
	}

	// [GeneratedRegex("<([0-9A-Fa-f]+)>\\s*<([0-9A-Fa-f]+)>\\s*(?:<([0-9A-Fa-f]*)>|\\[([^\\]]*)\\])")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BfRangeLine()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__BfRangeLine_31.Instance;
	}

	// [GeneratedRegex("<([0-9A-Fa-f]*)>")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex HexString()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__HexString_32.Instance;
	}

	// [GeneratedRegex("[ \\t]{2,}")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex MultipleSpaces()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MultipleSpaces_33.Instance;
	}

	// [GeneratedRegex("\\n{3,}")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BlankLines()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__BlankLines_34.Instance;
	}
}

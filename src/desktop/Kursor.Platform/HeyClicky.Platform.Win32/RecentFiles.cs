using System;
using System.IO;
using System.Linq;
using System.Text;

namespace HeyClicky.Platform.Win32;

public static class RecentFiles
{
	private static readonly string RecentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent");

	public static string? FileNameFromTitle(string title)
	{
		string[] array = title.Split(new string[4] { " - ", " — ", " – ", " | " }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim().TrimStart('*', '●', '•').TrimEnd('*', '●', '•')
				.Trim();
			if (!text.StartsWith('[') || !text.EndsWith(']'))
			{
				string extension = Path.GetExtension(text);
				int length = extension.Length;
				bool flag = ((length < 2 || length > 6) ? true : false);
				if (!flag && text.Length <= 200 && text.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && extension.Skip(1).All(char.IsLetterOrDigit))
				{
					if (extension.Equals(".env", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pem", StringComparison.OrdinalIgnoreCase) || extension.Equals(".key", StringComparison.OrdinalIgnoreCase) || extension.Equals(".kdbx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					return text;
				}
			}
		}
		return null;
	}

	public static string? Resolve(string fileName)
	{
		try
		{
			string text = Path.Combine(RecentFolder, fileName + ".lnk");
			if (File.Exists(text))
			{
				string text2 = Target(text);
				if (text2 != null && File.Exists(text2))
				{
					return text2;
				}
			}
			foreach (FileInfo item in (from f in new DirectoryInfo(RecentFolder).EnumerateFiles("*.lnk")
				orderby f.LastWriteTimeUtc descending
				select f).Take(400))
			{
				if (item.Name.StartsWith(Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
				{
					string text3 = Target(item.FullName);
					if (text3 != null && string.Equals(Path.GetFileName(text3), fileName, StringComparison.OrdinalIgnoreCase) && File.Exists(text3))
					{
						return text3;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("RecentFiles", ex.Message);
		}
		return null;
	}

	internal static string? Target(string shortcutPath)
	{
		byte[] array = File.ReadAllBytes(shortcutPath);
		if (array.Length < 76 || BitConverter.ToInt32(array, 0) != 76)
		{
			return null;
		}
		uint num = BitConverter.ToUInt32(array, 20);
		int num2 = 76;
		if ((num & 1) != 0)
		{
			if (!(num2 + 2 <= array.Length))
			{
				return null;
			}
			num2 += 2 + BitConverter.ToUInt16(array, num2);
		}
		int num3 = num2;
		if ((num & 2) == 0 || num3 < 0 || !(num3 + 28 <= array.Length))
		{
			return null;
		}
		int num4 = BitConverter.ToInt32(array, num3 + 4);
		if ((BitConverter.ToInt32(array, num3 + 8) & 1) == 0)
		{
			return null;
		}
		string text = null;
		string text2 = null;
		if (num4 >= 36 && num3 + 36 <= array.Length)
		{
			int num5 = BitConverter.ToInt32(array, num3 + 28);
			int num6 = BitConverter.ToInt32(array, num3 + 32);
			if (num5 > 0)
			{
				text = ReadUnicode(array, num3 + num5);
			}
			if (num6 > 0)
			{
				text2 = ReadUnicode(array, num3 + num6);
			}
		}
		if (text == null)
		{
			text = ReadAnsi(array, num3 + BitConverter.ToInt32(array, num3 + 16));
		}
		if (text2 == null)
		{
			text2 = ReadAnsi(array, num3 + BitConverter.ToInt32(array, num3 + 24));
		}
		if (!string.IsNullOrEmpty(text))
		{
			return text + text2;
		}
		return null;
	}

	private static string ReadAnsi(byte[] data, int at)
	{
		if (at <= 0 || at >= data.Length)
		{
			return "";
		}
		int num = Array.IndexOf(data, (byte)0, at);
		return Encoding.Default.GetString(data, at, ((num < 0) ? data.Length : num) - at);
	}

	private static string ReadUnicode(byte[] data, int at)
	{
		if (!(at >= 0 && at < data.Length))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = at; i + 1 < data.Length; i += 2)
		{
			char c = (char)BitConverter.ToUInt16(data, i);
			if (c == '\0')
			{
				break;
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}
}

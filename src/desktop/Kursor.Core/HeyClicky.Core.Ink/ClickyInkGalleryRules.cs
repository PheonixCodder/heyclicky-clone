using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Ink;

public static class ClickyInkGalleryRules
{
	public const double Spacing = 16.0;

	private static readonly double[] Tilts = new double[6] { -1.1, 0.8, -0.5, 1.0, -0.8, 0.6 };

	private static readonly HashSet<string> AudioExtensions = new HashSet<string> { "mp3", "m4a", "wav", "aiff", "aif", "aac", "flac", "caf" };

	private static readonly HashSet<string> WebImageExtensions = new HashSet<string> { "jpg", "jpeg", "png", "webp", "gif", "heic" };

	public static int Columns(int itemCount)
	{
		if (itemCount < 3)
		{
			return Math.Max(itemCount, 1);
		}
		return 3;
	}

	public static double TileWidth(double inkWidth, int columns)
	{
		return (inkWidth - 8.0 - (double)(columns - 1) * 16.0) / (double)columns;
	}

	public static double Tilt(int index)
	{
		return Tilts[index % Tilts.Length];
	}

	public static bool IsWebLink(string entry)
	{
		if (!entry.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			return entry.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static bool IsAudio(string path)
	{
		return AudioExtensions.Contains(PathExtension(path));
	}

	public static bool IsWebImage(string rawPath)
	{
		if (!IsWebLink(rawPath) || !Uri.TryCreate(rawPath, UriKind.Absolute, out Uri result))
		{
			return false;
		}
		return WebImageExtensions.Contains(PathExtension(result.AbsolutePath));
	}

	public static string PickCaption(ClickyInkDocument.Item item)
	{
		if (item.Text.Length != 0)
		{
			return item.Text;
		}
		return LastPathComponent(item.Url ?? "");
	}

	public static string PathExtension(string path)
	{
		string text = LastPathComponent(path);
		int num = text.LastIndexOf('.');
		if (num > 0)
		{
			string text2 = text;
			int num2 = num + 1;
			return text2.Substring(num2, text2.Length - num2).ToLowerInvariant();
		}
		return "";
	}

	public static string LastPathComponent(string path)
	{
		string text = path.TrimEnd('/', '\\');
		int num = text.LastIndexOfAny(new char[2] { '/', '\\' });
		if (num >= 0)
		{
			string text2 = text;
			int num2 = num + 1;
			return text2.Substring(num2, text2.Length - num2);
		}
		return text;
	}
}

using System;
using System.Collections.Generic;
using System.IO;

namespace HeyClicky.Core.Ink;

public static class ClickyInkPaths
{
	public sealed record FileRules(Func<IReadOnlyList<string>, ISet<string>> SupportingPaths, Func<string, bool> IsMeaningfulFile);

	public static FileRules MainArtifacts { get; set; } = new FileRules((IReadOnlyList<string> _) => new HashSet<string>(StringComparer.OrdinalIgnoreCase), (string _) => true);

	public static bool IsWebLink(string entry)
	{
		if (!entry.StartsWith("http://", StringComparison.Ordinal))
		{
			return entry.StartsWith("https://", StringComparison.Ordinal);
		}
		return true;
	}

	public static string ExpandingTilde(string path)
	{
		if (path == "~")
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}
		if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Substring(2, path.Length - 2));
		}
		return path;
	}

	public static string Standardized(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
	}

	public static string PathExtension(string path)
	{
		string text = path.TrimEnd('/', '\\');
		int num = Math.Max(text.LastIndexOf('/'), text.LastIndexOf('\\'));
		string text2;
		if (num < 0)
		{
			text2 = text;
		}
		else
		{
			string text3 = text;
			int num2 = num + 1;
			text2 = text3.Substring(num2, text3.Length - num2);
		}
		string text4 = text2;
		int num3 = text4.LastIndexOf('.');
		if (num3 > 0 && num3 != text4.Length - 1)
		{
			string text3 = text4;
			int num2 = num3 + 1;
			return text3.Substring(num2, text3.Length - num2);
		}
		return "";
	}
}

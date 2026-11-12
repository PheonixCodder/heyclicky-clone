using System;

namespace HeyClicky.Drawing.Media;

internal static class PhotoQueries
{
	internal const string ArticleQueryPrefix = "page:";

	internal const string NoPeopleQueryPrefix = "nopeople:";

	internal static string Key(string query)
	{
		string text = query.Trim();
		string text2 = "";
		if (text.StartsWith("nopeople:", StringComparison.OrdinalIgnoreCase))
		{
			text2 = "nopeople: ";
			string text3 = text;
			int length = "nopeople:".Length;
			text = text3.Substring(length, text3.Length - length).Trim();
		}
		if (text.StartsWith("page:", StringComparison.OrdinalIgnoreCase))
		{
			string text4 = text2;
			string text3 = text;
			int length = "page:".Length;
			return text4 + "page:" + text3.Substring(length, text3.Length - length);
		}
		return text2 + text.ToLowerInvariant();
	}

	internal static bool KnownPhotoIsUsed(string key, string lowercasedProgram)
	{
		string text;
		if (!key.StartsWith("nopeople:", StringComparison.Ordinal))
		{
			text = key;
		}
		else
		{
			int length = "nopeople:".Length;
			text = key.Substring(length, key.Length - length).Trim();
		}
		string text2 = text;
		if (text2.Length > 0)
		{
			return lowercasedProgram.Contains(text2.ToLowerInvariant(), StringComparison.Ordinal);
		}
		return false;
	}

	internal static bool IsBigEnough(int width, int height, int minimumEdge, int minimumLongestEdge)
	{
		if (width >= minimumEdge && height >= minimumEdge)
		{
			return Math.Max(width, height) >= minimumLongestEdge;
		}
		return false;
	}
}

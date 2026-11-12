using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HeyClicky.Core.Auth;

public static class AuthDeepLink
{
	public static readonly string[] Schemes = new string[2] { "clicky", "heyclicky" };

	private static readonly Regex EmailPattern = new Regex("[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}", RegexOptions.CultureInvariant);

	private static readonly Regex TokenLikeRunPattern = new Regex("[A-Za-z0-9/_+=.-]{24,}", RegexOptions.CultureInvariant);

	public static bool IsAuthCallback(Uri uri)
	{
		if (uri.IsAbsoluteUri && Schemes.Contains<string>(uri.Scheme, StringComparer.OrdinalIgnoreCase))
		{
			return string.Equals(uri.Host, "auth-callback", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static AuthCallback? Parse(Uri uri)
	{
		if (!IsAuthCallback(uri))
		{
			return null;
		}
		Dictionary<string, string> dictionary = ParseParameters(uri.OriginalString);
		if (!dictionary.TryGetValue("access_token", out var value) || string.IsNullOrEmpty(value))
		{
			return null;
		}
		dictionary.TryGetValue("refresh_token", out var value2);
		string value3;
		int result;
		int? expiresIn = ((dictionary.TryGetValue("expires_in", out value3) && int.TryParse(value3, out result)) ? new int?(result) : ((int?)null));
		dictionary.TryGetValue("referral_code", out var value4);
		dictionary.TryGetValue("clicky_state", out var value5);
		return new AuthCallback(value, string.IsNullOrEmpty(value2) ? null : value2, expiresIn, string.IsNullOrEmpty(value4) ? null : value4, string.IsNullOrEmpty(value5) ? null : value5);
	}

	public static AuthCallbackError? ParseError(Uri uri)
	{
		if (!IsAuthCallback(uri))
		{
			return null;
		}
		Dictionary<string, string> parameters = ParseParameters(uri.OriginalString);
		if (parameters.TryGetValue("access_token", out string value) && !string.IsNullOrEmpty(value))
		{
			return null;
		}
		return new AuthCallbackError(FormDecoded("error"), FormDecoded("error_code"), FormDecoded("error_description"));
		string? FormDecoded(string name)
		{
			if (!parameters.TryGetValue(name, out string value2) || string.IsNullOrWhiteSpace(value2))
			{
				return null;
			}
			return value2.Replace('+', ' ').Trim();
		}
	}

	public static string RedactSignInErrorText(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string input = EmailPattern.Replace(text.Trim(), "[email]");
		input = TokenLikeRunPattern.Replace(input, "[redacted]");
		if (input.Length <= 200)
		{
			return input;
		}
		return input.Substring(0, 200);
	}

	public static Dictionary<string, string> ParseParameters(string url)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		int num = url.IndexOf('?');
		int num2 = url.IndexOf('#');
		if (num >= 0)
		{
			int num3 = ((num2 > num) ? num2 : url.Length);
			AddPairs(url.AsSpan(num + 1, num3 - num - 1), dictionary);
		}
		if (num2 >= 0)
		{
			AddPairs(url.AsSpan(num2 + 1), dictionary);
		}
		return dictionary;
	}

	private static void AddPairs(ReadOnlySpan<char> text, Dictionary<string, string> into)
	{
		MemoryExtensions.SpanSplitEnumerator<char> enumerator = text.Split('&').GetEnumerator();
		while (enumerator.MoveNext())
		{
			Range current = enumerator.Current;
			Range range = current;
			ReadOnlySpan<char> readOnlySpan = text[range.Start..range.End];
			if (!readOnlySpan.IsEmpty)
			{
				int num = readOnlySpan.IndexOf('=');
				ReadOnlySpan<char> readOnlySpan2 = ((num < 0) ? readOnlySpan : readOnlySpan.Slice(0, num));
				ReadOnlySpan<char> readOnlySpan3;
				if (num >= 0)
				{
					int num2 = num + 1;
					readOnlySpan3 = readOnlySpan.Slice(num2, readOnlySpan.Length - num2);
				}
				else
				{
					readOnlySpan3 = ReadOnlySpan<char>.Empty;
				}
				ReadOnlySpan<char> readOnlySpan4 = readOnlySpan3;
				into[Uri.UnescapeDataString(readOnlySpan2.ToString())] = Uri.UnescapeDataString(readOnlySpan4.ToString());
			}
		}
	}
}

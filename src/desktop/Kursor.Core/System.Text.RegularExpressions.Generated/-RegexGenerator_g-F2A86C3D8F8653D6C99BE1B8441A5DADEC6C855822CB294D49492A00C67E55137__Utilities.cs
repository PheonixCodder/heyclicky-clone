using System.Buffers;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
internal static class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	internal static readonly SearchValues<char> s_asciiExceptDigits = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*+,-./:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f");

	internal static readonly SearchValues<char> s_asciiHexDigits = SearchValues.Create("0123456789ABCDEFabcdef");

	internal static readonly SearchValues<char> s_asciiLettersAndDigits = SearchValues.Create("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

	internal static readonly SearchValues<char> s_ascii_FEFFFF87FEFFFF07 = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

	internal static readonly SearchValues<string> s_indexOfString_0FEBAE6F8D3130021BEE242C5C87919F120C14E1F307F3C10E34BFDC4576BC4D;

	internal static readonly SearchValues<string> s_indexOfString_12E1DBB486F0197D53653D68DE3420934C9CD495A60442018C6EBA22DAEBA218;

	internal static readonly SearchValues<string> s_indexOfString_1A32F470B3399637EEACAA4C40413209A17D35821C9C2505754B2D11F229B63A;

	internal static readonly SearchValues<string> s_indexOfString_2385CD56C40C5D62C478D5F27F84F1F9A7A50C0402090FFF8E7761C8AD9A74FC;

	internal static readonly SearchValues<string> s_indexOfString_4203FB690845B95F1E40E1EEB32CE001294F9D26760E6BA546169272699BC784;

	internal static readonly SearchValues<string> s_indexOfString_4A9084D27E350810B767E64C519DE5B703918D35110A6B1184C035CE277E0E26;

	internal static readonly SearchValues<string> s_indexOfString_4B938BB3612E6695167AB8A3A3AD73E3F6DB0CC76E502A73DB7C1EC79B5A2092;

	internal static readonly SearchValues<string> s_indexOfString_6EA455EE8B5DA4F1D7845D265904B991B56C71CC454D10190DEE48BAD102773D;

	internal static readonly SearchValues<string> s_indexOfString_921E30F6B7332314FAB4E2ECB69027ECA9BA90F395F673D24018A15794472969;

	internal static readonly SearchValues<string> s_indexOfString_A35D34A63ABD1CA9D7860076AA22A8E02AF70FE7A4FEF864363EC52527771C59;

	internal static readonly SearchValues<string> s_indexOfString_B6E25B48D6C5403C779E1AB3ECB2F42DD6921933466E65DB70DC7CFC9AC507AD;

	internal static readonly SearchValues<string> s_indexOfString_C19EAA2CA0036A82D9EC9ECB47A1B2E86367A0C3D616F1D0F994C214F6E265B7;

	internal static readonly SearchValues<string> s_indexOfString_C81372FD91E31CDD0ABA0C9786D61E845D30428353FADE938E54007304B3525A;

	internal static readonly SearchValues<string> s_indexOfString_D474EFD1CA39D108A6187B4741FD0C789626191596A3DE21E8493678667EC153;

	internal static readonly SearchValues<string> s_indexOfString_E74F74D0ED66D8490D5B0B1D9BB2497AE7C17B70A136C471D98114C581F89718;

	internal static readonly SearchValues<string> s_indexOfString_beginbfchar_Ordinal;

	internal static readonly SearchValues<string> s_indexOfString_beginbfrange_Ordinal;

	internal static readonly SearchValues<string> s_indexOfString_begincodespacerange_Ordinal;

	internal static readonly SearchValues<char> s_whitespace;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyDigit(this ReadOnlySpan<char> span)
	{
		int num = span.IndexOfAnyExcept(s_asciiExceptDigits);
		if ((uint)num < (uint)span.Length)
		{
			if (char.IsAscii(span[num]))
			{
				return num;
			}
			do
			{
				if (char.IsDigit(span[num]))
				{
					return num;
				}
				num++;
			}
			while ((uint)num < (uint)span.Length);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		return ((uint)num < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[num])) != ((uint)index < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[index]));
		static bool IsBoundaryWordChar(char ch)
		{
			if (!IsWordChar(ch))
			{
				return ch == '\u200c' || ch == '\u200d';
			}
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsWordChar(char ch)
	{
		ReadOnlySpan<byte> readOnlySpan = new byte[16]
		{
			0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
			255, 135, 254, 255, 255, 7
		};
		int num = (int)ch >> 3;
		if ((uint)num >= (uint)readOnlySpan.Length)
		{
			return (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		}
		return (readOnlySpan[num] & (1 << (ch & 7))) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPop(int[] stack, ref int pos, out int arg0, out int arg1)
	{
		arg0 = stack[--pos];
		arg1 = stack[--pos];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)num < (uint)array.Length)
		{
			array[num] = arg0;
			pos++;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] stack, ref int pos, int arg0)
		{
			Array.Resize(ref stack, pos * 2);
			StackPush(ref stack, ref pos, arg0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 1) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			pos += 2;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] stack, ref int pos, int arg0, int arg1)
		{
			Array.Resize(ref stack, (pos + 1) * 2);
			StackPush(ref stack, ref pos, arg0, arg1);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 2) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			array[num + 2] = arg2;
			pos += 3;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1, arg2);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
		{
			Array.Resize(ref stack, (pos + 2) * 2);
			StackPush(ref stack, ref pos, arg0, arg1, arg2);
		}
	}

	static _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities()
	{
		string reference = "\n\n\n";
		s_indexOfString_0FEBAE6F8D3130021BEE242C5C87919F120C14E1F307F3C10E34BFDC4576BC4D = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "<a:t>";
		s_indexOfString_12E1DBB486F0197D53653D68DE3420934C9CD495A60442018C6EBA22DAEBA218 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "/Root";
		s_indexOfString_1A32F470B3399637EEACAA4C40413209A17D35821C9C2505754B2D11F229B63A = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "<w:tab";
		s_indexOfString_2385CD56C40C5D62C478D5F27F84F1F9A7A50C0402090FFF8E7761C8AD9A74FC = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "/Type";
		s_indexOfString_4203FB690845B95F1E40E1EEB32CE001294F9D26760E6BA546169272699BC784 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "==";
		s_indexOfString_4A9084D27E350810B767E64C519DE5B703918D35110A6B1184C035CE277E0E26 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "**";
		s_indexOfString_4B938BB3612E6695167AB8A3A3AD73E3F6DB0CC76E502A73DB7C1EC79B5A2092 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "/Encrypt";
		s_indexOfString_6EA455EE8B5DA4F1D7845D265904B991B56C71CC454D10190DEE48BAD102773D = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "/Length";
		s_indexOfString_921E30F6B7332314FAB4E2ECB69027ECA9BA90F395F673D24018A15794472969 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "![";
		s_indexOfString_A35D34A63ABD1CA9D7860076AA22A8E02AF70FE7A4FEF864363EC52527771C59 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "<text:tab";
		s_indexOfString_B6E25B48D6C5403C779E1AB3ECB2F42DD6921933466E65DB70DC7CFC9AC507AD = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "<si>";
		s_indexOfString_C19EAA2CA0036A82D9EC9ECB47A1B2E86367A0C3D616F1D0F994C214F6E265B7 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "<t";
		s_indexOfString_C81372FD91E31CDD0ABA0C9786D61E845D30428353FADE938E54007304B3525A = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "</w:p>";
		s_indexOfString_D474EFD1CA39D108A6187B4741FD0C789626191596A3DE21E8493678667EC153 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "<w:";
		s_indexOfString_E74F74D0ED66D8490D5B0B1D9BB2497AE7C17B70A136C471D98114C581F89718 = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "beginbfchar";
		s_indexOfString_beginbfchar_Ordinal = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "beginbfrange";
		s_indexOfString_beginbfrange_Ordinal = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		reference = "begincodespacerange";
		s_indexOfString_begincodespacerange_Ordinal = SearchValues.Create(new ReadOnlySpan<string>(in reference), StringComparison.Ordinal);
		s_whitespace = SearchValues.Create("\t\n\v\f\r \u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000");
	}
}

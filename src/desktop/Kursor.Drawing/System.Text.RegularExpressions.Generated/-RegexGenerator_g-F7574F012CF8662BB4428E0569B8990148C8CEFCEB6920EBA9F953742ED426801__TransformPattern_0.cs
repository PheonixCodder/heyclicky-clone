using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__TransformPattern_0 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 7)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 6; num2++)
					{
						int num3 = readOnlySpan.Slice(num2 + 1).IndexOfAny(_003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__Utilities.s_ascii_A880400);
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						if ((uint)(num2 + 3) >= (uint)readOnlySpan.Length)
						{
							break;
						}
						uint num4;
						if ((int)((uint)(-2030043136 << (int)(short)(num4 = (ushort)(readOnlySpan[num2] - 109))) & (num4 - 32)) < 0 && (int)((uint)(-2146156032 << (int)(short)(num4 = (ushort)(readOnlySpan[num2 + 3] - 97))) & (num4 - 32)) < 0)
						{
							runtextpos = num + num2;
							return true;
						}
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num5 = num;
				num4 = num;
				num3 = Crawlpos();
				if (!span.StartsWith("matrix"))
				{
					goto IL_0055;
				}
				num2 = 0;
				num += 6;
				span = inputSpan.Slice(num);
				goto IL_019c;
				IL_010a:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if ((uint)span.Length < 5u || !span.StartsWith("skew") || !char.IsBetween(span[4], 'X', 'Y'))
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = 4;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_019c;
				IL_019c:
				while (true)
				{
					Capture(1, num5, num);
					int i;
					for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
					{
					}
					span = span.Slice(i);
					num += i;
					if (!span.IsEmpty && span[0] == '(')
					{
						num++;
						span = inputSpan.Slice(num);
						num6 = num;
						int num7 = span.IndexOf(')');
						if (num7 < 0)
						{
							num7 = span.Length;
						}
						span = span.Slice(num7);
						num += num7;
						Capture(2, num6, num);
						if (!span.IsEmpty && span[0] == ')')
						{
							Capture(0, start, runtextpos = num + 1);
							return true;
						}
					}
					if (_003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					switch (num2)
					{
					case 0:
						break;
					case 1:
						goto IL_0092;
					case 2:
						goto IL_00ce;
					case 3:
						goto IL_010a;
					case 4:
						UncaptureUntil(0);
						return false;
					default:
						continue;
					}
					break;
				}
				goto IL_0055;
				IL_0055:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (!span.StartsWith("translate"))
				{
					goto IL_0092;
				}
				num2 = 1;
				num += 9;
				span = inputSpan.Slice(num);
				goto IL_019c;
				IL_0092:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (!span.StartsWith("scale"))
				{
					goto IL_00ce;
				}
				num2 = 2;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_019c;
				IL_00ce:
				num = num4;
				span = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (!span.StartsWith("rotate"))
				{
					goto IL_010a;
				}
				num2 = 3;
				num += 6;
				span = inputSpan.Slice(num);
				goto IL_019c;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__TransformPattern_0 Instance = new _003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__TransformPattern_0();

	private _003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__TransformPattern_0()
	{
		pattern = "(matrix|translate|scale|rotate|skewX|skewY)\\s*\\(([^)]*)\\)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF7574F012CF8662BB4428E0569B8990148C8CEFCEB6920EBA9F953742ED426801__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}

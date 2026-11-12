using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MarkRegex_38 : Regex
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
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 4)
				{
					ReadOnlySpan<char> span = inputSpan.Slice(num);
					int num2 = span.IndexOf('{');
					if (num2 >= 0)
					{
						int num3 = num2 - 1;
						while ((uint)num3 < (uint)span.Length && char.IsWhiteSpace(span[num3]))
						{
							num3--;
						}
						runtextpos = num + num3 + 1;
						runtrackpos = num + num2;
						return true;
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
				int capturePosition2 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num = runtrackpos;
				span = inputSpan.Slice(num);
				if (runtextpos < num)
				{
					runtextpos = num;
				}
				if (span.IsEmpty || span[0] != '{')
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 1; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				num7 = num;
				num5 = num;
				num4 = Crawlpos();
				if (!span.IsEmpty && (span[0] | 0x20) == 110)
				{
					num6 = num;
					capturePosition2 = Crawlpos();
					if ((uint)span.Length < 3u || !span.Slice(1).StartsWith("ew", StringComparison.OrdinalIgnoreCase))
					{
						goto IL_010d;
					}
					num3 = 0;
					num += 3;
					span = inputSpan.Slice(num);
					goto IL_016f;
				}
				goto IL_0176;
				IL_04e6:
				while (true)
				{
					Capture(1, num7, num);
					int j;
					for (j = 0; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
					{
					}
					span = span.Slice(j);
					num += j;
					if (span.IsEmpty || span[0] != '}')
					{
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						switch (num2)
						{
						case 0:
							break;
						case 1:
							goto IL_01be;
						case 2:
							goto IL_0229;
						case 3:
							goto IL_0271;
						case 4:
							goto IL_02b9;
						case 5:
							goto IL_0301;
						case 6:
							goto IL_03d5;
						case 7:
							goto IL_041d;
						case 8:
							UncaptureUntil(0);
							return false;
						default:
							continue;
						}
						break;
					}
					Capture(0, start, runtextpos = num + 1);
					return true;
				}
				if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num3 == 0)
				{
					goto IL_010d;
				}
				if (num3 != 1)
				{
					goto IL_016f;
				}
				goto IL_0176;
				IL_016f:
				num2 = 0;
				goto IL_04e6;
				IL_0176:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 4u || !span.StartsWith("done", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_01be;
				}
				num2 = 1;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_04e6;
				IL_03ce:
				num2 = 6;
				goto IL_04e6;
				IL_010d:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition2);
				if ((uint)span.Length >= 5u && span.Slice(1).StartsWith("eeds", StringComparison.OrdinalIgnoreCase))
				{
					num3 = 1;
					num += 5;
					span = inputSpan.Slice(num);
					goto IL_016f;
				}
				goto IL_0176;
				IL_0301:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length >= 5u && span.StartsWith("ruled", StringComparison.OrdinalIgnoreCase) && (uint)span.Length >= 6u)
				{
					char c = span[5];
					if (c != '-')
					{
						if ((c == 'O' || c == 'o') && (uint)span.Length >= 8u && span.Slice(6).StartsWith("ut", StringComparison.OrdinalIgnoreCase))
						{
							num += 8;
							span = inputSpan.Slice(num);
							goto IL_03ce;
						}
					}
					else if ((uint)span.Length >= 9u && span.Slice(6).StartsWith("out", StringComparison.OrdinalIgnoreCase))
					{
						num += 9;
						span = inputSpan.Slice(num);
						goto IL_03ce;
					}
				}
				goto IL_03d5;
				IL_03d5:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 2u || !span.StartsWith("no", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_041d;
				}
				num2 = 7;
				num += 2;
				span = inputSpan.Slice(num);
				goto IL_04e6;
				IL_01be:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				char c2;
				if ((uint)span.Length < 4u || !span.StartsWith("pic", StringComparison.OrdinalIgnoreCase) || (((c2 = span[3]) | 0x20) != 107 && c2 != 'K'))
				{
					goto IL_0229;
				}
				num2 = 2;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_04e6;
				IL_0229:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 3u || !span.StartsWith("now", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0271;
				}
				num2 = 3;
				num += 3;
				span = inputSpan.Slice(num);
				goto IL_04e6;
				IL_041d:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 4u || (span[0] | 0x20) != 115 || (((c2 = span[1]) | 0x20) != 107 && c2 != 'K') || !span.Slice(2).StartsWith("ip", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = 8;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_04e6;
				IL_02b9:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 3u || !span.StartsWith("out", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0301;
				}
				num2 = 5;
				num += 3;
				span = inputSpan.Slice(num);
				goto IL_04e6;
				IL_0271:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 4u || !span.StartsWith("gone", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_02b9;
				}
				num2 = 4;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_04e6;
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MarkRegex_38 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MarkRegex_38();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MarkRegex_38()
	{
		pattern = "\\s*\\{\\s*(new|needs|done|pick|now|gone|out|ruled-out|ruledout|no|skip)\\s*\\}";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}

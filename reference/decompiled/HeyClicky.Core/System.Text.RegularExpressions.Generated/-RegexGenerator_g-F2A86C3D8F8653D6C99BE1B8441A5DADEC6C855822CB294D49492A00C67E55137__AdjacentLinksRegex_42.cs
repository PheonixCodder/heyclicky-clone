using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__AdjacentLinksRegex_42 : Regex
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
				if (num <= inputSpan.Length - 13)
				{
					int num2 = inputSpan.Slice(num).IndexOf('[');
					if (num2 >= 0)
					{
						runtextpos = num + num2;
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num2 = num;
				if (span.IsEmpty || span[0] != '[')
				{
					UncaptureUntil(0);
					return false;
				}
				int num3 = span.Slice(1).IndexOf(']');
				if (num3 < 0)
				{
					num3 = span.Length - 1;
				}
				if (num3 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num3);
				num += num3;
				if (!span.Slice(1).StartsWith("](http"))
				{
					UncaptureUntil(0);
					return false;
				}
				if ((uint)span.Length > 7u && span[7] == 's')
				{
					span = span.Slice(1);
					num++;
				}
				if (!span.Slice(7).StartsWith("://"))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 10;
				span = inputSpan.Slice(num);
				int i;
				for (i = 0; (uint)i < (uint)span.Length; i++)
				{
					char c;
					if ((((c = span[i]) < '\u0080') ? ("쇿\uffff﷾\uffff\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\u0001\u0002\u0001)*d") ? 1 : 0)) == 0)
					{
						break;
					}
				}
				if (i == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(i);
				num += i;
				if (span.IsEmpty || span[0] != ')')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				Capture(1, num2, num);
				int num4 = span.IndexOfAnyExcept('\t', ' ');
				if (num4 < 0)
				{
					num4 = span.Length;
				}
				span = span.Slice(num4);
				num += num4;
				int num5 = num;
				if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (span.IsEmpty || span[0] != '[')
				{
					UncaptureUntil(0);
					return false;
				}
				int num6 = span.Slice(1).IndexOf(']');
				if (num6 < 0)
				{
					num6 = span.Length - 1;
				}
				if (num6 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num6);
				num += num6;
				if (!span.Slice(1).StartsWith("](http"))
				{
					UncaptureUntil(0);
					return false;
				}
				if ((uint)span.Length > 7u && span[7] == 's')
				{
					span = span.Slice(1);
					num++;
				}
				if (!span.Slice(7).StartsWith("://"))
				{
					UncaptureUntil(0);
					return false;
				}
				num = num5;
				span = inputSpan.Slice(num);
				runtextpos = num;
				Capture(0, start, num);
				return true;
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__AdjacentLinksRegex_42 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__AdjacentLinksRegex_42();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__AdjacentLinksRegex_42()
	{
		pattern = "(\\[[^\\]]+\\]\\(https?://[^)\\s]+\\))[ \\t]*(?=\\[[^\\]]+\\]\\(https?://)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}

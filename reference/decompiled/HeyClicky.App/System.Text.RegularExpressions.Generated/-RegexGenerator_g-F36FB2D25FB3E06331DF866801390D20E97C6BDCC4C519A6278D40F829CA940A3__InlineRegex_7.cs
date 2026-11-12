using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__InlineRegex_7 : Regex
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
					if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 3)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_ascii_400000000008801000000);
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
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (span.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (span[0])
				{
				case '*':
				{
					int num9 = num;
					int capturePosition2 = Crawlpos();
					if ((uint)span.Length >= 2u && span[1] == '*')
					{
						num += 2;
						span = inputSpan.Slice(num);
						num2 = num;
						int num10 = span.IndexOf('*');
						if (num10 < 0)
						{
							num10 = span.Length;
						}
						if (num10 != 0)
						{
							span = span.Slice(num10);
							num += num10;
							Capture(1, num2, num);
							if (span.StartsWith("**"))
							{
								num += 2;
								span = inputSpan.Slice(num);
								break;
							}
						}
					}
					num = num9;
					span = inputSpan.Slice(num);
					UncaptureUntil(capturePosition2);
					num++;
					span = inputSpan.Slice(num);
					num3 = num;
					char c;
					if (span.IsEmpty || (((c = span[0]) < '\u0080') ? (("쇿\uffffﯾ\uffff\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\u0001\u0002\u0001*+d"))))
					{
						UncaptureUntil(0);
						return false;
					}
					int num11 = span.Slice(1).IndexOf('*');
					if (num11 < 0)
					{
						num11 = span.Length - 1;
					}
					span = span.Slice(num11);
					num += num11;
					num++;
					span = inputSpan.Slice(num);
					Capture(2, num3, num);
					if (span.IsEmpty || span[0] != '*')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					span = inputSpan.Slice(num);
					break;
				}
				case '_':
				{
					if ((uint)span.Length < 2u || span[1] != '_')
					{
						UncaptureUntil(0);
						return false;
					}
					num += 2;
					span = inputSpan.Slice(num);
					num4 = num;
					int num13 = span.IndexOf('_');
					if (num13 < 0)
					{
						num13 = span.Length;
					}
					if (num13 == 0)
					{
						UncaptureUntil(0);
						return false;
					}
					span = span.Slice(num13);
					num += num13;
					Capture(1, num4, num);
					if (!span.StartsWith("__"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 2;
					span = inputSpan.Slice(num);
					break;
				}
				case '`':
				{
					num++;
					span = inputSpan.Slice(num);
					num5 = num;
					int num12 = span.IndexOf('`');
					if (num12 < 0)
					{
						num12 = span.Length;
					}
					if (num12 == 0)
					{
						UncaptureUntil(0);
						return false;
					}
					span = span.Slice(num12);
					num += num12;
					Capture(3, num5, num);
					if (span.IsEmpty || span[0] != '`')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					span = inputSpan.Slice(num);
					break;
				}
				case '[':
				{
					num++;
					span = inputSpan.Slice(num);
					num6 = num;
					int num8 = span.IndexOf(']');
					if (num8 < 0)
					{
						num8 = span.Length;
					}
					if (num8 == 0)
					{
						UncaptureUntil(0);
						return false;
					}
					span = span.Slice(num8);
					num += num8;
					Capture(4, num6, num);
					if (!span.StartsWith("]("))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 2;
					span = inputSpan.Slice(num);
					num7 = num;
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
					Capture(5, num7, num);
					if (span.IsEmpty || span[0] != ')')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					span = inputSpan.Slice(num);
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
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

	internal static readonly _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__InlineRegex_7 Instance = new _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__InlineRegex_7();

	private _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__InlineRegex_7()
	{
		pattern = "\\*\\*(?<bold>[^*]+)\\*\\*|__(?<bold>[^_]+)__|\\*(?<ital>[^*\\s][^*]*)\\*|`(?<code>[^`]+)`|\\[(?<ltext>[^\\]]+)\\]\\((?<lurl>[^)\\s]+)\\)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "bold", 1 },
			{ "code", 3 },
			{ "ital", 2 },
			{ "ltext", 4 },
			{ "lurl", 5 }
		};
		capslist = new string[6] { "0", "bold", "ital", "code", "ltext", "lurl" };
		capsize = 6;
	}
}

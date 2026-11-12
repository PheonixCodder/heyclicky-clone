using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordTextOrBreak_7 : Regex
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
				if ((uint)num < (uint)inputSpan.Length)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny('\t', '\n', '<');
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
				int arg = 0;
				int arg2 = 0;
				int num3 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				int num4 = pos;
				if (span.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (span[0])
				{
				case '<':
					if (!span.Slice(1).StartsWith("w:t"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 4;
					span = inputSpan.Slice(num);
					num3 = 0;
					while (true)
					{
						_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num3++;
						if (!span.IsEmpty && char.IsWhiteSpace(span[0]))
						{
							num++;
							span = inputSpan.Slice(num);
							arg = num;
							int num5 = span.IndexOf('>');
							if (num5 < 0)
							{
								num5 = span.Length;
							}
							span = span.Slice(num5);
							num += num5;
							arg2 = num;
							goto IL_014f;
						}
						goto IL_016e;
						IL_01cd:
						if (!span.IsEmpty && span[0] == '>')
						{
							num++;
							span = inputSpan.Slice(num);
							num2 = num;
							int num6 = span.IndexOf('<');
							if (num6 < 0)
							{
								num6 = span.Length;
							}
							span = span.Slice(num6);
							num += num6;
							Capture(1, num2, num);
							if (span.StartsWith("</w:t>"))
							{
								break;
							}
						}
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num3 != 0)
						{
							UncaptureUntil(runstack[--pos]);
							_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
							if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (arg < arg2)
							{
								num = --arg2;
								span = inputSpan.Slice(num);
								goto IL_014f;
							}
							goto IL_016e;
						}
						UncaptureUntil(0);
						return false;
						IL_016e:
						if (--num3 < 0)
						{
							UncaptureUntil(0);
							return false;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_01cd;
						IL_014f:
						_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
						if (num3 == 0)
						{
							continue;
						}
						goto IL_01cd;
					}
					num += 6;
					span = inputSpan.Slice(num);
					break;
				case '\t':
				case '\n':
					num++;
					span = inputSpan.Slice(num);
					break;
				default:
					UncaptureUntil(0);
					return false;
				}
				pos = num4;
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordTextOrBreak_7 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordTextOrBreak_7();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__WordTextOrBreak_7()
	{
		pattern = "<w:t(?:\\s[^>]*)?>([^<]*)</w:t>|\\n|\\t";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}

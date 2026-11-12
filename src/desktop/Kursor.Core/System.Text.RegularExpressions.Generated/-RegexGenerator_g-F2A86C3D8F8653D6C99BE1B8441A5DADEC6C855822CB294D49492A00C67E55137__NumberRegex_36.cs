using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__NumberRegex_36 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if ((uint)num < (uint)inputSpan.Length && num == 0)
				{
					return true;
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
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int capturePosition2 = 0;
				int capturePosition3 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				int num14 = 0;
				int num15 = 0;
				int arg = 0;
				int arg2 = 0;
				int num16 = 0;
				int num17 = 0;
				int num18 = 0;
				int num19 = 0;
				int num20 = 0;
				int num21 = 0;
				int num22 = 0;
				int num23 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num5 = num;
				num12 = num;
				char c;
				if (!span.IsEmpty && ((c = span[0]) == '+' || c == '-'))
				{
					span = span.Slice(1);
					num++;
				}
				num13 = num;
				while (true)
				{
					num10 = Crawlpos();
					Capture(1, num5, num);
					num6 = num;
					num21 = 0;
					num22 = num;
					while (true)
					{
						num20 = Crawlpos();
						Capture(2, num6, num);
						num7 = num;
						if (!span.IsEmpty && ((c = span[0]) == '+' || c == '-'))
						{
							span = span.Slice(1);
							num++;
						}
						Capture(3, num7, num);
						num8 = num;
						num4 = num;
						num3 = Crawlpos();
						if (!span.IsEmpty && char.IsDigit(span[0]))
						{
							num++;
							span = inputSpan.Slice(num);
							num14 = num;
							int i;
							for (i = 0; (uint)i < (uint)span.Length; i++)
							{
								if ((((c = span[i]) < '\u0080') ? ("\0\0ကϿ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\0\u0002\u0001,-\t") ? 1 : 0)) == 0)
								{
									break;
								}
							}
							span = span.Slice(i);
							num += i;
							num15 = num;
							goto IL_02ab;
						}
						goto IL_040c;
						IL_03b0:
						if (--num23 < 0)
						{
							goto IL_027b;
						}
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						span = inputSpan.Slice(num);
						goto IL_0405;
						IL_04c7:
						capturePosition3 = Crawlpos();
						num2 = 1;
						goto IL_04ea;
						IL_02b6:
						_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
						num23++;
						if (!span.IsEmpty && span[0] == '.')
						{
							num++;
							span = inputSpan.Slice(num);
							arg = num;
							int j;
							for (j = 0; (uint)j < (uint)span.Length && char.IsDigit(span[j]); j++)
							{
							}
							if (j != 0)
							{
								span = span.Slice(j);
								num += j;
								arg2 = num;
								arg++;
								goto IL_0390;
							}
						}
						goto IL_03b0;
						IL_04ea:
						while (true)
						{
							Capture(4, num8, num);
							num9 = num;
							num18 = num;
							int num24 = span.IndexOf('\n');
							if (num24 < 0)
							{
								num24 = span.Length;
							}
							span = span.Slice(num24);
							num += num24;
							num19 = num;
							while (true)
							{
								num11 = Crawlpos();
								Capture(5, num9, num);
								if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
								{
									UncaptureUntil(num11);
									if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (num18 >= num19)
									{
										break;
									}
									num = --num19;
									span = inputSpan.Slice(num);
									continue;
								}
								runtextpos = num;
								Capture(0, start, num);
								return true;
							}
							if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num2 == 0)
							{
								break;
							}
							if (num2 != 1)
							{
								continue;
							}
							goto IL_0497;
						}
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num23 != 0)
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
								goto IL_0390;
							}
							goto IL_03b0;
						}
						goto IL_027b;
						IL_0497:
						UncaptureUntil(capturePosition3);
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num16 >= num17)
						{
							goto IL_00fe;
						}
						num = --num17;
						span = inputSpan.Slice(num);
						goto IL_04c7;
						IL_0390:
						_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
						if (num23 == 0)
						{
							goto IL_02b6;
						}
						goto IL_0405;
						IL_0405:
						num2 = 0;
						goto IL_04ea;
						IL_040c:
						num = num4;
						span = inputSpan.Slice(num);
						UncaptureUntil(num3);
						if (!span.IsEmpty && span[0] == '.')
						{
							num++;
							span = inputSpan.Slice(num);
							num16 = num;
							int k;
							for (k = 0; (uint)k < (uint)span.Length && char.IsDigit(span[k]); k++)
							{
							}
							if (k != 0)
							{
								span = span.Slice(k);
								num += k;
								num17 = num;
								num16++;
								goto IL_04c7;
							}
						}
						goto IL_00fe;
						IL_02ab:
						capturePosition2 = Crawlpos();
						num23 = 0;
						goto IL_02b6;
						IL_027b:
						UncaptureUntil(capturePosition2);
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num14 < num15)
						{
							num = --num15;
							span = inputSpan.Slice(num);
							goto IL_02ab;
						}
						goto IL_040c;
						IL_00fe:
						UncaptureUntil(num20);
						if (num21 >= 4)
						{
							break;
						}
						num21++;
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						num = num22;
						span = inputSpan.Slice(num);
						if (span.IsEmpty || (((c = span[0]) < '\u0080') ? (("\uffff\uffff響ﰀ\uffff\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\u0001\u0004\u0001+,-/\t"))))
						{
							break;
						}
						num++;
						span = inputSpan.Slice(num);
						num22 = num;
					}
					UncaptureUntil(num10);
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num12 >= num13)
					{
						break;
					}
					num = --num13;
					span = inputSpan.Slice(num);
				}
				UncaptureUntil(0);
				return false;
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__NumberRegex_36 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__NumberRegex_36();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__NumberRegex_36()
	{
		pattern = "^([+-]?)([^\\d+\\-.]{0,4}?)([+-]?)(\\d[\\d,]*(?:\\.\\d+)?|\\.\\d+)(.*)$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 6;
	}
}

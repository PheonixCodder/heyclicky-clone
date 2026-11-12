using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__TableRuleRegex_6 : Regex
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
				if (num <= inputSpan.Length - 2 && num == 0)
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
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int num11 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				if (!span.IsEmpty && span[0] == '|')
				{
					span = span.Slice(1);
					num++;
				}
				int i;
				for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				if (!span.IsEmpty && span[0] == ':')
				{
					span = span.Slice(1);
					num++;
				}
				num5 = num;
				int num12 = span.IndexOfAnyExcept('-');
				if (num12 < 0)
				{
					num12 = span.Length;
				}
				if (num12 < 2)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num12);
				num += num12;
				num6 = num;
				num5 += 2;
				while (true)
				{
					num2 = Crawlpos();
					num7 = num;
					if (!span.IsEmpty && span[0] == ':')
					{
						span = span.Slice(1);
						num++;
					}
					num8 = num;
					while (true)
					{
						num3 = Crawlpos();
						num9 = num;
						int j;
						for (j = 0; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
						{
						}
						span = span.Slice(j);
						num += j;
						num10 = num;
						while (true)
						{
							num4 = Crawlpos();
							num11 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
								num11++;
								int num13 = num;
								if (!span.IsEmpty && span[0] == '|')
								{
									int k;
									for (k = 1; (uint)k < (uint)span.Length && char.IsWhiteSpace(span[k]); k++)
									{
									}
									span = span.Slice(k);
									num += k;
									if (!span.IsEmpty && span[0] == ':')
									{
										span = span.Slice(1);
										num++;
									}
									arg = num;
									int num14 = span.IndexOfAnyExcept('-');
									if (num14 < 0)
									{
										num14 = span.Length;
									}
									if (num14 >= 2)
									{
										span = span.Slice(num14);
										num += num14;
										arg2 = num;
										arg += 2;
										goto IL_034c;
									}
								}
								while (--num11 >= 0)
								{
									num = runstack[--pos];
									UncaptureUntil(runstack[--pos]);
									span = inputSpan.Slice(num);
									if (!span.IsEmpty && span[0] == '|')
									{
										span = span.Slice(1);
										num++;
									}
									if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
									{
										if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
										{
											CheckTimeout();
										}
										if (num11 == 0)
										{
											break;
										}
										num13 = runstack[--pos];
										UncaptureUntil(runstack[--pos]);
										_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPop(runstack, ref pos, out arg6, out arg5);
										if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
										{
											CheckTimeout();
										}
										if (arg5 >= arg6)
										{
											UncaptureUntil(runstack[--pos]);
											_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
											if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											if (arg3 >= arg4)
											{
												UncaptureUntil(runstack[--pos]);
												_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
												if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
												{
													CheckTimeout();
												}
												if (arg >= arg2)
												{
													continue;
												}
												goto IL_033a;
											}
											goto IL_03ca;
										}
										goto IL_046d;
									}
									runtextpos = num;
									Capture(0, start, num);
									return true;
								}
								break;
								IL_03dc:
								_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
								arg5 = num;
								int l;
								for (l = 0; (uint)l < (uint)span.Length && char.IsWhiteSpace(span[l]); l++)
								{
								}
								span = span.Slice(l);
								num += l;
								arg6 = num;
								goto IL_047f;
								IL_034c:
								_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
								arg3 = num;
								if (!span.IsEmpty && span[0] == ':')
								{
									span = span.Slice(1);
									num++;
								}
								arg4 = num;
								goto IL_03dc;
								IL_047f:
								_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, arg5, arg6, Crawlpos());
								Capture(1, num13, num);
								_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, num13);
								continue;
								IL_046d:
								num = --arg6;
								span = inputSpan.Slice(num);
								goto IL_047f;
								IL_03ca:
								num = --arg4;
								span = inputSpan.Slice(num);
								goto IL_03dc;
								IL_033a:
								num = --arg2;
								span = inputSpan.Slice(num);
								goto IL_034c;
							}
							UncaptureUntil(num4);
							if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num9 >= num10)
							{
								break;
							}
							num = --num10;
							span = inputSpan.Slice(num);
						}
						UncaptureUntil(num3);
						if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num7 >= num8)
						{
							break;
						}
						num = --num8;
						span = inputSpan.Slice(num);
					}
					UncaptureUntil(num2);
					if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num5 >= num6)
					{
						break;
					}
					num = --num6;
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

	internal static readonly _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__TableRuleRegex_6 Instance = new _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__TableRuleRegex_6();

	private _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__TableRuleRegex_6()
	{
		pattern = "^\\|?\\s*:?-{2,}:?\\s*(\\|\\s*:?-{2,}:?\\s*)*\\|?$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}

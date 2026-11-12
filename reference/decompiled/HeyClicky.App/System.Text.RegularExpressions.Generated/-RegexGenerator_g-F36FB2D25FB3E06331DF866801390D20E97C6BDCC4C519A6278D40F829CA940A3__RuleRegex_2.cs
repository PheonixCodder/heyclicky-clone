using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__RuleRegex_2 : Regex
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
				int pos = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[i]); i++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				num2 = num;
				char c;
				if (readOnlySpan.IsEmpty || ((c = readOnlySpan[0]) != '*' && c != '-' && c != '_'))
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				Capture(1, num2, num);
				num9 = num;
				num8 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num9, num);
					num9 = num;
					num8++;
					int num11 = num;
					num4 = num;
					int j;
					for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
					{
					}
					readOnlySpan = readOnlySpan.Slice(j);
					num += j;
					num5 = num;
					while (true)
					{
						_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, num4, num5, Crawlpos());
						if (IsMatched(1))
						{
							num10 = MatchLength(1);
							if (readOnlySpan.Length >= num10 && inputSpan.Slice(MatchIndex(1), num10).SequenceEqual(readOnlySpan.Slice(0, num10)))
							{
								num += num10;
								readOnlySpan = inputSpan.Slice(num);
								Capture(2, num11, num);
								_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPush(ref runstack, ref pos, num11);
								if (num != num9 || num8 < 2)
								{
									break;
								}
								goto IL_0297;
							}
						}
						goto IL_012b;
						IL_0297:
						num6 = num;
						int k;
						for (k = 0; (uint)k < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[k]); k++)
						{
						}
						readOnlySpan = readOnlySpan.Slice(k);
						num += k;
						num7 = num;
						while (true)
						{
							num3 = Crawlpos();
							if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
							{
								UncaptureUntil(num3);
								if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num6 >= num7)
								{
									break;
								}
								num = --num7;
								readOnlySpan = inputSpan.Slice(num);
								continue;
							}
							runtextpos = num;
							Capture(0, start, num);
							return true;
						}
						if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num8 == 0)
						{
							UncaptureUntil(0);
							return false;
						}
						goto IL_01fd;
						IL_012b:
						UncaptureUntil(runstack[--pos]);
						_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPop(runstack, ref pos, out num5, out num4);
						if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num4 < num5)
						{
							num = --num5;
							readOnlySpan = inputSpan.Slice(num);
							continue;
						}
						if (--num8 < 0)
						{
							UncaptureUntil(0);
							return false;
						}
						_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.StackPop(runstack, ref pos, out num, out num9);
						UncaptureUntil(runstack[--pos]);
						readOnlySpan = inputSpan.Slice(num);
						if (num8 == 0)
						{
							UncaptureUntil(0);
							return false;
						}
						if (num8 < 2)
						{
							goto IL_01fd;
						}
						goto IL_0297;
						IL_01fd:
						num11 = runstack[--pos];
						goto IL_012b;
					}
				}
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

	internal static readonly _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__RuleRegex_2 Instance = new _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__RuleRegex_2();

	private _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__RuleRegex_2()
	{
		pattern = "^\\s*([-*_])(\\s*\\1){2,}\\s*$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}

using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__HeadingRegex_1 : Regex
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = num;
				int i;
				for (i = 0; i < 6 && (uint)i < (uint)span.Length && span[i] == '#'; i++)
				{
				}
				if (i == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(i);
				num += i;
				Capture(1, num2, num);
				num6 = num;
				int j;
				for (j = 0; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
				{
				}
				if (j == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(j);
				num += j;
				num7 = num;
				num6++;
				while (true)
				{
					num4 = Crawlpos();
					num3 = num;
					num8 = num;
					int num10 = span.IndexOf('\n');
					if (num10 < 0)
					{
						num10 = span.Length;
					}
					span = span.Slice(num10);
					num += num10;
					num9 = num;
					while (true)
					{
						num5 = Crawlpos();
						Capture(2, num3, num);
						if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
						{
							UncaptureUntil(num5);
							if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num8 >= num9)
							{
								break;
							}
							num = --num9;
							span = inputSpan.Slice(num);
							continue;
						}
						runtextpos = num;
						Capture(0, start, num);
						return true;
					}
					UncaptureUntil(num4);
					if (_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num6 >= num7)
					{
						break;
					}
					num = --num7;
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

	internal static readonly _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__HeadingRegex_1 Instance = new _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__HeadingRegex_1();

	private _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__HeadingRegex_1()
	{
		pattern = "^(#{1,6})\\s+(.*)$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}

using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__BulletRegex_3 : Regex
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[i]); i++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				char c;
				if (readOnlySpan.IsEmpty || ((c = readOnlySpan[0]) != '*' && c != '-'))
				{
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				int j;
				for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
				{
				}
				if (j == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(j);
				Capture(0, start, runtextpos = num + j);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__BulletRegex_3 Instance = new _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__BulletRegex_3();

	private _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__BulletRegex_3()
	{
		pattern = "^\\s*[-*]\\s+";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}

using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ScriptBlock_15 : Regex
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
				if (num <= inputSpan.Length - 16)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 15; num2++)
					{
						int num3 = readOnlySpan.Slice(num2).IndexOf('<');
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						if ((uint)(num2 + 5) >= (uint)readOnlySpan.Length)
						{
							break;
						}
						if ((readOnlySpan[num2 + 5] | 0x20) == 112 && (readOnlySpan[num2 + 2] | 0x20) == 99)
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if ((uint)readOnlySpan.Length < 7u || readOnlySpan[0] != '<' || !readOnlySpan.Slice(1).StartsWith("script", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				num += 7;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				while ((uint)readOnlySpan.Length < 9u || !readOnlySpan.StartsWith("</") || !readOnlySpan.Slice(2).StartsWith("script", StringComparison.OrdinalIgnoreCase) || readOnlySpan[8] != '>')
				{
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num2;
					if (inputSpan.Slice(num).IsEmpty)
					{
						return false;
					}
					num++;
					readOnlySpan = inputSpan.Slice(num);
					num2 = readOnlySpan.IndexOf("</");
					if (num2 < 0)
					{
						return false;
					}
					num += num2;
					readOnlySpan = inputSpan.Slice(num);
					num2 = num;
				}
				Capture(0, start, runtextpos = num + 9);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ScriptBlock_15 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ScriptBlock_15();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ScriptBlock_15()
	{
		pattern = "<script.*?</script>";
		roptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}

using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__LeadingCommaPattern_1 : Regex
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
					if (_003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 2)
				{
					if (num <= 0 || inputSpan[num - 1] == '\n')
					{
						goto IL_0058;
					}
					int num2 = inputSpan.Slice(num).IndexOf('\n');
					if ((uint)num2 <= inputSpan.Length - num - 1)
					{
						num += num2 + 1;
						if (num <= inputSpan.Length - 2)
						{
							goto IL_0058;
						}
					}
				}
				goto IL_007a;
				IL_0058:
				int num3 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__Utilities.s_indexOfString_216CC4263C7DA6B2A60D4BCBE1F21E6D43372DC59E18D4D1A32EBDA2E2D2E2D0);
				if (num3 >= 0)
				{
					runtextpos = num + num3;
					return true;
				}
				goto IL_007a;
				IL_007a:
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num > 0 && inputSpan[num - 1] != '\n')
				{
					return false;
				}
				if (!span.StartsWith(", "))
				{
					return false;
				}
				Capture(0, start, runtextpos = num + 2);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__LeadingCommaPattern_1 Instance = new _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__LeadingCommaPattern_1();

	private _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__LeadingCommaPattern_1()
	{
		pattern = "(?m)^, ";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}

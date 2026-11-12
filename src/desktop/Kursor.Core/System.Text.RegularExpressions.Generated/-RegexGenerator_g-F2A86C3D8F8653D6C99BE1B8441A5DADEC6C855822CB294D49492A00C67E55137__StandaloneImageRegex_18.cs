using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__StandaloneImageRegex_18 : Regex
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
				if (num <= inputSpan.Length - 6 && num == 0)
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
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				if (!span.StartsWith("!["))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 2;
				span = inputSpan.Slice(num);
				num2 = num;
				int num10 = span.IndexOf(']');
				if (num10 < 0)
				{
					num10 = span.Length;
				}
				span = span.Slice(num10);
				num += num10;
				Capture(1, num2, num);
				if (!span.StartsWith("]("))
				{
					UncaptureUntil(0);
					return false;
				}
				num += 2;
				span = inputSpan.Slice(num);
				num5 = num;
				int i;
				for (i = 0; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				num6 = num;
				while (true)
				{
					num4 = Crawlpos();
					num3 = num;
					if (!span.IsEmpty && !char.IsWhiteSpace(span[0]))
					{
						num++;
						span = inputSpan.Slice(num);
						num8 = num;
						while (true)
						{
							num7 = Crawlpos();
							Capture(2, num3, num);
							num9 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
								num9++;
								int j;
								for (j = 0; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
								{
								}
								if (j != 0)
								{
									span = span.Slice(j);
									num += j;
									if (!span.IsEmpty && span[0] == '"')
									{
										int num11 = span.Slice(1).IndexOf('"');
										if (num11 < 0)
										{
											num11 = span.Length - 1;
										}
										span = span.Slice(num11);
										num += num11;
										if ((uint)span.Length >= 2u && span[1] == '"')
										{
											num += 2;
											span = inputSpan.Slice(num);
											if (num9 == 0)
											{
												continue;
											}
											goto IL_02d8;
										}
									}
								}
								goto IL_029e;
								IL_029e:
								if (--num9 < 0)
								{
									break;
								}
								num = runstack[--pos];
								UncaptureUntil(runstack[--pos]);
								span = inputSpan.Slice(num);
								goto IL_02d8;
								IL_02d8:
								int k;
								for (k = 0; (uint)k < (uint)span.Length && char.IsWhiteSpace(span[k]); k++)
								{
								}
								span = span.Slice(k);
								num += k;
								if (!span.IsEmpty && span[0] == ')' && 2 >= span.Length && (1 >= span.Length || span[1] == '\n'))
								{
									Capture(0, start, runtextpos = num + 1);
									return true;
								}
								goto IL_029e;
							}
							UncaptureUntil(num7);
							if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							num = num8;
							span = inputSpan.Slice(num);
							if (span.IsEmpty || char.IsWhiteSpace(span[0]))
							{
								break;
							}
							num++;
							span = inputSpan.Slice(num);
							num8 = num;
						}
					}
					UncaptureUntil(num4);
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__StandaloneImageRegex_18 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__StandaloneImageRegex_18();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__StandaloneImageRegex_18()
	{
		pattern = "^!\\[([^\\]]*)\\]\\(\\s*(\\S+?)(?:\\s+\"[^\"]*\")?\\s*\\)$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}

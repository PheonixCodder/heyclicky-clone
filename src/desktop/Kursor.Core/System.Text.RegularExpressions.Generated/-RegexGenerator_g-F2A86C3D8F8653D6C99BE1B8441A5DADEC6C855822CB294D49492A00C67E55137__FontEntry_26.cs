using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__FontEntry_26 : Regex
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
				if (num <= inputSpan.Length - 8)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 7; num2++)
					{
						int num3 = readOnlySpan.Slice(num2).IndexOf('/');
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						if ((uint)(num2 + 1) >= (uint)readOnlySpan.Length)
						{
							break;
						}
						char c;
						if (((c = readOnlySpan[num2 + 1]) < '\u0080') ? ((byte)("쇿\uffff糾꿿\uffff\ud7ff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\u0001\f\u0001(*/0<=>?[\\]^d"))
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
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '/')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				num5 = num;
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length; i++)
				{
					char c;
					if ((((c = readOnlySpan[i]) < '\u0080') ? ("쇿\uffff糾꿿\uffff\ud7ff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\u0001\f\u0001(*/0<=>?[\\]^d") ? 1 : 0)) == 0)
					{
						break;
					}
				}
				if (i == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				num6 = num;
				num5++;
				while (true)
				{
					num4 = Crawlpos();
					Capture(1, num2, num);
					int j;
					for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
					{
					}
					if (j != 0)
					{
						readOnlySpan = readOnlySpan.Slice(j);
						num += j;
						num3 = num;
						int k;
						for (k = 0; (uint)k < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[k]); k++)
						{
						}
						if (k != 0)
						{
							readOnlySpan = readOnlySpan.Slice(k);
							num += k;
							Capture(2, num3, num);
							int l;
							for (l = 0; (uint)l < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[l]); l++)
							{
							}
							if (l != 0)
							{
								readOnlySpan = readOnlySpan.Slice(l);
								num += l;
								int m;
								for (m = 0; (uint)m < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[m]); m++)
								{
								}
								if (m != 0)
								{
									readOnlySpan = readOnlySpan.Slice(m);
									num += m;
									int n;
									for (n = 0; (uint)n < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[n]); n++)
									{
									}
									if (n != 0)
									{
										readOnlySpan = readOnlySpan.Slice(n);
										num += n;
										if (!readOnlySpan.IsEmpty && readOnlySpan[0] == 'R')
										{
											break;
										}
									}
								}
							}
						}
					}
					UncaptureUntil(num4);
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num5 >= num6)
					{
						UncaptureUntil(0);
						return false;
					}
					num = --num6;
					readOnlySpan = inputSpan.Slice(num);
				}
				Capture(0, start, runtextpos = num + 1);
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__FontEntry_26 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__FontEntry_26();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__FontEntry_26()
	{
		pattern = "/([^\\s/<>\\[\\]()]+)\\s+(\\d+)\\s+\\d+\\s+R";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}

using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__TimedLineRegex_43 : Regex
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
				int capturePosition2 = 0;
				int capturePosition3 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int num11 = 0;
				int num12 = 0;
				int num13 = 0;
				int num14 = 0;
				int num15 = 0;
				int num16 = 0;
				int num17 = 0;
				int pos = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num5 = num;
				num4 = num;
				num3 = Crawlpos();
				num7 = num;
				int i;
				for (i = 0; i < 2 && (uint)i < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[i]); i++)
				{
				}
				if (i != 0)
				{
					readOnlySpan = readOnlySpan.Slice(i);
					num += i;
					num8 = num;
					num7++;
					goto IL_00f2;
				}
				goto IL_080d;
				IL_020b:
				capturePosition3 = Crawlpos();
				num14 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num14++;
					if (readOnlySpan.IsEmpty)
					{
						break;
					}
					switch (readOnlySpan[0])
					{
					case 'a':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'm')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0312;
					case 'p':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'm')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0312;
					case 'A':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'M')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0312;
					case 'P':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'M')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0312;
					}
					break;
					IL_0312:
					if (num14 == 0)
					{
						continue;
					}
					goto IL_0355;
				}
				goto IL_031b;
				IL_0355:
				num15 = 0;
				goto IL_0358;
				IL_04ad:
				_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
				num16 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num16++;
					if ((uint)readOnlySpan.Length < 3u || readOnlySpan[0] != ':' || !char.IsDigit(readOnlySpan[1]) || !char.IsDigit(readOnlySpan[2]))
					{
						break;
					}
					num += 3;
					readOnlySpan = inputSpan.Slice(num);
					if (num16 == 0)
					{
						continue;
					}
					goto IL_0566;
				}
				goto IL_052c;
				IL_0ab5:
				char c;
				while (true)
				{
					Capture(1, num5, num);
					num11 = num;
					int j;
					for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
					{
					}
					readOnlySpan = readOnlySpan.Slice(j);
					num += j;
					num12 = num;
					while (true)
					{
						num6 = Crawlpos();
						if (!readOnlySpan.IsEmpty && (((c = readOnlySpan[0]) < '\u0080') ? ((byte)("\0\0\u2000Ѐ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\b\0-.:;·\u00b8—―")))
						{
							readOnlySpan = readOnlySpan.Slice(1);
							num++;
						}
						int k;
						for (k = 0; (uint)k < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[k]); k++)
						{
						}
						if (k == 0)
						{
							UncaptureUntil(num6);
							if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num11 >= num12)
							{
								break;
							}
							num = --num12;
							readOnlySpan = inputSpan.Slice(num);
							continue;
						}
						readOnlySpan = readOnlySpan.Slice(k);
						Capture(0, start, runtextpos = num + k);
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
					if (num2 == 1)
					{
						UncaptureUntil(0);
						return false;
					}
				}
				if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num15 != 0)
				{
					num17 = runstack[--pos];
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					goto IL_0741;
				}
				goto IL_031b;
				IL_0162:
				if (--num13 < 0)
				{
					UncaptureUntil(capturePosition2);
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num7 < num8)
					{
						num = --num8;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_00f2;
					}
					goto IL_080d;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				readOnlySpan = inputSpan.Slice(num);
				goto IL_019c;
				IL_07b4:
				if (--num15 < 0)
				{
					goto IL_031b;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				readOnlySpan = inputSpan.Slice(num);
				goto IL_0806;
				IL_0806:
				num2 = 0;
				goto IL_0ab5;
				IL_00f2:
				capturePosition2 = Crawlpos();
				num13 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num13++;
					if ((uint)readOnlySpan.Length < 3u || readOnlySpan[0] != ':' || !char.IsDigit(readOnlySpan[1]) || !char.IsDigit(readOnlySpan[2]))
					{
						break;
					}
					num += 3;
					readOnlySpan = inputSpan.Slice(num);
					if (num13 == 0)
					{
						continue;
					}
					goto IL_019c;
				}
				goto IL_0162;
				IL_0622:
				_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
				num17 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num17++;
					if (readOnlySpan.IsEmpty)
					{
						break;
					}
					switch (readOnlySpan[0])
					{
					case 'a':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'm')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0738;
					case 'p':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'm')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0738;
					case 'A':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'M')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0738;
					case 'P':
						if ((uint)readOnlySpan.Length < 2u || readOnlySpan[1] != 'M')
						{
							break;
						}
						num += 2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_0738;
					}
					break;
					IL_0738:
					if (num17 == 0)
					{
						continue;
					}
					goto IL_077b;
				}
				goto IL_0741;
				IL_052c:
				if (--num16 < 0)
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
						readOnlySpan = inputSpan.Slice(num);
						goto IL_04ad;
					}
					goto IL_07b4;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				readOnlySpan = inputSpan.Slice(num);
				goto IL_0566;
				IL_0741:
				if (--num17 < 0)
				{
					UncaptureUntil(runstack[--pos]);
					_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (arg3 >= arg4)
					{
						num16 = runstack[--pos];
						if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						goto IL_052c;
					}
					num = --arg4;
					readOnlySpan = inputSpan.Slice(num);
					goto IL_0622;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				readOnlySpan = inputSpan.Slice(num);
				goto IL_077b;
				IL_0566:
				_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, num16);
				arg3 = num;
				int l;
				for (l = 0; (uint)l < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[l]); l++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(l);
				num += l;
				arg4 = num;
				goto IL_0622;
				IL_080d:
				num = num4;
				readOnlySpan = inputSpan.Slice(num);
				UncaptureUntil(num3);
				if (readOnlySpan.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (readOnlySpan[0])
				{
				case 'M':
					if (!readOnlySpan.Slice(1).StartsWith("orning"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 7;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'A':
					if (!readOnlySpan.Slice(1).StartsWith("fternoon"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 9;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'E':
					if (!readOnlySpan.Slice(1).StartsWith("vening"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 7;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'N':
					if (!readOnlySpan.Slice(1).StartsWith("ight"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 5;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'L':
					if (!readOnlySpan.Slice(1).StartsWith("unch"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 5;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'D':
					if (!readOnlySpan.Slice(1).StartsWith("inner"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 6;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case 'B':
					if (!readOnlySpan.Slice(1).StartsWith("reakfast"))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 9;
					readOnlySpan = inputSpan.Slice(num);
					break;
				default:
					UncaptureUntil(0);
					return false;
				}
				int num18 = num;
				if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				int m;
				for (m = 0; (uint)m < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[m]); m++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(m);
				num += m;
				if (readOnlySpan.IsEmpty || (((c = readOnlySpan[0]) < '\u0080') ? (("\0\0\u2000Ѐ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) == 0) : (!RegexRunner.CharInClass(c, "\0\b\0-.:;·\u00b8—―"))))
				{
					UncaptureUntil(0);
					return false;
				}
				num = num18;
				readOnlySpan = inputSpan.Slice(num);
				num2 = 1;
				goto IL_0ab5;
				IL_019c:
				num9 = num;
				int n;
				for (n = 0; (uint)n < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[n]); n++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(n);
				num += n;
				num10 = num;
				goto IL_020b;
				IL_077b:
				_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, num17);
				if (num15 == 0)
				{
					goto IL_0358;
				}
				goto IL_0806;
				IL_0358:
				_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
				num15++;
				int num19;
				for (num19 = 0; (uint)num19 < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[num19]); num19++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(num19);
				num += num19;
				if (!readOnlySpan.IsEmpty && !((c = readOnlySpan[0]) != '-' && c != '–'))
				{
					int num20;
					for (num20 = 1; (uint)num20 < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[num20]); num20++)
					{
					}
					readOnlySpan = readOnlySpan.Slice(num20);
					num += num20;
					arg = num;
					int num21;
					for (num21 = 0; num21 < 2 && (uint)num21 < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[num21]); num21++)
					{
					}
					if (num21 != 0)
					{
						readOnlySpan = readOnlySpan.Slice(num21);
						num += num21;
						arg2 = num;
						arg++;
						goto IL_04ad;
					}
				}
				goto IL_07b4;
				IL_031b:
				if (--num14 < 0)
				{
					UncaptureUntil(capturePosition3);
					if (_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num9 >= num10)
					{
						goto IL_0162;
					}
					num = --num10;
					readOnlySpan = inputSpan.Slice(num);
					goto IL_020b;
				}
				num = runstack[--pos];
				UncaptureUntil(runstack[--pos]);
				readOnlySpan = inputSpan.Slice(num);
				goto IL_0355;
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

	internal static readonly _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__TimedLineRegex_43 Instance = new _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__TimedLineRegex_43();

	private _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__TimedLineRegex_43()
	{
		pattern = "^((?:\\d{1,2}(?::\\d{2})?\\s*(?:am|pm|AM|PM)?)(?:\\s*[–-]\\s*\\d{1,2}(?::\\d{2})?\\s*(?:am|pm|AM|PM)?)?|(?:Morning|Afternoon|Evening|Night|Lunch|Dinner|Breakfast)(?=\\s*[:·—-]))\\s*[:·—-]?\\s+";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}

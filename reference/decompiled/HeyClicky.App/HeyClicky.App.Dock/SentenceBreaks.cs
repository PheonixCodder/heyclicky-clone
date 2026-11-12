using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace HeyClicky.App.Dock;

public static class SentenceBreaks
{
	private enum Kind
	{
		Other,
		CR,
		LF,
		Sep,
		Sp,
		Lower,
		Upper,
		OLetter,
		Numeric,
		ATerm,
		STerm,
		Close,
		SContinue,
		Extend
	}

	[CompilerGenerated]
	private sealed class _003CSentences_003Ed__1 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private string text;

		public string _003C_003E3__text;

		private List<int> _003Cstarts_003E5__2;

		private List<Kind> _003Ckinds_003E5__3;

		private int _003Ci_003E5__4;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CSentences_003Ed__1(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Cstarts_003E5__2 = null;
			_003Ckinds_003E5__3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			int num;
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				_003Cstarts_003E5__2 = new List<int>();
				_003Ckinds_003E5__3 = new List<Kind>();
				int num2 = 0;
				while (num2 < this.text.Length)
				{
					Rune runeAt = Rune.GetRuneAt(this.text, num2);
					Kind kind = KindOf(runeAt);
					bool flag = kind == Kind.Extend && _003Ckinds_003E5__3.Count > 0;
					if (flag)
					{
						List<Kind> list = _003Ckinds_003E5__3;
						Kind kind2 = list[list.Count - 1];
						bool flag2 = (uint)(kind2 - 1) <= 2u;
						flag = !flag2;
					}
					if (flag)
					{
						num2 += runeAt.Utf16SequenceLength;
						continue;
					}
					_003Cstarts_003E5__2.Add(num2);
					_003Ckinds_003E5__3.Add((kind != Kind.Extend) ? kind : Kind.Other);
					num2 += runeAt.Utf16SequenceLength;
				}
				num = 0;
				_003Ci_003E5__4 = 1;
				goto IL_0165;
			}
			case 1:
				_003C_003E1__state = -1;
				num = _003Cstarts_003E5__2[_003Ci_003E5__4];
				goto IL_0153;
			case 2:
				{
					_003C_003E1__state = -1;
					break;
				}
				IL_0153:
				_003Ci_003E5__4++;
				goto IL_0165;
				IL_0165:
				if (_003Ci_003E5__4 < _003Ckinds_003E5__3.Count)
				{
					if (BreaksBefore(_003Ckinds_003E5__3, _003Ci_003E5__4))
					{
						string obj = this.text;
						int num3 = num;
						_003C_003E2__current = obj.Substring(num3, _003Cstarts_003E5__2[_003Ci_003E5__4] - num3);
						_003C_003E1__state = 1;
						return true;
					}
					goto IL_0153;
				}
				if (num < this.text.Length)
				{
					string text = this.text;
					int num3 = num;
					_003C_003E2__current = text.Substring(num3, text.Length - num3);
					_003C_003E1__state = 2;
					return true;
				}
				break;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CSentences_003Ed__1 _003CSentences_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CSentences_003Ed__ = this;
			}
			else
			{
				_003CSentences_003Ed__ = new _003CSentences_003Ed__1(0);
			}
			_003CSentences_003Ed__.text = _003C_003E3__text;
			return _003CSentences_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	[IteratorStateMachine(typeof(_003CSentences_003Ed__1))]
	public static IEnumerable<string> Sentences(string text)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CSentences_003Ed__1(-2)
		{
			_003C_003E3__text = text
		};
	}

	private static bool BreaksBefore(List<Kind> c, int i)
	{
		Kind kind = c[i - 1];
		Kind kind2 = c[i];
		if (kind == Kind.CR && kind2 == Kind.LF)
		{
			return false;
		}
		if ((uint)(kind - 1) <= 2u)
		{
			return true;
		}
		if (kind == Kind.ATerm && kind2 == Kind.Numeric)
		{
			return false;
		}
		bool flag = kind == Kind.ATerm && kind2 == Kind.Upper && i >= 2;
		if (flag)
		{
			Kind kind3 = c[i - 2];
			bool flag2 = (uint)(kind3 - 5) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			return false;
		}
		int num = i - 1;
		while (num >= 0 && c[num] == Kind.Sp)
		{
			num--;
		}
		bool flag3 = num < i - 1;
		while (num >= 0 && c[num] == Kind.Close)
		{
			num--;
		}
		flag = num < 0;
		if (!flag)
		{
			Kind kind3 = c[num];
			bool flag2 = (uint)(kind3 - 9) <= 1u;
			flag = !flag2;
		}
		if (flag)
		{
			return false;
		}
		if (c[num] == Kind.ATerm)
		{
			int num2 = i;
			while (true)
			{
				flag = num2 < c.Count;
				if (flag)
				{
					bool flag2;
					switch (c[num2])
					{
					case Kind.CR:
					case Kind.LF:
					case Kind.Sep:
					case Kind.Lower:
					case Kind.Upper:
					case Kind.OLetter:
					case Kind.ATerm:
					case Kind.STerm:
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					flag = !flag2;
				}
				if (!flag)
				{
					break;
				}
				num2++;
			}
			if (num2 < c.Count && c[num2] == Kind.Lower)
			{
				return false;
			}
		}
		if (((uint)(kind2 - 9) <= 1u || kind2 == Kind.SContinue) ? true : false)
		{
			return false;
		}
		flag = !flag3;
		if (flag)
		{
			bool flag2 = (((uint)(kind2 - 1) <= 3u || kind2 == Kind.Close) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return false;
		}
		if ((uint)(kind2 - 1) <= 3u)
		{
			return false;
		}
		return true;
	}

	private static Kind KindOf(Rune rune)
	{
		switch (rune.Value)
		{
		case 13:
			return Kind.CR;
		case 10:
			return Kind.LF;
		case 133:
		case 8232:
		case 8233:
			return Kind.Sep;
		case 46:
		case 8228:
		case 65106:
		case 65294:
			return Kind.ATerm;
		case 33:
		case 63:
		case 1417:
		case 1567:
		case 1748:
		case 1792:
		case 1793:
		case 1794:
		case 2404:
		case 2405:
		case 8252:
		case 8253:
		case 8263:
		case 8264:
		case 8265:
		case 12290:
		case 65110:
		case 65111:
		case 65281:
		case 65311:
		case 65377:
			return Kind.STerm;
		case 44:
		case 45:
		case 58:
		case 1373:
		case 1548:
		case 1549:
		case 2040:
		case 6146:
		case 6152:
		case 8211:
		case 8212:
		case 12289:
		case 65040:
		case 65041:
		case 65043:
		case 65073:
		case 65074:
		case 65104:
		case 65105:
		case 65109:
		case 65112:
		case 65123:
		case 65292:
		case 65293:
		case 65306:
		case 65380:
			return Kind.SContinue;
		case 34:
		case 39:
		case 171:
		case 187:
			return Kind.Close;
		default:
			if (Rune.IsWhiteSpace(rune))
			{
				return Kind.Sp;
			}
			if (Rune.IsDigit(rune))
			{
				return Kind.Numeric;
			}
			switch (Rune.GetUnicodeCategory(rune))
			{
			case UnicodeCategory.LowercaseLetter:
				return Kind.Lower;
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
				return Kind.Upper;
			case UnicodeCategory.ModifierLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.LetterNumber:
				return Kind.OLetter;
			case UnicodeCategory.OpenPunctuation:
			case UnicodeCategory.ClosePunctuation:
			case UnicodeCategory.InitialQuotePunctuation:
			case UnicodeCategory.FinalQuotePunctuation:
				return Kind.Close;
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.EnclosingMark:
			case UnicodeCategory.Format:
				return Kind.Extend;
			default:
				return Kind.Other;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkStreak
{
	public const int MaximumDays = 42;

	public static List<ClickyInkStreakDay> Days(string? text)
	{
		string text2 = text?.TrimWs() ?? "";
		if (text2.Length == 0)
		{
			return new List<ClickyInkStreakDay>();
		}
		List<string> list = text2.SplitCharacters((string c) => (c == "," || c == " ") ? true : false);
		List<ClickyInkStreakDay> list2 = ((list.Count <= 1 || !list.Any((string w) => w.CharacterCount() > 1)) ? (from d in text2.Characters().Select(DayOfCharacter)
			where d.HasValue
			select d.Value).ToList() : (from d in list.Select(DayOfWord)
			where d.HasValue
			select d.Value).ToList());
		return list2.Skip(Math.Max(0, list2.Count - 42)).ToList();
	}

	private static ClickyInkStreakDay? DayOfCharacter(string character)
	{
		if (character != null)
		{
			int length = character.Length;
			if (length == 1)
			{
				switch (character[0])
				{
				case '#':
				case '*':
				case '+':
				case '1':
				case 'V':
				case 'X':
				case 'Y':
				case 'v':
				case 'x':
				case 'y':
				case '█':
				case '■':
				case '●':
				case '☑':
				case '✓':
				case '✔':
					return ClickyInkStreakDay.Done;
				case '.':
				case '0':
				case 'N':
				case 'O':
				case 'n':
				case 'o':
				case '×':
				case '□':
				case '○':
				case '☐':
				case '✗':
					return ClickyInkStreakDay.Missed;
				case '-':
				case '/':
				case 'R':
				case 'r':
				case '~':
				case '–':
					return ClickyInkStreakDay.Rest;
				case '?':
				case '@':
				case 'T':
				case 't':
				case '◐':
					return ClickyInkStreakDay.Today;
				case '_':
					return ClickyInkStreakDay.Upcoming;
				}
			}
		}
		return null;
	}

	private static ClickyInkStreakDay? DayOfWord(string word)
	{
		string text = word.Lowered();
		if (text != null)
		{
			switch (text.Length)
			{
			case 4:
			{
				char c = text[0];
				if ((uint)c <= 109u)
				{
					if (c == 'd')
					{
						if (!(text == "done"))
						{
							break;
						}
						goto IL_0259;
					}
					if (c != 'm' || !(text == "miss"))
					{
						break;
					}
				}
				else
				{
					if (c == 'r')
					{
						if (!(text == "rest"))
						{
							break;
						}
						goto IL_026b;
					}
					if (c != 's' || !(text == "skip"))
					{
						break;
					}
				}
				goto IL_0262;
			}
			case 3:
			{
				char c = text[0];
				if ((uint)c <= 104u)
				{
					if (c != 'd')
					{
						if (c != 'h' || !(text == "hit"))
						{
							break;
						}
					}
					else if (!(text == "did"))
					{
						break;
					}
				}
				else
				{
					if (c == 'n')
					{
						if (!(text == "now"))
						{
							break;
						}
						goto IL_0274;
					}
					if (c == 'o')
					{
						if (!(text == "off"))
						{
							break;
						}
						goto IL_026b;
					}
					if (c != 'y' || !(text == "yes"))
					{
						break;
					}
				}
				goto IL_0259;
			}
			case 1:
				switch (text[0])
				{
				case '1':
				case 'x':
				case 'y':
				case '✓':
					break;
				case '0':
				case 'n':
					goto IL_0262;
				case '-':
					goto IL_026b;
				case '?':
					goto IL_0274;
				case '_':
					goto IL_027d;
				default:
					goto end_IL_0017;
				}
				goto IL_0259;
			case 5:
			{
				char c = text[0];
				if (c != 'l')
				{
					if (c != 't' || !(text == "today"))
					{
						break;
					}
					goto IL_0274;
				}
				if (!(text == "later"))
				{
					break;
				}
				goto IL_027d;
			}
			case 6:
				if (!(text == "missed"))
				{
					break;
				}
				goto IL_0262;
			case 2:
				if (!(text == "no"))
				{
					break;
				}
				goto IL_0262;
			case 7:
				if (!(text == "skipped"))
				{
					break;
				}
				goto IL_0262;
			case 8:
				{
					if (!(text == "upcoming"))
					{
						break;
					}
					goto IL_027d;
				}
				IL_0262:
				return ClickyInkStreakDay.Missed;
				IL_0259:
				return ClickyInkStreakDay.Done;
				IL_027d:
				return ClickyInkStreakDay.Upcoming;
				IL_0274:
				return ClickyInkStreakDay.Today;
				IL_026b:
				return ClickyInkStreakDay.Rest;
				end_IL_0017:
				break;
			}
		}
		return (word.CharacterCount() == 1) ? DayOfCharacter(word) : ((ClickyInkStreakDay?)null);
	}

	public static int CurrentStreak(IReadOnlyList<ClickyInkStreakDay> days)
	{
		int num = 0;
		for (int num2 = days.Count - 1; num2 >= 0; num2--)
		{
			switch (days[num2])
			{
			case ClickyInkStreakDay.Done:
				num++;
				break;
			case ClickyInkStreakDay.Missed:
				return num;
			}
		}
		return num;
	}
}

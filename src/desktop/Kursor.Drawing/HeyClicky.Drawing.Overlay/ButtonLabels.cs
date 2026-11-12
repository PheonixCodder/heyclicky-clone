using System.Globalization;

namespace HeyClicky.Drawing.Overlay;

internal static class ButtonLabels
{
	internal static int LimitFor(string kind)
	{
		if (!(kind == "ask"))
		{
			return 40;
		}
		return 60;
	}

	internal static string Label(string text, int limit)
	{
		StringInfo stringInfo = new StringInfo(text);
		if (stringInfo.LengthInTextElements <= limit)
		{
			return text;
		}
		string text2 = stringInfo.SubstringByTextElements(0, limit - 1);
		int num = text2.LastIndexOf(' ');
		string text3 = ((num >= 0) ? text2.Substring(0, num) : text2);
		int i = 0;
		int num2;
		for (num2 = text3.Length; i < num2 && IsTrimmed(text3[i]); i++)
		{
		}
		while (num2 > i && IsTrimmed(text3[num2 - 1]))
		{
			num2--;
		}
		int num3 = i;
		return text3.Substring(num3, num2 - num3) + "…";
	}

	private static bool IsTrimmed(char c)
	{
		if (!char.IsWhiteSpace(c))
		{
			return char.IsPunctuation(c);
		}
		return true;
	}
}

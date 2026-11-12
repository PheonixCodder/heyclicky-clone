using System;
using System.Globalization;

namespace HeyClicky.Core.Brain;

public static class DrawingDisplayChoices
{
	private const string Marker = "onScreen(";

	public static int Decide(string codeSoFar, bool isComplete, int displayCount)
	{
		string text = DrawingContinuationDecisions.FirstStatementText(codeSoFar);
		if (text.StartsWith("onScreen(", StringComparison.Ordinal))
		{
			string text2 = text;
			int length = "onScreen(".Length;
			string text3 = text2.Substring(length, text2.Length - length);
			int num = text3.IndexOf(')');
			if (num < 0)
			{
				if (!isComplete)
				{
					return -1;
				}
				return 0;
			}
			int result;
			int num2 = ((!int.TryParse(text3.Substring(0, num), NumberStyles.Integer, CultureInfo.InvariantCulture, out result)) ? 1 : result);
			if (num2 < 1 || num2 > displayCount)
			{
				return 0;
			}
			return num2 - 1;
		}
		if (!isComplete && "onScreen(".StartsWith(text, StringComparison.Ordinal))
		{
			return -1;
		}
		return 0;
	}
}

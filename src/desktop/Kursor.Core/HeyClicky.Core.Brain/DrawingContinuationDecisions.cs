using System;

namespace HeyClicky.Core.Brain;

public static class DrawingContinuationDecisions
{
	private const string StartOverMarker = "startOver(";

	public static DrawingContinuationDecision Decide(string codeSoFar, bool isComplete)
	{
		string text = FirstStatementText(codeSoFar);
		if (text.StartsWith("startOver(", StringComparison.Ordinal))
		{
			return DrawingContinuationDecision.StartOver;
		}
		if (!isComplete && "startOver(".StartsWith(text, StringComparison.Ordinal))
		{
			return DrawingContinuationDecision.Undecided;
		}
		return DrawingContinuationDecision.BuildOn;
	}

	internal static string FirstStatementText(string code)
	{
		ReadOnlySpan<char> span = code.AsSpan();
		while (true)
		{
			int i;
			for (i = 0; i < span.Length && (char.IsWhiteSpace(span[i]) || span[i] == ';'); i++)
			{
			}
			ref ReadOnlySpan<char> reference = ref span;
			int num = i;
			span = reference.Slice(num, reference.Length - num);
			if (span.StartsWith("//"))
			{
				int num2 = span.IndexOf('\n');
				if (num2 < 0)
				{
					return "";
				}
				reference = ref span;
				num = num2;
				span = reference.Slice(num, reference.Length - num);
				continue;
			}
			if (!span.StartsWith("/*"))
			{
				break;
			}
			int num3 = span.IndexOf("*/");
			if (num3 < 0)
			{
				return "";
			}
			reference = ref span;
			num = num3 + 2;
			span = reference.Slice(num, reference.Length - num);
		}
		return span.Slice(0, Math.Min(24, span.Length)).ToString().Replace(" ", "");
	}
}

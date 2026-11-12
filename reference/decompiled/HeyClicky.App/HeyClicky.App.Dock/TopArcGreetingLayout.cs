using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.App.Dock;

public static class TopArcGreetingLayout
{
	public const double BodyHeight = 64.0;

	public const double EmptyDockExtraGap = 20.0;

	public static (double W, double H) PanelSize(double width, double headerRowHeight, (double W, double H)? frontCardSize)
	{
		double num = headerRowHeight + 64.0 + 44.0;
		if (frontCardSize.HasValue)
		{
			(double, double) valueOrDefault = frontCardSize.GetValueOrDefault();
			width = Math.Max(width, valueOrDefault.Item1 + 56.0);
			num += 20.0 + valueOrDefault.Item2 + 22.0;
		}
		else
		{
			num += 20.0;
		}
		return (W: width, H: num);
	}

	public static string Greeting(int hour, string? firstName)
	{
		string text = ((hour < 12) ? "Morning" : ((hour < 17) ? "Afternoon" : "Evening"));
		if (firstName == null || firstName.Length <= 0)
		{
			return text + ".";
		}
		return text + ", " + firstName + ".";
	}

	public static string? FirstName(string? displayName, string? email)
	{
		string text = displayName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		if (text != null && text.Length > 0)
		{
			return text;
		}
		string text2 = email?.Split('@').FirstOrDefault();
		if (text2 != null && text2.Length > 0)
		{
			string text3 = text2.Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/)).FirstOrDefault((string p) => p.Length > 0) ?? text2;
			if (text3.Length == 0)
			{
				text3 = text2;
			}
			ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text3[0]));
			string text4 = text3;
			return string.Concat(readOnlySpan, text4.Substring(1, text4.Length - 1));
		}
		return null;
	}

	public static string StatusLine(int workingCount, int needsYouCount, string voiceHoldDescription)
	{
		List<string> list = new List<string>();
		if (workingCount > 0)
		{
			list.Add((workingCount == 1) ? "1 job working" : $"{workingCount} jobs working");
		}
		if (needsYouCount > 0)
		{
			list.Add((needsYouCount == 1) ? "1 needs you" : $"{needsYouCount} need you");
		}
		if (list.Count > 0)
		{
			return string.Join(" · ", list);
		}
		return "Hold " + voiceHoldDescription + " and tell me what to take off your plate.";
	}
}

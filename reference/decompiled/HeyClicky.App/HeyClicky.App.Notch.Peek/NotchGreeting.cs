using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.App.Notch.Peek;

public static class NotchGreeting
{
	public static string? FirstName(UserProfile? user)
	{
		if ((object)user == null)
		{
			return null;
		}
		string text = user.DisplayName?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		if (text != null && text.Length > 0)
		{
			return text;
		}
		string text2 = user.Email.Split('@').FirstOrDefault();
		if (string.IsNullOrEmpty(text2))
		{
			return null;
		}
		string text3 = text2.Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/)).FirstOrDefault((string s) => s.Length > 0) ?? text2;
		ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(text3[0]));
		string text4 = text3;
		return string.Concat(readOnlySpan, text4.Substring(1, text4.Length - 1));
	}

	public static string Greeting(UserProfile? user)
	{
		int hour = DateTime.Now.Hour;
		string text = ((hour < 12) ? "Morning" : ((hour < 17) ? "Afternoon" : "Evening"));
		string text2 = FirstName(user);
		if (text2 == null)
		{
			return text + ".";
		}
		return text + ", " + text2 + ".";
	}
}

using System;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public static class ClickyFoundingMarker
{
	private const string HeaderPrefix = "[New Clicky \"";

	private const string HeaderSuffix = "\" founded]";

	public static string GreetingLine(string clickyName)
	{
		return "Hi there, I'm " + clickyName + ". I'm very happy you summoned me into your world <3";
	}

	public static string Compose(string clickyName, string firstMessage, string body)
	{
		string text = clickyName.Replace("\n", " ").Replace("\"", "'");
		string text2 = string.Join(" ", from l in firstMessage.Split('\n')
			select l.Trim() into l
			where l.Length > 0
			select l);
		global::_003C_003Ey__InlineArray4<string> buffer = default(global::_003C_003Ey__InlineArray4<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 0) = "[New Clicky \"" + text + "\" founded]";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 1) = text2;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 2) = "";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<string>, string>(ref buffer, 3) = body;
		return string.Join("\n", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray4<string>, string>(in buffer, 4));
	}

	public static (string Name, string FirstMessage)? Parse(string message)
	{
		string[] array = message.Trim().Split('\n');
		if (!array[0].StartsWith("[New Clicky \"", StringComparison.Ordinal) || !array[0].EndsWith("\" founded]", StringComparison.Ordinal))
		{
			return null;
		}
		string text = array[0];
		int length = "[New Clicky \"".Length;
		int length2 = "\" founded]".Length;
		return (text.Substring(length, text.Length - length2 - length), string.Join(" ", (from l in array.Skip(1)
			select l.Trim()).TakeWhile((string l) => l.Length > 0)));
	}

	public static string DisplayText(string message)
	{
		(string, string)? tuple = SuggestionApprovalMarker.Parse(message);
		if (tuple.HasValue)
		{
			(string, string) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item2.Length <= 0)
			{
				return valueOrDefault.Item1;
			}
			return valueOrDefault.Item2;
		}
		(string, string)? tuple2 = Parse(message);
		if (tuple2.HasValue)
		{
			(string, string) valueOrDefault2 = tuple2.GetValueOrDefault();
			if (valueOrDefault2.Item2.Length > 0)
			{
				return valueOrDefault2.Item2;
			}
		}
		return message;
	}
}

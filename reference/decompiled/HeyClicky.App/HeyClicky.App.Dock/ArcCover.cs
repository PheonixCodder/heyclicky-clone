using System.Globalization;
using System.Linq;

namespace HeyClicky.App.Dock;

public static class ArcCover
{
	public const string FinalizingTitle = "Finalizing";

	public const string WrappingUpLine = "Finalizing…";

	public static readonly (double State, double Title)[] Levels = new(double, double)[8]
	{
		(50.0, 38.0),
		(48.0, 35.0),
		(46.0, 32.0),
		(44.0, 29.0),
		(40.0, 27.0),
		(38.0, 25.0),
		(36.0, 23.0),
		(34.0, 21.0)
	};

	public static readonly (double State, double Title)[] SuggestedLevels = new(double, double)[6]
	{
		(30.0, 30.0),
		(28.0, 28.0),
		(26.0, 26.0),
		(24.0, 24.0),
		(22.0, 22.0),
		(20.0, 20.0)
	};

	public static readonly (double Title, double Body, double Spacing)[] Suggested = new(double, double, double)[7]
	{
		(30.0, 20.0, 14.0),
		(28.0, 19.0, 12.0),
		(26.0, 18.0, 11.0),
		(24.0, 17.0, 10.0),
		(22.0, 16.0, 9.0),
		(20.0, 15.0, 8.0),
		(19.0, 14.0, 7.0)
	};

	public const int CardPitchMaximumLines = 5;

	public static string Title(ArcCardState state)
	{
		return state switch
		{
			ArcCardState.New => "Ready to view", 
			ArcCardState.Failed => "Didn't finish", 
			ArcCardState.Idle => "No work yet", 
			_ => ArcCardStates.Title(state), 
		};
	}

	public static string? Symbol(ArcCardState state)
	{
		switch (state)
		{
		case ArcCardState.NeedsYou:
			return "hand.raised.fill";
		case ArcCardState.Failed:
			return "exclamationmark.triangle.fill";
		case ArcCardState.Stopped:
			return "stop.circle.fill";
		case ArcCardState.New:
		case ArcCardState.Suggested:
			return "sparkles";
		case ArcCardState.Done:
			return "checkmark.circle.fill";
		case ArcCardState.Pinning:
			return "pin.fill";
		default:
			return null;
		}
	}

	public static bool IsQuiet(ArcCardState state)
	{
		if ((uint)(state - 6) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static (double State, double Title)[] LevelsFor(ArcCardState state)
	{
		if (state != ArcCardState.Suggested)
		{
			return Levels;
		}
		return SuggestedLevels;
	}

	public static string CardPitch(string pitch, int maximumLength = 200)
	{
		string text = pitch.Trim();
		if (CharacterCount(text) <= maximumLength)
		{
			return text;
		}
		string text2 = "";
		foreach (string item in from s in SentenceBreaks.Sentences(text)
			select s.Trim())
		{
			if (item.Length != 0)
			{
				string text3 = ((text2.Length == 0) ? item : (text2 + " " + item));
				if (text2.Length > 0 && CharacterCount(text3) > maximumLength)
				{
					break;
				}
				text2 = text3;
			}
		}
		if (text2.Length != 0)
		{
			return text2;
		}
		return text;
	}

	private static int CharacterCount(string text)
	{
		return new StringInfo(text).LengthInTextElements;
	}
}

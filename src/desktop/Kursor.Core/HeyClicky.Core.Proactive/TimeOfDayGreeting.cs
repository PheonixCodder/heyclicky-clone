using System;

namespace HeyClicky.Core.Proactive;

public static class TimeOfDayGreeting
{
	public static string For(DateTime local)
	{
		switch (local.Hour)
		{
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
		case 11:
			return "Good morning";
		case 12:
		case 13:
		case 14:
		case 15:
		case 16:
			return "Good afternoon";
		case 17:
		case 18:
		case 19:
		case 20:
		case 21:
			return "Good evening";
		default:
			return "Hey";
		}
	}
}

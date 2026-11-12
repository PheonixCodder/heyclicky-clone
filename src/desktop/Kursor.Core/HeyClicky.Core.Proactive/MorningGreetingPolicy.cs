using System;

namespace HeyClicky.Core.Proactive;

public static class MorningGreetingPolicy
{
	public const int WindowStartHour = 7;

	public const int WindowEndHour = 12;

	public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15L);

	public static readonly TimeSpan ArmDelay = TimeSpan.FromSeconds(10L);

	public static readonly TimeSpan ActiveRecency = TimeSpan.FromSeconds(30L);

	public static readonly TimeSpan PostStreamHold = TimeSpan.FromSeconds(5L);

	public static readonly string[] Kaomoji = new string[17]
	{
		"＼(^_^ )", "( ^_^)／", "(^o^)", "(^\u203f^)", "(・∀・)", "(\uffe3▽\uffe3)", "( \u02d8▽\u02d8)", "(=^･^=)", "(^_^)v", "＼(^o^)／",
		"( \u00b4▽`)", "(⌒\u203f⌒)", "(\uff3e▽\uff3e)", "(・ω・)ノ", "(^-^)ノ", "( \u02d9▿\u02d9 )", "☀(^_^)☀"
	};

	public static readonly string[] Messages = new string[18]
	{
		"good morning beautiful. today's gonna be a good one.", "morning! drink a glass of water before anything else.", "good morning. hope you slept like a rock.", "yo good morning. glad you're here.", "morning! no rush today, we ease into it.", "morning. one thing at a time today. we got this.", "good morning. i'm around if you need me.", "morning! go grab some coffee, i'll be here.", "morning. fresh day, clean slate.", "morning! breakfast first, greatness second.",
		"yo, you made it. the day officially started.", "morning! the sun's up and so are we.", "hey, there you are. missed you.", "good morning! today's to-do list doesn't stand a chance.", "yo, morning. happy to see you.", "morning! whatever today brings, we got it.", "good morning. it's a nice day to make something cool.", "morning! small wins today. they add up."
	};

	public static bool IsWithinWindow(DateTime local)
	{
		if (local.Hour >= 7)
		{
			return local.Hour < 12;
		}
		return false;
	}

	public static string DayKey(DateTime local)
	{
		return $"{local.Year}-{local.Month}-{local.Day}";
	}

	public static string Pick(Random random)
	{
		return Kaomoji[random.Next(Kaomoji.Length)] + " " + Messages[random.Next(Messages.Length)];
	}

	public static TimeSpan UntilNextCheck(DateTime nowLocal, bool greetedToday)
	{
		if (!greetedToday && IsWithinWindow(nowLocal))
		{
			return PollInterval;
		}
		DateTime dateTime = nowLocal.Date.AddHours(7.0);
		TimeSpan timeSpan = ((nowLocal < dateTime) ? dateTime : dateTime.AddDays(1.0)) - nowLocal;
		if (!(timeSpan < PollInterval))
		{
			return timeSpan;
		}
		return PollInterval;
	}
}

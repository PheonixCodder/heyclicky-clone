using System;

namespace HeyClicky.App.Services.Updates;

public static class IdleInstallPolicy
{
	public static readonly TimeSpan IdleFor = TimeSpan.FromMinutes(5L);

	public static string? BusyReason(IdleInstallContext now)
	{
		if (now.AgentWorking)
		{
			return "an agent is working";
		}
		if (now.VoiceActive)
		{
			return "a voice turn is active";
		}
		if (now.Driving)
		{
			return "driving the screen";
		}
		if (now.NotchAnswering)
		{
			return "the notch is answering";
		}
		if (now.NotchOpen)
		{
			return "the notch is open";
		}
		if (now.ShowingOnScreen)
		{
			return "something is on screen";
		}
		if (now.Recording)
		{
			return "recording";
		}
		if (now.Onboarding)
		{
			return "onboarding";
		}
		return null;
	}

	public static string? WhyWait(IdleInstallContext now, TimeSpan heyClickyIdle, TimeSpan idleFor)
	{
		string text = BusyReason(now);
		if (text != null)
		{
			return text;
		}
		if (heyClickyIdle < idleFor)
		{
			return "HeyClicky was busy recently";
		}
		if (now.FullScreen)
		{
			return "a fullscreen app";
		}
		if (now.Presentation)
		{
			return "a presentation";
		}
		if (now.OnCall)
		{
			return "on a call";
		}
		return null;
	}
}

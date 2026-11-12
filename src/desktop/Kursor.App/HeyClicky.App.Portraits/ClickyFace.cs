namespace HeyClicky.App.Portraits;

public static class ClickyFace
{
	public static PortraitView Create(string seed, double size, PortraitMood mood = PortraitMood.Happy, bool showsAvatarBackground = true, bool tracksPointer = true, bool frozen = false)
	{
		return new PortraitView(ClickyRoster.Shared.AppearanceFor(seed), size, size, mood.Expression(), showsAvatarBackground, null)
		{
			AnimatesWhileIdle = (size >= 64.0 && !frozen),
			TracksPointer = tracksPointer
		};
	}

	public static PortraitView NotchBadge(string seed, double size = 26.0)
	{
		return Create(seed, size, PortraitMood.Running, showsAvatarBackground: false, tracksPointer: false, frozen: true);
	}

	public static string SeedFor(AgentTaskSnapshot task)
	{
		return task.TaskId;
	}
}

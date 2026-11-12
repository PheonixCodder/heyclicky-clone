namespace HeyClicky.Core.Onboarding;

public sealed record SquadPack(string Id, string Title, bool IsCloud)
{
	public string VoiceIdentifier
	{
		get
		{
			if (!IsCloud)
			{
				return "marin";
			}
			return "cedar";
		}
	}
}

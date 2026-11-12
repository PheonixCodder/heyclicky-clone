namespace HeyClicky.Core.Proactive;

public static class ProactiveCheckReasonWire
{
	public static string Wire(this ProactiveCheckReason r)
	{
		return r switch
		{
			ProactiveCheckReason.Morning => "morning", 
			ProactiveCheckReason.Debug => "debug", 
			ProactiveCheckReason.HomeSpace => "agent_space", 
			ProactiveCheckReason.Onboarding => "onboarding", 
			_ => "conversation", 
		};
	}
}

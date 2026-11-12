namespace HeyClicky;

public abstract record NotchPresentation
{
	public sealed record Idle : NotchPresentation;

	public sealed record LiveEvent(NotchLiveEvent Event) : NotchPresentation();

	public sealed record OnboardingPinned(int Step) : NotchPresentation();

	public sealed record TextInput(string Draft, NotchAgentFollowUp? AgentFollowUp = null) : NotchPresentation();

	public sealed record TextResponse(string Question, string Answer, bool IsThinking) : NotchPresentation();
}

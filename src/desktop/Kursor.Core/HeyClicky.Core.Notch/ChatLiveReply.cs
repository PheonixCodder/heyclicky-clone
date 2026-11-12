namespace HeyClicky.Core.Notch;

public abstract record ChatLiveReply
{
	public sealed record Thinking(string? Status) : ChatLiveReply();

	public sealed record Streaming(ConversationMessage Message) : ChatLiveReply();
}

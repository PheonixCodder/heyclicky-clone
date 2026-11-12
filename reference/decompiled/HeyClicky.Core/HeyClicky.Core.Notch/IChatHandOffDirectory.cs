namespace HeyClicky.Core.Notch;

public interface IChatHandOffDirectory
{
	(string Id, string Title)? JobForThread(string threadId);

	string? JobTitle(string jobId);

	string? ClickySlugForThread(string threadId);

	string? ClickyName(string slug);
}

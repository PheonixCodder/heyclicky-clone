namespace HeyClicky.Core.Voice;

public sealed record PageReference(string Kind, string Reference, int ProcessId = 0, nint WindowHandle = 0)
{
	public bool IsWebPage => Kind == "web_page";

	public string AgentPromptContextLine
	{
		get
		{
			if (!IsWebPage)
			{
				return "Context: the user currently has this file open on screen: " + Reference + " — if the task relates to it, open and read the file directly instead of relying on the screenshot's visible portion.";
			}
			return "Context: the user currently has this page open in their browser: " + Reference + " — if the task relates to it, fetch and read that page directly instead of relying on the screenshot's visible portion.";
		}
	}
}

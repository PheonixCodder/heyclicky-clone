using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Ink;

public sealed class InkSentActionFile
{
	[JsonPropertyName("kind")]
	public string Kind { get; set; } = "ask";

	[JsonPropertyName("objectKey")]
	public string ObjectKey { get; set; } = "";

	[JsonPropertyName("marginWords")]
	public string MarginWords { get; set; } = "";

	[JsonPropertyName("sentAt")]
	public double SentAt { get; set; }

	[JsonPropertyName("wasSentWhileRunning")]
	public bool WasSentWhileRunning { get; set; }

	[JsonPropertyName("reviewedItems")]
	public List<string>? ReviewedItems { get; set; }

	public ClickyInkSentAction ToAction()
	{
		return new ClickyInkSentAction(Kind switch
		{
			"answer" => ClickyInkNoteAction.ActionKind.Answer, 
			"send" => ClickyInkNoteAction.ActionKind.Send, 
			"review" => ClickyInkNoteAction.ActionKind.Review, 
			_ => ClickyInkNoteAction.ActionKind.Ask, 
		}, ObjectKey, MarginWords, InkFiles.FromSecondsSince1970(SentAt), WasSentWhileRunning, ReviewedItems);
	}

	public static InkSentActionFile From(ClickyInkSentAction action)
	{
		return new InkSentActionFile
		{
			Kind = action.Kind.ToString().ToLowerInvariant(),
			ObjectKey = action.ObjectKey,
			MarginWords = action.MarginWords,
			SentAt = InkFiles.SecondsSince1970(action.SentAt),
			WasSentWhileRunning = action.WasSentWhileRunning,
			ReviewedItems = action.ReviewedItems?.ToList()
		};
	}
}

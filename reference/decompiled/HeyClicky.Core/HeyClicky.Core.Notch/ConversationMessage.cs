using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HeyClicky.Core.Notch;

public sealed record ConversationMessage
{
	public required string Id { get; init; }

	public long? ServerId { get; init; }

	public required string Role { get; init; }

	public required string Content { get; init; }

	public string Source { get; init; } = "text";

	public required DateTimeOffset CreatedAt { get; init; }

	public IReadOnlyList<string>? AttachmentPaths { get; init; }

	public IReadOnlyList<string>? BubbleSegments { get; init; }

	public string? ReplacedLocalId { get; init; }

	public string? SentToAgentName { get; init; }

	[JsonIgnore]
	public bool IsPending
	{
		get
		{
			if (!ServerId.HasValue)
			{
				return SentToAgentName == null;
			}
			return false;
		}
	}

	[JsonIgnore]
	public bool IsUser => Role == "user";

	[JsonIgnore]
	public string RowId => ReplacedLocalId ?? Id;

	[JsonIgnore]
	public IReadOnlyList<string> BubbleTexts
	{
		get
		{
			IReadOnlyList<string> bubbleSegments = BubbleSegments;
			if (bubbleSegments != null && bubbleSegments.Count > 1)
			{
				return bubbleSegments;
			}
			string bubbleText = BubbleText;
			if (bubbleText.Length != 0)
			{
				return new _003C_003Ez__ReadOnlySingleElementList<string>(bubbleText);
			}
			return Array.Empty<string>();
		}
	}

	[JsonIgnore]
	public bool BubbleIsOnlyAboutAttachments
	{
		get
		{
			IReadOnlyList<string> attachmentPaths = AttachmentPaths;
			if (attachmentPaths == null || attachmentPaths.Count <= 0)
			{
				return false;
			}
			string text = TypedWords.Trim();
			if (text.Length != 0 && !text.StartsWith("\ud83d\udcce ", StringComparison.Ordinal))
			{
				return text == "Please review and act on the attached files.";
			}
			return true;
		}
	}

	[JsonIgnore]
	public string TypedWords
	{
		get
		{
			if (!IsUser)
			{
				return Content;
			}
			return WithoutAttachedFilesNote(Content);
		}
	}

	[JsonIgnore]
	public string BubbleText => Split(TypedWords).Spoken;

	[JsonIgnore]
	public string? ActionCaption => Split(TypedWords).Caption;

	public const string AttachmentOnlyTurnText = "Please review and act on the attached files.";

	private static readonly string[] AttachedFilesNoteHeaders = new string[2] { "[They attached files.", "[They attached files with no message." };

	private static readonly Regex TagPattern = new Regex("\\[(?:POINT|TYPE|TARGET|HOVER|CLICK)[^\\]]*\\]", RegexOptions.Compiled);

	private static readonly HashSet<string> HiddenActionTools = new HashSet<string> { "show_result" };

	public static List<string> BubbleSegmentsFrom(string text)
	{
		return (from s in text.Split("\n\n")
			select s.Trim() into s
			where s.Length > 0
			select s).ToList();
	}

	private static string WithoutAttachedFilesNote(string text)
	{
		string[] attachedFilesNoteHeaders = AttachedFilesNoteHeaders;
		foreach (string text2 in attachedFilesNoteHeaders)
		{
			if (text.StartsWith(text2, StringComparison.Ordinal))
			{
				return "";
			}
			int num = text.LastIndexOf("\n\n" + text2, StringComparison.Ordinal);
			if (num >= 0)
			{
				return text.Substring(0, num);
			}
		}
		return text;
	}

	private static string? WithoutHiddenActions(string caption)
	{
		List<string> list = new List<string>();
		string[] array = caption.Split("; ");
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (!text.StartsWith("called ", StringComparison.Ordinal))
			{
				if (text.Length > 0)
				{
					list.Add(text);
				}
				continue;
			}
			string text2 = text;
			int length = "called ".Length;
			List<string> list2 = (from t in text2.Substring(length, text2.Length - length).Split(", ")
				select t.Trim() into t
				where t.Length > 0 && !HiddenActionTools.Contains(t)
				select t).ToList();
			if (list2.Count > 0)
			{
				list.Add("called " + string.Join(", ", list2));
			}
		}
		if (list.Count != 0)
		{
			return string.Join("; ", list);
		}
		return null;
	}

	private static (string Spoken, string? Caption) Split(string content)
	{
		List<string> list = content.Split('\n').ToList();
		string item = null;
		string text = list[list.Count - 1].Trim();
		if (text.StartsWith('(') && text.EndsWith(')') && (text.StartsWith("(Drew ", StringComparison.Ordinal) || text.StartsWith("(called ", StringComparison.Ordinal)))
		{
			string text2 = text;
			item = WithoutHiddenActions(text2.Substring(1, text2.Length - 1 - 1));
			list.RemoveAt(list.Count - 1);
		}
		return (Spoken: TagPattern.Replace(string.Join("\n", list), "").Trim(), Caption: item);
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ConversationMessage(ConversationMessage original)
	{
		Id = original.Id;
		ServerId = original.ServerId;
		Role = original.Role;
		Content = original.Content;
		Source = original.Source;
		CreatedAt = original.CreatedAt;
		AttachmentPaths = original.AttachmentPaths;
		BubbleSegments = original.BubbleSegments;
		ReplacedLocalId = original.ReplacedLocalId;
		SentToAgentName = original.SentToAgentName;
	}

	public ConversationMessage()
	{
	}
}

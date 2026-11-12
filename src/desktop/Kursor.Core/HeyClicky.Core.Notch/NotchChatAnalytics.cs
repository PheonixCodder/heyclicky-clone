using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Notch;

public static class NotchChatAnalytics
{
	public static string RawValue(this NotchChatSessionOpenVia via)
	{
		return via switch
		{
			NotchChatSessionOpenVia.DoubleControl => "double_control", 
			NotchChatSessionOpenVia.ComposerClick => "composer_click", 
			NotchChatSessionOpenVia.FileDrag => "file_drag", 
			NotchChatSessionOpenVia.FileDrop => "file_drop", 
			_ => "send", 
		};
	}

	public static string RawValue(this NotchChatSessionCloseReason reason)
	{
		return reason switch
		{
			NotchChatSessionCloseReason.Escape => "escape", 
			NotchChatSessionCloseReason.DoubleControl => "double_control", 
			NotchChatSessionCloseReason.ClickElsewhere => "click_elsewhere", 
			NotchChatSessionCloseReason.DragAbandoned => "drag_abandoned", 
			NotchChatSessionCloseReason.LeftChatTab => "left_chat_tab", 
			NotchChatSessionCloseReason.NotchFolded => "notch_folded", 
			NotchChatSessionCloseReason.DrawingCaption => "drawing_caption", 
			NotchChatSessionCloseReason.ClickElsewhereDuringAnswer => "click_elsewhere_during_answer", 
			_ => "unspecified", 
		};
	}

	public static string RawValue(this NotchChatFullReplyOpenSource source)
	{
		return source switch
		{
			NotchChatFullReplyOpenSource.ShowMore => "show_more", 
			NotchChatFullReplyOpenSource.ArcCard => "arc_card", 
			_ => "notch_card", 
		};
	}

	public static string RawValue(this NotchChatFullReplyCloseVia via)
	{
		return via switch
		{
			NotchChatFullReplyCloseVia.Escape => "escape", 
			NotchChatFullReplyCloseVia.CloseButton => "close_button", 
			NotchChatFullReplyCloseVia.ExpandedToWindow => "expanded_to_window", 
			_ => "follow_up_sent", 
		};
	}

	public static string RawValue(this ChatHandOffOutcome outcome)
	{
		return outcome switch
		{
			ChatHandOffOutcome.Finished => "finished", 
			ChatHandOffOutcome.Failed => "failed", 
			ChatHandOffOutcome.Stopped => "stopped", 
			_ => "needsYou", 
		};
	}

	public static double Tenths(double seconds)
	{
		return Math.Round(seconds * 10.0, MidpointRounding.AwayFromZero) / 10.0;
	}

	public static JsonObject SessionOpened(NotchChatSessionOpenVia via, bool notchWasOpen, NotchTab tabBefore)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "session_opened",
			["via"] = via.RawValue(),
			["notch_was_open"] = notchWasOpen,
			["tab_before"] = tabBefore.RawValue()
		};
	}

	public static JsonObject SessionClosed(NotchChatSessionCloseReason reason, NotchChatSessionOpenVia openedVia, TimeSpan duration, int sentMessageCount, int sentAttachmentCount, bool hadUnsentDraft, bool answerInProgress)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "session_closed",
			["reason"] = reason.RawValue(),
			["opened_via"] = openedVia.RawValue(),
			["duration_seconds"] = Tenths(duration.TotalSeconds),
			["sent_message_count"] = sentMessageCount,
			["sent_attachment_count"] = sentAttachmentCount,
			["had_unsent_draft"] = hadUnsentDraft,
			["answer_in_progress"] = answerInProgress
		};
	}

	public static JsonObject FullReplyOpened(ChatHandOff? entry, NotchChatFullReplyOpenSource from)
	{
		string text = ChatReplyPreview.FullReplyMarkdown(entry);
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "full_reply_opened",
			["from"] = from.RawValue(),
			["layout"] = (ChatReplyPreview.UsesWideLayout(text) ? "wide" : "narrow"),
			["outcome"] = entry?.Outcome?.RawValue() ?? "none",
			["is_job"] = entry?.JobId != null,
			["is_routine_run"] = (entry?.IsRoutineRun ?? false),
			["file_count"] = (entry?.ArtifactEntries?.Count).GetValueOrDefault(),
			["follow_up_count"] = (entry?.FollowUps?.Count).GetValueOrDefault(),
			["reply_character_count"] = ((text != null) ? new StringInfo(text).LengthInTextElements : 0)
		};
	}

	public static JsonObject FullReplyClosed(NotchChatFullReplyCloseVia via, TimeSpan? openFor)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "full_reply_closed",
			["via"] = via.RawValue()
		};
		if (openFor.HasValue)
		{
			jsonObject["duration_seconds"] = Tenths(openFor.GetValueOrDefault().TotalSeconds);
		}
		return jsonObject;
	}

	public static JsonObject EarlierMessagesLoaded(string trigger, string source, int pageCount)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "earlier_messages_loaded",
			["trigger"] = trigger,
			["source"] = source,
			["page_count"] = pageCount
		};
	}

	public static JsonObject ReadAloudToggled(bool isOn, bool hasDraft)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "read_aloud_toggled",
			["is_on"] = isOn,
			["has_draft"] = hasDraft
		};
	}

	public static JsonObject ChatSend(int attachmentCount, bool hasText, bool readAloudOn)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "chat_send",
			["attachment_count"] = attachmentCount,
			["has_attachments"] = attachmentCount > 0,
			["has_text"] = hasText,
			["read_aloud_on"] = readAloudOn
		};
	}

	public static JsonObject ChatArtifactOpened(string location, string artifact)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "chat_artifact_opened",
			["location"] = location,
			["artifact_kind"] = ClickyAnalytics.ProductArtifactKind(artifact)
		};
	}

	public static JsonObject ChatFollowUpSuggestion(ChatHandOff entry, string followUp, bool fromFullReply)
	{
		int num = -1;
		IReadOnlyList<string> followUps = entry.FollowUps;
		if (followUps != null)
		{
			for (int i = 0; i < followUps.Count; i++)
			{
				if (followUps[i] == followUp)
				{
					num = i;
					break;
				}
			}
		}
		return new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "chat_follow_up_suggestion",
			["is_job"] = entry.JobId != null,
			["follow_up_index"] = num,
			["from_full_reply"] = fromFullReply
		};
	}
}

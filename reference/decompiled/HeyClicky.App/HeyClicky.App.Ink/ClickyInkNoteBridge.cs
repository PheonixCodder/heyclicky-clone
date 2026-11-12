using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.App.Ink;

public static class ClickyInkNoteBridge
{
	private const string DashboardActionEvent = "clicky_dashboard_action";

	private const string JobActionEvent = "job_action";

	public static IClickyInkNoteHost? Host { get; set; }

	public static ClickyInkCardSubject SubjectForClicky(string slug)
	{
		ClickyInkClickyLive clickyInkClickyLive = Host?.ClickyLiveStatus(slug) ?? new ClickyInkClickyLive(ClickyInkClickyPhase.Idle, null);
		ClickyInkCardSubject.SubjectPhase phase = clickyInkClickyLive.Phase switch
		{
			ClickyInkClickyPhase.Working => ClickyInkCardSubject.SubjectPhase.Working, 
			ClickyInkClickyPhase.NeedsYou => ClickyInkCardSubject.SubjectPhase.NeedsYou, 
			ClickyInkClickyPhase.Failed => ClickyInkCardSubject.SubjectPhase.Failed, 
			_ => ClickyInkCardSubject.SubjectPhase.Done, 
		};
		bool isRunning = clickyInkClickyLive.Phase == ClickyInkClickyPhase.Working;
		IClickyInkNoteHost? host = Host;
		return new ClickyInkCardSubject(slug, ClickyInkCardSubject.SubjectKind.Kept, isRunning, phase, (host != null && host.IsClickyUnread(slug)) ? ClickyThingStatus.Ready : ClickyThingStatus.Done, ClickyNotePaperColors.Stock("clicky:" + slug));
	}

	public static ClickyInkCardSubject SubjectForJob(ClickyInkJob job)
	{
		ClickyInkCardSubject.SubjectPhase subjectPhase;
		switch (job.Phase)
		{
		case ClickyInkJobPhase.Queued:
		case ClickyInkJobPhase.Working:
			subjectPhase = ClickyInkCardSubject.SubjectPhase.Working;
			break;
		case ClickyInkJobPhase.NeedsYou:
			subjectPhase = ClickyInkCardSubject.SubjectPhase.NeedsYou;
			break;
		case ClickyInkJobPhase.Failed:
			subjectPhase = ClickyInkCardSubject.SubjectPhase.Failed;
			break;
		default:
			subjectPhase = ClickyInkCardSubject.SubjectPhase.Done;
			break;
		}
		ClickyInkCardSubject.SubjectPhase phase = subjectPhase;
		string id = job.Id;
		ClickyInkJobPhase phase2 = job.Phase;
		bool isRunning = (uint)phase2 <= 1u;
		return new ClickyInkCardSubject(id, ClickyInkCardSubject.SubjectKind.Job, isRunning, phase, (job.Phase == ClickyInkJobPhase.Done && !job.IsSeen) ? ClickyThingStatus.Ready : ClickyThingStatus.Done, ClickyNotePaperColors.Stock("job:" + job.Id));
	}

	public static ClickyHomeSpreadPaper Paper(ClickyInkCardSubject subject)
	{
		return ClickyHomeSpreadPaper.Of(subject.Stock);
	}

	public static string ResolvedTarget(string rawTarget, string noteID)
	{
		string text = rawTarget;
		if (!text.StartsWith('/') && !text.StartsWith('\\') && (text.Length < 2 || text[1] != ':') && !text.StartsWith('~') && !text.Contains("://", StringComparison.Ordinal))
		{
			text = Path.Combine(Path.GetDirectoryName(noteID) ?? "", text);
		}
		return ClickyInkPaths.ExpandingTilde(text);
	}

	public static ClickyInkActions Actions(string noteID, ClickyInkNoteTarget target, ClickyHomeInkNote ink, ClickyInkCardSubject subject)
	{
		ClickyInkLiveNote note = ink.Note;
		ClickyInkActions clickyInkActions = new ClickyInkActions
		{
			Send = delegate(string words)
			{
				string text = words.Trim();
				if (text.Length != 0)
				{
					PerformAction(noteID, target, ClickyInkNoteActions.ActionForSentWords(text, note));
				}
			},
			Open = delegate(string opened)
			{
				OpenTarget(opened, noteID, target);
			},
			ResolvePath = (string raw) => ResolvedTarget(raw, noteID),
			Toggle = delegate(string blockID, string itemID)
			{
				string text2 = ItemText(note, blockID, itemID);
				if (text2 != null)
				{
					ClickyInkNoteStore.Shared.ToggleCheckbox(noteID, text2);
					(ClickyInkDocument.Kind, bool)? tuple = ItemCheckState(note, blockID, itemID);
					if (tuple.HasValue)
					{
						(ClickyInkDocument.Kind, bool) valueOrDefault = tuple.GetValueOrDefault();
						TrackNoteObjectAction(valueOrDefault.Item2 ? "item_unticked" : "item_ticked", valueOrDefault.Item1.RawValue(), target, sendsToAgent: false);
					}
				}
			},
			Perform = delegate(ClickyInkNoteAction action)
			{
				PerformAction(noteID, target, action);
			},
			Track = delegate(string action, ClickyInkDocument.Kind kind, JsonObject properties)
			{
				TrackNoteObjectAction(action, kind.RawValue(), target, sendsToAgent: false, properties);
			}
		};
		DateTimeOffset? dateTimeOffset = ((target is ClickyInkNoteTarget.Clicky clicky) ? Host?.ClickyLiveStatus(clicky.Slug).FinishedAt : ((!(target is ClickyInkNoteTarget.Job job)) ? ((DateTimeOffset?)null) : Host?.Job(job.Id)?.FinishedAt));
		DateTimeOffset? finishedAt = dateTimeOffset;
		IReadOnlyList<ClickyInkSentAction> sent = ClickyInkActionsStore.Shared.Sent(noteID);
		double? lastVersionAt = note.LastVersionAt;
		DateTimeOffset? lastVersionAt2;
		if (lastVersionAt.HasValue)
		{
			double valueOrDefault2 = lastVersionAt.GetValueOrDefault();
			lastVersionAt2 = ink.StartedAt.AddSeconds(valueOrDefault2);
		}
		else
		{
			lastVersionAt2 = null;
		}
		clickyInkActions.Feedback = ClickyInkActionFeedback.Make(sent, lastVersionAt2, subject.IsRunning, finishedAt, note.EditStarts.Select((double s) => ink.StartedAt.AddSeconds(s)).ToList(), DateTimeOffset.Now);
		if (target is ClickyInkNoteTarget.Clicky clicky2)
		{
			IClickyInkNoteHost host = Host;
			if (host != null)
			{
				clickyInkActions.Routines = host.RoutineControls(clicky2.Slug, DateTimeOffset.Now);
				clickyInkActions.SetRoutinePaused = delegate(string routineID, bool isPaused)
				{
					Host?.SetRoutinePaused(routineID, isPaused, "note");
				};
				clickyInkActions.SetRoutineInterval = delegate(string routineID, int minutes)
				{
					Host?.SetRoutineInterval(routineID, minutes, "note");
				};
				clickyInkActions.RunRoutineNow = (string routineID) => Host?.RunRoutineNow(routineID) ?? false;
			}
		}
		return clickyInkActions;
	}

	public static string? ItemText(ClickyInkLiveNote note, string blockID, string itemID)
	{
		foreach (ClickyInkLiveNote.Page page in note.Pages)
		{
			ClickyInkLiveNote.Block block = page.Blocks.FirstOrDefault((ClickyInkLiveNote.Block b) => b.Id == blockID);
			if (block != null)
			{
				return block.Items.FirstOrDefault((ClickyInkLiveNote.Item i) => i.Id == itemID)?.Source.Text;
			}
		}
		return null;
	}

	public static (ClickyInkDocument.Kind Kind, bool WasChecked)? ItemCheckState(ClickyInkLiveNote note, string blockID, string itemID)
	{
		foreach (ClickyInkLiveNote.Page page in note.Pages)
		{
			ClickyInkLiveNote.Block block = page.Blocks.FirstOrDefault((ClickyInkLiveNote.Block b) => b.Id == blockID);
			if (block != null)
			{
				ClickyInkLiveNote.Item item = block.Items.FirstOrDefault((ClickyInkLiveNote.Item i) => i.Id == itemID);
				if (item != null)
				{
					return (block.Source.Kind, item.Source.IsChecked == true);
				}
			}
		}
		return null;
	}

	public static void TrackNoteObjectAction(string action, string objectKind, ClickyInkNoteTarget target, bool sendsToAgent, JsonObject? properties = null)
	{
		properties = ((properties == null) ? new JsonObject((JsonNodeOptions?)null) : ((JsonObject)properties.DeepClone()));
		properties["action"] = action;
		properties["object"] = objectKind;
		properties["sends_to_agent"] = sendsToAgent;
		if (!(target is ClickyInkNoteTarget.Clicky clicky))
		{
			if (target is ClickyInkNoteTarget.Job job)
			{
				properties["is_kept_note"] = false;
				ClickyAnalytics.TrackProductEvent("note_object_action", null, Host?.Job(job.Id)?.ThreadId, properties);
			}
		}
		else
		{
			properties["is_kept_note"] = true;
			ClickyAnalytics.TrackProductEvent("note_object_action", clicky.Slug, null, properties);
		}
	}

	public static void PerformAction(string noteID, ClickyInkNoteTarget target, ClickyInkNoteAction action)
	{
		IReadOnlyList<ClickyInkSentAction> readOnlyList = ClickyInkActionsStore.Shared.Sent(noteID);
		if (readOnlyList.Count > 0)
		{
			ClickyInkSentAction clickyInkSentAction = readOnlyList[readOnlyList.Count - 1];
			if ((object)clickyInkSentAction != null && clickyInkSentAction.ObjectKey == action.ObjectKey && clickyInkSentAction.MarginWords == action.MarginWords && (DateTimeOffset.Now - clickyInkSentAction.SentAt).TotalSeconds < 3.0)
			{
				return;
			}
		}
		ClickyInkLiveNote clickyInkLiveNote = ClickyInkNoteStore.Shared.LiveNote(noteID);
		List<DateTimeOffset> list;
		if ((object)clickyInkLiveNote != null)
		{
			DateTimeOffset? dateTimeOffset = ClickyInkNoteStore.Shared.StartedAt(noteID);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset startedAt = dateTimeOffset.GetValueOrDefault();
				list = clickyInkLiveNote.EditStarts.Select((double s) => startedAt.AddSeconds(s)).ToList();
				goto IL_00e1;
			}
		}
		list = new List<DateTimeOffset>();
		goto IL_00e1;
		IL_00e1:
		IReadOnlyList<DateTimeOffset> editStartedAt = list;
		ClickyInkNoteAction.ActionKind kind = action.Kind;
		bool flag = (uint)(kind - 1) <= 1u;
		if (flag && readOnlyList.Any((ClickyInkSentAction s) => s.ObjectKey == action.ObjectKey && s.Kind == action.Kind && ClickyInkActionFeedback.IsStillCurrent(s, editStartedAt)))
		{
			return;
		}
		bool flag2 = ClickyInkNoteStore.Shared.IsTurnWriting(noteID);
		ClickyInkNotePages.Shared.NoteInkActionSent(noteID);
		ClickyInkActionsStore.Shared.Record(new ClickyInkSentAction(action, DateTimeOffset.Now, flag2), noteID);
		Sounds.Play("clicky-text-send", 0.2);
		JsonObject properties = new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "note_" + action.Kind.ToString().ToLowerInvariant(),
			["was_running"] = flag2
		};
		if (!(target is ClickyInkNoteTarget.Clicky clicky))
		{
			if (target is ClickyInkNoteTarget.Job job)
			{
				ClickyInkJob clickyInkJob = Host?.Job(job.Id);
				if ((object)clickyInkJob == null)
				{
					return;
				}
				string threadId = clickyInkJob.ThreadId;
				if (threadId == null)
				{
					return;
				}
				Host.SendToThread(threadId, action.Message, action.LaunchContext);
				ClickyAnalytics.TrackProductEvent("job_action", null, threadId, properties);
			}
		}
		else
		{
			IClickyInkNoteHost host = Host;
			if (host == null || !host.ClickyExists(clicky.Slug))
			{
				return;
			}
			host.SendToClicky(clicky.Slug, action.Message, action.LaunchContext);
			ClickyAnalytics.TrackProductEvent("clicky_dashboard_action", clicky.Slug, null, properties);
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["was_running"] = flag2 };
		string analyticsMethod = action.AnalyticsMethod;
		if (analyticsMethod != null)
		{
			jsonObject["method"] = analyticsMethod;
		}
		foreach (KeyValuePair<string, int> analyticsCount in action.AnalyticsCounts)
		{
			analyticsCount.Deconstruct(out var key, out var value);
			string propertyName = key;
			int num = value;
			jsonObject[propertyName] = num;
		}
		TrackNoteObjectAction((action.AnalyticsAction.Length == 0) ? ("note_" + action.Kind.ToString().ToLowerInvariant()) : action.AnalyticsAction, action.AnalyticsObject, target, sendsToAgent: true, jsonObject);
		ClickyInkNoteStore.Shared.NoteActionSent();
	}

	public static void OpenTarget(string rawTarget, string noteID, ClickyInkNoteTarget owner)
	{
		string text = ResolvedTarget(rawTarget, noteID);
		bool flag = text.StartsWith("https://", StringComparison.Ordinal) || text.StartsWith("http://", StringComparison.Ordinal);
		TrackNoteObjectAction(flag ? "link_opened" : "file_opened", flag ? "link" : "file", owner, sendsToAgent: false, new JsonObject((JsonNodeOptions?)null) { ["artifact_kind"] = ClickyAnalytics.ProductArtifactKind(text) });
		if (!(owner is ClickyInkNoteTarget.Job job))
		{
			if (owner is ClickyInkNoteTarget.Clicky clicky)
			{
				string threadId = Host?.BoundThreadId(clicky.Slug);
				IClickyInkNoteHost? host = Host;
				if (host == null || !host.OpenInViewerIfSupported(text, clicky.Slug, threadId))
				{
					OpenInItsApp(text);
				}
			}
		}
		else
		{
			Host?.OpenJobArtifact(text, job.Id);
		}
	}

	public static void OpenInItsApp(string target)
	{
		try
		{
			string target2;
			if (target.Contains("://", StringComparison.Ordinal))
			{
				if (!Uri.TryCreate(target, UriKind.Absolute, out Uri result))
				{
					return;
				}
				target2 = result.AbsoluteUri;
			}
			else
			{
				target2 = ClickyInkPaths.ExpandingTilde(target);
			}
			ShellOpen.Start(target2);
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "open " + target + ": " + ex.Message);
		}
	}
}

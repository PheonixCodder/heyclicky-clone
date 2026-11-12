using System;
using System.IO;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.Core.Ink;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkLiveNoteTurns(AgentService agents, DispatcherQueue ui) : IClickyLiveNoteTurns
{
	public string? NotePath(string clickySlug, string workspacePath)
	{
		if (!ClickyLiveNotePreferences.IsEnabled)
		{
			return null;
		}
		string text = agents.FindClicky(clickySlug)?.ThreadId;
		if (text != null)
		{
			ClickyJob clickyJob = agents.Jobs.JobForThread(text);
			if ((object)clickyJob != null)
			{
				return ClickyLiveNotePreferences.NotePath(clickyJob.FolderPath);
			}
		}
		if (workspacePath.Length != 0)
		{
			return ClickyLiveNotePreferences.NotePath(workspacePath);
		}
		return null;
	}

	public string PromptSection(string notePath, string? userMessage, string? routineRun, ClickyLiveNoteWriter writer)
	{
		return OnUi(delegate
		{
			bool num = ((userMessage != null) ? ClickyInkNotePages.Shared.UserMessageSent(userMessage, notePath) : (routineRun != null && ClickyInkNotePages.Shared.RoutineRunStarted(routineRun, notePath)));
			ClickyInkNoteStore.Shared.NoteTurnStarted(notePath, isKept: true);
			string folderPath = Path.GetDirectoryName(notePath) ?? "";
			bool turnedToFreshPage = num;
			bool freshPageIsRoutineRun = num && userMessage == null;
			return ClickyInkBrief.AgentSection(folderPath, isKept: true, turnedToFreshPage, freshPageIsRoutineRun, writer switch
			{
				ClickyLiveNoteWriter.NoteTools => ClickyInkBrief.NoteWriter.NoteTools, 
				ClickyLiveNoteWriter.FileTools => ClickyInkBrief.NoteWriter.FileTools, 
				_ => ClickyInkBrief.NoteWriter.ApplyPatch, 
			});
		}, "");
	}

	public void FileActivity(string notePath, string path, string? streamedKind, string? streamedDiff)
	{
		if (!string.Equals(Path.GetFileName(path), "note.md", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(Path.GetDirectoryName(notePath) ?? "", path));
		}
		catch
		{
			return;
		}
		if (!string.Equals(fullPath, notePath, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		ui.TryEnqueue(delegate
		{
			if (streamedKind != null && streamedDiff != null)
			{
				ClickyLiveNoteStore.Shared.StreamPatch(notePath, streamedKind, streamedDiff);
			}
			else
			{
				ClickyLiveNoteStore.Shared.ReadNow(notePath);
			}
		});
	}

	public void TurnEnded(string notePath)
	{
		ui.TryEnqueue(delegate
		{
			ClickyLiveNoteStore.Shared.ReadNow(notePath);
			ClickyInkNoteStore.Shared.NoteTurnEnded(notePath);
		});
	}

	public void Forget(string notePath)
	{
		ui.TryEnqueue(delegate
		{
			ClickyInkNoteStore.Shared.Forget(notePath);
		});
	}

	public static ClickyInkJob InkJob(AgentService agents, ClickyJob job)
	{
		string id = job.Id;
		return new ClickyInkJob(id, agents.RailPhase(job) switch
		{
			ClickyJobRailPhase.Queued => ClickyInkJobPhase.Queued, 
			ClickyJobRailPhase.Working => ClickyInkJobPhase.Working, 
			ClickyJobRailPhase.NeedsYou => ClickyInkJobPhase.NeedsYou, 
			ClickyJobRailPhase.Failed => ClickyInkJobPhase.Failed, 
			ClickyJobRailPhase.Done => ClickyInkJobPhase.Done, 
			_ => ClickyInkJobPhase.Stopped, 
		}, job.IsSeen, job.FinishedAt, job.ThreadId);
	}

	private T OnUi<T>(Func<T> work, T fallback)
	{
		if (ui.HasThreadAccess)
		{
			return work();
		}
		TaskCompletionSource<T> done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!ui.TryEnqueue(delegate
		{
			try
			{
				done.SetResult(work());
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "live note: " + ex.Message);
				done.SetResult(fallback);
			}
		}))
		{
			return fallback;
		}
		if (!done.Task.Wait(TimeSpan.FromSeconds(5L)))
		{
			return fallback;
		}
		return done.Task.Result;
	}
}

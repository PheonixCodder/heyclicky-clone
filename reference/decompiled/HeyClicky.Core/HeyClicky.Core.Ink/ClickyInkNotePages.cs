using System;
using System.Collections.Generic;
using System.IO;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkNotePages
{
	private const string RetiredFolderName = "note-pages";

	private const double InkActionGraceSeconds = 10.0;

	private readonly Dictionary<string, ClickyInkNotePage> currentPages = new Dictionary<string, ClickyInkNotePage>();

	private readonly Dictionary<string, DateTimeOffset> lastInkActionAt = new Dictionary<string, DateTimeOffset>();

	public static ClickyInkNotePages Shared { get; } = new ClickyInkNotePages();

	public IReadOnlyDictionary<string, ClickyInkNotePage> CurrentPages => currentPages;

	public event Action? Changed;

	private ClickyInkNotePages()
	{
	}

	public ClickyInkNotePage? CurrentPage(string notePath)
	{
		if (!currentPages.TryGetValue(notePath, out ClickyInkNotePage value))
		{
			return null;
		}
		return value;
	}

	public void NoteInkActionSent(string notePath)
	{
		lastInkActionAt[notePath] = DateTimeOffset.Now;
	}

	public bool UserMessageSent(string message, string notePath)
	{
		if (lastInkActionAt.TryGetValue(notePath, out var value) && (DateTimeOffset.Now - value).TotalSeconds < 10.0)
		{
			lastInkActionAt.Remove(notePath);
			return false;
		}
		return TurnPage(message, isRoutineRun: false, notePath);
	}

	public bool RoutineRunStarted(string runLine, string notePath)
	{
		return TurnPage(runLine, isRoutineRun: true, notePath);
	}

	private bool TurnPage(string message, bool isRoutineRun, string notePath)
	{
		string text = message.TrimWsNl();
		if (text.Length == 0)
		{
			return false;
		}
		bool flag = (InkFiles.ReadUtf8(notePath) ?? "").TrimWsNl().Length > 0;
		int number = (CurrentPage(notePath)?.Number ?? 0) + 1;
		currentPages[notePath] = new ClickyInkNotePage(number, text, isRoutineRun);
		this.Changed?.Invoke();
		string path = Path.Combine(Path.GetDirectoryName(notePath) ?? "", "note-pages");
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch (Exception)
		{
		}
		if (!flag)
		{
			return false;
		}
		try
		{
			File.Delete(notePath);
		}
		catch (Exception ex2)
		{
			Log.Warn("ink pages", "couldn't clear " + notePath + ": " + ex2.Message);
			return false;
		}
		ClickyInkNoteStore.Shared.StartFreshPage(notePath);
		ClickyLiveNoteStore.Shared.ReadNow(notePath);
		return true;
	}

	public void RemoveAllForSignOut()
	{
		currentPages.Clear();
		lastInkActionAt.Clear();
		this.Changed?.Invoke();
	}
}

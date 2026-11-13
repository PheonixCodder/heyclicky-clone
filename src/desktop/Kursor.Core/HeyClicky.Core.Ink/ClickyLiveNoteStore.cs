using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed class ClickyLiveNoteStore
{
	public sealed record Note(string Text, bool IsBeingWritten, DateTimeOffset? SavedAt);

	private readonly object syncRoot = new object();

	private readonly Dictionary<string, Note> notes = new Dictionary<string, Note>();

	private readonly Dictionary<string, string> savedTexts = new Dictionary<string, string>();

	private readonly Dictionary<string, (DateTimeOffset ModifiedAt, long Size)> savedStats = new Dictionary<string, (DateTimeOffset, long)>();

	private readonly Dictionary<string, int> shownCounts = new Dictionary<string, int>();

	private InkDispatch.RepeatingTimer? pollTimer;

	private readonly Dictionary<string, string> pendingStreamedTexts = new Dictionary<string, string>();

	private bool streamPublishScheduled;

	private const double PollInterval = 0.5;

	private const double StreamPublishInterval = 0.12;

	public static ClickyLiveNoteStore Shared { get; } = new ClickyLiveNoteStore();

	public IReadOnlyDictionary<string, Note> Notes
	{
		get
		{
			lock (syncRoot)
			{
				return new Dictionary<string, Note>(notes);
			}
		}
	}

	public event Action? Changed;

	private ClickyLiveNoteStore()
	{
	}

	private void SetNote(string path, Note note)
	{
		bool changed = false;
		lock (syncRoot)
		{
			if (!notes.TryGetValue(path, out Note? value) || !(value == note))
			{
				notes[path] = note;
				changed = true;
			}
		}
		if (changed)
		{
			this.Changed?.Invoke();
		}
	}

	public void BeginShowing(string path)
	{
		lock (syncRoot)
		{
			shownCounts[path] = (shownCounts.TryGetValue(path, out var value) ? value : 0) + 1;
		}
		ReadNow(path);
		StartPollingIfNeeded();
	}

	public void EndShowing(string path)
	{
		lock (syncRoot)
		{
			if (shownCounts.TryGetValue(path, out var value))
			{
				if (value <= 1)
				{
					shownCounts.Remove(path);
				}
				else
				{
					shownCounts[path] = value - 1;
				}
				if (shownCounts.Count == 0)
				{
					pollTimer?.Dispose();
					pollTimer = null;
				}
			}
		}
	}

	public void ReadNow(string path)
	{
		(DateTimeOffset, long)? tuple = InkFiles.Stat(path);
		string? text = ((!tuple.HasValue) ? null : InkFiles.ReadUtf8(path));
		if (tuple.HasValue)
		{
			(DateTimeOffset, long) valueOrDefault = tuple.GetValueOrDefault();
			if (text != null)
			{
				if (valueOrDefault.Item2 <= 524288)
				{
					lock (syncRoot)
					{
						savedStats[path] = valueOrDefault;
						savedTexts[path] = text;
						pendingStreamedTexts.Remove(path);
					}
					SetNote(path, new Note(text, IsBeingWritten: false, valueOrDefault.Item1));
					ClickyInkNoteStore.Shared.RecordSaved(path, text, valueOrDefault.Item1);
				}
				return;
			}
		}
		bool removed = false;
		lock (syncRoot)
		{
			if (notes.ContainsKey(path) && !File.Exists(path))
			{
				notes.Remove(path);
				savedTexts.Remove(path);
				savedStats.Remove(path);
				removed = true;
			}
		}
		if (removed)
		{
			this.Changed?.Invoke();
		}
	}

	public void StreamPatch(string path, string kind, string diff)
	{
		bool hasSaved;
		lock (syncRoot)
		{
			hasSaved = savedTexts.ContainsKey(path);
		}
		if (!hasSaved)
		{
			ReadNow(path);
		}
		ClickyInkNoteStore.Shared.StreamPatch(path, kind, diff);
		string savedText;
		lock (syncRoot)
		{
			savedText = (savedTexts.TryGetValue(path, out string? value) ? value : "");
		}
		string? text = ClickyInkPatchStream.PartialText(savedText, kind, diff);
		if (text != null)
		{
			bool schedule = false;
			lock (syncRoot)
			{
				pendingStreamedTexts[path] = text;
				if (!streamPublishScheduled)
				{
					streamPublishScheduled = true;
					schedule = true;
				}
			}
			if (schedule)
			{
				InkDispatch.After(0.12, PublishStreamedTexts);
			}
		}
	}

	private void PublishStreamedTexts()
	{
		List<KeyValuePair<string, string>> snapshot;
		lock (syncRoot)
		{
			streamPublishScheduled = false;
			snapshot = pendingStreamedTexts.ToList();
			pendingStreamedTexts.Clear();
		}
		foreach (var (text3, text4) in snapshot)
		{
			DateTimeOffset? savedAt;
			lock (syncRoot)
			{
				savedAt = (notes.TryGetValue(text3, out Note? value) ? value.SavedAt : null);
			}
			SetNote(text3, new Note(text4, IsBeingWritten: true, savedAt));
		}
	}

	private void StartPollingIfNeeded()
	{
		lock (syncRoot)
		{
			if (pollTimer == null && shownCounts.Count != 0)
			{
				pollTimer = new InkDispatch.RepeatingTimer(0.5, PollShownNotes);
			}
		}
	}

	private void PollShownNotes()
	{
		List<string> shownKeys;
		lock (syncRoot)
		{
			shownKeys = shownCounts.Keys.ToList();
		}
		foreach (string item in shownKeys)
		{
			(DateTimeOffset, long)? tuple = InkFiles.Stat(item);
			(DateTimeOffset, long)? tuple2;
			lock (syncRoot)
			{
				tuple2 = (savedStats.TryGetValue(item, out (DateTimeOffset, long) value) ? new(DateTimeOffset, long)?(value) : null);
			}
			DateTimeOffset? dateTimeOffset = tuple?.Item1;
			DateTimeOffset? dateTimeOffset2 = tuple2?.Item1;
			if (dateTimeOffset.HasValue != dateTimeOffset2.HasValue || (dateTimeOffset.HasValue && dateTimeOffset.GetValueOrDefault() != dateTimeOffset2.GetValueOrDefault()) || tuple?.Item2 != tuple2?.Item2)
			{
				ReadNow(item);
			}
		}
	}

	public void RemoveAllForSignOut()
	{
		lock (syncRoot)
		{
			notes.Clear();
			savedTexts.Clear();
			savedStats.Clear();
			pendingStreamedTexts.Clear();
		}
		this.Changed?.Invoke();
		ClickyInkNoteStore.Shared.RemoveAllForSignOut();
		ClickyInkActionsStore.Shared.RemoveAllForSignOut();
		ClickyInkNotePages.Shared.RemoveAllForSignOut();
	}
}

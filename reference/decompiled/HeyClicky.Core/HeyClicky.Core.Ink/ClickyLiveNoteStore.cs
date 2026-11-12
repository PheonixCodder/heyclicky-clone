using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed class ClickyLiveNoteStore
{
	public sealed record Note(string Text, bool IsBeingWritten, DateTimeOffset? SavedAt);

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

	public IReadOnlyDictionary<string, Note> Notes => notes;

	public event Action? Changed;

	private ClickyLiveNoteStore()
	{
	}

	private void SetNote(string path, Note note)
	{
		if (!notes.TryGetValue(path, out Note value) || !(value == note))
		{
			notes[path] = note;
			this.Changed?.Invoke();
		}
	}

	public void BeginShowing(string path)
	{
		shownCounts[path] = (shownCounts.TryGetValue(path, out var value) ? value : 0) + 1;
		ReadNow(path);
		StartPollingIfNeeded();
	}

	public void EndShowing(string path)
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

	public void ReadNow(string path)
	{
		(DateTimeOffset, long)? tuple = InkFiles.Stat(path);
		string text = ((!tuple.HasValue) ? null : InkFiles.ReadUtf8(path));
		if (tuple.HasValue)
		{
			(DateTimeOffset, long) valueOrDefault = tuple.GetValueOrDefault();
			if (text != null)
			{
				if (valueOrDefault.Item2 <= 524288)
				{
					savedStats[path] = valueOrDefault;
					savedTexts[path] = text;
					pendingStreamedTexts.Remove(path);
					SetNote(path, new Note(text, IsBeingWritten: false, valueOrDefault.Item1));
					ClickyInkNoteStore.Shared.RecordSaved(path, text, valueOrDefault.Item1);
				}
				return;
			}
		}
		if (notes.ContainsKey(path) && !File.Exists(path))
		{
			notes.Remove(path);
			savedTexts.Remove(path);
			savedStats.Remove(path);
			this.Changed?.Invoke();
		}
	}

	public void StreamPatch(string path, string kind, string diff)
	{
		if (!savedTexts.ContainsKey(path))
		{
			ReadNow(path);
		}
		ClickyInkNoteStore.Shared.StreamPatch(path, kind, diff);
		string value;
		string text = ClickyInkPatchStream.PartialText(savedTexts.TryGetValue(path, out value) ? value : "", kind, diff);
		if (text != null)
		{
			pendingStreamedTexts[path] = text;
			if (!streamPublishScheduled)
			{
				streamPublishScheduled = true;
				InkDispatch.After(0.12, PublishStreamedTexts);
			}
		}
	}

	private void PublishStreamedTexts()
	{
		streamPublishScheduled = false;
		foreach (var (text3, text4) in pendingStreamedTexts.ToList())
		{
			SetNote(text3, new Note(text4, IsBeingWritten: true, notes.TryGetValue(text3, out Note value) ? value.SavedAt : ((DateTimeOffset?)null)));
		}
		pendingStreamedTexts.Clear();
	}

	private void StartPollingIfNeeded()
	{
		if (pollTimer == null && shownCounts.Count != 0)
		{
			pollTimer = new InkDispatch.RepeatingTimer(0.5, PollShownNotes);
		}
	}

	private void PollShownNotes()
	{
		foreach (string item in shownCounts.Keys.ToList())
		{
			(DateTimeOffset, long)? tuple = InkFiles.Stat(item);
			(DateTimeOffset, long) value;
			(DateTimeOffset, long)? tuple2 = (savedStats.TryGetValue(item, out value) ? new(DateTimeOffset, long)?(value) : (((DateTimeOffset, long)?)null));
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
		notes.Clear();
		savedTexts.Clear();
		savedStats.Clear();
		pendingStreamedTexts.Clear();
		this.Changed?.Invoke();
		ClickyInkNoteStore.Shared.RemoveAllForSignOut();
		ClickyInkActionsStore.Shared.RemoveAllForSignOut();
		ClickyInkNotePages.Shared.RemoveAllForSignOut();
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkActionsStore
{
	public const string FileName = "note-actions.json";

	public const int MaximumActionsPerThing = 24;

	public const int MaximumThings = 400;

	private readonly string? baseDirectoryOverride;

	private readonly Func<string?> currentScopeKey;

	private string? loadedScopeKey;

	private Dictionary<string, List<ClickyInkSentAction>> actionsByThingId = new Dictionary<string, List<ClickyInkSentAction>>();

	private readonly InkDispatch.SerialQueue writeQueue = new InkDispatch.SerialQueue();

	public static ClickyInkActionsStore Shared { get; } = new ClickyInkActionsStore(null, () => ClickyInkNoteStore.Shared.ScopeKey);

	private string BaseDirectory => baseDirectoryOverride ?? AppPaths.Combine("Ink");

	public ClickyInkActionsStore(string? baseDirectory, Func<string?> scopeKey)
	{
		baseDirectoryOverride = baseDirectory;
		currentScopeKey = scopeKey;
	}

	public IReadOnlyList<ClickyInkSentAction> Sent(string thingId)
	{
		if (!LoadScopeIfNeeded())
		{
			return Array.Empty<ClickyInkSentAction>();
		}
		if (!actionsByThingId.TryGetValue(thingId, out List<ClickyInkSentAction> value))
		{
			return new List<ClickyInkSentAction>();
		}
		return value;
	}

	public void Record(ClickyInkSentAction action, string thingId)
	{
		if (!LoadScopeIfNeeded())
		{
			return;
		}
		List<ClickyInkSentAction> value;
		List<ClickyInkSentAction> list = (actionsByThingId.TryGetValue(thingId, out value) ? value.ToList() : new List<ClickyInkSentAction>());
		list.Add(action);
		actionsByThingId[thingId] = list.Skip(Math.Max(0, list.Count - 24)).ToList();
		if (actionsByThingId.Count > 400)
		{
			foreach (string item in (from kv in actionsByThingId.OrderBy<KeyValuePair<string, List<ClickyInkSentAction>>, DateTimeOffset>(delegate(KeyValuePair<string, List<ClickyInkSentAction>> kv)
				{
					if (kv.Value.Count <= 0)
					{
						return DateTimeOffset.MinValue;
					}
					List<ClickyInkSentAction> value2 = kv.Value;
					return value2[value2.Count - 1].SentAt;
				}).Take(actionsByThingId.Count - 400)
				select kv.Key).ToList())
			{
				actionsByThingId.Remove(item);
			}
		}
		Save();
	}

	public void RemoveAllForSignOut()
	{
		loadedScopeKey = null;
		actionsByThingId = new Dictionary<string, List<ClickyInkSentAction>>();
	}

	public void WaitForPendingSaves()
	{
		writeQueue.Sync();
	}

	private string FilePath(string scopeKey)
	{
		return Path.Combine(BaseDirectory, scopeKey, "note-actions.json");
	}

	private bool LoadScopeIfNeeded()
	{
		string text = currentScopeKey();
		if (text == null)
		{
			loadedScopeKey = null;
			actionsByThingId = new Dictionary<string, List<ClickyInkSentAction>>();
			return false;
		}
		if (text == loadedScopeKey)
		{
			return true;
		}
		loadedScopeKey = text;
		actionsByThingId = new Dictionary<string, List<ClickyInkSentAction>>();
		try
		{
			string path = FilePath(text);
			if (File.Exists(path))
			{
				Dictionary<string, List<InkSentActionFile>> dictionary = JsonSerializer.Deserialize(File.ReadAllBytes(path), InkJsonContext.Default.InkActionsSnapshot)?.ActionsByThingId;
				if (dictionary != null)
				{
					actionsByThingId = dictionary.ToDictionary((KeyValuePair<string, List<InkSentActionFile>> kv) => kv.Key, (KeyValuePair<string, List<InkSentActionFile>> kv) => kv.Value.Select((InkSentActionFile a) => a.ToAction()).ToList());
				}
			}
		}
		catch (Exception)
		{
			actionsByThingId = new Dictionary<string, List<ClickyInkSentAction>>();
		}
		return true;
	}

	private void Save()
	{
		string text = loadedScopeKey;
		if (text == null)
		{
			return;
		}
		string path = FilePath(text);
		InkActionsSnapshot value = new InkActionsSnapshot
		{
			Version = 1,
			ActionsByThingId = actionsByThingId.ToDictionary<KeyValuePair<string, List<ClickyInkSentAction>>, string, List<InkSentActionFile>>((KeyValuePair<string, List<ClickyInkSentAction>> kv) => kv.Key, (KeyValuePair<string, List<ClickyInkSentAction>> kv) => kv.Value.Select(InkSentActionFile.From).ToList())
		};
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, InkJsonContext.Default.InkActionsSnapshot);
		writeQueue.Async(delegate
		{
			try
			{
				InkFiles.WriteBytesAtomically(path, bytes);
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "couldn't save the notes' actions: " + ex.Message);
			}
		});
	}
}

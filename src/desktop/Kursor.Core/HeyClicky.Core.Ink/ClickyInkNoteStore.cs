using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkNoteStore
{
	private sealed record Watched(bool IsEdit);

	public const int MaximumVersions = 240;

	public const int MaximumNoteBytes = 524288;

	public const double WatchInterval = 0.4;

	public const double SettleSeconds = 1.6;

	public const double SurelySettledAfterSeconds = 3600.0;

	private readonly string? baseDirectoryOverride;

	private readonly Dictionary<string, List<ClickyInkNoteVersion>> versionsByNoteId = new Dictionary<string, List<ClickyInkNoteVersion>>();

	private readonly Dictionary<string, ClickyInkDocument> lastDocumentByNoteId = new Dictionary<string, ClickyInkDocument>();

	private readonly Dictionary<string, ClickyInkLiveNote> liveNotes = new Dictionary<string, ClickyInkLiveNote>();

	private readonly Dictionary<string, (string Key, ClickyInkLiveNote Note)> settledNotes = new Dictionary<string, (string, ClickyInkLiveNote)>();

	private readonly HashSet<string> keptNoteIds = new HashSet<string>();

	private readonly InkDispatch.SerialQueue writeQueue = new InkDispatch.SerialQueue();

	private readonly Dictionary<string, Watched> watched = new Dictionary<string, Watched>();

	private readonly Dictionary<string, List<double>> finishTimes = new Dictionary<string, List<double>>();

	private readonly HashSet<string> turnsAwaitingTheirFirstEdit = new HashSet<string>();

	private readonly HashSet<string> freshPageNoteIds = new HashSet<string>();

	private readonly Dictionary<string, (DateTimeOffset ModifiedAt, long Size)> lastReadStat = new Dictionary<string, (DateTimeOffset, long)>();

	private readonly Dictionary<string, (DateTimeOffset ModifiedAt, long Size)> pendingStat = new Dictionary<string, (DateTimeOffset, long)>();

	private InkDispatch.RepeatingTimer? watchTimer;

	private static readonly InkDispatch.SerialQueue ProblemsQueue = new InkDispatch.SerialQueue();

	public static ClickyInkNoteStore Shared { get; } = new ClickyInkNoteStore(null);

	public int Revision { get; private set; }

	public static Action<string>? ClearLiveSteps { get; set; }

	public static bool IsEnabled => ClickyLiveNotePreferences.IsEnabled;

	public string? ScopeKey { get; private set; } = "local";

	private string? BaseDirectory => baseDirectoryOverride ?? AppPaths.Combine("Ink");

	public event Action? RevisionChanged;

	private void Bump()
	{
		Revision++;
		this.RevisionChanged?.Invoke();
	}

	public ClickyInkNoteStore(string? baseDirectory)
	{
		baseDirectoryOverride = baseDirectory;
	}

	public void RemoveAllForSignOut()
	{
		string scopeKey = ScopeKey;
		versionsByNoteId.Clear();
		lastDocumentByNoteId.Clear();
		liveNotes.Clear();
		settledNotes.Clear();
		watched.Clear();
		finishTimes.Clear();
		turnsAwaitingTheirFirstEdit.Clear();
		freshPageNoteIds.Clear();
		lastReadStat.Clear();
		pendingStat.Clear();
		StopWatchTimer();
		Bump();
		if (scopeKey == null)
		{
			return;
		}
		string directory = ScopeDirectory(BaseDirectory, scopeKey);
		if (directory == null)
		{
			return;
		}
		writeQueue.Async(delegate
		{
			if (Directory.Exists(directory))
			{
				Directory.Delete(directory, recursive: true);
			}
		});
	}

	public IReadOnlyList<ClickyInkNoteVersion> Versions(string noteId)
	{
		return LoadVersions(noteId);
	}

	public ClickyHomeInkNote? InkNote(string noteId)
	{
		if (!IsEnabled)
		{
			return null;
		}
		if (freshPageNoteIds.Contains(noteId) && LoadVersions(noteId).Count == 0)
		{
			return new ClickyHomeInkNote(new ClickyInkLiveNote(), DateTimeOffset.Now);
		}
		if (!liveNotes.ContainsKey(noteId) && !watched.ContainsKey(noteId) && !keptNoteIds.Contains(noteId))
		{
			ClickyInkNoteVersion clickyInkNoteVersion = LoadVersions(noteId).LastOrDefault((ClickyInkNoteVersion v) => v.IsPartial != true);
			if ((object)clickyInkNoteVersion != null && (DateTimeOffset.Now - clickyInkNoteVersion.At).TotalSeconds > 3600.0)
			{
				ClickyInkLiveNote clickyInkLiveNote = SettledNote(noteId);
				if ((object)clickyInkLiveNote != null)
				{
					return new ClickyHomeInkNote(clickyInkLiveNote, DateTimeOffset.MinValue);
				}
			}
		}
		ClickyInkLiveNote clickyInkLiveNote2 = LiveNote(noteId);
		if ((object)clickyInkLiveNote2 != null)
		{
			DateTimeOffset? dateTimeOffset = StartedAt(noteId);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (IsSettled(noteId, clickyInkLiveNote2, valueOrDefault, DateTimeOffset.Now))
				{
					ClickyInkLiveNote clickyInkLiveNote3 = SettledNote(noteId);
					if ((object)clickyInkLiveNote3 != null)
					{
						return new ClickyHomeInkNote(clickyInkLiveNote3, DateTimeOffset.MinValue);
					}
				}
				return new ClickyHomeInkNote(clickyInkLiveNote2, valueOrDefault);
			}
		}
		return null;
	}

	public ClickyInkLiveNote? LiveNote(string noteId)
	{
		if (liveNotes.TryGetValue(noteId, out ClickyInkLiveNote value))
		{
			return value;
		}
		List<ClickyInkNoteVersion> list = LoadVersions(noteId);
		if (list.Count == 0)
		{
			return null;
		}
		ClickyInkNoteVersion clickyInkNoteVersion = list[0];
		ClickyInkLiveNote clickyInkLiveNote = new ClickyInkLiveNote();
		List<double> value2;
		List<double> list2 = (finishTimes.TryGetValue(noteId, out value2) ? value2 : new List<double>()).OrderBy((double t) => t).ToList();
		foreach (ClickyInkNoteVersion item in list)
		{
			double totalSeconds = (item.At - clickyInkNoteVersion.At).TotalSeconds;
			while (list2.Count > 0 && list2[0] <= totalSeconds)
			{
				clickyInkLiveNote.Finish(list2[0]);
				list2.RemoveAt(0);
			}
			clickyInkLiveNote.Apply(ClickyInkParser.Parse(item.Text, item.IsPartial == true), totalSeconds, item.IsEdit, item.StartsNewEdit, item.IsPartial == true);
		}
		foreach (double item2 in list2)
		{
			clickyInkLiveNote.Finish(item2);
		}
		liveNotes[noteId] = clickyInkLiveNote;
		return clickyInkLiveNote;
	}

	public ClickyInkLiveNote? SettledNote(string noteId)
	{
		List<ClickyInkNoteVersion> list = LoadVersions(noteId);
		ClickyInkNoteVersion clickyInkNoteVersion = list.LastOrDefault((ClickyInkNoteVersion v) => v.IsPartial != true);
		if ((object)clickyInkNoteVersion == null)
		{
			return null;
		}
		string text = $"{list.Count}|{InkText.DoubleDescription(InkFiles.SecondsSince1970(clickyInkNoteVersion.At))}";
		if (settledNotes.TryGetValue(noteId, out (string, ClickyInkLiveNote) value) && value.Item1 == text)
		{
			return value.Item2;
		}
		ClickyInkLiveNote clickyInkLiveNote = new ClickyInkLiveNote();
		clickyInkLiveNote.Apply(ClickyInkParser.Parse(clickyInkNoteVersion.Text), 0.0, isEdit: false, null);
		settledNotes[noteId] = (text, clickyInkLiveNote);
		return clickyInkLiveNote;
	}

	private bool IsSettled(string noteId, ClickyInkLiveNote live, DateTimeOffset startedAt, DateTimeOffset now)
	{
		if (watched.ContainsKey(noteId) || keptNoteIds.Contains(noteId))
		{
			return false;
		}
		return (now - startedAt).TotalSeconds > live.FinishedClock + 1.6;
	}

	public DateTimeOffset? LastEditAt(string noteId)
	{
		return LoadVersions(noteId).LastOrDefault((ClickyInkNoteVersion v) => v.IsEdit && v.IsPartial != true)?.At;
	}

	public DateTimeOffset? StartedAt(string noteId)
	{
		List<ClickyInkNoteVersion> list = LoadVersions(noteId);
		if (list.Count <= 0)
		{
			return null;
		}
		return list[0].At;
	}

	public double Clock(string noteId, DateTimeOffset? now = null)
	{
		DateTimeOffset? dateTimeOffset = StartedAt(noteId);
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			return ((now ?? DateTimeOffset.Now) - valueOrDefault).TotalSeconds;
		}
		return 0.0;
	}

	public void NoteTurnStarted(string noteId, bool isKept)
	{
		if (isKept)
		{
			keptNoteIds.Add(noteId);
		}
		else
		{
			keptNoteIds.Remove(noteId);
		}
		if (IsEnabled && !watched.ContainsKey(noteId))
		{
			bool flag = LoadVersions(noteId).Count > 0;
			watched[noteId] = new Watched(flag);
			if (flag)
			{
				turnsAwaitingTheirFirstEdit.Add(noteId);
			}
			StartWatchTimer();
		}
	}

	public void NoteTurnEnded(string noteId)
	{
		if (!watched.Remove(noteId))
		{
			return;
		}
		turnsAwaitingTheirFirstEdit.Remove(noteId);
		ClearLiveSteps?.Invoke(noteId);
		if (watched.Count == 0)
		{
			StopWatchTimer();
		}
		ClickyLiveNoteStore.Shared.ReadNow(noteId);
		if (StartedAt(noteId).HasValue)
		{
			double num = Clock(noteId, null);
			if (!finishTimes.TryGetValue(noteId, out List<double> value))
			{
				value = (finishTimes[noteId] = new List<double>());
			}
			value.Add(num);
			if (liveNotes.TryGetValue(noteId, out ClickyInkLiveNote value2))
			{
				ClickyInkLiveNote clickyInkLiveNote = value2.Clone();
				clickyInkLiveNote.Finish(num);
				liveNotes[noteId] = clickyInkLiveNote;
				Bump();
			}
			InkDispatch.After(Math.Max(0.0, (liveNotes.TryGetValue(noteId, out ClickyInkLiveNote value3) ? value3.FinishedClock : num) - num) + 1.6 + 0.2, Bump);
		}
	}

	public void NoteActionSent()
	{
		Bump();
	}

	public void StartFreshPage(string noteId)
	{
		versionsByNoteId[noteId] = new List<ClickyInkNoteVersion>();
		lastDocumentByNoteId.Remove(noteId);
		liveNotes.Remove(noteId);
		settledNotes.Remove(noteId);
		finishTimes.Remove(noteId);
		lastReadStat.Remove(noteId);
		pendingStat.Remove(noteId);
		turnsAwaitingTheirFirstEdit.Remove(noteId);
		if (watched.ContainsKey(noteId))
		{
			watched[noteId] = new Watched(IsEdit: false);
		}
		freshPageNoteIds.Add(noteId);
		Save(noteId, new List<ClickyInkNoteVersion>());
		Bump();
	}

	public void Forget(string noteId)
	{
		if (LoadVersions(noteId).Count != 0 || liveNotes.ContainsKey(noteId) || settledNotes.ContainsKey(noteId))
		{
			versionsByNoteId[noteId] = new List<ClickyInkNoteVersion>();
			lastDocumentByNoteId.Remove(noteId);
			liveNotes.Remove(noteId);
			settledNotes.Remove(noteId);
			finishTimes.Remove(noteId);
			lastReadStat.Remove(noteId);
			pendingStat.Remove(noteId);
			freshPageNoteIds.Remove(noteId);
			Save(noteId, new List<ClickyInkNoteVersion>());
			Bump();
		}
	}

	public bool IsTurnWriting(string noteId)
	{
		return watched.ContainsKey(noteId);
	}

	public void StreamPatch(string noteId, string kind, string diff)
	{
		string text = ClickyInkPatchStream.PartialText(LoadVersions(noteId).LastOrDefault((ClickyInkNoteVersion v) => v.IsPartial != true)?.Text ?? "", kind, diff);
		if (text != null)
		{
			RecordVersion(noteId, text, watched.TryGetValue(noteId, out Watched value) && value.IsEdit, isPartial: true, null);
		}
	}

	public void RecordSaved(string noteId, string text, DateTimeOffset modifiedAt)
	{
		if (watched.TryGetValue(noteId, out Watched value))
		{
			if (RecordVersion(noteId, text, value.IsEdit, isPartial: false, null))
			{
				WriteProblems(text, noteId);
			}
			return;
		}
		bool flag = LoadVersions(noteId).Count > 0;
		if (RecordVersion(noteId, text, flag, isPartial: false, flag ? DateTimeOffset.Now : modifiedAt))
		{
			WriteProblems(text, noteId);
		}
	}

	public bool ReadNoteNow(string noteId, bool isEdit, DateTimeOffset? at = null)
	{
		(DateTimeOffset, long)? tuple = InkFiles.Stat(noteId);
		if (tuple.HasValue)
		{
			(DateTimeOffset, long) valueOrDefault = tuple.GetValueOrDefault();
			if (valueOrDefault.Item2 <= 524288)
			{
				string text = InkFiles.ReadUtf8(noteId);
				if (text != null)
				{
					lastReadStat[noteId] = valueOrDefault;
					pendingStat.Remove(noteId);
					return RecordVersion(noteId, text, isEdit, isPartial: false, at ?? DateTimeOffset.Now);
				}
			}
		}
		return false;
	}

	public bool RecordVersion(string noteId, string text, bool isEdit, bool isPartial = false, DateTimeOffset? at = null)
	{
		DateTimeOffset dateTimeOffset = at ?? DateTimeOffset.Now;
		if (ScopeKey == null || text.TrimWsNl().Length == 0)
		{
			return false;
		}
		List<ClickyInkNoteVersion> list = LoadVersions(noteId).ToList();
		if (list.Count > 0)
		{
			List<ClickyInkNoteVersion> list2 = list;
			if (list2[list2.Count - 1].Text == text)
			{
				if (!isPartial)
				{
					List<ClickyInkNoteVersion> list3 = list;
					if (list3[list3.Count - 1].IsPartial == true)
					{
						goto IL_0088;
					}
				}
				return false;
			}
		}
		goto IL_0088;
		IL_0088:
		if (isPartial && list.Count > 0)
		{
			List<ClickyInkNoteVersion> list4 = list;
			if (list4[list4.Count - 1].IsPartial == true)
			{
				List<ClickyInkNoteVersion> list5 = list;
				if ((dateTimeOffset - list5[list5.Count - 1].At).TotalSeconds < 0.45)
				{
					return false;
				}
			}
		}
		ClickyInkDocument clickyInkDocument = ClickyInkParser.Parse(text, isPartial);
		int num;
		if (!isPartial && list.Count > 0)
		{
			List<ClickyInkNoteVersion> list6 = list;
			num = ((list6[list6.Count - 1].IsPartial == true) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (list.Count > 0 && !flag)
		{
			ClickyInkDocument clickyInkDocument2;
			if (!lastDocumentByNoteId.TryGetValue(noteId, out ClickyInkDocument value))
			{
				List<ClickyInkNoteVersion> list7 = list;
				clickyInkDocument2 = ClickyInkParser.Parse(list7[list7.Count - 1].Text);
			}
			else
			{
				clickyInkDocument2 = value;
			}
			if (clickyInkDocument2 == clickyInkDocument)
			{
				return false;
			}
		}
		bool flag2 = list.Count != 0 && isEdit;
		bool flag3 = flag2 && turnsAwaitingTheirFirstEdit.Remove(noteId);
		bool flag4 = false;
		if (!isPartial && list.Count > 1)
		{
			flag4 = (from v in list.Skip(1)
				where v.IsPartial == true
				select v).ToList().Any((ClickyInkNoteVersion v) => v.StartsNewEdit == true);
			List<ClickyInkNoteVersion> list8 = new List<ClickyInkNoteVersion>();
			list8.Add(list[0]);
			list8.AddRange(from v in list.Skip(1)
				where v.IsPartial != true
				select v);
			list = list8;
		}
		DateTimeOffset dateTimeOffset2 = ((list.Count > 0) ? list[0].At : dateTimeOffset);
		DateTimeOffset dateTimeOffset3;
		if (list.Count <= 0)
		{
			dateTimeOffset3 = dateTimeOffset;
		}
		else
		{
			List<ClickyInkNoteVersion> list9 = list;
			dateTimeOffset3 = list9[list9.Count - 1].At;
		}
		DateTimeOffset dateTimeOffset4 = dateTimeOffset3;
		ClickyInkNoteVersion clickyInkNoteVersion = new ClickyInkNoteVersion((dateTimeOffset > dateTimeOffset4) ? dateTimeOffset : dateTimeOffset4, text, flag2, isPartial ? new bool?(true) : ((bool?)null), (flag3 || flag4) ? new bool?(true) : ((bool?)null));
		list.Add(clickyInkNoteVersion);
		if (list.Count > 240)
		{
			List<ClickyInkNoteVersion> list10 = new List<ClickyInkNoteVersion>();
			list10.Add(list[0]);
			list10.AddRange(list.Skip(list.Count - 239));
			list = list10;
		}
		versionsByNoteId[noteId] = list;
		lastDocumentByNoteId[noteId] = clickyInkDocument;
		freshPageNoteIds.Remove(noteId);
		if (liveNotes.TryGetValue(noteId, out ClickyInkLiveNote value2))
		{
			ClickyInkLiveNote clickyInkLiveNote = value2.Clone();
			clickyInkLiveNote.Apply(clickyInkDocument, (clickyInkNoteVersion.At - dateTimeOffset2).TotalSeconds, clickyInkNoteVersion.IsEdit, clickyInkNoteVersion.StartsNewEdit, isPartial);
			liveNotes[noteId] = clickyInkLiveNote;
		}
		if (!isPartial)
		{
			Save(noteId, list);
		}
		Bump();
		return true;
	}

	private static void WriteProblems(string text, string notePath)
	{
		string problemsText = ClickyInkLint.FileText(ClickyInkLint.Problems(text));
		string path = Path.Combine(Path.GetDirectoryName(notePath) ?? "", "note.problems");
		ProblemsQueue.Async(delegate
		{
			if (problemsText != null)
			{
				InkFiles.WriteUtf8Atomically(path, problemsText);
			}
			else if (File.Exists(path))
			{
				File.Delete(path);
			}
		});
	}

	public bool ToggleCheckbox(string noteId, string itemText)
	{
		string text = InkFiles.ReadUtf8(noteId);
		if (text != null)
		{
			string text2 = ToggledCheckbox(text, itemText);
			if (text2 != null)
			{
				try
				{
					InkFiles.WriteUtf8Atomically(noteId, text2);
				}
				catch (Exception ex)
				{
					Log.Warn("ink", "couldn't tick the box in " + noteId + ": " + ex.Message);
					return false;
				}
				(DateTimeOffset, long)? tuple = InkFiles.Stat(noteId);
				if (tuple.HasValue)
				{
					(DateTimeOffset, long) valueOrDefault = tuple.GetValueOrDefault();
					lastReadStat[noteId] = valueOrDefault;
				}
				else
				{
					lastReadStat.Remove(noteId);
				}
				pendingStat.Remove(noteId);
				return RecordVersion(noteId, text2, isEdit: true, isPartial: false, null);
			}
		}
		return false;
	}

	public static string? ToggledCheckbox(string text, string itemText)
	{
		string text2 = Plain(itemText);
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i];
			string text4 = text3.TrimWs();
			if (text4.HasPrefix("```"))
			{
				flag = !flag;
			}
			else
			{
				if (flag)
				{
					continue;
				}
				ClickyInkParser.ListStart listStart = ClickyInkParser.ListItemStart(text4);
				if ((object)listStart == null || listStart.Kind != ClickyInkDocument.Kind.Checklist || Plain(listStart.Text) != text2)
				{
					continue;
				}
				int num = -1;
				string[] array2 = new string[3] { "[ ]", "[x]", "[X]" };
				foreach (string value in array2)
				{
					int num2 = text3.Find(value);
					if (num2 >= 0 && (num < 0 || num2 < num))
					{
						num = num2;
					}
				}
				if (num >= 0)
				{
					bool flag2 = text3.Substring(num, 3) != "[ ]";
					int num3 = i;
					string text5 = text3.Substring(0, num);
					string obj = (flag2 ? "[ ]" : "[x]");
					string text6 = text3;
					int j = num + 3;
					array[num3] = text5 + obj + text6.Substring(j, text6.Length - j);
					return string.Join("\n", array);
				}
			}
		}
		return null;
		static string Plain(string words)
		{
			StringBuilder stringBuilder = new StringBuilder(words.Length);
			foreach (Rune item in words.EnumerateRunes())
			{
				int value2 = item.Value;
				if ((value2 < 57344 || value2 > 63743) ? true : false)
				{
					stringBuilder.Append(item.ToString());
				}
			}
			return stringBuilder.ToString().TrimWs();
		}
	}

	private void StartWatchTimer()
	{
		if (watchTimer == null)
		{
			watchTimer = new InkDispatch.RepeatingTimer(0.4, WatchTick);
		}
	}

	private void StopWatchTimer()
	{
		watchTimer?.Dispose();
		watchTimer = null;
	}

	private void WatchTick()
	{
		foreach (string item in watched.Keys.ToList())
		{
			(DateTimeOffset, long)? tuple = InkFiles.Stat(item);
			if (tuple.HasValue)
			{
				if (lastReadStat.TryGetValue(item, out (DateTimeOffset, long) value))
				{
					(DateTimeOffset, long) tuple2 = value;
					(DateTimeOffset, long) value2 = tuple.Value;
					if (tuple2.Item1 == value2.Item1 && tuple2.Item2 == value2.Item2)
					{
						goto IL_0074;
					}
				}
				if (pendingStat.TryGetValue(item, out (DateTimeOffset, long) value3))
				{
					(DateTimeOffset, long) value2 = value3;
					(DateTimeOffset, long) tuple2 = tuple.Value;
					if (!(value2.Item1 != tuple2.Item1) && value2.Item2 == tuple2.Item2)
					{
						lastReadStat[item] = tuple.Value;
						pendingStat.Remove(item);
						if (tuple.Value.Item2 <= 524288)
						{
							ClickyLiveNoteStore.Shared.ReadNow(item);
						}
						continue;
					}
				}
				pendingStat[item] = tuple.Value;
				continue;
			}
			goto IL_0074;
			IL_0074:
			pendingStat.Remove(item);
		}
	}

	public static string? ScopeDirectory(string? baseDirectory, string scopeKey)
	{
		if (baseDirectory != null)
		{
			return Path.Combine(baseDirectory, scopeKey);
		}
		return null;
	}

	private static string FileNameForNoteId(string noteId)
	{
		return string.Concat(from c in noteId.Characters()
			select (!InkText.IsLetter(c) && !InkText.IsNumber(c) && !(c == "-")) ? "_" : c).SuffixCharacters(200) + ".json";
	}

	public string? VersionsFilePath(string noteId)
	{
		string scopeKey = ScopeKey;
		if (scopeKey == null)
		{
			return null;
		}
		string text = ScopeDirectory(BaseDirectory, scopeKey);
		if (text == null)
		{
			return null;
		}
		return Path.Combine(text, FileNameForNoteId(noteId));
	}

	public async Task PreloadVersions(IEnumerable<string> noteIds)
	{
		string scope = ScopeKey;
		if (scope == null)
		{
			return;
		}
		List<(string Id, string Path)> files = (from id in noteIds
			where !versionsByNoteId.ContainsKey(id)
			select (Id: id, Path: VersionsFilePath(id)) into f
			where f.Path != null
			select f).ToList();
		if (files.Count == 0)
		{
			return;
		}
		List<(string, List<ClickyInkNoteVersion>)> list = await Task.Run(() => files.Select(((string Id, string Path) f) => (Id: f.Id, Versions: ReadVersionsFile(f.Path))).ToList());
		if (ScopeKey != scope)
		{
			return;
		}
		foreach (var (key, value) in list)
		{
			versionsByNoteId.TryAdd(key, value);
		}
	}

	private List<ClickyInkNoteVersion> LoadVersions(string noteId)
	{
		if (versionsByNoteId.TryGetValue(noteId, out List<ClickyInkNoteVersion> value))
		{
			return value;
		}
		string text = VersionsFilePath(noteId);
		List<ClickyInkNoteVersion> list = ((text != null) ? ReadVersionsFile(text) : new List<ClickyInkNoteVersion>());
		if (ScopeKey != null)
		{
			versionsByNoteId[noteId] = list;
		}
		return list;
	}

	private static List<ClickyInkNoteVersion> ReadVersionsFile(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return new List<ClickyInkNoteVersion>();
			}
			return JsonSerializer.Deserialize(File.ReadAllBytes(path), InkJsonContext.Default.InkNoteSnapshot)?.Versions?.Select((InkNoteVersionFile v) => v.ToVersion()).ToList() ?? new List<ClickyInkNoteVersion>();
		}
		catch (Exception)
		{
			return new List<ClickyInkNoteVersion>();
		}
	}

	private void Save(string noteId, List<ClickyInkNoteVersion> versions)
	{
		string path = VersionsFilePath(noteId);
		if (path == null)
		{
			return;
		}
		List<ClickyInkNoteVersion> kept = versions.Where((ClickyInkNoteVersion v, int index) => index == 0 || v.IsPartial != true).ToList();
		writeQueue.Async(delegate
		{
			byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new InkNoteSnapshot
			{
				Version = 1,
				Versions = kept.Select(InkNoteVersionFile.From).ToList()
			}, InkJsonContext.Default.InkNoteSnapshot);
			try
			{
				InkFiles.WriteBytesAtomically(path, bytes);
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "couldn't save " + noteId + "'s versions: " + ex.Message);
			}
		});
	}

	public void WaitForPendingSaves()
	{
		writeQueue.Sync();
		ProblemsQueue.Sync();
	}
}

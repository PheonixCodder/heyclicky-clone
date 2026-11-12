using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Agents.Clickys;

internal sealed class ClickyStore
{
	private sealed class State
	{
		public List<ClickyInfo> Clickys { get; set; } = new List<ClickyInfo>();

		public Dictionary<string, string> ThreadTitles { get; set; } = new Dictionary<string, string>();

		public Dictionary<string, string> InstructionFingerprintByThread { get; set; } = new Dictionary<string, string>();

		public Dictionary<string, string> FiledThreadOwners { get; set; } = new Dictionary<string, string>();

		public Dictionary<string, string> DisplayPrompts { get; set; } = new Dictionary<string, string>();

		public HashSet<string> HiddenThreadIds { get; set; } = new HashSet<string>();
	}

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private readonly object gate = new object();

	private readonly string path;

	private volatile string projectsRoot;

	private State state;

	public string ProjectsRoot
	{
		get
		{
			return projectsRoot;
		}
		set
		{
			projectsRoot = value;
		}
	}

	public IReadOnlyList<ClickyInfo> Clickys
	{
		get
		{
			lock (gate)
			{
				return state.Clickys.Where((ClickyInfo c) => !c.IsArchived).ToList();
			}
		}
	}

	public IReadOnlyList<ClickyInfo> ArchivedClickys
	{
		get
		{
			lock (gate)
			{
				return (from c in state.Clickys
					where c.IsArchived
					orderby c.CreatedAt descending
					select c).ToList();
			}
		}
	}

	public ClickyStore(string stateDirectory, string projectsRoot)
	{
		Directory.CreateDirectory(stateDirectory);
		path = Path.Combine(stateDirectory, "clickys.json");
		this.projectsRoot = projectsRoot;
		state = Load(path);
	}

	private static State Load(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				return JsonSerializer.Deserialize<State>(File.ReadAllText(path), Json) ?? new State();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "clickys.json unreadable, starting fresh: " + ex.Message);
		}
		return new State();
	}

	private void Save()
	{
		string sourceFileName = path + ".tmp";
		File.WriteAllText(sourceFileName, JsonSerializer.Serialize(state, Json));
		File.Move(sourceFileName, path, overwrite: true);
	}

	public ClickyInfo? Get(string slug)
	{
		lock (gate)
		{
			return state.Clickys.FirstOrDefault((ClickyInfo c) => c.Slug == slug);
		}
	}

	public ClickyInfo? ForThread(string threadId)
	{
		lock (gate)
		{
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo c) => c.ThreadId == threadId);
			if ((object)clickyInfo != null)
			{
				return clickyInfo;
			}
			string slug;
			return state.FiledThreadOwners.TryGetValue(threadId, out slug) ? state.Clickys.FirstOrDefault((ClickyInfo c) => c.Slug == slug) : null;
		}
	}

	public ClickyInfo Found(string name, string roleText, string oneLiner)
	{
		lock (gate)
		{
			name = (string.IsNullOrWhiteSpace(name) ? "Clicky Buddy" : name.Trim());
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo c) => c.IsArchived && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
			if ((object)clickyInfo != null)
			{
				ClickyInfo clickyInfo2 = clickyInfo with
				{
					IsArchived = false
				};
				Replace(clickyInfo2);
				Save();
				return clickyInfo2;
			}
			string slug;
			string value = (slug = Slugify(name));
			int num = 2;
			while (state.Clickys.Any((ClickyInfo c) => c.Slug == slug))
			{
				slug = $"{value}-{num}";
				num++;
			}
			ClickyInfo clickyInfo3 = new ClickyInfo(slug, name, roleText, oneLiner, null, DateTimeOffset.Now);
			state.Clickys.Add(clickyInfo3);
			Save();
			return clickyInfo3;
		}
	}

	public void BindThread(string slug, string threadId)
	{
		lock (gate)
		{
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo x) => x.Slug == slug);
			if ((object)clickyInfo == null || clickyInfo.ThreadId == threadId)
			{
				return;
			}
			string threadId2 = clickyInfo.ThreadId;
			if (threadId2 != null)
			{
				state.FiledThreadOwners[threadId2] = slug;
			}
			for (int i = 0; i < state.Clickys.Count; i++)
			{
				if (state.Clickys[i].ThreadId == threadId && state.Clickys[i].Slug != slug)
				{
					state.Clickys[i] = state.Clickys[i]with
					{
						ThreadId = null
					};
				}
			}
			Replace(clickyInfo with
			{
				ThreadId = threadId
			});
			Save();
		}
	}

	public void UnbindThread(string slug)
	{
		lock (gate)
		{
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo x) => x.Slug == slug);
			if (clickyInfo?.ThreadId != null)
			{
				Replace(clickyInfo with
				{
					ThreadId = null
				});
				Save();
			}
		}
	}

	public void Archive(string slug)
	{
		lock (gate)
		{
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo x) => x.Slug == slug);
			if ((object)clickyInfo != null)
			{
				Replace(clickyInfo with
				{
					IsArchived = true,
					ThreadId = null
				});
				Save();
			}
		}
	}

	private void Replace(ClickyInfo updated)
	{
		int num = state.Clickys.FindIndex((ClickyInfo c) => c.Slug == updated.Slug);
		if (num >= 0)
		{
			state.Clickys[num] = updated;
		}
		else
		{
			state.Clickys.Add(updated);
		}
	}

	private static string PromptKey(string raw)
	{
		string text = raw.Trim().Replace('\n', ' ');
		if (text.Length <= 160)
		{
			return text;
		}
		return text.Substring(0, 160);
	}

	public void RecordDisplayPrompt(string rawPrompt, string display)
	{
		lock (gate)
		{
			state.DisplayPrompts[PromptKey(rawPrompt)] = display;
			Save();
		}
	}

	public string? DisplayFor(string text)
	{
		string text2 = PromptKey(text);
		lock (gate)
		{
			if (state.DisplayPrompts.TryGetValue(text2, out string value))
			{
				return value;
			}
			foreach (KeyValuePair<string, string> displayPrompt in state.DisplayPrompts)
			{
				if (text2.StartsWith(displayPrompt.Key, StringComparison.Ordinal) || (displayPrompt.Key.StartsWith(text2, StringComparison.Ordinal) && text2.Length >= 40))
				{
					return displayPrompt.Value;
				}
			}
		}
		return null;
	}

	public bool IsThreadHidden(string threadId)
	{
		lock (gate)
		{
			return state.HiddenThreadIds.Contains(threadId);
		}
	}

	public bool HideThread(string threadId)
	{
		lock (gate)
		{
			if (!state.HiddenThreadIds.Add(threadId))
			{
				return false;
			}
			Save();
			return true;
		}
	}

	public void ClearHiddenThreads()
	{
		lock (gate)
		{
			if (state.HiddenThreadIds.Count != 0)
			{
				state.HiddenThreadIds.Clear();
				Save();
			}
		}
	}

	public string? TitleForThread(string threadId)
	{
		lock (gate)
		{
			return state.ThreadTitles.GetValueOrDefault(threadId);
		}
	}

	public void SetThreadTitle(string threadId, string title)
	{
		lock (gate)
		{
			if (!state.ThreadTitles.TryGetValue(threadId, out string value) || !(value == title))
			{
				state.ThreadTitles[threadId] = title;
				Save();
			}
		}
	}

	public void MarkThreadStartedWithInstructions(string threadId, string instructions)
	{
		lock (gate)
		{
			state.InstructionFingerprintByThread[threadId] = Fingerprint(instructions);
			if (state.InstructionFingerprintByThread.Count > 2000)
			{
				state.InstructionFingerprintByThread.Remove(state.InstructionFingerprintByThread.Keys.First());
			}
			Save();
		}
	}

	public string? InstructionsToCarry(string? threadId, string? currentInstructions)
	{
		if (threadId == null || currentInstructions == null)
		{
			return null;
		}
		lock (gate)
		{
			string value;
			return (state.InstructionFingerprintByThread.TryGetValue(threadId, out value) && value == Fingerprint(currentInstructions)) ? null : currentInstructions;
		}
	}

	private static string Fingerprint(string text)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
	}

	public string WorkspacePath(string slug)
	{
		return Path.Combine(projectsRoot, "agents", slug);
	}

	public string EnsureWorkspace(ClickyInfo clicky)
	{
		string text = WorkspacePath(clicky.Slug);
		Directory.CreateDirectory(Path.Combine(text, "output"));
		Directory.CreateDirectory(Path.Combine(text, "tmp"));
		string text2 = Path.Combine(text, "AGENTS.md");
		if (!File.Exists(text2))
		{
			File.WriteAllText(text2, SeededAgentsFile(clicky), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		return text;
	}

	private static string SeededAgentsFile(ClickyInfo c)
	{
		return $"# {c.Name}\n\nYou are {c.Name}, the user's persistent {c.RoleText.ToLowerInvariant()} agent in HeyClicky.\nWhat you do: {c.OneLiner}.\n\nThis folder is YOUR workspace: your notes live in this file and the\nfiles you produce belong here. You are not a one-off task runner —\nthe user returns to you, and you remember.\n\n## Identity\n- Name: {c.Name}\n- Role: {c.RoleText}\n- Speak as {c.Name} — consistent voice, first person.\n\n## Standing preferences\n(Durable instructions the user has given you. Keep each to one line.)\n\n## Notes\n(Your working memory. After any turn where you learn something durable\nabout this job — accounts, structures, the user's taste, decisions\nmade, ongoing threads of work — update this section: add the fact,\ndated, and prune anything stale or superseded. Keep it curated and\nshort; this file is read before every turn. Never store secrets,\npasswords, or tokens here.)\n";
	}

	public static string Slugify(string name)
	{
		string text = NonSlug().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');
		if (text.Length != 0)
		{
			if (text.Length <= 40)
			{
				return text;
			}
			return text.Substring(0, 40).Trim('-');
		}
		return "clicky";
	}

	[GeneratedRegex("[^a-z0-9]+")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex NonSlug()
	{
		return _003CRegexGenerator_g_003EF438A85ABF99E47EA4FBA640CE8836B84C8719F8A5025C90CE820D2BC67EAC31D__NonSlug_0.Instance;
	}

	public static string NormalizedName(string name)
	{
		string text = string.Join(' ', (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		if (text.Length <= 40)
		{
			return text;
		}
		return text.Substring(0, 40).TrimEnd();
	}

	public ClickyInfo? Rename(string slug, string name)
	{
		string text = NormalizedName(name);
		if (text.Length == 0)
		{
			return null;
		}
		lock (gate)
		{
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo x) => x.Slug == slug);
			if ((object)clickyInfo == null)
			{
				return null;
			}
			if (clickyInfo.Name == text)
			{
				return clickyInfo;
			}
			ClickyInfo clickyInfo2 = clickyInfo with
			{
				Name = text
			};
			Replace(clickyInfo2);
			Save();
			return clickyInfo2;
		}
	}

	public ClickyInfo? Unarchive(string slug)
	{
		lock (gate)
		{
			ClickyInfo clickyInfo = state.Clickys.FirstOrDefault((ClickyInfo x) => x.Slug == slug);
			if ((object)clickyInfo == null || !clickyInfo.IsArchived)
			{
				return clickyInfo;
			}
			ClickyInfo clickyInfo2 = clickyInfo with
			{
				IsArchived = false
			};
			Replace(clickyInfo2);
			Save();
			return clickyInfo2;
		}
	}

	public bool OwnsThread(string threadId)
	{
		lock (gate)
		{
			return state.Clickys.Any((ClickyInfo c) => c.ThreadId == threadId) || state.FiledThreadOwners.ContainsKey(threadId);
		}
	}

	public void FileThread(string threadId, string slug)
	{
		lock (gate)
		{
			if (!state.FiledThreadOwners.TryGetValue(threadId, out string value) || !(value == slug))
			{
				state.FiledThreadOwners[threadId] = slug;
				Save();
			}
		}
	}

	public void AppendNotes(ClickyInfo clicky, string markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return;
		}
		string text = Path.Combine(EnsureWorkspace(clicky), "AGENTS.md");
		lock (gate)
		{
			string text2 = (File.Exists(text) ? File.ReadAllText(text) : "");
			if (text2.Length > 0 && !text2.EndsWith('\n'))
			{
				text2 += "\n";
			}
			text2 = text2 + "\n" + markdown.Trim() + "\n";
			File.WriteAllText(text, text2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}

	public string NotesPath(string slug)
	{
		return Path.Combine(WorkspacePath(slug), "AGENTS.md");
	}

	public IReadOnlyList<string> DeleteAll()
	{
		lock (gate)
		{
			List<string> result = state.Clickys.Select((ClickyInfo c) => WorkspacePath(c.Slug)).Distinct<string>(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists)
				.ToList();
			state.Clickys.Clear();
			state.FiledThreadOwners.Clear();
			Save();
			return result;
		}
	}
}

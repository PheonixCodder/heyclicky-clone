using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Notch;

public sealed class ChatHandOffs
{
	public sealed record BackfillTarget(string? JobId, string? ClickySlug, string ThreadId, string Name, string? TaskTitle);

	public sealed record FinishedTurn(DateTimeOffset? FinishedAt, IReadOnlyList<string> AssistantMessages, IReadOnlyList<string> ArtifactEntries);

	private const int KeptEntryLimit = 200;

	private static readonly TimeSpan ReturnWindow = TimeSpan.FromDays(3);

	private static readonly TimeSpan BackfillWindow = TimeSpan.FromDays(7);

	private static readonly TimeSpan TurnMatchTolerance = TimeSpan.FromMinutes(20L);

	private readonly object gate = new object();

	private readonly Func<string?> accountId;

	private readonly Func<DateTimeOffset> clock;

	private readonly string folder;

	private List<ChatHandOff> entries = new List<ChatHandOff>();

	private string? account;

	private string? backfilledAccount;

	public IChatHandOffDirectory? Directory { get; set; }

	public Action? RefreshHistorySoon { get; set; }

	public IReadOnlyList<ChatHandOff> Entries
	{
		get
		{
			lock (gate)
			{
				AdoptAccount(accountId());
				return entries.ToList();
			}
		}
	}

	public Func<IReadOnlyList<(string Slug, string? ThreadId, DateTimeOffset? InterruptedAt)>>? TakeRestartInterruptions { get; set; }

	private string? FilePath
	{
		get
		{
			if (string.IsNullOrEmpty(account))
			{
				return null;
			}
			string text = new string(account.Where((char c) => char.IsLetterOrDigit(c) || c == '-').ToArray());
			if (text.Length != 0)
			{
				return Path.Combine(folder, text + "-handoffs.json");
			}
			return null;
		}
	}

	public event Action? Changed;

	public ChatHandOffs(Func<string?> accountId, string? folder = null, Func<DateTimeOffset>? clock = null)
	{
		this.accountId = accountId;
		this.clock = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow));
		this.folder = folder ?? AppPaths.Combine("HeyClickyHistory");
	}

	public ChatHandOff? Entry(string entryId)
	{
		lock (gate)
		{
			return entries.FirstOrDefault((ChatHandOff e) => e.Id == entryId);
		}
	}

	public void NoteStartedJob(string jobId, string title)
	{
		Append(new ChatHandOff
		{
			Id = Guid.NewGuid().ToString(),
			Kind = ChatHandOffKind.Started,
			JobId = jobId,
			Title = title,
			CreatedAt = clock(),
			TaskTitle = title
		});
	}

	public void NoteStarted(string? clickySlug, string? threadId, bool isFollowUp, string? taskTitle = null, bool isSentFromWindow = false, string? sentText = null, IReadOnlyList<string>? sentFiles = null)
	{
		string text = ShortTaskTitle(taskTitle);
		string sentText2 = (string.IsNullOrWhiteSpace(sentText) ? null : sentText.Trim());
		List<string> sentFiles2 = ((sentFiles != null && sentFiles.Count > 0) ? sentFiles.ToList() : null);
		if (threadId != null && clickySlug == null)
		{
			(string, string)? tuple = Directory?.JobForThread(threadId);
			if (tuple.HasValue)
			{
				(string, string) valueOrDefault = tuple.GetValueOrDefault();
				Append(new ChatHandOff
				{
					Id = Guid.NewGuid().ToString(),
					Kind = ChatHandOffKind.Started,
					JobId = valueOrDefault.Item1,
					Title = valueOrDefault.Item2,
					CreatedAt = clock(),
					IsFollowUp = isFollowUp,
					TaskTitle = (isFollowUp ? text : valueOrDefault.Item2),
					IsSentFromWindow = isSentFromWindow,
					SentText = sentText2,
					SentFiles = sentFiles2
				});
				return;
			}
		}
		string text2 = clickySlug ?? ((threadId == null) ? null : Directory?.ClickySlugForThread(threadId));
		if (text2 != null)
		{
			string text3 = Directory?.ClickyName(text2);
			if (text3 != null)
			{
				Append(new ChatHandOff
				{
					Id = Guid.NewGuid().ToString(),
					Kind = ChatHandOffKind.Started,
					ClickySlug = text2,
					Title = text3,
					CreatedAt = clock(),
					IsFollowUp = isFollowUp,
					TaskTitle = text,
					IsSentFromWindow = isSentFromWindow,
					SentText = sentText2,
					SentFiles = sentFiles2
				});
			}
		}
	}

	public static string? ShortTaskTitle(string? text)
	{
		if (text == null)
		{
			return null;
		}
		string text2 = string.Join(" ", text.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
		if (text2.Length == 0)
		{
			return null;
		}
		if (text2.Length <= 70)
		{
			return text2;
		}
		return text2.Substring(0, 69).Trim() + "…";
	}

	public void NoteTaskEnded(ChatTaskEnd task, ChatHandOffOutcome outcome)
	{
		if (task.IsDemo)
		{
			return;
		}
		string text = task.JobId ?? ((task.ThreadId == null) ? null : Directory?.JobForThread(task.ThreadId)?.Id);
		string text2 = ((text == null) ? (task.ClickySlug ?? ((task.ThreadId == null) ? null : Directory?.ClickySlugForThread(task.ThreadId))) : null);
		if (text == null && text2 == null)
		{
			return;
		}
		ChatHandOff entry;
		lock (gate)
		{
			AdoptAccount(accountId());
			string key = ChatHandOff.TargetKeyFor(text, text2);
			ChatHandOff chatHandOff = entries.LastOrDefault((ChatHandOff e) => e.TargetKey == key && e.Kind == ChatHandOffKind.Started);
			if ((object)chatHandOff != null && clock() - chatHandOff.CreatedAt >= ReturnWindow)
			{
				chatHandOff = null;
			}
			string title = chatHandOff?.Title ?? ((text == null) ? null : Directory?.JobTitle(text)) ?? ((text2 == null) ? null : Directory?.ClickyName(text2)) ?? "Your Clicky";
			bool flag = task.PendingComputerUseRequest != null || task.PendingUserStep != null || task.NeedsChromeConnection;
			ChatHandOffOutcome chatHandOffOutcome = ((outcome == ChatHandOffOutcome.Finished && flag) ? ChatHandOffOutcome.NeedsYou : outcome);
			ChatHandOff chatHandOff2 = entries.LastOrDefault((ChatHandOff e) => e.TargetKey == key);
			if ((object)chatHandOff2 != null && chatHandOff2.Kind == ChatHandOffKind.CameBack && clock() - chatHandOff2.CreatedAt < TimeSpan.FromSeconds(3L))
			{
				return;
			}
			bool flag2 = chatHandOffOutcome == ChatHandOffOutcome.Finished;
			List<string> list = (flag2 ? FollowUpsFrom(task.SuggestedNextActions) : new List<string>());
			entry = new ChatHandOff
			{
				Id = Guid.NewGuid().ToString(),
				Kind = ChatHandOffKind.CameBack,
				JobId = (chatHandOff?.JobId ?? text),
				ClickySlug = (chatHandOff?.ClickySlug ?? text2),
				Title = title,
				CreatedAt = clock(),
				Outcome = chatHandOffOutcome,
				ReplyText = ReplyText(task, chatHandOffOutcome),
				FullReply = (flag2 ? FullReplyText(task) : null),
				ArtifactEntries = ((flag2 && task.UserFacingArtifactPaths.Count > 0) ? task.UserFacingArtifactPaths.ToList() : null),
				ThreadId = task.ThreadId,
				IsRoutineRun = (task.IsScheduledRoutineRun ? new bool?(true) : ((bool?)null)),
				Answer = (flag2 ? task.FinalAnswer : null),
				FollowUps = ((list.Count > 0) ? list : null),
				TaskTitle = (chatHandOff?.TaskTitle ?? ((text == null) ? null : Directory?.JobTitle(text)) ?? ShortTaskTitle(task.CompletedThreadName)),
				SitesRead = ((task.SitesRead.Count > 0) ? task.SitesRead.ToList() : null)
			};
		}
		Append(entry);
	}

	public IReadOnlyList<string> ShownFollowUps(string entryId)
	{
		lock (gate)
		{
			ChatHandOff entry = entries.FirstOrDefault((ChatHandOff e) => e.Id == entryId);
			if ((object)entry != null && entry.Kind == ChatHandOffKind.CameBack)
			{
				ChatHandOffOutcome? outcome = entry.Outcome;
				if (outcome.HasValue && outcome.GetValueOrDefault() == ChatHandOffOutcome.Finished)
				{
					if (entries.LastOrDefault((ChatHandOff e) => e.TargetKey == entry.TargetKey)?.Id != entry.Id)
					{
						return Array.Empty<string>();
					}
					return entry.FollowUps ?? Array.Empty<string>();
				}
			}
			return Array.Empty<string>();
		}
	}

	public static List<string> FollowUpsFrom(IEnumerable<string> suggestedNextActions)
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<string> list = new List<string>();
		char[] trimChars = new char[9] { ' ', '\t', '\n', '\r', '"', '\'', '“', '”', '.' };
		foreach (string suggestedNextAction in suggestedNextActions)
		{
			string text = string.Join(" ", suggestedNextAction.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)).Trim(trimChars);
			if (text.Length != 0 && text.Length <= 70 && hashSet.Add(text.ToLowerInvariant()))
			{
				list.Add(text);
				if (list.Count == 3)
				{
					break;
				}
			}
		}
		return list;
	}

	public static string? FullReplyText(ChatTaskEnd task)
	{
		List<string> list = task.CompletedAssistantMessages.ToList();
		string finalResponseMarkdown = task.FinalResponseMarkdown;
		if (finalResponseMarkdown != null && finalResponseMarkdown.Trim() != list.LastOrDefault()?.Trim())
		{
			list.Add(finalResponseMarkdown);
		}
		return FullReplyText(list);
	}

	public static string? FullReplyText(IEnumerable<string> assistantMessages)
	{
		string text = string.Join("\n\n", from m in assistantMessages
			select m.Trim() into m
			where m.Length > 0
			select m);
		if (text.Length == 0)
		{
			return null;
		}
		if (text.Length <= 40000)
		{
			return text;
		}
		return text.Substring(0, 39999) + "…";
	}

	private static string? ReplyText(ChatTaskEnd task, ChatHandOffOutcome outcome)
	{
		string text = (outcome switch
		{
			ChatHandOffOutcome.NeedsYou => new string[4]
			{
				task.PendingComputerUseRequest,
				task.PendingUserStep,
				task.NeedsChromeConnection ? "Needs your Chrome connected to carry on." : null,
				task.FinalSummary
			}, 
			ChatHandOffOutcome.Finished => new string[2] { task.FinalSummary, task.FinalResponseMarkdown }, 
			_ => new string[1] { task.FinalSummary }, 
		}).Select((string c) => c?.Trim()).FirstOrDefault((string c) => !string.IsNullOrEmpty(c));
		if (text == null)
		{
			return null;
		}
		if (text.Length <= 600)
		{
			return text;
		}
		return text.Substring(0, 599).Trim() + "…";
	}

	public string? LatestReturnEntryId(string? jobId, string? clickySlug)
	{
		string key = ChatHandOff.TargetKeyFor(jobId, clickySlug);
		lock (gate)
		{
			return entries.LastOrDefault((ChatHandOff e) => e.TargetKey == key && e.Kind == ChatHandOffKind.CameBack)?.Id;
		}
	}

	public string? LatestReplyEntryId(string? jobId, string? clickySlug)
	{
		string key = ChatHandOff.TargetKeyFor(jobId, clickySlug);
		lock (gate)
		{
			ChatHandOff chatHandOff = entries.LastOrDefault(delegate(ChatHandOff e)
			{
				bool flag = e.TargetKey == key;
				if (flag)
				{
					ChatHandOffKind kind = e.Kind;
					bool flag2 = (uint)kind <= 1u;
					flag = flag2;
				}
				return flag;
			});
			if ((object)chatHandOff == null || chatHandOff.Kind != ChatHandOffKind.CameBack)
			{
				return null;
			}
			return ((chatHandOff.FullReply ?? chatHandOff.ReplyText ?? "").Trim().Length == 0) ? null : chatHandOff.Id;
		}
	}

	public string? LatestEntryId(string? jobId, string? clickySlug)
	{
		string text = LatestReturnEntryId(jobId, clickySlug);
		if (text != null)
		{
			return text;
		}
		string key = ChatHandOff.TargetKeyFor(jobId, clickySlug);
		lock (gate)
		{
			return entries.LastOrDefault((ChatHandOff e) => e.TargetKey == key)?.Id;
		}
	}

	public void NoteRestartInterruption(string slug, string? threadId, DateTimeOffset? interruptedAt)
	{
		string text = Directory?.ClickyName(slug);
		if (text == null)
		{
			return;
		}
		ChatHandOff chatHandOff;
		lock (gate)
		{
			chatHandOff = entries.LastOrDefault((ChatHandOff e) => e.TargetKey == ChatHandOff.TargetKeyFor(null, slug) && e.Kind == ChatHandOffKind.Started);
		}
		Append(new ChatHandOff
		{
			Id = Guid.NewGuid().ToString(),
			Kind = ChatHandOffKind.CameBack,
			ClickySlug = slug,
			Title = (chatHandOff?.Title ?? text),
			CreatedAt = (interruptedAt ?? clock()),
			Outcome = ChatHandOffOutcome.Stopped,
			ReplyText = "HeyClicky restarted before this finished.",
			ThreadId = threadId,
			TaskTitle = chatHandOff?.TaskTitle
		});
	}

	public bool NoteCard(JsonObject card, string threadId)
	{
		lock (gate)
		{
			int num = entries.FindLastIndex((ChatHandOff e) => e.Kind == ChatHandOffKind.CameBack && e.ThreadId == threadId);
			if (num < 0 || clock() - entries[num].CreatedAt >= TimeSpan.FromDays(1))
			{
				return false;
			}
			entries[num] = entries[num]with
			{
				Card = card
			};
			SaveToDisk();
		}
		RaiseChanged();
		return true;
	}

	public JsonObject? LatestCard(string? jobId, string? clickySlug)
	{
		string text = LatestReturnEntryId(jobId, clickySlug);
		if (text == null)
		{
			return null;
		}
		return Entry(text)?.Card;
	}

	public void NoteTextDelivered(string text, string? appName, bool wasCopied)
	{
		string text2 = text.Trim();
		if (text2.Length != 0)
		{
			string deliveredText = ((text2.Length > 4000) ? (text2.Substring(0, 3999) + "…") : text2);
			Append(new ChatHandOff
			{
				Id = Guid.NewGuid().ToString(),
				Kind = ChatHandOffKind.Text,
				Title = (appName ?? ""),
				CreatedAt = clock(),
				DeliveredText = deliveredText,
				DeliveredToApp = appName,
				WasCopied = (wasCopied ? new bool?(true) : ((bool?)null))
			});
		}
	}

	public void NoteDrawingKept(string lessonId, string title)
	{
		Append(new ChatHandOff
		{
			Id = Guid.NewGuid().ToString(),
			Kind = ChatHandOffKind.Drawing,
			Title = title,
			CreatedAt = clock(),
			LessonId = lessonId
		});
	}

	public void Remove(string entryId)
	{
		lock (gate)
		{
			if (entries.RemoveAll((ChatHandOff e) => e.Id == entryId) == 0)
			{
				return;
			}
			SaveToDisk();
		}
		RaiseChanged();
	}

	public void Reset()
	{
		lock (gate)
		{
			account = null;
			entries = new List<ChatHandOff>();
		}
		RaiseChanged();
	}

	public void Load()
	{
		bool flag;
		lock (gate)
		{
			AdoptAccount(accountId());
			flag = account != null;
		}
		if (flag)
		{
			IReadOnlyList<(string, string, DateTimeOffset?)> readOnlyList = TakeRestartInterruptions?.Invoke();
			if (readOnlyList != null && readOnlyList.Count > 0)
			{
				foreach (var item in readOnlyList)
				{
					NoteRestartInterruption(item.Item1, item.Item2, item.Item3);
				}
			}
		}
		RaiseChanged();
	}

	public void BackfillRepliesIfNeeded(IEnumerable<BackfillTarget> targets, Func<string, IReadOnlyList<FinishedTurn>> turns)
	{
		bool flag = false;
		lock (gate)
		{
			AdoptAccount(accountId());
			if (account == null || backfilledAccount == account)
			{
				return;
			}
			backfilledAccount = account;
			foreach (BackfillTarget target in targets)
			{
				IReadOnlyList<FinishedTurn> readOnlyList = turns(target.ThreadId);
				if (readOnlyList.Count == 0)
				{
					continue;
				}
				FinishedTurn finishedTurn = readOnlyList[readOnlyList.Count - 1];
				string key = ChatHandOff.TargetKeyFor(target.JobId, target.ClickySlug);
				List<int> list = (from p in entries.Select((ChatHandOff e, int i) => (e: e, i: i))
					where p.e.TargetKey == key && p.e.Kind == ChatHandOffKind.CameBack
					select p.i).ToList();
				foreach (int item in list)
				{
					ChatHandOff entry = entries[item];
					if (entry.FullReply == null && entry.Outcome.GetValueOrDefault() == ChatHandOffOutcome.Finished)
					{
						FinishedTurn finishedTurn2 = (from t in readOnlyList
							where t.FinishedAt.HasValue
							orderby Math.Abs((t.FinishedAt.Value - entry.CreatedAt).TotalSeconds)
							select t).FirstOrDefault();
						object obj;
						if ((object)finishedTurn2 == null || !(Math.Abs((finishedTurn2.FinishedAt.Value - entry.CreatedAt).TotalSeconds) <= TurnMatchTolerance.TotalSeconds))
						{
							obj = ((item == list[list.Count - 1]) ? finishedTurn : null);
						}
						else
						{
							obj = finishedTurn2;
						}
						FinishedTurn finishedTurn3 = (FinishedTurn)obj;
						if ((object)finishedTurn3 != null)
						{
							entries[item] = entry with
							{
								FullReply = FullReplyText(finishedTurn3.AssistantMessages),
								ArtifactEntries = (entry.ArtifactEntries ?? ((finishedTurn3.ArtifactEntries.Count > 0) ? finishedTurn3.ArtifactEntries : null)),
								ThreadId = (entry.ThreadId ?? target.ThreadId),
								TaskTitle = (entry.TaskTitle ?? target.TaskTitle)
							};
							flag = true;
						}
					}
				}
				if (list.Count != 0)
				{
					continue;
				}
				DateTimeOffset? finishedAt = finishedTurn.FinishedAt;
				if (finishedAt.HasValue)
				{
					DateTimeOffset valueOrDefault = finishedAt.GetValueOrDefault();
					if (clock() - valueOrDefault < BackfillWindow)
					{
						entries.Add(new ChatHandOff
						{
							Id = Guid.NewGuid().ToString(),
							Kind = ChatHandOffKind.CameBack,
							JobId = target.JobId,
							ClickySlug = target.ClickySlug,
							Title = target.Name,
							CreatedAt = valueOrDefault,
							Outcome = ChatHandOffOutcome.Finished,
							FullReply = FullReplyText(finishedTurn.AssistantMessages),
							ArtifactEntries = ((finishedTurn.ArtifactEntries.Count > 0) ? finishedTurn.ArtifactEntries : null),
							ThreadId = target.ThreadId,
							TaskTitle = target.TaskTitle
						});
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			entries = entries.OrderBy((ChatHandOff e) => e.CreatedAt).ToList();
			if (entries.Count > 200)
			{
				entries.RemoveRange(0, entries.Count - 200);
			}
			SaveToDisk();
		}
		RaiseChanged();
	}

	private void Append(ChatHandOff entry)
	{
		lock (gate)
		{
			AdoptAccount(accountId());
			if (account == null)
			{
				return;
			}
			entries.Add(entry);
			if (entries.Count > 200)
			{
				entries.RemoveRange(0, entries.Count - 200);
			}
			SaveToDisk();
		}
		RaiseChanged();
		Action refresh = RefreshHistorySoon;
		if (refresh != null)
		{
			Task.Delay(TimeSpan.FromSeconds(3L)).ContinueWith(delegate
			{
				refresh();
			}, TaskScheduler.Default);
		}
	}

	private void AdoptAccount(string? next)
	{
		if (next == account)
		{
			return;
		}
		account = next;
		entries = new List<ChatHandOff>();
		string filePath = FilePath;
		if (filePath == null || !File.Exists(filePath))
		{
			return;
		}
		try
		{
			entries = JsonSerializer.Deserialize(File.ReadAllBytes(filePath), NotchChatJsonContext.Default.ListChatHandOff) ?? new List<ChatHandOff>();
		}
		catch (Exception ex)
		{
			Log.Warn("ChatHandOffs", "saved entries unreadable: " + ex.Message);
		}
	}

	private void SaveToDisk()
	{
		string path = FilePath;
		if (path == null)
		{
			return;
		}
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(entries, NotchChatJsonContext.Default.ListChatHandOff);
		Task.Run(delegate
		{
			try
			{
				System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
				string text = path + ".tmp";
				File.WriteAllBytes(text, bytes);
				File.Move(text, path, overwrite: true);
			}
			catch (Exception ex)
			{
				Log.Warn("ChatHandOffs", "write failed: " + ex.Message);
			}
		});
	}

	private void RaiseChanged()
	{
		try
		{
			this.Changed?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Warn("ChatHandOffs", "Changed handler threw: " + ex.Message);
		}
	}
}

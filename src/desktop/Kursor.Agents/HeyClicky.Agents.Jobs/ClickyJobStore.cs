using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyJobStore
{
	private const string DefaultsKey = "clicky.jobs.v1";

	private const int MaximumJobsKept = 80;

	private static readonly string[] CloudColors = new string[4] { "red", "blue", "yellow", "green" };

	private readonly object gate = new object();

	private List<ClickyJob> jobs;

	public Func<IReadOnlyList<string>>? CloudColorsInUseByClickys { get; set; }

	public Func<ISet<string>>? JobIdsWithRoutines { get; set; }

	public IReadOnlyList<ClickyJob> Jobs
	{
		get
		{
			lock (gate)
			{
				return jobs.ToList();
			}
		}
	}

	public IReadOnlyList<ClickyJob> RailJobs
	{
		get
		{
			lock (gate)
			{
				return (from j in jobs
					where !j.IsDismissed
					orderby j.CreatedAt
					select j).ToList();
			}
		}
	}

	public IReadOnlyList<ClickyJob> ArchivedJobs
	{
		get
		{
			lock (gate)
			{
				return (from j in jobs
					where j.IsArchived == true
					orderby j.CreatedAt descending
					select j).ToList();
			}
		}
	}

	public ClickyWorkFolderFiles WorkFolderFiles { get; } = new ClickyWorkFolderFiles();

	public event EventHandler? Changed;

	public ClickyJobStore()
	{
		jobs = (AgentDefaults.Get<List<ClickyJob>>("clicky.jobs.v1") ?? new List<ClickyJob>()).Select((ClickyJob job) => (job.Outcome != 0) ? job : job with
		{
			Outcome = ClickyJobOutcome.Stopped,
			FinishedAt = (job.FinishedAt ?? DateTimeOffset.Now)
		}).ToList();
	}

	public ClickyJob? Job(string jobId)
	{
		lock (gate)
		{
			return jobs.FirstOrDefault((ClickyJob j) => j.Id == jobId);
		}
	}

	public ClickyJob? JobForThread(string threadId)
	{
		lock (gate)
		{
			return jobs.FirstOrDefault((ClickyJob j) => j.ThreadId == threadId) ?? jobs.FirstOrDefault((ClickyJob j) => j.Runs?.Any((ClickyJobRun r) => r.ThreadId == threadId) ?? false);
		}
	}

	public string? BuildsOnContext(IEnumerable<string> jobIds, string? excludingJobId, Func<string, (string Id, string Title, string? Folder)?>? resolveElsewhere = null)
	{
		HashSet<string> seen = new HashSet<string>();
		List<(string Id, string Title, string? Folder)> list = (from work in jobIds.Select(delegate(string id)
			{
				ClickyJob? clickyJob = Job(id) ?? JobForThread(id);
				return ((object?)clickyJob == null) ? resolveElsewhere?.Invoke(id) : (clickyJob.Id, clickyJob.Title, clickyJob.FolderPath);
			}).OfType<(string Id, string Title, string? Folder)>()
			where work.Id != excludingJobId && seen.Add(work.Id)
			select work).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		string text = string.Join("; ", list.Select(((string Id, string Title, string? Folder) work) => (work.Folder == null) ? $"\"{work.Title}\" (job_id {work.Id})" : $"\"{work.Title}\" (job_id {work.Id}, folder {work.Folder})"));
		return "This builds on earlier work: " + text + ". Before you start, read what it found with read_job (its report, and the files and rules it names), and use it: don't redo that work or ask the user for what it already has.";
	}

	public ClickyJob CreateJob(string title, string prompt, string projectsRootPath, bool startsWorking = true)
	{
		string text = Guid.NewGuid().ToString("N").Substring(0, 10)
			.ToLowerInvariant();
		string text2 = title.Trim();
		string title2 = ((text2.Length == 0) ? FallbackTitle(prompt) : text2);
		string text3 = ClickyWorkFolderFiles.FolderSlug(title2);
		string text4 = Path.Combine(projectsRootPath, "jobs", (text3.Length == 0) ? text : (text3 + "-" + text));
		string[] array = new string[2] { "tmp", "output" };
		foreach (string text5 in array)
		{
			try
			{
				Directory.CreateDirectory(Path.Combine(text4, text5));
			}
			catch (Exception ex)
			{
				Log.Warn("Jobs", $"couldn't make {text5} in {text4}: {ex.Message}");
			}
		}
		string text6 = ((prompt.Length > 2000) ? prompt.Substring(0, 2000) : prompt);
		IReadOnlyList<ClickyJobRun> runs;
		if (!startsWorking)
		{
			IReadOnlyList<ClickyJobRun> readOnlyList = Array.Empty<ClickyJobRun>();
			runs = readOnlyList;
		}
		else
		{
			IReadOnlyList<ClickyJobRun> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<ClickyJobRun>(new ClickyJobRun
			{
				StartedAt = DateTimeOffset.Now,
				Ask = text6,
				Outcome = ClickyJobOutcome.Working
			});
			runs = readOnlyList;
		}
		ClickyJob obj = new ClickyJob
		{
			Id = text,
			Title = title2,
			Prompt = text6,
			FolderPath = text4,
			CreatedAt = DateTimeOffset.Now,
			FinishedAt = (startsWorking ? ((DateTimeOffset?)null) : new DateTimeOffset?(DateTimeOffset.Now)),
			Outcome = ((!startsWorking) ? ClickyJobOutcome.Done : ClickyJobOutcome.Working),
			IsSeen = !startsWorking,
			IsDismissed = !startsWorking,
			CloudColorName = LeastUsedCloudColor(),
			Runs = runs
		};
		ClickyJob clickyJob = obj;
		lock (gate)
		{
			jobs.Add(clickyJob);
			TrimKeptJobsLocked();
			SaveLocked();
		}
		RaiseChanged();
		return clickyJob;
	}

	public void BeginRun(string jobId, string ask, string? threadId = null)
	{
		Update(jobId, delegate(ClickyJob job)
		{
			IReadOnlyList<ClickyJobRun> readOnlyList = job.Runs;
			if (readOnlyList == null)
			{
				string threadId2 = job.ThreadId;
				if (threadId2 == null)
				{
					IReadOnlyList<ClickyJobRun> readOnlyList2 = Array.Empty<ClickyJobRun>();
					readOnlyList = readOnlyList2;
				}
				else
				{
					IReadOnlyList<ClickyJobRun> readOnlyList2 = new _003C_003Ez__ReadOnlySingleElementList<ClickyJobRun>(new ClickyJobRun
					{
						ThreadId = threadId2,
						StartedAt = job.CreatedAt,
						Ask = job.Prompt,
						FinishedAt = job.FinishedAt,
						Outcome = ((job.Outcome == ClickyJobOutcome.Working) ? ClickyJobOutcome.Stopped : job.Outcome),
						Summary = job.Summary
					});
					readOnlyList = readOnlyList2;
				}
			}
			List<ClickyJobRun> list = readOnlyList.ToList();
			list.Add(new ClickyJobRun
			{
				ThreadId = threadId,
				StartedAt = DateTimeOffset.Now,
				Ask = ((ask.Length > 2000) ? ask.Substring(0, 2000) : ask),
				Outcome = ClickyJobOutcome.Working
			});
			return job with
			{
				Runs = list,
				WaitingStep = null
			};
		});
	}

	public void SetResultCard(string jobId, ClickyAnswer? card)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			ResultCard = card
		});
	}

	public void SetWaitingStep(string jobId, ClickyJobUserStep? step)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			WaitingStep = step
		});
	}

	public void SetWaitsForChrome(string jobId, bool waits)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			WaitsForChrome = (waits ? new bool?(true) : ((bool?)null))
		});
	}

	public void SetSkipsChrome(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			SkipsChrome = true
		});
	}

	public void SetNickname(string jobId, string nickname)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			Nickname = nickname
		});
	}

	private string LeastUsedCloudColor()
	{
		List<string> inUse;
		lock (gate)
		{
			inUse = (from j in jobs
				where !j.IsDismissed
				select j.CloudColorName ?? SeededCloudColor(j.Id)).ToList();
		}
		try
		{
			inUse.AddRange(CloudColorsInUseByClickys?.Invoke() ?? Array.Empty<string>());
		}
		catch (Exception)
		{
		}
		int fewest = ((IEnumerable<string>)CloudColors).Min((Func<string, int>)Uses);
		List<string> list = CloudColors.Where((string c) => Uses(c) == fewest).ToList();
		return list[Random.Shared.Next(list.Count)];
		int Uses(string color)
		{
			return inUse.Count((string c) => c == color);
		}
	}

	public static string SeededCloudColor(string seed)
	{
		ulong num = 5381uL;
		foreach (Rune item in seed.EnumerateRunes())
		{
			num = num * 33 + (ulong)item.Value;
		}
		return CloudColors[(uint)(num % (ulong)CloudColors.Length)];
	}

	public ClickyJob CreateWaitingJob(string title, string pitch, Guid suggestionCardId, string projectsRootPath)
	{
		lock (gate)
		{
			ClickyJob clickyJob = jobs.FirstOrDefault((ClickyJob j) => j.SuggestionCardId == suggestionCardId && !j.IsDismissed);
			if ((object)clickyJob != null)
			{
				return clickyJob;
			}
		}
		ClickyJob clickyJob2 = CreateJob(title, pitch, projectsRootPath);
		Update(clickyJob2.Id, (ClickyJob j) => j with
		{
			Outcome = ClickyJobOutcome.Stopped,
			FinishedAt = DateTimeOffset.Now,
			IsSeen = true,
			SuggestionCardId = suggestionCardId,
			Runs = Array.Empty<ClickyJobRun>()
		});
		return Job(clickyJob2.Id) ?? clickyJob2;
	}

	public ClickyJob? WaitingJob(Guid suggestionCardId)
	{
		lock (gate)
		{
			return jobs.FirstOrDefault(delegate(ClickyJob j)
			{
				Guid? suggestionCardId2 = j.SuggestionCardId;
				Guid guid = suggestionCardId;
				return suggestionCardId2.HasValue && suggestionCardId2.GetValueOrDefault() == guid && j.ThreadId == null && !j.IsDismissed;
			});
		}
	}

	public ClickyJob? BeginWaitingJob(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			SuggestionCardId = null,
			Runs = new _003C_003Ez__ReadOnlySingleElementList<ClickyJobRun>(new ClickyJobRun
			{
				StartedAt = DateTimeOffset.Now,
				Ask = job.Prompt,
				Outcome = ClickyJobOutcome.Working
			})
		});
		MarkWorking(jobId);
		return Job(jobId);
	}

	public void RelinkWaitingJobsToRestoredCards(IReadOnlyList<(Guid Id, string Description)> cards)
	{
		foreach (KeyValuePair<string, Guid> item in WaitingJobRelinks(Jobs, cards))
		{
			var (jobId, cardId) = item;
			Update(jobId, (ClickyJob job) => job with
			{
				SuggestionCardId = cardId
			});
		}
	}

	public static Dictionary<string, Guid> WaitingJobRelinks(IReadOnlyList<ClickyJob> jobs, IReadOnlyList<(Guid Id, string Description)> cards)
	{
		HashSet<Guid> hashSet = cards.Select<(Guid, string), Guid>(((Guid Id, string Description) c) => c.Id).ToHashSet();
		HashSet<Guid> claimedCardIds = (from job in jobs
			where job.IsWaitingToStart && !job.IsDismissed
			select job.SuggestionCardId.Value).Where(hashSet.Contains).ToHashSet();
		Dictionary<string, Guid> dictionary = new Dictionary<string, Guid>();
		foreach (ClickyJob job2 in jobs.Where((ClickyJob job) => job.IsWaitingToStart && !job.IsDismissed))
		{
			Guid? suggestionCardId = job2.SuggestionCardId;
			if (!suggestionCardId.HasValue)
			{
				continue;
			}
			Guid valueOrDefault = suggestionCardId.GetValueOrDefault();
			if (!hashSet.Contains(valueOrDefault))
			{
				List<(Guid, string)> list = cards.Where<(Guid, string)>(((Guid Id, string Description) card) => !claimedCardIds.Contains(card.Id) && ((card.Description.Length > 2000) ? card.Description.Substring(0, 2000) : card.Description) == job2.Prompt).ToList();
				if (list.Count == 1)
				{
					claimedCardIds.Add(list[0].Item1);
					dictionary[job2.Id] = list[0].Item1;
				}
			}
		}
		return dictionary;
	}

	public ClickyJob CreateJobForMovedRoutine(string title, string prompt, string folderPath, string? earlierThreadId, DateTimeOffset? lastRunAt)
	{
		string id = Guid.NewGuid().ToString("N").Substring(0, 10)
			.ToLowerInvariant();
		string[] array = new string[2] { "tmp", "output" };
		foreach (string text in array)
		{
			try
			{
				Directory.CreateDirectory(Path.Combine(folderPath, text));
			}
			catch (Exception ex)
			{
				Log.Warn("Jobs", $"couldn't make {text} in {folderPath}: {ex.Message}");
			}
		}
		DateTimeOffset now = DateTimeOffset.Now;
		string text2 = title.Trim();
		string text3 = ((prompt.Length > 2000) ? prompt.Substring(0, 2000) : prompt);
		IReadOnlyList<ClickyJobRun> runs;
		if (earlierThreadId != null)
		{
			IReadOnlyList<ClickyJobRun> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<ClickyJobRun>(new ClickyJobRun
			{
				ThreadId = earlierThreadId,
				StartedAt = (lastRunAt ?? now),
				Ask = text3,
				FinishedAt = (lastRunAt ?? now),
				Outcome = ClickyJobOutcome.Done
			});
			runs = readOnlyList;
		}
		else
		{
			IReadOnlyList<ClickyJobRun> readOnlyList = Array.Empty<ClickyJobRun>();
			runs = readOnlyList;
		}
		ClickyJob obj = new ClickyJob
		{
			Id = id,
			Title = ((text2.Length == 0) ? FallbackTitle(prompt) : text2),
			Prompt = text3,
			FolderPath = folderPath,
			CreatedAt = now,
			FinishedAt = (lastRunAt ?? now),
			Outcome = ClickyJobOutcome.Done,
			IsSeen = true,
			IsDismissed = true,
			CloudColorName = LeastUsedCloudColor(),
			Runs = runs
		};
		ClickyJob clickyJob = obj;
		lock (gate)
		{
			jobs.Add(clickyJob);
			SaveLocked();
		}
		RaiseChanged();
		return clickyJob;
	}

	public void RecordThreadId(string threadId, string jobId)
	{
		Update(jobId, delegate(ClickyJob job)
		{
			List<ClickyJobRun> list = job.Runs?.ToList();
			if (list != null && list.Count > 0)
			{
				if (list[list.Count - 1].ThreadId == null)
				{
					list[list.Count - 1] = list[list.Count - 1]with
					{
						ThreadId = threadId
					};
				}
			}
			IReadOnlyList<ClickyJobRun>? readOnlyList = list;
			ClickyJob obj = job with
			{
				ThreadId = threadId,
				Runs = readOnlyList ?? job.Runs
			};
			return obj;
		});
	}

	public void Retitle(string jobId, string newTitle)
	{
		string title = newTitle.Trim();
		if (title.Length != 0)
		{
			Update(jobId, (ClickyJob job) => job with
			{
				Title = ((title.Length > 80) ? title.Substring(0, 80) : title)
			});
		}
	}

	public void MarkWorking(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			FollowedUpOnLaterDay = ((job.ThreadId != null && job.CreatedAt.LocalDateTime.Date != DateTime.Now.Date) ? new bool?(true) : job.FollowedUpOnLaterDay),
			Outcome = ClickyJobOutcome.Working,
			FinishedAt = null,
			WaitingStep = null,
			ResultCard = null,
			IsSeen = false,
			IsDismissed = false,
			IsArchived = null
		});
	}

	public void MarkEnded(string jobId, ClickyJobOutcome outcome, string? summary, string? replyText, IReadOnlyList<string> artifactEntries)
	{
		Update(jobId, delegate(ClickyJob job)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			List<ClickyJobRun> list = job.Runs?.ToList();
			if (list != null && list.Count > 0)
			{
				if (!list[list.Count - 1].FinishedAt.HasValue)
				{
					int index = list.Count - 1;
					object? summary2;
					if (!string.IsNullOrEmpty(summary))
					{
						summary2 = ((summary.Length > 400) ? summary.Substring(0, 400) : summary);
					}
					else
					{
						summary2 = list[list.Count - 1].Summary;
					}
					ClickyJobRun obj = list[list.Count - 1] with
					{
						FinishedAt = now,
						Outcome = outcome,
						Summary = (string?)summary2
					};
					list[index] = obj;
				}
			}
			IReadOnlyList<ClickyJobRun>? readOnlyList = list;
			ClickyJob obj2 = job with
			{
				Outcome = outcome,
				FinishedAt = now,
				Summary = (string.IsNullOrEmpty(summary) ? job.Summary : summary),
				ReplyText = (string.IsNullOrEmpty(replyText) ? job.ReplyText : ((replyText.Length > 1200) ? replyText.Substring(0, 1200) : replyText)),
				ArtifactEntries = ((artifactEntries.Count > 0) ? artifactEntries : job.ArtifactEntries),
				LatestReplyArtifactEntries = artifactEntries,
				IsSeen = false,
				Runs = readOnlyList ?? job.Runs
			};
			return obj2;
		});
	}

	public void MarkSeen(string jobId)
	{
		ClickyJob clickyJob = Job(jobId);
		if ((object)clickyJob != null && clickyJob.IsFinished && !clickyJob.IsSeen)
		{
			Update(jobId, (ClickyJob job) => job with
			{
				IsSeen = true
			});
		}
	}

	public void KeepOnDock(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			IsDismissed = false,
			IsArchived = null
		});
	}

	public void Dismiss(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			IsDismissed = true
		});
	}

	public void Archive(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			IsDismissed = true,
			IsArchived = true
		});
	}

	public void Unarchive(string jobId)
	{
		Update(jobId, (ClickyJob job) => job with
		{
			IsDismissed = false,
			IsArchived = null
		});
	}

	public void RemoveAllForSignOut()
	{
		lock (gate)
		{
			jobs = new List<ClickyJob>();
		}
		AgentDefaults.Remove("clicky.jobs.v1");
		RaiseChanged();
	}

	private void Update(string jobId, Func<ClickyJob, ClickyJob> change)
	{
		lock (gate)
		{
			int num = jobs.FindIndex((ClickyJob j) => j.Id == jobId);
			if (num < 0)
			{
				return;
			}
			ClickyJob clickyJob = change(jobs[num]);
			if (JobsEqual(clickyJob, jobs[num]))
			{
				return;
			}
			jobs[num] = clickyJob;
			SaveLocked();
		}
		RaiseChanged();
	}

	private static bool JobsEqual(ClickyJob a, ClickyJob b)
	{
		return JsonSerializer.Serialize(a) == JsonSerializer.Serialize(b);
	}

	private void TrimKeptJobsLocked()
	{
		if (jobs.Count > 80)
		{
			ISet<string> withRoutines = JobIdsWithRoutines?.Invoke() ?? new HashSet<string>();
			HashSet<string> removable = (from j in (from j in jobs
					where j.IsDismissed && !withRoutines.Contains(j.Id)
					orderby j.CreatedAt
					select j).Take(jobs.Count - 80)
				select j.Id).ToHashSet();
			jobs.RemoveAll((ClickyJob j) => removable.Contains(j.Id));
		}
	}

	private void SaveLocked()
	{
		AgentDefaults.Set("clicky.jobs.v1", jobs);
		WorkFolderFiles.Refresh(jobs.ToList());
	}

	private void RaiseChanged()
	{
		try
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Warn("Jobs", "Changed handler threw: " + ex.Message);
		}
	}

	private static string FallbackTitle(string prompt)
	{
		string text = (prompt.Split(['\r', '\n']).FirstOrDefault((string l) => l.Length > 0) ?? prompt).Trim();
		if (text.Length <= 48)
		{
			if (text.Length != 0)
			{
				return text;
			}
			return "Job";
		}
		return text.Substring(0, 46).Trim() + "…";
	}
}

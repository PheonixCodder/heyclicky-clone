using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyWorkFolderFiles
{
	public const string StateFileName = "state.md";

	public const bool WritesStateFiles = false;

	public const string IndexFileName = "WORK.md";

	public static readonly string PreviousNoteRelativePath = Path.Combine(".clicky", "previous-note.md");

	public const string NoteFileName = "note.md";

	public const string RunsBlockStart = "<!-- clicky:runs -->";

	public const string RunsBlockEnd = "<!-- /clicky:runs -->";

	private readonly object gate = new object();

	private readonly Dictionary<string, string> lastIndexLinesByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> reportedTurnKeys = new HashSet<string>();

	private Task writes = Task.CompletedTask;

	private string? ownerStamp;

	public bool IsBackfillingReports { get; set; }

	public string? OwnerStamp
	{
		get
		{
			lock (gate)
			{
				return ownerStamp;
			}
		}
		set
		{
			lock (gate)
			{
				ownerStamp = value;
			}
		}
	}

	private void Enqueue(Action work, string what)
	{
		lock (gate)
		{
			writes = writes.ContinueWith(delegate
			{
				try
				{
					work();
				}
				catch (Exception ex)
				{
					Log.Warn("Jobs", what + ": " + ex.Message);
				}
			}, TaskScheduler.Default);
		}
	}

	public Task WhenWritten()
	{
		lock (gate)
		{
			return writes;
		}
	}

	public void Refresh(IReadOnlyList<ClickyJob> jobs, bool force = false)
	{
		foreach (IGrouping<string, ClickyJob> item in jobs.GroupBy<ClickyJob, string>((ClickyJob j) => Path.Combine(Path.GetDirectoryName(j.FolderPath.TrimEnd('\\', '/')) ?? j.FolderPath, "WORK.md"), StringComparer.OrdinalIgnoreCase))
		{
			string indexPath = item.Key;
			List<ClickyJob> list = item.ToList();
			IReadOnlyList<(DateTimeOffset Date, string Line)> listedLines = IndexLines(list);
			string text = string.Join("\n", listedLines.Select<(DateTimeOffset, string), string>(((DateTimeOffset Date, string Line) l) => l.Line));
			string stamp;
			lock (gate)
			{
				if (!force && lastIndexLinesByPath.TryGetValue(indexPath, out string value) && value == text)
				{
					continue;
				}
				lastIndexLinesByPath[indexPath] = text;
				stamp = ownerStamp;
				goto IL_00fd;
			}
			IL_00fd:
			List<string> listedFolders = list.Select((ClickyJob j) => j.FolderPath).ToList();
			Enqueue(delegate
			{
				string directoryName = Path.GetDirectoryName(indexPath);
				IEnumerable<(DateTimeOffset, string)> second = from entry in ClickyWorkIndex.UnlistedFolderEntries(directoryName, listedFolders, stamp)
					select (LastActive: entry.LastActive, IndexLine(entry));
				string text2 = IndexText(listedLines.Concat<(DateTimeOffset, string)>(second).ToList());
				if (!File.Exists(indexPath) || !(File.ReadAllText(indexPath) == text2))
				{
					Directory.CreateDirectory(directoryName);
					File.WriteAllText(indexPath, text2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				}
			}, "WORK.md not written");
		}
	}

	public bool ClaimReport(string jobId, string turnKey)
	{
		lock (gate)
		{
			return reportedTurnKeys.Add(jobId + "|" + turnKey);
		}
	}

	public void AppendReportSection(string section, string title, string jobId, string folderPath)
	{
		string reportPath = Path.Combine(folderPath, "report.md");
		string header = ClickyWorkIndex.ReportHeader(title, jobId);
		string stamp = OwnerStamp;
		Enqueue(delegate
		{
			string text = (File.Exists(reportPath) ? File.ReadAllText(reportPath) : "");
			string contents = ((text.Length == 0) ? (header + "\n" + section) : (text + (text.EndsWith('\n') ? "\n" : "\n\n") + section));
			Directory.CreateDirectory(folderPath);
			File.WriteAllText(reportPath, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			if (stamp != null)
			{
				ClickyWorkIndex.StampFolder(folderPath, stamp);
			}
		}, "report.md not written");
	}

	public void WriteReportIfMissing(string text, string folderPath)
	{
		string reportPath = Path.Combine(folderPath, "report.md");
		string stamp = OwnerStamp;
		Enqueue(delegate
		{
			if (stamp != null)
			{
				ClickyWorkIndex.StampFolder(folderPath, stamp);
			}
			if (!File.Exists(reportPath))
			{
				File.WriteAllText(reportPath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
		}, "report.md not written");
	}

	public void StampFolders(IReadOnlyList<string> folderPaths)
	{
		string stamp = OwnerStamp;
		if (stamp == null)
		{
			return;
		}
		Enqueue(delegate
		{
			foreach (string item in folderPaths.Where(Directory.Exists))
			{
				ClickyWorkIndex.StampFolder(item, stamp);
			}
		}, "job folders not stamped");
	}

	public void ResetForSignOut(string jobsRootPath)
	{
		lock (gate)
		{
			ownerStamp = null;
			lastIndexLinesByPath.Clear();
			reportedTurnKeys.Clear();
		}
		string indexPath = Path.Combine(jobsRootPath, "WORK.md");
		Enqueue(delegate
		{
			if (File.Exists(indexPath))
			{
				File.Delete(indexPath);
			}
		}, "WORK.md not removed");
	}

	public void RemoveReports(string? deletedAccountId, string jobsRootPath)
	{
		string stamp = ClickyWorkIndex.OwnerStamp(deletedAccountId);
		if (stamp != null)
		{
			Enqueue(delegate
			{
				ClickyWorkIndex.RemoveReports(stamp, jobsRootPath);
			}, "reports not removed");
		}
	}

	public static bool RetireNote(string folderPath)
	{
		string path = Path.Combine(folderPath, "note.md");
		string text;
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			text = File.ReadAllText(path);
		}
		catch (Exception)
		{
			return false;
		}
		try
		{
			File.Delete(path);
		}
		catch (Exception)
		{
		}
		if (text.Trim().Length == 0)
		{
			return false;
		}
		string path2 = Path.Combine(folderPath, PreviousNoteRelativePath);
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path2));
			File.WriteAllText(path2, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		catch (Exception ex3)
		{
			Log.Warn("Jobs", "previous-note.md not written: " + ex3.Message);
		}
		return true;
	}

	public static string FolderSlug(string title)
	{
		string text = new string((from c in title.Normalize(NormalizationForm.FormD)
			where CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark
			select c).ToArray()).ToLowerInvariant();
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		string text2 = text;
		foreach (char c2 in text2)
		{
			if (c2 < '\u0080' && char.IsLetterOrDigit(c2))
			{
				stringBuilder.Append(c2);
			}
			else if (stringBuilder.Length > 0)
			{
				list.Add(stringBuilder.ToString());
				stringBuilder.Clear();
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		string text3 = "";
		foreach (string item in list)
		{
			string text4 = ((text3.Length == 0) ? item : (text3 + "-" + item));
			if (text4.Length > 40)
			{
				break;
			}
			text3 = text4;
		}
		return text3;
	}

	private static string RunDate(DateTimeOffset date)
	{
		return date.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
	}

	private static string OneLine(string text, int limit)
	{
		string text2 = string.Join(" ", from l in text.Split(['\r', '\n'])
			where l.Length > 0
			select l).Trim();
		if (text2.Length > limit)
		{
			return text2.Substring(0, limit - 1).TrimEnd() + "…";
		}
		return text2;
	}

	private static string OutcomeText(ClickyJobOutcome outcome, string? summary)
	{
		string text = ((summary == null) ? "" : (": " + OneLine(summary, 220)));
		return outcome switch
		{
			ClickyJobOutcome.Working => "working now", 
			ClickyJobOutcome.Done => "done" + text, 
			ClickyJobOutcome.Failed => "failed" + text, 
			_ => "stopped by the user" + text, 
		};
	}

	public static string RunsBlock(ClickyJob job)
	{
		IEnumerable<string> second = (job.Runs ?? Array.Empty<ClickyJobRun>()).Select((ClickyJobRun run) => $"- {RunDate(run.StartedAt)} · asked: \"{OneLine(run.Ask, 240)}\" · " + OutcomeText(run.Outcome, run.Summary));
		return string.Join("\n", new string[2] { "<!-- clicky:runs -->", "## Runs (HeyClicky keeps this list, newest last)" }.Concat(second).Append("<!-- /clicky:runs -->"));
	}

	internal static IReadOnlyList<(DateTimeOffset Date, string Line)> IndexLines(IReadOnlyList<ClickyJob> jobs)
	{
		return jobs.Select(delegate(ClickyJob job)
		{
			string fileName = Path.GetFileName(job.FolderPath.TrimEnd('\\', '/'));
			DateTimeOffset dateTimeOffset = job.FinishedAt ?? job.Runs?.LastOrDefault()?.StartedAt ?? job.CreatedAt;
			string text = job.Runs?.LastOrDefault()?.Ask ?? job.Prompt;
			return (lastActive: dateTimeOffset, $"- {RunDate(dateTimeOffset)} · {OneLine(job.Title, 80)} · {fileName}/ · asked: \"{OneLine(text, 160)}\" · " + OutcomeText(job.Outcome, job.Summary));
		}).ToList();
	}

	internal static string IndexLine(ClickyWorkEntry entry)
	{
		string fileName = Path.GetFileName((entry.FolderPath ?? entry.Id).TrimEnd('\\', '/'));
		object obj;
		if (entry.Asks.Count <= 0)
		{
			obj = "";
		}
		else
		{
			IReadOnlyList<string> asks = entry.Asks;
			obj = " · asked: \"" + ClickyWorkIndex.OneLine(asks[asks.Count - 1], 160) + "\"";
		}
		string value = (string)obj;
		string result = entry.Result;
		string value2 = ((result != null) ? (": " + ClickyWorkIndex.OneLine(result, 220)) : "");
		return $"- {ClickyWorkIndex.DateText(entry.LastActive)} · {ClickyWorkIndex.OneLine(entry.Title, 80)} · {fileName}/{value} · {entry.State}{value2}";
	}

	internal static string IndexText(IReadOnlyList<(DateTimeOffset Date, string Line)> lines)
	{
		return "# Work HeyClicky has done for the user\n\nOne folder per piece of work, newest first. In each: report.md (what each run was asked and what it reported) and output/ (the files it made).\n\n" + string.Join("\n", from l in lines
			orderby l.Date descending
			select l.Line) + "\n";
	}
}

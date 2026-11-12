using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyLiveSteps
{
	public static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(90L);

	public static readonly TimeSpan LineHold = TimeSpan.FromSeconds(2.5);

	private readonly object gate = new object();

	private readonly Dictionary<string, ClickyInkWorkStep> steps = new Dictionary<string, ClickyInkWorkStep>();

	private readonly Dictionary<string, ClickyArcPlan> plans = new Dictionary<string, ClickyArcPlan>();

	private readonly Dictionary<string, ClickyArcPlan> lastPlans = new Dictionary<string, ClickyArcPlan>();

	private readonly Dictionary<string, ClickyLiveLine> lines = new Dictionary<string, ClickyLiveLine>();

	private readonly Dictionary<string, ClickyLiveLine> waitingLines = new Dictionary<string, ClickyLiveLine>();

	public event Action<string>? Changed;

	private void RaiseChanged(string key)
	{
		try
		{
			this.Changed?.Invoke(key);
		}
		catch (Exception ex)
		{
			Log.Warn("Jobs", "live steps handler threw: " + ex.Message);
		}
	}

	public ClickyInkWorkStep? Note(string rawActivity, string key, DateTimeOffset? at = null)
	{
		ClickyInkWorkStep clickyInkWorkStep = Step(rawActivity, at ?? DateTimeOffset.Now);
		if ((object)clickyInkWorkStep == null)
		{
			return null;
		}
		lock (gate)
		{
			if (steps.TryGetValue(key, out ClickyInkWorkStep value) && value.IsSame(clickyInkWorkStep))
			{
				return null;
			}
			steps[key] = clickyInkWorkStep;
		}
		Offer(ClickyLiveLine.FromStep(clickyInkWorkStep), key);
		RaiseChanged(key);
		return clickyInkWorkStep;
	}

	public void Clear(string key)
	{
		lock (gate)
		{
			steps.Remove(key);
			lines.Remove(key);
			waitingLines.Remove(key);
			plans.Remove(key);
		}
		RaiseChanged(key);
	}

	public void NoteBatch(ClickyProgramBatch batch, string key, DateTimeOffset? at = null)
	{
		DateTimeOffset at2 = at ?? DateTimeOffset.Now;
		ClickyLiveLine clickyLiveLine = ClickyLiveLine.FromBatch(batch, at2);
		if ((object)clickyLiveLine == null)
		{
			return;
		}
		ClickyInkWorkStep clickyInkWorkStep = batch.WorkStep(at2);
		if ((object)clickyInkWorkStep != null)
		{
			lock (gate)
			{
				if (!steps.TryGetValue(key, out ClickyInkWorkStep value) || !value.IsSame(clickyInkWorkStep))
				{
					steps[key] = clickyInkWorkStep;
				}
			}
		}
		Offer(clickyLiveLine, key);
	}

	public ClickyArcPlan? Plan(string key)
	{
		lock (gate)
		{
			return plans.GetValueOrDefault(key);
		}
	}

	public ClickyArcPlan? LastPlan(string key)
	{
		lock (gate)
		{
			return lastPlans.GetValueOrDefault(key);
		}
	}

	public void NotePlan(ClickyArcPlan plan, string key)
	{
		lock (gate)
		{
			if (plans.TryGetValue(key, out ClickyArcPlan value) && value.Equals(plan))
			{
				return;
			}
			plans[key] = plan;
			lastPlans[key] = plan;
		}
		RaiseChanged(key);
	}

	public void RememberLastPlan(ClickyArcPlan plan, string key)
	{
		lock (gate)
		{
			lastPlans[key] = plan;
		}
	}

	public ClickyLiveLine? Line(string key)
	{
		lock (gate)
		{
			return lines.GetValueOrDefault(key);
		}
	}

	public void NoteThinking(string heading, string key, DateTimeOffset? at = null)
	{
		string text = heading.Trim();
		if (text.Length != 0)
		{
			Offer(new ClickyLiveLine(ClickyLiveLine.LineKind.Thinking, null, text, at ?? DateTimeOffset.Now), key);
		}
	}

	private void Offer(ClickyLiveLine line, string key)
	{
		DateTimeOffset now = DateTimeOffset.Now;
		bool flag;
		TimeSpan timeSpan;
		lock (gate)
		{
			if (lines.TryGetValue(key, out ClickyLiveLine value) && value.Kind == line.Kind && value.StepKind == line.StepKind && value.Text == line.Text && value.Icons.SequenceEqual(line.Icons) && value.MoreIcons == line.MoreIcons)
			{
				return;
			}
			if ((object)value == null)
			{
				goto IL_0113;
			}
			string batchId = line.BatchId;
			if (batchId == null || !(value.BatchId == batchId))
			{
				goto IL_0113;
			}
			lines[key] = line with
			{
				ShownAt = value.ShownAt
			};
			flag = false;
			timeSpan = TimeSpan.Zero;
			goto end_IL_0025;
			IL_0113:
			if (!lines.TryGetValue(key, out value) || now - value.ShownAt >= LineHold)
			{
				lines[key] = line with
				{
					ShownAt = now
				};
				flag = false;
				timeSpan = TimeSpan.Zero;
			}
			else
			{
				flag = !waitingLines.ContainsKey(key);
				waitingLines[key] = line;
				if (!flag)
				{
					return;
				}
				timeSpan = LineHold - (now - value.ShownAt);
			}
			end_IL_0025:;
		}
		if (!flag)
		{
			RaiseChanged(key);
			return;
		}
		Task.Delay((timeSpan < TimeSpan.Zero) ? TimeSpan.Zero : timeSpan).ContinueWith(delegate
		{
			lock (gate)
			{
				if (!waitingLines.Remove(key, out ClickyLiveLine value2) || !lines.ContainsKey(key))
				{
					return;
				}
				lines[key] = value2 with
				{
					ShownAt = DateTimeOffset.Now
				};
			}
			RaiseChanged(key);
		}, TaskScheduler.Default);
	}

	public string? Current(string key, DateTimeOffset? now = null)
	{
		return CurrentStep(key, now)?.Present;
	}

	public ClickyInkWorkStep? CurrentStep(string key, DateTimeOffset? now = null)
	{
		lock (gate)
		{
			ClickyInkWorkStep value;
			return (steps.TryGetValue(key, out value) && (now ?? DateTimeOffset.Now) - value.At < FreshFor) ? value : null;
		}
	}

	public static string? Phrase(string activity)
	{
		return Step(activity, DateTimeOffset.Now)?.Present;
	}

	public static ClickyInkWorkStep? Step(string activity, DateTimeOffset at)
	{
		string trimmed = activity.Trim();
		string text = After("Search:");
		if (text != null)
		{
			if (text.Length == 0 || text == "the web")
			{
				return Make(ClickyInkWorkStep.StepKind.Search);
			}
			if (IsMath(text))
			{
				return Make(ClickyInkWorkStep.StepKind.Math);
			}
			(string, string) tuple = ReadableSearch(text);
			if (tuple.Item2.Length > 0)
			{
				return Make(ClickyInkWorkStep.StepKind.Search, "“" + Shortened(tuple.Item2, 34) + "”");
			}
			return Make(ClickyInkWorkStep.StepKind.Search, tuple.Item1);
		}
		string text2 = After("Reading:");
		if (text2 != null)
		{
			string obj2;
			if (!text2.StartsWith("www.", StringComparison.Ordinal))
			{
				obj2 = text2;
			}
			else
			{
				string text3 = text2;
				obj2 = text3.Substring(4, text3.Length - 4);
			}
			return Make(ClickyInkWorkStep.StepKind.Read, obj2);
		}
		if (After("Finding:") != null)
		{
			return Make(ClickyInkWorkStep.StepKind.Look);
		}
		string text4 = After("Editing:");
		if (text4 != null)
		{
			List<string> list = (from n in text4.Split(',')
				select n.Trim() into n
				where n.Length > 0
				select n).ToList();
			if (list.Count == 0 || list.Any((string n) => n.EndsWith("note.md", StringComparison.Ordinal)))
			{
				return Make(ClickyInkWorkStep.StepKind.Note);
			}
			return Make(ClickyInkWorkStep.StepKind.File, list[0]);
		}
		string text5 = After("Using:");
		if (text5 != null)
		{
			return ToolStep(text5.ToLowerInvariant(), Make);
		}
		if (trimmed.Contains("note.md", StringComparison.Ordinal))
		{
			return Make(ClickyInkWorkStep.StepKind.Note);
		}
		return null;
		string? After(string prefix)
		{
			if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
			{
				return null;
			}
			string text6 = trimmed;
			int length = prefix.Length;
			return text6.Substring(length, text6.Length - length).Trim();
		}
		ClickyInkWorkStep Make(ClickyInkWorkStep.StepKind kind, string? obj = null)
		{
			return new ClickyInkWorkStep(kind, obj, at);
		}
	}

	private static ClickyInkWorkStep? ToolStep(string tool, Func<ClickyInkWorkStep.StepKind, string?, ClickyInkWorkStep> make)
	{
		if (Has(new string[3] { "gmail", "outlook", "mail" }))
		{
			return make(ClickyInkWorkStep.StepKind.Apps, "your email");
		}
		if (Has(new string[1] { "calendar" }))
		{
			return make(ClickyInkWorkStep.StepKind.Apps, "your calendar");
		}
		if (Has(new string[9] { "screenshot", "window", "click", "type text", "launch app", "scroll", "hotkey", "press key", "desktop" }))
		{
			return make(ClickyInkWorkStep.StepKind.Mac, null);
		}
		if (Has(new string[10] { "composio", "multi execute", "tool schema", "drive", "docs", "sheet", "notion", "linear", "github", "slack" }))
		{
			return make(ClickyInkWorkStep.StepKind.Apps, null);
		}
		if (Has(new string[4] { "edit", "patch", "note", "write" }))
		{
			return make(ClickyInkWorkStep.StepKind.Note, null);
		}
		if (Has(new string[1] { "search" }))
		{
			return make(ClickyInkWorkStep.StepKind.Search, null);
		}
		return null;
		bool Has(string[] words)
		{
			return words.Any(tool.Contains);
		}
	}

	public static bool IsMath(string query)
	{
		string text = query.ToLowerInvariant();
		if (text.StartsWith("calculator", StringComparison.Ordinal) || text.StartsWith("calc:", StringComparison.Ordinal))
		{
			return true;
		}
		bool flag = query.Any(char.IsDigit);
		bool flag2 = query.IndexOfAny(new char[7] { '+', '*', '/', '×', '÷', '=', '^' }) >= 0;
		if (flag && flag2)
		{
			return query.All((char c) => "0123456789.,+-*/×÷x%=()^ $".Contains(c));
		}
		return false;
	}

	public static string Shortened(string words, int limit)
	{
		if (words.Length <= limit)
		{
			return words;
		}
		string text = words.Substring(0, limit);
		int num = text.LastIndexOf(' ');
		return ((num >= 0) ? text.Substring(0, num) : text).Trim() + "…";
	}

	public static (string? Site, string Words) ReadableSearch(string query)
	{
		string text = null;
		List<string> list = new List<string>();
		string[] array = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			string lowered = text2.ToLowerInvariant();
			if (lowered.StartsWith("site:", StringComparison.Ordinal))
			{
				string text3 = text2;
				string text4 = text3.Substring(5, text3.Length - 5);
				int num = text4.IndexOf('/');
				if (num >= 0)
				{
					text4 = text4.Substring(0, num);
				}
				if (text4.StartsWith("www.", StringComparison.Ordinal))
				{
					text3 = text4;
					text4 = text3.Substring(4, text3.Length - 4);
				}
				if (text == null && text4.Length > 0)
				{
					text = text4;
				}
			}
			else
			{
				if (new string[6] { "intitle:", "inurl:", "intext:", "filetype:", "after:", "before:" }.Any((string p) => lowered.StartsWith(p, StringComparison.Ordinal)))
				{
					continue;
				}
				bool flag;
				switch (text2)
				{
				case "OR":
				case "AND":
				case "|":
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (!flag && (!text2.StartsWith('-') || text2.Length <= 1))
				{
					string text5 = text2.Replace("\"", "");
					if (text5.Length > 0)
					{
						list.Add(text5);
					}
				}
			}
		}
		return (Site: text, Words: string.Join(" ", list));
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.Agents.Routines;

public sealed class RoutinesTool(RoutineScheduler routines, ClickyJobStore jobs, Func<string, string?> clickySlugForThread, Func<string, string?> clickyName, Func<string> projectsRoot)
{
	private static readonly string[] WeekdayNames = new string[7] { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };

	public string ContextNote()
	{
		List<ClickyRoutine> list = routines.Routines.OrderBy((ClickyRoutine r) => r.CreatedAt).ToList();
		if (list.Count == 0)
		{
			return "[Your routines] none yet.";
		}
		IEnumerable<string> values = list.Select(delegate(ClickyRoutine routine)
		{
			string jobId = routine.JobId;
			string value;
			if (jobId != null)
			{
				ClickyJob clickyJob = jobs.Job(jobId);
				value = "job \"" + (clickyJob?.Title ?? routine.Title) + "\"" + (((object)clickyJob != null) ? (" (job_id: " + clickyJob.Id + ")") : "");
			}
			else
			{
				value = "Clicky \"" + (clickyName(routine.ClickySlug) ?? routine.ClickySlug) + "\"";
			}
			string value2 = (routine.IsPaused ? "PAUSED" : ("next run " + RunTime(routine.NextRunAt)));
			string text = string.Join(' ', from l in routine.TaskPrompt.Split(['\r', '\n'])
				select l.Trim() into l
				where l.Length > 0
				select l);
			return $"- routine_id {routine.Id}: \"{routine.Title}\" — {routine.ScheduleLabel.ToLowerInvariant()}, {value2}, ran {routine.RunCount}×, runs as {value}. Each run: {Prefix(text, 400)}";
		});
		return "[Your routines] Work that repeats on a schedule (the routines tool changes them; this list is all of them):\n" + string.Join("\n", values);
	}

	public async Task<JsonObject> RunAsync(string argumentsJson)
	{
		JsonObject arguments;
		try
		{
			arguments = (JsonNode.Parse(argumentsJson, null) as JsonObject) ?? new JsonObject((JsonNodeOptions?)null);
		}
		catch (JsonException)
		{
			arguments = new JsonObject((JsonNodeOptions?)null);
		}
		string? Text(string key)
		{
			if (arguments[key] is JsonValue jsonValue3 && jsonValue3.TryGetValue<string>(out string value4))
			{
				string text7 = value4.Trim();
				if (text7 != null && text7.Length > 0)
				{
					return text7;
				}
			}
			return null;
		}
		int value;
		double value2;
		int? num = ((!(arguments["every_minutes"] is JsonValue jsonValue)) ? ((int?)null) : (jsonValue.TryGetValue<int>(out value) ? new int?(value) : (jsonValue.TryGetValue<double>(out value2) ? new int?((int)value2) : ((int?)null))));
		string text = Text("at");
		int? num2 = ((text != null) ? MinuteOfDay(text) : ((int?)null));
		if (Text("at") != null && !num2.HasValue)
		{
			return Error("at must be a time like \"09:00\".");
		}
		string value3;
		List<string> list = ((arguments["days"] is JsonArray source && source.All((JsonNode e) => e is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out value3))) ? source.Select((JsonNode e) => e.GetValue<string>()).ToList() : null);
		object obj = list;
		if (obj == null)
		{
			string text2 = Text("weekday");
			obj = ((text2 != null) ? new List<string>(1) { text2 } : null);
		}
		List<string> list2 = (List<string>)obj;
		List<int> list3 = list2?.Select(Weekday).OfType<int>().ToList();
		List<int> list4 = ((list3 != null && list3.Count > 0) ? list3 : null);
		bool flag = list != null && list.Count == 0;
		if (list2 != null && list2.Count > 0 && list4 == null)
		{
			return Error("days must be weekday names like \"tuesday\".");
		}
		string text3 = Text("action") ?? "";
		if (text3 == "create")
		{
			return await CreateAsync(Text("title"), Text("task"), num, num2, list4, Text("codexThreadId"), Text("first_run") != "scheduled").ConfigureAwait(continueOnCapturedContext: false);
		}
		ClickyRoutine clickyRoutine = RoutineNamed(Text("routine_id"));
		if (clickyRoutine == null)
		{
			return Error("No routine with that routine_id: use one from the [Your routines] note.");
		}
		switch (text3)
		{
		case "update":
		{
			string text4 = Text("title");
			string text5 = Text("task");
			string text6 = ((text5 != null) ? ClickyRoutine.TaskPromptStrippingLeadingCadence(text5) : null);
			if (text4 != null || text6 != null)
			{
				routines.UpdateDetails(clickyRoutine.Id, text4 ?? clickyRoutine.Title, text6 ?? clickyRoutine.TaskPrompt, "one_brain");
			}
			if (num.HasValue || num2.HasValue || list4 != null || flag)
			{
				object obj2;
				if (num.HasValue || flag)
				{
					obj2 = null;
				}
				else
				{
					obj2 = clickyRoutine.ScheduledWeekdays;
					if (obj2 == null)
					{
						int? scheduledWeekday = clickyRoutine.ScheduledWeekday;
						if (scheduledWeekday.HasValue)
						{
							int valueOrDefault = scheduledWeekday.GetValueOrDefault();
							obj2 = new List<int>(1) { valueOrDefault };
						}
						else
						{
							obj2 = null;
						}
					}
				}
				IReadOnlyList<int> readOnlyList = (IReadOnlyList<int>)obj2;
				RoutineScheduler routineScheduler = routines;
				string id = clickyRoutine.Id;
				int minutes = num ?? clickyRoutine.IntervalMinutes;
				int? minuteOfDay = num2 ?? clickyRoutine.ScheduledMinuteOfDay;
				IReadOnlyList<int> readOnlyList2 = list4;
				routineScheduler.SetSchedule(id, minutes, minuteOfDay, readOnlyList2 ?? readOnlyList, "one_brain");
			}
			return new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "updated",
				["routine"] = Summary(clickyRoutine.Id)
			};
		}
		case "pause":
			routines.SetPaused(clickyRoutine.Id, paused: true, "one_brain");
			return new JsonObject((JsonNodeOptions?)null) { ["status"] = "paused" };
		case "resume":
			routines.SetPaused(clickyRoutine.Id, paused: false, "one_brain");
			return new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "resumed",
				["routine"] = Summary(clickyRoutine.Id)
			};
		case "delete":
			routines.Delete(clickyRoutine.Id, "one_brain");
			return new JsonObject((JsonNodeOptions?)null) { ["status"] = "deleted" };
		case "run_now":
			return (await routines.RunNowAsync(clickyRoutine.Id).ConfigureAwait(continueOnCapturedContext: false)) ? new JsonObject((JsonNodeOptions?)null) { ["status"] = "started" } : new JsonObject((JsonNodeOptions?)null)
			{
				["status"] = "not_started",
				["message"] = "It couldn't start now (its job is busy, or the PC is offline); it will try again shortly."
			};
		default:
			return Error("Unknown action " + text3 + ".");
		}
	}

	private async Task<JsonObject> CreateAsync(string? title, string? task, int? everyMinutes, int? minuteOfDay, IReadOnlyList<int>? weekdays, string? threadId, bool runsNow)
	{
		if (task != null)
		{
			int? num = everyMinutes ?? ((weekdays != null) ? new int?(1440) : ((int?)null));
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault > 0)
				{
					string text = ClickyRoutine.TaskPromptStrippingLeadingCadence(task);
					string title2 = title ?? string.Join(' ', task.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(4));
					string text2 = null;
					string text3 = null;
					if (threadId != null)
					{
						ClickyJob clickyJob = jobs.JobForThread(threadId);
						if ((object)clickyJob != null)
						{
							text2 = clickyJob.Id;
						}
						else
						{
							string text4 = clickySlugForThread(threadId);
							if (text4 == null)
							{
								return Error("No job with that codexThreadId: leave it out to start a new job for the routine.");
							}
							text3 = text4;
						}
					}
					IReadOnlyList<ClickyRoutine> readOnlyList2;
					if (text2 == null)
					{
						if (text3 == null)
						{
							IReadOnlyList<ClickyRoutine> readOnlyList = Array.Empty<ClickyRoutine>();
							readOnlyList2 = readOnlyList;
						}
						else
						{
							readOnlyList2 = routines.ForClicky(text3);
						}
					}
					else
					{
						readOnlyList2 = routines.ForJob(text2);
					}
					IReadOnlyList<ClickyRoutine> source = readOnlyList2;
					ClickyRoutine routine = source.FirstOrDefault();
					if (routine != null)
					{
						routines.UpdateDetails(routine.Id, title2, text, "one_brain");
						routines.SetSchedule(routine.Id, valueOrDefault, minuteOfDay, weekdays, "one_brain");
						if (routine.IsPaused)
						{
							routines.SetPaused(routine.Id, paused: false, "one_brain");
						}
						if (runsNow)
						{
							await routines.RunNowAsync(routine.Id).ConfigureAwait(continueOnCapturedContext: false);
						}
						return new JsonObject((JsonNodeOptions?)null)
						{
							["status"] = "updated",
							["routine"] = Summary(routine.Id)
						};
					}
					if (text2 == null && text3 == null)
					{
						text2 = jobs.CreateJob(title2, text, projectsRoot(), startsWorking: false).Id;
					}
					ClickyRoutine created = routines.CreateRoutine(text3 ?? "", title2, task, valueOrDefault, runImmediately: false, text2);
					if (minuteOfDay.HasValue || weekdays != null)
					{
						routines.SetSchedule(created.Id, valueOrDefault, minuteOfDay, weekdays, "one_brain");
					}
					if (runsNow)
					{
						await routines.RunNowAsync(created.Id).ConfigureAwait(continueOnCapturedContext: false);
					}
					return new JsonObject((JsonNodeOptions?)null)
					{
						["status"] = "created",
						["routine"] = Summary(created.Id)
					};
				}
			}
		}
		return Error("create needs task and every_minutes (or days).");
	}

	private ClickyRoutine? RoutineNamed(string? reference)
	{
		if (reference == null)
		{
			return null;
		}
		IReadOnlyList<ClickyRoutine> list = routines.Routines;
		return list.FirstOrDefault((ClickyRoutine r) => r.Id == reference) ?? list.FirstOrDefault((ClickyRoutine r) => string.Equals(r.Title, reference, StringComparison.OrdinalIgnoreCase));
	}

	private string Summary(string routineId)
	{
		ClickyRoutine clickyRoutine = routines.Routines.FirstOrDefault((ClickyRoutine r) => r.Id == routineId);
		if (clickyRoutine == null)
		{
			return "";
		}
		return clickyRoutine.Title + ": " + clickyRoutine.ScheduleLabel.ToLowerInvariant() + (clickyRoutine.IsPaused ? ", paused" : (", next run " + RunTime(clickyRoutine.NextRunAt)));
	}

	private static JsonObject Error(string message)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["status"] = "error",
			["message"] = message
		};
	}

	private static string RunTime(DateTimeOffset date)
	{
		return date.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt", CultureInfo.GetCultureInfo("en-US"));
	}

	private static string Prefix(string text, int limit)
	{
		if (text.Length <= limit)
		{
			return text;
		}
		if (!char.IsHighSurrogate(text[limit - 1]))
		{
			return text.Substring(0, limit);
		}
		return text.Substring(0, limit - 1);
	}

	public static int? MinuteOfDay(string text)
	{
		string text2 = text.ToLowerInvariant().Replace(" ", "");
		bool flag = text2.EndsWith("pm", StringComparison.Ordinal);
		bool flag2 = text2.EndsWith("am", StringComparison.Ordinal);
		string text3;
		if (!(flag || flag2))
		{
			text3 = text2;
		}
		else
		{
			string text4 = text2;
			text3 = text4.Substring(0, text4.Length - 2);
		}
		string[] array = text3.Split(':', StringSplitOptions.RemoveEmptyEntries);
		int num = array.Length;
		bool flag3 = ((num < 1 || num > 2) ? true : false);
		if (flag3 || !TryWhole(array[0], out var value))
		{
			return null;
		}
		int value2 = 0;
		if (array.Length == 2 && !TryWhole(array[1], out value2))
		{
			return null;
		}
		if ((value2 < 0 || value2 >= 60) ? true : false)
		{
			return null;
		}
		if (flag || flag2)
		{
			if ((value < 1 || value > 12) ? true : false)
			{
				return null;
			}
			value = value % 12 + (flag ? 12 : 0);
		}
		if ((value < 0 || value >= 24) ? true : false)
		{
			return null;
		}
		return value * 60 + value2;
	}

	private static bool TryWhole(string text, out int value)
	{
		return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
	}

	public static int? Weekday(string text)
	{
		string lowered = text.ToLowerInvariant().Trim();
		if (lowered.Length < 3)
		{
			return null;
		}
		int num = Array.FindIndex(WeekdayNames, (string name) => name.StartsWith(lowered, StringComparison.Ordinal) || lowered.StartsWith(name, StringComparison.Ordinal));
		if (num >= 0)
		{
			return num + 1;
		}
		return null;
	}
}

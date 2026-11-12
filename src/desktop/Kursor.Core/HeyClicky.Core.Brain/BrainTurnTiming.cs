using System;
using System.Linq;
using System.Text.Json.Nodes;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Brain;

public static class BrainTurnTiming
{
	private sealed class Marks
	{
		public DateTimeOffset? ReleasedAt;

		public DateTimeOffset? TranscriptAt;

		public DateTimeOffset? RequestStartedAt;

		public DateTimeOffset? HeadersAt;

		public DateTimeOffset? FirstTokenAt;

		public int RequestBodyBytes;

		public bool RequestBodyWasGzip;

		public bool IsTyped;
	}

	private static readonly object gate = new object();

	private static Marks? current;

	private static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(120L);

	internal static Func<DateTimeOffset> Now = () => DateTimeOffset.UtcNow;

	internal static Action<string, JsonObject>? ReportedForTest;

	private const double PostHogSampleRate = 0.1;

	public static void NoteReleased()
	{
		lock (gate)
		{
			current = new Marks
			{
				ReleasedAt = Now()
			};
		}
	}

	public static void NoteTranscriptReady()
	{
		lock (gate)
		{
			if (current != null)
			{
				DateTimeOffset? transcriptAt = current.TranscriptAt;
				if (!transcriptAt.HasValue)
				{
					current.TranscriptAt = Now();
				}
			}
		}
	}

	public static void NoteRequestStarted(bool isTyped)
	{
		lock (gate)
		{
			if (current == null || IsStaleLocked())
			{
				current = new Marks
				{
					IsTyped = isTyped
				};
			}
			DateTimeOffset? requestStartedAt = current.RequestStartedAt;
			if (!requestStartedAt.HasValue)
			{
				current.RequestStartedAt = Now();
				current.IsTyped = isTyped;
			}
		}
	}

	public static void NoteRequestBody(int bytes, bool gzip)
	{
		lock (gate)
		{
			if (current != null && current.RequestBodyBytes == 0)
			{
				current.RequestBodyBytes = bytes;
				current.RequestBodyWasGzip = gzip;
			}
		}
	}

	public static void NoteResponseHeaders()
	{
		lock (gate)
		{
			if (current == null)
			{
				return;
			}
			DateTimeOffset? headersAt = current.HeadersAt;
			if (!headersAt.HasValue)
			{
				headersAt = current.RequestStartedAt;
				if (headersAt.HasValue)
				{
					current.HeadersAt = Now();
				}
			}
		}
	}

	public static void NoteFirstToken()
	{
		lock (gate)
		{
			if (current == null)
			{
				return;
			}
			DateTimeOffset? firstTokenAt = current.FirstTokenAt;
			if (!firstTokenAt.HasValue)
			{
				firstTokenAt = current.RequestStartedAt;
				if (firstTokenAt.HasValue)
				{
					current.FirstTokenAt = Now();
				}
			}
		}
	}

	public static void DiscardCurrent()
	{
		lock (gate)
		{
			current = null;
		}
	}

	public static void NoteFirstLineHeard(string? model)
	{
		Marks marks2;
		lock (gate)
		{
			Marks marks = current;
			if (marks == null)
			{
				goto IL_0037;
			}
			DateTimeOffset? requestStartedAt = marks.RequestStartedAt;
			if (!requestStartedAt.HasValue || IsStaleLocked())
			{
				goto IL_0037;
			}
			marks2 = current;
			current = null;
			goto end_IL_000a;
			IL_0037:
			current = null;
			return;
			end_IL_000a:;
		}
		DateTimeOffset value = Now();
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["typed"] = marks2.IsTyped,
			["body_kb"] = marks2.RequestBodyBytes / 1024,
			["body_gzip"] = marks2.RequestBodyWasGzip
		};
		if (model != null)
		{
			jsonObject["model"] = model;
		}
		(string, int?)[] array = new(string, int?)[7]
		{
			("release_to_transcript_ms", Milliseconds(marks2.ReleasedAt, marks2.TranscriptAt)),
			("transcript_to_request_ms", Milliseconds(marks2.TranscriptAt, marks2.RequestStartedAt)),
			("request_to_headers_ms", Milliseconds(marks2.RequestStartedAt, marks2.HeadersAt)),
			("headers_to_first_token_ms", Milliseconds(marks2.HeadersAt, marks2.FirstTokenAt)),
			("first_token_to_first_line_ms", Milliseconds(marks2.FirstTokenAt, value)),
			("request_to_first_line_ms", Milliseconds(marks2.RequestStartedAt, value)),
			("release_to_first_line_ms", Milliseconds(marks2.ReleasedAt, value))
		};
		(string, int?)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			var (propertyName, num) = array2[i];
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				jsonObject[propertyName] = valueOrDefault;
			}
		}
		string text = "⏱ [one brain] " + string.Join(" ", from l in array.Where(delegate((string Key, int? Value) l)
			{
				int? item = l.Value;
				return item.HasValue;
			})
			select $"{l.Item1.Replace("_ms", "")}={l.Item2}") + $" body={marks2.RequestBodyBytes / 1024}KB{(marks2.RequestBodyWasGzip ? " gzip" : "")}";
		Log.Info("OneBrain", text);
		Action<string, JsonObject> reportedForTest = ReportedForTest;
		if (reportedForTest != null)
		{
			reportedForTest(text, jsonObject);
		}
		else if (Random.Shared.NextDouble() < 0.1)
		{
			jsonObject["sample_rate"] = 0.1;
			ClickyAnalytics.Capture("one_brain_turn_timing", jsonObject);
		}
		static int? Milliseconds(DateTimeOffset? from, DateTimeOffset? to)
		{
			if (from.HasValue)
			{
				DateTimeOffset valueOrDefault2 = from.GetValueOrDefault();
				if (to.HasValue)
				{
					DateTimeOffset valueOrDefault3 = to.GetValueOrDefault();
					return Math.Max(0, (int)(valueOrDefault3 - valueOrDefault2).TotalMilliseconds);
				}
			}
			return null;
		}
	}

	private static bool IsStaleLocked()
	{
		DateTimeOffset? dateTimeOffset = current?.ReleasedAt ?? current?.RequestStartedAt;
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			return Now() - valueOrDefault > StaleAfter;
		}
		return false;
	}

	internal static void ResetForTest()
	{
		lock (gate)
		{
			current = null;
		}
	}
}

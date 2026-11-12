using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Proactive;

public sealed class UpcomingMeetingsClient
{
	private readonly WorkerClient worker;

	public Func<byte[]?>? TestResponse { get; set; }

	public UpcomingMeetingsClient(WorkerClient worker)
	{
		this.worker = worker;
	}

	public async Task<UpcomingCalendarFetch?> FetchAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			byte[] array = TestResponse?.Invoke();
			if (array == null)
			{
				array = await worker.SendRawAsync(HttpMethod.Get, "/me/calendar/upcoming", null, authenticated: true, ct, TimeSpan.FromSeconds(20L)).ConfigureAwait(continueOnCapturedContext: false);
			}
			byte[] array2 = array;
			return (array2 == null) ? null : Parse(array2);
		}
		catch (Exception ex)
		{
			Log.Info("Meetings", "upcoming-events fetch failed: " + ex.Message);
			return null;
		}
	}

	public static UpcomingCalendarFetch? Parse(byte[] bytes)
	{
		if (!(JsonNode.Parse(bytes, null) is JsonObject jsonObject))
		{
			return null;
		}
		List<UpcomingCalendarEvent> list = new List<UpcomingCalendarEvent>();
		bool value = default(bool);
		foreach (JsonObject item in ((jsonObject["events"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>())
		{
			JsonObject e = item;
			string text = Str("starts_at");
			if (text != null && DateTimeOffset.TryParse(text, out var result))
			{
				string text2 = Str("ends_at");
				DateTimeOffset result2;
				DateTimeOffset? endsAt = ((text2 != null && DateTimeOffset.TryParse(text2, out result2)) ? new DateTimeOffset?(result2) : ((DateTimeOffset?)null));
				string? id = Str("id") ?? text;
				string? title = Str("title") ?? "Meeting";
				DateTimeOffset startsAt = result;
				bool isAllDay = e["is_all_day"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value) && value;
				Uri? meetingUrl = AsUri(Str("meeting_url"));
				string text3 = Str("location");
				list.Add(new UpcomingCalendarEvent(id, title, startsAt, endsAt, isAllDay, meetingUrl, (text3 != null && text3.Length > 0) ? text3 : null, AsUri(Str("html_link"))));
			}
			string? Str(string k)
			{
				if (!(e[k] is JsonValue jsonValue3) || !jsonValue3.TryGetValue<string>(out string value3))
				{
					return null;
				}
				return value3;
			}
		}
		bool value2 = default(bool);
		return new UpcomingCalendarFetch(jsonObject["connected"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<bool>(out value2) && value2, list);
		static Uri? AsUri(string? s)
		{
			Uri result3;
			bool flag = Uri.TryCreate(s, UriKind.Absolute, out result3);
			if (flag)
			{
				string scheme = result3.Scheme;
				bool flag2 = ((scheme == "http" || scheme == "https") ? true : false);
				flag = flag2;
			}
			if (!flag)
			{
				return null;
			}
			return result3;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Telemetry;

public sealed class PostHogAnalyticsSink : IAnalyticsSink, IDisposable
{
	private const int FlushAt = 20;

	private const int MaxQueued = 1000;

	private readonly HttpClient http;

	private readonly Uri batchUrl;

	private readonly string apiKey;

	private readonly object gate = new object();

	private readonly List<AnalyticsEvent> queue = new List<AnalyticsEvent>();

	private readonly Timer timer;

	private readonly SemaphoreSlim sending = new SemaphoreSlim(1, 1);

	public string Name => "posthog";

	public PostHogAnalyticsSink(HttpClient http, string host, string apiKey)
	{
		this.http = http;
		this.apiKey = apiKey;
		batchUrl = new Uri(host.TrimEnd('/') + "/batch/");
		timer = new Timer(delegate(object? _)
		{
			_ = FlushAsync();
		}, null, TimeSpan.FromSeconds(30L), TimeSpan.FromSeconds(30L));
	}

	public void Enqueue(AnalyticsEvent analyticsEvent)
	{
		bool flag;
		lock (gate)
		{
			queue.Add(analyticsEvent);
			if (queue.Count > 1000)
			{
				queue.RemoveRange(0, queue.Count - 1000);
			}
			flag = queue.Count >= 20;
		}
		if (flag)
		{
			FlushAsync();
		}
	}

	public async Task FlushAsync()
	{
		if (!(await sending.WaitAsync(0).ConfigureAwait(continueOnCapturedContext: false)))
		{
			return;
		}
		try
		{
			AnalyticsEvent[] batch;
			lock (gate)
			{
				if (queue.Count == 0)
				{
					return;
				}
				batch = queue.ToArray();
				queue.Clear();
			}
			JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
			AnalyticsEvent[] array = batch;
			foreach (AnalyticsEvent analyticsEvent in array)
			{
				jsonArray.Add(analyticsEvent.ToPostHogJson());
			}
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
			{
				["api_key"] = apiKey,
				["batch"] = jsonArray
			};
			try
			{
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, batchUrl)
				{
					Content = new StringContent(jsonObject.ToJsonString(), Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
				};
				using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15L));
				using HttpResponseMessage httpResponseMessage = await http.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (!httpResponseMessage.IsSuccessStatusCode)
				{
					Log.Warn("analytics", $"PostHog batch of {batch.Length} answered HTTP {httpResponseMessage.StatusCode}");
					if (httpResponseMessage.StatusCode >= HttpStatusCode.InternalServerError)
					{
						Requeue(batch);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn("analytics", $"PostHog batch of {batch.Length} not sent: {ex.Message}");
				Requeue(batch);
			}
		}
		finally
		{
			sending.Release();
		}
	}

	private void Requeue(AnalyticsEvent[] batch)
	{
		lock (gate)
		{
			queue.InsertRange(0, batch);
			if (queue.Count > 1000)
			{
				queue.RemoveRange(1000, queue.Count - 1000);
			}
		}
	}

	public void Dispose()
	{
		timer.Dispose();
	}
}

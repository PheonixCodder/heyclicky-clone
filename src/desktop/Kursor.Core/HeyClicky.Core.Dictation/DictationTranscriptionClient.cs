using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Brain;
using HeyClicky.Core.Net;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Dictation;

public sealed class DictationTranscriptionClient
{
	private const string Area = "Dictation";

	private const int MaxAccumulatedSeconds = 360;

	private readonly ClickyHttp http;

	private readonly Func<bool, CancellationToken, Task<string?>> accessToken;

	private readonly string usageContext;

	private readonly string languageHint;

	private readonly int sampleRate;

	private readonly object gate = new object();

	private byte[] pcm = new byte[65536];

	private int pcmLength;

	private bool cancelled;

	public DictationCleanupContext Context { get; set; } = new DictationCleanupContext("", null, "", "", "", SingleLine: false);

	public IReadOnlyList<string> KnownTerms { get; set; } = Array.Empty<string>();

	public double AccumulatedSeconds
	{
		get
		{
			lock (gate)
			{
				return (double)pcmLength / 2.0 / (double)sampleRate;
			}
		}
	}

	public DictationTranscriptionClient(ClickyHttp http, Func<bool, CancellationToken, Task<string?>> accessToken, string usageContext, string languageHint, int sampleRate)
	{
		this.http = http;
		this.accessToken = accessToken;
		this.usageContext = usageContext;
		this.languageHint = languageHint;
		this.sampleRate = sampleRate;
	}

	public byte[] AccumulatedAudio()
	{
		lock (gate)
		{
			return pcm.AsSpan(0, pcmLength).ToArray();
		}
	}

	public void AppendAudio(ReadOnlySpan<byte> pcm16)
	{
		lock (gate)
		{
			if (!cancelled && pcmLength + pcm16.Length <= 360 * sampleRate * 2)
			{
				if (pcmLength + pcm16.Length > pcm.Length)
				{
					Array.Resize(ref pcm, Math.Max(pcm.Length * 2, pcmLength + pcm16.Length));
				}
				pcm16.CopyTo(pcm.AsSpan(pcmLength));
				pcmLength += pcm16.Length;
			}
		}
	}

	public void Cancel()
	{
		lock (gate)
		{
			cancelled = true;
			pcm = Array.Empty<byte>();
			pcmLength = 0;
		}
	}

	public async Task<(string Text, double AudioSeconds)> TranscribeAsync(CancellationToken ct = default(CancellationToken))
	{
		double seconds;
		byte[] inArray;
		lock (gate)
		{
			seconds = (double)pcmLength / 2.0 / (double)sampleRate;
			if (seconds < 0.25)
			{
				return (Text: "", AudioSeconds: 0.0);
			}
			inArray = Pcm16.WavFromPcm16Mono(pcm.AsSpan(0, pcmLength), sampleRate);
		}
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["audio_wav_base64"] = Convert.ToBase64String(inArray),
			["audio_seconds"] = seconds,
			["context"] = ContextPayload()
		};
		string text = languageHint.Trim();
		if (text.Length > 0 && text != "multi")
		{
			jsonObject["language"] = text;
		}
		return (Text: await PostExpectingTextAsync("/v2/dictation/transcribe", jsonObject, null, TimeSpan.FromSeconds(60L), ct).ConfigureAwait(continueOnCapturedContext: false), AudioSeconds: seconds);
	}

	public Task<string> CleanUpAsync(string transcript, DictationCleanupContext? focusContext, string? receipt = null, CancellationToken ct = default(CancellationToken))
	{
		if (focusContext != null)
		{
			Context = focusContext;
		}
		return CleanUpAsync(transcript, AccumulatedSeconds, receipt, useFastCleanupLane: true, null, "batch", ct);
	}

	public Task<string> CleanUpAsync(string transcript, double audioSeconds, string? receipt, bool useFastCleanupLane, int? sttMilliseconds, string? sttProvider = null, CancellationToken ct = default(CancellationToken))
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["transcript"] = transcript,
			["audio_seconds"] = audioSeconds,
			["context"] = ContextPayload()
		};
		if (useFastCleanupLane)
		{
			jsonObject["fast_cleanup"] = true;
		}
		if (sttMilliseconds.HasValue && sttMilliseconds.GetValueOrDefault() >= 0)
		{
			jsonObject["stt_ms"] = sttMilliseconds.Value;
		}
		if (!string.IsNullOrEmpty(sttProvider))
		{
			jsonObject["stt_provider"] = sttProvider;
		}
		return PostExpectingTextAsync("/v2/dictation/cleanup", jsonObject, receipt, TimeSpan.FromSeconds(20L), ct);
	}

	private JsonObject ContextPayload()
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null);
		if (Context.AppName.Length > 0)
		{
			jsonObject["app_name"] = Context.AppName;
		}
		if (Context.BeforeText.Length > 0)
		{
			jsonObject["before_text"] = Context.BeforeText;
		}
		if (Context.SelectedText.Length > 0)
		{
			jsonObject["selected_text"] = Context.SelectedText;
		}
		if (Context.AfterText.Length > 0)
		{
			jsonObject["after_text"] = Context.AfterText;
		}
		jsonObject["single_line"] = Context.SingleLine;
		if (!string.IsNullOrEmpty(Context.SiteHost))
		{
			jsonObject["site_host"] = Context.SiteHost;
		}
		if (KnownTerms.Count > 0)
		{
			JsonArray jsonArray = new JsonArray((JsonNodeOptions?)null);
			foreach (string item in KnownTerms.Take(50))
			{
				jsonArray.Add(item);
			}
			jsonObject["known_terms"] = jsonArray;
		}
		return jsonObject;
	}

	private async Task<string> PostExpectingTextAsync(string path, JsonObject body, string? receipt, TimeSpan timeout, CancellationToken ct)
	{
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(body, JsonNodeContext.Default.JsonObject);
		var (num, array) = await SendAsync(path, bytes, receipt, forceRefresh: false, timeout, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (num == 401)
		{
			Log.Info("Dictation", (receipt == null) ? (path + ": 401, retrying with a refreshed token") : (path + ": receipt rejected (expired?), retrying with the bearer"));
			(num, array) = await SendAsync(path, bytes, null, receipt == null, timeout, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		bool flag;
		switch (num)
		{
		case 402:
			BillingPaywallDetector.HandleIfPaywall(402, array);
			throw new DictationPaywallReachedException();
		default:
			flag = true;
			break;
		case 200:
		case 201:
		case 202:
		case 203:
		case 204:
		case 205:
		case 206:
		case 207:
		case 208:
		case 209:
		case 210:
		case 211:
		case 212:
		case 213:
		case 214:
		case 215:
		case 216:
		case 217:
		case 218:
		case 219:
		case 220:
		case 221:
		case 222:
		case 223:
		case 224:
		case 225:
		case 226:
		case 227:
		case 228:
		case 229:
		case 230:
		case 231:
		case 232:
		case 233:
		case 234:
		case 235:
		case 236:
		case 237:
		case 238:
		case 239:
		case 240:
		case 241:
		case 242:
		case 243:
		case 244:
		case 245:
		case 246:
		case 247:
		case 248:
		case 249:
		case 250:
		case 251:
		case 252:
		case 253:
		case 254:
		case 255:
		case 256:
		case 257:
		case 258:
		case 259:
		case 260:
		case 261:
		case 262:
		case 263:
		case 264:
		case 265:
		case 266:
		case 267:
		case 268:
		case 269:
		case 270:
		case 271:
		case 272:
		case 273:
		case 274:
		case 275:
		case 276:
		case 277:
		case 278:
		case 279:
		case 280:
		case 281:
		case 282:
		case 283:
		case 284:
		case 285:
		case 286:
		case 287:
		case 288:
		case 289:
		case 290:
		case 291:
		case 292:
		case 293:
		case 294:
		case 295:
		case 296:
		case 297:
		case 298:
		case 299:
			flag = false;
			break;
		}
		if (flag)
		{
			throw new WorkerHttpException(num, Encoding.UTF8.GetString(array), path);
		}
		if (!((JsonNode.Parse(array, null) as JsonObject)?["text"] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value))
		{
			throw new InvalidOperationException("Invalid dictation response.");
		}
		return value.Trim();
	}

	private async Task<(int Status, byte[] Body)> SendAsync(string path, byte[] body, string? receipt, bool forceRefresh, TimeSpan timeout, CancellationToken ct)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl(path));
		if (!string.IsNullOrEmpty(receipt))
		{
			request.Headers.TryAddWithoutValidation("X-Clicky-Dictation-Receipt", receipt);
		}
		else
		{
			string text = await accessToken(forceRefresh, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrEmpty(text))
			{
				throw new InvalidOperationException("Please sign in to use dictation.");
			}
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text);
		}
		request.Headers.TryAddWithoutValidation("X-Clicky-Usage-Context", usageContext);
		request.Content = new ByteArrayContent(body);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeoutSource.CancelAfter(timeout);
		using HttpResponseMessage response = await http.Client.SendAsync(request, timeoutSource.Token).ConfigureAwait(continueOnCapturedContext: false);
		byte[] item = await response.Content.ReadAsByteArrayAsync(timeoutSource.Token).ConfigureAwait(continueOnCapturedContext: false);
		return (Status: (int)response.StatusCode, Body: item);
	}
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Net;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Voice;

public sealed class GeminiTranscriber : IAsyncDisposable
{
	internal sealed record MintedSession(string Token, JsonElement Setup, string? Receipt = null);

	private readonly record struct AudioFrame(byte[]? Buffer, int Length, bool IsKeepAlive = false);

	internal static readonly Uri ListenUrl = new Uri("wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContentConstrained");

	public const string EarsTokenPath = "/brain/ears/token";

	public const string DictationTokenPath = "/v2/dictation/gemini-token";

	public const string SttProviderName = "gemini";

	internal const int StreamSampleRate = 16000;

	internal const int ChunkBytes = 3200;

	private static readonly byte[] ActivityStart = "{\"realtimeInput\":{\"activityStart\":{}}}"u8.ToArray();

	private static readonly byte[] ActivityEnd = "{\"realtimeInput\":{\"activityEnd\":{}}}"u8.ToArray();

	private readonly ClickyHttp http;

	private readonly string tokenPath;

	private readonly string area;

	private readonly Func<bool, CancellationToken, Task<string?>> accessToken;

	private readonly Func<CancellationToken, Task<MintedSession?>> mint;

	private readonly Func<string, CancellationToken, Task<WebSocket>> connect;

	private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

	private readonly Channel<AudioFrame> outgoing = Channel.CreateBounded<AudioFrame>(new BoundedChannelOptions(2048)
	{
		FullMode = BoundedChannelFullMode.Wait,
		SingleReader = true,
		SingleWriter = false
	});

	private readonly object gate = new object();

	private readonly List<string> finals = new List<string>();

	private readonly TaskCompletionSource<bool> setupReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly TaskCompletionSource<bool> opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly TaskCompletionSource<string?> delivered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly TaskCompletionSource<bool> activityEndSent = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private WebSocket? socket;

	private Task? runTask;

	private string interim = "";

	private long bufferedBytes;

	private long sentBytes;

	private bool warming;

	private bool finishRequested;

	private bool finishQueued;

	private bool finishSent;

	private bool disposed;

	private double loudestLevel;

	internal static readonly TimeSpan WarmKeepAliveInterval = TimeSpan.FromSeconds(20L);

	internal const double SilentPressLevel = 0.006;

	internal static readonly TimeSpan SilentPressWait = TimeSpan.FromSeconds(0.6);

	internal static readonly TimeSpan FinalTranscriptTimeout = TimeSpan.FromSeconds(2.8);

	private static readonly object AccountRefusalGate = new object();

	private static (string Reason, DateTimeOffset Until)? accountRefusal;

	internal static readonly TimeSpan AccountRefusalPause = TimeSpan.FromMinutes(10L);

	public string LanguageCode { get; }

	public IReadOnlyList<string> Keyterms { get; }

	public string UsageContextValue { get; }

	public int SampleRate { get; }

	public bool DidFail { get; private set; }

	public string? FailureMessage { get; private set; }

	public bool PaywallReached { get; private set; }

	public string? Receipt { get; private set; }

	public Task<bool> Opened => opened.Task;

	public double SentAudioSeconds => (double)Interlocked.Read(in sentBytes) / ((double)SampleRate * 2.0);

	public bool IsAlive
	{
		get
		{
			if (!DidFail && !disposed)
			{
				WebSocket? webSocket = socket;
				if (webSocket == null)
				{
					return false;
				}
				return webSocket.State == WebSocketState.Open;
			}
			return false;
		}
	}

	public GeminiTranscriber(ClickyHttp http, Func<bool, CancellationToken, Task<string?>> accessToken, string languageCode, IReadOnlyList<string> keyterms, string usageContext, int sampleRate, string tokenPath = "/brain/ears/token")
		: this(http, accessToken, languageCode, keyterms, usageContext, sampleRate, null, null, tokenPath)
	{
	}

	internal GeminiTranscriber(ClickyHttp http, Func<bool, CancellationToken, Task<string?>> accessToken, string languageCode, IReadOnlyList<string> keyterms, string usageContext, int sampleRate, Func<CancellationToken, Task<MintedSession?>>? mint, Func<string, CancellationToken, Task<WebSocket>>? connect, string tokenPath = "/brain/ears/token")
	{
		if ((sampleRate < 8000 || sampleRate > 48000) ? true : false)
		{
			throw new ArgumentOutOfRangeException("sampleRate");
		}
		this.http = http;
		this.tokenPath = tokenPath;
		area = ((tokenPath == "/v2/dictation/gemini-token") ? "Dictation" : "Ears");
		this.accessToken = accessToken;
		LanguageCode = languageCode;
		Keyterms = keyterms;
		UsageContextValue = usageContext;
		SampleRate = sampleRate;
		this.mint = mint ?? new Func<CancellationToken, Task<MintedSession>>(MintAsync);
		this.connect = connect ?? new Func<string, CancellationToken, Task<WebSocket>>(ConnectAsync);
	}

	public void Start(bool asWarmSocket = false)
	{
		lock (gate)
		{
			if (runTask == null && !disposed)
			{
				warming = asWarmSocket;
				runTask = Task.Run((Func<Task?>)RunAsync);
			}
		}
	}

	public bool Adopt()
	{
		lock (gate)
		{
			if (!warming || DidFail || disposed || finishRequested || delivered.Task.IsCompleted)
			{
				return false;
			}
			warming = false;
			return true;
		}
	}

	public void PushAudio(ReadOnlySpan<byte> pcm16)
	{
		lock (gate)
		{
			if (pcm16.IsEmpty || finishRequested || DidFail || disposed)
			{
				return;
			}
			if (pcm16.Length % 2 != 0 || Interlocked.Read(in bufferedBytes) + pcm16.Length > (long)SampleRate * 2L * 120)
			{
				Fail("Speech audio exceeded the supported format or duration.");
				return;
			}
			loudestLevel = Math.Max(loudestLevel, LoudestWindowLevel(pcm16, SampleRate));
			byte[] array = ArrayPool<byte>.Shared.Rent(pcm16.Length);
			pcm16.CopyTo(array);
			Interlocked.Add(ref bufferedBytes, pcm16.Length);
			if (!outgoing.Writer.TryWrite(new AudioFrame(array, pcm16.Length)))
			{
				Interlocked.Add(ref bufferedBytes, -pcm16.Length);
				ArrayPool<byte>.Shared.Return(array);
				Fail("Speech audio could not be queued.");
			}
		}
	}

	public string PartialTranscriptSoFar()
	{
		lock (gate)
		{
			return string.Join(" ", finals.Concat((interim.Length == 0) ? ((IEnumerable<string>)Array.Empty<string>()) : ((IEnumerable<string>)new string[1] { interim }))).Trim();
		}
	}

	public async Task<string?> FinishAsync(TimeSpan? timeout = null)
	{
		lock (gate)
		{
			finishRequested = true;
		}
		try
		{
			if (!(await opened.Task.WaitAsync(TimeSpan.FromSeconds(1.5)).ConfigureAwait(continueOnCapturedContext: false)))
			{
				return await delivered.Task.ConfigureAwait(continueOnCapturedContext: false);
			}
			double num;
			lock (gate)
			{
				if (!finishQueued && !finishSent && !DidFail && !disposed)
				{
					finishQueued = true;
					if (!outgoing.Writer.TryWrite(new AudioFrame(null, 0)))
					{
						Fail("Speech end could not be queued.");
					}
				}
				num = (double)Interlocked.Read(in bufferedBytes) / ((double)SampleRate * 2.0);
			}
			Task uploadBudget = Task.Delay(TimeSpan.FromSeconds(3.0 + num / 4.0));
			global::_003C_003Ey__InlineArray3<Task> buffer = default(global::_003C_003Ey__InlineArray3<Task>);
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<Task>, Task>(ref buffer, 0) = activityEndSent.Task;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<Task>, Task>(ref buffer, 1) = delivered.Task;
			global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<Task>, Task>(ref buffer, 2) = uploadBudget;
			Task task = await Task.WhenAny(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray3<Task>, Task>(in buffer, 3)).ConfigureAwait(continueOnCapturedContext: false);
			if (task == delivered.Task)
			{
				return await delivered.Task.ConfigureAwait(continueOnCapturedContext: false);
			}
			if (task == uploadBudget)
			{
				Fail("Speech audio upload stalled.");
				return await delivered.Task.ConfigureAwait(continueOnCapturedContext: false);
			}
			bool flag;
			lock (gate)
			{
				flag = finals.Count == 0 && interim.Length == 0 && loudestLevel < 0.006;
			}
			if (flag)
			{
				try
				{
					return await delivered.Task.WaitAsync(SilentPressWait).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (TimeoutException)
				{
					bool flag2;
					lock (gate)
					{
						flag2 = finals.Count > 0 || interim.Length > 0;
						if (!flag2)
						{
							delivered.TrySetResult("");
						}
					}
					if (!flag2)
					{
						return await delivered.Task.ConfigureAwait(continueOnCapturedContext: false);
					}
				}
			}
			return await delivered.Task.WaitAsync(timeout ?? FinalTranscriptTimeout).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (TimeoutException)
		{
			Fail("Speech final transcript timed out.");
			return null;
		}
	}

	internal static string? CurrentAccountRefusal(DateTimeOffset now)
	{
		lock (AccountRefusalGate)
		{
			(string, DateTimeOffset)? tuple = accountRefusal;
			object result;
			if (tuple.HasValue)
			{
				(string, DateTimeOffset) valueOrDefault = tuple.GetValueOrDefault();
				if (valueOrDefault.Item2 > now)
				{
					(result, _) = valueOrDefault;
					goto IL_003e;
				}
			}
			result = null;
			goto IL_003e;
			IL_003e:
			return (string?)result;
		}
	}

	internal static void ClearAccountRefusal()
	{
		lock (AccountRefusalGate)
		{
			accountRefusal = null;
		}
	}

	internal static bool IsAccountRefusal(string reason)
	{
		string lowered = reason.ToLowerInvariant();
		return new string[6] { "credit", "billing", "quota", "exhausted", "prepayment", "payment" }.Any((string word) => lowered.Contains(word, StringComparison.Ordinal));
	}

	private async Task RunAsync()
	{
		CancellationToken ct = lifetime.Token;
		try
		{
			string text = CurrentAccountRefusal(DateTimeOffset.UtcNow);
			if (text != null)
			{
				Fail("Gemini refused the speech session: " + text);
				return;
			}
			MintedSession session = await mint(ct).ConfigureAwait(continueOnCapturedContext: false);
			ct.ThrowIfCancellationRequested();
			if ((object)session != null)
			{
				if (session.Token != null && session.Token.StartsWith("gem_kursor_", StringComparison.Ordinal))
				{
					Fail("Local Groq Whisper batch transcription active.");
					return;
				}
				Receipt = session.Receipt;
				socket = await connect(session.Token, ct).ConfigureAwait(continueOnCapturedContext: false);
				await socket.SendAsync(BuildSetupMessage(session.Setup).AsMemory(), WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
				Task receiver = ReceiveLoopAsync(socket, ct);
				if (await setupReady.Task.WaitAsync(TimeSpan.FromSeconds(10L), ct).ConfigureAwait(continueOnCapturedContext: false))
				{
					opened.TrySetResult(result: true);
					Task sender = SendLoopAsync(socket, ct);
					KeepWarmSocketAliveAsync(ct);
					await receiver.ConfigureAwait(continueOnCapturedContext: false);
					lifetime.Cancel();
					await sender.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
		}
		catch (Exception)
		{
			Fail("The speech connection was interrupted.");
		}
		finally
		{
			opened.TrySetResult(result: false);
			delivered.TrySetResult(null);
		}
	}

	private async Task KeepWarmSocketAliveAsync(CancellationToken ct)
	{
		try
		{
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(WarmKeepAliveInterval, ct).ConfigureAwait(continueOnCapturedContext: false);
				lock (gate)
				{
					if (!warming || finishRequested || DidFail || disposed)
					{
						break;
					}
					outgoing.Writer.TryWrite(new AudioFrame(null, 0, IsKeepAlive: true));
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	internal static double LoudestWindowLevel(ReadOnlySpan<byte> pcm16, int sampleRate)
	{
		ReadOnlySpan<short> readOnlySpan = MemoryMarshal.Cast<byte, short>(pcm16);
		int num = Math.Max(1, sampleRate / 100);
		double num2 = 0.0;
		for (int i = 0; i < readOnlySpan.Length; i += num)
		{
			int num3 = Math.Min(i + num, readOnlySpan.Length);
			double num4 = 0.0;
			for (int j = i; j < num3; j++)
			{
				double num5 = (double)readOnlySpan[j] / 32768.0;
				num4 += num5 * num5;
			}
			num2 = Math.Max(num2, Math.Sqrt(num4 / (double)(num3 - i)));
		}
		return num2;
	}

	private async Task<MintedSession?> MintAsync(CancellationToken ct)
	{
		for (int attempt = 0; attempt < 2; attempt++)
		{
			string text = await accessToken(attempt > 0, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrEmpty(text))
			{
				Fail("Please sign in to talk to HeyClicky.");
				return null;
			}
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl(tokenPath));
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text);
			request.Headers.TryAddWithoutValidation("X-Clicky-Usage-Context", UsageContextValue);
			using MemoryStream body = new MemoryStream();
			using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(body))
			{
				utf8JsonWriter.WriteStartObject();
				utf8JsonWriter.WriteString("language", LanguageCode);
				utf8JsonWriter.WriteStartArray("keyterms");
				foreach (string keyterm in Keyterms)
				{
					utf8JsonWriter.WriteStringValue(keyterm);
				}
				utf8JsonWriter.WriteEndArray();
				utf8JsonWriter.WriteEndObject();
			}
			request.Content = new ByteArrayContent(body.ToArray());
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
			deadline.CancelAfter(TimeSpan.FromSeconds(10L));
			using HttpResponseMessage response = await http.Client.SendAsync(request, deadline.Token).ConfigureAwait(continueOnCapturedContext: false);
			byte[] array = await response.Content.ReadAsByteArrayAsync(deadline.Token).ConfigureAwait(continueOnCapturedContext: false);
			int statusCode = (int)response.StatusCode;
			if (statusCode == 401 && attempt == 0)
			{
				continue;
			}
			if (statusCode == 402)
			{
				bool presentsPaywall;
				lock (gate)
				{
					PaywallReached = true;
					presentsPaywall = !warming && !disposed;
				}
				BillingPaywallDetector.HandleIfPaywall(statusCode, array, presentsPaywall);
				Fail("Speech quota reached.");
				return null;
			}
			if (!response.IsSuccessStatusCode)
			{
				Fail($"Could not create the speech session (HTTP {statusCode}).");
				return null;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(array);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("access_token", out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()) || !rootElement.TryGetProperty("setup", out var value2) || value2.ValueKind != JsonValueKind.Object)
			{
				Fail("Invalid speech session response.");
				return null;
			}
			object obj;
			if (rootElement.TryGetProperty("receipt", out var value3) && value3.ValueKind == JsonValueKind.String)
			{
				string @string = value3.GetString();
				if (@string != null && @string.Length > 0)
				{
					obj = @string;
					goto IL_04cd;
				}
			}
			obj = null;
			goto IL_04cd;
			IL_04cd:
			string receipt = (string)obj;
			return new MintedSession(value.GetString(), value2.Clone(), receipt);
		}
		Fail("Speech authentication failed.");
		return null;
	}

	public static Uri BuildWebSocketUri(string accessToken)
	{
		if (!string.IsNullOrEmpty(accessToken) && accessToken.StartsWith("AIza", StringComparison.Ordinal))
		{
			return new Uri($"wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={Uri.EscapeDataString(accessToken)}");
		}
		return new Uri($"wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContentConstrained?access_token={Uri.EscapeDataString(accessToken ?? "")}");
	}

	private async Task<WebSocket> ConnectAsync(string token, CancellationToken ct)
	{
		ClientWebSocket ws = new ClientWebSocket();
		bool isDirectApiKey = !string.IsNullOrEmpty(token) && token.StartsWith("AIza", StringComparison.Ordinal);
		if (!isDirectApiKey)
		{
			ws.Options.SetRequestHeader("Authorization", "Token " + token);
		}
		ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(7L);
		ws.Options.KeepAliveTimeout = TimeSpan.FromSeconds(5L);
		try
		{
			using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
			deadline.CancelAfter(TimeSpan.FromSeconds(10L));
			Uri connectUri = isDirectApiKey ? BuildWebSocketUri(token) : ListenUrl;
			await ws.ConnectAsync(connectUri, http.Invoker, deadline.Token).ConfigureAwait(continueOnCapturedContext: false);
			return ws;
		}
		catch
		{
			ws.Dispose();
			throw;
		}
	}

	internal static byte[] BuildSetupMessage(JsonElement setup)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WritePropertyName("setup");
			setup.WriteTo(utf8JsonWriter);
			utf8JsonWriter.WriteEndObject();
		}
		return memoryStream.ToArray();
	}

	internal static byte[] BuildAudioMessage(ReadOnlySpan<byte> pcm16, int sampleRate)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WriteStartObject("realtimeInput");
			utf8JsonWriter.WriteStartObject("audio");
			utf8JsonWriter.WriteBase64String("data", pcm16);
			utf8JsonWriter.WriteString("mimeType", $"audio/pcm;rate={sampleRate}");
			utf8JsonWriter.WriteEndObject();
			utf8JsonWriter.WriteEndObject();
			utf8JsonWriter.WriteEndObject();
		}
		return memoryStream.ToArray();
	}

	private async Task SendLoopAsync(WebSocket ws, CancellationToken ct)
	{
		bool activityStarted = false;
		Pcm16MonoResampler resampler = new Pcm16MonoResampler(SampleRate, 16000);
		byte[] chunk = new byte[3200];
		int chunkLength = 0;
		try
		{
			await foreach (AudioFrame frame in outgoing.Reader.ReadAllAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
			{
				try
				{
					if (frame.IsKeepAlive)
					{
						if (!activityStarted)
						{
							await ws.SendAsync(BuildAudioMessage(new byte[3200], 16000).AsMemory(), WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
						}
						continue;
					}
					if (!activityStarted)
					{
						await ws.SendAsync(ActivityStart.AsMemory(), WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
						activityStarted = true;
					}
					if (frame.Buffer == null)
					{
						await SendResampledAsync(resampler.Drain()).ConfigureAwait(continueOnCapturedContext: false);
						if (chunkLength > 0)
						{
							await ws.SendAsync(BuildAudioMessage(chunk.AsSpan(0, chunkLength), 16000).AsMemory(), WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
						}
						chunkLength = 0;
						lock (gate)
						{
							finishSent = true;
						}
						await ws.SendAsync(ActivityEnd.AsMemory(), WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
						activityEndSent.TrySetResult(result: true);
					}
					else
					{
						await SendResampledAsync(resampler.Process(frame.Buffer.AsSpan(0, frame.Length))).ConfigureAwait(continueOnCapturedContext: false);
						Interlocked.Add(ref sentBytes, frame.Length);
					}
				}
				finally
				{
					if (frame.Buffer != null)
					{
						Interlocked.Add(ref bufferedBytes, -frame.Length);
						ArrayPool<byte>.Shared.Return(frame.Buffer);
					}
				}
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
		}
		catch (Exception)
		{
			Fail("Could not send speech audio.");
		}
		async Task SendResampledAsync(byte[] pcm16)
		{
			int offset = 0;
			while (offset < pcm16.Length)
			{
				int num = Math.Min(3200 - chunkLength, pcm16.Length - offset);
				pcm16.AsSpan(offset, num).CopyTo(chunk.AsSpan(chunkLength));
				chunkLength += num;
				offset += num;
				if (chunkLength < 3200)
				{
					break;
				}
				await ws.SendAsync(BuildAudioMessage(chunk, 16000).AsMemory(), WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
				chunkLength = 0;
			}
		}
	}

	private async Task ReceiveLoopAsync(WebSocket ws, CancellationToken ct)
	{
		byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
		int length = 0;
		try
		{
			while (!delivered.Task.IsCompleted && !ct.IsCancellationRequested)
			{
				ValueWebSocketReceiveResult valueWebSocketReceiveResult = await ws.ReceiveAsync(buffer.AsMemory(length), ct).ConfigureAwait(continueOnCapturedContext: false);
				if (valueWebSocketReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					FailClosedBy(ws, "Speech closed before a final transcript.");
					break;
				}
				length += valueWebSocketReceiveResult.Count;
				if (!valueWebSocketReceiveResult.EndOfMessage)
				{
					if (length == buffer.Length)
					{
						if (buffer.Length >= 1048576)
						{
							Fail("Speech response exceeded the supported size.");
							break;
						}
						byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
						buffer.AsSpan(0, length).CopyTo(array);
						ArrayPool<byte>.Shared.Return(buffer);
						buffer = array;
					}
				}
				else
				{
					HandleMessage(buffer.AsSpan(0, length));
					length = 0;
				}
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
		}
		catch (Exception)
		{
			FailClosedBy(ws, "Invalid or interrupted speech response.");
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
			setupReady.TrySetResult(result: false);
		}
	}

	private void FailClosedBy(WebSocket ws, string withoutReason)
	{
		string text = ws.CloseStatusDescription?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			WebSocketCloseStatus? closeStatus = ws.CloseStatus;
			if (closeStatus.HasValue)
			{
				WebSocketCloseStatus valueOrDefault = closeStatus.GetValueOrDefault();
				if (IsAccountRefusal(text))
				{
					lock (AccountRefusalGate)
					{
						accountRefusal = (text, DateTimeOffset.UtcNow + AccountRefusalPause);
					}
					Log.Warn(area, "Gemini refused for its account; using the fallback for 10 minutes: " + text);
				}
				Fail($"Gemini closed the speech connection ({valueOrDefault}): {text}");
				return;
			}
		}
		Fail(withoutReason);
	}

	internal void HandleMessage(ReadOnlySpan<byte> bytes)
	{
		using JsonDocument jsonDocument = JsonDocument.Parse(bytes.ToArray());
		JsonElement rootElement = jsonDocument.RootElement;
		lock (gate)
		{
			if (disposed || delivered.Task.IsCompleted)
			{
				return;
			}
			if (rootElement.TryGetProperty("error", out var value) || rootElement.TryGetProperty("goAway", out value))
			{
				Fail("The speech connection was interrupted.");
				return;
			}
			if (rootElement.TryGetProperty("setupComplete", out value))
			{
				setupReady.TrySetResult(result: true);
			}
			if (!rootElement.TryGetProperty("serverContent", out var value2))
			{
				return;
			}
			if (value2.TryGetProperty("interimInputTranscription", out var value3) && value3.TryGetProperty("text", out var value4))
			{
				interim = value4.GetString() ?? "";
			}
			if (value2.TryGetProperty("inputTranscription", out var value5) && value5.TryGetProperty("text", out var value6))
			{
				string @string = value6.GetString();
				if (@string != null && @string.Length > 0)
				{
					finals.Add(@string);
				}
				interim = "";
				if (finishSent)
				{
					delivered.TrySetResult(string.Join(" ", finals).Trim());
				}
			}
		}
	}

	private void Fail(string message)
	{
		lock (gate)
		{
			if (!delivered.Task.IsCompleted && !disposed)
			{
				DidFail = true;
				FailureMessage = message;
				ReportFailureLocked(message);
				opened.TrySetResult(result: false);
				setupReady.TrySetResult(result: false);
				delivered.TrySetResult(null);
				lifetime.Cancel();
				if (!warming && !PaywallReached)
				{
					Log.Warn(area, message);
				}
			}
		}
	}

	private void ReportFailureLocked(string message)
	{
		if (!PaywallReached && !message.StartsWith("Please sign in", StringComparison.Ordinal) && !(message == "Speech quota reached."))
		{
			string value = ((tokenPath == "/v2/dictation/gemini-token") ? "dictation" : "ears");
			bool flag = setupReady.Task.IsCompletedSuccessfully && setupReady.Task.Result;
			string value2 = (finishRequested ? "finishing" : ((!warming) ? (flag ? "press" : "press_connecting") : (flag ? "warm_idle" : "warm_connecting")));
			WebSocketCloseStatus? webSocketCloseStatus = socket?.CloseStatus;
			int num;
			if (webSocketCloseStatus.HasValue)
			{
				WebSocketCloseStatus valueOrDefault = webSocketCloseStatus.GetValueOrDefault();
				num = (int)valueOrDefault;
			}
			else
			{
				num = 0;
			}
			int value3 = num;
			string text = socket?.CloseStatusDescription?.Trim() ?? "";
			if (text.Length > 200)
			{
				text = text.Substring(0, 200);
			}
			ClickyAnalytics.ReportInternalError("gemini_speech", "connection_failed", null, $"purpose={value} phase={value2} close={value3} reason={text} error={message}", CrashReportLevel.Warning, $"{value}|{value2}|close_{value3}");
		}
	}

	public async ValueTask DisposeAsync()
	{
		lock (gate)
		{
			if (disposed)
			{
				return;
			}
			disposed = true;
			outgoing.Writer.TryComplete();
			delivered.TrySetResult(null);
			opened.TrySetResult(result: false);
			lifetime.Cancel();
			socket?.Abort();
		}
		if (runTask != null)
		{
			await runTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		socket?.Dispose();
		AudioFrame item;
		while (outgoing.Reader.TryRead(out item))
		{
			if (item.Buffer != null)
			{
				ArrayPool<byte>.Shared.Return(item.Buffer);
			}
		}
		lifetime.Dispose();
	}
}

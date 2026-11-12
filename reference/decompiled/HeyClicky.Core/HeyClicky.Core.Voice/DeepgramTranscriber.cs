using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Net;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Voice;

public sealed class DeepgramTranscriber : IAsyncDisposable
{
	private readonly struct OutgoingFrame
	{
		public byte[] Buffer { get; }

		public int Length { get; }

		public bool IsText { get; }

		public bool Pooled { get; }

		public OutgoingFrame(byte[] buffer, int length, bool isText, bool pooled = true)
		{
			Buffer = buffer;
			Length = length;
			IsText = isText;
			Pooled = pooled;
		}
	}

	private const string Area = "Ears";

	private const int MaximumBufferedChunks = 2048;

	private static readonly byte[] KeepAliveMessage = "{\"type\":\"KeepAlive\"}"u8.ToArray();

	private static readonly byte[] CloseStreamMessage = "{\"type\":\"CloseStream\"}"u8.ToArray();

	private readonly ClickyHttp http;

	private readonly Func<bool, CancellationToken, Task<string?>> accessToken;

	private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

	private readonly Channel<OutgoingFrame> outgoing = Channel.CreateBounded<OutgoingFrame>(new BoundedChannelOptions(2048)
	{
		FullMode = BoundedChannelFullMode.DropOldest,
		SingleReader = true,
		SingleWriter = false
	});

	private readonly List<string> finalSegments = new List<string>();

	private readonly object gate = new object();

	private readonly TaskCompletionSource<string?> delivered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

	private readonly TaskCompletionSource<bool> opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

	private ClientWebSocket? socket;

	private string latestInterim = "";

	private long sentPcmBytes;

	private bool isWarmUnadopted;

	private bool closeStreamSent;

	private volatile bool failed;

	private Exception? failure;

	public string LanguageCode { get; }

	public IReadOnlyList<string> Keyterms { get; }

	public string UsageContextValue { get; }

	public int SampleRate { get; }

	public string? Purpose { get; }

	public string? Receipt { get; private set; }

	public bool DidFail => failed;

	public Exception? Failure => failure;

	public bool PaywallReached { get; private set; }

	public double SentAudioSeconds => (double)Interlocked.Read(in sentPcmBytes) / 2.0 / (double)SampleRate;

	public Task<bool> Opened => opened.Task;

	public bool IsAlive
	{
		get
		{
			ClientWebSocket? clientWebSocket = socket;
			if (clientWebSocket != null && clientWebSocket.State == WebSocketState.Open)
			{
				return !failed;
			}
			return false;
		}
	}

	public long OpenedAtTicks { get; private set; }

	public DeepgramTranscriber(ClickyHttp http, Func<bool, CancellationToken, Task<string?>> accessToken, string languageCode, IReadOnlyList<string> keyterms, string usageContext, int sampleRate, string? purpose = "one_brain")
	{
		Purpose = purpose;
		this.http = http;
		this.accessToken = accessToken;
		LanguageCode = languageCode;
		Keyterms = keyterms;
		UsageContextValue = usageContext;
		SampleRate = sampleRate;
	}

	public void Start(bool asWarmSocket = false)
	{
		isWarmUnadopted = asWarmSocket;
		CancellationToken ct;
		try
		{
			ct = lifetime.Token;
		}
		catch (ObjectDisposedException)
		{
			return;
		}
		Task.Run(() => RunAsync(ct));
	}

	public bool Adopt()
	{
		if (!failed && !PaywallReached)
		{
			if (opened.Task.IsCompleted)
			{
				ClientWebSocket? clientWebSocket = socket;
				if (clientWebSocket == null || clientWebSocket.State != WebSocketState.Open)
				{
					goto IL_003e;
				}
			}
			isWarmUnadopted = false;
			return true;
		}
		goto IL_003e;
		IL_003e:
		return false;
	}

	public void PushAudio(ReadOnlySpan<byte> pcm16)
	{
		if (!pcm16.IsEmpty && !closeStreamSent && !failed)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(pcm16.Length);
			pcm16.CopyTo(array);
			if (!outgoing.Writer.TryWrite(new OutgoingFrame(array, pcm16.Length, isText: false)))
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	public string PartialTranscriptSoFar()
	{
		lock (gate)
		{
			string text = string.Join(" ", finalSegments);
			return string.IsNullOrEmpty(latestInterim) ? text : ((text.Length == 0) ? latestInterim : (text + " " + latestInterim));
		}
	}

	public async Task<string?> FinishAsync(TimeSpan? timeout = null)
	{
		if (!opened.Task.IsCompleted && await Task.WhenAny(opened.Task, Task.Delay(1500)).ConfigureAwait(continueOnCapturedContext: false) != opened.Task)
		{
			MarkFailed(new TimeoutException("the Deepgram socket did not open in time"));
		}
		if (!failed && !closeStreamSent)
		{
			closeStreamSent = true;
			outgoing.Writer.TryWrite(new OutgoingFrame(CloseStreamMessage, CloseStreamMessage.Length, isText: true, pooled: false));
		}
		if (await Task.WhenAny(delivered.Task, Task.Delay(timeout ?? TimeSpan.FromSeconds(3.5))).ConfigureAwait(continueOnCapturedContext: false) != delivered.Task)
		{
			string text = PartialTranscriptSoFar().Trim();
			delivered.TrySetResult((text.Length > 0) ? text : null);
		}
		return await delivered.Task.ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task RunAsync(CancellationToken ct)
	{
		if (ct.IsCancellationRequested)
		{
			return;
		}
		try
		{
			(string, string)? tuple = await MintAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (tuple.HasValue)
			{
				Uri uri = BuildListenUrl(tuple.Value.Item2, SampleRate, LanguageCode, Keyterms);
				ClientWebSocket ws = new ClientWebSocket();
				ws.Options.SetRequestHeader("Authorization", "Bearer " + tuple.Value.Item1);
				ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20L);
				socket = ws;
				using (CancellationTokenSource connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
				{
					connectTimeout.CancelAfter(TimeSpan.FromSeconds(10L));
					await ws.ConnectAsync(uri, http.Invoker, connectTimeout.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				OpenedAtTicks = Environment.TickCount64;
				opened.TrySetResult(result: true);
				Task sender = SendLoopAsync(ws, ct);
				KeepAliveLoopAsync(ct);
				await ReceiveLoopAsync(ws, ct).ConfigureAwait(continueOnCapturedContext: false);
				await Task.WhenAny(sender, Task.Delay(100)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			delivered.TrySetResult(null);
		}
		catch (Exception ex2)
		{
			MarkFailed(ex2);
		}
		finally
		{
			opened.TrySetResult(result: false);
		}
	}

	private async Task<(string Token, string Model)?> MintAsync(CancellationToken ct)
	{
		for (int attempt = 0; attempt < 2; attempt++)
		{
			string text = await accessToken(attempt > 0, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (text == null)
			{
				MarkFailed(new InvalidOperationException("not signed in"));
				return null;
			}
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl((Purpose == null) ? "/v2/dictation/deepgram-token" : ("/v2/dictation/deepgram-token?purpose=" + Uri.EscapeDataString(Purpose))));
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text);
			request.Headers.TryAddWithoutValidation("X-Clicky-Usage-Context", UsageContextValue);
			request.Content = new ByteArrayContent(Array.Empty<byte>());
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(TimeSpan.FromSeconds(10L));
			using HttpResponseMessage response = await http.Client.SendAsync(request, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			byte[] array = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			int statusCode = (int)response.StatusCode;
			if (statusCode == 401 && attempt == 0)
			{
				continue;
			}
			switch (statusCode)
			{
			case 402:
				PaywallReached = true;
				BillingPaywallDetector.HandleIfPaywall(statusCode, array, !isWarmUnadopted);
				MarkFailed(new InvalidOperationException("paywall"), isPaywall: true);
				return null;
			default:
				MarkFailed(new WorkerHttpException(statusCode, Encoding.UTF8.GetString(array), "/v2/dictation/deepgram-token"));
				return null;
			case 200:
			{
				DeepgramTokenResponse deepgramTokenResponse = JsonSerializer.Deserialize(array, CoreJsonContext.Default.DeepgramTokenResponse);
				if (string.IsNullOrEmpty(deepgramTokenResponse?.AccessToken))
				{
					MarkFailed(new InvalidOperationException("the Deepgram token mint returned no access_token"));
					return null;
				}
				Receipt = (string.IsNullOrEmpty(deepgramTokenResponse.Receipt) ? null : deepgramTokenResponse.Receipt);
				return (deepgramTokenResponse.AccessToken, string.IsNullOrEmpty(deepgramTokenResponse.Model) ? "nova-3" : deepgramTokenResponse.Model);
			}
			}
		}
		return null;
	}

	public static Uri BuildListenUrl(string model, int sampleRate, string languageCode, IReadOnlyList<string> keyterms)
	{
		StringBuilder stringBuilder = new StringBuilder("wss://api.deepgram.com/v1/listen?");
		stringBuilder.Append("model=").Append(Uri.EscapeDataString(model));
		stringBuilder.Append("&encoding=linear16");
		stringBuilder.Append("&sample_rate=").Append(sampleRate);
		stringBuilder.Append("&channels=1");
		stringBuilder.Append("&smart_format=true");
		stringBuilder.Append("&interim_results=true");
		if (!string.IsNullOrEmpty(languageCode))
		{
			stringBuilder.Append("&language=").Append(Uri.EscapeDataString(languageCode));
		}
		if (string.IsNullOrEmpty(languageCode) || languageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("&filler_words=true");
		}
		foreach (string keyterm in keyterms)
		{
			stringBuilder.Append("&keyterm=").Append(Uri.EscapeDataString(keyterm));
		}
		return new Uri(stringBuilder.ToString());
	}

	private async Task SendLoopAsync(ClientWebSocket ws, CancellationToken ct)
	{
		_ = 2;
		try
		{
			await foreach (OutgoingFrame frame in outgoing.Reader.ReadAllAsync(ct).ConfigureAwait(continueOnCapturedContext: false))
			{
				try
				{
					if (ws.State == WebSocketState.Open)
					{
						await ws.SendAsync(frame.Buffer.AsMemory(0, frame.Length), (!frame.IsText) ? WebSocketMessageType.Binary : WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(continueOnCapturedContext: false);
						if (!frame.IsText)
						{
							Interlocked.Add(ref sentPcmBytes, frame.Length);
						}
					}
				}
				finally
				{
					if (frame.Pooled)
					{
						ArrayPool<byte>.Shared.Return(frame.Buffer);
					}
				}
			}
		}
		catch (Exception ex) when (((ex is OperationCanceledException || ex is WebSocketException || ex is ObjectDisposedException) ? 1 : 0) != 0)
		{
			if (!ct.IsCancellationRequested && !delivered.Task.IsCompleted)
			{
				MarkFailed(ex);
			}
		}
	}

	private async Task KeepAliveLoopAsync(CancellationToken ct)
	{
		try
		{
			while (!ct.IsCancellationRequested)
			{
				await Task.Delay(TimeSpan.FromSeconds(7L), ct).ConfigureAwait(continueOnCapturedContext: false);
				if (!isWarmUnadopted)
				{
					break;
				}
				outgoing.Writer.TryWrite(new OutgoingFrame(KeepAliveMessage, KeepAliveMessage.Length, isText: true, pooled: false));
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
	{
		byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);
		int length = 0;
		try
		{
			while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
			{
				ValueWebSocketReceiveResult valueWebSocketReceiveResult = await ws.ReceiveAsync(buffer.AsMemory(length), ct).ConfigureAwait(continueOnCapturedContext: false);
				if (valueWebSocketReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					delivered.TrySetResult(FinalTranscript());
					break;
				}
				length += valueWebSocketReceiveResult.Count;
				if (!valueWebSocketReceiveResult.EndOfMessage)
				{
					if (length == buffer.Length)
					{
						byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
						buffer.AsSpan(0, length).CopyTo(array);
						ArrayPool<byte>.Shared.Return(buffer);
						buffer = array;
					}
				}
				else
				{
					if (valueWebSocketReceiveResult.MessageType == WebSocketMessageType.Text)
					{
						HandleMessage(buffer.AsSpan(0, length));
					}
					length = 0;
				}
			}
		}
		catch (Exception ex) when (((ex is OperationCanceledException || ex is WebSocketException || ex is ObjectDisposedException) ? 1 : 0) != 0)
		{
			if (!ct.IsCancellationRequested && !delivered.Task.IsCompleted)
			{
				MarkFailed(ex);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	internal void HandleMessage(ReadOnlySpan<byte> json)
	{
		using JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
		JsonElement rootElement = jsonDocument.RootElement;
		JsonElement value;
		switch ((rootElement.TryGetProperty("type", out value) && value.ValueKind == JsonValueKind.String) ? value.GetString() : null)
		{
		case "Results":
		{
			string text = "";
			if (rootElement.TryGetProperty("channel", out var value4) && value4.TryGetProperty("alternatives", out var value5) && value5.ValueKind == JsonValueKind.Array && value5.GetArrayLength() > 0 && value5[0].TryGetProperty("transcript", out var value6) && value6.ValueKind == JsonValueKind.String)
			{
				text = value6.GetString() ?? "";
			}
			JsonElement value7;
			bool flag = rootElement.TryGetProperty("is_final", out value7) && value7.ValueKind == JsonValueKind.True;
			lock (gate)
			{
				if (flag)
				{
					if (text.Trim().Length > 0)
					{
						finalSegments.Add(text.Trim());
					}
					latestInterim = "";
				}
				else if (text.Trim().Length > 0)
				{
					latestInterim = text.Trim();
				}
				break;
			}
		}
		case "Metadata":
			if (closeStreamSent)
			{
				delivered.TrySetResult(FinalTranscript());
			}
			break;
		case "Error":
		{
			JsonElement value2;
			JsonElement value3;
			string message = (rootElement.TryGetProperty("description", out value2) ? value2.GetString() : null) ?? (rootElement.TryGetProperty("message", out value3) ? value3.GetString() : null) ?? "Deepgram error";
			MarkFailed(new IOException(message));
			break;
		}
		}
	}

	private string? FinalTranscript()
	{
		lock (gate)
		{
			string text = string.Join(" ", finalSegments).Trim();
			return (text.Length == 0) ? null : text;
		}
	}

	private void MarkFailed(Exception ex, bool isPaywall = false)
	{
		if (!delivered.Task.IsCompleted)
		{
			if (!isPaywall)
			{
				Log.Warn("Ears", "Deepgram failed: " + ex.Message);
			}
			failure = ex;
			failed = true;
			opened.TrySetResult(result: false);
			delivered.TrySetResult(null);
		}
	}

	public async ValueTask DisposeAsync()
	{
		lifetime.Cancel();
		outgoing.Writer.TryComplete();
		delivered.TrySetResult(null);
		ClientWebSocket ws = socket;
		if (ws != null)
		{
			try
			{
				if (ws.State == WebSocketState.Open)
				{
					using CancellationTokenSource cts = new CancellationTokenSource(500);
					await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch
			{
			}
			ws.Dispose();
		}
		lifetime.Dispose();
	}
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Net;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Brain;

public sealed class BrainClient
{
	private const string Area = "Brain";

	private readonly ClickyHttp http;

	internal static readonly TimeSpan SendRetryWindow = TimeSpan.FromSeconds(8L);

	internal static readonly TimeSpan SendRetryDelay = TimeSpan.FromMilliseconds(150L, 0L);

	public BrainClient(ClickyHttp http)
	{
		this.http = http;
	}

	public async Task<BrainReply> StreamTurnAsync(Action<Utf8JsonWriter> writeBody, string accessToken, string usageContext, BrainReplyAccumulator accumulator, CancellationToken ct, Action? onResponseHeaders = null, Action<int, bool>? onRequestBody = null)
	{
		ArrayBufferWriter<byte> arrayBufferWriter = new ArrayBufferWriter<byte>(262144);
		using (Utf8JsonWriter obj = new Utf8JsonWriter(arrayBufferWriter, new JsonWriterOptions
		{
			SkipValidation = true
		}))
		{
			writeBody(obj);
		}
		byte[] bodyBytes = arrayBufferWriter.WrittenMemory.ToArray();
		(HttpContent, int, bool) tuple = BrainRequestCompression.Content(bodyBytes, bodyBytes.Length, "/brain/turn");
		HttpContent content = tuple.Item1;
		int sentBytes = tuple.Item2;
		bool isGzip = tuple.Item3;
		byte[] sentBody = await content.ReadAsByteArrayAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		onRequestBody?.Invoke(sentBytes, isGzip);
		if (isGzip)
		{
			Log.Info("OneBrain", $"turn body gzip {bodyBytes.Length / 1024} KB → {sentBytes / 1024} KB");
		}
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_BRAIN_DUMP_DIR");
		if (environmentVariable != null && environmentVariable.Length > 0)
		{
			Directory.CreateDirectory(environmentVariable);
			File.WriteAllBytes(Path.Combine(environmentVariable, $"turn-{DateTime.Now:HHmmss-fff}.json"), bodyBytes);
		}
		using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
		{
			timeout.CancelAfter(TimeSpan.FromSeconds(120L));
			using HttpResponseMessage response = await SendWithOneRetryAsync(BuildRequest, timeout.Token, ct).ConfigureAwait(continueOnCapturedContext: false);
			onResponseHeaders?.Invoke();
			BrainRequestCompression.NoteResponse(response, "/brain/turn");
			int status = (int)response.StatusCode;
			if (status != 200)
			{
				byte[] array = await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (BillingPaywallDetector.HandleIfPaywall(status, array, presentsPaywall: true, out PaywallPayload payload, out JsonElement raw))
				{
					throw new PaywallReachedException(payload, raw);
				}
				string @string = Encoding.UTF8.GetString(array.AsSpan(0, Math.Min(400, array.Length)));
				if (status == 409 && @string.Contains("screenshot_files_unavailable", StringComparison.Ordinal))
				{
					throw new ScreenshotFilesUnavailableException();
				}
				throw new WorkerHttpException(status, @string, "/brain/turn");
			}
			BrainReply reply;
			await using (Stream stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false))
			{
				using BrainSseParser parser = new BrainSseParser();
				byte[] buffer = ArrayPool<byte>.Shared.Rent(16384);
				try
				{
					while (true)
					{
						int num = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
						if (num == 0)
						{
							break;
						}
						parser.Feed(buffer.AsSpan(0, num), accumulator);
						ThrowIfError(accumulator.Reply);
					}
					parser.Complete(accumulator);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
				ThrowIfError(accumulator.Reply);
				if (!accumulator.Reply.SawDone)
				{
					throw new IOException("the connection ended before the answer was complete");
				}
				accumulator.Finish();
				reply = accumulator.Reply;
			}
			return reply;
		}
		HttpRequestMessage BuildRequest()
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl("/brain/turn"));
			httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			httpRequestMessage.Headers.TryAddWithoutValidation("X-Clicky-Usage-Context", usageContext);
			httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
			ByteArrayContent byteArrayContent = new ByteArrayContent(sentBody);
			byteArrayContent.Headers.ContentType = content.Headers.ContentType;
			foreach (string item in content.Headers.ContentEncoding)
			{
				byteArrayContent.Headers.ContentEncoding.Add(item);
			}
			httpRequestMessage.Content = byteArrayContent;
			return httpRequestMessage;
		}
	}

	internal async Task<HttpResponseMessage> SendWithOneRetryAsync(Func<HttpRequestMessage> buildRequest, CancellationToken sendToken, CancellationToken callerToken)
	{
		Stopwatch started = Stopwatch.StartNew();
		HttpRequestMessage first = buildRequest();
		try
		{
			return await http.Client.SendAsync(first, HttpCompletionOption.ResponseHeadersRead, sendToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (IsRetryableSendFailure(ex) && !sendToken.IsCancellationRequested && !callerToken.IsCancellationRequested && started.Elapsed < SendRetryWindow)
		{
			first.Dispose();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(72, 3);
			defaultInterpolatedStringHandler.AppendLiteral("/brain/turn send failed after ");
			defaultInterpolatedStringHandler.AppendFormatted(started.ElapsedMilliseconds);
			defaultInterpolatedStringHandler.AppendLiteral(" ms before any answer (");
			defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
			Exception innerException = ex.InnerException;
			defaultInterpolatedStringHandler.AppendFormatted((innerException != null) ? (": " + innerException.Message) : "");
			defaultInterpolatedStringHandler.AppendLiteral("); sending it again");
			Log.Warn("Brain", defaultInterpolatedStringHandler.ToStringAndClear());
		}
		await Task.Delay(SendRetryDelay, sendToken).ConfigureAwait(continueOnCapturedContext: false);
		return await http.Client.SendAsync(buildRequest(), HttpCompletionOption.ResponseHeadersRead, sendToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	internal static bool IsRetryableSendFailure(Exception ex)
	{
		if (ex is HttpRequestException || ex is IOException)
		{
			return true;
		}
		return false;
	}

	private static void ThrowIfError(BrainReply reply)
	{
		string error = reply.Error;
		if (error == null)
		{
			return;
		}
		if (reply.ErrorCode == "screenshot_files_unavailable")
		{
			throw new ScreenshotFilesUnavailableException();
		}
		throw new BrainTurnException(error);
	}

	public async Task<(int Status, JsonObject Json)?> PostJsonAsync(string path, Action<Utf8JsonWriter> writeBody, string accessToken, TimeSpan? timeout = null, string? usageContext = null, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl(path));
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			if (usageContext != null)
			{
				request.Headers.TryAddWithoutValidation("X-Clicky-Usage-Context", usageContext);
			}
			if (BrainRequestCompression.GzipPaths.Contains(path) && BrainRequestCompression.WorkerAcceptsGzip)
			{
				ArrayBufferWriter<byte> arrayBufferWriter = new ArrayBufferWriter<byte>(65536);
				using (Utf8JsonWriter writer2 = new Utf8JsonWriter(arrayBufferWriter, new JsonWriterOptions
				{
					SkipValidation = true
				}))
				{
					Write(writer2);
				}
				byte[] array = arrayBufferWriter.WrittenMemory.ToArray();
				request.Content = BrainRequestCompression.Content(array, array.Length, path).Content;
			}
			else
			{
				request.Content = new JsonWriterContent(Write);
			}
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(120L));
			using HttpResponseMessage response = await http.Client.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			BrainRequestCompression.NoteResponse(response, path);
			byte[] array2 = await response.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			JsonObject item;
			try
			{
				item = ((array2.Length != 0) ? (JsonNode.Parse(array2, null) as JsonObject) : null) ?? new JsonObject((JsonNodeOptions?)null);
			}
			catch (JsonException)
			{
				item = new JsonObject((JsonNodeOptions?)null);
			}
			return ((int)response.StatusCode, item);
		}
		catch (Exception ex2) when (((ex2 is HttpRequestException || ex2 is OperationCanceledException || ex2 is IOException) ? 1 : 0) != 0)
		{
			Log.Warn("Brain", path + " failed: " + ex2.Message);
			return null;
		}
		void Write(Utf8JsonWriter writer)
		{
			writer.WriteStartObject();
			writeBody(writer);
			writer.WriteEndObject();
		}
	}

	public async Task<BrainCompactReply?> CompactAsync(IReadOnlyList<JsonNode> thread, string sessionId, int inputTokens, string accessToken, CancellationToken ct = default(CancellationToken))
	{
		(int, JsonObject)? tuple = await PostJsonAsync("/brain/compact", delegate(Utf8JsonWriter writer)
		{
			WriteThread(writer, "thread", thread);
			writer.WriteString("session_id", sessionId);
			writer.WriteNumber("input_tokens", inputTokens);
		}, accessToken, TimeSpan.FromSeconds(180L), null, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (tuple.HasValue)
		{
			(int, JsonObject) valueOrDefault = tuple.GetValueOrDefault();
			JsonObject jsonObject = valueOrDefault.Item2["note_item"] as JsonObject;
			int valueOrDefault2 = ((int?)valueOrDefault.Item2["drop_leading_items"]?.AsValue().GetValue<double>()).GetValueOrDefault();
			if (valueOrDefault.Item1 == 200 && (jsonObject != null || valueOrDefault2 > 0) && valueOrDefault.Item2["keep_from_index"] is JsonValue jsonValue)
			{
				return new BrainCompactReply(new BrainCompactResult(jsonObject?.DeepClone(), (int)jsonValue.GetValue<double>(), ((int?)valueOrDefault.Item2["keep_leading_items"]?.AsValue().GetValue<double>()).GetValueOrDefault(), valueOrDefault2), null);
			}
			Log.Info("Brain", $"no compaction ({valueOrDefault.Item1}): {valueOrDefault.Item2["reason"] ?? valueOrDefault.Item2["error"]}");
			if (valueOrDefault.Item1 == 200 && valueOrDefault.Item2["tokens_until_fold"] is JsonValue jsonValue2)
			{
				return new BrainCompactReply(null, (int)jsonValue2.GetValue<double>());
			}
			return null;
		}
		Log.Warn("Brain", "compaction request failed");
		return null;
	}

	public static void WriteThread(Utf8JsonWriter writer, string propertyName, IReadOnlyList<JsonNode> thread)
	{
		writer.WritePropertyName(propertyName);
		writer.WriteStartArray();
		foreach (JsonNode item in thread)
		{
			item.WriteTo(writer);
		}
		writer.WriteEndArray();
	}
}

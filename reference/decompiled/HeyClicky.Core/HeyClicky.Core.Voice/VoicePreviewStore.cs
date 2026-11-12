using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Net;

namespace HeyClicky.Core.Voice;

public sealed class VoicePreviewStore
{
	public const int PrefetchConcurrency = 3;

	private readonly Func<CancellationToken, Task<string?>> accessToken;

	private readonly Func<string, byte[], CancellationToken, Task<byte[]?>> fetch;

	private readonly string directory;

	private readonly object gate = new object();

	private readonly Dictionary<string, Task<string?>> inFlight = new Dictionary<string, Task<string>>();

	private CancellationTokenSource? prefetch;

	public static VoicePreviewStore? Shared { get; set; }

	public event Action<string, string>? PreviewMade;

	public VoicePreviewStore(ClickyHttp http, Func<CancellationToken, Task<string?>> accessToken, string? directory = null)
		: this(accessToken, (string token, byte[] body, CancellationToken ct) => PostAsync(http, token, body, ct), directory)
	{
	}

	internal VoicePreviewStore(Func<CancellationToken, Task<string?>> accessToken, Func<string, byte[], CancellationToken, Task<byte[]?>> fetch, string? directory)
	{
		this.accessToken = accessToken;
		this.fetch = fetch;
		this.directory = directory ?? Path.Combine(AppBundleConfiguration.AppDataDirectory, "VoicePreviews");
	}

	public string FilePath(string voiceId, string languageCode)
	{
		string text = new string((VoiceCatalog.GeminiName(voiceId).ToLowerInvariant() + "-" + languageCode).Select(delegate(char c)
		{
			bool flag = char.IsAsciiLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '_') ? true : false);
				flag = flag2;
			}
			return (!flag) ? '_' : c;
		}).ToArray());
		return Path.Combine(directory, text + ".wav");
	}

	public string? Cached(string voiceId, string languageCode)
	{
		string text = FilePath(voiceId, languageCode);
		if (!File.Exists(text))
		{
			return null;
		}
		return text;
	}

	public bool IsMaking(string voiceId, string languageCode)
	{
		lock (gate)
		{
			Task<string> value;
			return inFlight.TryGetValue(Key(voiceId, languageCode), out value) && !value.IsCompleted;
		}
	}

	public Task<string?> PreviewAsync(string voiceId, string languageCode, CancellationToken ct = default(CancellationToken))
	{
		string text = Cached(voiceId, languageCode);
		if (text != null)
		{
			return Task.FromResult(text);
		}
		string key = Key(voiceId, languageCode);
		lock (gate)
		{
			if (inFlight.TryGetValue(key, out Task<string> value) && !value.IsCompleted)
			{
				return value;
			}
			Task<string> task = MakeAsync(voiceId, languageCode, ct);
			inFlight[key] = task;
			return task;
		}
	}

	public Task PrefetchAsync(IReadOnlyList<string> voiceIds, string languageCode)
	{
		CancellationTokenSource cts = new CancellationTokenSource();
		Interlocked.Exchange(ref prefetch, cts)?.Cancel();
		List<string> missing = voiceIds.Where((string id) => Cached(id, languageCode) == null).ToList();
		if (missing.Count == 0)
		{
			return Task.CompletedTask;
		}
		Log.Info("VoicePreview", $"prefetching {missing.Count} preview(s) in {languageCode}");
		return Task.Run(async delegate
		{
			for (int start = 0; start < missing.Count; start += 3)
			{
				if (cts.IsCancellationRequested)
				{
					break;
				}
				IEnumerable<Task<string>> tasks = from id in missing.Skip(start).Take(3)
					select PreviewAsync(id, languageCode, CancellationToken.None);
				try
				{
					await Task.WhenAll(tasks).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch
				{
				}
			}
		});
	}

	private async Task<string?> MakeAsync(string voiceId, string languageCode, CancellationToken ct)
	{
		_ = 2;
		try
		{
			string text = await accessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			using MemoryStream buffer = new MemoryStream();
			using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(buffer))
			{
				utf8JsonWriter.WriteStartObject();
				utf8JsonWriter.WriteString("voice", VoiceCatalog.GeminiName(voiceId));
				utf8JsonWriter.WriteString("language", languageCode);
				utf8JsonWriter.WriteEndObject();
			}
			byte[] arg = buffer.ToArray();
			byte[] array = await fetch(text, arg, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (array == null || array.Length <= 44 || Encoding.ASCII.GetString(array, 0, 4) != "RIFF")
			{
				Log.Info("VoicePreview", voiceId + " in " + languageCode + " not made");
				return null;
			}
			string path = FilePath(voiceId, languageCode);
			Directory.CreateDirectory(directory);
			string temporary = path + ".part";
			await File.WriteAllBytesAsync(temporary, array, ct).ConfigureAwait(continueOnCapturedContext: false);
			File.Move(temporary, path, overwrite: true);
			this.PreviewMade?.Invoke(voiceId, languageCode);
			return path;
		}
		catch (Exception ex)
		{
			Log.Info("VoicePreview", $"{voiceId} in {languageCode} failed: {ex.Message}");
			return null;
		}
	}

	private static async Task<byte[]?> PostAsync(ClickyHttp http, string token, byte[] body, CancellationToken ct)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl("/agent/realtime/voice-preview"));
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		request.Content = new ByteArrayContent(body);
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(TimeSpan.FromSeconds(30L));
		using HttpResponseMessage response = await http.Client.SendAsync(request, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		return (!response.IsSuccessStatusCode) ? null : (await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false));
	}

	private static string Key(string voiceId, string languageCode)
	{
		return voiceId + "|" + languageCode;
	}
}

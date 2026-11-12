using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Drawing.Media;

public sealed class PhotoLoader
{
	public const string ArticleQueryPrefix = "page:";

	public const string ArticleQuerySeparator = " :: ";

	public const string WebSearchQueryPrefix = "web:";

	public const string SourceIconQueryPrefix = "favicon:";

	public const string NoPeopleQueryPrefix = "nopeople:";

	private const int MaximumPhotoBytes = 15000000;

	private const int MaximumOriginalPhotoBytes = 1500000;

	private static readonly TimeSpan OriginalPhotoGrace = TimeSpan.FromSeconds(1.2);

	private const int MinimumPhotoEdgeInPixels = 120;

	private const int MinimumPhotoLongestEdgeInPixels = 300;

	private static readonly TimeSpan NextHitHeadStart = TimeSpan.FromSeconds(1.5);

	private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(6L);

	private readonly Func<CancellationToken, Task<string?>> accessTokenProvider;

	private readonly Uri workerBaseUrl;

	private readonly HttpClient workerClient;

	private readonly HttpClient publicClient;

	private readonly ConcurrentDictionary<string, byte[]> images = new ConcurrentDictionary<string, byte[]>();

	private readonly ConcurrentDictionary<string, Task<byte[]?>> loads = new ConcurrentDictionary<string, Task<byte[]>>();

	private readonly Action<HttpRequestMessage>? configureWorkerRequest;

	public PhotoLoader(Func<CancellationToken, Task<string?>> accessTokenProvider, Uri workerBaseUrl, Action<HttpRequestMessage>? configureWorkerRequest = null)
	{
		this.configureWorkerRequest = configureWorkerRequest;
		this.accessTokenProvider = accessTokenProvider;
		this.workerBaseUrl = workerBaseUrl;
		workerClient = new HttpClient(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(5L)
		})
		{
			Timeout = TimeSpan.FromSeconds(15L)
		};
		publicClient = new HttpClient(new SocketsHttpHandler
		{
			AllowAutoRedirect = true,
			MaxAutomaticRedirections = 5,
			ConnectCallback = ConnectToPublicHostAsync,
			AutomaticDecompression = DecompressionMethods.All
		})
		{
			Timeout = Timeout.InfiniteTimeSpan
		};
		publicClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) HeyClicky");
	}

	private static string Key(string query)
	{
		return PhotoQueries.Key(query);
	}

	public void Prefetch(string query)
	{
		ImageAsync(query, CancellationToken.None);
	}

	public Task<byte[]?> ImageAsync(string query, CancellationToken cancellationToken)
	{
		string key = Key(query);
		if (images.TryGetValue(key, out byte[] value))
		{
			return Task.FromResult(value);
		}
		return loads.GetOrAdd(key, (string k) => Task.Run(async delegate
		{
			try
			{
				byte[] array = await LoadAsync(k).ConfigureAwait(continueOnCapturedContext: false);
				if (array != null)
				{
					images[k] = array;
				}
				if (array != null)
				{
					Log.Info("Drawing", $"photo \"{k}\": {array.Length} bytes");
				}
				return array;
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "photo \"" + k + "\" failed: " + ex.Message);
				return (byte[])null;
			}
			finally
			{
				loads.TryRemove(k, out Task<byte[]> _);
			}
		}));
	}

	public async Task<byte[]?> ImageAsync(string query, CancellationToken cancellationToken, string? savedPhotosFolder)
	{
		string key = Key(query);
		if (savedPhotosFolder == null || images.ContainsKey(key))
		{
			return await ImageAsync(query, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		string path = Path.Combine(savedPhotosFolder, SavedPhotoFileName(key));
		try
		{
			if (File.Exists(path))
			{
				byte[] array = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (array.Length != 0)
				{
					images[key] = array;
					Log.Info("Drawing", $"photo \"{key}\": the saved copy ({array.Length} bytes)");
					return array;
				}
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
		}
		return await ImageAsync(query, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public IReadOnlyDictionary<string, byte[]> KnownPhotos(string program)
	{
		string lowercasedProgram = program.ToLowerInvariant();
		return images.Where<KeyValuePair<string, byte[]>>((KeyValuePair<string, byte[]> pair) => PhotoQueries.KnownPhotoIsUsed(pair.Key, lowercasedProgram)).ToDictionary((KeyValuePair<string, byte[]> pair) => pair.Key, (KeyValuePair<string, byte[]> pair) => pair.Value);
	}

	public static string SavedPhotoFileName(string query)
	{
		string text = query.Trim().ToLowerInvariant();
		string text2 = new string(text.Select((char c) => (!char.IsLetterOrDigit(c)) ? '-' : c).ToArray());
		if (text2.Length <= 80)
		{
			return text2 + ".jpg";
		}
		ulong num = 5381uL;
		foreach (Rune item in text.EnumerateRunes())
		{
			num = num * 33 + (ulong)item.Value;
		}
		return text2.Substring(0, 80) + "-" + num.ToString("x") + ".jpg";
	}

	private async Task<byte[]?> LoadAsync(string key)
	{
		if (key.StartsWith("favicon:", StringComparison.Ordinal))
		{
			string text = key;
			int length = "favicon:".Length;
			string text2 = text.Substring(length, text.Length - length).Trim();
			if (text2.Length == 0)
			{
				return null;
			}
			Uri requestUri = new Uri(workerBaseUrl, "/favicon?host=" + Uri.EscapeDataString(text2) + "&sz=64");
			using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6L));
			using HttpRequestMessage faviconRequest = new HttpRequestMessage(HttpMethod.Get, requestUri);
			configureWorkerRequest?.Invoke(faviconRequest);
			using HttpResponseMessage response = await workerClient.SendAsync(faviconRequest, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			return (!response.IsSuccessStatusCode) ? null : (await response.Content.ReadAsByteArrayAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false));
		}
		if (key.Length == 0)
		{
			return null;
		}
		string pageUrl = null;
		bool rejectsPeople = key.StartsWith("nopeople:", StringComparison.Ordinal);
		string text3;
		if (!rejectsPeople)
		{
			text3 = key;
		}
		else
		{
			string text = key;
			int length = "nopeople:".Length;
			text3 = text.Substring(length, text.Length - length).Trim();
		}
		string text4 = text3;
		bool isWebOnly = false;
		if (text4.StartsWith("page:", StringComparison.Ordinal))
		{
			string text = text4;
			int length = "page:".Length;
			string[] array = text.Substring(length, text.Length - length).Split(" :: ");
			pageUrl = array[0].Trim();
			text4 = string.Join(" ", array.Skip(1)).Trim().ToLowerInvariant();
			isWebOnly = true;
		}
		else if (text4.StartsWith("web:", StringComparison.Ordinal))
		{
			string text = text4;
			int length = "web:".Length;
			text4 = text.Substring(length, text.Length - length).Trim();
			isWebOnly = true;
		}
		if (text4.Length == 0 && pageUrl == null)
		{
			return null;
		}
		string searchTerm = ((text4.Length == 0) ? pageUrl : text4);
		List<(string Full, string Thumbnail)> results = await SearchAsync(searchTerm, !isWebOnly, pageUrl).ConfigureAwait(continueOnCapturedContext: false);
		if (results.Count == 0)
		{
			Log.Info("Drawing", "photo \"" + key + "\": search came back empty, asking once more");
			await Task.Delay(300).ConfigureAwait(continueOnCapturedContext: false);
			results = await SearchAsync(searchTerm, !isWebOnly, pageUrl).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (results.Count == 0)
		{
			Log.Info("Drawing", "photo \"" + key + "\": nothing found (the search returned no photos)");
			return null;
		}
		(byte[], int)? tuple = await FirstPhotoAsync(results.Take(rejectsPeople ? 8 : 4).ToList(), rejectsPeople).ConfigureAwait(continueOnCapturedContext: false);
		if (tuple.HasValue)
		{
			(byte[], int) valueOrDefault = tuple.GetValueOrDefault();
			Log.Info("Drawing", $"photo \"{key}\": hit #{valueOrDefault.Item2 + 1}");
			return valueOrDefault.Item1;
		}
		Log.Info("Drawing", $"photo \"{key}\": nothing found (none of {Math.Min(results.Count, 4)} photos downloaded)");
		return null;
	}

	private async Task<(byte[] Bytes, int Index)?> FirstPhotoAsync(IReadOnlyList<(string Full, string Thumbnail)> hits, bool rejectsPeople)
	{
		if (hits.Count == 0)
		{
			return null;
		}
		CancellationToken token;
		List<Task> pending;
		Dictionary<Task, int> headStarts;
		int started;
		using (CancellationTokenSource cancellation = new CancellationTokenSource())
		{
			token = cancellation.Token;
			pending = new List<Task>();
			headStarts = new Dictionary<Task, int>();
			started = 0;
			int failed = 0;
			(byte[] Bytes, int Index)? firstPhotoOfPeople = null;
			StartNext();
			try
			{
				while (pending.Count > 0)
				{
					Task task = await Task.WhenAny(pending).ConfigureAwait(continueOnCapturedContext: false);
					pending.Remove(task);
					if (headStarts.Remove(task, out var value))
					{
						if (value == started - 1)
						{
							StartNext();
						}
						continue;
					}
					int num;
					if (!(task is Task<(int, byte[], bool)> task2) || !task.IsCompletedSuccessfully)
					{
						num = failed + 1;
						failed = num;
						if (num != hits.Count)
						{
							continue;
						}
						return firstPhotoOfPeople;
					}
					var (num2, array, flag) = task2.Result;
					if (array != null && !flag)
					{
						return (array, num2);
					}
					if (array != null)
					{
						Log.Info("Drawing", $"photo hit #{num2 + 1} passed over: it shows people");
						firstPhotoOfPeople.GetValueOrDefault();
						if (!firstPhotoOfPeople.HasValue)
						{
							(byte[], int) value2 = (array, num2);
							firstPhotoOfPeople = value2;
						}
					}
					num = failed + 1;
					failed = num;
					if (num == hits.Count)
					{
						return firstPhotoOfPeople;
					}
					if (num2 == started - 1)
					{
						StartNext();
					}
				}
				return firstPhotoOfPeople;
			}
			finally
			{
				cancellation.Cancel();
			}
		}
		void StartNext()
		{
			if (started < hits.Count)
			{
				int index = started++;
				var (full, thumbnail) = hits[index];
				pending.Add(Task.Run(async delegate
				{
					byte[] bytes = await BestPhotoAsync(full, thumbnail, token).ConfigureAwait(continueOnCapturedContext: false);
					bool flag2 = rejectsPeople && bytes != null;
					if (flag2)
					{
						flag2 = await PhotoInspection.ShowsPeopleAsync(bytes).ConfigureAwait(continueOnCapturedContext: false);
					}
					bool item = flag2;
					return (Index: index, Bytes: bytes, ShowsPeople: item);
				}));
				if (started < hits.Count)
				{
					Task task3 = Task.Delay(NextHitHeadStart, token).ContinueWith(delegate
					{
					}, TaskScheduler.Default);
					headStarts[task3] = index;
					pending.Add(task3);
				}
			}
		}
	}

	private async Task<List<(string Full, string Thumbnail)>> SearchAsync(string query, bool preferSubjectPhoto, string? pageUrl)
	{
		List<(string, string)> results = new List<(string, string)>();
		string text = await accessTokenProvider(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		if (string.IsNullOrEmpty(text))
		{
			Log.Warn("Drawing", "photo search skipped: not signed in");
			return results;
		}
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(workerBaseUrl, "/image-search"));
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text);
		configureWorkerRequest?.Invoke(request);
		Dictionary<string, object> dictionary = new Dictionary<string, object>
		{
			["query"] = query,
			["limit"] = 4
		};
		if (preferSubjectPhoto)
		{
			dictionary["subject_photo"] = true;
		}
		if (pageUrl != null)
		{
			dictionary["page_url"] = pageUrl;
		}
		request.Content = new StringContent(JsonSerializer.Serialize(dictionary), Encoding.UTF8, "application/json");
		using CancellationTokenSource timeout = new CancellationTokenSource(SearchTimeout);
		try
		{
			using HttpResponseMessage response = await workerClient.SendAsync(request, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			if (!response.IsSuccessStatusCode)
			{
				Log.Warn("Drawing", $"image-search HTTP {response.StatusCode}");
				return results;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false));
			if (jsonDocument.RootElement.TryGetProperty("images", out var value) && value.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value.EnumerateArray())
				{
					JsonElement value2;
					string text2 = (item.TryGetProperty("full_url", out value2) ? (value2.GetString() ?? "") : "");
					JsonElement value3;
					string text3 = (item.TryGetProperty("thumbnail_url", out value3) ? (value3.GetString() ?? "") : "");
					if (text2.Length > 0 || text3.Length > 0)
					{
						results.Add((text2, text3));
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			Log.Warn("Drawing", $"image-search gave up after {SearchTimeout.TotalSeconds:F0} s");
		}
		return results;
	}

	private async Task<byte[]?> BestPhotoAsync(string originalUrl, string smallCopyUrl, CancellationToken cancellationToken)
	{
		if (originalUrl == smallCopyUrl || originalUrl.Length == 0)
		{
			return await FetchPhotoAsync(smallCopyUrl, 15000000, TimeSpan.FromSeconds(6L), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (smallCopyUrl.Length == 0)
		{
			return await FetchPhotoAsync(originalUrl, 1500000, TimeSpan.FromSeconds(2.5), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		Task<byte[]?> original = FetchPhotoAsync(originalUrl, 1500000, TimeSpan.FromSeconds(2.5), cancellationToken);
		Task<byte[]?> smallCopy = FetchPhotoAsync(smallCopyUrl, 15000000, TimeSpan.FromSeconds(6L), cancellationToken);
		Task task = Task.Delay(OriginalPhotoGrace, cancellationToken).ContinueWith(delegate
		{
		}, TaskScheduler.Default);
		if (await Task.WhenAny(original, task).ConfigureAwait(continueOnCapturedContext: false) == original)
		{
			byte[] result = original.Result;
			if (result != null)
			{
				return result;
			}
		}
		byte[] array = await smallCopy.ConfigureAwait(continueOnCapturedContext: false);
		if (array != null)
		{
			return array;
		}
		return await original.ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task<byte[]?> FetchPhotoAsync(string url, int maximumBytes, TimeSpan timeout, CancellationToken cancellationToken)
	{
		byte[] bytes = await FetchAsync(url, maximumBytes, timeout, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (bytes == null)
		{
			return null;
		}
		(int, int)? tuple = await PhotoInspection.PixelSizeAsync(bytes).ConfigureAwait(continueOnCapturedContext: false);
		object result;
		if (tuple.HasValue)
		{
			(int, int) valueOrDefault = tuple.GetValueOrDefault();
			if (PhotoQueries.IsBigEnough(valueOrDefault.Item1, valueOrDefault.Item2, 120, 300))
			{
				result = bytes;
				goto IL_0156;
			}
		}
		result = null;
		goto IL_0156;
		IL_0156:
		return (byte[]?)result;
	}

	private async Task<byte[]?> FetchAsync(string url, int maximumBytes, TimeSpan timeout, CancellationToken cancellationToken)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result) || (result.Scheme != "https" && result.Scheme != "http"))
		{
			return null;
		}
		try
		{
			using (CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
			{
				cancellation.CancelAfter(timeout);
				using HttpResponseMessage response = await publicClient.GetAsync(result, HttpCompletionOption.ResponseHeadersRead, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (!response.IsSuccessStatusCode)
				{
					return null;
				}
				long? contentLength = response.Content.Headers.ContentLength;
				if (contentLength.HasValue)
				{
					long valueOrDefault = contentLength.GetValueOrDefault();
					if (valueOrDefault > maximumBytes)
					{
						return null;
					}
				}
				byte[] result2;
				await using (Stream stream = await response.Content.ReadAsStreamAsync(cancellation.Token).ConfigureAwait(continueOnCapturedContext: false))
				{
					using MemoryStream buffer = new MemoryStream();
					byte[] chunk = new byte[81920];
					while (true)
					{
						int num;
						if ((num = await stream.ReadAsync(chunk, cancellation.Token).ConfigureAwait(continueOnCapturedContext: false)) > 0)
						{
							if (buffer.Length + num > maximumBytes)
							{
								result2 = null;
								break;
							}
							buffer.Write(chunk, 0, num);
							continue;
						}
						result2 = ((buffer.Length > 0) ? buffer.ToArray() : null);
						break;
					}
				}
				return result2;
			}
			IL_042f:
			byte[] result3;
			return result3;
		}
		catch
		{
			return null;
		}
	}

	private static async ValueTask<Stream> ConnectToPublicHostAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
	{
		IPAddress[] array = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (array.Length == 0 || !array.All(IsPublic))
		{
			throw new HttpRequestException("not a public host: " + context.DnsEndPoint.Host);
		}
		Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
		{
			NoDelay = true
		};
		try
		{
			await socket.ConnectAsync(array, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new NetworkStream(socket, ownsSocket: true);
		}
		catch
		{
			socket.Dispose();
			throw;
		}
	}

	private static bool IsPublic(IPAddress address)
	{
		if (address.IsIPv4MappedToIPv6)
		{
			address = address.MapToIPv4();
		}
		if (IPAddress.IsLoopback(address))
		{
			return false;
		}
		byte[] addressBytes = address.GetAddressBytes();
		if (address.AddressFamily == AddressFamily.InterNetwork)
		{
			if (addressBytes[0] != 0 && addressBytes[0] != 10 && addressBytes[0] != 127 && (addressBytes[0] != 100 || addressBytes[1] < 64 || addressBytes[1] >= 128) && (addressBytes[0] != 169 || addressBytes[1] != 254) && (addressBytes[0] != 172 || addressBytes[1] < 16 || addressBytes[1] >= 32) && (addressBytes[0] != 192 || addressBytes[1] != 168) && (addressBytes[0] != 192 || addressBytes[1] != 0 || addressBytes[2] != 0) && (addressBytes[0] != 198 || (addressBytes[1] != 18 && addressBytes[1] != 19)))
			{
				return addressBytes[0] < 224;
			}
			return false;
		}
		if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
		{
			return false;
		}
		if ((addressBytes[0] & 0xFE) == 252)
		{
			return false;
		}
		if (!address.Equals(IPAddress.IPv6None))
		{
			return !address.Equals(IPAddress.IPv6Any);
		}
		return false;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Browser;

public sealed class LoopbackHttpServer : IDisposable
{
	public sealed record Request(string Method, string Path, IReadOnlyDictionary<string, string> Headers, byte[] Body);

	public sealed record Response(int Status, IReadOnlyDictionary<string, string> Headers, byte[] Body)
	{
		public static Response Json(int status, JsonObject? body, IReadOnlyDictionary<string, string>? headers = null)
		{
			return new Response(status, headers ?? new Dictionary<string, string>(), (body == null) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body.ToJsonString()));
		}
	}

	private const int MaxHeaderBytes = 262144;

	private const int MaxBodyBytes = 4194304;

	private readonly Func<Request, Task<Response>> handler;

	private readonly object gate = new object();

	private TcpListener? listener;

	private CancellationTokenSource? stop;

	private int? port;

	private bool listening;

	public string BearerToken { get; } = RandomToken();

	public string PathPrefix { get; }

	public int? Port
	{
		get
		{
			lock (gate)
			{
				return port;
			}
		}
	}

	public string? Url
	{
		get
		{
			int? num = Port;
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				return $"http://127.0.0.1:{valueOrDefault}{PathPrefix}";
			}
			return null;
		}
	}

	public bool IsListening
	{
		get
		{
			lock (gate)
			{
				int? num = port;
				return num.HasValue && listening;
			}
		}
	}

	public LoopbackHttpServer(Func<Request, Task<Response>> handler, string path = "/mcp")
	{
		this.handler = handler;
		PathPrefix = path;
	}

	public void Start()
	{
		lock (gate)
		{
			int? num = port;
			if (!num.HasValue)
			{
				TcpListener tcpListener = new TcpListener(IPAddress.Loopback, 0);
				tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, optionValue: true);
				tcpListener.Start();
				listener = tcpListener;
				stop = new CancellationTokenSource();
				port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
				AcceptLoopAsync(tcpListener, stop.Token);
			}
		}
	}

	public void Stop()
	{
		lock (gate)
		{
			stop?.Cancel();
			try
			{
				listener?.Stop();
			}
			catch
			{
			}
			listener = null;
			port = null;
		}
	}

	public void Dispose()
	{
		Stop();
	}

	private async Task AcceptLoopAsync(TcpListener l, CancellationToken ct)
	{
		lock (gate)
		{
			listening = true;
		}
		int consecutiveFailures = 0;
		try
		{
			while (!ct.IsCancellationRequested)
			{
				TcpClient client;
				try
				{
					client = await l.AcceptTcpClientAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception) when (ct.IsCancellationRequested)
				{
					break;
				}
				catch (ObjectDisposedException)
				{
					break;
				}
				catch (Exception ex3) when (((ex3 is SocketException || ex3 is IOException || ex3 is InvalidOperationException) ? 1 : 0) != 0)
				{
					consecutiveFailures++;
					Log.Warn("Browser", $"loopback accept failed ({consecutiveFailures}): {ex3.Message}");
					if (consecutiveFailures >= 20)
					{
						break;
					}
					try
					{
						await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(1000, 50 * consecutiveFailures), 0L), ct).ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					continue;
				}
				consecutiveFailures = 0;
				Task.Run(() => HandleAsync(client, ct));
			}
		}
		finally
		{
			lock (gate)
			{
				if (listener == l)
				{
					listening = false;
				}
			}
		}
	}

	private async Task HandleAsync(TcpClient client, CancellationToken ct)
	{
		using (client)
		{
			try
			{
				using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
				timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
				client.NoDelay = true;
				NetworkStream stream = client.GetStream();
				MemoryStream buffer = new MemoryStream();
				byte[] chunk = new byte[65536];
				int num = -1;
				while (num < 0)
				{
					int num2 = await stream.ReadAsync(chunk, timeoutCts.Token).ConfigureAwait(continueOnCapturedContext: false);
					if (num2 <= 0)
					{
						return;
					}
					buffer.Write(chunk, 0, num2);
					num = IndexOf(buffer.GetBuffer(), (int)buffer.Length, "\r\n\r\n"u8);
					if (num < 0 && buffer.Length >= 262144)
					{
						return;
					}
				}
				byte[] buffer2 = buffer.GetBuffer();
				string[] array = Encoding.UTF8.GetString(buffer2, 0, num).Split("\r\n");
				string[] array2 = array[0].Split(' ');
				string method = ((array2.Length != 0) ? array2[0] : "GET");
				string target = ((array2.Length > 1) ? array2[1] : "/");
				Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.Ordinal);
				foreach (string item in array.Skip(1))
				{
					int num3 = item.IndexOf(':');
					if (num3 > 0)
					{
						string key = item.Substring(0, num3).Trim().ToLowerInvariant();
						string text = item;
						int num4 = num3 + 1;
						headers[key] = text.Substring(num4, text.Length - num4).Trim();
					}
				}
				int result;
				int contentLength = (int.TryParse(headers.GetValueOrDefault("content-length"), out result) ? result : 0);
				if (contentLength < 0 || contentLength > 4194304)
				{
					return;
				}
				int bodyStart = num + 4;
				while (buffer.Length - bodyStart < contentLength)
				{
					int num5 = await stream.ReadAsync(chunk, timeoutCts.Token).ConfigureAwait(continueOnCapturedContext: false);
					if (num5 <= 0)
					{
						return;
					}
					buffer.Write(chunk, 0, num5);
				}
				byte[] array3 = new byte[contentLength];
				Array.Copy(buffer.GetBuffer(), bodyStart, array3, 0, contentLength);
				int num6 = target.IndexOfAny(new char[2] { '?', '#' });
				string path = ((num6 >= 0) ? target.Substring(0, num6) : target);
				await SendAsync(stream, await RouteAsync(new Request(method, path, headers, array3)).ConfigureAwait(continueOnCapturedContext: false), ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
			}
		}
	}

	private async Task<Response> RouteAsync(Request request)
	{
		if (request.Path != PathPrefix && request.Path != PathPrefix + "/")
		{
			return Response.Json(404, new JsonObject((JsonNodeOptions?)null) { ["error"] = "not_found" });
		}
		if (!FixedTimeEquals(request.Headers.GetValueOrDefault("authorization"), "Bearer " + BearerToken))
		{
			return Response.Json(401, new JsonObject((JsonNodeOptions?)null) { ["error"] = "unauthorized" });
		}
		return await handler(request).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static bool FixedTimeEquals(string? a, string b)
	{
		if (a != null)
		{
			return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
		}
		return false;
	}

	private static async Task SendAsync(NetworkStream stream, Response response, CancellationToken ct)
	{
		string key = response.Status switch
		{
			200 => "OK", 
			202 => "Accepted", 
			400 => "Bad Request", 
			401 => "Unauthorized", 
			404 => "Not Found", 
			405 => "Method Not Allowed", 
			_ => "Status", 
		};
		string value = key;
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder2);
		appendInterpolatedStringHandler.AppendLiteral("HTTP/1.1 ");
		appendInterpolatedStringHandler.AppendFormatted(response.Status);
		appendInterpolatedStringHandler.AppendLiteral(" ");
		appendInterpolatedStringHandler.AppendFormatted(value);
		appendInterpolatedStringHandler.AppendLiteral("\r\n");
		stringBuilder3.Append(ref appendInterpolatedStringHandler);
		stringBuilder.Append("Content-Type: application/json\r\n");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		appendInterpolatedStringHandler.AppendLiteral("Content-Length: ");
		appendInterpolatedStringHandler.AppendFormatted(response.Body.Length);
		appendInterpolatedStringHandler.AppendLiteral("\r\n");
		stringBuilder4.Append(ref appendInterpolatedStringHandler);
		foreach (KeyValuePair<string, string> header in response.Headers)
		{
			header.Deconstruct(out key, out var value2);
			string value3 = key;
			string value4 = value2;
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
			appendInterpolatedStringHandler.AppendFormatted(value3);
			appendInterpolatedStringHandler.AppendLiteral(": ");
			appendInterpolatedStringHandler.AppendFormatted(value4);
			appendInterpolatedStringHandler.AppendLiteral("\r\n");
			stringBuilder5.Append(ref appendInterpolatedStringHandler);
		}
		stringBuilder.Append("Cache-Control: no-store\r\n");
		stringBuilder.Append("Connection: close\r\n\r\n");
		await stream.WriteAsync(Encoding.UTF8.GetBytes(stringBuilder.ToString()), ct).ConfigureAwait(continueOnCapturedContext: false);
		if (response.Body.Length != 0)
		{
			await stream.WriteAsync(response.Body, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		await stream.FlushAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static int IndexOf(byte[] haystack, int length, ReadOnlySpan<byte> needle)
	{
		return haystack.AsSpan(0, length).IndexOf(needle);
	}

	public static string RandomToken()
	{
		return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
	}
}

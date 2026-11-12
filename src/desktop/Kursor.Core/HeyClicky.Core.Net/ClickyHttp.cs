using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Net;

public sealed class ClickyHttp : IDisposable
{
	private readonly SocketsHttpHandler socketsHandler;

	private long lastWorkerPrewarmTicks;

	private long lastOpenAIPrewarmTicks;

	private long lastDeepgramPrewarmTicks;

	private long lastGeminiPrewarmTicks;

	public HttpClient Client { get; }

	public HttpMessageInvoker Invoker { get; }

	public ClickyEndpoints Endpoints { get; }

	public ClientIdentity Identity { get; }

	public ClickyHttp(ClickyEndpoints endpoints, ClientIdentity identity)
	{
		Endpoints = endpoints;
		Identity = identity;
		SocketsHttpHandler obj = new SocketsHttpHandler
		{
			EnableMultipleHttp2Connections = true,
			PooledConnectionLifetime = TimeSpan.FromMinutes(10L),
			PooledConnectionIdleTimeout = TimeSpan.FromMinutes(3L),
			ConnectTimeout = TimeSpan.FromSeconds(6L),
			KeepAlivePingDelay = TimeSpan.FromSeconds(30L),
			KeepAlivePingTimeout = TimeSpan.FromSeconds(10L),
			KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
			AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli),
			MaxConnectionsPerServer = 16,
			UseCookies = false
		};
		SslClientAuthenticationOptions sslClientAuthenticationOptions = new SslClientAuthenticationOptions();
		int num = 2;
		List<SslApplicationProtocol> list = new List<SslApplicationProtocol>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<SslApplicationProtocol> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = SslApplicationProtocol.Http2;
		num2++;
		span[num2] = SslApplicationProtocol.Http11;
		sslClientAuthenticationOptions.ApplicationProtocols = list;
		obj.SslOptions = sslClientAuthenticationOptions;
		socketsHandler = obj;
		Invoker = new HttpMessageInvoker(socketsHandler, disposeHandler: false);
		Client = new HttpClient(new ClickyClientHeadersHandler(endpoints, identity, socketsHandler), disposeHandler: false)
		{
			Timeout = Timeout.InfiniteTimeSpan,
			DefaultRequestVersion = HttpVersion.Version20,
			DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
		};
	}

	public Uri WorkerUrl(string pathAndQuery)
	{
		return new Uri(Endpoints.WorkerBaseUrl, pathAndQuery);
	}

	public Uri SupabaseUrl(string pathAndQuery)
	{
		return new Uri(Endpoints.SupabaseUrl, pathAndQuery);
	}

	public void PrewarmWorker()
	{
		PrewarmHost(Endpoints.WorkerBaseUrl, ref lastWorkerPrewarmTicks, TimeSpan.FromSeconds(5L));
	}

	public void PrewarmOpenAI()
	{
		PrewarmHost(new Uri("https://api.openai.com"), ref lastOpenAIPrewarmTicks, TimeSpan.FromSeconds(60L));
	}

	public void PrewarmDeepgram()
	{
		PrewarmHost(new Uri("https://api.deepgram.com"), ref lastDeepgramPrewarmTicks, TimeSpan.FromSeconds(60L));
	}

	public void PrewarmGemini()
	{
		PrewarmHost(new Uri("https://generativelanguage.googleapis.com"), ref lastGeminiPrewarmTicks, TimeSpan.FromSeconds(60L));
	}

	public void PrewarmAll()
	{
		PrewarmWorker();
		PrewarmOpenAI();
		PrewarmDeepgram();
		PrewarmGemini();
	}

	private void PrewarmHost(Uri baseUrl, ref long lastTicks, TimeSpan minimumGap)
	{
		long tickCount = Environment.TickCount64;
		long num = Interlocked.Read(in lastTicks);
		if (num != 0L && (double)(tickCount - num) < minimumGap.TotalMilliseconds)
		{
			return;
		}
		Interlocked.Exchange(ref lastTicks, tickCount);
		Task.Run(async delegate
		{
			try
			{
				using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10L));
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, baseUrl);
				using (await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(continueOnCapturedContext: false))
				{
				}
			}
			catch
			{
			}
		});
	}

	public void Dispose()
	{
		Client.Dispose();
		Invoker.Dispose();
		socketsHandler.Dispose();
	}
}

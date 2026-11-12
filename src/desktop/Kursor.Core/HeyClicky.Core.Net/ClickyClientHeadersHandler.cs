using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Net;

public sealed class ClickyClientHeadersHandler : DelegatingHandler
{
	private readonly ClickyEndpoints endpoints;

	private readonly ClientIdentity identity;

	public ClickyClientHeadersHandler(ClickyEndpoints endpoints, ClientIdentity identity, HttpMessageHandler inner)
		: base(inner)
	{
		this.endpoints = endpoints;
		this.identity = identity;
	}

	public static bool IsClickyBackendUrl(Uri url, ClickyEndpoints endpoints)
	{
		if (url.IsAbsoluteUri && string.Equals(url.Host, endpoints.WorkerBaseUrl.Host, StringComparison.OrdinalIgnoreCase))
		{
			return url.Port == endpoints.WorkerBaseUrl.Port;
		}
		return false;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Uri requestUri = request.RequestUri;
		if ((object)requestUri != null && IsClickyBackendUrl(requestUri, endpoints))
		{
			Apply(request.Headers, identity);
		}
		return base.SendAsync(request, cancellationToken);
	}

	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Uri requestUri = request.RequestUri;
		if ((object)requestUri != null && IsClickyBackendUrl(requestUri, endpoints))
		{
			Apply(request.Headers, identity);
		}
		return base.Send(request, cancellationToken);
	}

	public static void Apply(HttpRequestHeaders headers, ClientIdentity identity)
	{
		headers.TryAddWithoutValidation("X-Clicky-Platform", identity.Platform);
		headers.TryAddWithoutValidation("X-Clicky-Device", identity.DeviceId);
		headers.TryAddWithoutValidation("X-Clicky-App-Version", identity.AppVersion);
		headers.TryAddWithoutValidation("X-Clicky-App-Build", identity.AppBuild);
		headers.TryAddWithoutValidation("X-Clicky-OS-Version", identity.OsVersion);
		try
		{
			ClickyAITracing.Apply(headers);
		}
		catch
		{
		}
	}
}

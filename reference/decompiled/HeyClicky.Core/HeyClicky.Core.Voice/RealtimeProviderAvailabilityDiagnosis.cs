using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Voice;

public static class RealtimeProviderAvailabilityDiagnosis
{
	public enum SessionOpenFailureKind
	{
		WorkerCouldNotReachProvider,
		AppCouldNotReachProvider
	}

	public enum ProbeFailureKind
	{
		TlsRefused,
		HostUnreachable,
		Other
	}

	public sealed record ProviderProbeOutcome(int StatusCode, string BodyText, ProbeFailureKind? Failure)
	{
		public static ProviderProbeOutcome Answered(int statusCode, string bodyText)
		{
			return new ProviderProbeOutcome(statusCode, bodyText, null);
		}

		public static ProviderProbeOutcome Failed(ProbeFailureKind failure)
		{
			return new ProviderProbeOutcome(0, "", failure);
		}
	}

	private const string ProviderNameForMessages = "OpenAI";

	private static readonly Uri ProviderProbeUrl = new Uri("https://api.openai.com/v1/models");

	private static readonly TimeSpan ProviderProbeTimeout = TimeSpan.FromSeconds(4L);

	private static readonly TimeSpan PositiveDiagnosisLifetime = TimeSpan.FromMinutes(10L);

	private static readonly object rememberedGate = new object();

	private static (RealtimeProviderUnavailableReason Reason, DateTimeOffset MadeAt)? rememberedDiagnosis;

	public static async Task<RealtimeProviderUnavailableReason?> DiagnoseAsync(Exception? sessionOpenError, Func<Task<ProviderProbeOutcome>>? probe = null, Func<DateTimeOffset>? clock = null)
	{
		if (sessionOpenError == null)
		{
			return null;
		}
		DateTimeOffset now = (clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow)))();
		RealtimeProviderUnavailableReason realtimeProviderUnavailableReason = ReasonReportedByWorker(sessionOpenError);
		if ((object)realtimeProviderUnavailableReason != null)
		{
			lock (rememberedGate)
			{
				rememberedDiagnosis = (realtimeProviderUnavailableReason, now);
			}
			return realtimeProviderUnavailableReason;
		}
		SessionOpenFailureKind? sessionOpenFailureKind = FailureKindWorthProbing(sessionOpenError);
		if (sessionOpenFailureKind.HasValue)
		{
			SessionOpenFailureKind failureKind = sessionOpenFailureKind.GetValueOrDefault();
			lock (rememberedGate)
			{
				(RealtimeProviderUnavailableReason, DateTimeOffset)? tuple = rememberedDiagnosis;
				if (tuple.HasValue)
				{
					(RealtimeProviderUnavailableReason, DateTimeOffset) valueOrDefault = tuple.GetValueOrDefault();
					if (now - valueOrDefault.Item2 < PositiveDiagnosisLifetime)
					{
						return valueOrDefault.Item1;
					}
				}
			}
			RealtimeProviderUnavailableReason realtimeProviderUnavailableReason2 = Reason(await (probe ?? new Func<Task<ProviderProbeOutcome>>(ProbeProviderAsync))().ConfigureAwait(continueOnCapturedContext: false), failureKind);
			if ((object)realtimeProviderUnavailableReason2 != null)
			{
				lock (rememberedGate)
				{
					rememberedDiagnosis = (realtimeProviderUnavailableReason2, now);
				}
			}
			return realtimeProviderUnavailableReason2;
		}
		return null;
	}

	internal static void ForgetRememberedDiagnosis()
	{
		lock (rememberedGate)
		{
			rememberedDiagnosis = null;
		}
	}

	public static RealtimeProviderUnavailableReason? ReasonReportedByWorker(Exception sessionOpenError)
	{
		if (!(sessionOpenError is WorkerHttpException ex))
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(ex.Body);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("error_code", out var value) || value.ValueKind != JsonValueKind.String || value.GetString() != "provider_region_unsupported")
			{
				return null;
			}
			return new RealtimeProviderUnavailableReason.RegionUnsupported(Field(rootElement, "provider_name") ?? "OpenAI", Field(rootElement, "client_location_name"));
		}
		catch (JsonException)
		{
			return null;
		}
		static string? Field(JsonElement root, string name)
		{
			if (root.TryGetProperty(name, out var value2) && value2.ValueKind == JsonValueKind.String)
			{
				string text = value2.GetString().Trim();
				if (text != null && text.Length > 0)
				{
					return text;
				}
			}
			return null;
		}
	}

	public static SessionOpenFailureKind? FailureKindWorthProbing(Exception sessionOpenError)
	{
		if (!(sessionOpenError is WorkerHttpException ex))
		{
			if (sessionOpenError is RealtimeSocketOpenException)
			{
				return SessionOpenFailureKind.AppCouldNotReachProvider;
			}
			return null;
		}
		return (ex.Status == 502) ? new SessionOpenFailureKind?(SessionOpenFailureKind.WorkerCouldNotReachProvider) : ((SessionOpenFailureKind?)null);
	}

	public static RealtimeProviderUnavailableReason? Reason(ProviderProbeOutcome outcome, SessionOpenFailureKind failureKind)
	{
		ProbeFailureKind? failure = outcome.Failure;
		if (failure.HasValue)
		{
			ProbeFailureKind valueOrDefault = failure.GetValueOrDefault();
			if (valueOrDefault == ProbeFailureKind.TlsRefused)
			{
				return new RealtimeProviderUnavailableReason.NetworkBlocksProvider("OpenAI");
			}
			if (failureKind == SessionOpenFailureKind.AppCouldNotReachProvider && valueOrDefault == ProbeFailureKind.HostUnreachable)
			{
				return new RealtimeProviderUnavailableReason.NetworkBlocksProvider("OpenAI");
			}
			return null;
		}
		string text = outcome.BodyText.ToLowerInvariant();
		if (outcome.StatusCode != 403 || (!text.Contains("unsupported_country_region_territory") && !text.Contains("country, region, or territory not supported")))
		{
			return null;
		}
		return new RealtimeProviderUnavailableReason.RegionUnsupported("OpenAI", null);
	}

	public static ProbeFailureKind ClassifyConnectionFailure(Exception error)
	{
		for (Exception ex = error; ex != null; ex = ex.InnerException)
		{
			if (ex is AuthenticationException)
			{
				return ProbeFailureKind.TlsRefused;
			}
			if (ex is HttpRequestException { HttpRequestError: var httpRequestError })
			{
				switch (httpRequestError)
				{
				case HttpRequestError.SecureConnectionError:
					return ProbeFailureKind.TlsRefused;
				case HttpRequestError.NameResolutionError:
				case HttpRequestError.ConnectionError:
					return ProbeFailureKind.HostUnreachable;
				}
			}
			if (ex is SocketException { SocketErrorCode: var socketErrorCode })
			{
				bool flag;
				switch (socketErrorCode)
				{
				case SocketError.NetworkUnreachable:
				case SocketError.ConnectionRefused:
				case SocketError.HostUnreachable:
				case SocketError.HostNotFound:
				case SocketError.NoData:
					flag = true;
					break;
				default:
					flag = false;
					break;
				}
				if (!flag)
				{
					return ProbeFailureKind.Other;
				}
				return ProbeFailureKind.HostUnreachable;
			}
		}
		return ProbeFailureKind.Other;
	}

	private static async Task<ProviderProbeOutcome> ProbeProviderAsync()
	{
		using SocketsHttpHandler handler = new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.Zero,
			UseCookies = false
		};
		using HttpClient client = new HttpClient(handler)
		{
			Timeout = ProviderProbeTimeout
		};
		_ = 1;
		try
		{
			using HttpResponseMessage response = await client.GetAsync(ProviderProbeUrl).ConfigureAwait(continueOnCapturedContext: false);
			string text = await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
			return ProviderProbeOutcome.Answered((int)response.StatusCode, (text.Length > 2000) ? text.Substring(0, 2000) : text);
		}
		catch (Exception error)
		{
			return ProviderProbeOutcome.Failed(ClassifyConnectionFailure(error));
		}
	}
}

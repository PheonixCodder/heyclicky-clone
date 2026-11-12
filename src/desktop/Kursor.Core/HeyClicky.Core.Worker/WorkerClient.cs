using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Net;
using Microsoft.Win32;

namespace HeyClicky.Core.Worker;

public sealed class WorkerClient
{
	private const string Area = "Worker";

	private readonly ClickyHttp http;

	private readonly SupabaseAuthService auth;

	public static readonly IReadOnlyList<string> DiscoveryChannels = new global::_003C_003Ez__ReadOnlyArray<string>(new string[13]
	{
		"instagram_ad", "instagram", "youtube_ad", "youtube", "tiktok", "x", "linkedin", "reddit", "chatgpt", "claude",
		"google", "friend", "other"
	});

	public ClickyHttp Http => http;

	public WorkerClient(ClickyHttp http, SupabaseAuthService auth)
	{
		this.http = http;
		this.auth = auth;
	}

	public Task<PlanSnapshot?> GetPlanAsync(CancellationToken ct = default(CancellationToken))
	{
		return GetAsync("/me/plan", CoreJsonContext.Default.PlanSnapshot, ct);
	}

	public async Task<AppConfig?> GetAppConfigAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await SendAsync(HttpMethod.Get, "/app-config", null, CoreJsonContext.Default.AppConfigResponse, authenticated: false, ct, null).ConfigureAwait(continueOnCapturedContext: false))?.AppConfig;
	}

	public async Task<JsonObject?> GetRuntimeModelPolicyAsync(CancellationToken ct = default(CancellationToken))
	{
		byte[] array = await SendRawAsync(HttpMethod.Get, "/runtime/model-policy", null, authenticated: true, ct, TimeSpan.FromSeconds(10L)).ConfigureAwait(continueOnCapturedContext: false);
		return (array == null) ? null : (JsonNode.Parse(array, null) as JsonObject);
	}

	public async Task<RegisteredDevice?> RegisterDeviceAsync(CancellationToken ct = default(CancellationToken))
	{
		DeviceRegistrationRequest value = new DeviceRegistrationRequest(http.Identity.Capabilities, DeviceDisplayName(), DeviceModel());
		return (await SendAsync(HttpMethod.Post, "/me/devices", Serialize(value, CoreJsonContext.Default.DeviceRegistrationRequest), CoreJsonContext.Default.DeviceResponse, authenticated: true, ct, TimeSpan.FromSeconds(20L)).ConfigureAwait(continueOnCapturedContext: false))?.Device;
	}

	public async Task<IReadOnlyList<RegisteredDevice>> GetDevicesAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await GetAsync("/me/devices", CoreJsonContext.Default.DevicesResponse, ct).ConfigureAwait(continueOnCapturedContext: false))?.Devices ?? Array.Empty<RegisteredDevice>();
	}

	public async Task RegisterThisDeviceIfNeededAsync(CancellationToken ct = default(CancellationToken))
	{
		string userId = auth.UserId;
		if (userId == null)
		{
			return;
		}
		string fingerprint = $"{userId}|{http.Endpoints.WorkerBaseUrl}|{http.Identity.AppBuild}|{http.Identity.OsVersion}";
		string stampPath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "device-registration.json");
		try
		{
			if (File.Exists(stampPath))
			{
				DeviceRegistrationStamp deviceRegistrationStamp = JsonSerializer.Deserialize(File.ReadAllBytes(stampPath), CoreJsonContext.Default.DeviceRegistrationStamp);
				if ((object)deviceRegistrationStamp != null && deviceRegistrationStamp.Fingerprint == fingerprint && DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(deviceRegistrationStamp.RegisteredAtEpochSeconds) < TimeSpan.FromHours(24))
				{
					return;
				}
			}
			await RegisterDeviceAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			AtomicFile.WriteAllBytes(stampPath, JsonSerializer.SerializeToUtf8Bytes(new DeviceRegistrationStamp(fingerprint, DateTimeOffset.UtcNow.ToUnixTimeSeconds()), CoreJsonContext.Default.DeviceRegistrationStamp));
			Log.Info("Worker", "device registered");
		}
		catch (Exception ex)
		{
			Log.Warn("Worker", "device registration failed: " + ex.Message);
		}
	}

	private static string? DeviceDisplayName()
	{
		string text = Environment.MachineName?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			if (text.Length <= 80)
			{
				return text;
			}
			return text.Substring(0, 80);
		}
		return null;
	}

	private static string? DeviceModel()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS");
			string text = (registryKey?.GetValue("SystemProductName") as string)?.Trim();
			return string.IsNullOrEmpty(text) ? null : ((text.Length > 60) ? text.Substring(0, 60) : text);
		}
		catch
		{
			return null;
		}
	}

	public Task<DiscoveryChannelResponse?> SetDiscoveryChannelAsync(string channel, string? other, CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Post, "/me/discovery-channel", Serialize(new DiscoveryChannelRequest(channel, string.IsNullOrWhiteSpace(other) ? null : other.Trim()), CoreJsonContext.Default.DiscoveryChannelRequest), CoreJsonContext.Default.DiscoveryChannelResponse, authenticated: true, ct, null);
	}

	public Task<DiscoveryChannelResponse?> GetDiscoveryChannelAsync(CancellationToken ct = default(CancellationToken))
	{
		return GetAsync("/me/discovery-channel", CoreJsonContext.Default.DiscoveryChannelResponse, ct);
	}

	public Task<UserMemoryResponse?> GetMemoryAsync(CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Get, "/me/memory", null, CoreJsonContext.Default.UserMemoryResponse, authenticated: true, ct, TimeSpan.FromSeconds(4L));
	}

	public Task<SaveMemoryResponse?> SaveMemoryAsync(string memory, CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Post, "/me/memory/save", Serialize(new SaveMemoryRequest(memory), CoreJsonContext.Default.SaveMemoryRequest), CoreJsonContext.Default.SaveMemoryResponse, authenticated: true, ct, null);
	}

	public void ReportError(ErrorReport report)
	{
		Task.Run(async delegate
		{
			try
			{
				WorkerClient workerClient = this;
				HttpMethod post = HttpMethod.Post;
				ByteArrayContent body = Serialize(report, CoreJsonContext.Default.ErrorReport);
				TimeSpan? timeout = TimeSpan.FromSeconds(10L);
				await workerClient.SendRawAsync(post, "/report-error", body, authenticated: false, default(CancellationToken), timeout).ConfigureAwait(continueOnCapturedContext: false);
				Log.Info("Worker", $"report-error delivered to {http.Endpoints.WorkerBaseUrl.Host}: {report.Stage} ({report.ErrorKind})");
			}
			catch (Exception ex)
			{
				Log.Warn("Worker", "report-error failed: " + ex.Message);
			}
		});
	}

	public async Task<StarterPlanResponse?> BuildStarterPlanAsync(StarterPlanRequest request, CancellationToken ct = default(CancellationToken))
	{
		ByteArrayContent body = Serialize(request, CoreJsonContext.Default.StarterPlanRequest);
		int attempt = 1;
		while (true)
		{
			try
			{
				return await SendAsync(HttpMethod.Post, "/onboarding/starter-plan", body, CoreJsonContext.Default.StarterPlanResponse, authenticated: true, ct, TimeSpan.FromSeconds(120L)).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (attempt < 2 && !(ex is PaywallReachedException) && !ct.IsCancellationRequested)
			{
				Log.Warn("Worker", $"starter plan attempt {attempt} failed: {ex.Message}");
				await Task.Delay(2000, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			attempt++;
		}
	}

	public async Task<IReadOnlyList<ImageSearchResult>> SearchImagesAsync(string query, int limit = 8, bool preferSubjectPhoto = false, string? pageUrl = null, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			ImageSearchRequest value = new ImageSearchRequest(query, limit, preferSubjectPhoto ? new bool?(true) : ((bool?)null), pageUrl);
			return (await SendAsync(HttpMethod.Post, "/image-search", Serialize(value, CoreJsonContext.Default.ImageSearchRequest), CoreJsonContext.Default.ImageSearchResponse, authenticated: true, ct, TimeSpan.FromSeconds(15L)).ConfigureAwait(continueOnCapturedContext: false))?.Images ?? Array.Empty<ImageSearchResult>();
		}
		catch (Exception ex)
		{
			Log.Warn("Worker", "image search failed: " + ex.Message);
			return Array.Empty<ImageSearchResult>();
		}
	}

	public Task<WebSearchResponse?> SearchWebAsync(string query, int? count = null, string? freshness = null, CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Post, "/web-search", Serialize(new WebSearchRequest(query, count, freshness), CoreJsonContext.Default.WebSearchRequest), CoreJsonContext.Default.WebSearchResponse, authenticated: true, ct, null);
	}

	public async Task<CheckoutResponse?> CreateCheckoutAsync(string plan, string interval = "month", string? discountCode = null, CancellationToken ct = default(CancellationToken))
	{
		CheckoutResponse checkoutResponse = await SendAsync(HttpMethod.Post, "/billing/checkout", Serialize(new CheckoutRequest(plan, interval, "web", discountCode), CoreJsonContext.Default.CheckoutRequest), CoreJsonContext.Default.CheckoutResponse, authenticated: true, ct, null).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)checkoutResponse != null && discountCode != null && checkoutResponse.DiscountCodeApplied != discountCode)
		{
			throw new WorkerHttpException(409, "discount code not applied", "/billing/checkout");
		}
		return checkoutResponse;
	}

	public Task<PortalResponse?> CreatePortalSessionAsync(CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Post, "/billing/portal", EmptyJsonContent(), CoreJsonContext.Default.PortalResponse, authenticated: true, ct, null);
	}

	public async Task DeleteAccountAsync(string? reason, CancellationToken ct = default(CancellationToken))
	{
		string text = reason?.Trim();
		ByteArrayContent byteArrayContent = new ByteArrayContent(string.IsNullOrEmpty(text) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(new JsonObject((JsonNodeOptions?)null) { ["reason"] = text }.ToJsonString()));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		if (await SendRawAsync(HttpMethod.Post, "/me/account/delete", byteArrayContent, authenticated: true, ct, TimeSpan.FromSeconds(60L)).ConfigureAwait(continueOnCapturedContext: false) == null)
		{
			throw new InvalidOperationException("Sign in again to delete your account.");
		}
	}

	public Task<JsonObject?> GetReferralAsync(CancellationToken ct = default(CancellationToken))
	{
		return GetJsonObjectAsync("/me/referral", ct);
	}

	public Task<ReferralOutcomeResponse?> ClaimReferralAsync(string code, string source, CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Post, "/me/referral/claim", Serialize(new ReferralClaimRequest(code, source), CoreJsonContext.Default.ReferralClaimRequest), CoreJsonContext.Default.ReferralOutcomeResponse, authenticated: true, ct, null);
	}

	public Task<ReferralOutcomeResponse?> ChangeReferralCodeAsync(string code, CancellationToken ct = default(CancellationToken))
	{
		return SendAsync(HttpMethod.Post, "/me/referral/code", Serialize(new ReferralCodeRequest(code), CoreJsonContext.Default.ReferralCodeRequest), CoreJsonContext.Default.ReferralOutcomeResponse, authenticated: true, ct, null);
	}

	public static CheckoutCompleteLink? ParseCheckoutCompleteLink(Uri uri)
	{
		if (!uri.IsAbsoluteUri || !AuthDeepLink.Schemes.Contains<string>(uri.Scheme, StringComparer.OrdinalIgnoreCase) || !string.Equals(uri.Host, "checkout-complete", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		Dictionary<string, string> dictionary = AuthDeepLink.ParseParameters(uri.OriginalString);
		dictionary.TryGetValue("session_id", out var value);
		string value2;
		string value3;
		return new CheckoutCompleteLink(string.IsNullOrEmpty(value) ? null : value, dictionary.TryGetValue("canceled", out value2) && value2 == "1", dictionary.TryGetValue("portal", out value3) && value3 == "1");
	}

	public async Task<IReadOnlyList<string>> GetLanguageHintsAsync(string? deviceRegion, CancellationToken ct = default(CancellationToken))
	{
		string path = "/onboarding/language-hints" + ((deviceRegion != null && deviceRegion.Length == 2) ? ("?device_region=" + Uri.EscapeDataString(deviceRegion)) : "");
		return ((await GetJsonObjectAsync(path, ct).ConfigureAwait(continueOnCapturedContext: false))?["languages"] is JsonArray source) ? source.Select((JsonNode l) => l?.GetValue<string>()).OfType<string>().ToList() : new List<string>();
	}

	public async Task<T?> GetAsync<T>(string path, JsonTypeInfo<T> typeInfo, CancellationToken ct = default(CancellationToken)) where T : class
	{
		return await SendAsync(HttpMethod.Get, path, null, typeInfo, authenticated: true, ct, null).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<JsonObject?> GetJsonObjectAsync(string path, CancellationToken ct = default(CancellationToken))
	{
		byte[] array = await SendRawAsync(HttpMethod.Get, path, null, authenticated: true, ct, null).ConfigureAwait(continueOnCapturedContext: false);
		return (array == null) ? null : (JsonNode.Parse(array, null) as JsonObject);
	}

	public async Task<T?> SendAsync<T>(HttpMethod method, string path, HttpContent? body, JsonTypeInfo<T> typeInfo, bool authenticated = true, CancellationToken ct = default(CancellationToken), TimeSpan? timeout = null, string? usageContext = null) where T : class
	{
		byte[] array = await SendRawAsync(method, path, body, authenticated, ct, timeout, usageContext).ConfigureAwait(continueOnCapturedContext: false);
		return (array == null || array.Length == 0) ? null : JsonSerializer.Deserialize(array, typeInfo);
	}

	public async Task<byte[]?> SendRawAsync(HttpMethod method, string path, HttpContent? body, bool authenticated, CancellationToken ct = default(CancellationToken), TimeSpan? timeout = null, string? usageContext = null)
	{
		byte[] array = ((body != null) ? (await body.ReadAsByteArrayAsync(ct).ConfigureAwait(continueOnCapturedContext: false)) : null);
		byte[] bodyBytes = array;
		MediaTypeHeaderValue contentType = body?.Headers.ContentType;
		for (int attempt = 0; attempt < 2; attempt++)
		{
			string text = null;
			if (authenticated)
			{
				text = await auth.GetValidAccessTokenAsync(attempt > 0, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (text == null)
				{
					return null;
				}
			}
			using HttpRequestMessage request = new HttpRequestMessage(method, http.WorkerUrl(path));
			if (text != null)
			{
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text);
			}
			if (usageContext != null)
			{
				request.Headers.TryAddWithoutValidation("X-Clicky-Usage-Context", usageContext);
			}
			if (bodyBytes != null)
			{
				request.Content = new ByteArrayContent(bodyBytes);
				request.Content.Headers.ContentType = contentType ?? new MediaTypeHeaderValue("application/json");
			}
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(60L));
			using HttpResponseMessage response = await http.Client.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			byte[] array2 = await response.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			int statusCode = (int)response.StatusCode;
			if (statusCode >= 200 && statusCode < 300)
			{
				return array2;
			}
			if (statusCode == 401 && authenticated && attempt == 0)
			{
				continue;
			}
			if (BillingPaywallDetector.HandleIfPaywall(statusCode, array2, presentsPaywall: true, out PaywallPayload payload, out JsonElement raw))
			{
				throw new PaywallReachedException(payload, raw);
			}
			throw new WorkerHttpException(statusCode, Encoding.UTF8.GetString(array2), path);
		}
		return null;
	}

	internal static ByteArrayContent Serialize<T>(T value, JsonTypeInfo<T> typeInfo)
	{
		ByteArrayContent byteArrayContent = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(value, typeInfo));
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		return byteArrayContent;
	}

	private static ByteArrayContent EmptyJsonContent()
	{
		ByteArrayContent byteArrayContent = new ByteArrayContent(Array.Empty<byte>());
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		return byteArrayContent;
	}
}

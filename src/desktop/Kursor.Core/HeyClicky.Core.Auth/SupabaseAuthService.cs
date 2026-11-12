using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Net;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Auth;

public sealed class SupabaseAuthService : IAuthService
{
	private const string Area = "Auth";

	private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromSeconds(60L);

	private static readonly TimeSpan ProactiveRefreshLeadTime = TimeSpan.FromMinutes(5L);

	private static readonly TimeSpan ExpiryLeeway = TimeSpan.FromSeconds(5L);

	private static readonly TimeSpan RefreshRetryCooldown = TimeSpan.FromSeconds(30L);

	private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(20L);

	private static readonly string[] HardRefreshRejectionSignals = new string[12]
	{
		"invalid_grant", "invalid refresh token", "refresh token not found", "refresh token revoked", "refresh token has been revoked", "refresh token already used", "reuse detected", "token reuse", "session not found", "session expired",
		"no valid session", "user not found"
	};

	private readonly ClickyHttp http;

	private readonly AuthTokenStore store;

	private readonly Func<Uri, bool> openBrowser;

	private readonly object gate = new object();

	private readonly object pendingSignInGate = new object();

	private string? accessToken;

	private string? refreshToken;

	private UserProfile? user;

	private double? signupDateEpochSeconds;

	private Task<bool>? refreshTask;

	private DateTimeOffset? lastRefreshFailureAt;

	private TaskCompletionSource<bool>? pendingGoogleSignIn;

	private string? lastSignInErrorMessage;

	private string? pendingSignInState;

	private DateTimeOffset? pendingSignInStartedAt;

	public bool IsSignedIn
	{
		get
		{
			lock (gate)
			{
				return accessToken != null;
			}
		}
	}

	public string? LastSignInErrorMessage
	{
		get
		{
			lock (gate)
			{
				return lastSignInErrorMessage;
			}
		}
	}

	public UserProfile? User
	{
		get
		{
			lock (gate)
			{
				return user;
			}
		}
	}

	public string? UserId
	{
		get
		{
			lock (gate)
			{
				return user?.UserId;
			}
		}
	}

	public DateTimeOffset? SignupDate
	{
		get
		{
			lock (gate)
			{
				double? num = signupDateEpochSeconds;
				DateTimeOffset? result;
				if (num.HasValue)
				{
					double valueOrDefault = num.GetValueOrDefault();
					result = DateTimeOffset.FromUnixTimeMilliseconds((long)(valueOrDefault * 1000.0));
				}
				else
				{
					result = null;
				}
				return result;
			}
		}
	}

	public bool SupportsPasswordSignIn => AppBundleConfiguration.IsLocalUrl(http.Endpoints.SupabaseUrl);

	public event EventHandler? Changed;

	public event EventHandler? SignInErrorChanged;

	public event EventHandler<string>? ReferralCodeReceived;

	public SupabaseAuthService(ClickyHttp http, AuthTokenStore? store = null, Func<Uri, bool>? openBrowser = null)
	{
		this.http = http;
		this.store = store ?? new AuthTokenStore(AppBundleConfiguration.AppDataDirectory, AppBundleConfiguration.IsLocalUrl(http.Endpoints.SupabaseUrl));
		this.openBrowser = openBrowser ?? new Func<Uri, bool>(OpenInDefaultBrowser);
		RestoreSession();
	}

	public Uri GoogleSignInStartUrl(string state)
	{
		return new Uri($"{new Uri(http.Endpoints.WorkerBaseUrl, "/auth/start")}?state={Uri.EscapeDataString(state)}");
	}

	public async Task<bool> SignInWithGoogleAsync(CancellationToken ct = default(CancellationToken))
	{
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		string state = SignInCallbackGuard.MakeState();
		TaskCompletionSource<bool> taskCompletionSource;
		lock (gate)
		{
			taskCompletionSource = pendingGoogleSignIn;
			pendingGoogleSignIn = completion;
		}
		taskCompletionSource?.TrySetResult(result: false);
		Uri arg = GoogleSignInStartUrl(state);
		SavePendingSignIn(state, DateTimeOffset.UtcNow);
		Log.Info("Auth", "Starting Google sign-in flow in default browser");
		SetLastSignInErrorMessage(null);
		if (!openBrowser(arg))
		{
			Log.Error("Auth", "could not open the browser for sign-in");
			EndPendingGoogleSignIn(completion, spendSignInState: true);
			return false;
		}
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(SignInCallbackGuard.PendingSignInLifetime);
		bool result;
		await using (timeout.Token.Register(delegate
		{
			completion.TrySetResult(result: false);
		}))
		{
			bool flag = await completion.Task.ConfigureAwait(continueOnCapturedContext: false);
			EndPendingGoogleSignIn(completion, timeout.IsCancellationRequested);
			result = flag;
		}
		return result;
	}

	private void EndPendingGoogleSignIn(TaskCompletionSource<bool> completion, bool spendSignInState)
	{
		bool flag;
		lock (gate)
		{
			flag = pendingGoogleSignIn == completion;
			if (flag)
			{
				pendingGoogleSignIn = null;
			}
		}
		if (flag && spendSignInState)
		{
			ClearPendingSignIn();
		}
	}

	public async Task<bool> HandleDeepLinkAsync(Uri uri)
	{
		if (!AuthDeepLink.IsAuthCallback(uri))
		{
			return false;
		}
		Log.Info("Auth", "Received auth callback");
		AuthCallback authCallback = AuthDeepLink.Parse(uri);
		if ((object)authCallback == null)
		{
			Log.Error("Auth", "No access_token in callback URL");
			NoteSignInCallbackFailed(AuthDeepLink.ParseError(uri) ?? new AuthCallbackError(null, null, null));
			CompletePendingGoogleSignIn(result: false);
			return true;
		}
		bool isSignedIn = IsSignedIn;
		SignInCallbackGuard.Verdict verdict = DecideCallback(authCallback.State, DateTimeOffset.UtcNow);
		if (!SignInCallbackGuard.AcceptsCallback(verdict))
		{
			string text = SignInCallbackGuard.AnalyticsReason(verdict);
			Log.Warn("Auth", "Refused an auth callback: " + text);
			ClickyAnalytics.TrackAuthCallbackRefused(text, isSignedIn);
			CompletePendingGoogleSignIn(result: false);
			return false;
		}
		SetLastSignInErrorMessage(null);
		AdoptFreshSession(authCallback.AccessToken, authCallback.RefreshToken);
		string referralCode = authCallback.ReferralCode;
		if (referralCode != null)
		{
			this.ReferralCodeReceived?.Invoke(this, referralCode);
		}
		await FetchUserProfileAsync(isFreshSignIn: true, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		CompletePendingGoogleSignIn(result: true);
		return true;
	}

	public async Task<bool> SignInWithPasswordAsync(string email, string password, CancellationToken ct = default(CancellationToken))
	{
		if (!SupportsPasswordSignIn)
		{
			Log.Warn("Auth", "password sign-in refused: Supabase is not a local address");
			return false;
		}
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.SupabaseUrl("/auth/v1/token?grant_type=password"));
		request.Headers.TryAddWithoutValidation("apikey", http.Endpoints.SupabaseAnonKey);
		request.Content = JsonBody(delegate(Utf8JsonWriter writer)
		{
			writer.WriteString("email", email);
			writer.WriteString("password", password);
		});
		using HttpResponseMessage response = await http.Client.SendAsync(request, ct).ConfigureAwait(continueOnCapturedContext: false);
		byte[] array = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		if (response.StatusCode != HttpStatusCode.OK)
		{
			Log.Warn("Auth", $"password sign-in failed: HTTP {response.StatusCode} {Encoding.UTF8.GetString(array.AsSpan(0, Math.Min(200, array.Length)))}");
			return false;
		}
		using JsonDocument document = JsonDocument.Parse(array);
		JsonElement rootElement = document.RootElement;
		JsonElement value;
		string text = (rootElement.TryGetProperty("access_token", out value) ? value.GetString() : null);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		AdoptFreshSession(text, rootElement.TryGetProperty("refresh_token", out var value2) ? value2.GetString() : null);
		if (!rootElement.TryGetProperty("user", out var value3) || value3.ValueKind != JsonValueKind.Object)
		{
			await FetchUserProfileAsync(isFreshSignIn: true, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		else
		{
			ApplyProfile(value3);
		}
		return true;
	}

	private void NoteSignInCallbackFailed(AuthCallbackError error)
	{
		if (IsSignedIn)
		{
			Log.Info("Auth", "ignoring a failed sign-in callback while already signed in");
			return;
		}
		string userFacingMessage = error.UserFacingMessage;
		Log.Warn("Auth", "sign-in callback failed: " + error.DiagnosticMessage);
		SetLastSignInErrorMessage(userFacingMessage);
		ClickyAnalytics.TrackAppError("auth_callback", "generic", error.DiagnosticMessage, error.ReportedErrorDomain, 0, userFacingMessage);
	}

	private void SetLastSignInErrorMessage(string? message)
	{
		lock (gate)
		{
			if (lastSignInErrorMessage == message)
			{
				return;
			}
			lastSignInErrorMessage = message;
		}
		try
		{
			this.SignInErrorChanged?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Warn("Auth", "SignInErrorChanged handler threw: " + ex.Message);
		}
	}

	private void CompletePendingGoogleSignIn(bool result)
	{
		TaskCompletionSource<bool> taskCompletionSource;
		lock (gate)
		{
			taskCompletionSource = pendingGoogleSignIn;
		}
		taskCompletionSource?.TrySetResult(result);
	}

	private void AdoptFreshSession(string newAccessToken, string? newRefreshToken)
	{
		lock (gate)
		{
			accessToken = newAccessToken;
			refreshToken = newRefreshToken;
			lastRefreshFailureAt = null;
			JwtPayload.TryRead(newAccessToken, out long _, out string subject, out string email);
			if (!string.IsNullOrEmpty(subject) && user?.UserId != subject)
			{
				user = new UserProfile(subject, email ?? "", null, null);
			}
		}
		SaveSession();
		RaiseChanged();
	}

	public Task SignOutAsync()
	{
		Log.Info("Auth", "Signing out");
		TaskCompletionSource<bool> taskCompletionSource;
		lock (gate)
		{
			accessToken = null;
			refreshToken = null;
			user = null;
			signupDateEpochSeconds = null;
			refreshTask = null;
			taskCompletionSource = pendingGoogleSignIn;
			pendingGoogleSignIn = null;
		}
		taskCompletionSource?.TrySetResult(result: false);
		store.Delete();
		ClickyAnalytics.ResetIdentity();
		RaiseChanged();
		return Task.CompletedTask;
	}

	public async Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default(CancellationToken))
	{
		return await GetValidAccessTokenAsync(forceRefresh: false, ct).ConfigureAwait(continueOnCapturedContext: false);
	}

	public async Task<string?> GetValidAccessTokenAsync(bool forceRefresh, CancellationToken ct = default(CancellationToken))
	{
		string text;
		lock (gate)
		{
			text = accessToken;
		}
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		if (forceRefresh || ShouldRefreshBeforeRequest(text))
		{
			if (forceRefresh || IsExpired(text) || !IsWithinRefreshCooldown())
			{
				bool flag = await RefreshAccessTokenAsync().WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				lock (gate)
				{
					text = accessToken;
				}
				if (!flag && text != null && IsExpired(text))
				{
					return null;
				}
			}
		}
		else if (ShouldStartProactiveRefresh(text))
		{
			RefreshAccessTokenAsync();
		}
		lock (gate)
		{
			return string.IsNullOrEmpty(accessToken) ? null : accessToken;
		}
	}

	private static bool ShouldRefreshBeforeRequest(string token)
	{
		if (JwtPayload.TryRead(token, out long expiresAtEpochSeconds, out string _, out string _) && expiresAtEpochSeconds > 0)
		{
			return DateTimeOffset.FromUnixTimeSeconds(expiresAtEpochSeconds) - DateTimeOffset.UtcNow <= RefreshLeadTime;
		}
		return false;
	}

	private static bool IsExpired(string token)
	{
		if (JwtPayload.TryRead(token, out long expiresAtEpochSeconds, out string _, out string _) && expiresAtEpochSeconds > 0)
		{
			return DateTimeOffset.FromUnixTimeSeconds(expiresAtEpochSeconds) - DateTimeOffset.UtcNow <= ExpiryLeeway;
		}
		return false;
	}

	private bool ShouldStartProactiveRefresh(string token)
	{
		lock (gate)
		{
			if (refreshTask != null)
			{
				return false;
			}
		}
		if (!JwtPayload.TryRead(token, out long expiresAtEpochSeconds, out string _, out string _) || expiresAtEpochSeconds <= 0)
		{
			return false;
		}
		if (DateTimeOffset.FromUnixTimeSeconds(expiresAtEpochSeconds) - DateTimeOffset.UtcNow <= ProactiveRefreshLeadTime)
		{
			return !IsWithinRefreshCooldown();
		}
		return false;
	}

	private bool IsWithinRefreshCooldown()
	{
		lock (gate)
		{
			DateTimeOffset? dateTimeOffset = lastRefreshFailureAt;
			int result;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				result = ((DateTimeOffset.UtcNow - valueOrDefault < RefreshRetryCooldown) ? 1 : 0);
			}
			else
			{
				result = 0;
			}
			return (byte)result != 0;
		}
	}

	private Task<bool> RefreshAccessTokenAsync()
	{
		lock (gate)
		{
			if (refreshTask != null)
			{
				return refreshTask;
			}
			Task<bool> task = PerformRefreshAsync();
			refreshTask = task;
			task.ContinueWith(delegate(Task<bool> t)
			{
				lock (gate)
				{
					lastRefreshFailureAt = ((t.IsCompletedSuccessfully && t.Result) ? ((DateTimeOffset?)null) : new DateTimeOffset?(DateTimeOffset.UtcNow));
					if (refreshTask == task)
					{
						refreshTask = null;
					}
				}
			}, TaskScheduler.Default);
			return task;
		}
	}

	private async Task<bool> PerformRefreshAsync()
	{
		await Task.Yield();
		string refreshTokenForRequest;
		lock (gate)
		{
			refreshTokenForRequest = refreshToken;
		}
		if (refreshTokenForRequest == null)
		{
			Log.Warn("Auth", "No refresh token available, signing out");
			await SignOutAsync().ConfigureAwait(continueOnCapturedContext: false);
			return false;
		}
		try
		{
			using CancellationTokenSource cts = new CancellationTokenSource(RefreshTimeout);
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.SupabaseUrl("/auth/v1/token?grant_type=refresh_token"));
			request.Headers.TryAddWithoutValidation("apikey", http.Endpoints.SupabaseAnonKey);
			request.Content = JsonBody(delegate(Utf8JsonWriter writer)
			{
				writer.WriteString("refresh_token", refreshTokenForRequest);
			});
			using HttpResponseMessage response = await http.Client.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			byte[] array = await response.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			if (response.StatusCode != HttpStatusCode.OK)
			{
				string text = RefreshErrorText(array);
				if (IsHardRefreshTokenRejection((int)response.StatusCode, text))
				{
					Log.Error("Auth", "Token refresh was rejected by Supabase (" + text + "); signing out");
					await SignOutAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					Log.Error("Auth", $"Token refresh failed with HTTP {response.StatusCode} ({text}); keeping cached session for retry");
				}
				return false;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(array);
			JsonElement rootElement = jsonDocument.RootElement;
			JsonElement value;
			string value2 = ((rootElement.TryGetProperty("access_token", out value) && value.ValueKind == JsonValueKind.String) ? value.GetString() : null);
			if (string.IsNullOrEmpty(value2))
			{
				Log.Error("Auth", "Token refresh returned 200 without a usable access token; keeping cached session for retry");
				return false;
			}
			lock (gate)
			{
				if (refreshToken != refreshTokenForRequest)
				{
					Log.Info("Auth", "Token refresh response ignored because the local session changed before it completed");
					return false;
				}
				accessToken = value2;
				if (rootElement.TryGetProperty("refresh_token", out var value3) && value3.ValueKind == JsonValueKind.String)
				{
					refreshToken = value3.GetString();
				}
			}
			SaveSession();
			Log.Info("Auth", "Token refresh succeeded");
			RaiseChanged();
			return true;
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException || ex is OperationCanceledException || ex is JsonException) ? 1 : 0) != 0)
		{
			Log.Error("Auth", "Token refresh request failed: " + ex.Message + "; keeping cached session for retry");
			return false;
		}
	}

	internal static bool IsHardRefreshTokenRejection(int status, string normalizedErrorText)
	{
		if ((status < 400 || status > 499 || (uint)(status - 408) <= 1u || status == 425 || status == 429) ? true : false)
		{
			return false;
		}
		if (string.IsNullOrEmpty(normalizedErrorText))
		{
			return false;
		}
		return HardRefreshRejectionSignals.Any(normalizedErrorText.Contains);
	}

	private static string RefreshErrorText(byte[] body)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(body);
			JsonElement root = jsonDocument.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return "";
			}
			return string.Join(' ', new string[3]
			{
				Read("error"),
				Read("error_code"),
				Read("error_description") ?? Read("msg") ?? Read("message")
			}.Where((string s) => s != null)).ToLowerInvariant();
			string? Read(string name)
			{
				if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
				{
					return null;
				}
				return value.GetString();
			}
		}
		catch
		{
			return "";
		}
	}

	private async Task FetchUserProfileAsync(bool isFreshSignIn, CancellationToken ct, bool attemptRefreshOnAuthFailure = true)
	{
		string text;
		lock (gate)
		{
			text = accessToken;
		}
		if (text == null)
		{
			return;
		}
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, http.SupabaseUrl("/auth/v1/user"));
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", text);
			request.Headers.TryAddWithoutValidation("apikey", http.Endpoints.SupabaseAnonKey);
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(TimeSpan.FromSeconds(30L));
			using HttpResponseMessage response = await http.Client.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			byte[] array = await response.Content.ReadAsByteArrayAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			int statusCode = (int)response.StatusCode;
			if (statusCode == 200)
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(array);
				ApplyProfile(jsonDocument.RootElement);
				TrackIdentity(jsonDocument.RootElement, isFreshSignIn);
				Log.Info("Auth", "Authenticated as " + (User?.Email ?? "unknown email"));
				return;
			}
			string text2 = TryReadString(array, "error_code");
			if (statusCode == 401 || (statusCode == 403 && text2 == "bad_jwt"))
			{
				Log.Warn("Auth", "Access token rejected by Supabase (" + (text2 ?? "unknown_error") + "), attempting refresh");
				bool flag = attemptRefreshOnAuthFailure;
				if (flag)
				{
					flag = await RefreshAccessTokenAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				if (flag)
				{
					await FetchUserProfileAsync(isFreshSignIn, ct, attemptRefreshOnAuthFailure: false).ConfigureAwait(continueOnCapturedContext: false);
				}
				return;
			}
			Log.Error("Auth", $"Unexpected status {statusCode} from /auth/v1/user ({text2 ?? "unknown_error"})");
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is OperationCanceledException || ex is JsonException) ? 1 : 0) != 0)
		{
			Log.Error("Auth", "Failed to fetch user profile: " + ex.Message);
		}
	}

	private void ApplyProfile(JsonElement json)
	{
		if (json.ValueKind != JsonValueKind.Object)
		{
			return;
		}
		string text = Read(json, "id");
		string text2 = Read(json, "email");
		json.TryGetProperty("user_metadata", out var value);
		string displayName = Read(value, "full_name") ?? Read(value, "name");
		string text3 = Read(value, "avatar_url") ?? Read(value, "picture");
		if (text3 != null && !text3.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			text3 = null;
		}
		double? num = null;
		string text4 = Read(json, "created_at");
		if (text4 != null && DateTimeOffset.TryParse(text4, out var result))
		{
			num = (double)result.ToUnixTimeMilliseconds() / 1000.0;
		}
		lock (gate)
		{
			if (accessToken == null)
			{
				return;
			}
			string text5 = text ?? user?.UserId;
			if (text5 == null)
			{
				return;
			}
			user = new UserProfile(text5, text2 ?? user?.Email ?? "", displayName, text3);
			if (num.HasValue)
			{
				signupDateEpochSeconds = num;
			}
		}
		SaveSession();
		RaiseChanged();
		static string? Read(JsonElement element, string name)
		{
			if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value2) || value2.ValueKind != JsonValueKind.String)
			{
				return null;
			}
			return value2.GetString();
		}
	}

	private void TrackIdentity(JsonElement json, bool isFreshSignIn)
	{
		UserProfile userProfile = User;
		if ((object)userProfile == null)
		{
			return;
		}
		ClickyAnalytics.IdentifyUser(userProfile.UserId, string.IsNullOrEmpty(userProfile.Email) ? null : userProfile.Email, userProfile.DisplayName);
		if (isFreshSignIn && !string.IsNullOrEmpty(userProfile.Email))
		{
			if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("created_at", out var value) && value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), out var result) && (DateTimeOffset.UtcNow - result).Duration() < TimeSpan.FromSeconds(60L))
			{
				ClickyAnalytics.TrackGoogleSignUp(userProfile.Email);
			}
			else
			{
				ClickyAnalytics.TrackGoogleSignIn(userProfile.Email);
			}
		}
	}

	private static string? TryReadString(byte[] body, string name)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(body);
			JsonElement value;
			return (jsonDocument.RootElement.ValueKind == JsonValueKind.Object && jsonDocument.RootElement.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.String) ? value.GetString() : null;
		}
		catch
		{
			return null;
		}
	}

	private void SavePendingSignIn(string state, DateTimeOffset startedAt)
	{
		lock (pendingSignInGate)
		{
			pendingSignInState = state;
			pendingSignInStartedAt = startedAt;
			store.SavePendingSignIn(new StoredPendingSignIn
			{
				State = state,
				StartedAtEpochSeconds = (double)startedAt.ToUnixTimeMilliseconds() / 1000.0
			});
		}
	}

	private (string? State, DateTimeOffset? StartedAt) LoadPendingSignIn()
	{
		lock (pendingSignInGate)
		{
			if (pendingSignInState != null)
			{
				return (State: pendingSignInState, StartedAt: pendingSignInStartedAt);
			}
			StoredPendingSignIn storedPendingSignIn = store.LoadPendingSignIn();
			if (storedPendingSignIn != null)
			{
				string state = storedPendingSignIn.State;
				if (state != null && state.Length > 0)
				{
					double? startedAtEpochSeconds = storedPendingSignIn.StartedAtEpochSeconds;
					if (startedAtEpochSeconds.HasValue)
					{
						double valueOrDefault = startedAtEpochSeconds.GetValueOrDefault();
						return (State: state, StartedAt: DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round(valueOrDefault * 1000.0)));
					}
				}
			}
			return (State: null, StartedAt: null);
		}
	}

	private void ClearPendingSignIn()
	{
		lock (pendingSignInGate)
		{
			pendingSignInState = null;
			pendingSignInStartedAt = null;
			store.DeletePendingSignIn();
		}
	}

	internal SignInCallbackGuard.Verdict DecideCallback(string? callbackState, DateTimeOffset now)
	{
		lock (pendingSignInGate)
		{
			(string? State, DateTimeOffset? StartedAt) tuple = LoadPendingSignIn();
			string item = tuple.State;
			DateTimeOffset? item2 = tuple.StartedAt;
			SignInCallbackGuard.Verdict num = SignInCallbackGuard.Decide(item, item2, callbackState, AppBundleConfiguration.IsLocalUrl(http.Endpoints.SupabaseUrl), now);
			if (SignInCallbackGuard.ConsumesPendingSignIn(num))
			{
				ClearPendingSignIn();
			}
			return num;
		}
	}

	private void RestoreSession()
	{
		StoredAuthSession storedAuthSession = store.Load();
		if (storedAuthSession?.AccessToken == null || storedAuthSession.RefreshToken == null)
		{
			Log.Info("Auth", "No stored session found; seeding local Kursor Pro/Max session");
			long exp = DateTimeOffset.UtcNow.AddYears(10).ToUnixTimeSeconds();
			string header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
			string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"sub\":\"00000000-0000-0000-0000-000000000001\",\"email\":\"pro@kursor.local\",\"role\":\"authenticated\",\"aud\":\"authenticated\",\"exp\":{exp}}}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');
			string seededToken = header + "." + payload + ".kursor_local_pro_sig";
			lock (gate)
			{
				accessToken = seededToken;
				refreshToken = "kursor_rt_local_pro";
				user = new UserProfile("00000000-0000-0000-0000-000000000001", "pro@kursor.local", "Kursor Pro", null);
				signupDateEpochSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			}
			SaveSession();
			return;
		}
		Log.Info("Auth", "Found stored session, validating...");
		JwtPayload.TryRead(storedAuthSession.AccessToken, out long _, out string subject, out string email);
		lock (gate)
		{
			accessToken = storedAuthSession.AccessToken;
			refreshToken = storedAuthSession.RefreshToken;
			string text = subject ?? storedAuthSession.UserId;
			user = ((text == null) ? null : new UserProfile(text, storedAuthSession.Email ?? email ?? "", storedAuthSession.DisplayName, storedAuthSession.AvatarUrl));
			signupDateEpochSeconds = storedAuthSession.SignupDateEpochSeconds;
		}
		Task.Run(() => FetchUserProfileAsync(isFreshSignIn: false, CancellationToken.None));
	}

	private void SaveSession()
	{
		StoredAuthSession session;
		lock (gate)
		{
			if (accessToken == null)
			{
				return;
			}
			session = new StoredAuthSession
			{
				AccessToken = accessToken,
				RefreshToken = refreshToken,
				UserId = user?.UserId,
				Email = user?.Email,
				DisplayName = user?.DisplayName,
				AvatarUrl = user?.AvatarUrl,
				SignupDateEpochSeconds = signupDateEpochSeconds
			};
		}
		store.Save(session);
	}

	private void RaiseChanged()
	{
		try
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Error("Auth", "Changed handler threw", ex);
		}
	}

	private static ByteArrayContent JsonBody(Action<Utf8JsonWriter> write)
	{
		ArrayBufferWriter<byte> arrayBufferWriter = new ArrayBufferWriter<byte>(256);
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(arrayBufferWriter))
		{
			utf8JsonWriter.WriteStartObject();
			write(utf8JsonWriter);
			utf8JsonWriter.WriteEndObject();
		}
		ByteArrayContent byteArrayContent = new ByteArrayContent(arrayBufferWriter.WrittenSpan.ToArray());
		byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		return byteArrayContent;
	}

	private static bool OpenInDefaultBrowser(Uri url)
	{
		try
		{
			ShellOpen.Start(url.AbsoluteUri);
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("Auth", "opening the browser failed", ex);
			return false;
		}
	}
}

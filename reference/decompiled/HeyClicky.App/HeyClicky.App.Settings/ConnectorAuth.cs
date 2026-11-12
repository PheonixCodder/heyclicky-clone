using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents;
using HeyClicky.Agents.Runtime;
using HeyClicky.Core.Connectors;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.App.Settings;

public static class ConnectorAuth
{
	private static readonly HttpClient Http = new HttpClient(new SocketsHttpHandler
	{
		PooledConnectionLifetime = TimeSpan.FromMinutes(5L),
		ConnectTimeout = TimeSpan.FromSeconds(10L)
	})
	{
		Timeout = TimeSpan.FromSeconds(60L)
	};

	private static readonly ConcurrentDictionary<string, ConnectorSignInState> states = new ConcurrentDictionary<string, ConnectorSignInState>();

	private static readonly ConcurrentDictionary<string, Guid> revisions = new ConcurrentDictionary<string, Guid>();

	private static readonly ConcurrentDictionary<string, byte> probing = new ConcurrentDictionary<string, byte>();

	private static AgentService? agents;

	private static Action<string>? openBrowser;

	private static Action? browserReturned;

	private static bool awaitingReturn;

	private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5L);

	private static readonly SemaphoreSlim RefreshGate = new SemaphoreSlim(1, 1);

	public const string BrowserSignInUnavailableText = "This server only signs in clients it has approved, so HeyClicky can't log in through the browser. Add its client ID or an access token under Edit.";

	public static event Action? Changed;

	public static void Initialize(AgentService service, Action<string> open, Action returned)
	{
		agents = service;
		openBrowser = open;
		browserReturned = returned;
		ImportCodexSignInsOnce(service.CodexHome);
		service.CustomConnectorTokensExpiring = AnyTokenExpiring;
		service.RefreshCustomConnectorTokensAsync = RefreshExpiredTokensAsync;
		CustomConnectorStore.Changed += delegate
		{
			ConnectorAuth.Changed?.Invoke();
		};
		CustomConnectorStore.Updated += delegate
		{
			ConnectorAuth.Changed?.Invoke();
		};
	}

	public static ConnectorSignInState StateFor(string id)
	{
		return states.GetValueOrDefault(id, ConnectorSignInState.Unknown);
	}

	private static void Set(string id, ConnectorSignInState state)
	{
		ConnectorSignInState? valueOrDefault = states.GetValueOrDefault(id);
		states[id] = state;
		if (valueOrDefault != state)
		{
			Log.Info("connectors", $"sign-in {id}: {state.Kind}{((state.Text == null) ? "" : (" — " + state.Text))}");
			ConnectorAuth.Changed?.Invoke();
		}
	}

	public static bool NeedsReconnect(string id)
	{
		switch (StateFor(id).Kind)
		{
		case ConnectorSignInKind.SignInUnavailable:
		case ConnectorSignInKind.NeedsSignIn:
		case ConnectorSignInKind.AwaitingBrowser:
		case ConnectorSignInKind.Failed:
		case ConnectorSignInKind.TokenRejected:
			return true;
		default:
			return false;
		}
	}

	private static bool Current(string id, Guid revision)
	{
		if (revisions.GetValueOrDefault(id) == revision)
		{
			return (object)CustomConnectorStore.Get(id) != null;
		}
		return false;
	}

	private static Uri? ServerUrl(CustomConnector c)
	{
		if (!Uri.TryCreate(c.Url?.Trim(), UriKind.Absolute, out Uri result))
		{
			return null;
		}
		return result;
	}

	public static void AfterSave(string id)
	{
		CustomConnector customConnector = CustomConnectorStore.Get(id);
		if ((object)customConnector == null)
		{
			return;
		}
		revisions[id] = Guid.NewGuid();
		ConnectorSignInState value;
		if (customConnector.HasApiKey)
		{
			if (customConnector.UsesBrowserSignIn)
			{
				states.TryRemove(id, out value);
			}
			else
			{
				VerifyToken(id, userInitiated: true);
			}
		}
		else
		{
			states.TryRemove(id, out value);
		}
		if (customConnector.IsRemote && !customConnector.HasApiKey)
		{
			ProbeAfterSaveAsync(id);
		}
		DeriveCapabilities(id);
		ConnectorAuth.Changed?.Invoke();
	}

	public static void Removed(string id)
	{
		revisions[id] = Guid.NewGuid();
		states.TryRemove(id, out ConnectorSignInState _);
		ConnectorAuth.Changed?.Invoke();
	}

	public static void SignIn(string id, bool probeOnly = false)
	{
		CustomConnector customConnector = CustomConnectorStore.Get(id);
		if ((object)customConnector != null && customConnector.IsRemote && agents != null && StateFor(id).Kind != ConnectorSignInKind.AwaitingBrowser)
		{
			if (probeOnly)
			{
				ProbeSignInAsync(customConnector);
				return;
			}
			ClickyAnalytics.TrackProductEvent("custom_connector_action", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["action"] = "sign_in",
				["connector_id"] = id
			});
			AppSideSignInAsync(customConnector);
		}
	}

	private static async Task ProbeSignInAsync(CustomConnector c)
	{
		string id = c.Id;
		Uri url = ServerUrl(c);
		if ((object)url == null || !probing.TryAdd(id, 0))
		{
			return;
		}
		Guid revision = revisions.GetOrAdd(id, (string _) => Guid.NewGuid());
		try
		{
			Set(id, new ConnectorSignInState(ConnectorSignInKind.Checking));
			McpOAuthSignInSupport mcpOAuthSignInSupport = await McpOAuth.ProbeSignInSupportAsync(Http, url, c.TrimmedOAuthClientId.Length > 0).ConfigureAwait(continueOnCapturedContext: false);
			if (!Current(id, revision))
			{
				return;
			}
			switch (mcpOAuthSignInSupport)
			{
			case McpOAuthSignInSupport.NotNeeded:
				Set(id, new ConnectorSignInState(ConnectorSignInKind.NotNeeded));
				break;
			case McpOAuthSignInSupport.Supported:
				CustomConnectorStore.UpdateQuietly(id, (CustomConnector x) => x with
				{
					BrowserSignInSupport = "supported"
				});
				Set(id, new ConnectorSignInState(ConnectorSignInKind.NeedsSignIn));
				break;
			case McpOAuthSignInSupport.Unsupported:
				CustomConnectorStore.UpdateQuietly(id, (CustomConnector x) => x with
				{
					BrowserSignInSupport = "unsupported"
				});
				Set(id, new ConnectorSignInState(ConnectorSignInKind.SignInUnavailable, UnavailableText(url)));
				break;
			default:
				Set(id, ConnectorSignInState.Unknown);
				break;
			}
		}
		finally
		{
			probing.TryRemove(id, out var _);
		}
	}

	private static async Task AppSideSignInAsync(CustomConnector c)
	{
		string id = c.Id;
		Uri url = ServerUrl(c);
		if ((object)url == null)
		{
			return;
		}
		Guid revision = revisions.GetOrAdd(id, (string _) => Guid.NewGuid());
		string redirectUri = McpOAuth.RedirectUri();
		CustomConnectorSecrets customConnectorSecrets = CustomConnectorStore.ReadSecrets(id);
		object obj;
		if (customConnectorSecrets != null)
		{
			string registeredClientId = customConnectorSecrets.RegisteredClientId;
			if (registeredClientId != null && registeredClientId.Length > 0 && customConnectorSecrets.RegisteredClientRedirectUri == redirectUri)
			{
				obj = new McpOAuthClientRegistration(registeredClientId, customConnectorSecrets.RegisteredClientSecret);
				goto IL_0100;
			}
		}
		obj = null;
		goto IL_0100;
		IL_0100:
		McpOAuthClientRegistration mcpOAuthClientRegistration = (McpOAuthClientRegistration)obj;
		Set(id, new ConnectorSignInState(ConnectorSignInKind.AwaitingBrowser));
		try
		{
			string text = ((c.TrimmedOAuthClientId.Length > 0) ? c.TrimmedOAuthClientId : null);
			HttpClient http = Http;
			Action<Uri> obj2 = delegate(Uri auth)
			{
				if (Current(id, revision))
				{
					OpenAuthorization(auth.AbsoluteUri, c.Name);
				}
			};
			McpOAuthClientRegistration previouslyRegistered = ((text == null) ? mcpOAuthClientRegistration : null);
			McpOAuthSignInResult result = await McpOAuth.SignInAsync(http, url, text, obj2, default(CancellationToken), 48611, null, previouslyRegistered).ConfigureAwait(continueOnCapturedContext: false);
			if (!Current(id, revision))
			{
				return;
			}
			McpOAuthClientRegistration registered = (result.RegisteredDynamically ? result.Client : null);
			string text2 = await McpOAuth.VerifyBearerTokenAsync(Http, url, result.Tokens.AccessToken).ConfigureAwait(continueOnCapturedContext: false);
			if (Current(id, revision))
			{
				revisions[id] = revision;
				CustomConnectorStore.StoreSignInTokens(id, result.Tokens, result.TokenEndpoint, registered, ((object)registered == null) ? null : redirectUri);
				Set(id, (text2 == null) ? new ConnectorSignInState(ConnectorSignInKind.SignedIn) : new ConnectorSignInState(ConnectorSignInKind.Failed, ServerRefusalText(text2, url)));
				if (text2 == null)
				{
					DeriveCapabilities(id);
				}
			}
		}
		catch (McpOAuthException ex) when (ex.Failure == McpOAuthFailure.ServerDoesNotRequireOAuth)
		{
			if (Current(id, revision))
			{
				Set(id, new ConnectorSignInState(ConnectorSignInKind.NotNeeded));
			}
		}
		catch (Exception ex2)
		{
			if (Current(id, revision))
			{
				Log.Warn("connectors", c.Name + ": app-side sign-in failed: " + ex2.Message);
				Set(id, (ex2 is McpOAuthException { Failure: McpOAuthFailure.RegistrationFailed }) ? new ConnectorSignInState(ConnectorSignInKind.SignInUnavailable, UnavailableText(url)) : new ConnectorSignInState(ConnectorSignInKind.Failed, ex2.Message));
			}
		}
		finally
		{
			FinishBrowserReturn();
		}
	}

	private static void OpenAuthorization(string url, string name)
	{
		Log.Info("connectors", name + ": authorization page → browser");
		awaitingReturn = true;
		openBrowser?.Invoke(url);
	}

	private static void FinishBrowserReturn()
	{
		if (awaitingReturn)
		{
			awaitingReturn = false;
			browserReturned?.Invoke();
		}
	}

	private static async Task ProbeAfterSaveAsync(string id)
	{
		CustomConnector customConnector = CustomConnectorStore.Get(id);
		if ((object)customConnector == null)
		{
			return;
		}
		if (customConnector.TrimmedOAuthClientId.Length > 0)
		{
			SignIn(id);
			return;
		}
		await ProbeSignInAsync(customConnector).ConfigureAwait(continueOnCapturedContext: false);
		if (StateFor(id).Kind == ConnectorSignInKind.NeedsSignIn)
		{
			SignIn(id);
		}
	}

	public static Task RefreshStatesAsync()
	{
		foreach (CustomConnector item in from c in CustomConnectorStore.Load()
			where c.UsesBrowserSignIn
			select c)
		{
			bool flag;
			switch (StateFor(item.Id).Kind)
			{
			case ConnectorSignInKind.Checking:
			case ConnectorSignInKind.SignInUnavailable:
			case ConnectorSignInKind.NotNeeded:
			case ConnectorSignInKind.AwaitingBrowser:
			case ConnectorSignInKind.Failed:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				ConnectorSignInState connectorSignInState = StandingFromStore(item);
				if ((object)connectorSignInState != null)
				{
					Set(item.Id, connectorSignInState);
				}
				else
				{
					SignIn(item.Id, probeOnly: true);
				}
			}
		}
		DeriveMissingCapabilities();
		VerifyTokensIfNeeded();
		return Task.CompletedTask;
	}

	public static async Task OnLaunchAsync()
	{
		await RefreshExpiredTokensAsync(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
		DeriveMissingCapabilities();
		VerifyTokensIfNeeded();
		foreach (CustomConnector item in from c in CustomConnectorStore.Load()
			where c.UsesBrowserSignIn
			select c)
		{
			if (StateFor(item.Id).Kind == ConnectorSignInKind.Unknown)
			{
				ConnectorSignInState connectorSignInState = StandingFromStore(item);
				if ((object)connectorSignInState != null)
				{
					Set(item.Id, connectorSignInState);
				}
				else
				{
					SignIn(item.Id, probeOnly: true);
				}
			}
		}
	}

	private static ConnectorSignInState? StandingFromStore(CustomConnector c)
	{
		if (c.HasApiKey)
		{
			return new ConnectorSignInState(ConnectorSignInKind.SignedIn);
		}
		if (c.BrowserSignInSupport == "unsupported" && c.TrimmedOAuthClientId.Length == 0)
		{
			return new ConnectorSignInState(ConnectorSignInKind.SignInUnavailable, UnavailableText(ServerUrl(c)));
		}
		if (c.BrowserSignInSupport == "supported" || c.TrimmedOAuthClientId.Length > 0)
		{
			return new ConnectorSignInState(ConnectorSignInKind.NeedsSignIn);
		}
		return null;
	}

	private static void ImportCodexSignInsOnce(string codexHome)
	{
		string path = AppPaths.Combine("connectors.codex-sign-ins-imported");
		if (File.Exists(path))
		{
			return;
		}
		try
		{
			JsonObject jsonObject = CodexStoredSignIns.ReadFile(codexHome);
			if (jsonObject != null)
			{
				int num = 0;
				foreach (CustomConnector item in from c in CustomConnectorStore.Load()
					where c.IsRemote && !c.HasApiKey && c.TrimmedOAuthClientId.Length == 0
					select c)
				{
					Uri uri = ServerUrl(item);
					if ((object)uri != null)
					{
						CodexStoredSignIn codexStoredSignIn = CodexStoredSignIns.Find(jsonObject, uri, item.ServerKey, DateTimeOffset.UtcNow);
						if ((object)codexStoredSignIn != null)
						{
							string clientId = codexStoredSignIn.ClientId;
							McpOAuthClientRegistration registered = ((clientId != null) ? new McpOAuthClientRegistration(clientId, codexStoredSignIn.ClientSecret) : null);
							CustomConnectorStore.StoreSignInTokens(item.Id, new McpOAuthTokens(codexStoredSignIn.AccessToken, codexStoredSignIn.RefreshToken, codexStoredSignIn.ExpiresAt), null, registered);
							num++;
						}
					}
				}
				Log.Info("connectors", $"imported {num} sign-in(s) Codex had saved");
			}
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, DateTimeOffset.UtcNow.ToString("O"));
		}
		catch (Exception ex)
		{
			Log.Warn("connectors", "importing Codex's sign-ins: " + ex.Message);
		}
	}

	public static void VerifyToken(string id, bool userInitiated)
	{
		CustomConnector c = CustomConnectorStore.Get(id);
		if ((object)c == null || !c.UsesPlainBearerToken)
		{
			return;
		}
		Uri url = ServerUrl(c);
		if ((object)url == null)
		{
			return;
		}
		CustomConnectorSecrets secrets = CustomConnectorStore.ReadSecrets(id);
		if (string.IsNullOrEmpty(secrets.BearerToken))
		{
			return;
		}
		Guid revision = revisions.GetOrAdd(id, (string _) => Guid.NewGuid());
		ConnectorSignInState before = StateFor(id);
		Set(id, new ConnectorSignInState(ConnectorSignInKind.Checking));
		Task.Run(async delegate
		{
			McpVerification mcpVerification = await McpProbe.VerifyConnectionAsync(Http, url, secrets.BearerToken, secrets.Headers).ConfigureAwait(continueOnCapturedContext: false);
			if (Current(id, revision))
			{
				if (!(mcpVerification is McpVerification.Reachable reachable))
				{
					if (!(mcpVerification is McpVerification.Unauthorized unauthorized))
					{
						if (mcpVerification is McpVerification.ServerError serverError)
						{
							Set(id, userInitiated ? new ConnectorSignInState(ConnectorSignInKind.Failed, $"{c.Name} answered HTTP {serverError.StatusCode}. Check the server URL, then try again.") : before);
						}
						else
						{
							Set(id, userInitiated ? new ConnectorSignInState(ConnectorSignInKind.Failed, "Couldn't reach " + c.Name + ". Check the server URL and your connection, then try again.") : before);
						}
					}
					else
					{
						Set(id, new ConnectorSignInState(ConnectorSignInKind.TokenRejected, $"{c.Name} rejected the token (HTTP {unauthorized.StatusCode}). Paste a fresh one under Edit."));
					}
				}
				else
				{
					Log.Info("connectors", $"{c.Name} accepted the token ({reachable.ToolCount} tools)");
					Set(id, new ConnectorSignInState(ConnectorSignInKind.NotNeeded));
				}
			}
		});
	}

	public static void VerifyTokensIfNeeded()
	{
		foreach (CustomConnector item in from c in CustomConnectorStore.Load()
			where c.UsesPlainBearerToken
			select c)
		{
			ConnectorSignInKind kind = StateFor(item.Id).Kind;
			if ((kind != ConnectorSignInKind.Checking && kind != ConnectorSignInKind.NotNeeded) || 1 == 0)
			{
				VerifyToken(item.Id, userInitiated: false);
			}
		}
	}

	private static bool AnyTokenExpiring()
	{
		return CustomConnectorStore.Load().Any(delegate(CustomConnector c)
		{
			if (c.UsesBrowserSignIn && c.HasApiKey)
			{
				CustomConnectorSecrets customConnectorSecrets = CustomConnectorStore.ReadSecrets(c.Id);
				if (customConnectorSecrets != null)
				{
					DateTimeOffset? expiresAt = customConnectorSecrets.ExpiresAt;
					if (expiresAt.HasValue)
					{
						DateTimeOffset valueOrDefault = expiresAt.GetValueOrDefault();
						string refreshToken = customConnectorSecrets.RefreshToken;
						if (refreshToken != null && refreshToken.Length > 0)
						{
							return valueOrDefault - DateTimeOffset.UtcNow < RefreshMargin;
						}
					}
				}
			}
			return false;
		});
	}

	private static async Task<IReadOnlyList<CustomConnectorRegistration>?> RefreshExpiredTokensAsync(CancellationToken ct)
	{
		await RefreshGate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			return await RefreshExpiredTokensOnceAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			RefreshGate.Release();
		}
	}

	private static async Task<IReadOnlyList<CustomConnectorRegistration>?> RefreshExpiredTokensOnceAsync(CancellationToken ct)
	{
		bool changed = false;
		foreach (CustomConnector c2 in from c in CustomConnectorStore.Load()
			where c.UsesBrowserSignIn && c.HasApiKey
			select c)
		{
			CustomConnectorSecrets s = CustomConnectorStore.ReadSecrets(c2.Id);
			DateTimeOffset? expiresAt = s.ExpiresAt;
			if (!expiresAt.HasValue)
			{
				continue;
			}
			DateTimeOffset valueOrDefault = expiresAt.GetValueOrDefault();
			if (valueOrDefault - DateTimeOffset.UtcNow >= RefreshMargin || string.IsNullOrEmpty(s.RefreshToken))
			{
				continue;
			}
			string clientId = ((c2.TrimmedOAuthClientId.Length > 0) ? c2.TrimmedOAuthClientId : s.RegisteredClientId);
			if (string.IsNullOrEmpty(clientId))
			{
				continue;
			}
			Uri uri = ServerUrl(c2);
			if ((object)uri == null)
			{
				continue;
			}
			Guid revision = revisions.GetOrAdd(c2.Id, (string _) => Guid.NewGuid());
			try
			{
				Uri result;
				Uri uri2 = ((!Uri.TryCreate(s.TokenEndpoint, UriKind.Absolute, out result)) ? (await McpOAuth.DiscoverAsync(Http, uri, ct).ConfigureAwait(continueOnCapturedContext: false)).TokenEndpoint : result);
				Uri endpoint = uri2;
				McpOAuthClientRegistration client = new McpOAuthClientRegistration(clientId, (c2.TrimmedOAuthClientId.Length > 0) ? null : s.RegisteredClientSecret);
				McpOAuthTokens mcpOAuthTokens = await McpOAuth.RefreshAsync(Http, endpoint, client, s.RefreshToken, ct).ConfigureAwait(continueOnCapturedContext: false);
				if (Current(c2.Id, revision))
				{
					revisions[c2.Id] = revision;
					CustomConnectorStore.StoreSignInTokens(c2.Id, mcpOAuthTokens with
					{
						RefreshToken = (mcpOAuthTokens.RefreshToken ?? s.RefreshToken)
					}, endpoint, (c2.TrimmedOAuthClientId.Length > 0) ? null : client, (c2.TrimmedOAuthClientId.Length > 0) ? null : s.RegisteredClientRedirectUri);
					Log.Info("connectors", c2.Name + ": access token refreshed");
					changed = true;
				}
			}
			catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
			{
				if (Current(c2.Id, revision))
				{
					Log.Warn("connectors", c2.Name + ": token refresh failed: " + ex.Message);
					if (ex is McpOAuthException error && McpOAuth.IsRefusedRefresh(error))
					{
						Set(c2.Id, new ConnectorSignInState(ConnectorSignInKind.Failed, "Your sign-in expired. Sign in again."));
					}
				}
			}
		}
		return changed ? CustomConnectorStore.Registrations() : null;
	}

	public static void DeriveCapabilities(string id)
	{
		CustomConnector customConnector = CustomConnectorStore.Get(id);
		if ((object)customConnector == null || !customConnector.IsRemote)
		{
			return;
		}
		Uri url = ServerUrl(customConnector);
		if ((object)url == null)
		{
			return;
		}
		CustomConnectorSecrets secrets = CustomConnectorStore.ReadSecrets(id);
		Task.Run(async delegate
		{
			McpToolCatalog mcpToolCatalog = await McpProbe.FetchToolCatalogAsync(Http, url, secrets.BearerToken, secrets.Headers).ConfigureAwait(continueOnCapturedContext: false);
			if ((object)mcpToolCatalog != null)
			{
				CustomConnector customConnector2 = CustomConnectorStore.Get(id);
				if ((object)customConnector2 != null)
				{
					string line = McpProbe.DeriveCapabilityDescription(customConnector2.Name, mcpToolCatalog.ToolNames);
					if (line != null && !(line == customConnector2.DerivedCapabilityDescription))
					{
						Log.Info("connectors", "derived capability line for " + customConnector2.Name + ": " + line);
						CustomConnectorStore.UpdateQuietly(id, (CustomConnector x) => x with
						{
							DerivedCapabilityDescription = line
						});
					}
				}
			}
		});
	}

	public static void DeriveMissingCapabilities()
	{
		foreach (CustomConnector item in from c in CustomConnectorStore.Load()
			where c.IsRemote && string.IsNullOrEmpty(c.DerivedCapabilityDescription)
			select c)
		{
			DeriveCapabilities(item.Id);
		}
	}

	public static string UnavailableText(Uri? serverUrl)
	{
		if ((object)serverUrl == null || !serverUrl.Host.EndsWith("facebook.com", StringComparison.OrdinalIgnoreCase))
		{
			return "This server only signs in clients it has approved, so HeyClicky can't log in through the browser. Add its client ID or an access token under Edit.";
		}
		return "Meta only signs in apps it has approved, so HeyClicky can't log in through the browser. Under Edit, paste your Meta app's App ID as the OAuth client ID (the app needs the \"Create & manage ads with ads MCP server\" use case and the redirect URL shown there), then Sign in.";
	}

	private static string ServerRefusalText(string refusal, Uri? serverUrl)
	{
		string text = Regex.Replace(refusal, "\\s+", " ").Trim();
		string text2 = "The server refused the connection: " + ((text.Length > 200) ? text.Substring(0, 200) : text);
		if ((object)serverUrl != null && serverUrl.Host.EndsWith("facebook.com", StringComparison.OrdinalIgnoreCase))
		{
			text2 += " For Meta Ads, add the \"Create & manage ads with ads MCP server\" use case to your Meta app (it grants ads_mcp_management), then sign in again.";
		}
		return text2;
	}
}

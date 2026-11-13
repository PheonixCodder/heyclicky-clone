using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Connectors;

public static class McpOAuth
{
	public const int CallbackPort = 48611;

	public const string CallbackPath = "/callback";

	public static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(5L);

	private const string ProtocolVersion = "2025-06-18";

	private const string InitializeBody = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{},\"clientInfo\":{\"name\":\"HeyClicky\",\"version\":\"1\"}}}";

	public static string RedirectUri(int port = 48611)
	{
		return $"http://localhost:{port}{"/callback"}";
	}

	private static HttpRequestMessage InitializeProbe(Uri serverUrl)
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, serverUrl);
		httpRequestMessage.Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{},\"clientInfo\":{\"name\":\"HeyClicky\",\"version\":\"1\"}}}", Encoding.UTF8, "application/json");
		httpRequestMessage.Headers.Accept.ParseAdd("application/json");
		httpRequestMessage.Headers.Accept.ParseAdd("text/event-stream");
		return httpRequestMessage;
	}

	public static async Task<McpOAuthDiscovery> DiscoverAsync(HttpClient http, Uri serverUrl, CancellationToken ct = default(CancellationToken))
	{
		HttpResponseMessage httpResponseMessage;
		try
		{
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(TimeSpan.FromSeconds(15L));
			httpResponseMessage = await http.SendAsync(InitializeProbe(serverUrl), HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException) ? true : false) && !ct.IsCancellationRequested)
		{
			throw new McpOAuthException(McpOAuthFailure.ServerUnreachable, "Couldn't reach the server: " + ex.Message);
		}
		using (httpResponseMessage)
		{
			if (httpResponseMessage.StatusCode != HttpStatusCode.Unauthorized)
			{
				throw new McpOAuthException(McpOAuthFailure.ServerDoesNotRequireOAuth, "This server didn't ask for a sign-in.");
			}
			Dictionary<string, string> dictionary = ParseAuthenticateChallenge(string.Join(", ", httpResponseMessage.Headers.WwwAuthenticate.Select((AuthenticationHeaderValue h) => h.ToString())));
			List<string> scopes = Split(dictionary.GetValueOrDefault("scope"));
			Uri issuer = null;
			string value;
			Uri result;
			Uri uri = ((dictionary.TryGetValue("resource_metadata", out value) && Uri.TryCreate(value, UriKind.Absolute, out result)) ? result : WellKnownUrl(serverUrl, "oauth-protected-resource"));
			bool flag = (object)uri != null;
			JsonObject jsonObject = default(JsonObject);
			if (flag)
			{
				jsonObject = await FetchJsonAsync(http, uri, TimeSpan.FromSeconds(15L), ct).ConfigureAwait(continueOnCapturedContext: false);
				flag = jsonObject != null;
			}
			if (flag)
			{
				string text = (jsonObject["authorization_servers"] as JsonArray)?.FirstOrDefault()?.ToString();
				if (text != null && Uri.TryCreate(text, UriKind.Absolute, out Uri result2))
				{
					issuer = result2;
				}
				if (scopes.Count == 0)
				{
					scopes = Strings(jsonObject["scopes_supported"]);
				}
			}
			if ((object)issuer == null)
			{
				issuer = Origin(serverUrl);
			}
			JsonObject metadata = null;
			foreach (Uri item in AuthorizationServerMetadataCandidates(issuer))
			{
				JsonObject jsonObject2 = await FetchJsonAsync(http, item, TimeSpan.FromSeconds(15L), ct).ConfigureAwait(continueOnCapturedContext: false);
				if (jsonObject2 != null && jsonObject2["authorization_endpoint"] != null)
				{
					metadata = jsonObject2;
					break;
				}
			}
			if (metadata == null || !Uri.TryCreate(metadata["authorization_endpoint"]?.ToString(), UriKind.Absolute, out Uri result3) || !Uri.TryCreate(metadata["token_endpoint"]?.ToString(), UriKind.Absolute, out Uri result4))
			{
				throw new McpOAuthException(McpOAuthFailure.DiscoveryFailed, $"Couldn't read the server's sign-in settings: no authorization server metadata at {issuer}");
			}
			List<string> resourceScopes = scopes.ToList();
			if (scopes.Count == 0)
			{
				scopes = Strings(metadata["scopes_supported"]);
			}
			Uri.TryCreate(metadata["registration_endpoint"]?.ToString(), UriKind.Absolute, out Uri result5);
			return new McpOAuthDiscovery(result3, result4, result5, scopes, serverUrl.AbsoluteUri)
			{
				ResourceScopes = resourceScopes
			};
		}
	}

	public static async Task<McpOAuthSignInSupport> ProbeSignInSupportAsync(HttpClient http, Uri serverUrl, bool hasClientId, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			return ((object)(await DiscoverAsync(http, serverUrl, ct).ConfigureAwait(continueOnCapturedContext: false)).RegistrationEndpoint != null || hasClientId) ? McpOAuthSignInSupport.Supported : McpOAuthSignInSupport.Unsupported;
		}
		catch (McpOAuthException ex) when (ex.Failure == McpOAuthFailure.ServerDoesNotRequireOAuth)
		{
			return McpOAuthSignInSupport.NotNeeded;
		}
		catch (McpOAuthException)
		{
			return McpOAuthSignInSupport.Unknown;
		}
	}

	public static JsonObject RegistrationRequestBody(string redirectUri)
	{
		JsonObject obj = new JsonObject((JsonNodeOptions?)null) { ["client_name"] = "HeyClicky" };
		JsonNode reference = redirectUri;
		obj["redirect_uris"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
		global::_003C_003Ey__InlineArray2<JsonNode> buffer = default(global::_003C_003Ey__InlineArray2<JsonNode>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 0) = "authorization_code";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 1) = "refresh_token";
		obj["grant_types"] = new JsonArray(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(in buffer, 2));
		JsonNode reference2 = "code";
		obj["response_types"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference2));
		obj["token_endpoint_auth_method"] = "none";
		return obj;
	}

	public static async Task<McpOAuthClientRegistration> RegisterClientAsync(HttpClient http, McpOAuthDiscovery discovery, string redirectUri, CancellationToken ct = default(CancellationToken))
	{
		Uri registrationEndpoint = discovery.RegistrationEndpoint;
		if ((object)registrationEndpoint == null)
		{
			throw new McpOAuthException(McpOAuthFailure.RegistrationFailed, "This server only signs in clients it has approved. Add its client ID or an access token under Edit.");
		}
		JsonObject jsonObject = RegistrationRequestBody(redirectUri);
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, registrationEndpoint)
		{
			Content = new StringContent(jsonObject.ToJsonString(), Encoding.UTF8, "application/json")
		};
		request.Headers.Accept.ParseAdd("application/json");
		using HttpResponseMessage response = await http.SendAsync(request, ct).ConfigureAwait(continueOnCapturedContext: false);
		JsonObject jsonObject2 = await ReadJsonAsync(response, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (response.IsSuccessStatusCode)
		{
			string text = jsonObject2?["client_id"]?.ToString();
			if (text != null && text.Length > 0)
			{
				string text2 = jsonObject2["client_secret"]?.ToString();
				return new McpOAuthClientRegistration(text, (text2 != null && text2.Length > 0) ? text2 : null);
			}
		}
		throw new McpOAuthException(McpOAuthFailure.RegistrationFailed, "The server refused to register HeyClicky: " + ErrorDetail(jsonObject2, response));
	}

	public static async Task<McpOAuthSignInResult> SignInAsync(HttpClient http, Uri serverUrl, string? clientId, Action<Uri> openBrowser, CancellationToken ct = default(CancellationToken), int port = 48611, TimeSpan? timeout = null, McpOAuthClientRegistration? previouslyRegistered = null)
	{
		McpOAuthDiscovery discovery = await DiscoverAsync(http, serverUrl, ct).ConfigureAwait(continueOnCapturedContext: false);
		string redirectUri = RedirectUri(port);
		bool dynamic = string.IsNullOrWhiteSpace(clientId);
		McpOAuthClientRegistration mcpOAuthClientRegistration;
		if (!dynamic)
		{
			mcpOAuthClientRegistration = new McpOAuthClientRegistration(clientId.Trim(), null);
		}
		else
		{
			McpOAuthClientRegistration mcpOAuthClientRegistration2 = previouslyRegistered;
			if ((object)mcpOAuthClientRegistration2 == null)
			{
				mcpOAuthClientRegistration2 = await RegisterClientAsync(http, discovery, redirectUri, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			mcpOAuthClientRegistration = mcpOAuthClientRegistration2;
		}
		McpOAuthClientRegistration client = mcpOAuthClientRegistration;
		IReadOnlyList<string> readOnlyList = SignInScopes(discovery, dynamic);
		(string, string) tuple = NewPkce();
		string verifier = tuple.Item1;
		string item = tuple.Item2;
		string state = RandomUrlSafe(24);
		using LoopbackCallbackServer callback = new LoopbackCallbackServer(port, "/callback", state);
		List<(string, string)> list = new List<(string, string)>
		{
			("response_type", "code"),
			("client_id", client.ClientId),
			("redirect_uri", redirectUri),
			("state", state),
			("code_challenge", item),
			("code_challenge_method", "S256"),
			("resource", discovery.Resource)
		};
		if (readOnlyList.Count > 0)
		{
			list.Add(("scope", string.Join(' ', readOnlyList)));
		}
		UriBuilder uriBuilder = new UriBuilder(discovery.AuthorizationEndpoint);
		string text = uriBuilder.Query.TrimStart('?');
		uriBuilder.Query = ((text.Length > 0) ? (text + "&") : "") + FormEncode(list);
		openBrowser(uriBuilder.Uri);
		Dictionary<string, string> dictionary = await callback.WaitAsync(timeout ?? SignInTimeout, ct).ConfigureAwait(continueOnCapturedContext: false);
		if (dictionary.TryGetValue("error", out var value))
		{
			string valueOrDefault = dictionary.GetValueOrDefault("error_description");
			throw new McpOAuthException(McpOAuthFailure.AuthorizationDenied, (valueOrDefault != null && valueOrDefault.Length > 0) ? valueOrDefault : ((value.Length > 0) ? value : "The sign-in was cancelled."));
		}
		if (dictionary.GetValueOrDefault("state") != state)
		{
			throw new McpOAuthException(McpOAuthFailure.StateMismatch, "The sign-in callback didn't match this request.");
		}
		string valueOrDefault2 = dictionary.GetValueOrDefault("code");
		if (valueOrDefault2 == null || valueOrDefault2.Length <= 0)
		{
			throw new McpOAuthException(McpOAuthFailure.AuthorizationDenied, "The sign-in returned no code.");
		}
		List<(string, string)> list2 = new List<(string, string)>
		{
			("grant_type", "authorization_code"),
			("code", valueOrDefault2),
			("redirect_uri", redirectUri),
			("client_id", client.ClientId),
			("code_verifier", verifier),
			("resource", discovery.Resource)
		};
		string clientSecret = client.ClientSecret;
		if (clientSecret != null)
		{
			list2.Add(("client_secret", clientSecret));
		}
		return new McpOAuthSignInResult(await RequestTokensAsync(http, discovery.TokenEndpoint, list2, ct).ConfigureAwait(continueOnCapturedContext: false), discovery.TokenEndpoint, client, dynamic);
	}

	public static IReadOnlyList<string> SignInScopes(McpOAuthDiscovery discovery, bool isRegisteredClient)
	{
		if (!isRegisteredClient)
		{
			return discovery.Scopes;
		}
		return discovery.ResourceScopes;
	}

	public static Task<McpOAuthTokens> RefreshAsync(HttpClient http, Uri tokenEndpoint, McpOAuthClientRegistration client, string refreshToken, CancellationToken ct = default(CancellationToken))
	{
		List<(string, string)> list = new List<(string, string)>
		{
			("grant_type", "refresh_token"),
			("refresh_token", refreshToken),
			("client_id", client.ClientId)
		};
		string clientSecret = client.ClientSecret;
		if (clientSecret != null)
		{
			list.Add(("client_secret", clientSecret));
		}
		return RequestTokensAsync(http, tokenEndpoint, list, ct);
	}

	public static bool IsRefusedRefresh(McpOAuthException error)
	{
		if (error.Failure == McpOAuthFailure.TokenExchangeFailed)
		{
			return new string[6] { "invalid_grant", "invalid_client", "unauthorized", "HTTP 400", "HTTP 401", "HTTP 403" }.Any((string marker) => error.Message.Contains(marker, StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	private static async Task<McpOAuthTokens> RequestTokensAsync(HttpClient http, Uri tokenEndpoint, List<(string, string)> form, CancellationToken ct)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
		{
			Content = new StringContent(FormEncode(form), Encoding.UTF8, "application/x-www-form-urlencoded")
		};
		request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
		request.Headers.Accept.ParseAdd("application/json");
		HttpResponseMessage response;
		try
		{
			response = await http.SendAsync(request, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (HttpRequestException ex)
		{
			throw new McpOAuthException(McpOAuthFailure.TokenExchangeFailed, "The server refused to issue a token: " + ex.Message);
		}
		using (response)
		{
			JsonObject jsonObject = await ReadJsonAsync(response, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (response.IsSuccessStatusCode)
			{
				string text = jsonObject?["access_token"]?.ToString();
				if (text != null && text.Length > 0)
				{
					double value;
					double? num = ((jsonObject["expires_in"] is JsonValue jsonValue && (jsonValue.TryGetValue<double>(out value) || double.TryParse(jsonValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))) ? new double?(value) : ((double?)null));
					string text2 = jsonObject["refresh_token"]?.ToString();
					string refreshToken = ((text2 != null && text2.Length > 0) ? text2 : null);
					DateTimeOffset? expiresAt;
					if (num.HasValue)
					{
						double valueOrDefault = num.GetValueOrDefault();
						expiresAt = DateTimeOffset.UtcNow.AddSeconds(valueOrDefault);
					}
					else
					{
						expiresAt = null;
					}
					return new McpOAuthTokens(text, refreshToken, expiresAt);
				}
			}
			throw new McpOAuthException(McpOAuthFailure.TokenExchangeFailed, "The server refused to issue a token: " + ErrorDetail(jsonObject, response));
		}
	}

	public static async Task<string?> VerifyBearerTokenAsync(HttpClient http, Uri serverUrl, string accessToken, CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			using HttpRequestMessage request = InitializeProbe(serverUrl);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-06-18");
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(TimeSpan.FromSeconds(20L));
			using HttpResponseMessage response = await http.SendAsync(request, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			HttpStatusCode statusCode = response.StatusCode;
			if ((statusCode != HttpStatusCode.Unauthorized && statusCode != HttpStatusCode.Forbidden) || 1 == 0)
			{
				return null;
			}
			JsonObject jsonObject = await ReadJsonAsync(response, ct).ConfigureAwait(continueOnCapturedContext: false);
			string text = (jsonObject?["title"] ?? jsonObject?["detail"] ?? jsonObject?["error"])?.ToString()?.Trim();
			return string.IsNullOrEmpty(text) ? $"The server refused the token (HTTP {response.StatusCode})." : text;
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException) ? true : false) && !ct.IsCancellationRequested)
		{
			return null;
		}
	}

	// [GeneratedRegex("([A-Za-z_]+)\\s*=\\s*\"([^\"]*)\"")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex ChallengeParameter()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__ChallengeParameter_3.Instance;
	}

	public static Dictionary<string, string> ParseAuthenticateChallenge(string header)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (Match item in ChallengeParameter().Matches(header ?? ""))
		{
			dictionary[item.Groups[1].Value.ToLowerInvariant()] = item.Groups[2].Value;
		}
		return dictionary;
	}

	public static (string Verifier, string Challenge) NewPkce()
	{
		string text = RandomUrlSafe(48);
		return (Verifier: text, Challenge: Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(text))));
	}

	public static string RandomUrlSafe(int bytes)
	{
		return Base64Url(RandomNumberGenerator.GetBytes(bytes));
	}

	public static string Base64Url(byte[] bytes)
	{
		return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_')
			.TrimEnd('=');
	}

	public static string FormEncode(IEnumerable<(string Key, string Value)> pairs)
	{
		return string.Join('&', pairs.Select<(string, string), string>(((string Key, string Value) p) => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
	}

	private static Uri Origin(Uri url)
	{
		return new Uri(url.Scheme + "://" + url.Authority);
	}

	public static Uri? WellKnownUrl(Uri url, string suffix)
	{
		string text = url.AbsolutePath.Trim('/');
		if (!Uri.TryCreate($"{url.Scheme}://{url.Authority}/.well-known/{suffix}{((text.Length == 0) ? "" : ("/" + text))}", UriKind.Absolute, out Uri result))
		{
			return null;
		}
		return result;
	}

	private static IEnumerable<Uri> AuthorizationServerMetadataCandidates(Uri issuer)
	{
		HashSet<string> seen = new HashSet<string>();
		Uri?[] candidates = new Uri?[4]
		{
			WellKnownUrl(issuer, "oauth-authorization-server"),
			new Uri(issuer.Scheme + "://" + issuer.Authority + "/.well-known/oauth-authorization-server"),
			Uri.TryCreate(issuer.AbsoluteUri.TrimEnd('/') + "/.well-known/oauth-authorization-server", UriKind.Absolute, out Uri? result) ? result : null,
			WellKnownUrl(issuer, "openid-configuration")
		};
		foreach (Uri? uri in candidates)
		{
			if ((object?)uri != null && seen.Add(uri.AbsoluteUri))
			{
				yield return uri;
			}
		}
	}

	private static async Task<JsonObject?> FetchJsonAsync(HttpClient http, Uri url, TimeSpan timeout, CancellationToken ct)
	{
		_ = 1;
		try
		{
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(timeout);
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Accept.ParseAdd("application/json");
			using HttpResponseMessage response = await http.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			return (!response.IsSuccessStatusCode) ? null : (await ReadJsonAsync(response, cts.Token).ConfigureAwait(continueOnCapturedContext: false));
		}
		catch (Exception ex) when (((ex is HttpRequestException || ex is TaskCanceledException) ? true : false) && !ct.IsCancellationRequested)
		{
			return null;
		}
	}

	private static async Task<JsonObject?> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
	{
		try
		{
			return JsonNode.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false), null) as JsonObject;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static string ErrorDetail(JsonObject? json, HttpResponseMessage response)
	{
		string text = json?["error_description"]?.ToString();
		if (text == null || text.Length <= 0)
		{
			if (json?["error"] is JsonObject jsonObject)
			{
				string text2 = jsonObject["message"]?.ToString();
				if (text2 != null && text2.Length > 0)
				{
					return text2;
				}
			}
			string text3 = json?["error"]?.ToString();
			if (text3 == null || text3.Length <= 0)
			{
				return $"HTTP {response.StatusCode}";
			}
			return text3;
		}
		return text;
	}

	private static List<string> Split(string? value)
	{
		return (value ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
	}

	private static List<string> Strings(JsonNode? node)
	{
		return (from s in ((node as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).Select((JsonNode n) => n?.ToString()?.Trim()).OfType<string>()
			where s.Length > 0
			select s).ToList();
	}
}

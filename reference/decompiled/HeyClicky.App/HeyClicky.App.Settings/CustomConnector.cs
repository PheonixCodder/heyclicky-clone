using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HeyClicky.App.Settings;

public sealed record CustomConnector(string Id, string Name, string Kind, string? Url, string? Command, IReadOnlyList<string> Arguments, IReadOnlyList<string> EnvironmentNames, bool HasApiKey)
{
	public string? OAuthClientId { get; init; }

	public IReadOnlyList<string> HeaderNames { get; init; } = Array.Empty<string>();

	public bool TokenFromSignIn { get; init; }

	public string? DerivedCapabilityDescription { get; init; }

	public string? BrowserSignInSupport { get; init; }

	public DateTimeOffset CreatedAt { get; init; }

	[JsonIgnore]
	public bool IsRemote => Kind == "remote";

	[JsonIgnore]
	public string ServerKey => "custom_" + new string(Id.Where(char.IsAsciiLetterOrDigit).ToArray()).ToLowerInvariant();

	[JsonIgnore]
	public string IntegrationId => "custom:" + Id;

	[JsonIgnore]
	public string TrimmedOAuthClientId => OAuthClientId?.Trim() ?? "";

	[JsonIgnore]
	public bool UsesBrowserSignIn
	{
		get
		{
			if (IsRemote)
			{
				if (TrimmedOAuthClientId.Length <= 0 && !TokenFromSignIn)
				{
					return !HasApiKey;
				}
				return true;
			}
			return false;
		}
	}

	[JsonIgnore]
	public bool UsesPlainBearerToken
	{
		get
		{
			if (IsRemote && HasApiKey)
			{
				return !UsesBrowserSignIn;
			}
			return false;
		}
	}

	[JsonIgnore]
	public string LocationSummary
	{
		get
		{
			if (!IsRemote)
			{
				return string.Join(" ", new string[1] { Path.GetFileName(Command ?? "") }.Concat(Arguments));
			}
			string text;
			if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri result))
			{
				text = Url;
				if (text == null)
				{
					return "";
				}
			}
			else
			{
				text = result.Host;
			}
			return text;
		}
	}

	[JsonIgnore]
	public string Summary
	{
		get
		{
			if (!IsRemote)
			{
				return string.Join(" ", new string[1] { Command ?? "" }.Concat(Arguments));
			}
			return Url ?? "";
		}
	}

	[JsonIgnore]
	public string EffectiveCapabilityDescription => DerivedCapabilityDescription?.Trim() ?? "";

	public bool HasSameAuthenticationTarget(CustomConnector other)
	{
		if (Kind != other.Kind || TrimmedOAuthClientId != other.TrimmedOAuthClientId)
		{
			return false;
		}
		if (!IsRemote)
		{
			return true;
		}
		return Origin(Url) == Origin(other.Url);
		static string? Origin(string? u)
		{
			if (Uri.TryCreate(u?.Trim(), UriKind.Absolute, out Uri result))
			{
				return $"{result.Scheme}://{result.Host.ToLowerInvariant()}:{result.Port}";
			}
			return null;
		}
	}

	public string? LogoUrl(string workerBaseUrl)
	{
		if (!IsRemote || !Uri.TryCreate(Url?.Trim(), UriKind.Absolute, out Uri result) || result.Host.Length == 0 || result.IsLoopback)
		{
			return null;
		}
		string[] array = result.Host.ToLowerInvariant().Split('.');
		string stringToEscape = ((array.Length > 2) ? string.Join('.', array[^2..]) : result.Host.ToLowerInvariant());
		return workerBaseUrl.TrimEnd('/') + "/favicon?host=" + Uri.EscapeDataString(stringToEscape) + "&sz=64";
	}

	public static bool IsValidEnvironmentVariableName(string name)
	{
		if (name.Length > 0 && (name[0] == '_' || char.IsAsciiLetter(name[0])))
		{
			return name.All((char c) => c == '_' || char.IsAsciiLetterOrDigit(c));
		}
		return false;
	}

	public static bool IsValidHeaderName(string name)
	{
		if (name.Length > 0)
		{
			return name.All(delegate(char c)
			{
				bool flag = char.IsAsciiLetterOrDigit(c);
				if (!flag)
				{
					bool flag2 = ((c == '-' || c == '_') ? true : false);
					flag = flag2;
				}
				return flag;
			});
		}
		return false;
	}
}

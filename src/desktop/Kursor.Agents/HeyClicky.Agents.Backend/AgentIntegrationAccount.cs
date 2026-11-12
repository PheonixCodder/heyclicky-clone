using System;

namespace HeyClicky.Agents.Backend;

public sealed record AgentIntegrationAccount(string ConnectedAccountId, string? AccountName, string? Label, string? AvatarUrl, string? AccountNameKind, string Status, string? ConnectedAt)
{
	public bool IsActive => Status == "connected";

	public bool IsOrganization => AccountNameKind == "organization";

	public string? TrimmedAccountName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(AccountName))
			{
				return AccountName.Trim();
			}
			return null;
		}
	}

	public string? TrimmedLabel
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Label))
			{
				return Label.Trim();
			}
			return null;
		}
	}

	public DateTimeOffset? ConnectedDate
	{
		get
		{
			if (!DateTimeOffset.TryParse(ConnectedAt, out var result))
			{
				return null;
			}
			return result;
		}
	}
}

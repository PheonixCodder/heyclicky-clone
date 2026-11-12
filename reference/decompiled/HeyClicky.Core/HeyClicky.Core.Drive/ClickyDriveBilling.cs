using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Drive;

public abstract record ClickyDriveBilling
{
	public sealed record User : ClickyDriveBilling;

	public sealed record Job(string? LeaseId) : ClickyDriveBilling();

	public sealed record Setup : ClickyDriveBilling;

	public static readonly ClickyDriveBilling UserDrive = new User();

	public static readonly ClickyDriveBilling SetupDrive = new Setup();

	public static JsonObject RequestFields(ClickyDriveBilling billing, string driveId, IReadOnlyList<string> askingTurnTokens)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null) { ["drive_id"] = driveId };
		if (!(billing is User))
		{
			if (!(billing is Job job))
			{
				if (billing is Setup)
				{
					jsonObject["drive_billing"] = "setup";
				}
			}
			else
			{
				jsonObject["drive_billing"] = "job";
				string leaseId = job.LeaseId;
				if (leaseId != null)
				{
					jsonObject["drive_job_lease_id"] = leaseId;
				}
			}
		}
		else
		{
			jsonObject["drive_billing"] = "agent";
			if (askingTurnTokens.Count > 0)
			{
				jsonObject["asking_turn_tokens"] = new JsonArray(((IEnumerable<string>)askingTurnTokens).Select((Func<string, JsonNode>)((string t) => JsonValue.Create(t, (JsonNodeOptions?)null))).ToArray());
			}
		}
		return jsonObject;
	}

	[CompilerGenerated]
	protected ClickyDriveBilling(ClickyDriveBilling original)
	{
	}

	protected ClickyDriveBilling()
	{
	}
}

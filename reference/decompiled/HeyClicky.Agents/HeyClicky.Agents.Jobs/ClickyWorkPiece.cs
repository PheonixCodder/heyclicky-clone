using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyWorkPiece
{
	public required string Id { get; init; }

	public required string Slug { get; init; }

	public string? ThreadId { get; init; }

	public string? ClientTurnKey { get; init; }

	public string Ask { get; init; } = "";

	public string Title { get; init; } = "";

	public string Summary { get; init; } = "";

	public string Reply { get; init; } = "";

	public IReadOnlyList<string> ArtifactPaths { get; init; } = Array.Empty<string>();

	public DateTimeOffset FinishedAt { get; init; }

	public bool Failed { get; init; }

	public bool IsRoutineRun { get; init; }

	public string DisplayTitle
	{
		get
		{
			if (Title.Length <= 0)
			{
				if (Ask.Length != 0)
				{
					return Ask;
				}
				return "Work";
			}
			return Title;
		}
	}

	public IReadOnlyList<string> ExistingFilePaths => ArtifactPaths.Where((string p) => !IsLink(p) && (File.Exists(p) || Directory.Exists(p))).ToList();

	public IReadOnlyList<string> Links => ArtifactPaths.Where(IsLink).ToList();

	public ClickyWorkPiece Reidentified(string newId, DateTimeOffset newFinishedAt)
	{
		return this with
		{
			Id = newId,
			FinishedAt = newFinishedAt
		};
	}

	private static bool IsLink(string path)
	{
		if (!path.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			return path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public bool Equals(ClickyWorkPiece? other)
	{
		if ((object)other != null && Id == other.Id && Slug == other.Slug && ThreadId == other.ThreadId && ClientTurnKey == other.ClientTurnKey && Ask == other.Ask && Title == other.Title && Summary == other.Summary && Reply == other.Reply && ArtifactPaths.SequenceEqual(other.ArtifactPaths) && FinishedAt == other.FinishedAt && Failed == other.Failed)
		{
			return IsRoutineRun == other.IsRoutineRun;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Id, Reply, FinishedAt);
	}

	[CompilerGenerated]
	[SetsRequiredMembers]
	private ClickyWorkPiece(ClickyWorkPiece original)
	{
		Id = original.Id;
		Slug = original.Slug;
		ThreadId = original.ThreadId;
		ClientTurnKey = original.ClientTurnKey;
		Ask = original.Ask;
		Title = original.Title;
		Summary = original.Summary;
		Reply = original.Reply;
		ArtifactPaths = original.ArtifactPaths;
		FinishedAt = original.FinishedAt;
		Failed = original.Failed;
		IsRoutineRun = original.IsRoutineRun;
	}

	public ClickyWorkPiece()
	{
	}
}

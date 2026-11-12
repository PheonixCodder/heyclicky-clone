using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyLiveLine(ClickyLiveLine.LineKind Kind, ClickyInkWorkStep.StepKind? StepKind, string Text, DateTimeOffset At)
{
	public enum LineKind
	{
		Thinking,
		Step
	}

	public DateTimeOffset ShownAt { get; init; } = DateTimeOffset.MinValue;

	public IReadOnlyList<ClickyLiveLineIcon> Icons { get; init; } = Array.Empty<ClickyLiveLineIcon>();

	public int MoreIcons { get; init; }

	public string? BatchId { get; init; }

	public static ClickyLiveLine FromStep(ClickyInkWorkStep step)
	{
		string text = step.Object?.Replace("“", "").Replace("”", "").Replace("…", "")
			.Trim();
		if (string.IsNullOrEmpty(text))
		{
			text = null;
		}
		string text2 = step.Kind switch
		{
			ClickyInkWorkStep.StepKind.Search => text ?? "the web", 
			ClickyInkWorkStep.StepKind.Read => text ?? "a page", 
			ClickyInkWorkStep.StepKind.Look => "looking through the page", 
			ClickyInkWorkStep.StepKind.Math => "doing the math", 
			ClickyInkWorkStep.StepKind.Note => "writing the note", 
			ClickyInkWorkStep.StepKind.File => text ?? "a file", 
			ClickyInkWorkStep.StepKind.Apps => text ?? "your apps", 
			_ => "using your PC", 
		};
		return new ClickyLiveLine(LineKind.Step, step.Kind, text2, step.At);
	}

	public static ClickyLiveLine? FromBatch(ClickyProgramBatch batch, DateTimeOffset at)
	{
		(string, ClickyInkWorkStep.StepKind)? summary = batch.Summary;
		if (summary.HasValue)
		{
			(string, ClickyInkWorkStep.StepKind) valueOrDefault = summary.GetValueOrDefault();
			IReadOnlyList<ClickyLiveLineIcon> icons = batch.Icons;
			return new ClickyLiveLine(LineKind.Step, valueOrDefault.Item2, valueOrDefault.Item1, at)
			{
				Icons = icons.Take(4).ToList(),
				MoreIcons = Math.Max(0, icons.Count - 4) + Math.Max(0, batch.SiteCount - batch.Sites.Count),
				BatchId = batch.Id
			};
		}
		return null;
	}

	public bool Equals(ClickyLiveLine? other)
	{
		if ((object)other != null && Kind == other.Kind && StepKind == other.StepKind && Text == other.Text && ShownAt == other.ShownAt && Icons.SequenceEqual(other.Icons))
		{
			return MoreIcons == other.MoreIcons;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Kind, StepKind, Text, ShownAt, Icons.Count, MoreIcons);
	}
}

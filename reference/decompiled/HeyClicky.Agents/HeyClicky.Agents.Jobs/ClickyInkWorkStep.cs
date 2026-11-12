using System;
using System.Runtime.CompilerServices;

namespace HeyClicky.Agents.Jobs;

public sealed record ClickyInkWorkStep(StepKind Kind, string? Object, DateTimeOffset At)
{
	public enum StepKind
	{
		Search,
		Read,
		Look,
		Math,
		Note,
		File,
		Apps,
		Mac
	}

	public string Present => Kind switch
	{
		StepKind.Search => "searching " + (Object ?? "the web"), 
		StepKind.Read => "reading " + (Object ?? "a page"), 
		StepKind.Look => "looking through the page", 
		StepKind.Math => "doing the math", 
		StepKind.Note => "writing the note", 
		StepKind.File => "working on " + (Object ?? "a file"), 
		StepKind.Apps => "checking " + (Object ?? "your apps"), 
		_ => "using your PC", 
	};

	public string Past => Kind switch
	{
		StepKind.Search => "searched " + (Object ?? "the web"), 
		StepKind.Read => "read " + (Object ?? "a page"), 
		StepKind.Look => "looked through the page", 
		StepKind.Math => "did the math", 
		StepKind.Note => "wrote the note", 
		StepKind.File => "worked on " + (Object ?? "a file"), 
		StepKind.Apps => "checked " + (Object ?? "your apps"), 
		_ => "used your PC", 
	};

	public string Symbol => Kind switch
	{
		StepKind.Search => "magnifyingglass", 
		StepKind.Read => "doc.text", 
		StepKind.Look => "text.magnifyingglass", 
		StepKind.Math => "function", 
		StepKind.Note => "pencil.line", 
		StepKind.File => "doc", 
		StepKind.Apps => "square.grid.2x2", 
		_ => "cursorarrow", 
	};

	public const int Kept = 12;

	public bool IsSame(ClickyInkWorkStep other)
	{
		if (Kind == other.Kind)
		{
			return Object == other.Object;
		}
		return false;
	}

	[CompilerGenerated]
	private ClickyInkWorkStep(ClickyInkWorkStep original)
	{
		Kind = original.Kind;
		Object = original.Object;
		At = original.At;
	}
}

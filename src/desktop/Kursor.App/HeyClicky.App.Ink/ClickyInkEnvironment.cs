using System;
using System.Runtime.CompilerServices;

namespace HeyClicky.App.Ink;

public sealed record ClickyInkEnvironment
{
	private sealed class Restore(ClickyInkEnvironment? previous) : IDisposable
	{
		public void Dispose()
		{
			current = previous;
		}
	}

	public double ContentWidth { get; init; } = 378.0;

	public bool IsWindowPreview { get; init; }

	public bool IsGlancePreview { get; init; }

	public static ClickyInkEnvironment Current => current ?? Default;

	public static readonly ClickyInkEnvironment Default = new ClickyInkEnvironment();

	[ThreadStatic]
	private static ClickyInkEnvironment? current;

	public static IDisposable Use(ClickyInkEnvironment environment)
	{
		ClickyInkEnvironment? previous = current;
		current = environment;
		return new Restore(previous);
	}

	[CompilerGenerated]
	private ClickyInkEnvironment(ClickyInkEnvironment original)
	{
		ContentWidth = original.ContentWidth;
		IsWindowPreview = original.IsWindowPreview;
		IsGlancePreview = original.IsGlancePreview;
	}

	public ClickyInkEnvironment()
	{
	}
}

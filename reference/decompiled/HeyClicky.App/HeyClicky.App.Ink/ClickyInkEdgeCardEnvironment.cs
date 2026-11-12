using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed record ClickyInkEdgeCardEnvironment
{
	private sealed class Restore(ClickyInkEdgeCardEnvironment? previous) : IDisposable
	{
		public void Dispose()
		{
			current = previous;
		}
	}

	public double? NoteWindowTitleBarHeight { get; init; }

	public double FooterHeight { get; init; } = 46.0;

	public Size? SheetSize { get; init; }

	public static ClickyInkEdgeCardEnvironment Current => current ?? Default;

	public static readonly ClickyInkEdgeCardEnvironment Default = new ClickyInkEdgeCardEnvironment();

	[ThreadStatic]
	private static ClickyInkEdgeCardEnvironment? current;

	public static IDisposable Use(ClickyInkEdgeCardEnvironment environment)
	{
		ClickyInkEdgeCardEnvironment? previous = current;
		current = environment;
		return new Restore(previous);
	}

	[CompilerGenerated]
	private ClickyInkEdgeCardEnvironment(ClickyInkEdgeCardEnvironment original)
	{
		NoteWindowTitleBarHeight = original.NoteWindowTitleBarHeight;
		FooterHeight = original.FooterHeight;
		SheetSize = original.SheetSize;
	}

	public ClickyInkEdgeCardEnvironment()
	{
	}
}

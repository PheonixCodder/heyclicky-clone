using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HeyClicky.Core.Voice;

namespace HeyClicky.Core.Tools;

public sealed record CompanionToolContext(string? UserWords, bool Typed, IReadOnlyList<string> AttachedFilePaths, byte[]? ScreenshotJpeg = null, string? HighlightGesture = null, PageReference? Page = null)
{
	public static readonly CompanionToolContext None = new CompanionToolContext(null, Typed: false, Array.Empty<string>());

	[CompilerGenerated]
	private CompanionToolContext(CompanionToolContext original)
	{
		UserWords = original.UserWords;
		Typed = original.Typed;
		AttachedFilePaths = original.AttachedFilePaths;
		ScreenshotJpeg = original.ScreenshotJpeg;
		HighlightGesture = original.HighlightGesture;
		Page = original.Page;
	}
}

using System;

namespace HeyClicky.Core.Brain;

public sealed class ScreenshotFilesUnavailableException : Exception
{
	public const string Code = "screenshot_files_unavailable";

	public ScreenshotFilesUnavailableException()
		: base("the Worker couldn't read a screenshot by reference")
	{
	}
}

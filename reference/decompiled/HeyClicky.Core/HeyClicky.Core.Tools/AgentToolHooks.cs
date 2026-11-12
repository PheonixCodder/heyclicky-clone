using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeyClicky.Core.Tools;

public static class AgentToolHooks
{
	public static volatile Func<bool>? IsTourInterviewFoundingOnly;

	public static volatile Func<string?, Task<string>>? SaveDrawing;

	public static volatile Func<string, string>? ShowResult;

	public static volatile Func<string, Task<string>>? Routines;

	public static volatile Func<AgentToolLaunch, string?>? HoldJobForChrome;

	public static volatile Func<string, string, Task<string?>>? StartWaitingJobNamed;

	public static volatile Func<string?, string, Task<string?>>? AnswerChromeSetup;

	public static volatile Func<string>? ChromeSetupBrowserName;

	public static volatile Func<string, string>? JobIdForThread;

	public static volatile bool JobsEnabled = true;

	public static bool BrainAskedAboutChrome(IEnumerable<string> spokenLines)
	{
		return spokenLines.Any((string line) => line.Contains("chrome", StringComparison.CurrentCultureIgnoreCase));
	}
}

using System;
using System.Collections.Generic;

namespace HeyClicky.Core.Browser;

public sealed class BrowserTurnUsage(string thread, DateTime firstCallAt)
{
	public string Thread { get; } = thread;

	public DateTime FirstCallAt { get; } = firstCallAt;

	public int OperationCount { get; set; }

	public Dictionary<string, int> OperationCounts { get; } = new Dictionary<string, int>();

	public int BatchStepCount { get; set; }

	public int CommandsSentToBrowser { get; set; }

	public int ErrorCount { get; set; }

	public Dictionary<string, int> ErrorKinds { get; } = new Dictionary<string, int>();

	public SortedSet<string> DetachReasons { get; } = new SortedSet<string>(StringComparer.Ordinal);

	public int TimeoutCount { get; set; }

	public Dictionary<string, int> TimeoutOperations { get; } = new Dictionary<string, int>();

	public int TabsKept { get; set; }

	public int ScreenshotsSaved { get; set; }

	public bool WasStoppedByUser { get; set; }

	public bool AccessEndedByTimeout { get; set; }

	public DateTime? EndedAt { get; set; }
}

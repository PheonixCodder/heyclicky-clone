using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Telemetry;

public static class CrashReporter
{
	private sealed class Off : ICrashReporter
	{
		public bool IsEnabled => false;

		public void AddBreadcrumb(string category, string? message, CrashReportLevel level, JsonObject? data)
		{
		}

		public void CaptureMessage(string message, CrashReportLevel level, IReadOnlyList<string> fingerprint, IReadOnlyDictionary<string, string> tags, string contextName, JsonObject context, IReadOnlyList<string>? attachmentPaths = null)
		{
		}

		public void CaptureException(Exception exception, string mechanism, bool handled)
		{
		}

		public void SetUser(string? id, string? email, string? name)
		{
		}

		public void ClearBreadcrumbs()
		{
		}

		public Task FlushAsync(TimeSpan timeout)
		{
			return Task.CompletedTask;
		}
	}

	public static ICrashReporter Current { get; private set; } = new Off();

	public static void Install(ICrashReporter reporter)
	{
		Current = reporter;
	}
}

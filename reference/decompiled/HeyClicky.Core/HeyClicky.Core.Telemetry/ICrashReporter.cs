using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.Core.Telemetry;

public interface ICrashReporter
{
	bool IsEnabled { get; }

	void AddBreadcrumb(string category, string? message, CrashReportLevel level, JsonObject? data);

	void CaptureMessage(string message, CrashReportLevel level, IReadOnlyList<string> fingerprint, IReadOnlyDictionary<string, string> tags, string contextName, JsonObject context, IReadOnlyList<string>? attachmentPaths = null);

	void CaptureException(Exception exception, string mechanism, bool handled);

	void SetUser(string? id, string? email, string? name);

	void ClearBreadcrumbs();

	Task FlushAsync(TimeSpan timeout);
}

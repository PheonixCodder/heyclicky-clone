using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Audio;

namespace HeyClicky.App.Settings;

public static class BugReport
{
	private const int MaxLogBytes = 3145728;

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	public static string Run(SettingsHost host)
	{
		string text = "clicky-" + Guid.NewGuid().ToString().ToLowerInvariant();
		string text2 = host.Billing?.State.ReportPlanLabel ?? "unknown";
		string text3 = null;
		try
		{
			text3 = WriteDiagnostics(host, text, text2);
		}
		catch (Exception ex)
		{
			Log.Warn("bugreport", "diagnostics zip failed: " + ex.Message);
		}
		Version version = typeof(BugReport).Assembly.GetName().Version;
		bool isEnabled = CrashReporter.Current.IsEnabled;
		StringBuilder stringBuilder = new StringBuilder().Append("Hey team,\n\n<tell us about your bug or request>\n\n--\n\n");
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(18, 3, stringBuilder);
		handler.AppendLiteral("App: HeyClicky ");
		handler.AppendFormatted(version?.Major);
		handler.AppendLiteral(".");
		handler.AppendFormatted(version?.Minor);
		handler.AppendLiteral(".");
		handler.AppendFormatted(version?.Build);
		handler.AppendLiteral("\n");
		StringBuilder stringBuilder2 = stringBuilder.Append(ref handler);
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(13, 2, stringBuilder2);
		handler2.AppendLiteral("Windows: ");
		handler2.AppendFormatted(Environment.OSVersion.Version);
		handler2.AppendLiteral(" (");
		handler2.AppendFormatted(RuntimeInformation.OSArchitecture);
		handler2.AppendLiteral(")\n");
		StringBuilder stringBuilder3 = stringBuilder2.Append(ref handler2);
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder3);
		handler3.AppendLiteral("PC: ");
		handler3.AppendFormatted(Environment.MachineName);
		handler3.AppendLiteral("\n");
		StringBuilder stringBuilder4 = stringBuilder3.Append(ref handler3);
		StringBuilder.AppendInterpolatedStringHandler handler4 = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder4);
		handler4.AppendLiteral("Number of screens: ");
		handler4.AppendFormatted(GetSystemMetrics(80));
		handler4.AppendLiteral("\n");
		StringBuilder stringBuilder5 = stringBuilder4.Append(ref handler4);
		string text4 = host.Billing?.State.PaidPlanName;
		StringBuilder stringBuilder6 = stringBuilder5.Append((text4 != null) ? ("Plan: " + text4 + "\n") : "");
		StringBuilder.AppendInterpolatedStringHandler handler5 = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder6);
		handler5.AppendLiteral("Report ID: ");
		handler5.AppendFormatted(text);
		string stringToEscape = stringBuilder6.Append(ref handler5).Append((text3 == null || isEnabled) ? "" : ("\nDiagnostics: please attach " + Path.GetFileName(text3))).ToString();
		string url = "https://mail.google.com/mail/?view=cm&fs=1&to=hi%40heyclicky.com&su=" + Uri.EscapeDataString("hey clicky") + "&body=" + Uri.EscapeDataString(stringToEscape);
		host.OpenUrl(url);
		host.App?.FoldNotchForOpenedLink();
		ClickyAnalytics.TrackBugReportSubmitted(text);
		if (text3 != null && !isEnabled && Environment.GetEnvironmentVariable("HEYCLICKY_E2E_NO_BROWSER") != "1")
		{
			try
			{
				Process.Start("explorer.exe", "/select,\"" + text3 + "\"");
			}
			catch (Exception ex2)
			{
				Log.Warn("bugreport", "reveal failed: " + ex2.Message);
			}
		}
		host.Worker?.ReportError(new ErrorReport("bug_report", "user_report", $"Bug report {text} (diagnostics: {((text3 == null) ? "none" : "local zip")})", "", $"{version?.Major}.{version?.Minor}.{version?.Build}", 0, "bug_report", host.Services.Auth.User?.UserId, host.Services.Auth.User?.Email));
		if (isEnabled)
		{
			SendToSentry(host, text, text2, $"{version?.Major}.{version?.Minor}.{version?.Build}");
		}
		Log.Info("bugreport", $"report {text} ({text2}), diagnostics {text3 ?? "none"}");
		return text;
	}

	private static void SendToSentry(SettingsHost host, string id, string plan, string appVersion)
	{
		try
		{
			string dir = AppPaths.Combine("bug-reports");
			Directory.CreateDirectory(dir);
			JsonObject state = new JsonObject((JsonNodeOptions?)null)
			{
				["schemaVersion"] = 4,
				["report_id"] = id,
				["app_version"] = appVersion,
				["windows_version"] = Environment.OSVersion.Version.ToString(),
				["os_arch"] = RuntimeInformation.OSArchitecture.ToString(),
				["process_arch"] = RuntimeInformation.ProcessArchitecture.ToString(),
				["install_location"] = AppContext.BaseDirectory,
				["plan"] = plan,
				["signed_in"] = host.IsSignedIn,
				["voice_state"] = host.Services.Companion?.State.ToString(),
				["agent_task_count"] = host.Services.Agents?.Tasks.Count,
				["selected_microphone"] = host.Read((AppSettings x) => x.MicrophoneDeviceId) ?? "System Default",
				["selected_output_device"] = host.Read((AppSettings x) => x.OutputDeviceId) ?? "System Default",
				["display_count"] = GetSystemMetrics(80),
				["recentErrors"] = new JsonArray(((IEnumerable<ClickyAnalytics.RecentAppError>)ClickyAnalytics.RecentAppErrorsSnapshot()).Select((Func<ClickyAnalytics.RecentAppError, JsonNode>)((ClickyAnalytics.RecentAppError e) => new JsonObject((JsonNodeOptions?)null)
				{
					["timestamp"] = e.TimestampIso8601,
					["stage"] = e.Stage,
					["kind"] = e.Kind,
					["error_message"] = e.ErrorMessage,
					["error_domain"] = e.ErrorDomain,
					["error_code"] = e.ErrorCode,
					["user_facing_message"] = e.UserFacingMessage
				})).ToArray())
			};
			JsonObject context = new JsonObject((JsonNodeOptions?)null)
			{
				["report_id"] = id,
				["app_version"] = appVersion,
				["voice_state"] = host.Services.Companion?.State.ToString(),
				["signed_in"] = host.IsSignedIn,
				["running_agent_task_count"] = host.Services.Agents?.Tasks.Count
			};
			Task.Run(delegate
			{
				try
				{
					string text = Path.Combine(dir, "clicky-logs-" + id + ".log");
					File.WriteAllText(text, RedactSensitiveValues(RecentLogText()), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					string text2 = Path.Combine(dir, "state-" + id + ".json");
					File.WriteAllText(text2, RedactSensitiveValues(state.ToJsonString(new JsonSerializerOptions
					{
						WriteIndented = true
					})), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
					CrashReporter.Current.CaptureMessage("User Bug Report", CrashReportLevel.Info, new global::_003C_003Ez__ReadOnlySingleElementList<string>("user-bug-report"), new Dictionary<string, string>
					{
						["report_id"] = id,
						["plan"] = plan
					}, "clicky_state", context, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2] { text, text2 }));
					Log.Info("bugreport", $"report {id} sent to Sentry with {new FileInfo(text).Length / 1024} KB of logs and the state snapshot");
				}
				catch (Exception ex)
				{
					Log.Warn("bugreport", "Sentry report failed: " + ex.Message);
				}
			});
		}
		catch (Exception ex2)
		{
			Log.Warn("bugreport", "Sentry report failed: " + ex2.Message);
		}
	}

	private static string RecentLogText()
	{
		List<FileInfo> list = (from f in Directory.Exists(Log.Directory) ? ((IEnumerable<string>)Directory.GetFiles(Log.Directory, "*.log")) : ((IEnumerable<string>)Array.Empty<string>())
			select new FileInfo(f) into f
			where DateTime.Now - f.LastWriteTime <= TimeSpan.FromDays(2)
			orderby f.LastWriteTime descending
			select f).ToList();
		List<string> list2 = new List<string>();
		long num = 3145728L;
		bool flag = false;
		foreach (FileInfo item in list)
		{
			if (num <= 0)
			{
				flag = true;
				break;
			}
			using FileStream fileStream = new FileStream(item.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			long num2 = Math.Min(num, fileStream.Length);
			if (num2 < fileStream.Length)
			{
				fileStream.Seek(-num2, SeekOrigin.End);
				flag = true;
			}
			byte[] array = new byte[num2];
			fileStream.ReadExactly(array);
			string text = Encoding.UTF8.GetString(array);
			if (num2 < fileStream.Length)
			{
				int num3 = text.IndexOf('\n');
				if (num3 >= 0)
				{
					string text2 = text;
					int num4 = num3 + 1;
					text = text2.Substring(num4, text2.Length - num4);
				}
			}
			list2.Insert(0, "===== " + item.Name + " =====\n" + text);
			num -= num2;
		}
		string text3 = string.Join("\n", list2);
		if (!flag)
		{
			return text3;
		}
		return "{\"_note\":\"older log lines truncated to keep the most recent 3MB\"}\n" + text3;
	}

	public static string RedactSensitiveValues(string text)
	{
		text = Regex.Replace(text, "(?i)(access_token|refresh_token|id_token|provider_token)=[A-Za-z0-9._~+/\\-]+", "$1=[redacted]");
		text = Regex.Replace(text, "eyJ[A-Za-z0-9_-]{10,}(?:\\.[A-Za-z0-9_-]+){0,2}", "[redacted]");
		text = Regex.Replace(text, "(?i)bearer\\s+[A-Za-z0-9._\\-]+", "[redacted]");
		text = Regex.Replace(text, "sk-[A-Za-z0-9_-]{16,}", "[redacted]");
		return text;
	}

	private static string WriteDiagnostics(SettingsHost host, string id, string plan)
	{
		string text = AppPaths.Combine("bug-reports");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, id + ".zip");
		using ZipArchive zipArchive = ZipFile.Open(text2, ZipArchiveMode.Create);
		string[] array = (Directory.Exists(Log.Directory) ? Directory.GetFiles(Log.Directory, "*.log") : Array.Empty<string>());
		foreach (string text3 in array)
		{
			FileInfo fileInfo = new FileInfo(text3);
			if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(2) || fileInfo.Length > 41943040)
			{
				continue;
			}
			using FileStream fileStream = new FileStream(text3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using Stream destination = zipArchive.CreateEntry("logs/" + fileInfo.Name, CompressionLevel.Optimal).Open();
			fileStream.CopyTo(destination);
		}
		string filePath = host.Store.FilePath;
		if (File.Exists(filePath))
		{
			host.Store.Flush();
			using FileStream fileStream2 = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using Stream destination2 = zipArchive.CreateEntry("settings.json").Open();
			fileStream2.CopyTo(destination2);
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder);
		handler.AppendLiteral("report: ");
		handler.AppendFormatted(id);
		StringBuilder stringBuilder2 = stringBuilder.AppendLine(ref handler);
		StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
		handler2.AppendLiteral("time: ");
		handler2.AppendFormatted(DateTimeOffset.Now, "O");
		StringBuilder stringBuilder3 = stringBuilder2.AppendLine(ref handler2);
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder3);
		handler3.AppendLiteral("plan: ");
		handler3.AppendFormatted(plan);
		StringBuilder stringBuilder4 = stringBuilder3.AppendLine(ref handler3);
		StringBuilder.AppendInterpolatedStringHandler handler4 = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder4);
		handler4.AppendLiteral("signed in: ");
		handler4.AppendFormatted(host.IsSignedIn);
		StringBuilder stringBuilder5 = stringBuilder4.AppendLine(ref handler4);
		StringBuilder.AppendInterpolatedStringHandler handler5 = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder5);
		handler5.AppendLiteral("voice state: ");
		handler5.AppendFormatted(host.Services.Companion?.State);
		StringBuilder stringBuilder6 = stringBuilder5.AppendLine(ref handler5);
		StringBuilder.AppendInterpolatedStringHandler handler6 = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder6);
		handler6.AppendLiteral("agent tasks: ");
		handler6.AppendFormatted(host.Services.Agents?.Tasks.Count);
		StringBuilder stringBuilder7 = stringBuilder6.AppendLine(ref handler6);
		StringBuilder.AppendInterpolatedStringHandler handler7 = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder7);
		handler7.AppendLiteral("os: ");
		handler7.AppendFormatted(Environment.OSVersion);
		handler7.AppendLiteral(" ");
		handler7.AppendFormatted(RuntimeInformation.OSArchitecture);
		StringBuilder stringBuilder8 = stringBuilder7.AppendLine(ref handler7);
		StringBuilder.AppendInterpolatedStringHandler handler8 = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder8);
		handler8.AppendLiteral("process: ");
		handler8.AppendFormatted(RuntimeInformation.ProcessArchitecture);
		StringBuilder stringBuilder9 = stringBuilder8.AppendLine(ref handler8);
		StringBuilder.AppendInterpolatedStringHandler handler9 = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder9);
		handler9.AppendLiteral("microphone pick: ");
		handler9.AppendFormatted(host.Read((AppSettings x) => x.MicrophoneDeviceId) ?? "System Default");
		StringBuilder stringBuilder10 = stringBuilder9.AppendLine(ref handler9);
		StringBuilder.AppendInterpolatedStringHandler handler10 = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder10);
		handler10.AppendLiteral("output pick: ");
		handler10.AppendFormatted(host.Read((AppSettings x) => x.OutputDeviceId) ?? "System Default");
		StringBuilder stringBuilder11 = stringBuilder10.AppendLine(ref handler10);
		StringBuilder.AppendInterpolatedStringHandler handler11 = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder11);
		handler11.AppendLiteral("audio topology: ");
		handler11.AppendFormatted(AudioTopologyMonitor.Shared.Snapshot);
		StringBuilder stringBuilder12 = stringBuilder11.AppendLine(ref handler11);
		StringBuilder.AppendInterpolatedStringHandler handler12 = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder12);
		handler12.AppendLiteral("hotkeys: ");
		handler12.AppendFormatted(host.Read((AppSettings x) => x.EffectiveHotkeys));
		StringBuilder stringBuilder13 = stringBuilder12.AppendLine(ref handler12);
		using (StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("app-state.txt").Open()))
		{
			streamWriter.Write(stringBuilder13.ToString());
		}
		return text2;
	}
}

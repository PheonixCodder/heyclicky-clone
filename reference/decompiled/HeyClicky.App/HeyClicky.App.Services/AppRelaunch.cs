using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HeyClicky.App.Services;

public static class AppRelaunch
{
	public static bool ScheduleRelaunch(IEnumerable<string>? arguments = null)
	{
		string processPath = Environment.ProcessPath;
		if (string.IsNullOrEmpty(processPath))
		{
			return false;
		}
		List<string> list = arguments?.ToList() ?? new List<string>();
		string text = ((list.Count == 0) ? "" : (" -ArgumentList " + string.Join(",", list.Select(Quote))));
		return RunAfterExit("Start-Process -FilePath " + Quote(processPath) + text, "relaunch");
	}

	public static bool RunAfterExit(string script, string purpose)
	{
		try
		{
			int processId = Environment.ProcessId;
			string s = "$ProgressPreference='SilentlyContinue'\n" + $"$attempt=0; while (Get-Process -Id {processId} -ErrorAction SilentlyContinue) {{ $attempt++; if ($attempt -ge 300) {{ exit 1 }}; Start-Sleep -Milliseconds 100 }}\n" + script;
			string item = Convert.ToBase64String(Encoding.Unicode.GetBytes(s));
			using Process process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"))
			{
				ArgumentList = { "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-WindowStyle", "Hidden", "-EncodedCommand", item },
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = Path.GetTempPath()
			});
			Log.Info("relaunch", $"{purpose}: helper {process?.Id} waits for pid {processId} to exit");
			return process != null;
		}
		catch (Exception ex)
		{
			Log.Warn("relaunch", purpose + ": helper failed to start: " + ex.Message);
			return false;
		}
	}

	public static string Quote(string value)
	{
		return "'" + value.Replace("'", "''") + "'";
	}
}

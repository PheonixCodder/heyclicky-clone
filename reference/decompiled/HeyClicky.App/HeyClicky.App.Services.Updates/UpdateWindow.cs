using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace HeyClicky.App.Services.Updates;

internal static class UpdateWindow
{
	private const string Area = "updates";

	private const string ExeName = "HeyClicky-Update.exe";

	private const string AppReadyEventName = "Local\\HeyClicky.AppReady";

	public static bool TryShow(string? packageFileName)
	{
		try
		{
			string text = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string text2 = Path.GetDirectoryName(text) ?? "";
			string text3 = Path.Combine(text, "HeyClicky-Update.exe");
			if (!File.Exists(text3) || !File.Exists(Path.Combine(text2, "Update.exe")))
			{
				Log.Info("updates", "no update window in this build");
				return false;
			}
			string text4 = AppPaths.Combine("updater");
			Directory.CreateDirectory(text4);
			string text5 = Path.Combine(text4, "HeyClicky-Update.exe");
			try
			{
				File.Copy(text3, text5, overwrite: true);
			}
			catch (IOException)
			{
				text5 = Path.Combine(text4, $"HeyClicky-Update-{Environment.ProcessId}.exe");
				File.Copy(text3, text5, overwrite: true);
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo(text5)
			{
				UseShellExecute = false,
				WorkingDirectory = text4
			};
			processStartInfo.ArgumentList.Add("--update-window");
			processStartInfo.ArgumentList.Add("--pid");
			processStartInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
			processStartInfo.ArgumentList.Add("--root");
			processStartInfo.ArgumentList.Add(text2);
			if (!string.IsNullOrEmpty(packageFileName))
			{
				processStartInfo.ArgumentList.Add("--package");
				processStartInfo.ArgumentList.Add(Path.Combine(text2, "packages", packageFileName));
			}
			processStartInfo.ArgumentList.Add("--log");
			processStartInfo.ArgumentList.Add(Path.Combine(Log.Directory, "update-window.log"));
			Process.Start(processStartInfo)?.Dispose();
			Log.Info("updates", "update window shown");
			return true;
		}
		catch (Exception ex2)
		{
			Log.Warn("updates", "update window didn't start: " + ex2.Message);
			return false;
		}
	}

	public static void SignalAppReady()
	{
		try
		{
			if (EventWaitHandle.TryOpenExisting("Local\\HeyClicky.AppReady", out EventWaitHandle result))
			{
				using (result)
				{
					result.Set();
				}
				Log.Info("updates", "told the installer / update window HeyClicky is up");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "app-ready signal: " + ex.Message);
		}
	}
}

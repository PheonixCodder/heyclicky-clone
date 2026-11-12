using System;
using Microsoft.Win32;

namespace HeyClicky.Platform.Win32;

public static class LaunchAtLogin
{
	private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	public const string StartupArgument = "--launched-at-login";

	public static bool IsEnabled(string valueName = "HeyClicky")
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
		return registryKey?.GetValue(valueName) is string;
	}

	public static void SetEnabled(bool enabled, string valueName = "HeyClicky", string? executablePath = null)
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
		if (enabled)
		{
			if (executablePath == null)
			{
				executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("no process path");
			}
			registryKey.SetValue(valueName, "\"" + executablePath + "\" --launched-at-login");
		}
		else
		{
			registryKey.DeleteValue(valueName, throwOnMissingValue: false);
		}
	}

	public static string? GetCommand(string valueName = "HeyClicky")
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
		return registryKey?.GetValue(valueName) as string;
	}
}

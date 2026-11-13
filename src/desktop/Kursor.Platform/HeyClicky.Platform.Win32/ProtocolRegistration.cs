using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace HeyClicky.Platform.Win32;

public static class ProtocolRegistration
{
	public static readonly string[] Schemes = new string[3] { "kursor", "clicky", "heyclicky" };

	public static void Register(string? executablePath = null)
	{
		if (executablePath == null)
		{
			executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("no process path");
		}
		string[] schemes = Schemes;
		foreach (string text in schemes)
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + text);
			registryKey.SetValue("", "URL:HeyClicky (" + text + ")");
			registryKey.SetValue("URL Protocol", "");
			using (RegistryKey registryKey2 = registryKey.CreateSubKey("DefaultIcon"))
			{
				registryKey2.SetValue("", "\"" + executablePath + "\",0");
			}
			using RegistryKey registryKey3 = registryKey.CreateSubKey("shell\\open\\command");
			registryKey3.SetValue("", "\"" + executablePath + "\" \"%1\"");
		}
	}

	public static bool IsRegistered(string? executablePath = null)
	{
		if (executablePath == null)
		{
			executablePath = Environment.ProcessPath;
		}
		string[] schemes = Schemes;
		foreach (string text in schemes)
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + text + "\\shell\\open\\command");
			if (!(registryKey?.GetValue("") is string text2) || !text2.Contains(executablePath ?? "", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	public static void Unregister()
	{
		string[] schemes = Schemes;
		foreach (string text in schemes)
		{
			Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\" + text, throwOnMissingSubKey: false);
		}
	}

	public static Uri? FindDeepLink(IEnumerable<string> args)
	{
		foreach (string arg in args)
		{
			if (Uri.TryCreate(arg, UriKind.Absolute, out Uri result) && Schemes.Contains<string>(result.Scheme, StringComparer.OrdinalIgnoreCase))
			{
				return result;
			}
		}
		return null;
	}
}

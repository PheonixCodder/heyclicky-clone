using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace HeyClicky.Core.Identity;

public static class ClickySystemEnvironment
{
	private const int SM_CMONITORS = 80;

	public static void Write(Utf8JsonWriter writer, string ianaTimeZone)
	{
		writer.WriteStartObject("environment");
		writer.WriteString("platform", "windows");
		writer.WriteString("timezone", ianaTimeZone);
		writer.WriteString("locale", CultureInfo.CurrentCulture.Name);
		writer.WriteString("os_version", ClickyClientIdentity.OsVersion());
		writer.WriteNumber("display_count", DisplayCount());
		IReadOnlyList<string> readOnlyList = PreferredLanguages();
		if (readOnlyList.Count > 0)
		{
			writer.WriteStartArray("preferred_languages");
			foreach (string item in readOnlyList)
			{
				writer.WriteStringValue(item);
			}
			writer.WriteEndArray();
		}
		string text = DeviceModel();
		if (text != null)
		{
			writer.WriteString("device_model", text);
		}
		writer.WriteEndObject();
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	public static int DisplayCount()
	{
		try
		{
			return Math.Max(1, GetSystemMetrics(80));
		}
		catch
		{
			return 1;
		}
	}

	public static IReadOnlyList<string> PreferredLanguages()
	{
		List<string> list = new List<string>();
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\International\\User Profile");
			if (registryKey?.GetValue("Languages") is string[] array)
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (!string.IsNullOrWhiteSpace(text) && !list.Contains(text.Trim()))
					{
						list.Add(text.Trim());
					}
				}
			}
		}
		catch
		{
		}
		if (list.Count == 0)
		{
			list.Add(CultureInfo.CurrentUICulture.Name);
		}
		return list.Take(10).ToList();
	}

	public static string? DeviceModel()
	{
		List<string> list = new List<string>();
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\BIOS");
			string text = (registryKey?.GetValue("SystemProductName") as string)?.Trim();
			if (text != null && text.Length > 0)
			{
				list.Add(text);
			}
			using RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
			string text2 = (registryKey2?.GetValue("ProcessorNameString") as string)?.Trim();
			if (text2 != null && text2.Length > 0)
			{
				list.Add(text2);
			}
		}
		catch
		{
		}
		string text3 = string.Join(", ", list);
		string text4 = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
		text3 += ((text3.Length == 0) ? text4 : (" (" + text4 + ")"));
		if (text3.Length != 0)
		{
			if (text3.Length <= 160)
			{
				return text3;
			}
			return text3.Substring(0, 160);
		}
		return null;
	}
}

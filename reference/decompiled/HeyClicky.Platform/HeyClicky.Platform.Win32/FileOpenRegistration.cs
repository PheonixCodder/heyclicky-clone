using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace HeyClicky.Platform.Win32;

public static class FileOpenRegistration
{
	public const string ProgId = "HeyClicky.Document";

	private const string AppKey = "Software\\Classes\\Applications\\HeyClicky.exe";

	public static readonly string[] DefaultTypes = new string[22]
	{
		".md", ".markdown", ".txt", ".log", ".csv", ".tsv", ".json", ".xml", ".yaml", ".yml",
		".pdf", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".docx", ".odt", ".rtf",
		".pptx", ".xlsx"
	};

	public static void Register(IEnumerable<string>? extensions = null, string? executablePath = null)
	{
		if (executablePath == null)
		{
			executablePath = Environment.ProcessPath ?? throw new InvalidOperationException("no process path");
		}
		List<string> list = (extensions ?? DefaultTypes).ToList();
		string value = "\"" + executablePath + "\" \"%1\"";
		if (IsRegistered(list, executablePath))
		{
			return;
		}
		using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\HeyClicky.Document"))
		{
			registryKey.SetValue("", "HeyClicky");
			registryKey.SetValue("FriendlyTypeName", "HeyClicky");
			using (RegistryKey registryKey2 = registryKey.CreateSubKey("DefaultIcon"))
			{
				registryKey2.SetValue("", "\"" + executablePath + "\",0");
			}
			using (RegistryKey registryKey3 = registryKey.CreateSubKey("shell\\open"))
			{
				registryKey3.SetValue("FriendlyAppName", "HeyClicky");
			}
			using RegistryKey registryKey4 = registryKey.CreateSubKey("shell\\open\\command");
			registryKey4.SetValue("", value);
		}
		using (RegistryKey registryKey5 = Registry.CurrentUser.CreateSubKey("Software\\Classes\\Applications\\HeyClicky.exe"))
		{
			registryKey5.SetValue("FriendlyAppName", "HeyClicky");
			using (RegistryKey registryKey6 = registryKey5.CreateSubKey("shell\\open\\command"))
			{
				registryKey6.SetValue("", value);
			}
			using RegistryKey registryKey7 = registryKey5.CreateSubKey("SupportedTypes");
			foreach (string item in list)
			{
				registryKey7.SetValue(item, "");
			}
		}
		foreach (string item2 in list)
		{
			using RegistryKey registryKey8 = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + item2 + "\\OpenWithProgids");
			registryKey8.SetValue("HeyClicky.Document", Array.Empty<byte>(), RegistryValueKind.None);
		}
		SHChangeNotify(134217728, 0u, IntPtr.Zero, IntPtr.Zero);
		Log.Info("Registration", $"open-with registered for {list.Count} file types");
	}

	public static bool IsRegistered(IEnumerable<string>? extensions = null, string? executablePath = null)
	{
		if (executablePath == null)
		{
			executablePath = Environment.ProcessPath;
		}
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\HeyClicky.Document\\shell\\open\\command");
		if (!(registryKey?.GetValue("") is string text) || !text.Contains(executablePath ?? "", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		foreach (string item in extensions ?? DefaultTypes)
		{
			using RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + item + "\\OpenWithProgids");
			if (registryKey2 == null || !registryKey2.GetValueNames().Contains("HeyClicky.Document"))
			{
				return false;
			}
		}
		return true;
	}

	public static void Unregister(IEnumerable<string>? extensions = null)
	{
		foreach (string item in extensions ?? DefaultTypes)
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + item + "\\OpenWithProgids", writable: true);
			registryKey?.DeleteValue("HeyClicky.Document", throwOnMissingValue: false);
		}
		Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\HeyClicky.Document", throwOnMissingSubKey: false);
		Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\Applications\\HeyClicky.exe", throwOnMissingSubKey: false);
		SHChangeNotify(134217728, 0u, IntPtr.Zero, IntPtr.Zero);
	}

	public static string? DefaultHandlerFor(string path)
	{
		string extension = Path.GetExtension(path);
		if (string.IsNullOrEmpty(extension))
		{
			return null;
		}
		uint outSize = 0u;
		AssocQueryString(32, 2, extension, null, null, ref outSize);
		if (outSize == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder((int)outSize);
		if (AssocQueryString(32, 2, extension, null, stringBuilder, ref outSize) != 0)
		{
			return null;
		}
		return stringBuilder.ToString();
	}

	[DllImport("shell32.dll")]
	private static extern void SHChangeNotify(int eventId, uint flags, nint item1, nint item2);

	[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
	private static extern int AssocQueryString(int flags, int str, string assoc, string? extra, StringBuilder? outBuffer, ref uint outSize);
}

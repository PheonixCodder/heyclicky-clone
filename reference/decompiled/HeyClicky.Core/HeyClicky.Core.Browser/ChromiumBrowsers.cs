using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace HeyClicky.Core.Browser;

public static class ChromiumBrowsers
{
	private sealed record PreferredCache(DateTime At, (string Name, string Executable)? Browser);

	public const string NativeHostName = "com.heyclicky.browser";

	private static volatile string? lastConnectedBrowserName;

	private static volatile PreferredCache? preferredCache;

	private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

	private static string ProgramFiles => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

	private static string ProgramFilesX86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

	public static IReadOnlyList<ChromiumBrowser> All { get; } = new global::_003C_003Ez__ReadOnlyArray<ChromiumBrowser>(new ChromiumBrowser[9]
	{
		new ChromiumBrowser("Chrome", "Google\\Chrome", "Google\\Chrome\\User Data", Installs("Google\\Chrome\\Application\\chrome.exe"), "chrome.exe", null, "ChromeHTML"),
		new ChromiumBrowser("Edge", "Microsoft\\Edge", "Microsoft\\Edge\\User Data", Installs("Microsoft\\Edge\\Application\\msedge.exe"), "msedge.exe", null, "MSEdgeHTM"),
		new ChromiumBrowser("Brave", "BraveSoftware\\Brave-Browser", "BraveSoftware\\Brave-Browser\\User Data", Installs("BraveSoftware\\Brave-Browser\\Application\\brave.exe"), "brave.exe", null, "BraveHTML"),
		new ChromiumBrowser("Arc", null, "Packages", Array.Empty<string>(), null, "TheBrowserCompany.Arc_", "Arc"),
		new ChromiumBrowser("Vivaldi", "Vivaldi", "Vivaldi\\User Data", Installs("Vivaldi\\Application\\vivaldi.exe"), "vivaldi.exe", null, "VivaldiHTM"),
		new ChromiumBrowser("Chrome Beta", "Google\\Chrome Beta", "Google\\Chrome Beta\\User Data", Installs("Google\\Chrome Beta\\Application\\chrome.exe"), null, null, "ChromeBHTML"),
		new ChromiumBrowser("Chrome Dev", "Google\\Chrome Dev", "Google\\Chrome Dev\\User Data", Installs("Google\\Chrome Dev\\Application\\chrome.exe"), null, null, "ChromeDHTML"),
		new ChromiumBrowser("Chrome Canary", "Google\\Chrome SxS", "Google\\Chrome SxS\\User Data", Installs("Google\\Chrome SxS\\Application\\chrome.exe", userLevel: true, machineLevel: false), null, null, "ChromeSSHTM"),
		new ChromiumBrowser("Chromium", "Chromium", "Chromium\\User Data", Installs("Chromium\\Application\\chrome.exe"), null, null, "ChromiumHTM")
	});

	public static IReadOnlyList<string> ExecutableNames { get; } = new global::_003C_003Ez__ReadOnlyArray<string>(new string[5] { "chrome.exe", "msedge.exe", "brave.exe", "vivaldi.exe", "arc.exe" });

	public static IReadOnlyList<string> InstalledNames => (from b in All
		where ExecutablePath(b) != null
		select b.Name).ToList();

	public static string? LastConnectedBrowserName
	{
		get
		{
			return lastConnectedBrowserName;
		}
		set
		{
			lastConnectedBrowserName = value;
			preferredCache = null;
		}
	}

	private static IReadOnlyList<string> Installs(string relative, bool userLevel = true, bool machineLevel = true)
	{
		List<string> list = new List<string>();
		if (machineLevel)
		{
			list.Add(Path.Combine(ProgramFiles, relative));
			list.Add(Path.Combine(ProgramFilesX86, relative));
		}
		if (userLevel)
		{
			list.Add(Path.Combine(LocalAppData, relative));
		}
		return list;
	}

	public static string? ExecutablePath(ChromiumBrowser browser)
	{
		foreach (string executable in browser.Executables)
		{
			if (File.Exists(executable))
			{
				return executable;
			}
		}
		string appPathsName = browser.AppPathsName;
		if (appPathsName != null)
		{
			RegistryKey[] array = new RegistryKey[2]
			{
				Registry.CurrentUser,
				Registry.LocalMachine
			};
			foreach (RegistryKey registryKey in array)
			{
				try
				{
					using RegistryKey registryKey2 = registryKey.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + appPathsName);
					object obj = registryKey2?.GetValue(null);
					string path = obj as string;
					if (path != null && File.Exists(path.Trim('"')) && browser.Executables.Any(delegate(string e)
					{
						string fileName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(e)));
						return fileName != null && path.Contains(fileName, StringComparison.OrdinalIgnoreCase);
					}))
					{
						return path.Trim('"');
					}
				}
				catch
				{
				}
			}
		}
		string packageFamilyPrefix = browser.PackageFamilyPrefix;
		if (packageFamilyPrefix != null)
		{
			try
			{
				if (Directory.EnumerateDirectories(Path.Combine(LocalAppData, "Packages"), packageFamilyPrefix + "*").Any())
				{
					string text = Path.Combine(LocalAppData, "Microsoft\\WindowsApps\\Arc.exe");
					return File.Exists(text) ? text : "";
				}
			}
			catch
			{
			}
		}
		return null;
	}

	public static IReadOnlyList<ChromiumBrowser> InstalledOrUsed()
	{
		return All.Where((ChromiumBrowser b) => ExecutablePath(b) != null || (b.PackageFamilyPrefix == null && Directory.Exists(Path.Combine(LocalAppData, b.UserDataFolder)))).ToList();
	}

	public static (string Name, string Executable)? Preferred()
	{
		PreferredCache preferredCache = ChromiumBrowsers.preferredCache;
		if ((object)preferredCache != null && (DateTime.UtcNow - preferredCache.At).TotalSeconds < 10.0)
		{
			return preferredCache.Browser;
		}
		(string, string)? tuple = ResolvePreferred(LastConnectedBrowserName, DefaultBrowserProgId(), LastActiveTime);
		ChromiumBrowsers.preferredCache = new PreferredCache(DateTime.UtcNow, tuple);
		return tuple;
	}

	public static void ForgetPreferred()
	{
		preferredCache = null;
	}

	public static (string Name, string Executable)? ResolvePreferred(string? lastConnected, string? defaultProgId, Func<ChromiumBrowser, double?> lastActiveTime, Func<ChromiumBrowser, string?>? executablePath = null)
	{
		if (executablePath == null)
		{
			executablePath = ExecutablePath;
		}
		List<(ChromiumBrowser, string)> list = (from b in All
			select (Browser: b, Path: executablePath(b)) into b
			where b.Path != null
			select b).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		(ChromiumBrowser, string) b2 = list.FirstOrDefault<(ChromiumBrowser, string)>(((ChromiumBrowser Browser, string Path) b) => b.Browser.Name == lastConnected);
		if ((object)b2.Item1 != null)
		{
			return Of(b2);
		}
		ChromiumBrowser defaultBrowser = BrowserForProgId(defaultProgId);
		if ((object)defaultBrowser != null)
		{
			(ChromiumBrowser, string) b3 = list.FirstOrDefault<(ChromiumBrowser, string)>(((ChromiumBrowser Browser, string Path) b) => b.Browser == defaultBrowser);
			if ((object)b3.Item1 != null)
			{
				return Of(b3);
			}
		}
		(ChromiumBrowser, string, double?) tuple = (from b in list.Select<(ChromiumBrowser, string), (ChromiumBrowser, string, double?)>(((ChromiumBrowser Browser, string Path) b) => (Browser: b.Browser, Path: b.Path, Active: lastActiveTime(b.Browser))).Where<(ChromiumBrowser, string, double?)>(delegate((ChromiumBrowser Browser, string Path, double? Active) b)
			{
				double? item = b.Active;
				return item.HasValue;
			})
			orderby b.Active descending
			select b).FirstOrDefault();
		if ((object)tuple.Item1 != null)
		{
			return (tuple.Item1.Name, tuple.Item2);
		}
		return Of(list[0]);
		static (string Name, string Executable) Of((ChromiumBrowser Browser, string? Path) b)
		{
			return (Name: b.Browser.Name, Executable: b.Path);
		}
	}

	public static ChromiumBrowser? BrowserForProgId(string? progId)
	{
		if (string.IsNullOrWhiteSpace(progId))
		{
			return null;
		}
		return (from b in All
			where b.ProgIdPrefix != null
			orderby b.ProgIdPrefix.Length descending
			select b).FirstOrDefault((ChromiumBrowser b) => progId.StartsWith(b.ProgIdPrefix, StringComparison.OrdinalIgnoreCase));
	}

	public static ChromiumBrowser? BrowserForExecutable(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		ChromiumBrowser chromiumBrowser = All.FirstOrDefault(delegate(ChromiumBrowser b)
		{
			string text = ExecutablePath(b);
			return text != null && text.Length > 0 && string.Equals(text, path, StringComparison.OrdinalIgnoreCase);
		});
		if ((object)chromiumBrowser != null)
		{
			return chromiumBrowser;
		}
		string name = Path.GetFileName(path).ToLowerInvariant();
		ChromiumBrowser chromiumBrowser2 = All.FirstOrDefault((ChromiumBrowser b) => b.AppPathsName == name);
		if ((object)chromiumBrowser2 == null)
		{
			if (!(name == "arc.exe"))
			{
				return null;
			}
			chromiumBrowser2 = All.First((ChromiumBrowser b) => b.Name == "Arc");
		}
		return chromiumBrowser2;
	}

	public static string? ExecutablePathFor(string browserName)
	{
		ChromiumBrowser chromiumBrowser = All.FirstOrDefault((ChromiumBrowser b) => b.Name == browserName);
		if ((object)chromiumBrowser == null)
		{
			return null;
		}
		return ExecutablePath(chromiumBrowser);
	}

	public static string ExtensionsPageUrl(string browserName)
	{
		if (!(browserName == "Edge"))
		{
			if (browserName == "Brave")
			{
				return "brave://extensions/";
			}
			return "chrome://extensions/";
		}
		return "edge://extensions/";
	}

	private static string? DefaultBrowserProgId()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\https\\UserChoice");
			return registryKey?.GetValue("ProgId") as string;
		}
		catch
		{
			return null;
		}
	}

	private static double? LastActiveTime(ChromiumBrowser browser)
	{
		if (browser.PackageFamilyPrefix != null)
		{
			return null;
		}
		try
		{
			if (!(JsonNode.Parse(File.ReadAllText(Path.Combine(LocalAppData, browser.UserDataFolder, "Local State")), null) is JsonObject jsonObject) || !(jsonObject["profile"]?["info_cache"] is JsonObject jsonObject2))
			{
				return null;
			}
			double? num = null;
			foreach (KeyValuePair<string, JsonNode> item in jsonObject2)
			{
				item.Deconstruct(out var _, out var value);
				if (value?["active_time"] is JsonValue jsonValue && jsonValue.GetValueKind() == JsonValueKind.Number)
				{
					double value2 = jsonValue.GetValue<double>();
					if (!num.HasValue || value2 > num)
					{
						num = value2;
					}
				}
			}
			return num;
		}
		catch
		{
			return null;
		}
	}

	public static JsonObject Manifest(string helperPath, IReadOnlyList<string> extensionIds)
	{
		return new JsonObject((JsonNodeOptions?)null)
		{
			["name"] = "com.heyclicky.browser",
			["description"] = "HeyClicky browser connection",
			["path"] = helperPath,
			["type"] = "stdio",
			["allowed_origins"] = new JsonArray(((IEnumerable<string>)extensionIds).Select((Func<string, JsonNode>)((string id) => "chrome-extension://" + id + "/")).ToArray())
		};
	}

	public static void Register(string manifestPath, string helperPath, IReadOnlyList<string> extensionIds, RegistryKey? registryRoot = null)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
		string contents = Manifest(helperPath, extensionIds).ToJsonString(new JsonSerializerOptions
		{
			WriteIndented = true
		});
		string text = manifestPath + ".tmp";
		File.WriteAllText(text, contents);
		File.Move(text, manifestPath, overwrite: true);
		List<ChromiumBrowser> list = InstalledOrUsed().ToList();
		if (!list.Contains(All[0]))
		{
			list.Insert(0, All[0]);
		}
		using RegistryKey registryKey = ((registryRoot == null) ? Registry.CurrentUser.CreateSubKey("Software") : null);
		RegistryKey registryKey2 = registryRoot ?? registryKey;
		int num = 0;
		ChromiumBrowserRegistrationException ex = null;
		foreach (ChromiumBrowser item in list)
		{
			string registryPath = item.RegistryPath;
			if (registryPath == null)
			{
				continue;
			}
			string text2 = "HKCU\\Software\\" + registryPath + "\\NativeMessagingHosts\\com.heyclicky.browser";
			try
			{
				using RegistryKey registryKey3 = registryKey2.CreateSubKey(registryPath + "\\NativeMessagingHosts\\com.heyclicky.browser");
				registryKey3.SetValue(null, manifestPath, RegistryValueKind.String);
				num++;
			}
			catch (Exception ex2) when (((ex2 is UnauthorizedAccessException || ex2 is SecurityException || ex2 is IOException) ? 1 : 0) != 0)
			{
				Log.Warn("Browser", $"native host registration failed for {item.Name} at {text2}: {ex2.GetType().Name} {ex2.Message}");
				if (ex == null)
				{
					ex = new ChromiumBrowserRegistrationException(item.Name, text2, ex2);
				}
			}
		}
		if (num == 0 && ex != null)
		{
			throw ex;
		}
	}

	public static void Unregister(string manifestPath, RegistryKey? registryRoot = null)
	{
		using RegistryKey registryKey = ((registryRoot == null) ? Registry.CurrentUser.OpenSubKey("Software", writable: true) : null);
		RegistryKey registryKey2 = registryRoot ?? registryKey;
		if (registryKey2 != null)
		{
			foreach (ChromiumBrowser item in All)
			{
				string registryPath = item.RegistryPath;
				if (registryPath != null)
				{
					try
					{
						registryKey2.DeleteSubKeyTree(registryPath + "\\NativeMessagingHosts\\com.heyclicky.browser", throwOnMissingSubKey: false);
					}
					catch
					{
					}
				}
			}
		}
		try
		{
			File.Delete(manifestPath);
		}
		catch
		{
		}
	}

	public static bool IsRegistered(string manifestPath, RegistryKey? registryRoot = null)
	{
		try
		{
			using RegistryKey registryKey = ((registryRoot == null) ? Registry.CurrentUser.OpenSubKey("Software") : null);
			using RegistryKey registryKey2 = (registryRoot ?? registryKey)?.OpenSubKey(All[0].RegistryPath + "\\NativeMessagingHosts\\com.heyclicky.browser");
			return string.Equals(registryKey2?.GetValue(null) as string, manifestPath, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
}

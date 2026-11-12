using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HeyClicky.Core.Configuration;
using Microsoft.Win32;

namespace HeyClicky.Core.Identity;

public static class ClickyClientIdentity
{
	public const string PlatformHeader = "X-Clicky-Platform";

	public const string DeviceHeader = "X-Clicky-Device";

	public const string AppVersionHeader = "X-Clicky-App-Version";

	public const string AppBuildHeader = "X-Clicky-App-Build";

	public const string OsVersionHeader = "X-Clicky-OS-Version";

	public const string Platform = "windows";

	public static readonly IReadOnlyList<string> Capabilities = new global::_003C_003Ez__ReadOnlyArray<string>(new string[5] { "voice", "dictation", "screen_drawing", "local_agents", "notch" });

	private static ClientIdentity? current;

	public static ClientIdentity Current => current ?? (current = Create());

	public static ClientIdentity Create(Assembly? appAssembly = null)
	{
		if ((object)appAssembly == null)
		{
			appAssembly = Assembly.GetEntryAssembly() ?? typeof(ClickyClientIdentity).Assembly;
		}
		var (appVersion, appBuild) = ReadVersion(appAssembly);
		return new ClientIdentity("windows", DeviceKey(), appVersion, appBuild, OsVersion(), Capabilities);
	}

	public static string DeviceKey()
	{
		string text = ReadMachineGuid();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return HashDeviceKey(text);
		}
		string path = Path.Combine(AppBundleConfiguration.AppDataDirectory, "device-key");
		try
		{
			if (File.Exists(path))
			{
				string text2 = File.ReadAllText(path).Trim();
				if (text2.Length >= 8)
				{
					return text2;
				}
			}
			string text3 = Guid.NewGuid().ToString("N");
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, text3);
			return text3;
		}
		catch
		{
			return Guid.NewGuid().ToString("N");
		}
	}

	public static string HashDeviceKey(string hardwareId)
	{
		Span<byte> destination = stackalloc byte[32];
		SHA256.HashData(Encoding.UTF8.GetBytes("heyclicky-device:" + hardwareId), destination);
		return Convert.ToHexStringLower(destination.Slice(0, 16));
	}

	private static string? ReadMachineGuid()
	{
		try
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
			return registryKey2?.GetValue("MachineGuid") as string;
		}
		catch
		{
			return null;
		}
	}

	private static (string Version, string Build) ReadVersion(Assembly assembly)
	{
		string obj = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		string text = ((obj != null) ? obj.Split('+')[0] : null);
		Version version = assembly.GetName().Version;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (((object)version == null) ? "unknown" : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}");
		}
		string text2 = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
		Version result;
		string item = ((text2 != null && Version.TryParse(text2, out result) && result.Revision > 0) ? result.Revision.ToString() : (((object)version != null && version.Revision > 0) ? version.Revision.ToString() : "0"));
		return (Version: text, Build: item);
	}

	public static string OsVersion()
	{
		Version version = Environment.OSVersion.Version;
		return $"{version.Major}.{version.Minor}.{version.Build}";
	}
}

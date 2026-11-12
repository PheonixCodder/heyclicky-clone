using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeyClicky.App.Settings;

public static class CommandResolver
{
	public static string SearchPath()
	{
		List<string> entries = new List<string>();
		Add(Environment.GetEnvironmentVariable("PATH"));
		Add(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
		Add(Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		global::_003C_003Ey__InlineArray7<string> buffer = default(global::_003C_003Ey__InlineArray7<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 0) = Path.Combine(folderPath, "npm");
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 1) = Path.Combine(folderPath2, "Programs", "nodejs");
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 2) = "C:\\Program Files\\nodejs";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 3) = Path.Combine(folderPath3, ".local", "bin");
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 4) = Path.Combine(folderPath3, ".cargo", "bin");
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 5) = Path.Combine(folderPath3, ".bun", "bin");
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray7<string>, string>(ref buffer, 6) = Path.Combine(folderPath2, "Volta", "bin");
		Add(string.Join(';', global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray7<string>, string>(in buffer, 7)));
		return string.Join(';', entries);
		void Add(string? value)
		{
			string[] array = (value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (string text in array)
			{
				if (!entries.Contains<string>(text, StringComparer.OrdinalIgnoreCase))
				{
					entries.Add(text);
				}
			}
		}
	}

	public static string Resolve(string command)
	{
		string text = command.Trim();
		if (text.Length == 0)
		{
			return text;
		}
		if (text.StartsWith("~\\", StringComparison.Ordinal) || text.StartsWith("~/", StringComparison.Ordinal))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string text2 = text;
			text = Path.Combine(folderPath, text2.Substring(2, text2.Length - 2));
		}
		if (Path.IsPathFullyQualified(text))
		{
			string text3;
			if (!File.Exists(text))
			{
				text3 = WithExtension(text);
				if (text3 == null)
				{
					return text;
				}
			}
			else
			{
				text3 = text;
			}
			return text3;
		}
		if (text.Contains('\\') || text.Contains('/'))
		{
			return text;
		}
		string[] array = SearchPath().Split(';');
		foreach (string path in array)
		{
			try
			{
				string text4 = WithExtension(Path.Combine(path, text));
				if (text4 != null)
				{
					return text4;
				}
			}
			catch (ArgumentException)
			{
			}
		}
		return text;
	}

	private static string? WithExtension(string candidate)
	{
		if (Path.HasExtension(candidate) && File.Exists(candidate))
		{
			return candidate;
		}
		string[] array = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			if (File.Exists(candidate + text.ToLowerInvariant()))
			{
				return candidate + text.ToLowerInvariant();
			}
		}
		return null;
	}

	public static bool IsResolvable(string command)
	{
		string path = Resolve(command);
		if (Path.IsPathFullyQualified(path))
		{
			return File.Exists(path);
		}
		return false;
	}
}

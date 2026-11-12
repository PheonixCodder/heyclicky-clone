using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeyClicky;

public static class ShellOpenRules
{
	public readonly record struct TopWindow(nint Handle, int ProcessId, string ExeName, string Title, bool Minimized = false);

	public const string FolderKey = "Folder";

	public static bool IsWebUrl(string target)
	{
		if (Uri.TryCreate(target, UriKind.Absolute, out Uri result))
		{
			if (!(result.Scheme == Uri.UriSchemeHttps))
			{
				return result.Scheme == Uri.UriSchemeHttp;
			}
			return true;
		}
		return false;
	}

	public static bool LooksLikeUri(string target)
	{
		int num = target.IndexOf(':');
		if (num < 2)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			char c = target[i];
			bool flag = char.IsAsciiLetterOrDigit(c);
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '+':
				case '-':
				case '.':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (!flag)
			{
				return false;
			}
		}
		if (char.IsAsciiLetter(target[0]))
		{
			return !target.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static (string? Key, bool IsProtocol) AssociationKey(string target, bool isDirectory)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return (Key: null, IsProtocol: false);
		}
		if (isDirectory)
		{
			return (Key: "Folder", IsProtocol: false);
		}
		if (target.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(target, UriKind.Absolute, out Uri result) && result.IsFile)
		{
			return AssociationKey(result.LocalPath, isDirectory: false);
		}
		if (LooksLikeUri(target))
		{
			return (Key: target.Substring(0, target.IndexOf(':')).ToLowerInvariant(), IsProtocol: true);
		}
		string extension = Path.GetExtension(target.TrimEnd('\\', '/'));
		if (!string.IsNullOrEmpty(extension))
		{
			return (Key: extension.ToLowerInvariant(), IsProtocol: false);
		}
		return (Key: null, IsProtocol: false);
	}

	public static bool TakesUserDataDir(string exeName)
	{
		if (!exeName.Equals("chrome.exe", StringComparison.OrdinalIgnoreCase) && !exeName.Equals("msedge.exe", StringComparison.OrdinalIgnoreCase) && !exeName.Equals("brave.exe", StringComparison.OrdinalIgnoreCase))
		{
			return exeName.Equals("chromium.exe", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static TopWindow? PickOpenedWindow(IReadOnlyList<TopWindow> before, IReadOnlyList<TopWindow> now, string? handlerExeName, int ownProcessId, bool final)
	{
		Dictionary<nint, string> dictionary = new Dictionary<nint, string>();
		foreach (TopWindow item in before)
		{
			dictionary[item.Handle] = item.Title;
		}
		bool known = !string.IsNullOrEmpty(handlerExeName);
		List<TopWindow> list = now.Where((TopWindow w) => w.ProcessId != ownProcessId && (!known || string.Equals(w.ExeName, handlerExeName, StringComparison.OrdinalIgnoreCase))).ToList();
		foreach (TopWindow item2 in list)
		{
			if (!dictionary.ContainsKey(item2.Handle))
			{
				return item2;
			}
		}
		if (!known)
		{
			return null;
		}
		foreach (TopWindow item3 in list)
		{
			if (dictionary.TryGetValue(item3.Handle, out var value) && value != item3.Title)
			{
				return item3;
			}
		}
		if (final)
		{
			TopWindow value2 = list.FirstOrDefault((TopWindow w) => !w.Minimized);
			if (value2.Handle != 0)
			{
				return value2;
			}
		}
		return null;
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Harness;

public static class ClickyUserSkillFolders
{
	public static readonly string[] RootsRelativeToHome = new string[2]
	{
		Path.Combine(".agents", "skills"),
		Path.Combine(".claude", "skills")
	};

	public const int MaximumSkills = 40;

	public static Task<IReadOnlyList<string>> SkillDirectoriesAsync(string? homeDirectory = null)
	{
		return Task.Run(() => SkillDirectoriesNow(homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
	}

	public static IReadOnlyList<string> SkillDirectoriesNow(string homeDirectory)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrEmpty(homeDirectory))
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		string[] rootsRelativeToHome = RootsRelativeToHome;
		foreach (string path in rootsRelativeToHome)
		{
			string path2 = Path.Combine(homeDirectory, path);
			string[] source;
			try
			{
				source = (Directory.Exists(path2) ? Directory.GetFileSystemEntries(path2) : Array.Empty<string>());
			}
			catch (Exception)
			{
				continue;
			}
			foreach (string item in source.OrderBy<string, string>((string e) => Path.GetFileName(e), StringComparer.Ordinal))
			{
				if (Path.GetFileName(item).StartsWith('.'))
				{
					continue;
				}
				try
				{
					if (!File.Exists(Path.Combine(item, "SKILL.md")))
					{
						continue;
					}
				}
				catch (Exception)
				{
					continue;
				}
				if (hashSet.Add(RealPath(item)))
				{
					list.Add(item);
					if (list.Count == 40)
					{
						return list;
					}
				}
			}
		}
		return list;
	}

	private static string RealPath(string directory)
	{
		try
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(directory);
			return Path.TrimEndingDirectorySeparator(Path.GetFullPath(directoryInfo.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? directoryInfo.FullName));
		}
		catch (Exception)
		{
			return Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
		}
	}
}

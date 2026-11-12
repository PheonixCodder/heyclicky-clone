using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HeyClicky.Agents.Jobs;

public static class ClickyMainArtifacts
{
	private static readonly HashSet<string> ScratchExtensions = new HashSet<string>
	{
		"json", "log", "jsonl", "lock", "yaml", "yml", "toml", "js", "css", "map",
		"py", "sh", "swift", "ts", "ps1", "cs"
	};

	private static readonly HashSet<string> PageExtensions = new HashSet<string> { "html", "htm", "css", "md", "markdown" };

	private static readonly object CacheLock = new object();

	private static readonly Dictionary<string, (DateTime ModifiedAt, HashSet<string> References)> ReferencesCache = new Dictionary<string, (DateTime, HashSet<string>)>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> KeptInsideProjectsExtensions = new HashSet<string>
	{
		"pdf", "doc", "docx", "pages", "xls", "xlsx", "numbers", "csv", "ppt", "pptx",
		"key", "mp4", "mov", "m4v", "webm"
	};

	private static readonly HashSet<string> ProjectMarkerNames = new HashSet<string> { "package.swift", "cargo.toml", "go.mod", "build.gradle", "build.gradle.kts", "pubspec.yaml", "project.yml" };

	private static readonly HashSet<string> ProjectMarkerExtensions = new HashSet<string> { "xcodeproj", "xcworkspace", "sln", "csproj" };

	private static readonly HashSet<string> WeakProjectMarkerNames = new HashSet<string> { "package.json", "pyproject.toml", "gemfile" };

	private static readonly HashSet<string> SourceFolderNames = new HashSet<string> { "src", "app", "pages", "public", "lib", "components" };

	private static readonly Dictionary<string, (DateTimeOffset CheckedAt, bool IsProject)> ProjectFolderCache = new Dictionary<string, (DateTimeOffset, bool)>(StringComparer.OrdinalIgnoreCase);

	public static bool IsMeaningfulFile(string entry)
	{
		if (entry.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string text = entry.ToLowerInvariant().Replace('\\', '/');
		string fileName = Path.GetFileName(text);
		if (fileName.StartsWith('.') || text.Contains("/tmp/") || text.Contains("/node_modules/") || text.Contains("/dashboard/"))
		{
			return false;
		}
		string text2 = Path.GetExtension(fileName).TrimStart('.');
		if (ScratchExtensions.Contains(text2))
		{
			return false;
		}
		if ((text2 == "app" || text2 == "exe") ? true : false)
		{
			return true;
		}
		ClickyJobKind clickyJobKind = ClickyJobKinds.Infer(new _003C_003Ez__ReadOnlySingleElementList<string>(entry), fileName, "");
		if ((uint)(clickyJobKind - 7) <= 1u)
		{
			return false;
		}
		return true;
	}

	public static ISet<string> SupportingPaths(IEnumerable<string> paths)
	{
		HashSet<string> hashSet = paths.Where(Path.IsPathFullyQualified).Select(Standardized).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> supporting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
		foreach (string item in hashSet.Where(IsReferencingPage))
		{
			HashSet<string> hashSet2 = References(item);
			hashSet2.IntersectWith(hashSet);
			hashSet2.Remove(item);
			dictionary[item] = hashSet2;
		}
		foreach (var (text2, hashSet4) in dictionary)
		{
			foreach (string item2 in hashSet4)
			{
				if (!dictionary.TryGetValue(item2, out var value) || !value.Contains(text2) || Path.GetFileName(text2).Equals("index.html", StringComparison.OrdinalIgnoreCase))
				{
					supporting.Add(item2);
				}
			}
		}
		foreach (string item3 in hashSet.Where((string p) => !supporting.Contains(p)))
		{
			string text3 = Path.GetFileName(item3).ToLowerInvariant();
			string text4 = Path.GetExtension(item3).TrimStart('.').ToLowerInvariant();
			bool flag = ((text4 == "app" || text4 == "exe") ? true : false);
			if (!flag && !(text3 == "index.html") && !KeptInsideProjectsExtensions.Contains(text4) && ProjectRoot(item3) != null)
			{
				supporting.Add(item3);
			}
		}
		return supporting;
	}

	public static IReadOnlyList<string> MainPaths(IReadOnlyList<string> paths)
	{
		ISet<string> supporting = SupportingPaths(paths);
		if (supporting.Count != 0)
		{
			return paths.Where((string p) => !supporting.Contains(Standardized(p))).ToList();
		}
		return paths;
	}

	public static IReadOnlyList<string> MainEntries(IReadOnlyList<string> entries, Func<string, string> resolve)
	{
		List<string> list = entries.Select(resolve).ToList();
		ISet<string> supporting = SupportingPaths(list);
		if (supporting.Count == 0)
		{
			return entries;
		}
		return (from p in entries.Zip(list)
			where !supporting.Contains(Standardized(p.Second))
			select p.First).ToList();
	}

	private static string Standardized(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception)
		{
			return path;
		}
	}

	private static bool IsReferencingPage(string path)
	{
		return PageExtensions.Contains(Path.GetExtension(path).TrimStart('.').ToLowerInvariant());
	}

	public static HashSet<string> References(string path)
	{
		HashSet<string> hashSet = DirectReferences(path);
		foreach (string item in hashSet.ToList())
		{
			if (Path.GetExtension(item).Equals(".css", StringComparison.OrdinalIgnoreCase) && !item.Equals(path, StringComparison.OrdinalIgnoreCase))
			{
				hashSet.UnionWith(DirectReferences(item));
			}
		}
		return hashSet;
	}

	private static HashSet<string> DirectReferences(string path)
	{
		FileInfo fileInfo;
		try
		{
			fileInfo = new FileInfo(path);
			if (!fileInfo.Exists || fileInfo.Length > 2000000)
			{
				return new HashSet<string>();
			}
		}
		catch (Exception)
		{
			return new HashSet<string>();
		}
		HashSet<string> hashSet;
		lock (CacheLock)
		{
			if (ReferencesCache.TryGetValue(path, out (DateTime, HashSet<string>) value) && value.Item1 == fileInfo.LastWriteTimeUtc)
			{
				hashSet = new HashSet<string>();
				foreach (string item in value.Item2)
				{
					hashSet.Add(item);
				}
				return hashSet;
			}
		}
		string @string;
		try
		{
			byte[] bytes = File.ReadAllBytes(path);
			try
			{
				@string = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
			}
			catch (DecoderFallbackException)
			{
				@string = Encoding.Latin1.GetString(bytes);
			}
		}
		catch (Exception)
		{
			return new HashSet<string>();
		}
		HashSet<string> hashSet2 = ReferencedPaths(@string, Path.GetExtension(path).TrimStart('.').ToLowerInvariant(), Path.GetDirectoryName(path) ?? "");
		lock (CacheLock)
		{
			if (ReferencesCache.Count > 400)
			{
				ReferencesCache.Clear();
			}
			ReferencesCache[path] = (fileInfo.LastWriteTimeUtc, hashSet2);
		}
		hashSet = new HashSet<string>();
		foreach (string item2 in hashSet2)
		{
			hashSet.Add(item2);
		}
		return hashSet;
	}

	public static HashSet<string> ReferencedPaths(string text, string pageExtension, string pageDirectory)
	{
		List<(string Value, bool IsHref)> raw = new List<(string, bool)>();
		switch (pageExtension)
		{
		case "md":
		case "markdown":
			Collect("!\\[[^\\]]*\\]\\(\\s*<?([^)\\s>]+)", isHref: false);
			Collect("<img[^>]*\\ssrc\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')", isHref: false);
			break;
		case "css":
			Collect("url\\(\\s*(?:\"([^\"]*)\"|'([^']*)'|([^)\\s]+))\\s*\\)", isHref: false);
			Collect("@import\\s+(?:\"([^\"]*)\"|'([^']*)')", isHref: false);
			break;
		default:
			Collect("\\s(?:src|poster|data-src)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')", isHref: false);
			Collect("\\ssrcset\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')", isHref: false);
			Collect("\\shref\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)')", isHref: true);
			Collect("url\\(\\s*(?:\"([^\"]*)\"|'([^']*)'|([^)\\s]+))\\s*\\)", isHref: false);
			Collect("[\"'`]([^\"'`\\s<>()]+\\.(?:png|jpe?g|gif|webp|avif|svg|ico|mp4|webm|mov|m4v|mp3|wav|m4a|woff2?|ttf|otf|css|js|json))[\"'`]", isHref: false);
			break;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (text2, flag) in raw)
		{
			IEnumerable<string?> enumerable2;
			if (!text2.Contains(',') || flag)
			{
				IEnumerable<string> enumerable = new _003C_003Ez__ReadOnlySingleElementList<string>(text2);
				enumerable2 = enumerable;
			}
			else
			{
				enumerable2 = from v in text2.Split(',')
					select v.Trim().Split(' ').FirstOrDefault() ?? "";
			}
			foreach (string item in enumerable2)
			{
				string text3 = LocalPath(item, pageDirectory);
				if (text3 == null)
				{
					continue;
				}
				bool flag2 = flag;
				if (flag2)
				{
					bool flag3;
					switch (Path.GetExtension(text3).TrimStart('.').ToLowerInvariant())
					{
					case "html":
					case "htm":
					case "css":
						flag3 = true;
						break;
					default:
						flag3 = false;
						break;
					}
					flag2 = !flag3;
				}
				if (!flag2)
				{
					hashSet.Add(text3);
				}
			}
		}
		return hashSet;
		void Collect(string pattern, bool isHref)
		{
			foreach (Match item2 in Regex.Matches(text, pattern, RegexOptions.IgnoreCase))
			{
				for (int i = 1; i < item2.Groups.Count; i++)
				{
					if (item2.Groups[i].Success && item2.Groups[i].Length > 0)
					{
						raw.Add((item2.Groups[i].Value, isHref));
						break;
					}
				}
			}
		}
	}

	private static string? LocalPath(string reference, string directory)
	{
		string text = reference.Trim();
		string lowered = text.ToLowerInvariant();
		if (text.Length == 0 || text.StartsWith('#') || text.StartsWith("//", StringComparison.Ordinal))
		{
			return null;
		}
		if (new string[7] { "http:", "https:", "data:", "mailto:", "tel:", "javascript:", "blob:" }.Any((string p) => lowered.StartsWith(p, StringComparison.Ordinal)))
		{
			return null;
		}
		if (lowered.StartsWith("file://", StringComparison.Ordinal))
		{
			string text3;
			if (!Uri.TryCreate(text, UriKind.Absolute, out Uri result))
			{
				string text2 = text;
				text3 = text2.Substring(7, text2.Length - 7);
			}
			else
			{
				text3 = result.LocalPath;
			}
			text = text3;
		}
		int num = text.IndexOfAny(new char[2] { '?', '#' });
		if (num >= 0)
		{
			text = text.Substring(0, num);
		}
		try
		{
			text = Uri.UnescapeDataString(text);
		}
		catch (Exception)
		{
		}
		if (text.Length == 0)
		{
			return null;
		}
		try
		{
			return Path.GetFullPath(Path.IsPathFullyQualified(text) ? text : Path.Combine(directory, text.Replace('/', Path.DirectorySeparatorChar)));
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string? ProjectRoot(string path)
	{
		string directoryName = Path.GetDirectoryName(path);
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		int num = 0;
		while (num < 5)
		{
			if (!string.IsNullOrEmpty(directoryName) && !directoryName.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
			{
				string? pathRoot = Path.GetPathRoot(directoryName);
				if (pathRoot == null || !pathRoot.Equals(directoryName, StringComparison.OrdinalIgnoreCase))
				{
					if (IsProjectFolder(directoryName))
					{
						return directoryName;
					}
					directoryName = Path.GetDirectoryName(directoryName);
					num++;
					continue;
				}
			}
			return null;
		}
		return null;
	}

	private static bool IsProjectFolder(string directory)
	{
		lock (CacheLock)
		{
			if (ProjectFolderCache.TryGetValue(directory, out (DateTimeOffset, bool) value) && DateTimeOffset.Now - value.Item1 < TimeSpan.FromSeconds(60L))
			{
				return value.Item2;
			}
		}
		HashSet<string> hashSet;
		try
		{
			hashSet = (from e in Directory.EnumerateFileSystemEntries(directory)
				select Path.GetFileName(e).ToLowerInvariant()).ToHashSet();
		}
		catch (Exception)
		{
			hashSet = new HashSet<string>();
		}
		bool flag = hashSet.Overlaps(ProjectMarkerNames) || hashSet.Any((string n) => ProjectMarkerExtensions.Contains(Path.GetExtension(n).TrimStart('.'))) || (hashSet.Overlaps(WeakProjectMarkerNames) && hashSet.Overlaps(SourceFolderNames));
		lock (CacheLock)
		{
			if (ProjectFolderCache.Count > 400)
			{
				ProjectFolderCache.Clear();
			}
			ProjectFolderCache[directory] = (DateTimeOffset.Now, flag);
			return flag;
		}
	}
}

using System;
using System.IO;

namespace HeyClicky;

public static class AppPaths
{
	private static readonly string? overrideRoot = Clean(Environment.GetEnvironmentVariable("HEYCLICKY_DATA_DIR"));

	public static string Root { get; } = overrideRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HeyClicky");

	public static string DocumentsRoot { get; }

	public static bool IsOverridden => overrideRoot != null;

	public static string Combine(params string[] parts)
	{
		string root = Root;
		int num = 0;
		string[] array = new string[1 + parts.Length];
		array[num] = root;
		num++;
		ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(parts);
		readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		return Path.Combine(new ReadOnlySpan<string>(array));
	}

	private static string? Clean(string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return Path.GetFullPath(value.Trim());
		}
		return null;
	}

	static AppPaths()
	{
		string text = overrideRoot;
		DocumentsRoot = ((text != null) ? Path.Combine(text, "Documents") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "HeyClicky"));
	}
}

using System;

namespace HeyClicky.Agents.Runtime;

internal static class WindowsInstructionAdapter
{
	private static readonly (string Mac, string Windows)[] Replacements = new(string, string)[4]
	{
		("You're a helper HeyClicky started on this person's Mac", "You're a helper HeyClicky started on this person's Windows PC"),
		("- To find a file of theirs, `mdfind -name invoice` (or `mdfind 'kMDItemTextContent == \"*lease*\"cd'`) searches the whole Mac without opening folders, and `rg` searches inside a folder you know. Don't walk Desktop, Documents or Downloads hunting for something (each asks macOS for permission the first time); when the ask is about one of those folders itself (tidy my Downloads, what's on my Desktop), work in it.", "- To find a file of theirs in a folder you know, `Get-ChildItem <folder> -Recurse -Filter '*invoice*'` finds it by name and `Select-String` searches inside files. Don't walk Desktop, Documents or Downloads hunting for something (they're private); ask where it is instead. When the ask is about one of those folders itself (tidy my Downloads, what's on my Desktop), work in it."),
		("- Never script clicks or keystrokes in their apps (System Events, `cliclick`) or drive them with `open`; an app's own scripting commands (AppleScript, Shortcuts) are fine.", "- Never script clicks or keystrokes in their apps (SendKeys, UI Automation) or drive them with `Start-Process`; an app's own command line or scripting interface is fine."),
		("(Python: `uv pip install --target tmp/pylib <package>`, then run with `PYTHONPATH=tmp/pylib`)", "(Python: `uv pip install --target tmp\\pylib <package>`, then run with `$env:PYTHONPATH = \"tmp\\pylib\"`)")
	};

	private const string ShellNote = "\nThis PC runs Windows: your shell is PowerShell, paths use backslashes (C:\\Users\\...), and `~` is $env:USERPROFILE.\n";

	public static string Adapt(string macBrief)
	{
		string text = macBrief.Replace("\r\n", "\n");
		(string, string)[] replacements = Replacements;
		for (int i = 0; i < replacements.Length; i++)
		{
			(string, string) tuple = replacements[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			text = text.Replace(item, item2, StringComparison.Ordinal);
		}
		text = text.Replace("their Mac", "their PC", StringComparison.Ordinal).Replace("this Mac", "this PC", StringComparison.Ordinal);
		int num = text.IndexOf("\n\n", StringComparison.Ordinal);
		if (num >= 0)
		{
			return text.Insert(num, "\n" + "\nThis PC runs Windows: your shell is PowerShell, paths use backslashes (C:\\Users\\...), and `~` is $env:USERPROFILE.\n".TrimEnd('\n'));
		}
		return text + "\nThis PC runs Windows: your shell is PowerShell, paths use backslashes (C:\\Users\\...), and `~` is $env:USERPROFILE.\n";
	}

	public static string AdaptPromptText(string text)
	{
		return text.Replace("while the Mac is awake", "while the PC is awake", StringComparison.Ordinal).Replace("while the Mac is asleep or off", "while the PC is asleep or off", StringComparison.Ordinal).Replace("the Mac", "the PC", StringComparison.Ordinal);
	}
}

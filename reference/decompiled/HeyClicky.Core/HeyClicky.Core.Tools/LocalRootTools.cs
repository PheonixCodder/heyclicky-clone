using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Documents;

namespace HeyClicky.Core.Tools;

public sealed class LocalRootTools
{
	public sealed record ShellOutcome(int ExitCode, string Output, bool TimedOut);

	public static readonly IReadOnlySet<string> Names = new HashSet<string> { "shell_exec", "file_read", "file_write", "file_list" };

	public const int DefaultTimeoutMs = 10000;

	public const int OutputLimit = 4000;

	private const int ListLimit = 100;

	private const long ReadLimitBytes = 4194304L;

	private readonly Func<string> workspaceRoot;

	private readonly string shell;

	public LocalRootTools(Func<string> workspaceRoot, string? shell = null)
	{
		this.workspaceRoot = workspaceRoot;
		this.shell = shell ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
	}

	public Task<string> RunAsync(string toolName, ToolArgs args, CancellationToken ct = default(CancellationToken))
	{
		switch (toolName)
		{
		case "shell_exec":
			return ShellExecAsync(args, ct);
		case "file_read":
		case "file_write":
		case "file_list":
			return Task.Run(() => FileTool(toolName, args), ct);
		default:
			return Task.FromResult(ToolResults.Summary("error", toolName, "not_implemented"));
		}
	}

	private async Task<string> ShellExecAsync(ToolArgs args, CancellationToken ct)
	{
		string command = args.Exact("command");
		if (string.IsNullOrWhiteSpace(command))
		{
			return ToolResults.Summary("blocked", "shell_exec", "shell_exec needs a command.");
		}
		int timeoutMs = Math.Max(100, args.Int("timeoutMs") ?? 10000);
		string text = workspaceRoot();
		string text2 = args.Str("cwd");
		string cwd = ((text2 != null) ? ResolvePath(text2, text) : text);
		Log.Info("Tools", $"shell_exec cwd={cwd} timeoutMs={timeoutMs} command={Truncate1(command, 300)}");
		ShellOutcome shellOutcome = await RunPowerShellAsync(command, cwd, timeoutMs, ct).ConfigureAwait(continueOnCapturedContext: false);
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["command"] = command,
			["cwd"] = cwd,
			["timeoutMs"] = timeoutMs,
			["shell"] = "powershell"
		};
		if (shellOutcome.TimedOut)
		{
			jsonObject["timedOut"] = true;
			Log.Info("Tools", "shell_exec timed out");
			string text3 = ((shellOutcome.Output.Length > 0) ? (" Output before the deadline: " + ToolResults.Truncate(shellOutcome.Output, 4000)) : "");
			return ToolResults.Summary("failed", "shell_exec", "Command timed out." + text3, jsonObject);
		}
		jsonObject["exitCode"] = shellOutcome.ExitCode;
		Log.Info("Tools", $"shell_exec exit {shellOutcome.ExitCode}: {Truncate1(shellOutcome.Output, 400)}");
		return ToolResults.Summary((shellOutcome.ExitCode == 0) ? "completed" : "failed", "shell_exec", ToolResults.Truncate((shellOutcome.Output.Length == 0) ? "Command completed." : shellOutcome.Output, 4000), jsonObject);
	}

	public async Task<ShellOutcome> RunPowerShellAsync(string command, string cwd, int timeoutMs, CancellationToken ct = default(CancellationToken))
	{
		if (!Directory.Exists(cwd))
		{
			return new ShellOutcome(-1, "The folder " + cwd + " doesn't exist.", TimedOut: false);
		}
		string s2 = "$ProgressPreference='SilentlyContinue'; [Console]::OutputEncoding=[System.Text.Encoding]::UTF8; $OutputEncoding=[System.Text.Encoding]::UTF8\n" + command;
		string item = Convert.ToBase64String(Encoding.Unicode.GetBytes(s2));
		ProcessStartInfo startInfo = new ProcessStartInfo(shell)
		{
			ArgumentList = { "-NoLogo", "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", item },
			WorkingDirectory = cwd,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};
		StringBuilder stdout = new StringBuilder();
		StringBuilder stderr = new StringBuilder();
		using Process process = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};
		process.OutputDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				lock (stdout)
				{
					if (stdout.Length < 16000)
					{
						stdout.Append(e.Data).Append('\n');
					}
				}
			}
		};
		process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				lock (stderr)
				{
					if (stderr.Length < 16000)
					{
						stderr.Append(e.Data).Append('\n');
					}
				}
			}
		};
		try
		{
			process.Start();
		}
		catch (Exception ex)
		{
			return new ShellOutcome(-1, ex.Message, TimedOut: false);
		}
		process.StandardInput.Close();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
		deadline.CancelAfter(timeoutMs);
		bool timedOut = false;
		try
		{
			await process.WaitForExitAsync(deadline.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			timedOut = true;
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch
			{
			}
		}
		try
		{
			await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2L)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
		}
		string output;
		lock (stdout)
		{
			lock (stderr)
			{
				output = string.Join("\n", new string[2]
				{
					stdout.ToString().Trim(),
					stderr.ToString().Trim()
				}.Where((string s) => s.Length > 0));
			}
		}
		return new ShellOutcome(timedOut ? (-1) : SafeExitCode(process), output, timedOut);
	}

	private static int SafeExitCode(Process process)
	{
		try
		{
			return process.ExitCode;
		}
		catch
		{
			return -1;
		}
	}

	private string FileTool(string toolName, ToolArgs args)
	{
		string text = args.Str("path");
		if (text == null)
		{
			return ToolResults.Summary("blocked", toolName, toolName + " needs a path.");
		}
		string text2 = ((toolName == "file_write") ? args.Exact("content") : null);
		if (toolName == "file_write" && text2 == null)
		{
			return ToolResults.Summary("blocked", toolName, "file_write needs content.");
		}
		string text3 = ResolvePath(text, workspaceRoot());
		try
		{
			if (!(toolName == "file_read"))
			{
				if (toolName == "file_list")
				{
					IEnumerable<string> values = (from entry in new DirectoryInfo(text3).EnumerateFileSystemInfos()
						select (!(entry is DirectoryInfo)) ? entry.Name : (entry.Name + "\\")).Order<string>(StringComparer.OrdinalIgnoreCase).Take(100);
					return ToolResults.Summary("completed", toolName, "Listed " + text3 + ": " + string.Join(", ", values));
				}
				Directory.CreateDirectory(Path.GetDirectoryName(text3));
				string text4 = text3 + ".clicky-write";
				File.WriteAllText(text4, text2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				File.Move(text4, text3, overwrite: true);
				return ToolResults.Summary("completed", toolName, "Wrote " + text3 + ".");
			}
			string text5 = ReadText(text3);
			return ToolResults.Summary("completed", toolName, "Read " + text3 + ": " + ToolResults.Truncate(text5, 4000));
		}
		catch (Exception ex)
		{
			return ToolResults.Summary("failed", toolName, ex.Message);
		}
	}

	private static string ReadText(string path)
	{
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		byte[] array = new byte[(int)Math.Min(4194304L, fileStream.Length)];
		fileStream.ReadExactly(array);
		return DocumentText.Decode(array) ?? throw new InvalidDataException("This file isn't text.");
	}

	public static string ResolvePath(string rawPath, string workspaceRoot)
	{
		string text = Environment.ExpandEnvironmentVariables(rawPath.Trim().Trim('"'));
		if (text == "~" || text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("~\\", StringComparison.Ordinal))
		{
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string text2 = text;
			text = folderPath + text2.Substring(1, text2.Length - 1);
		}
		text = text.Replace('/', '\\');
		return Path.GetFullPath(Path.IsPathFullyQualified(text) ? text : Path.Combine(workspaceRoot, text));
	}

	private static string Truncate1(string text, int limit)
	{
		return ToolResults.Truncate(text.Replace("\r", "\\r").Replace("\n", "\\n"), limit);
	}
}

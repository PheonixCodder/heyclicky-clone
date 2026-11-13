using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Agents.Runtime;

namespace HeyClicky.Agents.Harness;

public static class AgentHarnessRunnerProcess
{
	public sealed record Endpoint(int Port, string Token);

	private abstract record Launch
	{
		public sealed record Bundled(string Binary) : Launch();

		public sealed record Node(string NodePath, string Script) : Launch();
	}

	private static readonly SemaphoreSlim StartGate = new SemaphoreSlim(1, 1);

	private static Process? process;

	private static Endpoint? endpoint;

	private const string RunnerPathKey = "ClickyAgentHarnessRunnerPath";

	private const string NodePathKey = "ClickyAgentHarnessNodePath";

	public const string UsesBundledRunnerKey = "ClickyAgentHarnessUsesBundledRunner";

	public static string StateDirectory { get; set; } = AppPaths.Combine("agents", "AgentHarness");

	public static string BundledRunnerDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "HarnessRuntime");

	public static bool IsRunning
	{
		get
		{
			if ((object)endpoint != null)
			{
				Process process = AgentHarnessRunnerProcess.process;
				if (process != null)
				{
					return !process.HasExited;
				}
				return false;
			}
			return false;
		}
	}

	public static string RunnerScriptPath
	{
		get
		{
			string text = AgentDefaults.String("ClickyAgentHarnessRunnerPath");
			if (text != null && text.Length > 0)
			{
				return text;
			}
			string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_AGENT_HARNESS_RUNNER");
			if (environmentVariable != null && environmentVariable.Length > 0)
			{
				return environmentVariable;
			}
			return Path.Combine(RepoRoot(), "agent-harness", "runner.mjs");
		}
	}

	public static string BundledRunnerPath => Path.Combine(BundledRunnerDirectory, "clicky-harness.exe");

	public static event Action<string, string, string, string>? InternalError;

	private static void ReportInternalError(string stage, string detail, string key)
	{
		try
		{
			AgentHarnessRunnerProcess.InternalError?.Invoke("agent_harness", stage, (detail.Length > 500) ? detail.Substring(0, 500) : detail, key);
		}
		catch (Exception ex)
		{
			Log.Warn("Harness", "InternalError handler threw: " + ex.Message);
		}
	}

	public static async Task<Endpoint> EnsureRunningAsync(CancellationToken ct = default(CancellationToken))
	{
		Endpoint endpoint = AgentHarnessRunnerProcess.endpoint;
		if ((object)endpoint != null)
		{
			Process process = AgentHarnessRunnerProcess.process;
			if (process != null && !process.HasExited)
			{
				return endpoint;
			}
		}
		await StartGate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			Endpoint endpoint2 = AgentHarnessRunnerProcess.endpoint;
			if ((object)endpoint2 != null)
			{
				Process process = AgentHarnessRunnerProcess.process;
				if (process != null && !process.HasExited)
				{
					return endpoint2;
				}
			}
			AgentHarnessRunnerProcess.endpoint = null;
			try
			{
				return AgentHarnessRunnerProcess.endpoint = await StartAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (!(ex is OperationCanceledException) || !ct.IsCancellationRequested)
			{
				Log.Warn("Harness", "[harness runner] couldn't start: " + ex.Message);
				ReportInternalError("runner_start_failed", ex.Message, "runner_start");
				throw new AgentHarnessException("HeyClicky's agent runner couldn't start, so this job didn't run. " + ex.Message, notAvailable: true, isRunnerStartFailure: true);
			}
		}
		finally
		{
			StartGate.Release();
		}
	}

	public static void Stop()
	{
		Process process = AgentHarnessRunnerProcess.process;
		AgentHarnessRunnerProcess.process = null;
		endpoint = null;
		if (process == null)
		{
			return;
		}
		try
		{
			process.StandardInput.Close();
			if (!process.WaitForExit(1500))
			{
				process.Kill(entireProcessTree: true);
			}
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is Win32Exception || ex is IOException) ? 1 : 0) != 0)
		{
		}
		finally
		{
			process.Dispose();
		}
	}

	private static string RepoRoot([CallerFilePath] string sourceFile = "")
	{
		_003C_003Ey__InlineArray5<string> buffer = default(_003C_003Ey__InlineArray5<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 0) = Path.GetDirectoryName(sourceFile) ?? ".";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 1) = "..";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 2) = "..";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 3) = "..";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 4) = "..";
		return Path.GetFullPath(Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray5<string>, string>(in buffer, 5)));
	}

	public static string? NodeExecutablePath()
	{
		string text = AgentDefaults.String("ClickyAgentHarnessNodePath");
		if (text != null && text.Length > 0 && File.Exists(text))
		{
			return text;
		}
		List<string> list = new List<string>();
		string[] array = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			list.Add(Path.Combine(text2.Trim(), "node.exe"));
		}
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		list.Add(Path.Combine(folderPath, "nodejs", "node.exe"));
		list.AddRange(NewestVersionedNodes(Path.Combine(folderPath2, "nvm")));
		list.AddRange(NewestVersionedNodes(Path.Combine(folderPath3, "fnm", "node-versions"), Path.Combine("installation", "node.exe")));
		list.Add(Path.Combine(folderPath3, "Volta", "bin", "node.exe"));
		foreach (string item in list.Distinct<string>(StringComparer.OrdinalIgnoreCase))
		{
			if (File.Exists(item) && NodeMajorVersion(item) >= 22)
			{
				return item;
			}
		}
		return null;
	}

	private static IEnumerable<string> NewestVersionedNodes(string directory, string binary = "node.exe")
	{
		if (!Directory.Exists(directory))
		{
			return Array.Empty<string>();
		}
		Version result;
		return from c in (from d in Directory.GetDirectories(directory)
				select (Path: Path.Combine(d, binary), Version: Version.TryParse(Path.GetFileName(d).TrimStart('v'), out result) ? result : null)).Where(delegate((string Path, Version? Version) c)
			{
				Version? item = c.Version;
				return (object?)item != null && item.Major >= 22;
			})
			orderby c.Version descending
			select c.Path;
	}

	private static int NodeMajorVersion(string node)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo(node, "--version")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			});
			if (process == null)
			{
				return 0;
			}
			string text = process.StandardOutput.ReadToEnd().Trim();
			if (!process.WaitForExit(5000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return 0;
			}
			int result;
			return int.TryParse(text.TrimStart('v').Split('.')[0], out result) ? result : 0;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	private static Launch ResolveLaunch()
	{
		string bundledRunnerPath = BundledRunnerPath;
		if (!File.Exists(bundledRunnerPath))
		{
			throw new AgentHarnessException("This build has no agent harness runner (HarnessRuntime\\clicky-harness.exe).", notAvailable: true);
		}
		return new Launch.Bundled(bundledRunnerPath);
	}

	private static async Task<Endpoint> StartAsync(CancellationToken ct)
	{
		Launch launch = ResolveLaunch();
		Directory.CreateDirectory(StateDirectory);
		string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
		ProcessStartInfo processStartInfo;
		if (!(launch is Launch.Bundled bundled))
		{
			if (!(launch is Launch.Node node))
			{
				throw new InvalidOperationException();
			}
			processStartInfo = new ProcessStartInfo(node.NodePath)
			{
				ArgumentList = { node.Script },
				WorkingDirectory = Path.GetDirectoryName(node.Script)
			};
		}
		else
		{
			processStartInfo = new ProcessStartInfo(bundled.Binary)
			{
				WorkingDirectory = StateDirectory
			};
		}
		ProcessStartInfo processStartInfo2 = processStartInfo;
		processStartInfo2.UseShellExecute = false;
		processStartInfo2.RedirectStandardInput = true;
		processStartInfo2.RedirectStandardOutput = true;
		processStartInfo2.RedirectStandardError = true;
		processStartInfo2.CreateNoWindow = true;
		processStartInfo2.StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		processStartInfo2.StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
		if (launch is Launch.Node node2)
		{
			string value;
			string text = (processStartInfo2.Environment.TryGetValue("PATH", out value) ? (value ?? "") : "");
			processStartInfo2.Environment["PATH"] = string.Join(';', new string[2]
			{
				Path.GetDirectoryName(node2.NodePath),
				text
			}.Where((string s) => s.Length > 0));
		}
		processStartInfo2.Environment["CLICKY_HARNESS_TOKEN"] = token;
		processStartInfo2.Environment["CLICKY_HARNESS_STATE"] = StateDirectory;
		processStartInfo2.Environment["CLICKY_H1_STATE_DIR"] = Path.Combine(StateDirectory, "h1-local");
		Process started = new Process
		{
			StartInfo = processStartInfo2,
			EnableRaisingEvents = true
		};
		if (!started.Start())
		{
			throw new AgentHarnessException("The agent harness runner didn't start.");
		}
		try
		{
			KillOnCloseJob.Shared?.Assign(started);
		}
		catch (Exception ex)
		{
			Log.Warn("Harness", "could not assign the runner to the job object: " + ex.Message);
		}
		started.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(e.Data))
			{
				Log.Info("Harness", "[harness runner] " + ((e.Data.Length > 600) ? e.Data.Substring(0, 600) : e.Data));
			}
		};
		started.BeginErrorReadLine();
		started.Exited += delegate
		{
			int value2;
			try
			{
				value2 = started.ExitCode;
			}
			catch
			{
				value2 = -1;
			}
			Log.Info("Harness", $"[harness runner] exited ({value2})");
			if (process == started)
			{
				process = null;
				endpoint = null;
			}
		};
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(TimeSpan.FromSeconds(20L));
		string text2;
		try
		{
			text2 = await started.StandardOutput.ReadLineAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException) when (!ct.IsCancellationRequested)
		{
			try
			{
				started.Kill(entireProcessTree: true);
			}
			catch
			{
			}
			throw new AgentHarnessException("The agent harness runner didn't start within 20 s.");
		}
		int value3 = 0;
		try
		{
			if (text2 != null && JsonNode.Parse(text2, null) is JsonObject jsonObject && jsonObject["port"] is JsonValue jsonValue)
			{
				jsonValue.TryGetValue<int>(out value3);
			}
		}
		catch (JsonException)
		{
		}
		if (value3 <= 0)
		{
			try
			{
				started.Kill(entireProcessTree: true);
			}
			catch
			{
			}
			throw new AgentHarnessException("The agent harness runner said: " + (text2 ?? "(nothing)").Substring(0, Math.Min(300, (text2 ?? "(nothing)").Length)));
		}
		Task.Run(async delegate
		{
			try
			{
				while (await started.StandardOutput.ReadLineAsync().ConfigureAwait(continueOnCapturedContext: false) != null)
				{
				}
			}
			catch (Exception)
			{
			}
		});
		process = started;
		string message = ((launch is Launch.Bundled bundled2) ? $"[harness runner] up on 127.0.0.1:{value3} ({bundled2.Binary})" : ((!(launch is Launch.Node node3)) ? "" : $"[harness runner] up on 127.0.0.1:{value3} ({node3.NodePath} {Path.GetFileName(node3.Script)})"));
		Log.Info("Harness", message);
		return new Endpoint(value3, token);
	}
}

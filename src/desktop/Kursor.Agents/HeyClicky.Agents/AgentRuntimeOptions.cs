using System;
using System.IO;

namespace HeyClicky.Agents;

public sealed class AgentRuntimeOptions
{
	public string CodexHome { get; set; } = AppPaths.Combine("codex-home");

	public string ProjectsRoot { get; set; } = AppPaths.DocumentsRoot;

	public string StateDirectory { get; set; } = AppPaths.Combine("agents");

	public string HarnessRuntimeDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "HarnessRuntime");

	public string BundledResourcesDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "BundledResources");

	public string WorkerBaseUrl { get; set; } = EnvOr("HEYCLICKY_WORKER_BASE_URL", "http://127.0.0.1:8787");

	public string Model { get; set; } = EnvOr("CLICKY_LOCAL_CODEX_MODEL", "gpt-6-luna");

	public string ReasoningEffort { get; set; } = ValidEffort(Environment.GetEnvironmentVariable("CLICKY_LOCAL_CODEX_REASONING_EFFORT")) ?? "high";

	public int MaxConcurrentTasks { get; set; } = 5;

	public TimeSpan SessionTokenRefreshMargin { get; set; } = TimeSpan.FromMinutes(30L);

	public bool AutoApproveExtraUsage { get; set; }

	public string? ObsidianVaultPath { get; set; }

	public bool EnableRoutines { get; set; } = true;

	private static string EnvOr(string name, string fallback)
	{
		string text = Environment.GetEnvironmentVariable(name)?.Trim();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return fallback;
	}

	private static string? ValidEffort(string? v)
	{
		switch (v?.Trim())
		{
		case "low":
		case "medium":
		case "high":
		case "xhigh":
			return v.Trim();
		default:
			return null;
		}
	}
}

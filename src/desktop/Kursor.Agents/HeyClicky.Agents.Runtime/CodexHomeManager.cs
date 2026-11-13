using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HeyClicky.Agents.Runtime;

internal sealed class CodexHomeManager(AgentRuntimeOptions options)
{
	public static readonly string[] BundledSkillNames = new string[2] { "clicky-browser", "vercel-deploy" };

	private static readonly string[] HiddenBundledSkillNames = new string[1] { "nano-banana-slides" };

	public const string DefaultWorkerOpenAiBaseUrl = "http://127.0.0.1:8787/agent/openai/v1";

	public const string DefaultApprovalPolicy = "on-request";

	public const string DefaultSandboxMode = "workspace-write";

	private static readonly string[] BlockedSystemSkillNames = new string[6] { "imagegen", "openai-docs", "plugin-creator", "review-agent", "skill-creator", "skill-installer" };

	private readonly object gate = new object();

	private static readonly string MultipleAccountsRules;

	public string? DeliveredModelInstructions { get; private set; }

	public string CodexHome => options.CodexHome;

	private string SkillsDirectory => Path.Combine(CodexHome, "skills");

	private string BundledInstructionsPath => Path.Combine(options.BundledResourcesDirectory, "clicky-model-instructions.md");

	private string BundledSkillsDirectory => Path.Combine(options.BundledResourcesDirectory, "skills");

	private string IntegrationSkillsDirectory => Path.Combine(options.BundledResourcesDirectory, "integration-skills");

	public string TaskAssetsDirectory(Guid taskId)
	{
		string text = Path.Combine(CodexHome, "task-assets", taskId.ToString("D"));
		Directory.CreateDirectory(text);
		return text;
	}

	public void PrepareRuntimeConfiguration(ComposioMcpConfiguration? composio, bool areConnectedAppsTemporarilyUnreachable, IReadOnlyList<CustomConnectorRegistration> customConnectors)
	{
		lock (gate)
		{
			Directory.CreateDirectory(CodexHome);
			Directory.CreateDirectory(SkillsDirectory);
			Directory.CreateDirectory(options.ProjectsRoot);
			Dictionary<string, string> dictionary = SynchronizeSkills(composio?.Toolkits ?? Array.Empty<string>());
			string configToml = BuildCodexConfigToml();
			WriteIfChanged(Path.Combine(CodexHome, "config.toml"), configToml);
			string text = BuildModelInstructions(composio, areConnectedAppsTemporarilyUnreachable, customConnectors);
			bool num = text != null && WriteIfChanged(Path.Combine(CodexHome, "clicky-model-instructions.md"), text);
			DeliveredModelInstructions = text;
			if (num)
			{
				Log.Info("Agents", $"agent brief written: {text.Length} chars, skills=[{string.Join(',', dictionary.Keys)}]");
			}
		}
	}

	private string BuildCodexConfigToml()
	{
		string baseUrl = string.IsNullOrWhiteSpace(options.WorkerBaseUrl)
			? DefaultWorkerOpenAiBaseUrl
			: (options.WorkerBaseUrl.TrimEnd('/') + "/agent/openai/v1");
		return string.Join("\n", new string[6]
		{
			"model_provider = \"clicky\"",
			"approval_policy = \"on-request\"",
			"sandbox_mode = \"workspace-write\"",
			"",
			"[model_providers.clicky]",
			$"base_url = \"{baseUrl}\""
		}) + "\n";
	}

	private string? BuildModelInstructions(ComposioMcpConfiguration? composio, bool appsUnreachable, IReadOnlyList<CustomConnectorRegistration> customConnectors)
	{
		if (!File.Exists(BundledInstructionsPath))
		{
			return null;
		}
		return WindowsInstructionAdapter.Adapt(File.ReadAllText(BundledInstructionsPath, Encoding.UTF8)) + "\n" + ExternalIntegrationsInstructions(composio, appsUnreachable, customConnectors) + "\n";
	}

	private string ExternalIntegrationsInstructions(ComposioMcpConfiguration? composio, bool appsUnreachable, IReadOnlyList<CustomConnectorRegistration> customConnectors)
	{
		List<string> list = new List<string> { "## Connected apps" };
		if ((object)composio != null)
		{
			string text = ((composio.Toolkits.Count == 0) ? "connected toolkit list unavailable" : string.Join(", ", composio.Toolkits.OrderBy<string, string>((string t) => t, StringComparer.Ordinal)));
			list.Add("- When this chat started, these apps were connected through the `composio` MCP server: " + text + ". Its tools are the live source of truth, since the list can change later: search them before deciding an app can't do something. Google apps connect separately (gmail, googlesheets, googlecalendar, googledrive, googledocs), so name the specific one when it's missing.");
			list.Add("- If an app the task needs isn't connected, or a call comes back expired or unauthorized, name the app and tell them to connect or reconnect it in HeyClicky Settings -> Integrations. Never run OAuth, sign-in or an auth CLI yourself, and never act on HeyClicky's own settings screen.");
			list.Add("- If Gmail refuses to send, ask them to reconnect Gmail with send permission in HeyClicky Settings -> Integrations.");
			if (composio.MultiAccountLines.Count > 0)
			{
				list.Add("- Apps with more than one connected account:\n" + string.Join("\n", composio.MultiAccountLines.Select((string l) => "  - " + l)) + "\n" + string.Join("\n", from l in MultipleAccountsRules.Split('\n')
					select "  - " + l));
			}
		}
		else if (appsUnreachable)
		{
			list.Add("- Their connected apps couldn't be reached when this chat started (a temporary connection problem; they ARE connected). If `composio` tools are available now, use them. Otherwise, when a task needs their email, calendar, docs or other app data, say the apps couldn't be reached just now and to try again in a moment. Never tell them to connect or reconnect apps for this, and never run OAuth, sign-in or an auth CLI yourself.");
		}
		else
		{
			list.Add("- No apps were connected when this chat started. If `composio` tools are available now, use them for their email, calendar, docs and other app data; otherwise tell them to connect the app in HeyClicky Settings -> Integrations. Never run OAuth, sign-in or an auth CLI yourself.");
		}
		string text2 = options.ObsidianVaultPath?.Trim();
		list.Add((!string.IsNullOrEmpty(text2)) ? ("- Obsidian is their local vault at " + text2 + " (also `$env:OBSIDIAN_VAULT_PATH`): plain Markdown files you read and edit with the shell, keeping frontmatter, [[wikilinks]] and tags intact. Ask before deleting, bulk-moving or renaming notes, or changing its `.obsidian` settings. It is not a Composio app, and it is the only vault root; don't look for notes elsewhere.") : "- Obsidian isn't set up. For Obsidian work, tell them to configure their vault in HeyClicky Settings -> Integrations -> Obsidian.");
		if (customConnectors.Count > 0)
		{
			IEnumerable<string> values = customConnectors.Select(delegate(CustomConnectorRegistration c)
			{
				string text3 = c.CapabilityDescription.Trim();
				return $"  - {c.DisplayName}: MCP server `{c.ServerKey}` (its tools are prefixed with that name). " + ((text3.Length == 0) ? "the user did not describe it; discover what it does from its tool list" : text3);
			});
			list.Add("- Their own MCP connectors (Settings -> Integrations -> Custom connectors), one server each. Use them directly for tasks in their domain under the same approval rule, and also confirm first before sending messages to other people or changing ad spend:\n" + string.Join("\n", values) + "\n- Their credentials are already configured: never ask for or print a token. If a connector's tools are missing or a call comes back unauthorized, say so and point them to its row in Settings -> Integrations -> Custom connectors (\"Sign in\", or the token under Edit). Don't go looking for it in other tools' configuration or call it over HTTP yourself.");
		}
		return string.Join("\n", list);
	}

	private Dictionary<string, string> SynchronizeSkills(IReadOnlyList<string> connectedToolkits)
	{
		HashSet<string> hashSet = (Directory.Exists(IntegrationSkillsDirectory) ? (from n in Directory.GetDirectories(IntegrationSkillsDirectory).Select(Path.GetFileName)
			where n != null && File.Exists(Path.Combine(IntegrationSkillsDirectory, n, "SKILL.md"))
			select (n)).ToHashSet<string>(StringComparer.Ordinal) : new HashSet<string>());
		HashSet<string> hashSet2 = connectedToolkits.Where(hashSet.Contains).Except(BundledSkillNames).ToHashSet<string>(StringComparer.Ordinal);
		string path = Path.Combine(SkillsDirectory, ".system");
		string[] blockedSystemSkillNames = BlockedSystemSkillNames;
		foreach (string path2 in blockedSystemSkillNames)
		{
			TryDeleteDirectory(Path.Combine(path, path2));
		}
		blockedSystemSkillNames = HiddenBundledSkillNames;
		foreach (string path3 in blockedSystemSkillNames)
		{
			TryDeleteDirectory(Path.Combine(SkillsDirectory, path3));
		}
		HashSet<string> hashSet3 = BundledSkillNames.Concat(hashSet2).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		blockedSystemSkillNames = Directory.GetDirectories(SkillsDirectory);
		foreach (string text in blockedSystemSkillNames)
		{
			string fileName = Path.GetFileName(text);
			if (!fileName.StartsWith('.') && !hashSet3.Contains(fileName))
			{
				TryDeleteDirectory(text);
			}
		}
		foreach (string item in hashSet2)
		{
			SyncSkillFolder(Path.Combine(IntegrationSkillsDirectory, item), Path.Combine(SkillsDirectory, item));
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		blockedSystemSkillNames = BundledSkillNames;
		foreach (string text2 in blockedSystemSkillNames)
		{
			string text3 = Path.Combine(BundledSkillsDirectory, text2);
			if (File.Exists(Path.Combine(text3, "SKILL.md")))
			{
				string text4 = Path.Combine(SkillsDirectory, text2);
				SyncSkillFolder(text3, text4);
				dictionary[text2] = text4;
			}
		}
		return dictionary;
	}

	private static void SyncSkillFolder(string source, string dest)
	{
		string path = Path.Combine(source, "SKILL.md");
		string path2 = Path.Combine(dest, "SKILL.md");
		if (!File.Exists(path2) || !File.ReadAllBytes(path2).AsSpan().SequenceEqual(File.ReadAllBytes(path)))
		{
			TryDeleteDirectory(dest);
			CopyDirectory(source, dest);
		}
	}

	private static void CopyDirectory(string source, string dest)
	{
		Directory.CreateDirectory(dest);
		string[] files = Directory.GetFiles(source);
		foreach (string text in files)
		{
			File.Copy(text, Path.Combine(dest, Path.GetFileName(text)), overwrite: true);
		}
		files = Directory.GetDirectories(source);
		foreach (string text2 in files)
		{
			CopyDirectory(text2, Path.Combine(dest, Path.GetFileName(text2)));
		}
	}

	private static void TryDeleteDirectory(string dir)
	{
		try
		{
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, recursive: true);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Codex", "could not remove " + dir + ": " + ex.Message);
		}
	}

	private static bool WriteIfChanged(string path, string contents)
	{
		try
		{
			if (File.Exists(path) && File.ReadAllText(path, Encoding.UTF8) == contents)
			{
				return false;
			}
		}
		catch
		{
		}
		string text = path + ".tmp";
		File.WriteAllText(text, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		File.Move(text, path, overwrite: true);
		return true;
	}

	static CodexHomeManager()
	{
		_003C_003Ey__InlineArray5<string> buffer = default(_003C_003Ey__InlineArray5<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 0) = "Some apps have more than one connected account (for example a work Gmail and a personal Gmail). With more than one, every tool call for that app must name the account: pass the account id (or the account's address) as the tool's `account` field, e.g. COMPOSIO_MULTI_EXECUTE_TOOL tools[].account. A call that names no account fails with a list of the accounts; pick one and retry.";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 1) = "Reading (search, list, find, summarize): look in EVERY account of that app unless the user named one, and say which account each result came from.";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 2) = "Replying to or acting on something that already exists (an email thread, an event, a file): use the account it lives in. That is always known, so never ask.";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 3) = "Creating something new (a new email, event, doc) or sending: use the account the user named, or the one the context makes obvious (they said work or personal, the recipient is on the work account's domain, this Clicky's job is clearly one of them, or its memory already says which). If it is still unclear, ask ONE short question naming the accounts (\"Send this from your work or your personal Gmail?\") and wait. Never guess.";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 4) = "Once the user tells you which account a job uses, remember it in your notes so you never ask again.";
		MultipleAccountsRules = string.Join("\n", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray5<string>, string>(in buffer, 5));
	}
}

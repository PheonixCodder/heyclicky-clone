using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Brain;

namespace HeyClicky.Core.Tools;

public sealed class CompanionTools
{
	public static readonly IReadOnlySet<string> ClientToolNames = new HashSet<string>(new string[3] { "open_url", "copy_to_clipboard", "insert_text" }.Concat(LocalRootTools.Names).Concat(WorkerTools.Names).Concat(AgentTools.Names));

	private readonly LocalRootTools local;

	private readonly WorkerTools worker;

	private readonly AgentTools agents;

	public AgentTools Agents => agents;

	public CompanionTools(LocalRootTools local, WorkerTools worker, AgentTools agents)
	{
		this.local = local;
		this.worker = worker;
		this.agents = agents;
	}

	public async Task<string> HandleAsync(BrainToolCall call, CompanionToolContext context, CancellationToken ct)
	{
		IAgentToolHost host = agents.Host;
		BrainToolCall call2 = call with
		{
			Arguments = OneBrainJobIds.ArgumentsWithThreadIds(call.Arguments, host.ThreadIdForJobId)
		};
		return OneBrainJobIds.OutputWithJobIds(await HandleByThreadAsync(call2, context, ct).ConfigureAwait(continueOnCapturedContext: false), host.JobIdForThread);
	}

	private Task<string> HandleByThreadAsync(BrainToolCall call, CompanionToolContext context, CancellationToken ct)
	{
		ToolArgs args = new ToolArgs(call.Arguments);
		if (LocalRootTools.Names.Contains(call.Name))
		{
			return local.RunAsync(call.Name, args, ct);
		}
		if (WorkerTools.Names.Contains(call.Name))
		{
			return worker.RunAsync(call.Name, args, ct);
		}
		if (AgentTools.Names.Contains(call.Name))
		{
			return agents.RunAsync(call.Name, args, context, ct);
		}
		Log.Warn("Tools", "unknown tool " + call.Name);
		return Task.FromResult(ToolResults.Json(new JsonObject((JsonNodeOptions?)null)
		{
			["error"] = "not_implemented",
			["tool"] = call.Name
		}));
	}
}

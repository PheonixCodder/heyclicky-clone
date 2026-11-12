using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Brain;

public sealed class BrainReply
{
	public string Code { get; set; } = "";

	public List<BrainComputerCall> ComputerCalls { get; set; } = new List<BrainComputerCall>();

	public string? SpokenSentence { get; set; }

	public string? ScreenshotFile { get; set; }

	public string? ErrorCode { get; set; }

	public string? DriveLeaseId { get; set; }

	public int DriveMessagesRefunded { get; set; }

	public List<JsonNode> Items { get; } = new List<JsonNode>();

	public List<string> SearchRecords { get; } = new List<string>();

	public List<BrainToolCall> ToolCalls { get; set; } = new List<BrainToolCall>();

	public string? UserLayer { get; set; }

	public string? UserLayerSignature { get; set; }

	public string? ModelIdentifier { get; set; }

	public int? GuidedMaxRounds { get; set; }

	public int? FirstTokenMilliseconds { get; set; }

	public int TotalMilliseconds { get; set; }

	public double? CostUsd { get; set; }

	public int LastInputTokens { get; set; }

	public int InputTokens { get; set; }

	public bool KeepsReasoning { get; set; }

	public string? TurnToken { get; set; }

	public bool MessageRecorded { get; set; }

	public bool MessageRefunded { get; set; }

	public bool SawDone { get; set; }

	public string? Error { get; set; }

	public JsonObject? Usage { get; set; }
}

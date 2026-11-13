using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Brain;

public sealed class BrainReplyAccumulator : IBrainStreamSink
{
	private readonly StringBuilder rawText = new StringBuilder(4096);

	private readonly long startedAtTicks;

	private readonly Action<string>? onCodeUpdate;

	private readonly Action<string>? onStatus;

	private readonly Action<BrainToolCall>? onToolCall;

	private readonly Func<long> clock;

	private string lastReportedCode = "";

	private long lastReportAtTicks = -4611686018427387904L;

	private bool codeReportIsPending;

	public BrainReply Reply { get; } = new BrainReply();

	public string RawText => rawText.ToString();

	public Action? OnFirstToken { get; set; }

	public bool SpeechOnly { get; set; }

	public Action<string>? OnDriveLeaseId { get; set; }

	public BrainReplyAccumulator(Action<string>? onCodeUpdate = null, Action<string>? onStatus = null, Action<BrainToolCall>? onToolCall = null, Func<long>? clockMilliseconds = null)
	{
		this.onCodeUpdate = onCodeUpdate;
		this.onStatus = onStatus;
		this.onToolCall = onToolCall;
		clock = clockMilliseconds ?? ((Func<long>)(() => Environment.TickCount64));
		startedAtTicks = clock();
	}

	public void OnText(ReadOnlySpan<char> text)
	{
		if (!Reply.FirstTokenMilliseconds.HasValue)
		{
			Reply.FirstTokenMilliseconds = (int)(clock() - startedAtTicks);
			OnFirstToken?.Invoke();
		}
		rawText.Append(text);
		if (!SpeechOnly)
		{
			if (clock() - lastReportAtTicks >= 60)
			{
				ReportCodeIfItGrew();
			}
			else
			{
				codeReportIsPending = true;
			}
		}
	}

	public void OnEvent(string type, JsonElement root)
	{
		if (codeReportIsPending)
		{
			ReportCodeIfItGrew();
		}
		if (type == null)
		{
			return;
		}
		switch (type.Length)
		{
		case 4:
			switch (type[0])
			{
			case 'm':
				if (type == "meta")
				{
					Reply.ModelIdentifier = ReadString(root, "model");
					Reply.UserLayer = ReadString(root, "user_layer");
					Reply.UserLayerSignature = ReadString(root, "user_layer_signature");
					if (root.TryGetProperty("guided_max_rounds", out var value6) && value6.ValueKind == JsonValueKind.Number)
					{
						Reply.GuidedMaxRounds = value6.GetInt32();
					}
					string text = ReadString(root, "drive_lease_id");
					if (text != null && text.Length > 0)
					{
						Reply.DriveLeaseId = text;
						OnDriveLeaseId?.Invoke(text);
					}
				}
				break;
			case 'd':
				if (type == "done")
				{
					Reply.SawDone = true;
					if (root.TryGetProperty("cost", out var value2) && value2.ValueKind == JsonValueKind.Number)
					{
						Reply.CostUsd = value2.GetDouble();
					}
					Reply.LastInputTokens = ReadInt(root, "last_input");
					Reply.InputTokens = ReadInt(root, "input");
					Reply.KeepsReasoning = root.TryGetProperty("keep_reasoning", out var value3) && value3.ValueKind == JsonValueKind.True;
					Reply.TurnToken = ReadString(root, "turn_token");
					Reply.MessageRecorded = root.TryGetProperty("message_recorded", out var value4) && value4.ValueKind == JsonValueKind.True;
					Reply.MessageRefunded = root.TryGetProperty("message_refunded", out var value5) && value5.ValueKind == JsonValueKind.True;
					Reply.ScreenshotFile = ReadString(root, "screenshot_file");
					Reply.DriveMessagesRefunded = Math.Min(2, Math.Max(0, ReadInt(root, "drive_messages_refunded")));
					Reply.Usage = JsonNode.Parse(root.GetRawText(), null) as JsonObject;
					Reply.Usage?.Remove("type");
				}
				break;
			}
			break;
		case 5:
			switch (type[0])
			{
			case 'i':
			{
				if (!(type == "items") || !root.TryGetProperty("items", out var value9) || value9.ValueKind != JsonValueKind.Array)
				{
					break;
				}
				{
					foreach (JsonElement item in value9.EnumerateArray())
					{
						JsonNode jsonNode = JsonNode.Parse(item.GetRawText(), null);
						if (jsonNode != null)
						{
							Reply.Items.Add(jsonNode);
						}
					}
					break;
				}
			}
			case 'e':
				if (type == "error")
				{
					Reply.Error = ReadString(root, "message") ?? "unknown error";
					Reply.ErrorCode = ReadString(root, "code");
				}
				break;
			}
			break;
		case 6:
			if (type == "status")
			{
				string text3 = ReadString(root, "kind");
				if (text3 != null)
				{
					onStatus?.Invoke(text3);
				}
			}
			break;
		case 13:
			if (type == "search_record")
			{
				string text2 = ReadString(root, "text");
				if (text2 != null && text2.Length > 0)
				{
					Reply.SearchRecords.Add(text2);
				}
			}
			break;
		case 9:
		{
			if (type == "tool_call" && root.TryGetProperty("call", out var value7))
			{
				BrainToolCall brainToolCall = BrainToolCall.FromEvent(value7);
				if ((object)brainToolCall != null)
				{
					onToolCall?.Invoke(brainToolCall);
				}
			}
			break;
		}
		case 10:
			if (type == "tool_calls")
			{
				Reply.ToolCalls = ((root.TryGetProperty("calls", out var value8) && value8.ValueKind == JsonValueKind.Array) ? (from c in value8.EnumerateArray().Select(BrainToolCall.FromEvent)
					where (object)c != null
					select (c)).ToList() : new List<BrainToolCall>());
			}
			break;
		case 14:
			if (type == "computer_calls")
			{
				Reply.ComputerCalls = ((root.TryGetProperty("calls", out var value) && value.ValueKind == JsonValueKind.Array) ? (from c in value.EnumerateArray().Select(BrainComputerCall.FromEvent)
					where (object)c != null
					select (c)).ToList() : new List<BrainComputerCall>());
			}
			break;
		case 7:
		case 8:
		case 11:
		case 12:
			break;
		}
	}

	public void Finish()
	{
		string text = rawText.ToString();
		if (SpeechOnly && !text.Contains("```", StringComparison.Ordinal))
		{
			string text2 = text.Trim();
			if (text2.Length > 0)
			{
				Reply.SpokenSentence = text2;
				Reply.Code = SayLine((text2.Length > 300) ? text2.Substring(0, 300) : text2);
				lastReportedCode = Reply.Code;
				onCodeUpdate?.Invoke(Reply.Code);
			}
			Reply.TotalMilliseconds = (int)(clock() - startedAtTicks);
		}
		else
		{
			string text3 = ProgramText.WithoutLinks(ProgramText.ProgramCode(text, isComplete: true) ?? "").Trim();
			Reply.Code = text3;
			if (text3.Length > 0 && (text3.Length > lastReportedCode.Length || text3 != lastReportedCode))
			{
				lastReportedCode = text3;
				onCodeUpdate?.Invoke(text3);
			}
			if (SpeechOnly && string.IsNullOrWhiteSpace(Reply.SpokenSentence) && !string.IsNullOrWhiteSpace(Reply.Code))
			{
				List<string> spokenLines = SayLines.InOrder(Reply.Code);
				if (spokenLines.Count > 0)
				{
					Reply.SpokenSentence = string.Join(" ", spokenLines);
				}
			}
			Reply.TotalMilliseconds = (int)(clock() - startedAtTicks);
		}
	}

	private void ReportCodeIfItGrew()
	{
		codeReportIsPending = false;
		string? text = ProgramText.ProgramCode(rawText.ToString(), isComplete: false);
		if (text != null)
		{
			string text2 = ProgramText.WithoutLinks(text).Trim();
			if (text2.Length > lastReportedCode.Length)
			{
				lastReportedCode = text2;
				lastReportAtTicks = clock();
				onCodeUpdate?.Invoke(text2);
			}
		}
	}

	public static string SayLine(string sentence)
	{
		return "say(" + JsonSerializer.Serialize(sentence, new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}) + ");";
	}

	private static string? ReadString(JsonElement root, string name)
	{
		if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
		{
			return null;
		}
		return value.GetString();
	}

	private static int ReadInt(JsonElement root, string name)
	{
		if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var value2))
		{
			return 0;
		}
		return value2;
	}
}

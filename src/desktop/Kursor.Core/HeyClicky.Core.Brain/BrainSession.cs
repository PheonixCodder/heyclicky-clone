using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Configuration;

namespace HeyClicky.Core.Brain;

public sealed class BrainSession : IDisposable
{
	private sealed record Ended(string SessionId, List<JsonNode> Thread, List<string> ScreenshotFiles);

	private const string Area = "OneBrain";

	public static readonly TimeSpan IdleBeforeNewSession = TimeSpan.FromHours(2);

	private static readonly TimeSpan SavedThreadRetention = TimeSpan.FromDays(14);

	public const int MaximumScreenshotsInThread = 48;

	public const int ScreenshotsKeptAfterTrim = 32;

	private readonly BrainClient client;

	private readonly Func<CancellationToken, Task<string?>> accessToken;

	private readonly string rootDirectory;

	private readonly Func<DateTimeOffset> now;

	private readonly object gate = new object();

	private Timer? idleSessionEndTimer;

	private readonly List<JsonNode> thread = new List<JsonNode>();

	private readonly List<(string? Key, string Text)> pendingAppNotes = new List<(string, string)>();

	private readonly List<string> codeOfDrawingOnScreen = new List<string>();

	private string? userLayer;

	private string? userLayerSignature;

	private string? accountId;

	private bool hasLookedForSavedSession;

	private bool isCompacting;

	private int compactionGeneration;

	private int compactionNextCheckInputTokens;

	private int lastTurnInputTokens;

	private int sessionBaselineInputTokens;

	private bool isFirstKeyDownAfterReload;

	private (int Width, int Height)? lastCanvasSize;

	private DateTimeOffset lastPrewarmAt = DateTimeOffset.MinValue;

	private DateTimeOffset sessionStartedAt = DateTimeOffset.MinValue;

	private DateTimeOffset lastTurnAt = DateTimeOffset.MinValue;

	private DateTimeOffset previousTurnAt = DateTimeOffset.MinValue;

	private int statsTurnCount;

	private string? environmentSentForSessionId;

	private readonly List<string> sessionScreenshotFiles = new List<string>();

	private bool screenshotFilesOffForSession;

	public const string SafetyRefusalLine = "My AI provider's safety filter blocked this because it looks like hands-on security work. I can still explain the concepts.";

	public string SessionId { get; private set; } = Guid.NewGuid().ToString().ToUpperInvariant();

	public int ThreadCount
	{
		get
		{
			lock (gate)
			{
				return thread.Count;
			}
		}
	}

	public string? UserLayer
	{
		get
		{
			lock (gate)
			{
				return userLayer;
			}
		}
	}

	public string? UserLayerSignature
	{
		get
		{
			lock (gate)
			{
				return userLayerSignature;
			}
		}
	}

	public DateTimeOffset PreviousTurnAt
	{
		get
		{
			lock (gate)
			{
				return previousTurnAt;
			}
		}
	}

	public BrainSession(BrainClient client, Func<CancellationToken, Task<string?>> accessToken, string? rootDirectory = null, Func<DateTimeOffset>? clock = null)
	{
		this.client = client;
		this.accessToken = accessToken;
		this.rootDirectory = rootDirectory ?? Path.Combine(AppBundleConfiguration.AppDataDirectory, "OneBrain");
		now = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.UtcNow));
	}

	public List<JsonNode> ThreadSnapshot()
	{
		lock (gate)
		{
			return thread.ToList();
		}
	}

	public List<JsonNode> ThreadSnapshotForWorker()
	{
		lock (gate)
		{
			return ThreadForWorker(thread, screenshotFilesOffForSession);
		}
	}

	public void TurnOffScreenshotFilesForSession()
	{
		lock (gate)
		{
			screenshotFilesOffForSession = true;
		}
		Log.Info("OneBrain", "screenshots by reference refused: sending them in full for the rest of this session");
	}

	private void PublishStats()
	{
		OneBrainStats shared = OneBrainStats.Shared;
		if (shared.IsEnabled)
		{
			OneBrainStats.Session snapshot;
			lock (gate)
			{
				snapshot = new OneBrainStats.Session
				{
					Id = SessionId,
					StartedAt = sessionStartedAt,
					LastTurnAt = lastTurnAt,
					ThreadItems = thread.Count,
					ScreenshotsInThread = ScreenshotCount(thread),
					LastInputTokens = lastTurnInputTokens,
					BaselineInputTokens = sessionBaselineInputTokens,
					NextCompactionCheckInputTokens = compactionNextCheckInputTokens,
					IsCompacting = isCompacting,
					TurnCount = statsTurnCount
				};
			}
			shared.UpdateSession((OneBrainStats.Session _) => snapshot);
		}
	}

	public void AdoptAccount(string? userId)
	{
		if (string.IsNullOrEmpty(userId))
		{
			return;
		}
		lock (gate)
		{
			if (!(userId == accountId))
			{
				if (accountId != null)
				{
					Log.Info("OneBrain", "another account signed in: starting clean");
					ResetSessionStateLocked();
				}
				accountId = userId;
				hasLookedForSavedSession = false;
			}
		}
	}

	public void ResetForSignOut()
	{
		lock (gate)
		{
			ResetSessionStateLocked();
			accountId = null;
			hasLookedForSavedSession = false;
		}
	}

	private void ResetSessionStateLocked()
	{
		idleSessionEndTimer?.Dispose();
		idleSessionEndTimer = null;
		SessionId = NewSessionId();
		userLayer = null;
		userLayerSignature = null;
		thread.Clear();
		codeOfDrawingOnScreen.Clear();
		pendingAppNotes.Clear();
		lastTurnInputTokens = 0;
		sessionBaselineInputTokens = 0;
		sessionScreenshotFiles.Clear();
		screenshotFilesOffForSession = false;
		isFirstKeyDownAfterReload = false;
		compactionGeneration++;
		compactionNextCheckInputTokens = 0;
		lastTurnAt = DateTimeOffset.MinValue;
		sessionStartedAt = DateTimeOffset.MinValue;
		environmentSentForSessionId = null;
	}

	private static string NewSessionId()
	{
		return Guid.NewGuid().ToString().ToUpperInvariant();
	}

	public void NoteForNextTurn(string note, string? latestForKey = null)
	{
		lock (gate)
		{
			if (latestForKey != null)
			{
				pendingAppNotes.RemoveAll(((string? Key, string Text) n) => n.Key == latestForKey);
			}
			pendingAppNotes.Add((latestForKey, note));
		}
	}

	public void DropNoteForNextTurn(string key)
	{
		lock (gate)
		{
			pendingAppNotes.RemoveAll(((string? Key, string Text) n) => n.Key == key);
		}
	}

	public void NoteKeyDown(bool usesOpenAIBrain, (int Width, int Height) cursorScreenCanvas, BrainRequestFlags? flags = null)
	{
		StartNewSessionIfIdle();
		int conversationTokens;
		string layer;
		string signature;
		string sessionId;
		List<JsonNode> threadCopy;
		(int Width, int Height) canvas;
		lock (gate)
		{
			conversationTokens = BrainPrewarmPolicy.ConversationTokens(lastTurnInputTokens, sessionBaselineInputTokens);
			if (!BrainPrewarmPolicy.ShouldPrewarm(thread.Count, isFirstKeyDownAfterReload, now() - lastTurnAt, conversationTokens, usesOpenAIBrain, now() - lastPrewarmAt) || userLayer == null || userLayerSignature == null)
			{
				return;
			}
			lastPrewarmAt = now();
			layer = userLayer;
			signature = userLayerSignature;
			sessionId = SessionId;
			threadCopy = ThreadForWorker(thread, screenshotFilesOffForSession);
			canvas = lastCanvasSize ?? cursorScreenCanvas;
		}
		BrainRequestFlags requestFlags = flags ?? BrainRequestFlags.None;
		int idleMinutes = (int)(now() - lastTurnAt).TotalMinutes;
		Task.Run(async delegate
		{
			string text = await accessToken(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			if (text != null)
			{
				(int, JsonObject)? tuple = await client.PostJsonAsync("/brain/prewarm", delegate(Utf8JsonWriter writer)
				{
					writer.WriteString("session_id", sessionId);
					writer.WriteString("user_layer", layer);
					writer.WriteString("user_layer_signature", signature);
					BrainClient.WriteThread(writer, "thread", threadCopy);
					writer.WriteStartObject("canvas");
					writer.WriteNumber("w", canvas.Width);
					writer.WriteNumber("h", canvas.Height);
					writer.WriteEndObject();
					writer.WriteBoolean("async_tools", value: true);
					Drawing3DAvailability.WriteFlags(writer);
					requestFlags.Write(writer);
				}, text, null).ConfigureAwait(continueOnCapturedContext: false);
				if (tuple.HasValue)
				{
					OneBrainStats.Shared.Note($"cache warming at key-down (~{conversationTokens / 1000}k, idle {idleMinutes} min)");
					Log.Info("OneBrain", $"prompt cache warming ({tuple.Value.Item1}, conversation ~{conversationTokens} tokens, idle {idleMinutes} min)");
				}
			}
		});
	}

	public void BeginTurnStartingNewSessionIfIdle()
	{
		StartNewSessionIfIdle();
		lock (gate)
		{
			previousTurnAt = lastTurnAt;
			lastTurnAt = now();
			statsTurnCount++;
			ScheduleIdleSessionEndLocked();
		}
		PublishStats();
	}

	private void ScheduleIdleSessionEndLocked()
	{
		idleSessionEndTimer?.Dispose();
		TimeSpan dueTime = IdleBeforeNewSession + TimeSpan.FromSeconds(60L);
		idleSessionEndTimer = new Timer(delegate
		{
			StartNewSessionIfIdle();
		}, null, dueTime, Timeout.InfiniteTimeSpan);
	}

	public void StartNewSessionIfIdle()
	{
		Ended ended;
		lock (gate)
		{
			if (!hasLookedForSavedSession)
			{
				hasLookedForSavedSession = true;
				RestoreSavedSessionIfRecentLocked();
			}
			DateTimeOffset dateTimeOffset = ((lastTurnAt > sessionStartedAt) ? lastTurnAt : sessionStartedAt);
			if (now() - dateTimeOffset <= IdleBeforeNewSession)
			{
				return;
			}
			ended = BeginNewSessionLocked();
		}
		AfterNewSession(ended);
	}

	public void StartFreshSession(string? userId)
	{
		AdoptAccount(userId);
		Ended ended;
		lock (gate)
		{
			if (!hasLookedForSavedSession)
			{
				hasLookedForSavedSession = true;
				RestoreSavedSessionIfRecentLocked();
			}
			ended = BeginNewSessionLocked();
			pendingAppNotes.Clear();
			environmentSentForSessionId = null;
			lastTurnAt = now();
			ScheduleIdleSessionEndLocked();
		}
		OneBrainStats.Shared.Note("fresh session (Settings)");
		AfterNewSession(ended);
		SaveToDisk();
	}

	private Ended BeginNewSessionLocked()
	{
		idleSessionEndTimer?.Dispose();
		idleSessionEndTimer = null;
		Ended result = new Ended(SessionId, WithoutScreenshots(thread), sessionScreenshotFiles.ToList());
		sessionScreenshotFiles.Clear();
		screenshotFilesOffForSession = false;
		SessionId = NewSessionId();
		userLayer = null;
		userLayerSignature = null;
		thread.Clear();
		codeOfDrawingOnScreen.Clear();
		lastTurnInputTokens = 0;
		sessionBaselineInputTokens = 0;
		isFirstKeyDownAfterReload = false;
		compactionGeneration++;
		compactionNextCheckInputTokens = 0;
		sessionStartedAt = now();
		statsTurnCount = 0;
		Log.Info("OneBrain", "new session " + SessionId);
		return result;
	}

	private void AfterNewSession(Ended? ended)
	{
		string sessionId;
		lock (gate)
		{
			sessionId = SessionId;
		}
		OneBrainStats.Shared.ResetForNewSession();
		OneBrainStats.Shared.Note("new session " + ((sessionId.Length > 8) ? sessionId.Substring(0, 8) : sessionId));
		PublishStats();
		if ((object)ended != null)
		{
			List<JsonNode> list = ended.Thread;
			if (list != null && list.Count > 0)
			{
				EndSessionOnWorker(ended.SessionId, ended.Thread, ended.ScreenshotFiles);
			}
		}
	}

	private void EndSessionOnWorker(string endedSessionId, List<JsonNode> endedThread, List<string> screenshotFiles)
	{
		Task.Run(async delegate
		{
			string text = await accessToken(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			if (text != null)
			{
				(int, JsonObject)? tuple = await client.PostJsonAsync("/brain/session-end", delegate(Utf8JsonWriter writer)
				{
					writer.WriteString("session_id", endedSessionId);
					BrainClient.WriteThread(writer, "thread", endedThread);
					if (screenshotFiles.Count > 0)
					{
						writer.WriteStartArray("screenshot_files");
						foreach (string screenshotFile in screenshotFiles)
						{
							writer.WriteStringValue(screenshotFile);
						}
						writer.WriteEndArray();
					}
				}, text, null).ConfigureAwait(continueOnCapturedContext: false);
				Log.Info("OneBrain", $"session {endedSessionId} handed to the Worker ({tuple?.Item1.ToString() ?? "failed"})");
			}
		});
	}

	public BrainTurnContext BeginRequest((int Width, int Height) canvas, bool hasDrawingOnScreen, bool screenless = false)
	{
		lock (gate)
		{
			bool isSessionsFirstTurn = thread.Count == 0;
			foreach (var pendingAppNote in pendingAppNotes)
			{
				thread.Add(Message("developer", pendingAppNote.Text));
			}
			pendingAppNotes.Clear();
			if (!screenless)
			{
				lastCanvasSize = canvas;
				if (!hasDrawingOnScreen)
				{
					codeOfDrawingOnScreen.Clear();
				}
			}
			bool flag = environmentSentForSessionId != SessionId;
			if (flag)
			{
				environmentSentForSessionId = SessionId;
			}
			return new BrainTurnContext(SessionId, isSessionsFirstTurn, flag, hasDrawingOnScreen ? DrawingCodeMissingFromThreadLocked() : null);
		}
	}

	public void AdoptUserLayer(string layer, string? signature)
	{
		lock (gate)
		{
			if (userLayer == null)
			{
				userLayer = layer;
				userLayerSignature = signature;
			}
		}
	}

	public List<(string Role, string Text)> RecentConversation(int limit)
	{
		List<JsonNode> list;
		lock (gate)
		{
			list = thread.ToList();
		}
		List<(string, string)> list2 = new List<(string, string)>();
		foreach (JsonNode item in list)
		{
			if (!(item is JsonObject jsonObject))
			{
				continue;
			}
			string text = (string?)jsonObject["type"];
			if (text != null && text != "message")
			{
				continue;
			}
			string text2 = (string?)jsonObject["role"];
			if ((!(text2 == "user") && !(text2 == "assistant")) || 1 == 0)
			{
				continue;
			}
			JsonNode jsonNode = jsonObject["content"];
			string text3 = ((jsonNode is JsonValue jsonValue) ? (((string?)(JsonNode?)jsonValue) ?? "") : ((!(jsonNode is JsonArray source)) ? "" : string.Join("\n", from p in source.OfType<JsonObject>()
				select (string?)p["text"] into t
				where t != null
				select t)));
			string text4 = text3;
			if (text2 == "assistant")
			{
				List<string> list3 = SayLines.InOrder(text4);
				if (list3.Count == 0)
				{
					continue;
				}
				text4 = string.Join(" ", list3);
			}
			else if (text4.StartsWith("[This turn]", StringComparison.Ordinal))
			{
				continue;
			}
			if (!string.IsNullOrWhiteSpace(text4))
			{
				list2.Add((text2, text4.Trim()));
			}
		}
		return list2.TakeLast(limit).ToList();
	}

	public void NoteBaselineInputTokens(int firstInput)
	{
		lock (gate)
		{
			if (sessionBaselineInputTokens == 0 && firstInput > 0)
			{
				sessionBaselineInputTokens = firstInput;
			}
		}
	}

	public static bool IsSafetyRefusal(string? failure)
	{
		return failure?.Contains("cyber_policy", StringComparison.Ordinal) ?? false;
	}

	public bool CommitTurn(string sessionIdAtStart, string note, string said, string? screenshotDataUrl, IReadOnlyList<JsonNode> items, IReadOnlyList<string> searchRecords, bool wasInterrupted, bool keepReasoning, string? drawingCode, string? screenshotFile = null, bool refusedBySafetyFilter = false)
	{
		lock (gate)
		{
			if (SessionId != sessionIdAtStart)
			{
				Log.Info("OneBrain", "the session changed during this turn (sign-out, another account or a fresh session): not kept");
				return false;
			}
			isFirstKeyDownAfterReload = false;
			thread.Add(Message("developer", note));
			if (screenshotDataUrl != null)
			{
				JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "input_image",
					["image_url"] = screenshotDataUrl
				};
				if (screenshotFile != null)
				{
					jsonObject["clicky_file"] = screenshotFile;
					sessionScreenshotFiles.Add(screenshotFile);
				}
				List<JsonNode> list = thread;
				JsonObject obj = new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "message",
					["role"] = "user"
				};
				global::_003C_003Ey__InlineArray2<JsonNode> buffer = default(global::_003C_003Ey__InlineArray2<JsonNode>);
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 0) = jsonObject;
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 1) = new JsonObject((JsonNodeOptions?)null)
				{
					["type"] = "input_text",
					["text"] = said
				};
				obj["content"] = new JsonArray(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(in buffer, 2));
				list.Add(obj);
				if (ScreenshotCount(thread) > 48)
				{
					List<JsonNode> collection = WithNewestScreenshots(thread, 32);
					thread.Clear();
					thread.AddRange(collection);
					Log.Info("OneBrain", $"more than {48} screenshots in the thread: kept the newest {32}");
				}
			}
			else
			{
				thread.Add(Message("user", said));
			}
			foreach (string searchRecord in searchRecords)
			{
				if (searchRecord.Length > 0)
				{
					thread.Add(Message("developer", searchRecord));
				}
			}
			HashSet<string> hashSet = (from i in items
				where (string?)i["type"] == "function_call_output"
				select (string?)i["call_id"]).ToHashSet();
			foreach (JsonNode item in items)
			{
				switch ((string?)item["type"])
				{
				case "message":
				{
					if ((string?)item["role"] != "assistant")
					{
						break;
					}
					List<string> list2 = (from t in (item["content"] as JsonArray)?.Select((JsonNode c) => (string?)c?["text"])
						where t != null
						select t).ToList() ?? new List<string>();
					if (list2.Count != 0)
					{
						JsonObject obj2 = new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "message",
							["role"] = "assistant"
						};
						JsonNode reference = new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "output_text",
							["text"] = string.Concat(list2)
						};
						obj2["content"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
						JsonObject jsonObject3 = obj2;
						string text3 = (string?)item["phase"];
						if (text3 != null && text3.Length > 0)
						{
							jsonObject3["phase"] = text3;
						}
						thread.Add(jsonObject3);
					}
					break;
				}
				case "function_call":
					if (hashSet.Contains((string?)item["call_id"]))
					{
						thread.Add(new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "function_call",
							["call_id"] = ((string?)item["call_id"]) ?? "",
							["name"] = ((string?)item["name"]) ?? "",
							["arguments"] = ((string?)item["arguments"]) ?? "{}"
						});
					}
					break;
				case "function_call_output":
					thread.Add(new JsonObject((JsonNodeOptions?)null)
					{
						["type"] = "function_call_output",
						["call_id"] = ((string?)item["call_id"]) ?? "",
						["output"] = ((string?)item["output"]) ?? ""
					});
					break;
				case "reasoning":
				{
					if (!keepReasoning)
					{
						break;
					}
					string text = (string?)item["encrypted_content"];
					if (text != null && text.Length > 0)
					{
						JsonObject jsonObject2 = new JsonObject((JsonNodeOptions?)null)
						{
							["type"] = "reasoning",
							["summary"] = new JsonArray((JsonNodeOptions?)null),
							["encrypted_content"] = text
						};
						string text2 = (string?)item["id"];
						if (text2 != null && text2.Length > 0)
						{
							jsonObject2["id"] = text2;
						}
						thread.Add(jsonObject2);
					}
					break;
				}
				}
			}
			if (refusedBySafetyFilter)
			{
				thread.Add(Message("developer", "[App] The provider's safety filter refused this answer because it looked like hands-on security work, and the user was told so. If they ask why, say that plainly and offer to explain the concepts."));
			}
			else if (wasInterrupted)
			{
				thread.Add(Message("developer", "[App] The user cut this answer off here."));
			}
			if (!string.IsNullOrEmpty(drawingCode) && !wasInterrupted)
			{
				codeOfDrawingOnScreen.Add(drawingCode);
			}
			lastTurnAt = now();
		}
		return true;
	}

	public void AppendDeveloperNote(string sessionIdAtStart, string text)
	{
		lock (gate)
		{
			if (SessionId == sessionIdAtStart)
			{
				thread.Add(Message("developer", text));
			}
		}
	}

	public void AppendToolResult(BrainToolCall call, string output)
	{
		lock (gate)
		{
			thread.Add(new JsonObject((JsonNodeOptions?)null)
			{
				["type"] = "function_call",
				["call_id"] = call.CallId,
				["name"] = call.Name,
				["arguments"] = call.Arguments
			});
			thread.Add(new JsonObject((JsonNodeOptions?)null)
			{
				["type"] = "function_call_output",
				["call_id"] = call.CallId,
				["output"] = output
			});
		}
	}

	public void ForgetDrawingOnScreen()
	{
		lock (gate)
		{
			codeOfDrawingOnScreen.Clear();
		}
	}

	private string? DrawingCodeMissingFromThreadLocked()
	{
		if (codeOfDrawingOnScreen.Count == 0)
		{
			return null;
		}
		List<string> assistantTexts = (from i in thread
			where (string?)i["role"] == "assistant"
			select string.Concat((i["content"] as JsonArray)?.Select((JsonNode c) => (string?)c?["text"]) ?? Array.Empty<string>())).ToList();
		if (!codeOfDrawingOnScreen.Any(delegate(string code)
		{
			string start = ((code.Length > 120) ? code.Substring(0, 120) : code);
			return !assistantTexts.Any((string t) => t.Contains(start, StringComparison.Ordinal));
		}))
		{
			return null;
		}
		string text = string.Join("\n", codeOfDrawingOnScreen);
		if (text.Length <= 16000)
		{
			return text;
		}
		int length = text.Length;
		int num = length - 16000;
		return text.Substring(num, length - num);
	}

	public void CompactThreadIfLarge(int lastInputTokens)
	{
		int num;
		List<JsonNode> sentThread;
		string sentSessionId;
		int generation;
		int sentCount;
		lock (gate)
		{
			if (lastInputTokens > 0)
			{
				lastTurnInputTokens = lastInputTokens;
			}
			num = sessionBaselineInputTokens;
			if (isCompacting || !BrainCompactionPolicy.ShouldAskWorker(lastInputTokens, num, compactionNextCheckInputTokens))
			{
				sentThread = null;
				sentSessionId = null;
				generation = 0;
				sentCount = -1;
			}
			else
			{
				isCompacting = true;
				sentThread = ThreadForWorker(thread, screenshotFilesOffForSession);
				sentCount = thread.Count;
				sentSessionId = SessionId;
				generation = compactionGeneration;
			}
		}
		PublishStats();
		if (sentCount < 0)
		{
			return;
		}
		OneBrainStats.Shared.Note($"compacting at {lastInputTokens / 1000}k input ({sentCount} items)");
		PublishStats();
		Log.Info("OneBrain", $"input reached {lastInputTokens} tokens (conversation ~{Math.Max(0, lastInputTokens - num)}): asking the Worker to compact the thread ({sentCount} items)");
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				string text = await accessToken(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
				if (text != null)
				{
					BrainCompactReply brainCompactReply = await client.CompactAsync(sentThread, sentSessionId, lastInputTokens, text).ConfigureAwait(continueOnCapturedContext: false);
					if ((object)brainCompactReply != null)
					{
						ApplyCompaction(brainCompactReply, sentCount, sentSessionId, generation, lastInputTokens);
					}
				}
			}
			finally
			{
				lock (gate)
				{
					isCompacting = false;
				}
				PublishStats();
			}
		});
	}

	internal void ApplyCompaction(BrainCompactReply reply, int sentCount, string sentSessionId, int generation, int inputTokens)
	{
		lock (gate)
		{
			if (SessionId != sentSessionId || compactionGeneration != generation)
			{
				Log.Info("OneBrain", "compaction came back after the thread moved on; not applied");
				return;
			}
			int? tokensUntilFold = reply.TokensUntilFold;
			if (tokensUntilFold.HasValue)
			{
				int valueOrDefault = tokensUntilFold.GetValueOrDefault();
				compactionNextCheckInputTokens = BrainCompactionPolicy.NextCheckInputTokens(inputTokens, valueOrDefault);
				Log.Info("OneBrain", $"too little to fold yet; asking again at {compactionNextCheckInputTokens} input tokens");
				OneBrainStats.Shared.Note($"too little to fold; next ask at {compactionNextCheckInputTokens / 1000}k");
				return;
			}
			BrainCompactResult compacted = reply.Compacted;
			if ((object)compacted == null)
			{
				return;
			}
			if (thread.Count < sentCount || compacted.DropLeadingItems < 0 || compacted.DropLeadingItems > compacted.KeepLeadingItems || compacted.KeepLeadingItems > compacted.KeepFromIndex || compacted.KeepFromIndex > sentCount)
			{
				Log.Info("OneBrain", "compaction came back after the thread moved on; not applied");
				return;
			}
			int count = thread.Count;
			List<JsonNode> list = new List<JsonNode>();
			list.AddRange(thread.GetRange(compacted.DropLeadingItems, compacted.KeepLeadingItems - compacted.DropLeadingItems));
			if (compacted.NoteItem != null)
			{
				list.Add(compacted.NoteItem);
			}
			list.AddRange(thread.GetRange(compacted.KeepFromIndex, thread.Count - compacted.KeepFromIndex));
			thread.Clear();
			thread.AddRange(list);
			compactionGeneration++;
			compactionNextCheckInputTokens = 0;
			lastTurnInputTokens = 0;
			string value = ((compacted.DropLeadingItems > 0) ? $", the oldest {compacted.DropLeadingItems} notes left" : "");
			Log.Info("OneBrain", $"thread compacted: {count} -> {thread.Count} items{value}");
			OneBrainStats.Shared.Note($"compacted {count} → {thread.Count} items{value} (next turn rewrites the cache)");
		}
		SaveToDisk();
	}

	private string? AccountDirectoryLocked()
	{
		if (string.IsNullOrEmpty(accountId))
		{
			return null;
		}
		string text = new string(accountId.Where((char c) => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray());
		if (text.Length != 0)
		{
			return Path.Combine(rootDirectory, text);
		}
		return null;
	}

	public void SaveToDisk()
	{
		string text;
		byte[] bytes;
		string fileName;
		lock (gate)
		{
			text = AccountDirectoryLocked();
			if (text == null)
			{
				return;
			}
			JsonObject obj = new JsonObject((JsonNodeOptions?)null)
			{
				["session_id"] = SessionId,
				["last_turn_at"] = (double)lastTurnAt.ToUnixTimeMilliseconds() / 1000.0,
				["user_layer"] = userLayer,
				["user_layer_signature"] = userLayerSignature,
				["thread"] = new JsonArray((from i in WithoutScreenshots(thread)
					select i.DeepClone()).ToArray()),
				["drawing_code"] = new JsonArray(((IEnumerable<string>)codeOfDrawingOnScreen).Select((Func<string, JsonNode>)((string c) => JsonValue.Create(c, (JsonNodeOptions?)null))).ToArray()),
				["last_input_tokens"] = lastTurnInputTokens,
				["baseline_input_tokens"] = sessionBaselineInputTokens
			};
			(int, int)? tuple = lastCanvasSize;
			JsonArray value;
			if (tuple.HasValue)
			{
				(int, int) valueOrDefault = tuple.GetValueOrDefault();
				global::_003C_003Ey__InlineArray2<JsonNode> buffer = default(global::_003C_003Ey__InlineArray2<JsonNode>);
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 0) = valueOrDefault.Item1;
				global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(ref buffer, 1) = valueOrDefault.Item2;
				value = new JsonArray(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray2<JsonNode>, JsonNode>(in buffer, 2));
			}
			else
			{
				value = new JsonArray((JsonNodeOptions?)null);
			}
			obj["canvas"] = value;
			obj["environment_sent"] = environmentSentForSessionId == SessionId;
			JsonObject value2 = obj;
			bytes = JsonSerializer.SerializeToUtf8Bytes(value2, JsonNodeContext.Default.JsonObject);
			fileName = SessionId + ".json";
		}
		string threads = Path.Combine(text, "threads");
		Task.Run(delegate
		{
			try
			{
				AtomicFile.WriteAllBytes(Path.Combine(threads, fileName), bytes);
			}
			catch (Exception ex)
			{
				Log.Warn("OneBrain", "thread not saved: " + ex.Message);
			}
		});
	}

	private void RestoreSavedSessionIfRecentLocked()
	{
		string text = AccountDirectoryLocked();
		if (text == null)
		{
			return;
		}
		string path = Path.Combine(text, "threads");
		if (!Directory.Exists(path))
		{
			return;
		}
		try
		{
			FileInfo[] files = new DirectoryInfo(path).GetFiles("*.json");
			foreach (FileInfo item in files.Where((FileInfo f) => now() - f.LastWriteTimeUtc > SavedThreadRetention))
			{
				item.Delete();
			}
			FileInfo fileInfo = files.Where((FileInfo f) => f.Exists && now() - f.LastWriteTimeUtc <= IdleBeforeNewSession).MaxBy((FileInfo f) => f.LastWriteTimeUtc);
			if (fileInfo == null || !(JsonNode.Parse(File.ReadAllBytes(fileInfo.FullName), null) is JsonObject jsonObject))
			{
				return;
			}
			string text2 = (string?)jsonObject["session_id"];
			if (text2 == null || !(jsonObject["thread"] is JsonArray source))
			{
				return;
			}
			DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)((jsonObject["last_turn_at"]?.GetValue<double>() ?? 0.0) * 1000.0));
			if (now() - dateTimeOffset > IdleBeforeNewSession)
			{
				return;
			}
			SessionId = text2;
			lastTurnAt = dateTimeOffset;
			userLayer = (string?)jsonObject["user_layer"];
			userLayerSignature = (string?)jsonObject["user_layer_signature"];
			thread.Clear();
			thread.AddRange(from i in source
				where i != null
				select i.DeepClone());
			codeOfDrawingOnScreen.Clear();
			if (jsonObject["drawing_code"] is JsonArray source2)
			{
				codeOfDrawingOnScreen.AddRange(from c in source2
					select (string?)c into c
					where c != null
					select c);
			}
			lastTurnInputTokens = (int)(jsonObject["last_input_tokens"]?.GetValue<double>() ?? 0.0);
			sessionBaselineInputTokens = (int)(jsonObject["baseline_input_tokens"]?.GetValue<double>() ?? 0.0);
			if (jsonObject["canvas"] is JsonArray { Count: 2 } jsonArray)
			{
				lastCanvasSize = ((int)jsonArray[0].GetValue<double>(), (int)jsonArray[1].GetValue<double>());
			}
			isFirstKeyDownAfterReload = true;
			JsonNode? jsonNode = jsonObject["environment_sent"];
			if (jsonNode != null && jsonNode.GetValue<bool>())
			{
				environmentSentForSessionId = text2;
			}
			Log.Info("OneBrain", $"picked up session {text2} again ({thread.Count} items, last turn {(int)(now() - lastTurnAt).TotalMinutes} min ago)");
			OneBrainStats.Shared.Note($"picked up {((text2.Length > 8) ? text2.Substring(0, 8) : text2)} again ({thread.Count} items, no screenshots)");
		}
		catch (Exception ex)
		{
			Log.Warn("OneBrain", "saved session not restored: " + ex.Message);
		}
	}

	public void LogRecord(JsonObject record)
	{
		string directory;
		string sessionId;
		lock (gate)
		{
			directory = AccountDirectoryLocked();
			sessionId = SessionId;
		}
		if (directory == null)
		{
			return;
		}
		record["session"] = sessionId;
		record["at"] = DateTimeOffset.UtcNow.ToString("O");
		string line = record.ToJsonString() + "\n";
		Task.Run(delegate
		{
			try
			{
				Directory.CreateDirectory(directory);
				File.AppendAllText(Path.Combine(directory, sessionId + ".jsonl"), line);
			}
			catch
			{
			}
		});
	}

	public static JsonObject Message(string role, string text)
	{
		JsonObject obj = new JsonObject((JsonNodeOptions?)null)
		{
			["type"] = "message",
			["role"] = role
		};
		JsonNode reference = new JsonObject((JsonNodeOptions?)null)
		{
			["type"] = "input_text",
			["text"] = text
		};
		obj["content"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
		return obj;
	}

	private static bool IsScreenshotBlock(JsonNode? block)
	{
		return (string?)block?["type"] == "input_image";
	}

	public static int ScreenshotCount(IEnumerable<JsonNode> items)
	{
		return items.Sum((JsonNode i) => (i["content"] as JsonArray)?.Count(IsScreenshotBlock) ?? 0);
	}

	public static List<JsonNode> ThreadForWorker(IEnumerable<JsonNode> items, bool screenshotFilesOff)
	{
		return items.Select(delegate(JsonNode item)
		{
			if (item["content"] is JsonArray source && source.Any((JsonNode b) => b?["clicky_file"] != null))
			{
				JsonObject obj = (JsonObject)item.DeepClone();
				obj["content"] = new JsonArray(source.Select(delegate(JsonNode block)
				{
					if (!(block?["clicky_file"] is JsonValue jsonValue))
					{
						return block?.DeepClone();
					}
					if (screenshotFilesOff)
					{
						JsonObject obj2 = (JsonObject)block.DeepClone();
						obj2.Remove("clicky_file");
						return obj2;
					}
					return new JsonObject((JsonNodeOptions?)null)
					{
						["type"] = "input_image",
						["clicky_file"] = jsonValue.DeepClone()
					};
				}).ToArray());
				return obj;
			}
			return item;
		}).ToList();
	}

	public static List<JsonNode> WithoutScreenshots(IEnumerable<JsonNode> items)
	{
		return items.Select(delegate(JsonNode item)
		{
			if (item["content"] is JsonArray source && source.Any(IsScreenshotBlock))
			{
				JsonObject obj = (JsonObject)item.DeepClone();
				obj["content"] = new JsonArray((from b in source
					where !IsScreenshotBlock(b)
					select b.DeepClone()).ToArray());
				return obj;
			}
			return item;
		}).ToList();
	}

	public static List<JsonNode> WithNewestScreenshots(List<JsonNode> items, int keep)
	{
		int toDrop = ScreenshotCount(items) - keep;
		if (toDrop <= 0)
		{
			return items;
		}
		return items.Select(delegate(JsonNode item)
		{
			if (toDrop <= 0 || !(item["content"] is JsonArray jsonArray) || !jsonArray.Any(IsScreenshotBlock))
			{
				return item;
			}
			JsonObject jsonObject = (JsonObject)item.DeepClone();
			JsonArray jsonArray2 = new JsonArray((JsonNodeOptions?)null);
			foreach (JsonNode item2 in jsonArray)
			{
				if (toDrop > 0 && IsScreenshotBlock(item2))
				{
					toDrop--;
				}
				else
				{
					jsonArray2.Add(item2.DeepClone());
				}
			}
			jsonObject["content"] = jsonArray2;
			return jsonObject;
		}).ToList();
	}

	public static string StoredUserText(string words, string fact)
	{
		string text2 = Trimmed(words, 4000);
		if (text2.Length != 0)
		{
			return text2;
		}
		return "(" + Trimmed(fact, 1000) + " Here is the screen now.)";
		static string Trimmed(string text, int limit)
		{
			string text3 = text.Trim();
			if (text3.Length <= limit)
			{
				return text3;
			}
			return text3.Substring(0, limit);
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			idleSessionEndTimer?.Dispose();
		}
	}
}

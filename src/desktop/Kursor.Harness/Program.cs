using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Harness;

internal static class Program
{
    private const string HarnessVersion = "1.1.5";
    private static readonly SemaphoreSlim StdoutLock = new(1, 1);
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly HashSet<string> SupportedHarnesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "h1-mac",
        "h1-cloud",
        "pi-mac",
        "pi-cloud",
        "codex-mac",
        "codex-cloud"
    };

    private sealed class JobState
    {
        public string JobId { get; init; } = string.Empty;
        public object Sync { get; } = new();
        public List<JsonObject> Events { get; } = new();
        public long LatestSeq { get; set; }
        public bool IsRunning { get; set; }
        public CancellationTokenSource? TurnCts { get; set; }
        public TaskCompletionSource<bool> EventSignal { get; set; } = CreateSignal();
        public List<string> SteerQueue { get; } = new();

        public static TaskCompletionSource<bool> CreateSignal() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void AppendEvent(string type, Action<JsonObject>? configure = null)
        {
            TaskCompletionSource<bool> signalToComplete;
            lock (Sync)
            {
                LatestSeq++;
                JsonObject ev = new()
                {
                    ["seq"] = LatestSeq,
                    ["type"] = type
                };
                configure?.Invoke(ev);
                Events.Add(ev);
                signalToComplete = EventSignal;
                EventSignal = CreateSignal();
            }
            signalToComplete.TrySetResult(true);
        }
    }

    private sealed class StdioThreadState
    {
        public string ThreadId { get; init; } = string.Empty;
        public string? Model { get; set; }
        public string? BaseInstructions { get; set; }
        public CancellationTokenSource? ActiveTurnCts { get; set; }
        public string? ActiveTurnId { get; set; }
    }

    public static async Task<int> Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--version", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(a, "-v", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"kursor-harness {HarnessVersion}");
            return 0;
        }

        bool explicitStdio = args.Any(a =>
            string.Equals(a, "--stdio", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "app-server", StringComparison.OrdinalIgnoreCase));

        string? runnerToken = Environment.GetEnvironmentVariable("CLICKY_HARNESS_TOKEN");
        if (!explicitStdio && !string.IsNullOrWhiteSpace(runnerToken))
        {
            return await RunHttpRunnerModeAsync(runnerToken!).ConfigureAwait(false);
        }

        return await RunStdioJsonRpcModeAsync().ConfigureAwait(false);
    }

    // =========================================================================
    // Mode 1: JSON-RPC 2.0 over Stdio (--stdio / app-server / CodexSession)
    // =========================================================================
    private static async Task<int> RunStdioJsonRpcModeAsync()
    {
        ConcurrentDictionary<string, StdioThreadState> threads = new(StringComparer.Ordinal);
        ConcurrentBag<Task> activeTurnTasks = new();
        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.IsCancellationRequested)
            {
                string? line = await Console.In.ReadLineAsync(cts.Token).ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                line = line.Trim().TrimStart('\uFEFF');
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                JsonObject? msg;
                try
                {
                    msg = JsonNode.Parse(line) as JsonObject;
                }
                catch
                {
                    continue;
                }

                if (msg == null)
                {
                    continue;
                }

                string? method = GetString(msg, "method");
                JsonNode? idNode = msg.TryGetPropertyValue("id", out JsonNode? rawId) ? rawId?.DeepClone() : null;
                JsonObject? @params = msg["params"] as JsonObject;

                if (string.IsNullOrWhiteSpace(method))
                {
                    // Response from client to a server-initiated request (e.g. item/tool/call)
                    continue;
                }

                await HandleStdioMethodAsync(method!, idNode, @params, threads, activeTurnTasks, cts.Token).ConfigureAwait(false);
            }

            if (!activeTurnTasks.IsEmpty)
            {
                await Task.WhenAll(activeTurnTasks).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (TimeoutException)
        {
        }

        return 0;
    }

    private static async Task HandleStdioMethodAsync(
        string method,
        JsonNode? idNode,
        JsonObject? @params,
        ConcurrentDictionary<string, StdioThreadState> threads,
        ConcurrentBag<Task> activeTurnTasks,
        CancellationToken ct)
    {
        switch (method)
        {
            case "initialize":
            {
                JsonObject result = new()
                {
                    ["protocolVersion"] = "2025-03-26",
                    ["capabilities"] = new JsonObject
                    {
                        ["experimentalApi"] = true
                    },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "kursor-harness",
                        ["version"] = HarnessVersion
                    },
                    ["userAgent"] = $"kursor-harness/{HarnessVersion}"
                };
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, result, ct).ConfigureAwait(false);
                }
                break;
            }

            case "initialized":
            case "notifications/initialized":
            {
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, new JsonObject { ["ok"] = true }, ct).ConfigureAwait(false);
                }
                break;
            }

            case "account/read":
            {
                JsonObject result = new()
                {
                    ["account"] = new JsonObject
                    {
                        ["type"] = "apiKey",
                        ["email"] = "local@kursor.dev",
                        ["planType"] = "pro"
                    },
                    ["requiresOpenaiAuth"] = false
                };
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, result, ct).ConfigureAwait(false);
                }
                break;
            }

            case "account/login/start":
            {
                string loginId = Guid.NewGuid().ToString("N");
                JsonObject result = new()
                {
                    ["type"] = "apiKey",
                    ["loginId"] = loginId,
                    ["authUrl"] = "http://127.0.0.1:8787/auth"
                };
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, result, ct).ConfigureAwait(false);
                }

                await WriteStdioRpcNotificationAsync("account/login/completed", new JsonObject
                {
                    ["loginId"] = loginId,
                    ["success"] = true
                }, ct).ConfigureAwait(false);
                break;
            }

            case "thread/start":
            case "thread/resume":
            {
                string threadId = GetString(@params, "threadId") ?? $"thr_{Guid.NewGuid():N}";
                StdioThreadState state = threads.GetOrAdd(threadId, id => new StdioThreadState { ThreadId = id });
                if (GetString(@params, "model") is string model && !string.IsNullOrWhiteSpace(model))
                {
                    state.Model = model;
                }
                if (GetString(@params, "baseInstructions") is string instr && !string.IsNullOrWhiteSpace(instr))
                {
                    state.BaseInstructions = instr;
                }

                JsonObject result = new()
                {
                    ["threadId"] = threadId,
                    ["thread"] = new JsonObject
                    {
                        ["id"] = threadId
                    }
                };
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, result, ct).ConfigureAwait(false);
                }

                await WriteStdioRpcNotificationAsync("thread/started", new JsonObject
                {
                    ["thread"] = new JsonObject
                    {
                        ["id"] = threadId
                    }
                }, ct).ConfigureAwait(false);
                break;
            }

            case "turn/start":
            {
                string threadId = GetString(@params, "threadId") ?? $"thr_{Guid.NewGuid():N}";
                StdioThreadState state = threads.GetOrAdd(threadId, id => new StdioThreadState { ThreadId = id });
                if (GetString(@params, "model") is string model && !string.IsNullOrWhiteSpace(model))
                {
                    state.Model = model;
                }

                string turnId = $"turn_{Guid.NewGuid():N}";
                string promptText = ExtractStdioPrompt(@params);

                state.ActiveTurnCts?.Cancel();
                CancellationTokenSource turnCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                state.ActiveTurnCts = turnCts;
                state.ActiveTurnId = turnId;

                JsonObject result = new()
                {
                    ["turnId"] = turnId,
                    ["turn"] = new JsonObject
                    {
                        ["id"] = turnId,
                        ["status"] = "in_progress"
                    }
                };
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, result, ct).ConfigureAwait(false);
                }

                Task turnTask = Task.Run(() => ExecuteStdioTurnAsync(state, turnId, promptText, turnCts.Token), ct);
                activeTurnTasks.Add(turnTask);
                break;
            }

            case "turn/interrupt":
            {
                string? threadId = GetString(@params, "threadId");
                string? turnId = GetString(@params, "turnId");

                if (!string.IsNullOrWhiteSpace(threadId) && threads.TryGetValue(threadId!, out StdioThreadState? state))
                {
                    state.ActiveTurnCts?.Cancel();
                }
                else
                {
                    foreach (StdioThreadState s in threads.Values)
                    {
                        if (turnId == null || string.Equals(s.ActiveTurnId, turnId, StringComparison.Ordinal))
                        {
                            s.ActiveTurnCts?.Cancel();
                        }
                    }
                }

                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, new JsonObject { ["ok"] = true }, ct).ConfigureAwait(false);
                }
                break;
            }

            default:
            {
                if (idNode != null)
                {
                    await WriteStdioRpcResultAsync(idNode, new JsonObject { ["ok"] = true }, ct).ConfigureAwait(false);
                }
                break;
            }
        }
    }

    private static async Task ExecuteStdioTurnAsync(
        StdioThreadState state,
        string turnId,
        string promptText,
        CancellationToken turnCt)
    {
        string itemId = $"msg_{Guid.NewGuid():N}";
        try
        {
            await WriteStdioRpcNotificationAsync("turn/started", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turn"] = new JsonObject
                {
                    ["id"] = turnId,
                    ["status"] = "in_progress"
                }
            }, CancellationToken.None).ConfigureAwait(false);

            await WriteStdioRpcNotificationAsync("item/started", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turnId"] = turnId,
                ["item"] = new JsonObject
                {
                    ["id"] = itemId,
                    ["type"] = "agentMessage",
                    ["phase"] = "final_answer",
                    ["text"] = string.Empty
                }
            }, CancellationToken.None).ConfigureAwait(false);

            string replyText = await CompletePromptWithBackendOrFallbackAsync(
                promptText,
                state.BaseInstructions,
                state.Model,
                workerBaseUrl: null,
                workerToken: null,
                turnCt).ConfigureAwait(false);

            turnCt.ThrowIfCancellationRequested();

            await WriteStdioRpcNotificationAsync("item/agentMessage/delta", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turnId"] = turnId,
                ["itemId"] = itemId,
                ["delta"] = replyText
            }, CancellationToken.None).ConfigureAwait(false);

            await WriteStdioRpcNotificationAsync("item/completed", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turnId"] = turnId,
                ["item"] = new JsonObject
                {
                    ["id"] = itemId,
                    ["type"] = "agentMessage",
                    ["phase"] = "final_answer",
                    ["text"] = replyText
                }
            }, CancellationToken.None).ConfigureAwait(false);

            await WriteStdioRpcNotificationAsync("turn/completed", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turn"] = new JsonObject
                {
                    ["id"] = turnId,
                    ["status"] = "completed"
                }
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await WriteStdioRpcNotificationAsync("turn/completed", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turn"] = new JsonObject
                {
                    ["id"] = turnId,
                    ["status"] = "interrupted"
                }
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await WriteStdioRpcNotificationAsync("turn/completed", new JsonObject
            {
                ["threadId"] = state.ThreadId,
                ["turn"] = new JsonObject
                {
                    ["id"] = turnId,
                    ["status"] = "failed",
                    ["error"] = new JsonObject
                    {
                        ["message"] = ex.Message
                    }
                }
            }, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static string ExtractStdioPrompt(JsonObject? @params)
    {
        if (@params == null)
        {
            return string.Empty;
        }

        if (GetString(@params, "prompt") is string directPrompt && !string.IsNullOrWhiteSpace(directPrompt))
        {
            return directPrompt;
        }

        if (@params["input"] is JsonArray inputArr)
        {
            StringBuilder sb = new();
            foreach (JsonNode? item in inputArr)
            {
                if (item is JsonObject obj)
                {
                    string? text = GetString(obj, "text");
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        if (sb.Length > 0)
                        {
                            sb.AppendLine();
                        }
                        sb.Append(text);
                    }
                }
                else if (item is JsonValue val && val.TryGetValue(out string? s) && !string.IsNullOrWhiteSpace(s))
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }
                    sb.Append(s);
                }
            }
            return sb.ToString();
        }

        return string.Empty;
    }

    private static async Task WriteStdioRpcResultAsync(JsonNode idNode, JsonObject result, CancellationToken ct)
    {
        JsonObject envelope = new()
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idNode.DeepClone(),
            ["result"] = result
        };
        await WriteStdoutLineAsync(envelope.ToJsonString(), ct).ConfigureAwait(false);
    }

    private static async Task WriteStdioRpcNotificationAsync(string method, JsonObject @params, CancellationToken ct)
    {
        JsonObject envelope = new()
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = @params
        };
        await WriteStdoutLineAsync(envelope.ToJsonString(), ct).ConfigureAwait(false);
    }

    private static async Task WriteStdoutLineAsync(string line, CancellationToken ct)
    {
        await StdoutLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await Console.Out.WriteLineAsync(line).ConfigureAwait(false);
            await Console.Out.FlushAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            StdoutLock.Release();
        }
    }

    // =========================================================================
    // Mode 2: Loopback HTTP Runner Mode (AgentHarnessRunnerProcess / Runtime)
    // =========================================================================
    private static async Task<int> RunHttpRunnerModeAsync(string expectedToken)
    {
        string stateRoot = Environment.GetEnvironmentVariable("CLICKY_HARNESS_STATE")
            ?? Path.Combine(Path.GetTempPath(), "kursor-harness-state");
        Directory.CreateDirectory(stateRoot);

        ConcurrentDictionary<string, JobState> jobs = new(StringComparer.Ordinal);
        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        await WriteStdoutLineAsync(new JsonObject
        {
            ["ready"] = true,
            ["port"] = port
        }.ToJsonString(), cts.Token).ConfigureAwait(false);

        // Monitor stdin closure (parent process termination)
        _ = Task.Run(async () =>
        {
            try
            {
                char[] buf = new char[64];
                while (!cts.IsCancellationRequested)
                {
                    int read = await Console.In.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false);
                    if (read == 0)
                    {
                        cts.Cancel();
                        listener.Stop();
                        break;
                    }
                }
            }
            catch
            {
                cts.Cancel();
                try
                {
                    listener.Stop();
                }
                catch
                {
                }
            }
        });

        try
        {
            while (!cts.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                _ = Task.Run(() => HandleHttpClientAsync(client, expectedToken, stateRoot, jobs, cts.Token));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (SocketException) when (cts.IsCancellationRequested)
        {
        }
        finally
        {
            try
            {
                listener.Stop();
            }
            catch
            {
            }
        }

        return 0;
    }

    private static async Task HandleHttpClientAsync(
        TcpClient client,
        string expectedToken,
        string stateRoot,
        ConcurrentDictionary<string, JobState> jobs,
        CancellationToken serverCt)
    {
        using (client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                (string method, string rawTarget, Dictionary<string, string> headers, string body)? req =
                    await ReadHttpRequestAsync(stream, serverCt).ConfigureAwait(false);
                if (req == null)
                {
                    return;
                }

                (string method, string rawTarget, Dictionary<string, string> headers, string body) = req.Value;

                if (!IsAuthorized(headers, expectedToken))
                {
                    await WriteHttpResponseAsync(stream, 401, new JsonObject { ["error"] = "unauthorized" }, serverCt).ConfigureAwait(false);
                    return;
                }

                string path = rawTarget;
                string query = string.Empty;
                int qIndex = rawTarget.IndexOf('?');
                if (qIndex >= 0)
                {
                    path = rawTarget.Substring(0, qIndex);
                    query = rawTarget.Substring(qIndex + 1);
                }

                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(path, "/prewarm", StringComparison.Ordinal))
                {
                    await WriteHttpResponseAsync(stream, 200, new JsonObject
                    {
                        ["ok"] = true,
                        ["started"] = false
                    }, serverCt).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(path, "/turn", StringComparison.Ordinal))
                {
                    JsonObject? payload = ParseJsonObject(body);
                    string harness = GetString(payload, "harness") ?? string.Empty;
                    string place = GetString(payload, "place") ?? string.Empty;
                    string key = $"{harness}-{place}";

                    if (!SupportedHarnesses.Contains(key))
                    {
                        await WriteHttpResponseAsync(stream, 500, new JsonObject
                        {
                            ["error"] = $"No {key} harness in this runner."
                        }, serverCt).ConfigureAwait(false);
                        return;
                    }

                    string jobId = GetString(payload, "job") ?? Guid.NewGuid().ToString("N");
                    JobState jobState = jobs.GetOrAdd(jobId, id => new JobState { JobId = id });

                    long afterSeq;
                    CancellationTokenSource turnCts = CancellationTokenSource.CreateLinkedTokenSource(serverCt);
                    lock (jobState.Sync)
                    {
                        jobState.TurnCts?.Cancel();
                        jobState.TurnCts = turnCts;
                        jobState.IsRunning = true;
                        afterSeq = jobState.LatestSeq;
                    }

                    string turnRef = $"{harness}:{jobId}";
                    PersistH1JobMetadata(stateRoot, jobId, payload);

                    _ = Task.Run(() => ExecuteHttpRunnerTurnAsync(jobState, payload, stateRoot, turnCts.Token), serverCt);

                    await WriteHttpResponseAsync(stream, 200, new JsonObject
                    {
                        ["ok"] = true,
                        ["after"] = afterSeq,
                        ["turnRef"] = turnRef
                    }, serverCt).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(path, "/events", StringComparison.Ordinal))
                {
                    Dictionary<string, string> q = ParseQueryString(query);
                    string jobId = q.TryGetValue("job", out string? j) ? j : string.Empty;
                    long after = q.TryGetValue("after", out string? a) && long.TryParse(a, out long parsedAfter) ? parsedAfter : 0;
                    int waitMs = q.TryGetValue("wait", out string? w) && int.TryParse(w, out int parsedWait) ? Math.Clamp(parsedWait, 0, 25000) : 0;

                    if (!jobs.TryGetValue(jobId, out JobState? jobState))
                    {
                        await WriteHttpResponseAsync(stream, 200, new JsonObject
                        {
                            ["events"] = new JsonArray(),
                            ["latest"] = 0
                        }, serverCt).ConfigureAwait(false);
                        return;
                    }

                    JsonArray eventsArr = CollectEventsAfter(jobState, after, out long latest);
                    if (eventsArr.Count == 0 && waitMs > 0)
                    {
                        Task signalTask;
                        lock (jobState.Sync)
                        {
                            signalTask = jobState.EventSignal.Task;
                        }

                        try
                        {
                            await signalTask.WaitAsync(TimeSpan.FromMilliseconds(waitMs), serverCt).ConfigureAwait(false);
                        }
                        catch (TimeoutException)
                        {
                        }

                        eventsArr = CollectEventsAfter(jobState, after, out latest);
                    }

                    await WriteHttpResponseAsync(stream, 200, new JsonObject
                    {
                        ["events"] = eventsArr,
                        ["latest"] = latest
                    }, serverCt).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(path, "/steer", StringComparison.Ordinal))
                {
                    JsonObject? payload = ParseJsonObject(body);
                    string jobId = GetString(payload, "job") ?? string.Empty;
                    string steerText = GetString(payload, "text") ?? string.Empty;

                    if (jobs.TryGetValue(jobId, out JobState? jobState))
                    {
                        bool running;
                        lock (jobState.Sync)
                        {
                            running = jobState.IsRunning;
                            if (running && !string.IsNullOrWhiteSpace(steerText))
                            {
                                jobState.SteerQueue.Add(steerText);
                            }
                        }

                        if (running)
                        {
                            await WriteHttpResponseAsync(stream, 200, new JsonObject { ["ok"] = true }, serverCt).ConfigureAwait(false);
                            return;
                        }
                    }

                    await WriteHttpResponseAsync(stream, 200, new JsonObject
                    {
                        ["ok"] = false,
                        ["reason"] = "not running"
                    }, serverCt).ConfigureAwait(false);
                    return;
                }

                if (string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(path, "/cancel", StringComparison.Ordinal))
                {
                    JsonObject? payload = ParseJsonObject(body);
                    string jobId = GetString(payload, "job") ?? string.Empty;
                    if (jobs.TryGetValue(jobId, out JobState? jobState))
                    {
                        CancellationTokenSource? ctsToCancel;
                        lock (jobState.Sync)
                        {
                            ctsToCancel = jobState.TurnCts;
                        }
                        ctsToCancel?.Cancel();
                    }

                    await WriteHttpResponseAsync(stream, 200, new JsonObject { ["ok"] = true }, serverCt).ConfigureAwait(false);
                    return;
                }

                await WriteHttpResponseAsync(stream, 404, new JsonObject { ["error"] = "not found" }, serverCt).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    private static async Task ExecuteHttpRunnerTurnAsync(
        JobState jobState,
        JsonObject? payload,
        string stateRoot,
        CancellationToken turnCt)
    {
        string jobId = jobState.JobId;
        string harness = GetString(payload, "harness") ?? "h1";
        string turnRef = $"{harness}:{jobId}";

        try
        {
            jobState.AppendEvent("status", ev =>
            {
                ev["text"] = "Thinking...";
                ev["turnRef"] = turnRef;
            });

            string prompt = GetString(payload, "prompt") ?? string.Empty;
            string? instructions = GetString(payload, "instructions");
            string? model = GetString(payload?["model"] as JsonObject, "id");
            string? workerBase = GetString(payload?["worker"] as JsonObject, "base");
            string? workerToken = GetString(payload?["worker"] as JsonObject, "token");

            jobState.AppendEvent("message_start", ev =>
            {
                ev["role"] = "assistant";
            });

            string replyText = await CompletePromptWithBackendOrFallbackAsync(
                prompt,
                instructions,
                model,
                workerBase,
                workerToken,
                turnCt).ConfigureAwait(false);

            turnCt.ThrowIfCancellationRequested();

            jobState.AppendEvent("message_update", ev =>
            {
                ev["text"] = replyText;
                ev["delta"] = replyText;
            });

            jobState.AppendEvent("message_end", ev =>
            {
                ev["text"] = replyText;
            });

            PersistH1JobMessage(stateRoot, jobId, prompt, replyText);

            lock (jobState.Sync)
            {
                jobState.IsRunning = false;
            }

            jobState.AppendEvent("turn_end", ev =>
            {
                ev["status"] = "completed";
                ev["turnRef"] = turnRef;
            });
        }
        catch (OperationCanceledException)
        {
            lock (jobState.Sync)
            {
                jobState.IsRunning = false;
            }

            jobState.AppendEvent("turn_end", ev =>
            {
                ev["status"] = "interrupted";
                ev["turnRef"] = turnRef;
            });
        }
        catch (Exception ex)
        {
            lock (jobState.Sync)
            {
                jobState.IsRunning = false;
            }

            jobState.AppendEvent("turn_end", ev =>
            {
                ev["status"] = "failed";
                ev["error"] = ex.Message;
                ev["turnRef"] = turnRef;
            });
        }
    }

    private static void PersistH1JobMetadata(string stateRoot, string jobId, JsonObject? payload)
    {
        try
        {
            string localDir = Path.Combine(stateRoot, "h1-local", jobId);
            string macDir = Path.Combine(stateRoot, "h1-mac", jobId);
            Directory.CreateDirectory(localDir);
            Directory.CreateDirectory(macDir);

            string h1JsonPath = Path.Combine(macDir, "h1.json");
            JsonObject meta = new()
            {
                ["job"] = jobId,
                ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                ["model"] = GetString(payload?["model"] as JsonObject, "id") ?? "gpt-5.4"
            };
            File.WriteAllText(h1JsonPath, meta.ToJsonString());
        }
        catch
        {
        }
    }

    private static void PersistH1JobMessage(string stateRoot, string jobId, string prompt, string reply)
    {
        try
        {
            string macDir = Path.Combine(stateRoot, "h1-mac", jobId);
            Directory.CreateDirectory(macDir);
            string h1JsonPath = Path.Combine(macDir, "h1.json");
            JsonObject stateObj = new()
            {
                ["job"] = jobId,
                ["updatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["content"] = prompt },
                    new JsonObject { ["role"] = "assistant", ["content"] = reply }
                }
            };
            File.WriteAllText(h1JsonPath, stateObj.ToJsonString());
        }
        catch
        {
        }
    }

    private static JsonArray CollectEventsAfter(JobState jobState, long after, out long latest)
    {
        JsonArray arr = new();
        lock (jobState.Sync)
        {
            latest = jobState.LatestSeq;
            foreach (JsonObject ev in jobState.Events)
            {
                if (ev["seq"] is JsonValue seqVal && seqVal.TryGetValue(out long s) && s > after)
                {
                    arr.Add(ev.DeepClone());
                }
            }
        }
        return arr;
    }

    // =========================================================================
    // Shared LLM Backend Completion + Clean Local Fallback
    // =========================================================================
    private static async Task<string> CompletePromptWithBackendOrFallbackAsync(
        string prompt,
        string? instructions,
        string? model,
        string? workerBaseUrl,
        string? workerToken,
        CancellationToken ct)
    {
        string effectiveModel = string.IsNullOrWhiteSpace(model) ? "gpt-5.4" : model!;

        // 1. If a worker base URL was provided by AgentHarnessRunnerRuntime (e.g. http://127.0.0.1:<port> or backend worker)
        if (!string.IsNullOrWhiteSpace(workerBaseUrl))
        {
            string? workerReply = await TryCallWorkerResponsesEndpointAsync(
                workerBaseUrl!,
                workerToken,
                effectiveModel,
                prompt,
                instructions,
                ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(workerReply))
            {
                return workerReply!;
            }
        }

        // 2. Check environment-configured OpenAI / Kursor worker endpoints
        string? envBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
            ?? Environment.GetEnvironmentVariable("HEYCLICKY_WORKER_BASE_URL");
        string? envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? workerToken;

        if (!string.IsNullOrWhiteSpace(envBaseUrl))
        {
            string? envReply = await TryCallOpenAiCompatibleAsync(
                envBaseUrl!,
                envApiKey,
                effectiveModel,
                prompt,
                instructions,
                ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(envReply))
            {
                return envReply!;
            }
        }

        // 3. Try default local Kursor backend worker at http://127.0.0.1:8787 if port 8787 is listening
        if (await IsLocalPortOpenAsync(8787, ct).ConfigureAwait(false))
        {
            string? localWorkerReply = await TryCallWorkerResponsesEndpointAsync(
                "http://127.0.0.1:8787",
                envApiKey,
                effectiveModel,
                prompt,
                instructions,
                ct,
                timeoutSeconds: 5).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(localWorkerReply))
            {
                return localWorkerReply!;
            }
        }

        // 4. Clean deterministic offline fallback
        string trimmed = (prompt ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return "Ready.";
        }
        return $"[kursor-harness] Processed: {trimmed}";
    }

    private static async Task<bool> IsLocalPortOpenAsync(int port, CancellationToken ct)
    {
        try
        {
            using TcpClient tcp = new();
            using CancellationTokenSource connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromMilliseconds(150));
            await tcp.ConnectAsync(IPAddress.Loopback, port, connectCts.Token).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> TryCallWorkerResponsesEndpointAsync(
        string baseUrl,
        string? token,
        string model,
        string prompt,
        string? instructions,
        CancellationToken ct,
        int timeoutSeconds = 15)
    {
        try
        {
            string trimmedBase = baseUrl.TrimEnd('/');
            string url = $"{trimmedBase}/agent/openai/v1/responses";

            JsonObject body = new()
            {
                ["model"] = model,
                ["input"] = prompt
            };
            if (!string.IsNullOrWhiteSpace(instructions))
            {
                body["instructions"] = instructions;
            }

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using HttpRequestMessage req = new(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using HttpResponseMessage resp = await Http.SendAsync(req, timeoutCts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            string raw = await resp.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);
            return ExtractTextFromLlmResponse(raw);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> TryCallOpenAiCompatibleAsync(
        string baseUrl,
        string? apiKey,
        string model,
        string prompt,
        string? instructions,
        CancellationToken ct)
    {
        try
        {
            string trimmedBase = baseUrl.TrimEnd('/');
            string url = trimmedBase.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                ? trimmedBase
                : $"{trimmedBase}/chat/completions";

            JsonArray messages = new();
            if (!string.IsNullOrWhiteSpace(instructions))
            {
                messages.Add(new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = instructions
                });
            }
            messages.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = prompt
            });

            JsonObject body = new()
            {
                ["model"] = model,
                ["messages"] = messages
            };

            using HttpRequestMessage req = new(HttpMethod.Post, url)
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            using HttpResponseMessage resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            string raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return ExtractTextFromLlmResponse(raw);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractTextFromLlmResponse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        try
        {
            JsonObject? root = JsonNode.Parse(rawJson) as JsonObject;
            if (root == null)
            {
                return null;
            }

            if (GetString(root, "output_text") is string outputText && !string.IsNullOrWhiteSpace(outputText))
            {
                return outputText;
            }

            if (root["output"] is JsonArray outputArr)
            {
                StringBuilder sb = new();
                foreach (JsonNode? item in outputArr)
                {
                    if (item is JsonObject itemObj)
                    {
                        if (GetString(itemObj, "text") is string directText && !string.IsNullOrWhiteSpace(directText))
                        {
                            sb.Append(directText);
                        }
                        if (itemObj["content"] is JsonArray contentArr)
                        {
                            foreach (JsonNode? part in contentArr)
                            {
                                if (part is JsonObject partObj && GetString(partObj, "text") is string pText)
                                {
                                    sb.Append(pText);
                                }
                            }
                        }
                    }
                }
                if (sb.Length > 0)
                {
                    return sb.ToString();
                }
            }

            if (root["choices"] is JsonArray choicesArr &&
                choicesArr.Count > 0 &&
                choicesArr[0] is JsonObject firstChoice &&
                firstChoice["message"] is JsonObject msgObj &&
                GetString(msgObj, "content") is string choiceText)
            {
                return choiceText;
            }
        }
        catch
        {
        }

        return null;
    }

    // =========================================================================
    // Minimal HTTP/1.1 Loopback Helpers
    // =========================================================================
    private static async Task<(string Method, string Target, Dictionary<string, string> Headers, string Body)?> ReadHttpRequestAsync(
        NetworkStream stream,
        CancellationToken ct)
    {
        List<byte> headerBytes = new(1024);
        byte[] oneByte = new byte[1];
        while (headerBytes.Count < 65536)
        {
            int n = await stream.ReadAsync(oneByte.AsMemory(0, 1), ct).ConfigureAwait(false);
            if (n == 0)
            {
                return null;
            }
            headerBytes.Add(oneByte[0]);
            int count = headerBytes.Count;
            if (count >= 4 &&
                headerBytes[count - 4] == '\r' &&
                headerBytes[count - 3] == '\n' &&
                headerBytes[count - 2] == '\r' &&
                headerBytes[count - 1] == '\n')
            {
                break;
            }
        }

        string headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
        string[] lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return null;
        }

        string[] requestLine = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (requestLine.Length < 2)
        {
            return null;
        }

        string method = requestLine[0];
        string target = requestLine[1];
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon > 0)
            {
                string key = lines[i].Substring(0, colon).Trim();
                string val = lines[i].Substring(colon + 1).Trim();
                headers[key] = val;
            }
        }

        int contentLength = 0;
        if (headers.TryGetValue("Content-Length", out string? lenStr) && int.TryParse(lenStr, out int parsedLen) && parsedLen > 0)
        {
            contentLength = Math.Min(parsedLen, 16 * 1024 * 1024);
        }

        string body = string.Empty;
        if (contentLength > 0)
        {
            byte[] bodyBytes = new byte[contentLength];
            int offset = 0;
            while (offset < contentLength)
            {
                int r = await stream.ReadAsync(bodyBytes.AsMemory(offset, contentLength - offset), ct).ConfigureAwait(false);
                if (r == 0)
                {
                    break;
                }
                offset += r;
            }
            body = Encoding.UTF8.GetString(bodyBytes, 0, offset);
        }

        return (method, target, headers, body);
    }

    private static async Task WriteHttpResponseAsync(
        NetworkStream stream,
        int statusCode,
        JsonObject payload,
        CancellationToken ct)
    {
        string reason = statusCode switch
        {
            200 => "OK",
            401 => "Unauthorized",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "OK"
        };

        byte[] bodyBytes = Encoding.UTF8.GetBytes(payload.ToJsonString());
        string header = $"HTTP/1.1 {statusCode} {reason}\r\n" +
                        "Content-Type: application/json; charset=utf-8\r\n" +
                        $"Content-Length: {bodyBytes.Length}\r\n" +
                        "Connection: close\r\n\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(header);

        await stream.WriteAsync(headerBytes, ct).ConfigureAwait(false);
        await stream.WriteAsync(bodyBytes, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static bool IsAuthorized(Dictionary<string, string> headers, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            return true;
        }

        if (!headers.TryGetValue("Authorization", out string? auth) || string.IsNullOrWhiteSpace(auth))
        {
            return false;
        }

        const string bearerPrefix = "Bearer ";
        if (auth.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string actual = auth.Substring(bearerPrefix.Length).Trim();
            return string.Equals(actual, expectedToken, StringComparison.Ordinal);
        }

        return false;
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = part.IndexOf('=');
            if (eq > 0)
            {
                string key = Uri.UnescapeDataString(part.Substring(0, eq));
                string val = Uri.UnescapeDataString(part.Substring(eq + 1));
                result[key] = val;
            }
            else
            {
                result[Uri.UnescapeDataString(part)] = string.Empty;
            }
        }

        return result;
    }

    private static JsonObject? ParseJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonObject? obj, string key)
    {
        if (obj != null && obj.TryGetPropertyValue(key, out JsonNode? node) && node is JsonValue val && val.TryGetValue(out string? s))
        {
            return s;
        }
        return null;
    }
}

using System.Buffers.Binary;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.BrowserHost;

internal static class Program
{
    private const int MaxNativeMessageBytes = 16 * 1024 * 1024;
    private static readonly SemaphoreSlim StdoutLock = new(1, 1);
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(35)
    };

    private sealed record BridgeConnection(string EndpointUrl, string Token, bool IsMcp);

    public static async Task<int> Main(string[] args)
    {
        if (args.Any(a => string.Equals(a, "--version", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(a, "-v", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("ClickyBrowserHost 1.1.5");
            return 0;
        }

        string? extensionId = ParseExtensionIdFromArgs(args);
        string sessionId = Guid.NewGuid().ToString("N");
        string profileId = Guid.NewGuid().ToString("N");
        string? browserName = DetectParentBrowser();

        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        Task? pollLoopTask = null;
        object pollStartGate = new();

        void EnsurePollLoopStarted()
        {
            lock (pollStartGate)
            {
                if (pollLoopTask == null)
                {
                    pollLoopTask = Task.Run(() => RunPollLoopAsync(
                        () => profileId,
                        () => sessionId,
                        () => browserName,
                        () => extensionId,
                        cts.Token));
                }
            }
        }

        try
        {
            using Stream stdin = Console.OpenStandardInput();
            while (!cts.IsCancellationRequested)
            {
                byte[]? messageBytes = await ReadNativeFrameAsync(stdin, cts.Token).ConfigureAwait(false);
                if (messageBytes == null)
                {
                    break;
                }

                BridgeConnection? connection = ResolveBridgeConnection();
                if (connection == null)
                {
                    await Console.Error.WriteLineAsync("ClickyBrowserHost: browser bridge connection config not found.").ConfigureAwait(false);
                    return 1;
                }

                JsonObject? payload = null;
                try
                {
                    payload = JsonNode.Parse(messageBytes) as JsonObject;
                }
                catch
                {
                    // Ignore malformed JSON frames or wrap raw text
                }

                if (payload == null)
                {
                    continue;
                }

                if (TryGetString(payload, "profile", out string? msgProfile) ||
                    TryGetString(payload, "profileId", out msgProfile))
                {
                    if (!string.IsNullOrWhiteSpace(msgProfile))
                    {
                        profileId = msgProfile!;
                    }
                }

                if (TryGetString(payload, "session", out string? msgSession) ||
                    TryGetString(payload, "sessionId", out msgSession))
                {
                    if (!string.IsNullOrWhiteSpace(msgSession))
                    {
                        sessionId = msgSession!;
                    }
                }

                if (TryGetString(payload, "browser", out string? msgBrowser) && !string.IsNullOrWhiteSpace(msgBrowser))
                {
                    browserName = msgBrowser;
                }

                if (TryGetString(payload, "extension", out string? msgExt) && !string.IsNullOrWhiteSpace(msgExt))
                {
                    extensionId = msgExt;
                }

                EnsurePollLoopStarted();

                bool isReply = payload.ContainsKey("id") ||
                               payload.ContainsKey("result") ||
                               payload.ContainsKey("error") ||
                               string.Equals(GetStringOrDefault(payload, "type"), "stop", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(GetStringOrDefault(payload, "type"), "reply", StringComparison.OrdinalIgnoreCase);

                if (isReply)
                {
                    JsonObject replyArgs = (JsonObject)payload.DeepClone();
                    replyArgs["profile"] = profileId;
                    replyArgs["session"] = sessionId;
                    if (!string.IsNullOrWhiteSpace(browserName))
                    {
                        replyArgs["browser"] = browserName;
                    }
                    if (!string.IsNullOrWhiteSpace(extensionId))
                    {
                        replyArgs["extension"] = extensionId;
                    }

                    JsonNode? replyResponse = await SendToBridgeAsync(connection, "browser_reply", replyArgs, cts.Token).ConfigureAwait(false);
                    if (replyResponse is JsonObject replyObj && replyObj.ContainsKey("command") && replyObj["command"] is JsonObject cmdObj)
                    {
                        await WriteNativeFrameAsync(cmdObj, cts.Token).ConfigureAwait(false);
                    }
                }
                else if (string.Equals(GetStringOrDefault(payload, "type"), "ping", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteNativeFrameAsync(new JsonObject
                    {
                        ["type"] = "pong",
                        ["ok"] = true
                    }, cts.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Cancel();
            if (pollLoopTask != null)
            {
                try
                {
                    await pollLoopTask.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }

        return 0;
    }

    private static async Task RunPollLoopAsync(
        Func<string> getProfileId,
        Func<string> getSessionId,
        Func<string?> getBrowserName,
        Func<string?> getExtensionId,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                BridgeConnection? connection = ResolveBridgeConnection();
                if (connection == null)
                {
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    continue;
                }

                JsonObject pollArgs = new()
                {
                    ["profile"] = getProfileId(),
                    ["session"] = getSessionId()
                };

                string? browser = getBrowserName();
                if (!string.IsNullOrWhiteSpace(browser))
                {
                    pollArgs["browser"] = browser;
                }

                string? ext = getExtensionId();
                if (!string.IsNullOrWhiteSpace(ext))
                {
                    pollArgs["extension"] = ext;
                }

                JsonNode? response = await SendToBridgeAsync(connection, "browser_poll", pollArgs, ct).ConfigureAwait(false);
                if (response is JsonObject obj)
                {
                    if (obj["command"] is JsonObject command)
                    {
                        await WriteNativeFrameAsync(command, ct).ConfigureAwait(false);
                    }
                    else if (obj.ContainsKey("id") && obj.ContainsKey("action"))
                    {
                        await WriteNativeFrameAsync(obj, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                try
                {
                    await Task.Delay(750, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private static async Task<JsonNode?> SendToBridgeAsync(
        BridgeConnection connection,
        string toolName,
        JsonObject arguments,
        CancellationToken ct)
    {
        if (connection.IsMcp)
        {
            JsonObject rpcRequest = new()
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 1,
                ["method"] = "tools/call",
                ["params"] = new JsonObject
                {
                    ["name"] = toolName,
                    ["arguments"] = arguments
                }
            };

            using HttpRequestMessage req = new(HttpMethod.Post, connection.EndpointUrl)
            {
                Content = new StringContent(rpcRequest.ToJsonString(), Encoding.UTF8, "application/json")
            };
            AttachAuthHeaders(req, connection.Token);

            using HttpResponseMessage resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            string body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            JsonObject? rpcResp = JsonNode.Parse(body) as JsonObject;
            JsonObject? result = rpcResp?["result"] as JsonObject;
            if (result == null)
            {
                return rpcResp;
            }

            if (result["structuredContent"] is JsonNode structured)
            {
                return structured.DeepClone();
            }

            if (result["content"] is JsonArray contentArr &&
                contentArr.Count > 0 &&
                contentArr[0] is JsonObject firstBlock &&
                TryGetString(firstBlock, "text", out string? innerJson) &&
                !string.IsNullOrWhiteSpace(innerJson))
            {
                try
                {
                    return JsonNode.Parse(innerJson!);
                }
                catch
                {
                }
            }

            return result.DeepClone();
        }
        else
        {
            using HttpRequestMessage req = new(HttpMethod.Post, connection.EndpointUrl)
            {
                Content = new StringContent(arguments.ToJsonString(), Encoding.UTF8, "application/json")
            };
            AttachAuthHeaders(req, connection.Token);

            using HttpResponseMessage resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            string body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(body) ? null : JsonNode.Parse(body);
        }
    }

    private static void AttachAuthHeaders(HttpRequestMessage req, string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.TryAddWithoutValidation("X-Clicky-Browser-Token", token);
        }
    }

    private static BridgeConnection? ResolveBridgeConnection()
    {
        foreach (string candidate in GetCandidateConfigPaths())
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                string raw = File.ReadAllText(candidate).Trim();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                if (int.TryParse(raw, out int plainPort) && plainPort > 0)
                {
                    return new BridgeConnection($"http://127.0.0.1:{plainPort}/mcp", string.Empty, IsMcp: true);
                }

                if (JsonNode.Parse(raw) is JsonObject obj)
                {
                    string token = GetStringOrDefault(obj, "token") ?? string.Empty;
                    if (TryGetString(obj, "url", out string? url) && !string.IsNullOrWhiteSpace(url))
                    {
                        bool isMcp = url!.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase);
                        return new BridgeConnection(url, token, isMcp);
                    }

                    int port = GetIntOrDefault(obj, "port");
                    if (port > 0)
                    {
                        return new BridgeConnection($"http://127.0.0.1:{port}/mcp", token, IsMcp: true);
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateConfigPaths()
    {
        string? explicitPath = Environment.GetEnvironmentVariable("HEYCLICKY_BROWSER_BRIDGE_CONFIG");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            yield return explicitPath!;
        }

        string? dataDir = Environment.GetEnvironmentVariable("HEYCLICKY_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(dataDir))
        {
            yield return Path.Combine(dataDir!, "BrowserBridge", "connection.json");
            yield return Path.Combine(dataDir!, "browser-bridge.json");
            yield return Path.Combine(dataDir!, "browser-bridge.port");
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            foreach (string appFolder in new[] { "HeyClicky", "Kursor" })
            {
                string root = Path.Combine(localAppData, appFolder);
                yield return Path.Combine(root, "BrowserBridge", "connection.json");
                yield return Path.Combine(root, "browser-bridge.json");
                yield return Path.Combine(root, "browser-bridge.port");
            }
        }
    }

    private static async Task<byte[]?> ReadNativeFrameAsync(Stream stdin, CancellationToken ct)
    {
        byte[] lengthBuffer = new byte[4];
        int headerRead = await ReadExactAsync(stdin, lengthBuffer, ct).ConfigureAwait(false);
        if (headerRead == 0)
        {
            return null;
        }
        if (headerRead < 4)
        {
            throw new EndOfStreamException("Incomplete Native Messaging frame header.");
        }

        uint length = BinaryPrimitives.ReadUInt32LittleEndian(lengthBuffer);
        if (length == 0 || length > MaxNativeMessageBytes)
        {
            throw new InvalidDataException($"Invalid Native Messaging frame length: {length}");
        }

        byte[] payload = new byte[length];
        int payloadRead = await ReadExactAsync(stdin, payload, ct).ConfigureAwait(false);
        if (payloadRead < (int)length)
        {
            throw new EndOfStreamException("Incomplete Native Messaging frame payload.");
        }

        return payload;
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct).ConfigureAwait(false);
            if (n == 0)
            {
                return offset;
            }
            offset += n;
        }
        return offset;
    }

    private static async Task WriteNativeFrameAsync(JsonNode node, CancellationToken ct)
    {
        byte[] payload = Encoding.UTF8.GetBytes(node.ToJsonString());
        byte[] header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)payload.Length);

        await StdoutLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using Stream stdout = Console.OpenStandardOutput();
            await stdout.WriteAsync(header, ct).ConfigureAwait(false);
            await stdout.WriteAsync(payload, ct).ConfigureAwait(false);
            await stdout.FlushAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            StdoutLock.Release();
        }
    }

    private static string? ParseExtensionIdFromArgs(string[] args)
    {
        foreach (string arg in args)
        {
            const string prefix = "chrome-extension://";
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string id = arg.Substring(prefix.Length).TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return id;
                }
            }
        }
        return null;
    }

    private static string? DetectParentBrowser()
    {
        string? envBrowser = Environment.GetEnvironmentVariable("CLICKY_BROWSER_EXE");
        if (!string.IsNullOrWhiteSpace(envBrowser))
        {
            return envBrowser;
        }
        return "chrome.exe";
    }

    private static bool TryGetString(JsonObject obj, string key, out string? value)
    {
        value = GetStringOrDefault(obj, key);
        return value != null;
    }

    private static string? GetStringOrDefault(JsonObject obj, string key)
    {
        if (obj.TryGetPropertyValue(key, out JsonNode? node) && node is JsonValue val && val.TryGetValue(out string? s))
        {
            return s;
        }
        return null;
    }

    private static int GetIntOrDefault(JsonObject obj, string key)
    {
        if (obj.TryGetPropertyValue(key, out JsonNode? node) && node is JsonValue val)
        {
            if (val.TryGetValue(out int i))
            {
                return i;
            }
            if (val.TryGetValue(out string? s) && int.TryParse(s, out int parsed))
            {
                return parsed;
            }
        }
        return 0;
    }
}

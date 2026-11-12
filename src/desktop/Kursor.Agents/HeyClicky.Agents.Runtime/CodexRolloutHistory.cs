using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Agents.Runtime;

internal static class CodexRolloutHistory
{
	public sealed record RolloutThread(string ThreadId, DateTimeOffset? CreatedAt, DateTimeOffset UpdatedAt, string? Cwd, string FirstUserMessage);

	public sealed record RolloutTurn(IReadOnlyList<string> UserMessages, IReadOnlyList<string> ProgressLines, IReadOnlyList<string> Replies, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt);

	[CompilerGenerated]
	private sealed class _003CLines_003Ed__15 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private string path;

		public string _003C_003E3__path;

		private StreamReader _003Creader_003E5__2;

		private StreamReader _003C_003E7__wrap2;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CLines_003Ed__15(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 1)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003Creader_003E5__2 = null;
			_003C_003E7__wrap2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = _003C_003E1__state;
				if (num != 0)
				{
					if (num != 1)
					{
						return false;
					}
					_003C_003E1__state = -3;
				}
				else
				{
					_003C_003E1__state = -1;
					_003Creader_003E5__2 = Open(path);
					if (_003Creader_003E5__2 == null)
					{
						return false;
					}
					_003C_003E7__wrap2 = _003Creader_003E5__2;
					_003C_003E1__state = -3;
				}
				while (true)
				{
					string text = ReadLine(_003Creader_003E5__2);
					if (text == null)
					{
						break;
					}
					if (text.Length > 0)
					{
						_003C_003E2__current = text;
						_003C_003E1__state = 1;
						return true;
					}
				}
				_003C_003Em__Finally1();
				_003C_003E7__wrap2 = null;
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap2 != null)
			{
				((IDisposable)_003C_003E7__wrap2).Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CLines_003Ed__15 _003CLines_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CLines_003Ed__ = this;
			}
			else
			{
				_003CLines_003Ed__ = new _003CLines_003Ed__15(0);
			}
			_003CLines_003Ed__.path = _003C_003E3__path;
			return _003CLines_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private const int ListScanLineLimit = 400;

	private static string SessionsDirectory(string codexHome)
	{
		return Path.Combine(codexHome, "sessions");
	}

	private static IEnumerable<(string ThreadId, FileInfo File)> RolloutFiles(string codexHome)
	{
		string path = SessionsDirectory(codexHome);
		if (!Directory.Exists(path))
		{
			return Array.Empty<(string, FileInfo)>();
		}
		try
		{
			return (from f in new DirectoryInfo(path).EnumerateFiles("rollout-*.jsonl", SearchOption.AllDirectories)
				select (ThreadId: ThreadIdFromFileName(f.Name), File: f) into x
				where x.ThreadId != null
				orderby x.File.LastWriteTimeUtc descending
				select (x.ThreadId, File: x.File)).ToList();
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			Log.Warn("Agents", "Codex sessions unreadable: " + ex.Message);
			return Array.Empty<(string, FileInfo)>();
		}
	}

	internal static string? ThreadIdFromFileName(string fileName)
	{
		if (!fileName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		int length = ".jsonl".Length;
		string text = fileName.Substring(0, fileName.Length - length);
		if (text.Length < 36)
		{
			return null;
		}
		length = text.Length;
		int num = length - 36;
		string text2 = text.Substring(num, length - num);
		if (!Guid.TryParse(text2, out var _))
		{
			return null;
		}
		return text2;
	}

	private static string? RolloutPath(string codexHome, string threadId)
	{
		string path = SessionsDirectory(codexHome);
		if (!Directory.Exists(path) || threadId.Length == 0)
		{
			return null;
		}
		try
		{
			return Directory.EnumerateFiles(path, "rollout-*-" + threadId + ".jsonl", SearchOption.AllDirectories).FirstOrDefault();
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return null;
		}
	}

	public static IReadOnlyList<RolloutThread> List(string codexHome)
	{
		List<RolloutThread> list = new List<RolloutThread>();
		foreach (var item3 in RolloutFiles(codexHome))
		{
			string item = item3.ThreadId;
			FileInfo item2 = item3.File;
			DateTimeOffset? dateTimeOffset = null;
			string text = null;
			string text2 = null;
			bool flag = false;
			int num = 0;
			foreach (string item4 in Lines(item2.FullName))
			{
				if (++num > 400)
				{
					break;
				}
				if (!item4.Contains("session_meta", StringComparison.Ordinal) && !item4.Contains("\"message\"", StringComparison.Ordinal))
				{
					continue;
				}
				JsonObject jsonObject = Parse(item4);
				if (jsonObject == null)
				{
					continue;
				}
				JsonObject jsonObject2 = jsonObject["payload"] as JsonObject;
				if (jsonObject["type"]?.ToString() == "session_meta" && jsonObject2 != null)
				{
					DateTimeOffset? dateTimeOffset2 = dateTimeOffset;
					if (!dateTimeOffset2.HasValue)
					{
						dateTimeOffset = Timestamp(jsonObject2["timestamp"]) ?? Timestamp(jsonObject["timestamp"]);
					}
					if (text == null)
					{
						text = jsonObject2["cwd"]?.ToString();
					}
					flag |= (jsonObject2["source"] as JsonObject)?["subagent"] != null;
				}
				else
				{
					string text3 = UserText(jsonObject);
					if (text3 != null)
					{
						text2 = text3;
						break;
					}
				}
			}
			if (!flag)
			{
				list.Add(new RolloutThread(item, dateTimeOffset ?? new DateTimeOffset(item2.CreationTimeUtc), new DateTimeOffset(item2.LastWriteTimeUtc), text, text2 ?? ""));
			}
		}
		return list;
	}

	public static IReadOnlyList<RolloutTurn> ReadTurns(string codexHome, string threadId)
	{
		string text = RolloutPath(codexHome, threadId);
		if (text == null)
		{
			return Array.Empty<RolloutTurn>();
		}
		List<RolloutTurn> turns = new List<RolloutTurn>();
		List<string> users = null;
		List<string> progress = null;
		List<string> replies = null;
		DateTimeOffset? startedAt = null;
		DateTimeOffset? finishedAt = null;
		foreach (string item in Lines(text))
		{
			if (!item.Contains("\"message\"", StringComparison.Ordinal))
			{
				continue;
			}
			JsonObject jsonObject = Parse(item);
			if (jsonObject == null)
			{
				continue;
			}
			DateTimeOffset? dateTimeOffset = Timestamp(jsonObject["timestamp"]);
			string text2 = UserText(jsonObject);
			if (text2 != null)
			{
				Close();
				int num = 1;
				List<string> list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<string> span = CollectionsMarshal.AsSpan(list);
				int index = 0;
				span[index] = text2;
				users = list;
				progress = new List<string>();
				replies = new List<string>();
				startedAt = dateTimeOffset;
				finishedAt = dateTimeOffset;
			}
			else
			{
				if (users == null)
				{
					continue;
				}
				string text3 = AssistantText(jsonObject);
				if (text3 != null)
				{
					if (IsCommentary(jsonObject))
					{
						progress.Add(text3);
					}
					else
					{
						replies.Add(text3);
					}
					if (dateTimeOffset.HasValue)
					{
						finishedAt = dateTimeOffset;
					}
				}
			}
		}
		Close();
		return turns;
		void Close()
		{
			if (users != null)
			{
				turns.Add(new RolloutTurn(users, progress, replies, startedAt, finishedAt));
			}
		}
	}

	private static string? UserText(JsonObject entry)
	{
		JsonObject jsonObject = MessagePayload(entry, "user");
		if (jsonObject == null)
		{
			return null;
		}
		string text = ContentText(jsonObject).Trim();
		if (text.Length == 0 || IsScaffold(text))
		{
			return null;
		}
		return text;
	}

	private static string? AssistantText(JsonObject entry)
	{
		JsonObject jsonObject = MessagePayload(entry, "assistant");
		if (jsonObject == null)
		{
			return null;
		}
		string text = ContentText(jsonObject).Trim();
		if (text.Length != 0)
		{
			return text;
		}
		return null;
	}

	private static bool IsCommentary(JsonObject entry)
	{
		return (entry["payload"] as JsonObject)?["phase"]?.ToString() == "commentary";
	}

	private static JsonObject? MessagePayload(JsonObject entry, string role)
	{
		if (!(entry["type"]?.ToString() == "response_item") || !(entry["payload"] is JsonObject jsonObject) || !(jsonObject["type"]?.ToString() == "message") || !(jsonObject["role"]?.ToString() == role))
		{
			return null;
		}
		return jsonObject;
	}

	private static string ContentText(JsonObject payload)
	{
		return string.Join("\n", (from part in ((payload["content"] as JsonArray) ?? new JsonArray((JsonNodeOptions?)null)).OfType<JsonObject>()
			select part["text"]?.ToString()).OfType<string>());
	}

	internal static bool IsScaffold(string text)
	{
		if (!text.StartsWith("# AGENTS.md instructions", StringComparison.Ordinal) && !text.StartsWith("<environment_context>", StringComparison.Ordinal))
		{
			return text.StartsWith("<user_instructions>", StringComparison.Ordinal);
		}
		return true;
	}

	[IteratorStateMachine(typeof(_003CLines_003Ed__15))]
	private static IEnumerable<string> Lines(string path)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CLines_003Ed__15(-2)
		{
			_003C_003E3__path = path
		};
	}

	private static StreamReader? Open(string path)
	{
		try
		{
			return new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			Log.Warn("Agents", "Codex rollout unreadable (" + Path.GetFileName(path) + "): " + ex.Message);
			return null;
		}
	}

	private static string? ReadLine(StreamReader reader)
	{
		try
		{
			return reader.ReadLine();
		}
		catch (IOException)
		{
			return null;
		}
	}

	private static JsonObject? Parse(string line)
	{
		try
		{
			return JsonNode.Parse(line, null) as JsonObject;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static DateTimeOffset? Timestamp(JsonNode? node)
	{
		if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value) || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
		{
			return null;
		}
		return result;
	}
}

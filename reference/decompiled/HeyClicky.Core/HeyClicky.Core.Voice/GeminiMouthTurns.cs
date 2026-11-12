using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HeyClicky.Core.Voice;

public sealed class GeminiMouthTurns
{
	private sealed class Turn
	{
		public bool Discarded;

		public bool GenerationComplete;
	}

	private readonly object gate = new object();

	private readonly List<Turn> turns = new List<Turn>();

	public static readonly byte[] LineEndMessage = "{\"serverContent\":{\"generationComplete\":true,\"turnComplete\":true}}"u8.ToArray();

	public int OpenTurns
	{
		get
		{
			lock (gate)
			{
				return turns.Count;
			}
		}
	}

	public void Reset()
	{
		lock (gate)
		{
			turns.Clear();
		}
	}

	public void LineSent()
	{
		lock (gate)
		{
			turns.Add(new Turn());
		}
	}

	public bool CancelLineInProgress()
	{
		lock (gate)
		{
			Turn turn = turns.FirstOrDefault((Turn t) => !t.Discarded && !t.GenerationComplete);
			if (turn == null)
			{
				return false;
			}
			turn.Discarded = true;
			return true;
		}
	}

	public List<GeminiMouthEvent> Events(ReadOnlySpan<byte> json)
	{
		List<GeminiMouthEvent> list = new List<GeminiMouthEvent>();
		using JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
		JsonElement rootElement = jsonDocument.RootElement;
		if (rootElement.ValueKind != JsonValueKind.Object)
		{
			return list;
		}
		if (rootElement.TryGetProperty("setupComplete", out var value))
		{
			list.Add(new GeminiMouthEvent(GeminiMouthEventKind.SetupComplete));
		}
		if (rootElement.TryGetProperty("goAway", out value))
		{
			list.Add(new GeminiMouthEvent(GeminiMouthEventKind.GoAway));
		}
		if (rootElement.TryGetProperty("serverContent", out var value2) && value2.ValueKind == JsonValueKind.Object)
		{
			lock (gate)
			{
				ServerContentEvents(value2, list);
			}
		}
		if (rootElement.TryGetProperty("usageMetadata", out var value3) && value3.ValueKind == JsonValueKind.Object)
		{
			list.Add(new GeminiMouthEvent(GeminiMouthEventKind.Usage, null, value3.GetRawText()));
		}
		return list;
	}

	private void ServerContentEvents(JsonElement content, List<GeminiMouthEvent> events)
	{
		if (turns.Count == 0)
		{
			return;
		}
		Turn turn = turns[0];
		if (content.TryGetProperty("interrupted", out var value) && value.ValueKind == JsonValueKind.True)
		{
			if (!turn.Discarded && !turn.GenerationComplete)
			{
				events.Add(new GeminiMouthEvent(GeminiMouthEventKind.LineDone));
				turn.GenerationComplete = true;
			}
			turn.Discarded = true;
		}
		if (!turn.Discarded && content.TryGetProperty("modelTurn", out var value2) && value2.TryGetProperty("parts", out var value3) && value3.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in value3.EnumerateArray())
			{
				if (item.TryGetProperty("inlineData", out var value4) && value4.TryGetProperty("data", out var value5) && value5.ValueKind == JsonValueKind.String)
				{
					string @string = value5.GetString();
					if (@string != null && @string.Length > 0)
					{
						events.Add(new GeminiMouthEvent(GeminiMouthEventKind.Audio, @string));
					}
				}
			}
		}
		if (content.TryGetProperty("generationComplete", out var value6) && value6.ValueKind == JsonValueKind.True)
		{
			if (!turn.Discarded && !turn.GenerationComplete)
			{
				events.Add(new GeminiMouthEvent(GeminiMouthEventKind.LineDone));
			}
			turn.GenerationComplete = true;
		}
		if (content.TryGetProperty("turnComplete", out var value7) && value7.ValueKind == JsonValueKind.True)
		{
			if (!turn.Discarded && !turn.GenerationComplete)
			{
				events.Add(new GeminiMouthEvent(GeminiMouthEventKind.LineDone));
			}
			turns.RemoveAt(0);
		}
	}

	public static byte[] SpeakRequestBody(string voice, string line, IReadOnlyList<string> languages)
	{
		ArrayBufferWriter<byte> arrayBufferWriter = new ArrayBufferWriter<byte>(line.Length * 2 + 128);
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(arrayBufferWriter))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WriteString("voice", voice);
			utf8JsonWriter.WriteString("text", line);
			if (languages.Count > 0)
			{
				utf8JsonWriter.WriteStartArray("languages");
				foreach (string language in languages)
				{
					utf8JsonWriter.WriteStringValue(language);
				}
				utf8JsonWriter.WriteEndArray();
			}
			utf8JsonWriter.WriteEndObject();
		}
		return arrayBufferWriter.WrittenSpan.ToArray();
	}

	public static bool IsLineEnd(ReadOnlySpan<byte> json)
	{
		if (json.IndexOf("\"turnComplete\""u8) >= 0)
		{
			return json.IndexOf("\"modelTurn\""u8) < 0;
		}
		return false;
	}

	public static bool CarriesAudio(ReadOnlySpan<byte> json)
	{
		return json.IndexOf("\"inlineData\""u8) >= 0;
	}

	public static string? ErrorMessage(ReadOnlySpan<byte> json)
	{
		if (json.IndexOf("\"error\""u8) < 0)
		{
			return null;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
			JsonElement value;
			JsonElement value2;
			return (!jsonDocument.RootElement.TryGetProperty("error", out value)) ? null : (((value.ValueKind == JsonValueKind.Object && value.TryGetProperty("message", out value2)) ? value2.GetString() : null) ?? "the Worker couldn't speak the line");
		}
		catch (JsonException)
		{
			return null;
		}
	}
}

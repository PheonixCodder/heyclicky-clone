using System;
using System.Buffers;
using System.Text.Json;

namespace HeyClicky.Core.Brain;

public sealed class BrainSseParser : IDisposable
{
	private byte[] lineBuffer = ArrayPool<byte>.Shared.Rent(16384);

	private int lineLength;

	private char[] textBuffer = ArrayPool<char>.Shared.Rent(4096);

	public void Feed(ReadOnlySpan<byte> chunk, IBrainStreamSink sink)
	{
		while (!chunk.IsEmpty)
		{
			int num = chunk.IndexOf<byte>(10);
			if (num < 0)
			{
				Append(chunk);
				break;
			}
			if (lineLength == 0)
			{
				DispatchLine(chunk.Slice(0, num), sink);
			}
			else
			{
				Append(chunk.Slice(0, num));
				DispatchLine(lineBuffer.AsSpan(0, lineLength), sink);
				lineLength = 0;
			}
			int num2 = num + 1;
			chunk = chunk.Slice(num2, chunk.Length - num2);
		}
	}

	public void Complete(IBrainStreamSink sink)
	{
		if (lineLength != 0)
		{
			DispatchLine(lineBuffer.AsSpan(0, lineLength), sink);
			lineLength = 0;
		}
	}

	private void Append(ReadOnlySpan<byte> bytes)
	{
		if (lineLength + bytes.Length > lineBuffer.Length)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(Math.Max(lineBuffer.Length * 2, lineLength + bytes.Length));
			lineBuffer.AsSpan(0, lineLength).CopyTo(array);
			ArrayPool<byte>.Shared.Return(lineBuffer);
			lineBuffer = array;
		}
		bytes.CopyTo(lineBuffer.AsSpan(lineLength));
		lineLength += bytes.Length;
	}

	private void DispatchLine(ReadOnlySpan<byte> line, IBrainStreamSink sink)
	{
		if (!line.IsEmpty)
		{
			if (line[line.Length - 1] == 13)
			{
				line = line.Slice(0, line.Length - 1);
			}
		}
		if (!line.StartsWith("data:"u8))
		{
			return;
		}
		ReadOnlySpan<byte> json = line.Slice(5, line.Length - 5).TrimStart<byte>(32);
		if (json.IsEmpty || TryDispatchText(json, sink))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind == JsonValueKind.Object && rootElement.TryGetProperty("type", out var value) && value.ValueKind == JsonValueKind.String)
			{
				sink.OnEvent(value.GetString(), rootElement);
			}
		}
		catch (JsonException ex)
		{
			Log.Warn("Brain", "unreadable stream event skipped: " + ex.Message);
		}
	}

	private bool TryDispatchText(ReadOnlySpan<byte> json, IBrainStreamSink sink)
	{
		if (!json.StartsWith("{\"type\":\"text\""u8))
		{
			return false;
		}
		Utf8JsonReader utf8JsonReader = new Utf8JsonReader(json);
		try
		{
			if (!utf8JsonReader.Read() || utf8JsonReader.TokenType != JsonTokenType.StartObject)
			{
				return false;
			}
			bool flag = false;
			while (utf8JsonReader.Read() && utf8JsonReader.TokenType == JsonTokenType.PropertyName)
			{
				bool flag2 = utf8JsonReader.ValueTextEquals("text"u8);
				if (!utf8JsonReader.Read())
				{
					return false;
				}
				if (flag2 && utf8JsonReader.TokenType == JsonTokenType.String)
				{
					int num = (int)(utf8JsonReader.HasValueSequence ? utf8JsonReader.ValueSequence.Length : utf8JsonReader.ValueSpan.Length);
					if (num > textBuffer.Length)
					{
						ArrayPool<char>.Shared.Return(textBuffer);
						textBuffer = ArrayPool<char>.Shared.Rent(num);
					}
					int length = utf8JsonReader.CopyString(textBuffer);
					sink.OnText(textBuffer.AsSpan(0, length));
					flag = true;
				}
				else
				{
					utf8JsonReader.Skip();
				}
			}
			if (!flag)
			{
				sink.OnText(ReadOnlySpan<char>.Empty);
			}
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	public void Dispose()
	{
		if (lineBuffer.Length != 0)
		{
			ArrayPool<byte>.Shared.Return(lineBuffer);
		}
		if (textBuffer.Length != 0)
		{
			ArrayPool<char>.Shared.Return(textBuffer);
		}
		lineBuffer = Array.Empty<byte>();
		textBuffer = Array.Empty<char>();
	}
}

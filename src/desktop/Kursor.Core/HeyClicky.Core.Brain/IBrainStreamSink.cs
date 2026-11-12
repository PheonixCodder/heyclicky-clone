using System;
using System.Text.Json;

namespace HeyClicky.Core.Brain;

public interface IBrainStreamSink
{
	void OnText(ReadOnlySpan<char> text);

	void OnEvent(string type, JsonElement root);
}

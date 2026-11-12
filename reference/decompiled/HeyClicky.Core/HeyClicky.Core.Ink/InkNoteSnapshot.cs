using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HeyClicky.Core.Ink;

public sealed class InkNoteSnapshot
{
	[JsonPropertyName("version")]
	public int Version { get; set; } = 1;

	[JsonPropertyName("versions")]
	public List<InkNoteVersionFile>? Versions { get; set; }
}

using System.Text.Json.Nodes;

namespace HeyClicky.Core.Brain;

public sealed record BrainCompactResult(JsonNode? NoteItem, int KeepFromIndex, int KeepLeadingItems, int DropLeadingItems);

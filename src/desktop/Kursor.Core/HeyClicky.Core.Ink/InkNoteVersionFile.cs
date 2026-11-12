using System.Text.Json.Serialization;

namespace HeyClicky.Core.Ink;

public sealed class InkNoteVersionFile
{
	[JsonPropertyName("at")]
	public double At { get; set; }

	[JsonPropertyName("text")]
	public string Text { get; set; } = "";

	[JsonPropertyName("isEdit")]
	public bool IsEdit { get; set; }

	[JsonPropertyName("isPartial")]
	public bool? IsPartial { get; set; }

	[JsonPropertyName("startsNewEdit")]
	public bool? StartsNewEdit { get; set; }

	public ClickyInkNoteVersion ToVersion()
	{
		return new ClickyInkNoteVersion(InkFiles.FromSecondsSince1970(At), Text, IsEdit, IsPartial, StartsNewEdit);
	}

	public static InkNoteVersionFile From(ClickyInkNoteVersion version)
	{
		return new InkNoteVersionFile
		{
			At = InkFiles.SecondsSince1970(version.At),
			Text = version.Text,
			IsEdit = version.IsEdit,
			IsPartial = version.IsPartial,
			StartsNewEdit = version.StartsNewEdit
		};
	}
}

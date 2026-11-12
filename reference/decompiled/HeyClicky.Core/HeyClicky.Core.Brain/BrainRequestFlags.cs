using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HeyClicky.Core.Brain;

public sealed record BrainRequestFlags(bool JobsWorld, bool SavesDrawings, bool Drive)
{
	public bool FiguresFloat { get; init; }

	public static readonly BrainRequestFlags None = new BrainRequestFlags(JobsWorld: false, SavesDrawings: false, Drive: false);

	public void Write(Utf8JsonWriter writer)
	{
		if (FiguresFloat)
		{
			writer.WriteBoolean("figures_float", value: true);
		}
		if (JobsWorld)
		{
			writer.WriteBoolean("library_items", value: true);
			writer.WriteBoolean("routines_tool", value: true);
			writer.WriteBoolean("find_jobs_tool", value: true);
			writer.WriteBoolean("job_handoffs", value: true);
		}
		if (SavesDrawings)
		{
			writer.WriteBoolean("saves_drawings", value: true);
		}
		if (Drive)
		{
			writer.WriteBoolean("drive", value: true);
		}
	}

	[CompilerGenerated]
	private BrainRequestFlags(BrainRequestFlags original)
	{
		JobsWorld = original.JobsWorld;
		SavesDrawings = original.SavesDrawings;
		Drive = original.Drive;
		FiguresFloat = original.FiguresFloat;
	}
}

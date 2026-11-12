using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeyClicky.Core.Voice;
using HeyClicky.Drawing;

namespace HeyClicky.App.Home;

public sealed class StoredLesson
{
	public sealed class LineData
	{
		public string Text { get; set; } = "";

		public double Start { get; set; }

		public double? Length { get; set; }
	}

	public sealed class Meta
	{
		public string Id { get; set; } = "";

		public string Title { get; set; } = "";

		public DateTimeOffset CreatedAt { get; set; }

		public string Question { get; set; } = "";

		public List<LineData> Lines { get; set; } = new List<LineData>();

		public double Duration { get; set; }

		public int CanvasWidth { get; set; }

		public int CanvasHeight { get; set; }

		public int DisplayIndex { get; set; }

		public string DisplayDevice { get; set; } = "";

		public double[] DisplayBounds { get; set; } = Array.Empty<double>();

		public double[] DisplayWork { get; set; } = Array.Empty<double>();

		public double DisplayScale { get; set; } = 1.0;

		public bool DisplayIsPrimary { get; set; }

		public string CursorColorHex { get; set; } = "#3380FF";

		public bool HasNarration { get; set; }
	}

	public required string Folder { get; init; }

	public required Meta Data { get; init; }

	public string ProgramPath => Path.Combine(Folder, "program.js");

	public string ScreenPath => Path.Combine(Folder, "screen.jpg");

	public string PosterPath => Path.Combine(Folder, "poster.png");

	public string NarrationPath => Path.Combine(Folder, "narration.wav");

	public string PhotosFolder => Path.Combine(Folder, "photos");

	public double ReplayShift
	{
		get
		{
			if (Data.Lines.Count <= 0)
			{
				return 0.0;
			}
			return Math.Max(0.0, Data.Lines[0].Start - 0.6);
		}
	}

	public (byte[] Pcm16Mono, int SampleRate)? ReadNarration()
	{
		if (!Data.HasNarration)
		{
			return null;
		}
		try
		{
			return ((byte[] Pcm16Mono, int SampleRate)?)(File.Exists(NarrationPath) ? ((ValueType)LessonNarration.FromWav(File.ReadAllBytes(NarrationPath))) : ((ValueType)((byte[], int)?)null));
		}
		catch (Exception ex)
		{
			Log.Warn("lessons", "narration read: " + ex.Message);
			return null;
		}
	}

	public LessonPlaybackScript? PlaybackScript()
	{
		try
		{
			double shift = ReplayShift;
			double num = ((Data.DisplayScale > 0.0) ? Data.DisplayScale : 1.0);
			double pointWidth = ((Data.DisplayBounds.Length == 4 && Data.DisplayBounds[2] > 0.0) ? (Data.DisplayBounds[2] / num) : ((double)Data.CanvasWidth / num));
			return new LessonPlaybackScript(File.ReadAllText(ProgramPath), File.ReadAllBytes(ScreenPath), Data.CanvasWidth, Data.CanvasHeight, pointWidth, num, Data.Lines.Select((LineData l) => (l.Start - shift, Length: l.Length)).ToList(), Math.Max(1.0, Data.Duration - shift), Data.CursorColorHex);
		}
		catch (Exception ex)
		{
			Log.Warn("lessons", "playback read: " + ex.Message);
			return null;
		}
	}
}

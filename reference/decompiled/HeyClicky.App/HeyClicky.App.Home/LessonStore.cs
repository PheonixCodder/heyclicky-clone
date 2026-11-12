using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HeyClicky.Core.Voice;
using HeyClicky.Drawing.Media;
using Microsoft.VisualBasic.FileIO;

namespace HeyClicky.App.Home;

public sealed class LessonStore
{
	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public static string Root => AppPaths.Combine("Lessons");

	public event Action? Changed;

	public IReadOnlyList<StoredLesson> List()
	{
		try
		{
			if (!Directory.Exists(Root))
			{
				return Array.Empty<StoredLesson>();
			}
			return (from StoredLesson l in from l in Directory.EnumerateDirectories(Root).Select(delegate(string folder)
					{
						try
						{
							StoredLesson.Meta meta = JsonSerializer.Deserialize<StoredLesson.Meta>(File.ReadAllText(Path.Combine(folder, "lesson.json")), Json);
							return (meta == null) ? null : new StoredLesson
							{
								Folder = folder,
								Data = meta
							};
						}
						catch
						{
							return (StoredLesson)null;
						}
					})
					where l != null
					select l
				orderby l.Data.CreatedAt descending
				select l).ToList();
		}
		catch (Exception ex)
		{
			Log.Warn("lessons", "list: " + ex.Message);
			return Array.Empty<StoredLesson>();
		}
	}

	public async Task<StoredLesson?> SaveAsync(DrawingRecord record, byte[]? poster, IReadOnlyDictionary<string, byte[]>? photos = null)
	{
		try
		{
			StoredLesson.Meta meta = new StoredLesson.Meta
			{
				Id = Guid.NewGuid().ToString("N").Substring(0, 12),
				Title = record.Title,
				CreatedAt = record.At,
				Question = record.Question,
				Lines = record.Lines.Select((DrawingRecord.Line l) => new StoredLesson.LineData
				{
					Text = l.Text,
					Start = l.Start,
					Length = l.Length
				}).ToList(),
				Duration = record.Duration,
				CanvasWidth = record.Screen.PixelWidth,
				CanvasHeight = record.Screen.PixelHeight,
				DisplayIndex = record.Screen.Display.Index,
				DisplayDevice = record.Screen.Display.DeviceName,
				DisplayBounds = new double[4]
				{
					record.Screen.Display.BoundsPx.X,
					record.Screen.Display.BoundsPx.Y,
					record.Screen.Display.BoundsPx.Width,
					record.Screen.Display.BoundsPx.Height
				},
				DisplayWork = new double[4]
				{
					record.Screen.Display.WorkAreaPx.X,
					record.Screen.Display.WorkAreaPx.Y,
					record.Screen.Display.WorkAreaPx.Width,
					record.Screen.Display.WorkAreaPx.Height
				},
				DisplayScale = record.Screen.Display.Scale,
				DisplayIsPrimary = record.Screen.Display.IsPrimary,
				CursorColorHex = record.CursorColorHex
			};
			byte[] narration = record.Narration;
			meta.HasNarration = narration != null && narration.Length > 0 && record.NarrationSampleRate > 0;
			StoredLesson.Meta meta2 = meta;
			string folder = Path.Combine(Root, $"{record.At.ToLocalTime():yyyy-MM-dd HHmmss} {meta2.Id}");
			await Task.Run(delegate
			{
				Directory.CreateDirectory(folder);
				File.WriteAllText(Path.Combine(folder, "program.js"), record.Program);
				File.WriteAllBytes(Path.Combine(folder, "screen.jpg"), record.Screen.Jpeg);
				if (poster != null)
				{
					File.WriteAllBytes(Path.Combine(folder, "poster.png"), poster);
				}
				if (meta2.HasNarration)
				{
					File.WriteAllBytes(Path.Combine(folder, "narration.wav"), LessonNarration.ToWav(record.Narration, record.NarrationSampleRate));
				}
				if (photos != null && photos.Count > 0)
				{
					string text = Path.Combine(folder, "photos");
					Directory.CreateDirectory(text);
					foreach (var (query, bytes) in photos)
					{
						File.WriteAllBytes(Path.Combine(text, PhotoLoader.SavedPhotoFileName(query)), bytes);
					}
				}
				File.WriteAllText(Path.Combine(folder, "lesson.json"), JsonSerializer.Serialize(meta2, Json));
			});
			Log.Info("lessons", $"saved \"{meta2.Title}\" ({meta2.Lines.Count} lines, {(meta2.HasNarration ? $"voice {(double)record.Narration.Length / 2.0 / (double)record.NarrationSampleRate:0.0} s" : "no voice")}, {photos?.Count ?? 0} photos) to {folder}");
			this.Changed?.Invoke();
			return new StoredLesson
			{
				Folder = folder,
				Data = meta2
			};
		}
		catch (Exception ex)
		{
			Log.Warn("lessons", "save failed: " + ex.Message);
			return null;
		}
	}

	public void Delete(StoredLesson lesson)
	{
		try
		{
			FileSystem.DeleteDirectory(lesson.Folder, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
			Log.Info("lessons", "deleted \"" + lesson.Data.Title + "\"");
		}
		catch (Exception ex)
		{
			Log.Warn("lessons", "delete: " + ex.Message);
		}
		this.Changed?.Invoke();
	}
}

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace HeyClicky;

public static class Drawing3DAvailability
{
	public const string UnavailableNote = "3D can't be drawn on this PC: never call figure() or scene3d(). Show a thing with a shape as a 2D drawing or its photo instead.";

	private static readonly object Gate = new object();

	private static bool? knownUnavailable;

	private static string? markerPathOverride;

	private static string MarkerPath => markerPathOverride ?? AppPaths.Combine("3d-unavailable-build.txt");

	public static string CurrentBuild { get; } = (Assembly.GetEntryAssembly() ?? typeof(Drawing3DAvailability).Assembly).GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

	public static bool IsKnownUnavailable
	{
		get
		{
			lock (Gate)
			{
				bool? flag = knownUnavailable;
				if (flag.HasValue)
				{
					return flag == true;
				}
				try
				{
					knownUnavailable = File.Exists(MarkerPath) && File.ReadAllText(MarkerPath).Trim() == CurrentBuild;
				}
				catch (Exception)
				{
					knownUnavailable = false;
				}
				return knownUnavailable.Value;
			}
		}
	}

	public static string? Note
	{
		get
		{
			if (!IsKnownUnavailable)
			{
				return null;
			}
			return "3D can't be drawn on this PC: never call figure() or scene3d(). Show a thing with a shape as a 2D drawing or its photo instead.";
		}
	}

	public static void MarkUnavailable(string reason)
	{
		lock (Gate)
		{
			if (knownUnavailable == true)
			{
				return;
			}
			knownUnavailable = true;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath));
				File.WriteAllText(MarkerPath, CurrentBuild);
			}
			catch (Exception ex)
			{
				Log.Warn("3D", "could not remember that 3D is unavailable: " + ex.Message);
			}
		}
		Log.Warn("3D", $"3D is unavailable on this PC in build {CurrentBuild}: turns draw in 2D ({reason})");
	}

	public static void WriteFlags(Utf8JsonWriter writer)
	{
		if (IsKnownUnavailable)
		{
			writer.WriteBoolean("figure3d", value: false);
			writer.WriteBoolean("no_3d", value: true);
		}
		else
		{
			writer.WriteBoolean("figure3d", value: true);
		}
	}

	public static void UseMarkerPathForTests(string? markerPath)
	{
		lock (Gate)
		{
			markerPathOverride = markerPath;
			knownUnavailable = null;
		}
	}
}

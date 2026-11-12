using System;
using System.IO;
using System.Text.Json.Nodes;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.App.Notch.Peek;

public static class NotchChatSize
{
	public const double MinimumBodyHeight = 260.0;

	private static readonly string FilePath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "notch-chat-size.json");

	private static double widthDelta;

	private static double heightDelta;

	private static bool loaded;

	private static double maximumBodyHeight = 900.0;

	public static double MinimumContentWidth => 500.0;

	public static double MaximumContentWidth => 784.0;

	public static bool IsResizing { get; private set; }

	public static double ContentWidth
	{
		get
		{
			EnsureLoaded();
			return ClampWidth(500.0 + widthDelta);
		}
	}

	public static double BodyHeight
	{
		get
		{
			EnsureLoaded();
			return ClampHeight(NotchPeekLayout.BodyHeight + heightDelta);
		}
	}

	public static bool IsDefaultSize
	{
		get
		{
			EnsureLoaded();
			if (widthDelta == 0.0)
			{
				return heightDelta == 0.0;
			}
			return false;
		}
	}

	public static event Action? Changed;

	private static void EnsureLoaded()
	{
		if (loaded)
		{
			return;
		}
		loaded = true;
		try
		{
			if (File.Exists(FilePath))
			{
				JsonNode? jsonNode = JsonNode.Parse(File.ReadAllText(FilePath), null);
				widthDelta = (jsonNode?["width"]?.GetValue<double>()).GetValueOrDefault();
				heightDelta = (jsonNode?["height"]?.GetValue<double>()).GetValueOrDefault();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("notch-chat", "chat size unreadable: " + ex.Message);
		}
	}

	public static void NoteScreenHeight(double screenHeightDip)
	{
		double num = Math.Min(900.0, Math.Max(120.0, screenHeightDip - 80.0));
		maximumBodyHeight = Math.Max(260.0, num - 30.0 - 8.0 - 12.0);
	}

	public static void BeginResizing()
	{
		IsResizing = true;
		NotchChatSize.Changed?.Invoke();
	}

	public static void Resize(double contentWidth, double bodyHeight)
	{
		EnsureLoaded();
		double num = Math.Round(ClampWidth(contentWidth) - 500.0);
		double num2 = Math.Round(ClampHeight(bodyHeight) - NotchPeekLayout.BodyHeight);
		if (num != widthDelta || num2 != heightDelta)
		{
			widthDelta = num;
			heightDelta = num2;
			NotchChatSize.Changed?.Invoke();
		}
	}

	public static void EndResizing()
	{
		IsResizing = false;
		Save();
		NotchChatSize.Changed?.Invoke();
		ClickyAnalytics.TrackProductEvent("notch_peek_action", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["action"] = "chat_resized",
			["width"] = (int)ContentWidth,
			["height"] = (int)BodyHeight
		});
	}

	public static void ResetToDefault()
	{
		widthDelta = 0.0;
		heightDelta = 0.0;
		loaded = true;
		Save();
		NotchChatSize.Changed?.Invoke();
	}

	private static void Save()
	{
		try
		{
			if (widthDelta == 0.0 && heightDelta == 0.0)
			{
				if (File.Exists(FilePath))
				{
					File.Delete(FilePath);
				}
			}
			else
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
				File.WriteAllText(FilePath, new JsonObject((JsonNodeOptions?)null)
				{
					["width"] = widthDelta,
					["height"] = heightDelta
				}.ToJsonString());
			}
		}
		catch (Exception ex)
		{
			Log.Warn("notch-chat", "chat size not saved: " + ex.Message);
		}
	}

	private static double ClampWidth(double width)
	{
		return Math.Min(Math.Max(width, MinimumContentWidth), MaximumContentWidth);
	}

	private static double ClampHeight(double height)
	{
		return Math.Min(Math.Max(height, 260.0), maximumBodyHeight);
	}
}

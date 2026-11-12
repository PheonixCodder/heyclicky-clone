using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HeyClicky.Core.Drive;

public static class ClickyDriveActions
{
	public static readonly IReadOnlySet<string> ObservationActionTypes = new HashSet<string> { "screenshot", "zoom", "cursor_position" };

	public static string TypeOf(JsonObject action)
	{
		return (((string?)action["type"]) ?? "").ToLowerInvariant();
	}

	public static bool IsObservationOnly(IEnumerable<JsonObject> actions)
	{
		return actions.All((JsonObject a) => ObservationActionTypes.Contains(TypeOf(a)));
	}

	public static string NormalizedKeyName(string key)
	{
		string text = key.ToLowerInvariant();
		switch (text)
		{
		case "meta":
		case "super":
		case "win":
		case "windows":
			return "win";
		case "cmd":
		case "command":
			return "cmd";
		case "ctrl":
		case "control":
			return "ctrl";
		case "alt":
		case "opt":
		case "option":
			return "opt";
		case "enter":
		case "return":
			return "return";
		case "esc":
		case "escape":
			return "escape";
		case "arrowup":
		case "up":
			return "up";
		case "down":
		case "arrowdown":
			return "down";
		case "left":
		case "arrowleft":
			return "left";
		case "right":
		case "arrowright":
			return "right";
		case "backspace":
			return "delete";
		case "del":
		case "delete":
			return "forwarddelete";
		case "pageup":
			return "pageup";
		case "pagedown":
			return "pagedown";
		case "space":
		case "spacebar":
		case " ":
			return "space";
		default:
			return text;
		}
	}

	public static List<string> Keys(JsonObject action)
	{
		return (from k in (action["keys"] as JsonArray)?.Select((JsonNode k) => (k == null || k.GetValueKind() != JsonValueKind.String) ? null : ((string?)k))
			where k != null
			select NormalizedKeyName(k)).ToList() ?? new List<string>();
	}

	public static double? Number(JsonObject action, string key)
	{
		if (!(action[key] is JsonValue jsonValue) || !jsonValue.TryGetValue<double>(out var value))
		{
			return null;
		}
		return value;
	}

	public static int ScrollLines(double scrollY)
	{
		if (scrollY != 0.0)
		{
			if (!(scrollY > 0.0))
			{
				return Math.Min(-1, (int)(scrollY / 50.0));
			}
			return Math.Max(1, (int)(scrollY / 50.0));
		}
		return 0;
	}

	public static string Describe(JsonObject action)
	{
		IOrderedEnumerable<string> values = action.Where<KeyValuePair<string, JsonNode>>((KeyValuePair<string, JsonNode> field) => field.Key != "type" && field.Value != null).Select(delegate(KeyValuePair<string, JsonNode> field)
		{
			string text = ((field.Value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value)) ? value : field.Value.ToJsonString());
			text = text.Replace("\n", " ");
			return field.Key + "=" + ((text.Length > 60) ? text.Substring(0, 60) : text);
		}).OrderBy<string, string>((string f) => f, StringComparer.Ordinal);
		return (((string?)action["type"]) ?? "?") + " " + string.Join(" ", values);
	}

	public static string ClickLogLine(string type, JsonObject action)
	{
		return $"{type.Replace("_", " ")} at {(int)Number(action, "x").GetValueOrDefault()},{(int)Number(action, "y").GetValueOrDefault()}";
	}

	public static string TypedLogLine(string text)
	{
		return "typed \"" + ((text.Length > 40) ? text.Substring(0, 40) : text) + "\"";
	}

	public static PointD ScreenPoint(double canvasX, double canvasY, ScreenCaptureFrame frame)
	{
		RectD boundsPx = frame.Display.BoundsPx;
		double num = boundsPx.Width / (double)Math.Max(1, frame.PixelWidth);
		double num2 = boundsPx.Height / (double)Math.Max(1, frame.PixelHeight);
		return new PointD(boundsPx.X + canvasX * num, boundsPx.Y + canvasY * num2);
	}

	public static PointD CanvasPoint(PointD screen, ScreenCaptureFrame frame)
	{
		RectD boundsPx = frame.Display.BoundsPx;
		double num = (double)frame.PixelWidth / Math.Max(1.0, boundsPx.Width);
		double num2 = (double)frame.PixelHeight / Math.Max(1.0, boundsPx.Height);
		return new PointD((screen.X - boundsPx.X) * num, (screen.Y - boundsPx.Y) * num2);
	}

	public static string CursorPositionText(PointD pointer, ScreenCaptureFrame frame)
	{
		PointD pointD = CanvasPoint(pointer, frame);
		bool flag = pointD.X >= 0.0 && pointD.Y >= 0.0 && pointD.X <= (double)frame.PixelWidth && pointD.Y <= (double)frame.PixelHeight;
		return $"X={(int)Math.Round(pointD.X, MidpointRounding.AwayFromZero)}, Y={(int)Math.Round(pointD.Y, MidpointRounding.AwayFromZero)}" + (flag ? "" : " (off this screen)");
	}

	public static RectD? ZoomRegionPx(JsonObject action, ScreenCaptureFrame frame)
	{
		double value;
		List<double> list = (action["region"] as JsonArray)?.Select((JsonNode n) => (!(n is JsonValue jsonValue) || !jsonValue.TryGetValue<double>(out value)) ? double.NaN : value).ToList();
		if (list == null || list.Count != 4 || list.Any(double.IsNaN))
		{
			return null;
		}
		double num = frame.Display.BoundsPx.Width / (double)Math.Max(1, frame.PixelWidth);
		double num2 = frame.Display.BoundsPx.Height / (double)Math.Max(1, frame.PixelHeight);
		return new RectD(Math.Min(list[0], list[2]) * num, Math.Min(list[1], list[3]) * num2, Math.Abs(list[2] - list[0]) * num, Math.Abs(list[3] - list[1]) * num2);
	}
}

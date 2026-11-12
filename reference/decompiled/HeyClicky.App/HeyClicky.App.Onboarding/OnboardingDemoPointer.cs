using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core;
using HeyClicky.Core.Net;

namespace HeyClicky.App.Onboarding;

internal static class OnboardingDemoPointer
{
	private const string UserPrompt = "look around my screen and find something interesting to point at";

	private static int running;

	private static readonly Regex PointTag = new Regex("\\[POINT:(?:none|(\\d+(?:\\.\\d+)?)\\s*,\\s*(\\d+(?:\\.\\d+)?)(?::([^\\]:\\s][^\\]:]*?))?(?::screen(\\d+))?)\\]\\s*$", RegexOptions.CultureInvariant);

	public static async Task RunAsync(CoreServices core, IScreenCaptureService capture, ICursorOverlay cursor, Func<VoiceState> voiceState)
	{
		VoiceState voiceState2 = voiceState();
		if ((voiceState2 != 0 && voiceState2 != VoiceState.Responding) || 1 == 0)
		{
			return;
		}
		if (Interlocked.Exchange(ref running, 1) == 1)
		{
			Log.Info("video", "pointing demo: one is already in flight");
			return;
		}
		try
		{
			string token = await core.Auth.GetValidAccessTokenAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (string.IsNullOrEmpty(token))
			{
				Log.Info("video", "pointing demo skipped: not signed in");
				return;
			}
			IReadOnlyList<ScreenCaptureFrame> readOnlyList = await capture.CaptureAllAsync().ConfigureAwait(continueOnCapturedContext: false);
			int num = readOnlyList.ToList().FindIndex((ScreenCaptureFrame f) => f.ContainsCursor);
			if (num < 0)
			{
				Log.Info("video", "pointing demo: no screen under the pointer");
				return;
			}
			ScreenCaptureFrame frame = readOnlyList[num];
			string imageLabel = ((readOnlyList.Count == 1) ? "user's screen (cursor is here)" : $"screen {num + 1} of {readOnlyList.Count} — cursor is on this screen (primary focus)") + $" (image dimensions: {frame.PixelWidth}x{frame.PixelHeight} pixels)";
			string text = await AskAsync(core.Http, token, frame.Jpeg, imageLabel).ConfigureAwait(continueOnCapturedContext: false);
			(string, double, double, string)? tuple = Parse(text);
			if (tuple.HasValue)
			{
				(string, double, double, string) valueOrDefault = tuple.GetValueOrDefault();
				RectD boundsPx = frame.Display.BoundsPx;
				double num2 = Math.Clamp(valueOrDefault.Item2, 0.0, frame.PixelWidth);
				double num3 = Math.Clamp(valueOrDefault.Item3, 0.0, frame.PixelHeight);
				PointD screenPx = new PointD(boundsPx.X + num2 * boundsPx.Width / (double)Math.Max(1, frame.PixelWidth), boundsPx.Y + num3 * boundsPx.Height / (double)Math.Max(1, frame.PixelHeight));
				Log.Info("video", $"pointing demo: at \"{valueOrDefault.Item4 ?? "element"}\" with \"{valueOrDefault.Item1}\"");
				await cursor.PointAtAsync(screenPx, valueOrDefault.Item1).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				Log.Info("video", "pointing demo: nothing to point at (\"" + text.Trim() + "\")");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("video", "pointing demo: " + ex.Message);
		}
		finally
		{
			Volatile.Write(ref running, 0);
		}
	}

	internal static (string Comment, double X, double Y, string? Label)? Parse(string reply)
	{
		string text = reply.Trim();
		Match match = PointTag.Match(text);
		if (!match.Success || !match.Groups[1].Success)
		{
			return null;
		}
		double item = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
		double item2 = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
		return (text.Substring(0, match.Index).Trim(), item, item2, match.Groups[3].Success ? match.Groups[3].Value.Trim() : null);
	}

	internal static async Task<string> AskAsync(ClickyHttp http, string token, byte[] jpeg, string imageLabel)
	{
		JsonObject obj = new JsonObject((JsonNodeOptions?)null)
		{
			["prompt_profile"] = "onboarding_demo",
			["response_model_selection"] = "standard",
			["user_prompt"] = "look around my screen and find something interesting to point at"
		};
		JsonNode reference = new JsonObject((JsonNodeOptions?)null)
		{
			["base64_image_data"] = Convert.ToBase64String(jpeg),
			["image_label"] = imageLabel,
			["image_media_type"] = "image/jpeg"
		};
		obj["images"] = new JsonArray(new ReadOnlySpan<JsonNode>(in reference));
		JsonObject jsonObject = obj;
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, http.WorkerUrl("/chat"))
		{
			Content = new StringContent(jsonObject.ToJsonString(), Encoding.UTF8, "application/json")
		};
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
		using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30L));
		using HttpResponseMessage response = await http.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		if (!response.IsSuccessStatusCode)
		{
			throw new HttpRequestException($"/chat answered {response.StatusCode}");
		}
		using StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false));
		StringBuilder text = new StringBuilder();
		while (true)
		{
			string text2 = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			if (text2 == null)
			{
				break;
			}
			if (!text2.StartsWith("data: ", StringComparison.Ordinal))
			{
				continue;
			}
			string text3 = text2;
			string text4 = text3.Substring(6, text3.Length - 6);
			if (text4 == "[DONE]")
			{
				break;
			}
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(text4);
				JsonElement rootElement = jsonDocument.RootElement;
				JsonElement value4;
				JsonElement value5;
				JsonElement value6;
				if (rootElement.TryGetProperty("type", out var value) && value.ValueKind == JsonValueKind.String && value.GetString() == "content_block_delta" && rootElement.TryGetProperty("delta", out var value2) && value2.TryGetProperty("text", out var value3) && value3.ValueKind == JsonValueKind.String)
				{
					text.Append(value3.GetString());
				}
				else if (rootElement.TryGetProperty("choices", out value4) && value4.ValueKind == JsonValueKind.Array && value4.GetArrayLength() > 0 && value4[0].TryGetProperty("delta", out value5) && value5.TryGetProperty("content", out value6) && value6.ValueKind == JsonValueKind.String)
				{
					text.Append(value6.GetString());
				}
			}
			catch (JsonException)
			{
			}
		}
		return text.ToString();
	}
}

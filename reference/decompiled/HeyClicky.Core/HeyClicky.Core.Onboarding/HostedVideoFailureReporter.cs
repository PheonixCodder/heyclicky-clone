using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.Core.Onboarding;

public sealed class HostedVideoFailureReporter
{
	private readonly HashSet<string> reportedSignatures = new HashSet<string>();

	private readonly object gate = new object();

	public void Reset()
	{
		lock (gate)
		{
			reportedSignatures.Clear();
		}
	}

	public bool Report(HostedVideoFailure failure)
	{
		_003C_003Ey__InlineArray7<object> buffer = default(_003C_003Ey__InlineArray7<object>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 0) = failure.VideoKind;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 1) = failure.Phase;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 2) = failure.Reason;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 3) = failure.AssetUrl;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 4) = failure.ErrorDomain;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 5) = failure.ErrorCode;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<object>, object>(ref buffer, 6) = failure.HttpStatus.GetValueOrDefault();
		string item = string.Join('|', global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray7<object>, object>(in buffer, 7));
		lock (gate)
		{
			if (!reportedSignatures.Add(item))
			{
				return false;
			}
		}
		JsonObject jsonObject = Properties(failure);
		Log.Warn("video", $"intro video failed: {failure.VideoKind} {failure.Phase} {failure.Reason}: {failure.ErrorMessage} ({failure.AssetUrl})");
		ClickyAnalytics.TrackHostedVideoFailure(jsonObject);
		ICrashReporter current = CrashReporter.Current;
		if (!current.IsEnabled)
		{
			return true;
		}
		int errorCode = failure.ErrorCode;
		if ((errorCode == -2147221231 || errorCode == -2147221164) ? true : false)
		{
			return true;
		}
		try
		{
			current.CaptureMessage($"Hosted video failed: {failure.VideoKind} {failure.Phase} {failure.Reason}", CrashReportLevel.Error, new global::_003C_003Ez__ReadOnlyArray<string>(new string[6]
			{
				"hosted_video",
				failure.VideoKind,
				failure.Phase,
				failure.Reason,
				failure.ErrorDomain,
				failure.ErrorCode.ToString()
			}), new Dictionary<string, string>
			{
				["video.kind"] = failure.VideoKind,
				["video.phase"] = failure.Phase,
				["video.failure_reason"] = failure.Reason
			}, "hosted_video", (JsonObject)jsonObject.DeepClone());
		}
		catch (Exception ex)
		{
			Log.Warn("video", "crash reporter capture failed: " + ex.Message);
		}
		return true;
	}

	public static JsonObject Properties(HostedVideoFailure failure)
	{
		JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
		{
			["video_kind"] = failure.VideoKind,
			["phase"] = failure.Phase,
			["reason"] = failure.Reason,
			["player_status"] = failure.PlayerStatus,
			["item_status"] = failure.ItemStatus,
			["time_control_status"] = failure.TimeControlStatus,
			["asset_url"] = failure.AssetUrl,
			["error_domain"] = failure.ErrorDomain,
			["error_code"] = failure.ErrorCode,
			["error_message"] = failure.ErrorMessage
		};
		int? httpStatus = failure.HttpStatus;
		if (httpStatus.HasValue)
		{
			int valueOrDefault = httpStatus.GetValueOrDefault();
			if (valueOrDefault != 0)
			{
				jsonObject["http_status"] = valueOrDefault;
			}
		}
		return jsonObject;
	}
}

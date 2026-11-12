using System;
using System.Globalization;

namespace HeyClicky.Drawing3D;

internal sealed class StudioLook
{
	public float KeyIntensity = 2.4f;

	public float RimIntensity = 0.55f;

	public float EnvironmentIntensity = 0.9f;

	public float Exposure = 1f;

	public float GroundShadowOpacity = 0.3f;

	public float ShadowSoftness = 1.6f;

	public float OcclusionRadius = 0.2f;

	public float OcclusionStrength = 2.2f;

	public bool AgX = true;

	public float LookPower = 1f;

	public float LookSaturation = 1f;

	public float RoomLevel = 0.85f;

	public int Debug;

	public bool DebugDepth;

	public static readonly StudioLook Current = Make();

	private static StudioLook Make()
	{
		StudioLook studioLook = new StudioLook();
		if (Environment.GetEnvironmentVariable("CANVAS_STUDIO_TONEMAP") == "neutral")
		{
			studioLook.AgX = false;
		}
		if (studioLook.AgX)
		{
			studioLook.Exposure = 1.7f;
			studioLook.LookPower = 1.3f;
			studioLook.LookSaturation = 1.365f;
			studioLook.RoomLevel = 0.3f;
		}
		studioLook.LookPower = Value("CANVAS_STUDIO_POWER") ?? studioLook.LookPower;
		studioLook.LookSaturation = Value("CANVAS_STUDIO_SAT") ?? studioLook.LookSaturation;
		studioLook.RoomLevel = Value("CANVAS_STUDIO_ROOM") ?? studioLook.RoomLevel;
		studioLook.KeyIntensity = Value("CANVAS_STUDIO_KEY") ?? studioLook.KeyIntensity;
		studioLook.RimIntensity = Value("CANVAS_STUDIO_RIM") ?? studioLook.RimIntensity;
		studioLook.EnvironmentIntensity = Value("CANVAS_STUDIO_ENV") ?? studioLook.EnvironmentIntensity;
		studioLook.Exposure = Value("CANVAS_STUDIO_EXPOSURE") ?? studioLook.Exposure;
		studioLook.GroundShadowOpacity = Value("CANVAS_STUDIO_GROUND") ?? studioLook.GroundShadowOpacity;
		studioLook.ShadowSoftness = Value("CANVAS_STUDIO_SOFT") ?? studioLook.ShadowSoftness;
		studioLook.OcclusionRadius = Value("CANVAS_STUDIO_AO_RADIUS") ?? studioLook.OcclusionRadius;
		studioLook.OcclusionStrength = Value("CANVAS_STUDIO_AO") ?? studioLook.OcclusionStrength;
		string environmentVariable = Environment.GetEnvironmentVariable("CANVAS_STUDIO_DEBUG");
		StudioLook studioLook2 = studioLook;
		int debug;
		bool flag;
		switch (environmentVariable)
		{
		case "shadow":
			debug = 1;
			break;
		case "ao":
		case "depth":
			flag = true;
			goto IL_025b;
		default:
			{
				flag = false;
				goto IL_025b;
			}
			IL_025b:
			debug = (flag ? 2 : 0);
			break;
		}
		studioLook2.Debug = debug;
		studioLook.DebugDepth = environmentVariable == "depth";
		return studioLook;
		static float? Value(string key)
		{
			if (!float.TryParse(Environment.GetEnvironmentVariable(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return null;
			}
			return result;
		}
	}
}

using System;
using System.Numerics;

namespace HeyClicky.Platform.Overlay;

internal struct CursorClusterPose
{
	public double TriangleRotationDegrees;

	public double TriangleScale;

	public double TriangleOpacity;

	public double TriangleGlowRadius;

	public double WaveformOpacity;

	public float[] WaveformBarHeights;

	public double DotsOpacity;

	public int DotCount;

	public Vector2[] DotCenters;

	public double[] DotRadii;

	public double[] DotAcrossScale;

	public double[] DotAngles;

	public string BubbleText;

	public string BubbleLabel;

	public double BubbleScale;

	public double BubbleOpacity;

	public double WaveformScale;

	public double StopSquareOpacity;

	public double DwellRingOpacity;

	public double DwellRingProgress;

	public static CursorClusterPose CreateDefault()
	{
		CursorClusterPose result = default(CursorClusterPose);
		result.TriangleRotationDegrees = -35.0;
		result.TriangleScale = 1.0;
		result.TriangleOpacity = 1.0;
		result.TriangleGlowRadius = 8.0;
		result.WaveformBarHeights = new float[5];
		result.DotCenters = new Vector2[3];
		result.DotRadii = new double[3];
		result.DotAcrossScale = new double[3];
		result.DotAngles = new double[3];
		result.BubbleText = "";
		result.BubbleLabel = "";
		result.BubbleScale = 0.5;
		result.WaveformScale = 1.0;
		return result;
	}

	public readonly bool VisuallyEquals(in CursorClusterPose other)
	{
		if (TriangleRotationDegrees != other.TriangleRotationDegrees || TriangleScale != other.TriangleScale || TriangleOpacity != other.TriangleOpacity || TriangleGlowRadius != other.TriangleGlowRadius || WaveformOpacity != other.WaveformOpacity || DotsOpacity != other.DotsOpacity || DotCount != other.DotCount || BubbleText != other.BubbleText || BubbleLabel != other.BubbleLabel || BubbleScale != other.BubbleScale || BubbleOpacity != other.BubbleOpacity || WaveformScale != other.WaveformScale || StopSquareOpacity != other.StopSquareOpacity || DwellRingOpacity != other.DwellRingOpacity || DwellRingProgress != other.DwellRingProgress)
		{
			return false;
		}
		if (WaveformOpacity > 0.0 && !WaveformBarHeights.AsSpan().SequenceEqual(other.WaveformBarHeights))
		{
			return false;
		}
		if (DotsOpacity > 0.0 && DotCount > 0)
		{
			for (int i = 0; i < 3; i++)
			{
				if (DotCenters[i] != other.DotCenters[i] || DotRadii[i] != other.DotRadii[i] || DotAcrossScale[i] != other.DotAcrossScale[i] || DotAngles[i] != other.DotAngles[i])
				{
					return false;
				}
			}
		}
		return true;
	}

	public readonly void CopyTo(ref CursorClusterPose destination)
	{
		float[] waveformBarHeights = destination.WaveformBarHeights;
		Vector2[] dotCenters = destination.DotCenters;
		double[] dotRadii = destination.DotRadii;
		double[] dotAcrossScale = destination.DotAcrossScale;
		double[] dotAngles = destination.DotAngles;
		destination = this;
		destination.WaveformBarHeights = waveformBarHeights;
		destination.DotCenters = dotCenters;
		destination.DotRadii = dotRadii;
		destination.DotAcrossScale = dotAcrossScale;
		destination.DotAngles = dotAngles;
		WaveformBarHeights.CopyTo(waveformBarHeights, 0);
		DotCenters.CopyTo(dotCenters, 0);
		DotRadii.CopyTo(dotRadii, 0);
		DotAcrossScale.CopyTo(dotAcrossScale, 0);
		DotAngles.CopyTo(dotAngles, 0);
	}
}

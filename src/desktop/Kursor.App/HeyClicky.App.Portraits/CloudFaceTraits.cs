using System.Collections.Generic;
using System.Text;

namespace HeyClicky.App.Portraits;

public sealed class CloudFaceTraits
{
	public readonly record struct Blob(double CenterX, double CenterY, double Diameter);

	public double BaseHue { get; }

	public IReadOnlyList<Blob> ColorBlobs { get; }

	public bool IsWide { get; }

	public bool IsMirrored { get; }

	public double RestingGazeDirection { get; }

	public CloudFaceTraits(string seedString, double? baseHueOverride = null)
	{
		ulong num = 5381uL;
		foreach (Rune item in seedString.EnumerateRunes())
		{
			num = num * 33 + (ulong)item.Value;
		}
		ulong state = num;
		double num2 = Next();
		BaseHue = baseHueOverride ?? num2;
		List<Blob> list = new List<Blob>();
		int num3 = 2 + ((Next() > 0.4) ? 1 : 0);
		for (int i = 0; i < num3; i++)
		{
			Next();
			list.Add(new Blob(0.22 + Next() * 0.56, 0.25 + Next() * 0.5, 0.5 + Next() * 0.35));
		}
		ColorBlobs = list;
		IsWide = Next() > 0.5;
		IsMirrored = Next() > 0.5;
		RestingGazeDirection = (Next() - 0.5) * 2.0;
		double Next()
		{
			state += 11400714819323198485uL;
			ulong num4 = state;
			long num5 = (long)(num4 ^ (num4 >> 30)) * -4658895280553007687L;
			long num6 = (num5 ^ (num5 >>> 27)) * -7723592293110705685L;
			return (double)((ulong)(num6 ^ (num6 >>> 31)) % 10000uL) / 10000.0;
		}
	}
}

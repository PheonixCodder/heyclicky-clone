using System.Runtime.CompilerServices;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed record PaperShadow(Color Color, double Radius, double X = 0.0, double Y = 0.0)
{
	public const double SigmaPerRadius = 0.5;

	[CompilerGenerated]
	private PaperShadow(PaperShadow original)
	{
		Color = original.Color;
		Radius = original.Radius;
		X = original.X;
		Y = original.Y;
	}
}

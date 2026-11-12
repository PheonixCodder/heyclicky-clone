namespace HeyClicky.App.Dock;

public readonly record struct ArcPlacement(double CenterX, double CenterY, double Scale, double Degrees, double Opacity, double Z)
{
	public ArcRect Frame
	{
		get
		{
			double num = 494.0 * Scale;
			double num2 = 314.0 * Scale;
			return new ArcRect(CenterX - num / 2.0, CenterY - num2 / 2.0, num, num2);
		}
	}
}

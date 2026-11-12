namespace HeyClicky.App.Dock;

public readonly record struct DeckPose(double X, double Y, double Scale, double Degrees, double Opacity)
{
	public static readonly DeckPose Zero = new DeckPose(0.0, 0.0, 0.0, 0.0, 0.0);

	public static DeckPose operator +(DeckPose a, DeckPose b)
	{
		return new DeckPose(a.X + b.X, a.Y + b.Y, a.Scale + b.Scale, a.Degrees + b.Degrees, a.Opacity + b.Opacity);
	}

	public static DeckPose operator -(DeckPose a, DeckPose b)
	{
		return new DeckPose(a.X - b.X, a.Y - b.Y, a.Scale - b.Scale, a.Degrees - b.Degrees, a.Opacity - b.Opacity);
	}

	public static DeckPose operator *(DeckPose a, double k)
	{
		return new DeckPose(a.X * k, a.Y * k, a.Scale * k, a.Degrees * k, a.Opacity * k);
	}
}

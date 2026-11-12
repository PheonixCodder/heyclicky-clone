namespace HeyClicky;

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
	public double Right => X + Width;

	public double Bottom => Y + Height;

	public bool Contains(PointD p)
	{
		if (p.X >= X && p.X < Right && p.Y >= Y)
		{
			return p.Y < Bottom;
		}
		return false;
	}
}

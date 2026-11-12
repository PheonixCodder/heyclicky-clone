using System;

namespace HeyClicky.App.Dock;

public readonly record struct ArcRect(double X, double Y, double W, double H)
{
	public double MidX => X + W / 2.0;

	public double MidY => Y + H / 2.0;

	public double MaxX => X + W;

	public double MaxY => Y + H;

	public bool IsEmpty
	{
		get
		{
			if (!(W <= 0.0))
			{
				return H <= 0.0;
			}
			return true;
		}
	}

	public ArcRect Inset(double dx, double dy)
	{
		return new ArcRect(X + dx, Y + dy, W - 2.0 * dx, H - 2.0 * dy);
	}

	public bool Contains(double x, double y)
	{
		if (x >= X && x <= MaxX && y >= Y)
		{
			return y <= MaxY;
		}
		return false;
	}

	public ArcRect Union(ArcRect other)
	{
		if (IsEmpty)
		{
			return other;
		}
		if (other.IsEmpty)
		{
			return this;
		}
		double num = Math.Min(X, other.X);
		double num2 = Math.Min(Y, other.Y);
		return new ArcRect(num, num2, Math.Max(MaxX, other.MaxX) - num, Math.Max(MaxY, other.MaxY) - num2);
	}
}

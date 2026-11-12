using System;

namespace HeyClicky.Drawing3D;

public readonly record struct Rect2(double X, double Y, double Width, double Height)
{
	public double MinX => X;

	public double MinY => Y;

	public double MaxX => X + Width;

	public double MaxY => Y + Height;

	public double MidX => X + Width / 2.0;

	public double MidY => Y + Height / 2.0;

	public bool IsNull
	{
		get
		{
			if (!double.IsInfinity(X))
			{
				return double.IsInfinity(Y);
			}
			return true;
		}
	}

	public static readonly Rect2 Null = new Rect2(double.PositiveInfinity, double.PositiveInfinity, 0.0, 0.0);

	public Rect2 Inset(double dx, double dy)
	{
		return new Rect2(X + dx, Y + dy, Width - 2.0 * dx, Height - 2.0 * dy);
	}

	public Rect2 Offset(double dx, double dy)
	{
		return new Rect2(X + dx, Y + dy, Width, Height);
	}

	public bool Contains(double px, double py)
	{
		if (px >= X && px < MaxX && py >= Y)
		{
			return py < MaxY;
		}
		return false;
	}

	public Rect2 Union(Rect2 other)
	{
		if (IsNull)
		{
			return other;
		}
		if (other.IsNull)
		{
			return this;
		}
		double num = Math.Min(X, other.X);
		double num2 = Math.Min(Y, other.Y);
		return new Rect2(num, num2, Math.Max(MaxX, other.MaxX) - num, Math.Max(MaxY, other.MaxY) - num2);
	}

	public Rect2 Intersect(Rect2 other)
	{
		if (IsNull || other.IsNull)
		{
			return Null;
		}
		double num = Math.Max(X, other.X);
		double num2 = Math.Max(Y, other.Y);
		double num3 = Math.Min(MaxX, other.MaxX);
		double num4 = Math.Min(MaxY, other.MaxY);
		if (!(num3 < num) && !(num4 < num2))
		{
			return new Rect2(num, num2, num3 - num, num4 - num2);
		}
		return Null;
	}

	public Rect2 Union(double px, double py)
	{
		return Union(new Rect2(px, py, 0.0, 0.0));
	}
}

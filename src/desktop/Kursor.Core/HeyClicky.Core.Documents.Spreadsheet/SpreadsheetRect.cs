using System;

namespace HeyClicky.Core.Documents.Spreadsheet;

public readonly record struct SpreadsheetRect(double X, double Y, double Width, double Height)
{
	public double MinX => X;

	public double MinY => Y;

	public double MaxX => X + Width;

	public double MaxY => Y + Height;

	public double MidX => X + Width / 2.0;

	public double MidY => Y + Height / 2.0;

	public bool IsEmpty
	{
		get
		{
			if (!(Width <= 0.0))
			{
				return Height <= 0.0;
			}
			return true;
		}
	}

	public SpreadsheetRect Offset(double dx, double dy)
	{
		return this with
		{
			X = X + dx,
			Y = Y + dy
		};
	}

	public SpreadsheetRect Inset(double dx, double dy)
	{
		return new SpreadsheetRect(X + dx, Y + dy, Width - 2.0 * dx, Height - 2.0 * dy);
	}

	public SpreadsheetRect Intersection(SpreadsheetRect other)
	{
		double num = Math.Max(MinX, other.MinX);
		double num2 = Math.Max(MinY, other.MinY);
		double num3 = Math.Min(MaxX, other.MaxX);
		double num4 = Math.Min(MaxY, other.MaxY);
		if (!(num3 <= num) && !(num4 <= num2))
		{
			return new SpreadsheetRect(num, num2, num3 - num, num4 - num2);
		}
		return default(SpreadsheetRect);
	}

	public SpreadsheetRect Union(SpreadsheetRect other)
	{
		if (IsEmpty)
		{
			return other;
		}
		if (other.IsEmpty)
		{
			return this;
		}
		double num = Math.Min(MinX, other.MinX);
		double num2 = Math.Min(MinY, other.MinY);
		return new SpreadsheetRect(num, num2, Math.Max(MaxX, other.MaxX) - num, Math.Max(MaxY, other.MaxY) - num2);
	}

	public bool Intersects(SpreadsheetRect other)
	{
		return !Intersection(other).IsEmpty;
	}
}

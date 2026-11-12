namespace HeyClicky.Core.Ink;

public readonly record struct InkRange(double LowerBound, double UpperBound)
{
	public bool Contains(double value)
	{
		if (value >= LowerBound)
		{
			return value <= UpperBound;
		}
		return false;
	}
}

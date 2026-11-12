namespace HeyClicky.App.Ink;

public struct ClickySketchRandom
{
	private ulong state;

	public ClickySketchRandom(long seed)
	{
		state = (ulong)(seed + -7046029254386353131L);
	}

	public double Next()
	{
		state += 11400714819323198485uL;
		ulong num = state;
		long num2 = (long)(num ^ (num >> 30)) * -4658895280553007687L;
		long num3 = (num2 ^ (num2 >>> 27)) * -7723592293110705685L;
		return (double)((ulong)(num3 ^ (num3 >>> 31)) >> 11) / 9007199254740992.0;
	}

	public double Uniform(double low, double high)
	{
		return low + (high - low) * Next();
	}
}

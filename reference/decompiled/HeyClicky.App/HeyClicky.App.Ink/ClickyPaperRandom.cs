using System.Text;

namespace HeyClicky.App.Ink;

public struct ClickyPaperRandom
{
	private ulong state;

	public ClickyPaperRandom(ulong seed)
	{
		state = (ulong)(((long)seed * -7046029254386353131L) | 1);
	}

	public ulong Next()
	{
		state ^= state >> 12;
		state ^= state << 25;
		state ^= state >> 27;
		return state * 2685821657736338717L;
	}

	public static ulong StableSeed(string text)
	{
		ulong num = 14695981039346656037uL;
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		foreach (byte b in bytes)
		{
			num = (num ^ b) * 1099511628211L;
		}
		return num;
	}

	public double NextUnit()
	{
		return (double)(Next() >> 11) / 9007199254740992.0;
	}

	public double Next(double low, double high)
	{
		return low + NextUnit() * (high - low);
	}
}

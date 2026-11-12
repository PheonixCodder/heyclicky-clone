using Vortice.Mathematics;

namespace HeyClicky.Drawing3D;

internal readonly record struct WordsGlow(Color4 TightColor, float TightRadius, Color4 WideColor, float WideRadius, float WideOffsetY)
{
	public static WordsGlow? ForInk(string ink)
	{
		string text = ink.ToUpperInvariant();
		if (!(text == "#FFFFFF"))
		{
			if (text == "#15171C")
			{
				return new WordsGlow(new Color4(1f, 1f, 1f, 0.95f), 0.7f, new Color4(1f, 1f, 1f, 0.6f), 2f, 0f);
			}
			return null;
		}
		return new WordsGlow(new Color4(0.03137255f, 0.05490196f, 0.11764706f, 0.95f), 0.6f, new Color4(0.03137255f, 0.05490196f, 0.11764706f, 0.55f), 1.5f, 1f);
	}
}

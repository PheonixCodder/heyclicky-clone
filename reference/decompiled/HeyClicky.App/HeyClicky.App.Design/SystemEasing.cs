using System.Numerics;
using Windows.UI.Composition;

namespace HeyClicky.App.Design;

public static class SystemEasing
{
	public static CubicBezierEasingFunction Expressive(Compositor c)
	{
		return c.CreateCubicBezierEasingFunction(new Vector2(0.23f, 1f), new Vector2(0.32f, 1f));
	}

	public static CubicBezierEasingFunction EaseInOut(Compositor c)
	{
		return c.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(0.58f, 1f));
	}
}

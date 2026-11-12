using System.Numerics;
using Microsoft.UI.Composition;

namespace HeyClicky.App.Design;

public static class Easing
{
	public static CubicBezierEasingFunction Expressive(Compositor c)
	{
		return c.CreateCubicBezierEasingFunction(new Vector2(0.23f, 1f), new Vector2(0.32f, 1f));
	}

	public static CubicBezierEasingFunction EaseOut(Compositor c)
	{
		return c.CreateCubicBezierEasingFunction(new Vector2(0f, 0f), new Vector2(0.58f, 1f));
	}

	public static CubicBezierEasingFunction EaseInOut(Compositor c)
	{
		return c.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(0.58f, 1f));
	}

	public static CubicBezierEasingFunction Drawer(Compositor c)
	{
		return c.CreateCubicBezierEasingFunction(new Vector2(0.32f, 0.72f), new Vector2(0f, 1f));
	}
}

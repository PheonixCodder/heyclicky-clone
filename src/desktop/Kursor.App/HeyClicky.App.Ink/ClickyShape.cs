using System;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyShape(Func<Rect, PaperPath> path) : IClickyShape
{
	public static readonly IClickyShape Rectangle = new ClickyShape((Rect r) => new PaperPath().AddRect(r));

	public static readonly IClickyShape Capsule = new ClickyShape((Rect r) => new PaperPath().AddCapsule(r));

	public static readonly IClickyShape Circle = new ClickyShape(delegate(Rect r)
	{
		double num = Math.Min(r.Width, r.Height);
		return new PaperPath().AddEllipse(new Rect(r.X + (r.Width - num) / 2.0, r.Y + (r.Height - num) / 2.0, num, num));
	});

	public static readonly IClickyShape Ellipse = new ClickyShape((Rect r) => new PaperPath().AddEllipse(r));

	public PaperPath PathIn(Rect rect)
	{
		return path(rect);
	}

	public static IClickyShape RoundedRectangle(double radius)
	{
		return new ClickyShape((Rect r) => new PaperPath().AddRoundedRect(r, radius));
	}

	public static IClickyShape UnevenRoundedRectangle(double topLeading, double bottomLeading, double bottomTrailing, double topTrailing)
	{
		return new ClickyShape((Rect r) => new PaperPath().AddUnevenRoundedRect(r, topLeading, bottomLeading, bottomTrailing, topTrailing));
	}
}

using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkLeader : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		return new PaperPath().MoveTo(rect.X + 1.0, rect.Y + rect.Height - 1.0).LineTo(rect.X + rect.Width - 1.0, rect.Y + rect.Height - 1.0);
	}
}

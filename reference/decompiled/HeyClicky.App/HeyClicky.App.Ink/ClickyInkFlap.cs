using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkFlap : IClickyShape
{
	public PaperPath PathIn(Rect rect)
	{
		return new PaperPath().MoveTo(rect.X, rect.Y + rect.Height).LineTo(rect.X + rect.Width / 2.0, rect.Y).LineTo(rect.X + rect.Width, rect.Y + rect.Height)
			.Close();
	}
}

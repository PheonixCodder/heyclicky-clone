using Windows.Foundation;

namespace HeyClicky.App.Ink;

public interface IClickyShape
{
	PaperPath PathIn(Rect rect);
}

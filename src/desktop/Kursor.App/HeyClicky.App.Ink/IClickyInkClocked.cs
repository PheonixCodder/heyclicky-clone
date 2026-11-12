using System;

namespace HeyClicky.App.Ink;

public interface IClickyInkClocked
{
	void Render(DateTimeOffset now);
}

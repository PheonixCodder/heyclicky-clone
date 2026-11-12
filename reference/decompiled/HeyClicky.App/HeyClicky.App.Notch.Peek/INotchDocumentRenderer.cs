using System;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch.Peek;

public interface INotchDocumentRenderer
{
	bool CanRender(string path);

	FrameworkElement Render(string path, Action reload);
}

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Window;

internal static class UIElementCollectionExtensions
{
	public static void AddRange(this UIElementCollection children, IEnumerable<FrameworkElement> elements)
	{
		foreach (FrameworkElement element in elements)
		{
			children.Add(element);
		}
	}
}

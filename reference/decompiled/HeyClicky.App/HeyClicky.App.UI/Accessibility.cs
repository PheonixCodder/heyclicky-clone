using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.UI;

public static class Accessibility
{
	public static string TextWithin(DependencyObject root, int maxLength = 120)
	{
		List<string> parts = new List<string>();
		Walk(root);
		string text = string.Join(" ", parts.Where((string p) => p.Length > 0));
		if (text.Length > maxLength)
		{
			return text.Substring(0, maxLength);
		}
		return text;
		void Walk(DependencyObject node)
		{
			if (parts.Sum((string p) => p.Length) <= maxLength)
			{
				if (node is TextBlock { Text: { Length: >0 } text2, Visibility: Visibility.Visible } textBlock && !IsIconFont(textBlock))
				{
					parts.Add(text2.Trim());
				}
				int childrenCount = VisualTreeHelper.GetChildrenCount(node);
				for (int i = 0; i < childrenCount; i++)
				{
					Walk(VisualTreeHelper.GetChild(node, i));
				}
				if (childrenCount == 0 && node is Panel panel)
				{
					foreach (UIElement child in panel.Children)
					{
						Walk(child);
					}
				}
			}
		}
	}

	private static bool IsIconFont(TextBlock block)
	{
		return block.Text.All((char c) => c >= '\ue000' && c <= '\uf8ff');
	}

	public static T Named<T>(this T element, string name) where T : DependencyObject
	{
		AutomationProperties.SetName(element, name);
		return element;
	}
}

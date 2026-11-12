using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class InkBaseline
{
	public static readonly DependencyProperty FromBottomProperty = DependencyProperty.RegisterAttached("FromBottom", typeof(double), typeof(InkBaseline), new PropertyMetadata(double.NaN));

	public static void SetFromBottom(UIElement e, double value)
	{
		e.SetValue(FromBottomProperty, value);
	}

	public static double GetFromBottom(UIElement e)
	{
		return (double)e.GetValue(FromBottomProperty);
	}

	public static T BaselineFromBottom<T>(this T e, double value) where T : UIElement
	{
		SetFromBottom(e, value);
		return e;
	}

	public static double? First(UIElement element)
	{
		double num = ((element is FrameworkElement { Margin: var margin }) ? margin.Top : 0.0);
		double fromBottom = GetFromBottom(element);
		if (!double.IsNaN(fromBottom))
		{
			double num2 = element.DesiredSize.Height - ((element is FrameworkElement { Margin: { Top: var top }, Margin: var margin3 }) ? (top + margin3.Bottom) : 0.0);
			return num + num2 - fromBottom;
		}
		double? num3;
		if (!(element is IInkBaseline { FirstBaseline: var firstBaseline }))
		{
			if (!(element is TextBlock textBlock))
			{
				if (!(element is ClickyHandText clickyHandText))
				{
					if (!(element is Border { Child: var child } border))
					{
						Panel panel;
						if (element is StackPanel stackPanel)
						{
							if (stackPanel.Orientation == Orientation.Vertical)
							{
								foreach (UIElement child2 in stackPanel.Children)
								{
									if (child2.Visibility == Visibility.Collapsed)
									{
										continue;
									}
									num3 = First(child2);
									double? num4;
									if (num3.HasValue)
									{
										double valueOrDefault = num3.GetValueOrDefault();
										num4 = num + stackPanel.Padding.Top + valueOrDefault;
									}
									else
									{
										num3 = null;
										num4 = num3;
									}
									num3 = num4;
									goto IL_02af;
								}
								return null;
							}
							panel = (Panel)element;
						}
						else
						{
							panel = element as Panel;
							if ((object)panel == null)
							{
								goto IL_02a4;
							}
						}
						foreach (UIElement child3 in panel.Children)
						{
							double? num5 = First(child3);
							if (!num5.HasValue)
							{
								continue;
							}
							double valueOrDefault2 = num5.GetValueOrDefault();
							num3 = num + valueOrDefault2;
							goto IL_02af;
						}
						return null;
					}
					if ((object)child != null)
					{
						num3 = First(child);
						if (num3.HasValue)
						{
							double valueOrDefault3 = num3.GetValueOrDefault();
							return num + border.Padding.Top + border.BorderThickness.Top + valueOrDefault3;
						}
						return null;
					}
					goto IL_02a4;
				}
				return num + clickyHandText.FirstBaseline;
			}
			return num + textBlock.BaselineOffset;
		}
		if (firstBaseline.HasValue)
		{
			double valueOrDefault4 = firstBaseline.GetValueOrDefault();
			return num + valueOrDefault4;
		}
		return null;
		IL_02a4:
		return null;
		IL_02af:
		return num3;
	}
}

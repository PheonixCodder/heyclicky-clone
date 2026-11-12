using System;
using HeyClicky.App.Design;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.UI;

public sealed class GelButton : Pressable
{
	public enum Sizing
	{
		Normal,
		Large,
		Compact
	}

	private readonly GelSurface surface;

	private readonly TextBlock label;

	private readonly Grid content;

	private readonly TextBlock? icon;

	private readonly ShadowLayer shadow;

	private readonly GelAppearance appearance;

	private bool loading;

	private readonly ProgressRing? spinner;

	public string Title
	{
		get
		{
			return label.Text;
		}
		set
		{
			label.Text = value;
		}
	}

	public bool IsEnabledGel
	{
		get
		{
			return base.Enabled;
		}
		set
		{
			base.Enabled = value;
			surface.Enabled = value;
			Color c = ((appearance == GelAppearance.SecondaryOutline) ? DS.White(value ? 0.86 : 0.4) : (value ? Colors.Black : DS.Colors.GelDisabledContent));
			label.Foreground = Ui.Brush(c);
			if ((object)icon != null)
			{
				icon.Foreground = Ui.Brush(c);
			}
			if (icon?.Parent is Grid embossed)
			{
				Ui.SetEmboss(embossed, value ? 0.8 : 0.18);
			}
			if (label.Parent is Grid embossed2)
			{
				Ui.SetEmboss(embossed2, value ? 0.8 : 0.18);
			}
			shadow.Opacity = (value ? 1.0 : 0.27);
		}
	}

	public bool IsLoading
	{
		get
		{
			return loading;
		}
		set
		{
			loading = value;
			content.Opacity = ((!value) ? 1 : 0);
			if ((object)spinner != null)
			{
				spinner.Visibility = ((!value) ? Visibility.Collapsed : Visibility.Visible);
				spinner.IsActive = value;
			}
		}
	}

	public unsafe GelButton(string title, GelAppearance appearance = GelAppearance.PrimaryBlue, Sizing size = Sizing.Normal, double cornerRadius = 15.0, string? iconGlyph = null, double? labelFontSize = null, double? minimumHeight = null)
	{
		this.appearance = appearance;
		double size2 = labelFontSize ?? (size switch
		{
			Sizing.Large => 14.0, 
			Sizing.Compact => 11.0, 
			_ => 12.5, 
		});
		double num;
		if (labelFontSize.HasValue)
		{
			double valueOrDefault = labelFontSize.GetValueOrDefault();
			num = valueOrDefault * 0.93;
		}
		else
		{
			num = size switch
			{
				Sizing.Large => 13.0, 
				Sizing.Compact => 8.5, 
				_ => 10.5, 
			};
		}
		double size3 = num;
		object obj = size switch
		{
			Sizing.Large => (22, 12.0), 
			Sizing.Compact => (11, 4.5), 
			_ => (15, 8.0), 
		};
		double num2 = ((ValueTuple<int, double>*)(&obj))->Item1;
		double item = ((ValueTuple<int, double>*)(&obj))->Item2;
		bool flag = appearance == GelAppearance.SecondaryOutline;
		Color color = (flag ? DS.White(0.86) : Colors.Black);
		shadow = new ShadowLayer(cornerRadius, DS.Black(flag ? 0.0 : 0.11), 5.0, 4.0);
		surface = new GelSurface(appearance, cornerRadius);
		label = Ui.Text(title, size2, FontWeights.SemiBold, color);
		label.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = ((!string.IsNullOrEmpty(title)) ? 6 : 0),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		if (iconGlyph != null)
		{
			icon = Icons.Glyph(iconGlyph, size3, color);
			icon.FontWeight = FontWeights.Bold;
			stackPanel.Children.Add(flag ? ((FrameworkElement)icon) : ((FrameworkElement)Ui.Embossed(icon)));
		}
		if (!string.IsNullOrEmpty(title))
		{
			stackPanel.Children.Add(flag ? ((FrameworkElement)label) : ((FrameworkElement)Ui.Embossed(label)));
		}
		content = new Grid
		{
			Padding = new Thickness(num2, item, num2, item)
		};
		content.Children.Add(stackPanel);
		base.Children.Add(shadow);
		base.Children.Add(surface);
		base.Children.Add(content);
		if (size == Sizing.Large)
		{
			spinner = new ProgressRing
			{
				Width = 16.0,
				Height = 16.0,
				IsActive = false,
				Visibility = Visibility.Collapsed,
				Foreground = Ui.Brush(DS.Black(0.6)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			base.Children.Add(spinner);
		}
		base.HoverChanged += delegate(bool h)
		{
			surface.Hovered = h;
		};
		WithHoverScale(1.03f);
		base.CornerRadius = new CornerRadius(0.0);
		if (minimumHeight.HasValue)
		{
			double valueOrDefault2 = minimumHeight.GetValueOrDefault();
			base.MinHeight = valueOrDefault2;
		}
	}
}

using System;
using HeyClicky.App.Design;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.UI;

public sealed class GelSurface : Grid
{
	private readonly Border fill;

	private readonly Border shade1;

	private readonly Border shade2;

	private readonly Border gloss;

	private readonly Grid overlays;

	private readonly Grid plate = new Grid();

	private GelAppearance appearance;

	private bool enabled = true;

	private bool hovered;

	private readonly double radius;

	private readonly Border sheen = new Border
	{
		IsHitTestVisible = false,
		Opacity = 0.0,
		Background = Ui.Vertical((0.0, DS.White(0.3)), (1.0, DS.White(0.12)))
	};

	public bool ShowsHoverSheen { get; set; } = true;

	public GelAppearance Appearance
	{
		get
		{
			return appearance;
		}
		set
		{
			appearance = value;
			Apply();
		}
	}

	public bool Enabled
	{
		get
		{
			return enabled;
		}
		set
		{
			enabled = value;
			Apply();
			if (hovered)
			{
				ShowSheen();
			}
		}
	}

	public bool Hovered
	{
		get
		{
			return hovered;
		}
		set
		{
			if (hovered != value)
			{
				hovered = value;
				Apply();
				ShowSheen();
			}
		}
	}

	public GelSurface(GelAppearance appearance, double cornerRadius)
	{
		this.appearance = appearance;
		radius = cornerRadius;
		base.IsHitTestVisible = false;
		fill = new Border
		{
			CornerRadius = new CornerRadius(cornerRadius),
			BorderThickness = new Thickness(1.0)
		};
		shade1 = new Border
		{
			Height = 12.0,
			Margin = new Thickness(4.0, 0.0, 4.0, 2.0),
			VerticalAlignment = VerticalAlignment.Bottom,
			CornerRadius = new CornerRadius(6.0),
			Background = Ui.Vertical((0.0, Colors.Transparent), (1.0, DS.Hex(4414087u, 0.26)))
		};
		shade2 = new Border
		{
			Height = 3.0,
			Margin = new Thickness(10.0, 0.0, 10.0, 3.0),
			VerticalAlignment = VerticalAlignment.Bottom,
			CornerRadius = new CornerRadius(1.5),
			Background = Ui.Vertical((0.0, Colors.Transparent), (0.5, DS.White(0.32)), (1.0, Colors.Transparent))
		};
		gloss = new Border
		{
			Height = 7.0,
			Margin = new Thickness(12.0, 3.0, 12.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top,
			CornerRadius = new CornerRadius(3.5)
		};
		overlays = new Grid();
		overlays.Children.Add(shade1);
		overlays.Children.Add(shade2);
		overlays.Children.Add(gloss);
		overlays.Children.Add(sheen);
		plate.Children.Add(fill);
		plate.Children.Add(overlays);
		base.Children.Add(plate);
		base.SizeChanged += delegate
		{
			Layout();
		};
		Apply();
	}

	private void ShowSheen()
	{
		Anim.Opacity(sheen, (hovered && enabled && ShowsHoverSheen && appearance != GelAppearance.SecondaryOutline) ? 1 : 0, 0.083, Easing.EaseOut(Ui.Compositor));
	}

	private void Layout()
	{
		double actualWidth = base.ActualWidth;
		double actualHeight = base.ActualHeight;
		gloss.Visibility = ((!(actualWidth > 26.0) || !(actualHeight > 12.0)) ? Visibility.Collapsed : Visibility.Visible);
		shade1.Visibility = ((!(actualHeight >= 16.0) || !(actualWidth > 8.0)) ? Visibility.Collapsed : Visibility.Visible);
		shade2.Visibility = ((!(actualHeight >= 16.0) || !(actualWidth > 20.0)) ? Visibility.Collapsed : Visibility.Visible);
		double uniformRadius = Math.Min(radius, Math.Min(actualWidth, actualHeight) / 2.0);
		fill.CornerRadius = new CornerRadius(uniformRadius);
		Ui.ClipRounded(overlays, uniformRadius);
	}

	private void Apply()
	{
		switch (appearance)
		{
		case GelAppearance.PrimaryBlue:
			fill.Background = (enabled ? Ui.Vertical((0.0, DS.Hex(15594751u)), (0.18, DS.Hex(14084095u)), (0.5, DS.Hex(10668031u)), (0.8, DS.Hex(10079224u)), (1.0, DS.Hex(10670839u))) : Ui.Vertical((0.0, DS.Hex(12110044u)), (0.5, DS.Hex(10466768u)), (1.0, DS.Hex(9415364u))));
			fill.BorderBrush = Ui.Brush(enabled ? DS.Hex(2234520u) : DS.Hex(7374755u));
			break;
		case GelAppearance.SecondaryWhite:
			fill.Background = (enabled ? Ui.Vertical((0.0, DS.Hex(16514043u)), (0.42, DS.Hex(15592941u)), (1.0, DS.Hex(15000804u))) : Ui.Vertical((0.0, DS.Hex(13948892u)), (0.42, DS.Hex(12962513u)), (1.0, DS.Hex(12305098u))));
			fill.BorderBrush = Ui.Brush(enabled ? DS.Hex(6447714u) : DS.Hex(9213087u));
			break;
		case GelAppearance.SecondaryOutline:
		{
			double alpha = ((!enabled) ? 0.04 : (hovered ? 0.16 : 0.08));
			fill.Background = Ui.Vertical((0.0, DS.Hex(15594751u, alpha)), (0.18, DS.Hex(14084095u, alpha)), (0.5, DS.Hex(10668031u, alpha)), (0.8, DS.Hex(10079224u, alpha)), (1.0, DS.Hex(10670839u, alpha)));
			fill.BorderBrush = ((!enabled) ? Ui.Vertical((0.0, DS.White(0.12)), (1.0, DS.White(0.06))) : (hovered ? Ui.Vertical((0.0, DS.White(0.38)), (1.0, DS.White(0.22))) : Ui.Vertical((0.0, DS.White(0.24)), (1.0, DS.White(0.12)))));
			break;
		}
		case GelAppearance.Mint:
			fill.Background = Ui.Vertical((0.0, DS.Hex(15989491u)), (0.18, DS.Hex(14873571u)), (0.5, DS.Hex(14085847u)), (0.82, DS.Hex(14085847u)), (1.0, DS.Hex(14348507u)));
			fill.BorderBrush = Ui.Vertical((0.0, DS.White(0.9)), (1.0, DS.Black(0.22)));
			break;
		case GelAppearance.Orange:
			fill.Background = Ui.Vertical((0.0, DS.Hex(16573385u)), (0.18, DS.Hex(16301441u)), (0.5, DS.Hex(16097355u)), (0.82, DS.Hex(16097355u)), (1.0, DS.Hex(16165213u)));
			fill.BorderBrush = Ui.Vertical((0.0, DS.White(0.9)), (1.0, DS.Black(0.22)));
			break;
		case GelAppearance.Peach:
			fill.Background = Ui.Vertical((0.0, DS.Hex(16774379u)), (0.18, DS.Hex(16769735u)), (0.5, DS.Hex(16763289u)), (0.82, DS.Hex(16763289u)), (1.0, DS.Hex(16764835u)));
			fill.BorderBrush = Ui.Vertical((0.0, DS.White(0.9)), (1.0, DS.Black(0.22)));
			break;
		}
		bool flag = appearance == GelAppearance.SecondaryOutline;
		Border border = shade1;
		double opacity = (shade2.Opacity = (flag ? 0.0 : (enabled ? 1.0 : 0.2)));
		border.Opacity = opacity;
		double alpha2 = ((!enabled) ? 0.12 : (hovered ? 0.95 : 0.85));
		gloss.Background = Ui.Vertical((0.0, DS.White(alpha2)), (1.0, DS.White(0.02)));
		gloss.Opacity = (flag ? 0.12 : 1.0);
		plate.Opacity = (enabled ? 1.0 : 0.8);
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class IntegrationConnectedSurface : NotchSurface
{
	private readonly StackPanel view;

	public override VerticalAlignment ContentAlignment
	{
		get
		{
			if (!NotchGeometry.HasHardwareNotchEar)
			{
				return VerticalAlignment.Center;
			}
			return VerticalAlignment.Top;
		}
	}

	public override FrameworkElement View => view;

	public IntegrationConnectedSurface(NotchLiveEvent.IntegrationConnected e, IReadOnlyList<string> useCases)
	{
		Grid grid = new Grid
		{
			Width = 30.0,
			Height = 30.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		grid.Children.Add(new ShadowLayer(9.0, DS.Black(0.24), 8.0, 4.0));
		grid.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(9.0),
			Background = Ui.Brush(DS.Hex(16251135u))
		});
		FrameworkElement frameworkElement = IntegrationSuggestionSurface.ToolkitBadge(e.Toolkit, 20.0, e.DisplayName);
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		frameworkElement.VerticalAlignment = VerticalAlignment.Center;
		grid.Children.Add(frameworkElement);
		Grid grid2 = new Grid
		{
			Width = 12.0,
			Height = 12.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, -3.0, -3.0)
		};
		grid2.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(DS.Hex(3900150u)),
			Stroke = Ui.Brush(DS.Black(0.58)),
			StrokeThickness = 1.0
		});
		TextBlock textBlock = Icons.Glyph("\ue73e", 6.5, Colors.White);
		textBlock.FontWeight = FontWeights.Black;
		grid2.Children.Add(textBlock);
		grid.Children.Add(grid2);
		object obj;
		if (string.IsNullOrWhiteSpace(e.DisplayName))
		{
			if (!string.IsNullOrEmpty(e.Toolkit))
			{
				ReadOnlySpan<char> readOnlySpan = new ReadOnlySpan<char>(char.ToUpperInvariant(e.Toolkit[0]));
				string toolkit = e.Toolkit;
				obj = string.Concat(readOnlySpan, toolkit.Substring(1, toolkit.Length - 1));
			}
			else
			{
				obj = "Integration";
			}
		}
		else
		{
			obj = e.DisplayName;
		}
		string text = (string)obj;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 1.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Text(text + " connected", 12.5, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 1),
				(UIElement)Ui.Text("Try one of these next", 9.5, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Left, 1)
			}
		};
		Pressable dismiss = new Pressable
		{
			Height = 28.0,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			CornerRadius = new CornerRadius(14.0),
			Background = Ui.Brush(DS.Hex(2450411u)),
			BorderBrush = Ui.Brush(DS.White(0.1)),
			BorderThickness = new Thickness(0.8),
			VerticalAlignment = VerticalAlignment.Center
		};
		Border wash = new Border
		{
			Margin = new Thickness(-10.0, 0.0, -10.0, 0.0),
			Background = Ui.Brush(DS.White(0.1)),
			Opacity = 0.0,
			IsHitTestVisible = false
		}.Capsule();
		dismiss.Children.Add(wash);
		TextBlock textBlock2 = Icons.Glyph("\ue711", 8.5, Colors.White);
		textBlock2.FontWeight = FontWeights.Bold;
		dismiss.Children.Add(Ui.H(4.0, textBlock2, Ui.Text("Dismiss", 9.5, FontWeights.SemiBold, Colors.White)).Center());
		dismiss.HoverChanged += delegate(bool h)
		{
			dismiss.BorderBrush = Ui.Brush(DS.White(h ? 0.18 : 0.1));
			Anim.Opacity(wash, h ? 1 : 0, 0.12);
			Anim.Scale(dismiss, h ? 1.02f : 1f, 0.12, null, new Vector3((float)dismiss.ActualWidth / 2f, 14f, 0f));
		};
		dismiss.Click += delegate
		{
			base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.IntegrationConnected));
		};
		dismiss.Tip("Dismiss confirmation");
		Grid grid3 = new Grid
		{
			ColumnSpacing = 10.0
		};
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(dismiss, 2);
		grid3.Children.Add(grid);
		grid3.Children.Add(stackPanel);
		grid3.Children.Add(dismiss);
		view = new StackPanel
		{
			Spacing = 10.0,
			Margin = new Thickness(12.0, 8.0, 12.0, 2.0),
			Children = { (UIElement)grid3 }
		};
		if (useCases.Count > 0)
		{
			view.Children.Add(new UseCaseCarousel(useCases.Take(4).ToList()));
		}
	}
}

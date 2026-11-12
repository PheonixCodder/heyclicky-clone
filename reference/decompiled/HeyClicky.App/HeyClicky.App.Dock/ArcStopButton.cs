using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Dock;

public sealed class ArcStopButton : Grid
{
	private const double Diameter = 28.0;

	public ArcStopButton(Action action)
	{
		Pressable pressable = new Pressable
		{
			Width = 28.0,
			Height = 28.0
		};
		Grid face = new Grid
		{
			Width = 28.0,
			Height = 28.0,
			IsHitTestVisible = false
		};
		pressable.Children.Add(face);
		GelSurface surface = new GelSurface(GelAppearance.SecondaryWhite, 14.0);
		face.Children.Add(new ShadowLayer(14.0, DS.Black(0.11), 5.0, 4.0));
		face.Children.Add(surface);
		Grid grid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false
		};
		grid.Children.Add(new Rectangle
		{
			Width = 8.5,
			Height = 8.5,
			RadiusX = 1.6,
			RadiusY = 1.6,
			Fill = Ui.Brush(DS.White(0.8)),
			RenderTransform = new TranslateTransform
			{
				Y = 1.0
			}
		});
		grid.Children.Add(new Rectangle
		{
			Width = 8.5,
			Height = 8.5,
			RadiusX = 1.6,
			RadiusY = 1.6,
			Fill = Ui.Brush(DS.Black(0.85))
		});
		face.Children.Add(grid);
		pressable.HoverChanged += delegate(bool h)
		{
			surface.Hovered = h;
			Anim.Scale(face, h ? 1.05f : 1f, 0.15, Easing.EaseOut(Ui.Compositor), new Vector3(14f, 14f, 0f));
		};
		pressable.WithPressScale();
		pressable.Click += action;
		AutomationProperties.SetName(pressable, "Stop this task");
		AutomationProperties.SetHelpText(pressable, "Ends what it's doing now. The window stays so you can retry.");
		base.Children.Add(pressable);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			Width = 240.0,
			Padding = new Thickness(12.0, 9.0, 12.0, 9.0)
		};
		stackPanel.Children.Add(Ui.Text("Stop this task", 14.0, FontWeights.SemiBold, DS.White(0.95)));
		stackPanel.Children.Add(Ui.Text("Ends what it's doing now. The window stays so you can retry.", 13.0, FontWeights.Normal, DS.White(0.68), TextAlignment.Left, 0, wrap: true));
		Grid tipPlate = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 0.0, 36.0),
			Opacity = 0.0,
			IsHitTestVisible = false
		};
		tipPlate.Children.Add(new ShadowLayer(11.0, DS.Black(0.45), 10.0, 4.0));
		tipPlate.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(11.0),
			Background = Ui.Brush(DS.Rgb(0.17, 0.17, 0.17)),
			BorderBrush = Ui.Brush(DS.White(0.12)),
			BorderThickness = new Thickness(1.0)
		});
		tipPlate.Children.Add(stackPanel);
		Canvas canvas = new Canvas
		{
			Width = 0.0,
			Height = 0.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false
		};
		canvas.Children.Add(tipPlate);
		tipPlate.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			Canvas.SetLeft(tipPlate, 0.0 - e.NewSize.Width);
			Canvas.SetTop(tipPlate, 0.0 - e.NewSize.Height - 8.0);
		};
		base.Children.Add(canvas);
		DispatcherQueueTimer beat = null;
		pressable.HoverChanged += delegate(bool hovering)
		{
			beat?.Stop();
			if (!hovering)
			{
				Anim.Opacity(tipPlate, 0f, 0.16);
			}
			else
			{
				beat = DockTimer.After(0.25, delegate
				{
					tipPlate.CenterPoint = new Vector3((float)tipPlate.ActualWidth, (float)tipPlate.ActualHeight, 0f);
					tipPlate.Scale = new Vector3(0.96f, 0.96f, 1f);
					Anim.Scale(tipPlate, 1f, 0.16, null, null);
					Anim.Opacity(tipPlate, 1f, 0.16);
				});
			}
		};
	}
}

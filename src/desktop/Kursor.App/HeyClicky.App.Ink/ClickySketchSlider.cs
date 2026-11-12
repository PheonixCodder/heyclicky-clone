using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchSlider : Grid
{
	private readonly IReadOnlyList<string> stops;

	private readonly double width;

	private readonly long seed;

	private readonly Action<int> onSelect;

	private int selectedIndex;

	private readonly Canvas canvas = new Canvas();

	public int SelectedIndex
	{
		get
		{
			return selectedIndex;
		}
		set
		{
			if (selectedIndex != value)
			{
				selectedIndex = value;
				Draw();
			}
		}
	}

	private double[] Xs
	{
		get
		{
			if (stops.Count <= 1)
			{
				return new double[1] { width / 2.0 };
			}
			return (from i in Enumerable.Range(0, stops.Count)
				select 12.0 + (double)i * (width - 24.0) / (double)(stops.Count - 1)).ToArray();
		}
	}

	public ClickySketchSlider(IReadOnlyList<string> stops, int selectedIndex, Action<int> onSelect, double width = 262.0, long seed = 11L)
	{
		ClickySketchSlider clickySketchSlider = this;
		this.stops = stops;
		this.selectedIndex = selectedIndex;
		this.onSelect = onSelect;
		this.width = width;
		this.seed = seed;
		base.Width = width;
		base.Height = 52.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		base.Background = Ui.Transparent;
		base.ProtectedCursor = Pressable.ClickableCursor;
		base.Children.Add(canvas);
		Draw();
		bool dragging = false;
		base.PointerPressed += delegate(object _, PointerRoutedEventArgs e)
		{
			dragging = true;
			clickySketchSlider.CapturePointer(e.Pointer);
			clickySketchSlider.Pick(e.GetCurrentPoint(clickySketchSlider).Position.X);
			e.Handled = true;
		};
		base.PointerMoved += delegate(object _, PointerRoutedEventArgs e)
		{
			if (dragging)
			{
				clickySketchSlider.Pick(e.GetCurrentPoint(clickySketchSlider).Position.X);
			}
		};
		base.PointerReleased += delegate(object _, PointerRoutedEventArgs e)
		{
			dragging = false;
			clickySketchSlider.ReleasePointerCapture(e.Pointer);
		};
		AutomationProperties.SetName(this, "Speed");
	}

	private void Pick(double x)
	{
		double[] xs = Xs;
		int num = selectedIndex;
		double num2 = double.MaxValue;
		for (int i = 0; i < xs.Length; i++)
		{
			if (Math.Abs(xs[i] - x) < num2)
			{
				num2 = Math.Abs(xs[i] - x);
				num = i;
			}
		}
		if (num != selectedIndex)
		{
			onSelect(num);
		}
	}

	private void Draw()
	{
		canvas.Children.Clear();
		double[] xs = Xs;
		PaperDrawing paperDrawing = new PaperDrawing();
		paperDrawing.Stroke(ClickySketchPath.Line(new Point(xs.FirstOrDefault(12.0), 14.0), new Point((xs.Length != 0) ? xs[^1] : (width - 12.0), 14.0), seed, 0.5), new PaperStroke(PaperColor.Hex("#B3C7DF"), 5.0, PenLineCap.Round));
		PaperPath paperPath = new PaperPath();
		double[] array = xs;
		foreach (double x in array)
		{
			paperPath.MoveTo(x, 9.0).LineTo(x, 19.0);
		}
		paperDrawing.Stroke(paperPath, new PaperStroke(PaperColor.Hex("#7F96B4"), 1.4, PenLineCap.Round));
		canvas.Children.Add(paperDrawing);
		PaperPath p = new ClickySketchRect(10.0, seed + 3, 0.35).PathIn(new Rect(0f, 0f, 20f, 20f));
		PaperDrawing paperDrawing2 = new PaperDrawing().Fill(p, PaperColor.Hex("#FBFDFF")).Stroke(p, new PaperStroke(ClickySketchInk.TealLine, 1.6));
		Canvas.SetLeft(paperDrawing2, xs[Math.Clamp(selectedIndex, 0, xs.Length - 1)] - 10.0);
		Canvas.SetTop(paperDrawing2, 4.0);
		canvas.Children.Add(paperDrawing2);
		for (int j = 0; j < stops.Count; j++)
		{
			ClickyHandText clickyHandText = new ClickyHandText(stops[j], 16.0, 1.0, 1, 1.0, TextAlignment.Center)
			{
				Width = 48.0
			}.WithForeground((j == selectedIndex) ? PaperColor.Hex("#22395A") : PaperColor.Hex("#6E86A6"));
			Canvas.SetLeft(clickyHandText, xs[j] - 24.0);
			Canvas.SetTop(clickyHandText, 30.0);
			canvas.Children.Add(clickyHandText);
		}
	}
}

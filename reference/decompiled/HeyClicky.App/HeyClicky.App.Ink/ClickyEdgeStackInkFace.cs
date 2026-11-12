using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyEdgeStackInkFace : Grid
{
	public static readonly Size SheetSize = new Size(330f, 210f);

	public static readonly Thickness ShadowInsets = new Thickness(6.0, 5.0, 6.0, 9.0);

	public static Size BoxSize => new Size(SheetSize.Width + ShadowInsets.Left + ShadowInsets.Right, SheetSize.Height + ShadowInsets.Top + ShadowInsets.Bottom);

	public ClickyEdgeStackInkFace(ClickyInkLiveNote note, ClickyEdgeStackInkReading reading, double clock, ClickyHomeSpreadPaper paper, bool isJob, string? liveStep = null, bool showsTape = false, bool fillsScreen = false)
	{
		ClickyInkStyle style = ClickyEdgeStackInkLook.Style(note, paper, clock, reading);
		Grid grid = new Grid
		{
			Padding = new Thickness(18.0, isJob ? 18 : 16, 16.0, 13.0)
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.Children.Add(new ClickyEdgeStackInkHeader(reading, clock, style, 22.0, 8.5));
		FrameworkElement frameworkElement = ClickyEdgeStackInkLead.Make(reading, clock, style, isLarge: false);
		if ((object)frameworkElement != null)
		{
			frameworkElement.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
			Grid.SetRow(frameworkElement, 1);
			grid.Children.Add(frameworkElement);
		}
		Border border = new Border
		{
			Height = 8.0
		};
		Grid.SetRow(border, 2);
		border.VerticalAlignment = VerticalAlignment.Top;
		grid.Children.Add(border);
		grid.RowDefinitions[2].MinHeight = 8.0;
		FrameworkElement frameworkElement2 = ClickyEdgeStackInkFooter.Make(reading, liveStep, style);
		if ((object)frameworkElement2 != null)
		{
			frameworkElement2.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
			Grid.SetRow(frameworkElement2, 3);
			grid.Children.Add(frameworkElement2);
		}
		Grid grid2 = new Grid();
		if (fillsScreen)
		{
			grid2.HorizontalAlignment = HorizontalAlignment.Stretch;
			grid2.VerticalAlignment = VerticalAlignment.Stretch;
		}
		else
		{
			grid2.Width = SheetSize.Width;
			grid2.Height = SheetSize.Height;
			grid2.HorizontalAlignment = HorizontalAlignment.Left;
			grid2.VerticalAlignment = VerticalAlignment.Top;
			grid2.Children.Add(new ClickyEdgeStackInkPaper(paper, isJob));
		}
		grid2.Children.Add(grid);
		if (showsTape && reading.CurrentState.WantsAttention())
		{
			grid2.Children.Add(new ClickyEdgeStackInkTape(reading.CurrentState));
		}
		base.Children.Add(grid2);
		if (!fillsScreen)
		{
			base.Padding = ShadowInsets;
			base.HorizontalAlignment = HorizontalAlignment.Left;
			base.VerticalAlignment = VerticalAlignment.Top;
		}
	}
}

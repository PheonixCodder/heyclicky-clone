using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchChatResizeGrips : Grid
{
	private const double EdgeThickness = 7.0;

	private const double CornerSize = 18.0;

	public NotchChatResizeGrips(INotchHost host)
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 60.0, 0.0, 18.0)
		};
		grid.Children.Add(new NotchChatResizeGrip(host, left: true, right: false, bottom: false)
		{
			Width = 7.0,
			HorizontalAlignment = HorizontalAlignment.Left
		});
		grid.Children.Add(new NotchChatResizeGrip(host, left: false, right: true, bottom: false)
		{
			Width = 7.0,
			HorizontalAlignment = HorizontalAlignment.Right
		});
		base.Children.Add(grid);
		Grid grid2 = new Grid
		{
			Height = 7.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(18.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(18.0)
		});
		NotchChatResizeGrip item = new NotchChatResizeGrip(host, left: true, right: false, bottom: true);
		NotchChatResizeGrip notchChatResizeGrip = new NotchChatResizeGrip(host, left: false, right: false, bottom: true);
		NotchChatResizeGrip notchChatResizeGrip2 = new NotchChatResizeGrip(host, left: false, right: true, bottom: true);
		Grid.SetColumn(notchChatResizeGrip, 1);
		Grid.SetColumn(notchChatResizeGrip2, 2);
		grid2.Children.Add(item);
		grid2.Children.Add(notchChatResizeGrip);
		grid2.Children.Add(notchChatResizeGrip2);
		base.Children.Add(grid2);
		Grid grid3 = new Grid
		{
			Height = 18.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		grid3.Children.Add(new NotchChatResizeGrip(host, left: true, right: false, bottom: true)
		{
			Width = 7.0,
			HorizontalAlignment = HorizontalAlignment.Left
		});
		grid3.Children.Add(new NotchChatResizeGrip(host, left: false, right: true, bottom: true)
		{
			Width = 7.0,
			HorizontalAlignment = HorizontalAlignment.Right
		});
		base.Children.Add(grid3);
	}
}

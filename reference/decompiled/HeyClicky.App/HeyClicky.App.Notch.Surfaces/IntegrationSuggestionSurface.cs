using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class IntegrationSuggestionSurface : NotchSurface
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

	public IntegrationSuggestionSurface(NotchLiveEvent.IntegrationSuggestion e, IReadOnlyList<string> useCases)
	{
		IntegrationSuggestionSurface integrationSuggestionSurface = this;
		Canvas canvas = new Canvas
		{
			Width = 58.0,
			Height = 38.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		FrameworkElement frameworkElement = IntegrationLogos.Image("heyclicky-logo", 24.0, null);
		Ui.ClipRounded(frameworkElement, 5.5);
		Grid grid = CardParts.Tile(frameworkElement);
		Canvas.SetLeft(grid, -10.0);
		Canvas.SetTop(grid, 2.0);
		Grid grid2 = CardParts.Tile(ToolkitBadge(e.Toolkit, 24.0, e.Title));
		Canvas.SetLeft(grid2, 34.0);
		Canvas.SetTop(grid2, 2.0);
		canvas.Children.Add(grid);
		canvas.Children.Add(grid2);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Text("Connect " + e.Title + " to HeyClicky?", 12.5, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 1),
				(UIElement)Ui.Text("Use HeyClicky to:", 10.5, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Left, 1)
			}
		};
		GelActionButton gelActionButton = Btn("No", "\ue711", 72.0, GelAppearance.SecondaryWhite, "Never ask again for this integration");
		GelActionButton gelActionButton2 = Btn("Not now", "\ue823", 98.0, GelAppearance.SecondaryWhite, "Not now");
		GelActionButton gelActionButton3 = Btn("Yes", "\ue71b", 72.0, GelAppearance.PrimaryBlue, "Yes");
		gelActionButton.Click += delegate
		{
			Answer(IntegrationAnswer.No);
		};
		gelActionButton2.Click += delegate
		{
			Answer(IntegrationAnswer.NotNow);
		};
		gelActionButton3.Click += delegate
		{
			Answer(IntegrationAnswer.Yes);
		};
		Grid grid3 = new Grid
		{
			ColumnSpacing = 20.0
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
		StackPanel stackPanel2 = Ui.H(8.0, gelActionButton, gelActionButton2, gelActionButton3);
		stackPanel2.Margin = new Thickness(30.0, 0.0, 0.0, 0.0);
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(stackPanel2, 2);
		grid3.Children.Add(canvas);
		grid3.Children.Add(stackPanel);
		grid3.Children.Add(stackPanel2);
		view = new StackPanel
		{
			Spacing = 8.0,
			Margin = new Thickness(16.0, (useCases.Count == 0) ? 4 : 2, 16.0, 0.0),
			RenderTransform = new TranslateTransform
			{
				Y = ((useCases.Count == 0) ? 6 : 4)
			}
		};
		view.Children.Add(grid3);
		if (useCases.Count > 0)
		{
			view.Children.Add(new UseCaseCarousel(useCases.Take(4).ToList()));
		}
		void Answer(IntegrationAnswer answer)
		{
			if (integrationSuggestionSurface.Host.Actions is INotchCardActions notchCardActions)
			{
				notchCardActions.AnswerIntegrationSuggestion(e.Toolkit, answer);
			}
			else
			{
				integrationSuggestionSurface.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.IntegrationSuggestion));
			}
		}
		static GelActionButton Btn(string t, string glyph, double min, GelAppearance a, string tip)
		{
			return new GelActionButton(t, glyph, a, 28.0, 10.0, min, 10.5).Tip(tip);
		}
	}

	public static FrameworkElement ToolkitBadge(string toolkit, double size, string? displayName = null)
	{
		return IntegrationLogos.Artwork(toolkit, displayName, size, 16.0);
	}
}

using System;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchUsagePill : Pressable
{
	private static readonly Color SpentRed = DS.Rgb(0.96, 0.3, 0.28);

	private readonly BillingUsageRing usage;

	private Popup? popup;

	private Border? card;

	public NotchUsagePill(INotchHost host, BillingUsageRing usage)
	{
		NotchUsagePill notchUsagePill = this;
		this.usage = usage;
		base.Width = (base.Height = 32.0);
		base.CornerRadius = new CornerRadius(16.0);
		base.Background = Ui.Brush(DS.White(0.08));
		base.Tag = "notch-usage";
		base.Children.Add(Ring(Math.Max(0.03, usage.Progress), DS.White(0.18), usage.IsSpent ? SpentRed : DS.White(0.9)));
		base.HoverChanged += delegate(bool h)
		{
			notchUsagePill.Background = Ui.Brush(DS.White(h ? 0.14 : 0.08));
			if (h)
			{
				notchUsagePill.ShowCard();
			}
			else
			{
				notchUsagePill.HideCard();
			}
		};
		base.Click += delegate
		{
			notchUsagePill.HideCard();
			(host.Services.Presenter as NotchPresenter)?.Tabs.ShowSettings(NotchSettingsSection.Account);
		};
		base.Unloaded += delegate
		{
			notchUsagePill.HideCard();
		};
		AutomationProperties.SetName(this, "Monthly usage");
		AutomationProperties.SetHelpText(this, usage.AccessibilityValue);
	}

	private static FrameworkElement Ring(double progress, Color track, Color tint)
	{
		Grid obj = new Grid
		{
			Width = 13.0,
			Height = 13.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false,
			Children = { (UIElement)new Ellipse
			{
				Stroke = Ui.Brush(track),
				StrokeThickness = 2.5
			} }
		};
		double num = 32.98672286269283;
		double item = Math.Max(0.03, Math.Clamp(progress, 0.0, 1.0)) * num / 2.5;
		obj.Children.Add(new Ellipse
		{
			Stroke = Ui.Brush(tint),
			StrokeThickness = 2.5,
			StrokeDashCap = PenLineCap.Round,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeDashArray = new DoubleCollection { item, 1000.0 },
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = new RotateTransform
			{
				Angle = -90.0
			}
		});
		return obj;
	}

	private void ShowCard()
	{
		if ((object)base.XamlRoot != null)
		{
			if ((object)card == null)
			{
				card = UsageCard(usage);
			}
			if ((object)popup == null)
			{
				popup = new Popup
				{
					Child = card,
					IsHitTestVisible = false,
					XamlRoot = base.XamlRoot,
					ShouldConstrainToRootBounds = false
				};
			}
			Point point = TransformToVisual(null).TransformPoint(new Point(0f, 0f));
			popup.HorizontalOffset = point.X;
			popup.VerticalOffset = point.Y + base.ActualHeight + 8.0;
			if (!popup.IsOpen)
			{
				card.Opacity = 0.0;
				popup.IsOpen = true;
				Anim.Opacity(card, 1f, 0.16, Easing.EaseOut(Ui.Compositor));
			}
		}
	}

	public void HideCard()
	{
		Popup popup = this.popup;
		if ((object)popup != null && popup.IsOpen)
		{
			this.popup.IsOpen = false;
		}
	}

	private static Border UsageCard(BillingUsageRing usage)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0
		};
		stackPanel.Children.Add(Section(usage.Agents));
		stackPanel.Children.Add(new Border
		{
			Height = 1.0,
			Background = Ui.Brush(DS.White(0.12))
		});
		stackPanel.Children.Add(Section(usage.Voice));
		string resetsLine = usage.ResetsLine;
		if (resetsLine != null)
		{
			stackPanel.Children.Add(Ui.Text(resetsLine, 10.5, FontWeights.Medium, HomeTheme.SecondaryText));
		}
		return new Border
		{
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			CornerRadius = new CornerRadius(10.0),
			Background = Ui.Brush(HomeTheme.IncomingBubble),
			BorderBrush = Ui.Brush(DS.White(0.08)),
			BorderThickness = new Thickness(1.0),
			IsHitTestVisible = false,
			Child = stackPanel
		};
	}

	private static FrameworkElement Section(BillingUsageSection usage)
	{
		TextBlock textBlock = Ui.Text(usage.Title, 9.5, FontWeights.Bold, HomeTheme.TertiaryText);
		textBlock.CharacterSpacing = 53;
		TextBlock item = Ui.Text(usage.CountText, 15.0, FontWeights.SemiBold, HomeTheme.PrimaryText);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0,
			Children = { (UIElement)item }
		};
		string capText = usage.CapText;
		if (capText != null)
		{
			TextBlock textBlock2 = Ui.Text(capText, 11.0, FontWeights.Medium, HomeTheme.SecondaryText);
			textBlock2.VerticalAlignment = VerticalAlignment.Bottom;
			textBlock2.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
			stackPanel.Children.Add(textBlock2);
		}
		Grid grid = new Grid
		{
			Width = 150.0,
			Height = 4.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		grid.Children.Add(new Border
		{
			Background = Ui.Brush(DS.White(0.14))
		}.Capsule());
		grid.Children.Add(new Border
		{
			Width = Math.Max(4.0, 150.0 * usage.Progress),
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Ui.Brush(usage.IsSpent ? SpentRed : DS.White(0.9))
		}.Capsule());
		return new StackPanel
		{
			Spacing = 6.0,
			Children = 
			{
				(UIElement)textBlock,
				(UIElement)stackPanel,
				(UIElement)grid
			}
		};
	}
}

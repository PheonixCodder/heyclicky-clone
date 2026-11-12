using System;
using HeyClicky.App.Design;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace HeyClicky.App.Notch.Peek;

public sealed class ReadAloudSwitch : Pressable
{
	private readonly AppServices services;

	private readonly GelSurface gel;

	private readonly TextBlock glyph;

	private readonly double diameter;

	private DispatcherQueueTimer? tipTimer;

	private readonly Border tooltip;

	private readonly TextBlock tipState;

	private readonly TextBlock tipBody;

	private readonly GelSurface tipStateGel;

	private Popup? popup;

	private readonly bool tooltipBelow;

	private bool IsOn => services.Settings.LibraryModeEnabled;

	public event Action<bool>? Toggled;

	public ReadAloudSwitch(AppServices services, double diameter, bool tooltipBelow = false)
	{
		this.services = services;
		this.diameter = diameter;
		this.tooltipBelow = tooltipBelow;
		base.Width = (base.Height = diameter);
		gel = new GelSurface(GelAppearance.SecondaryOutline, diameter / 2.0);
		glyph = Icons.Glyph("\ue767", (diameter >= 26.0) ? 12 : 10, DS.White(0.5));
		glyph.FontWeight = FontWeights.SemiBold;
		base.Children.Add(gel);
		base.Children.Add(glyph);
		tipState = Ui.Text("", 10.0, FontWeights.Bold, DS.White(0.75));
		tipStateGel = new GelSurface(GelAppearance.SecondaryOutline, 100.0);
		Grid grid = new Grid
		{
			Children = 
			{
				(UIElement)tipStateGel,
				(UIElement)new Border
				{
					Padding = new Thickness(7.0, 2.0, 7.0, 2.0),
					Child = tipState
				}
			}
		};
		tipBody = Ui.Text("", 11.0, FontWeights.Normal, DS.White(0.68), TextAlignment.Left, 0, wrap: true);
		TextBlock textBlock = Ui.Text("Read aloud", 11.5, FontWeights.SemiBold, DS.White(0.95));
		tooltip = new Border
		{
			Width = 210.0,
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0),
			CornerRadius = new CornerRadius(10.0),
			Background = Ui.Brush(DS.Rgb(0.17, 0.17, 0.17)),
			BorderBrush = Ui.Brush(DS.White(0.12)),
			BorderThickness = new Thickness(1.0),
			Child = new StackPanel
			{
				Spacing = 3.0,
				Children = 
				{
					(UIElement)Ui.H(6.0, textBlock, grid),
					(UIElement)tipBody
				}
			}
		};
		Apply();
		base.HoverChanged += OnHover;
		base.Click += Toggle;
		AutomationProperties.SetName(this, "Read answers aloud");
		AutomationProperties.SetHelpText(this, "When on, HeyClicky speaks its answers as well as writing them.");
		base.Unloaded += delegate
		{
			HideTip();
		};
	}

	private void Apply()
	{
		bool isOn = IsOn;
		gel.Appearance = (isOn ? GelAppearance.SecondaryWhite : GelAppearance.SecondaryOutline);
		glyph.Foreground = Ui.Brush(isOn ? DS.Black(0.82) : DS.White(base.IsHovered ? 0.75 : 0.5));
		tipState.Text = (isOn ? "On" : "Off");
		tipState.Foreground = Ui.Brush(isOn ? DS.Black(0.8) : DS.White(0.75));
		tipStateGel.Appearance = (isOn ? GelAppearance.SecondaryWhite : GelAppearance.SecondaryOutline);
		tipBody.Text = (isOn ? "HeyClicky speaks its answers out loud as it writes them. Click to turn off." : "Click to have HeyClicky speak its answers out loud as it writes them.");
		AutomationProperties.SetItemStatus(this, isOn ? "On" : "Off");
	}

	private void OnHover(bool hovering)
	{
		gel.Hovered = hovering;
		Apply();
		tipTimer?.Stop();
		if (!hovering)
		{
			HideTip();
			return;
		}
		tipTimer = base.DispatcherQueue.CreateTimer().Rooted();
		tipTimer.Interval = TimeSpan.FromSeconds(0.25);
		tipTimer.IsRepeating = false;
		tipTimer.Tick += delegate
		{
			ShowTip();
		};
		tipTimer.Start();
	}

	private void Toggle()
	{
		bool flag = !IsOn;
		services.Settings.LibraryModeEnabled = flag;
		ClickyAnalytics_TrackLibraryModeToggled(flag);
		this.Toggled?.Invoke(flag);
		Apply();
		tipTimer?.Stop();
		ShowTip();
	}

	private static void ClickyAnalytics_TrackLibraryModeToggled(bool on)
	{
		ClickyAnalytics.TrackLibraryModeToggled(on);
	}

	private void ShowTip()
	{
		if ((object)base.XamlRoot != null)
		{
			if ((object)popup == null)
			{
				popup = new Popup
				{
					Child = tooltip,
					IsHitTestVisible = false,
					XamlRoot = base.XamlRoot,
					ShouldConstrainToRootBounds = !tooltipBelow
				};
				tooltip.Opacity = 0.0;
			}
			tooltip.Measure(new Size(210.0, double.PositiveInfinity));
			Point point = TransformToVisual(null).TransformPoint(new Point(0f, 0f));
			popup.HorizontalOffset = point.X + diameter - 210.0;
			popup.VerticalOffset = (tooltipBelow ? (point.Y + diameter + 8.0) : (point.Y - tooltip.DesiredSize.Height - 8.0));
			popup.IsOpen = true;
			Anim.Opacity(tooltip, 1f, 0.16, Easing.EaseOut(Ui.Compositor));
		}
	}

	private void HideTip()
	{
		Popup popup = this.popup;
		if ((object)popup != null && popup.IsOpen)
		{
			this.popup.IsOpen = false;
		}
	}
}

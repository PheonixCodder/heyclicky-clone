using System;
using HeyClicky.App.Design;
using HeyClicky.App.Proactive;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class MeetingCountdownSurface : NotchSurface
{
	private readonly Grid view;

	private readonly TextBlock countdown;

	private readonly DateTimeOffset startsAt;

	private DispatcherQueueTimer? timer;

	public override FrameworkElement View => view;

	public MeetingCountdownSurface(NotchLiveEvent.MeetingCountdown e)
	{
		startsAt = e.StartsAt;
		Grid tile = CardParts.IconTile("\ue787", 16.0, DS.Hex(2450411u));
		TextBlock t = Ui.Text(e.Title, 12.5, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 1);
		countdown = Ui.Text(Line(), 10.5, FontWeights.Medium, DS.Colors.TextSecondary, TextAlignment.Left, 1);
		Typography.SetNumeralAlignment(countdown, FontNumeralAlignment.Tabular);
		StackPanel lines = new StackPanel
		{
			Spacing = 3.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)Ui.Shrink(t),
				(UIElement)countdown
			}
		};
		Pressable pressable = null;
		Uri url = e.JoinUrl;
		if ((object)url != null)
		{
			string text = e.ActionKind ?? "join";
			string text2 = ((text == "directions") ? "Get directions" : ((!(text == "open")) ? "Join" : "Open"));
			string text3 = url.Host.ToLowerInvariant();
			string text4 = ((!(text3 == "meet.google.com")) ? ((!(text3 == "zoom.us") && !text3.EndsWith(".zoom.us") && !(text3 == "zoom.com") && !text3.EndsWith(".zoom.com")) ? (((text3 == "teams.microsoft.com" || text3 == "teams.live.com") ? true : false) ? "ms-teams-logo" : null) : "zoom-logo") : "google-meet-logo");
			if (!(text == "join") || text4 == null)
			{
				pressable = ((!(text == "directions")) ? new GelActionButton(text2, "\uea0c", GelAppearance.PrimaryBlue, 34.0, 16.0, 94.0, 12.0, 11.5, 6.0) : GelActionButton.WithIcon(text2, MapMarker, GelAppearance.PrimaryBlue, 34.0, 16.0, 94.0, 12.0, 6.0));
			}
			else
			{
				Pressable pressable2 = new Pressable
				{
					Height = 34.0,
					MinWidth = 94.0
				};
				GelSurface gel = new GelSurface(GelAppearance.PrimaryBlue, 17.0);
				pressable2.Children.Add(gel);
				pressable2.HoverChanged += delegate(bool h)
				{
					gel.Hovered = h;
				};
				FrameworkElement frameworkElement = IntegrationLogos.Image(text4, 16.0, null);
				Ui.ClipRounded(frameworkElement, 3.5);
				StackPanel stackPanel = Ui.H(6.0, frameworkElement, Ui.Embossed(Ui.Text(text2, 12.0, FontWeights.SemiBold, DS.Black(0.85)), 0.65, 1.0));
				stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
				stackPanel.VerticalAlignment = VerticalAlignment.Center;
				stackPanel.Margin = new Thickness(16.0, 0.0, 16.0, 0.0);
				pressable2.Children.Add(stackPanel);
				pressable2.WithPressScale(0.97f, 0.12, easeOut: true);
				pressable = pressable2;
			}
			pressable.Tip(text2);
			pressable.Click += delegate
			{
				ProactiveCoordinator current = ProactiveCoordinator.Current;
				if (current != null)
				{
					current.JoinActiveMeeting();
				}
				else
				{
					base.Host.Actions.OpenUrl(url.ToString());
				}
			};
		}
		Pressable close = CardParts.PlainClose(delegate
		{
			ProactiveCoordinator current2 = ProactiveCoordinator.Current;
			if (current2 != null)
			{
				current2.DismissActiveMeeting();
			}
			else
			{
				base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MeetingCountdown));
			}
		});
		view = CardParts.HeadsUpRow(tile, lines, pressable, close);
	}

	private static FrameworkElement MapMarker(Color c)
	{
		bool flag = c.R == byte.MaxValue && c.G == byte.MaxValue && c.B == byte.MaxValue;
		Grid grid = new Grid
		{
			Width = 12.0,
			Height = 16.0
		};
		Geometry data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M6,16 L3,11.196 A6,6 0 1 1 9,11.196 Z");
		grid.Children.Add(new Path
		{
			Data = data,
			Fill = Ui.Brush(flag ? c : Color.FromArgb(byte.MaxValue, 235, 66, 54))
		});
		if (!flag)
		{
			grid.Children.Add(new Ellipse
			{
				Width = 4.5,
				Height = 4.5,
				Fill = Ui.Brush(Colors.White),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 3.75, 0.0, 0.0)
			});
		}
		return grid;
	}

	private string Line()
	{
		int num = (int)Math.Round((startsAt - DateTimeOffset.Now).TotalSeconds);
		if (num > 0)
		{
			return $"Starting in {num / 60}:{num % 60:00}";
		}
		int num2 = -num / 60;
		if (num2 < 1)
		{
			return "Starting now";
		}
		if (num2 >= 60)
		{
			return $"Started {num2 / 60}h {num2 % 60}m ago";
		}
		return $"Started {num2}m ago";
	}

	protected override void OnAttached()
	{
		timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		timer.Interval = TimeSpan.FromSeconds(1L);
		timer.Tick += delegate
		{
			countdown.Text = Line();
		};
		timer.Start();
	}

	public override void Detached()
	{
		timer?.Stop();
	}
}

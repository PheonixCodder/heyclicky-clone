using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchTabSwitcher : Grid
{
	private readonly Dictionary<NotchTab, Pressable> buttons = new Dictionary<NotchTab, Pressable>();

	private readonly Dictionary<NotchTab, FrameworkElement> icons = new Dictionary<NotchTab, FrameworkElement>();

	private readonly Border capsule = new Border
	{
		Width = 34.0,
		Height = 24.0,
		CornerRadius = new CornerRadius(12.0),
		Background = Ui.Brush(DS.White(0.14)),
		IsHitTestVisible = false,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private readonly Canvas capsuleLayer = new Canvas
	{
		IsHitTestVisible = false
	};

	private readonly Grid planSlot = new Grid
	{
		VerticalAlignment = VerticalAlignment.Center
	};

	private INotchHost? host;

	private NotchTabs? tabs;

	private NotchTab? selected;

	private BrandMark? mark;

	private Vector3 lastCapsule = new Vector3(float.NaN);

	public Grid PlanPillHost
	{
		set
		{
			planSlot.Children.Clear();
			planSlot.Children.Add(value);
		}
	}

	public NotchTabSwitcher()
	{
		base.ColumnSpacing = 6.0;
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		FrameworkElement item = Group(NotchTab.Home, NotchTab.Chat);
		FrameworkElement frameworkElement = Group(NotchTab.Settings);
		Grid.SetColumn(planSlot, 2);
		Grid.SetColumn(frameworkElement, 3);
		base.Children.Add(item);
		base.Children.Add(planSlot);
		base.Children.Add(frameworkElement);
		Grid.SetColumnSpan(capsuleLayer, 4);
		capsuleLayer.Children.Add(capsule);
		base.Children.Insert(0, capsuleLayer);
		ElementCompositionPreview.SetIsTranslationEnabled(capsule, value: true);
		base.LayoutUpdated += delegate
		{
			PlaceCapsule(animated: false, onlyIfMoved: true);
		};
	}

	public void Bind(INotchHost host, NotchTabs? tabs)
	{
		this.host = host;
		this.tabs = tabs;
		Select(tabs?.Selected ?? NotchTab.Home, animated: false);
	}

	private FrameworkElement Group(params NotchTab[] shown)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 2.0,
			Padding = new Thickness(2.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		foreach (NotchTab tab in shown)
		{
			stackPanel.Children.Add(Button(tab));
		}
		return new Border
		{
			CornerRadius = new CornerRadius(14.0),
			Background = Ui.Brush(DS.White(0.05)),
			Child = stackPanel,
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	private Pressable Button(NotchTab tab)
	{
		Pressable pressable = new Pressable
		{
			Width = 34.0,
			Height = 24.0
		};
		FrameworkElement frameworkElement;
		if (tab == NotchTab.Home)
		{
			mark = new BrandMark(16.0, DS.White(0.5), 1.35)
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			frameworkElement = mark;
		}
		else
		{
			TextBlock textBlock = Icons.Glyph((tab == NotchTab.Chat) ? "\ue8f2" : "\ue713", 11.0, DS.White(0.5));
			textBlock.FontWeight = FontWeights.SemiBold;
			frameworkElement = textBlock;
		}
		pressable.Children.Add(frameworkElement);
		icons[tab] = frameworkElement;
		buttons[tab] = pressable;
		pressable.HoverChanged += delegate
		{
			Tint(tab);
		};
		pressable.Tip(tab.Title());
		AutomationProperties.SetName(pressable, tab.Title());
		pressable.Click += delegate
		{
			if (tab == NotchTab.Home)
			{
				mark?.BlinkOnce();
			}
			if (tabs != null && tabs.Selected != tab)
			{
				if (tab == NotchTab.Settings)
				{
					tabs.SettingsSection = NotchSettingsSection.Account;
				}
				if (tab == NotchTab.Chat)
				{
					tabs.DropExpandedReply();
				}
				(host?.Services.Presenter as NotchPresenter)?.NoteTabPickedInsideOpenNotch();
				tabs.Select(tab);
			}
		};
		return pressable;
	}

	private void Tint(NotchTab tab)
	{
		Pressable pressable = buttons[tab];
		Color color = DS.White((selected == tab) ? 0.92 : (pressable.IsHovered ? 0.75 : 0.5));
		FrameworkElement frameworkElement = icons[tab];
		if (!(frameworkElement is BrandMark brandMark))
		{
			if (frameworkElement is TextBlock textBlock)
			{
				textBlock.Foreground = Ui.Brush(color);
			}
		}
		else
		{
			brandMark.SetStroke(color);
		}
		AutomationProperties.SetItemStatus(pressable, (selected == tab) ? "Selected" : "");
	}

	public void Select(NotchTab tab, bool animated)
	{
		NotchTab? notchTab = selected;
		selected = tab;
		foreach (NotchTab key in buttons.Keys)
		{
			Tint(key);
		}
		if (notchTab != tab)
		{
			PlaceCapsule(animated && notchTab.HasValue, onlyIfMoved: false);
		}
	}

	private void PlaceCapsule(bool animated, bool onlyIfMoved)
	{
		NotchTab? notchTab = selected;
		if (notchTab.HasValue)
		{
			NotchTab valueOrDefault = notchTab.GetValueOrDefault();
			if (buttons.TryGetValue(valueOrDefault, out Pressable value) && (object)value.XamlRoot != null && !(value.ActualWidth <= 0.0))
			{
				capsule.Opacity = 1.0;
				Point point = value.TransformToVisual(capsuleLayer).TransformPoint(new Point(0f, 0f));
				Vector3 vector = new Vector3((float)point.X, (float)point.Y, 0f);
				if (!onlyIfMoved || !(vector == lastCapsule))
				{
					lastCapsule = vector;
					if (animated && !Anim.ReduceMotion)
					{
						Anim.TranslationSpring(capsule, vector, new SwiftSpring(0.28, 0.86));
					}
					else
					{
						capsule.Translation = vector;
					}
				}
				return;
			}
		}
		capsule.Opacity = (buttons.ContainsKey(selected ?? NotchTab.Document) ? capsule.Opacity : 0.0);
	}
}

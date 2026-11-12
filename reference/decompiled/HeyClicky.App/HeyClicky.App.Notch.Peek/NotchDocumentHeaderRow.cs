using System;
using System.IO;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchDocumentHeaderRow : Grid
{
	public NotchDocumentHeaderRow(INotchHost host)
	{
		base.ColumnSpacing = 8.0;
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
		NotchTabs tabs = (host.Services.Presenter as NotchPresenter)?.Tabs;
		string path = tabs?.DocumentPath;
		if (path != null)
		{
			NotchDocumentStyle notchDocumentStyle = NotchDocumentStyle.For(path);
			Border border = new Border
			{
				Width = 18.0,
				Height = 18.0,
				CornerRadius = new CornerRadius(5.0),
				Background = Ui.Brush(notchDocumentStyle.Tint),
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock textBlock = Icons.Glyph(notchDocumentStyle.Glyph, 9.5, Colors.White);
			textBlock.FontWeight = FontWeights.SemiBold;
			border.Child = textBlock;
			TextBlock textBlock2 = Ui.Text(Path.GetFileName(path), 12.5, FontWeights.SemiBold, HomeTheme.PrimaryText, TextAlignment.Left, 1);
			textBlock2.TextTrimming = TextTrimming.CharacterEllipsis;
			TextBlock textBlock3 = Ui.Text(notchDocumentStyle.KindLabel, 11.5, FontWeights.Normal, HomeTheme.SecondaryText, TextAlignment.Left, 1);
			Grid grid = new Grid
			{
				ColumnSpacing = 7.0,
				MaxWidth = 270.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			Grid.SetColumn(textBlock2, 1);
			Grid.SetColumn(textBlock3, 2);
			grid.Children.Add(border);
			grid.Children.Add(textBlock2);
			grid.Children.Add(textBlock3);
			grid.Tip(path);
			base.Children.Add(grid);
			StackPanel stackPanel = Ui.H(0.0, HeaderButton("\ue8a7", "Open in its app", delegate
			{
				NotchPeekSurface.PeekAction("document_open_in_app");
				(host.Actions as INotchActions)?.OpenInItsApp(path, tabs.DocumentThreadId);
			}), HeaderButton("\ue8b7", "Show in Explorer", delegate
			{
				(host.Actions as INotchActions)?.RevealInExplorer(path);
			}), HeaderButton("\ue711", "Close (esc)", delegate
			{
				tabs.CloseDocument();
			}));
			stackPanel.VerticalAlignment = VerticalAlignment.Center;
			Grid.SetColumn(stackPanel, 2);
			base.Children.Add(stackPanel);
		}
	}

	private static Pressable HeaderButton(string glyph, string tip, Action action)
	{
		Pressable p = new Pressable
		{
			Width = 28.0,
			Height = 28.0,
			CornerRadius = new CornerRadius(7.0)
		};
		TextBlock icon = Icons.Glyph(glyph, 12.5, HomeTheme.SecondaryText);
		icon.FontWeight = FontWeights.Medium;
		p.Children.Add(icon);
		p.HoverChanged += delegate(bool h)
		{
			icon.Foreground = Ui.Brush(h ? HomeTheme.PrimaryText : HomeTheme.SecondaryText);
			p.Background = Ui.Brush(DS.White(h ? 0.08 : 0.0));
		};
		p.Click += action;
		p.Tip(tip);
		AutomationProperties.SetName(p, tip);
		return p;
	}
}

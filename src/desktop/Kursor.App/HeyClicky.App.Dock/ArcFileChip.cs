using System;
using System.Globalization;
using System.IO;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Dock;

public sealed class ArcFileChip : Pressable
{
	public ArcFileChip(string entry, string? workspace, Action open)
	{
		bool flag = CardReadyOutput.IsWebLink(entry);
		string text = (flag ? entry : DockFiles.Resolve(entry, workspace));
		DockFileStyle dockFileStyle = DockFileStyle.For(flag ? entry : text);
		object text2;
		if (!flag)
		{
			text2 = Path.GetFileNameWithoutExtension(text);
		}
		else
		{
			ClickyLinkName.Name name = ClickyLinkName.For(entry);
			if ((object)name == null)
			{
				text2 = "Link";
			}
			else
			{
				string thing = name.Thing;
				text2 = ((thing != null) ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(thing) : name.Site);
			}
		}
		base.Height = 38.0;
		Border plate = new Border
		{
			CornerRadius = new CornerRadius(10.0),
			Background = Ui.Brush(Colors.White),
			BorderBrush = Ui.Brush(DS.Black(0.1)),
			BorderThickness = new Thickness(1.0)
		};
		base.Children.Add(new ShadowLayer(10.0, DS.Black(0.08), 3.0, 1.0));
		base.Children.Add(plate);
		Border border = new Border
		{
			Width = 26.0,
			Height = 26.0,
			CornerRadius = new CornerRadius(7.0),
			Background = Ui.Brush(dockFileStyle.Tint),
			Child = Icons.Glyph(dockFileStyle.Glyph, 13.0, Colors.White)
		};
		TextBlock textBlock = Ui.Text((string)text2, 16.0, FontWeights.SemiBold, ArcColors.Ink.WithAlpha(0.85));
		textBlock.MaxWidth = 130.0;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		StackPanel stackPanel = Ui.H(8.0, border, textBlock);
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Padding = new Thickness(6.0, 0.0, 12.0, 0.0);
		base.Children.Add(stackPanel);
		base.HoverChanged += delegate(bool h)
		{
			plate.BorderBrush = Ui.Brush(DS.Black(h ? 0.22 : 0.1));
		};
		base.Click += open;
		ToolTipService.SetToolTip(this, flag ? entry : text);
	}
}

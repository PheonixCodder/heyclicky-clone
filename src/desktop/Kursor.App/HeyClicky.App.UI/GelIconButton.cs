using System.Numerics;
using HeyClicky.App.Design;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.UI;

public sealed class GelIconButton : Pressable
{
	private readonly GelSurface gel;

	private readonly TextBlock glyph;

	private readonly TextBlock? highlight;

	private readonly GelAppearance appearance;

	public GelIconButton(string glyphText, GelAppearance appearance, double symbolSize, double diameter, string tip)
	{
		GelIconButton gelIconButton = this;
		this.appearance = appearance;
		base.Width = diameter;
		base.Height = diameter;
		bool flag = appearance == GelAppearance.SecondaryOutline;
		if (!flag)
		{
			base.Children.Add(new ShadowLayer(diameter / 2.0, DS.Black(0.11), 5.0, 4.0));
		}
		gel = new GelSurface(appearance, diameter / 2.0);
		base.Children.Add(gel);
		if (!flag)
		{
			highlight = Icons.Glyph(glyphText, symbolSize, DS.White(0.8));
			highlight.FontWeight = FontWeights.Bold;
			highlight.RenderTransform = new TranslateTransform
			{
				Y = 1.0
			};
			base.Children.Add(highlight);
		}
		glyph = Icons.Glyph(glyphText, symbolSize, flag ? DS.White(0.86) : DS.Black(0.85));
		glyph.FontWeight = FontWeights.Bold;
		base.Children.Add(glyph);
		base.HoverChanged += delegate(bool h)
		{
			gelIconButton.gel.Hovered = h && gelIconButton.Enabled;
			if (gelIconButton.Enabled)
			{
				Anim.Scale(gelIconButton, h ? 1.05f : 1f, 0.15, null, new Vector3((float)diameter / 2f, (float)diameter / 2f, 0f));
			}
		};
		this.Tip(tip);
	}

	public void SetEnabled(bool on)
	{
		base.Enabled = on;
		gel.Enabled = on;
		bool flag = appearance == GelAppearance.SecondaryOutline;
		glyph.Foreground = Ui.Brush(flag ? DS.White(on ? 0.86 : 0.4) : (on ? DS.Black(0.85) : DS.Colors.GelDisabledContent));
		if ((object)highlight != null)
		{
			highlight.Foreground = Ui.Brush(DS.White(on ? 0.8 : 0.18));
		}
	}
}

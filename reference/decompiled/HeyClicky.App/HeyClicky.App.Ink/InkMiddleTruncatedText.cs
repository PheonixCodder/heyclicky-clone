using System;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class InkMiddleTruncatedText : Panel, IInkBaseline
{
	private readonly string text;

	private readonly TextBlock block;

	public double? FirstBaseline
	{
		get
		{
			if (!(block.BaselineOffset > 0.0))
			{
				return null;
			}
			return block.BaselineOffset;
		}
	}

	public InkMiddleTruncatedText(string text, PaperFont font, Color color)
	{
		this.text = text;
		block = font.Apply(new TextBlock
		{
			Text = text,
			Foreground = Ui.Brush(color),
			TextWrapping = TextWrapping.NoWrap
		});
		block.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
		block.LineHeight = font.Size * 1.193;
		base.Children.Add(block);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		block.Text = this.text;
		block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		if (double.IsInfinity(availableSize.Width) || block.DesiredSize.Width <= availableSize.Width)
		{
			return block.DesiredSize;
		}
		string[] array = this.text.Characters();
		int num = 0;
		int num2 = array.Length;
		string text = "…";
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			int count = (num3 + 1) / 2;
			int num4 = num3 / 2;
			string text2 = string.Concat(string.Concat(array.Take(count)), "…", string.Concat(array.Skip(array.Length - num4)));
			block.Text = text2;
			block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			if (block.DesiredSize.Width <= availableSize.Width)
			{
				text = text2;
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		block.Text = text;
		block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return new Size(Math.Min(block.DesiredSize.Width, availableSize.Width), block.DesiredSize.Height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		block.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
		return finalSize;
	}
}

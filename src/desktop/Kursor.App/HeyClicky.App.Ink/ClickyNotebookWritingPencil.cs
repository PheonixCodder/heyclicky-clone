using System;
using System.Numerics;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyNotebookWritingPencil : StackPanel
{
	public ClickyNotebookWritingPencil(ClickyHomeSpreadPaper paper)
	{
		base.Orientation = Orientation.Horizontal;
		base.Spacing = 6.0;
		Grid holder = new Grid
		{
			Width = 18.0,
			Height = 18.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock item = ClickyInkIcon.Glyph("pencil", 15.0, paper.Ink.Opacity(0.75), bold: true);
		holder.Children.Add(item);
		base.Children.Add(holder);
		TextBlock textBlock = PaperText.Line("writing…", ClickyPaperFonts.Hand(19.0), paper.Quiet);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		base.Children.Add(textBlock);
		base.Loaded += delegate
		{
			holder.CenterPoint = new Vector3(9f, 9f, 0f);
			if (Anim.ReduceMotion || ClickyHomeRendering.IsOffscreen)
			{
				holder.Rotation = -6f;
				holder.Translation = new Vector3(0f, 1f, 0f);
			}
			else
			{
				static double Phase(double t)
				{
					return (1.0 - Math.Cos(t * Math.PI / 0.32)) / 2.0;
				}
				Anim.LoopScalar(holder, "Rotation", 0.64, (double t) => (float)(-6.0 - 14.0 * Phase(t)));
				Anim.LoopVector3(holder, "Translation", 0.64, (double t) => new Vector3(0f, (float)(1.0 - 3.0 * Phase(t)), 0f));
			}
		};
	}
}

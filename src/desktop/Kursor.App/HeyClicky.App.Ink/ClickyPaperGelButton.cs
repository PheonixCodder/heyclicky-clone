using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickyPaperGelButton : Pressable
{
	private readonly TextBlock label;

	private readonly StackPanel row;

	private readonly Grid scaled = new Grid();

	private readonly ClickyPaperGel gel;

	private readonly double fontSize;

	public string Title
	{
		get
		{
			return label.Text;
		}
		set
		{
			label.Text = value;
			UIElementCollection children = row.Children;
			if (!(children[children.Count - 1] is Grid grid))
			{
				return;
			}
			foreach (TextBlock item in grid.Children.OfType<TextBlock>())
			{
				item.Text = value;
			}
		}
	}

	public ClickyPaperGelButton(string title, Action? action = null, ClickyPaperGel gel = ClickyPaperGel.Blue, double height = 26.0, double horizontalPadding = 13.0, double fontSize = 12.0, string? systemImage = null)
	{
		this.gel = gel;
		this.fontSize = fontSize;
		base.Height = height;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		scaled.Children.Add(new ClickyPaperGelPlate(gel));
		label = ClickyPaperFonts.Typed(fontSize, FontWeights.SemiBold).Apply(new TextBlock
		{
			Text = title,
			Foreground = Ui.Brush(gel.Words()),
			TextWrapping = TextWrapping.NoWrap
		});
		row = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 4.0,
			Margin = new Thickness(horizontalPadding, 0.0, horizontalPadding, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		SetSymbol(systemImage);
		row.Children.Add(Ui.Embossed(label, 0.6, 1.0));
		scaled.Children.Add(row);
		base.Children.Add(scaled);
		if (action != null)
		{
			base.Click += action;
		}
		base.HoverChanged += delegate
		{
			Squash(0.12);
		};
		base.PressedChanged += delegate
		{
			Squash(0.08);
		};
		AutomationProperties.SetName(this, title);
	}

	private void Squash(double seconds)
	{
		scaled.CenterPoint = new Vector3((float)base.ActualWidth / 2f, (float)base.ActualHeight / 2f, 0f);
		Anim.Scale(scaled, base.IsPressed ? 0.96f : (base.IsHovered ? 1.03f : 1f), seconds, Easing.EaseOut(Ui.Compositor), null);
	}

	public void AnimateTitle(string title, string? systemImage, bool spring)
	{
		double actualWidth = base.ActualWidth;
		Title = title;
		SetSymbol(systemImage);
		AutomationProperties.SetName(this, title);
		if (actualWidth <= 0.0 || Anim.Instant || Anim.ReduceMotion)
		{
			return;
		}
		Measure(new Size(double.PositiveInfinity, base.ActualHeight));
		double width = base.DesiredSize.Width;
		if (spring)
		{
			row.Opacity = 0.0;
			Anim.OpacitySpring(row, 1f, new SwiftSpring(0.3, 0.8));
		}
		else
		{
			row.Opacity = 0.0;
			Anim.Opacity(row, 1f, 0.25, Easing.EaseOut(Ui.Compositor));
		}
		if (!(Math.Abs(width - actualWidth) < 0.5))
		{
			DoubleAnimation doubleAnimation = new DoubleAnimation
			{
				From = actualWidth,
				To = width,
				EnableDependentAnimation = true,
				Duration = TimeSpan.FromSeconds(spring ? 0.35 : 0.25),
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				}
			};
			Storyboard.SetTarget(doubleAnimation, this);
			Storyboard.SetTargetProperty(doubleAnimation, "Width");
			Storyboard storyboard = new Storyboard
			{
				Children = { (Timeline)doubleAnimation }
			};
			storyboard.Completed += delegate
			{
				storyboard.Stop();
			};
			storyboard.Begin();
		}
	}

	public void SetSymbol(string? systemImage)
	{
		if (row.Children.Count > 0 && row.Children[0] is TextBlock { Tag: string tag } textBlock && tag == "symbol")
		{
			row.Children.Remove(textBlock);
		}
		if (systemImage != null)
		{
			TextBlock textBlock2 = ClickyInkIcon.Glyph(systemImage, fontSize - 1.0, gel.Words(), bold: true);
			if ((object)textBlock2 != null)
			{
				textBlock2.Tag = "symbol";
				row.Children.Insert(0, textBlock2);
			}
		}
	}
}

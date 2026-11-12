using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class PlanStepRow : Grid
{
	private readonly PlanTickBox box;

	private readonly TextBlock text;

	private readonly Rectangle strike;

	private readonly Color ink;

	private bool isDone;

	private bool isCurrent;

	private readonly bool chat;

	public PlanStepRow(string words, bool isDone, bool isCurrent, Color ink, bool chat = false)
	{
		this.ink = ink;
		this.chat = chat;
		base.ColumnSpacing = (chat ? 9 : 11);
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		box = (chat ? new PlanTickBox(isDone, isCurrent, 15.0, 4.0, ink)
		{
			VerticalAlignment = VerticalAlignment.Center
		} : new PlanTickBox(isDone, isCurrent, 20.0, 5.0, ink, todoIsCircle: false)
		{
			VerticalAlignment = VerticalAlignment.Center
		});
		text = Ui.Text(words, chat ? 12.5 : 17.0, FontWeights.Medium, ink);
		text.VerticalAlignment = VerticalAlignment.Center;
		if (chat)
		{
			text.TextWrapping = TextWrapping.NoWrap;
			text.TextTrimming = TextTrimming.CharacterEllipsis;
		}
		strike = new Rectangle
		{
			Height = 1.2,
			Fill = Ui.Brush(ink.WithAlpha(chat ? 0.3 : 0.35)),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false
		};
		Grid grid = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)text,
				(UIElement)strike
			}
		};
		grid.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			strike.Width = e.NewSize.Width;
			strike.Margin = new Thickness(0.0, e.NewSize.Height * 0.54 - 0.6, 0.0, 0.0);
		};
		Grid.SetColumn(grid, 1);
		base.Children.Add(box);
		base.Children.Add(grid);
		this.isDone = !isDone;
		Set(isDone, isCurrent, animated: false);
	}

	public void Set(bool done, bool current, bool animated = true)
	{
		bool num = done != isDone;
		isDone = done;
		isCurrent = current;
		box.Set(done, current, animated);
		text.FontWeight = (current ? FontWeights.SemiBold : FontWeights.Medium);
		text.Foreground = Ui.Brush(ink.WithAlpha(done ? 0.5 : ((!current) ? (chat ? 0.65 : 0.7) : (chat ? 0.9 : 0.95))));
		strike.CenterPoint = new Vector3(0f, 0f, 0f);
		if (num)
		{
			if (!animated || Anim.ReduceMotion)
			{
				strike.Scale = new Vector3(done ? 1 : 0, 1f, 1f);
				return;
			}
			strike.Scale = new Vector3((!done) ? 1 : 0, 1f, 1f);
			Compositor compositor = Ui.Compositor;
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			vector3KeyFrameAnimation.InsertKeyFrame(1f, new Vector3(done ? 1 : 0, 1f, 1f), Easing.EaseOut(compositor));
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.26);
			vector3KeyFrameAnimation.DelayTime = TimeSpan.FromSeconds(0.08);
			vector3KeyFrameAnimation.Target = "Scale";
			strike.StartAnimation(vector3KeyFrameAnimation);
		}
	}
}

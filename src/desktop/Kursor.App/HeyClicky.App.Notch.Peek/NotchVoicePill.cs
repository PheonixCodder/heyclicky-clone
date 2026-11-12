using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Surfaces;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Peek;

public static class NotchVoicePill
{
	public static void Update(Grid layer, (NotchActivityPhase Phase, string? Label)? phase, INotchHost host)
	{
		Grid grid = layer.Children.OfType<Grid>().FirstOrDefault((Grid g) => g.Tag as string == "voice-pill");
		if (phase.HasValue)
		{
			(NotchActivityPhase, string) valueOrDefault = phase.GetValueOrDefault();
			ActivityPhase phase2 = (ActivityPhase)Enum.Parse(typeof(ActivityPhase), valueOrDefault.Item1.ToString());
			if (grid?.Children.LastOrDefault() is Border { Child: Grid { Tag: ActivitySurface tag } } && tag.Matches(phase2, valueOrDefault.Item2 ?? ""))
			{
				return;
			}
			if ((object)grid != null)
			{
				if (grid.Children.LastOrDefault() is Border { Child: Grid { Tag: ActivitySurface tag2 } })
				{
					tag2.DetachPill();
				}
				layer.Children.Remove(grid);
			}
			ActivitySurface activitySurface = new ActivitySurface(phase2, valueOrDefault.Item2 ?? "", recording: false, () => DS.CursorColor(host.Services.Settings.CursorColorHex), 12.0);
			activitySurface.AttachAsPill(host.Services);
			Grid child3 = new Grid
			{
				Height = 24.0,
				Tag = activitySurface,
				Children = { (UIElement)activitySurface.View }
			};
			Border item = new Border
			{
				Background = Ui.Brush(Colors.Black),
				BorderBrush = Ui.Brush(DS.White(0.14)),
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(12.0),
				Child = child3
			};
			Grid pill = new Grid
			{
				Tag = "voice-pill",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top
			};
			pill.Children.Add(new ShadowLayer(12.0, DS.Black(0.45), 10.0, 3.0));
			pill.Children.Add(item);
			float num = 0.86f;
			pill.Scale = new Vector3(num, num, 1f);
			pill.SizeChanged += delegate(object _, SizeChangedEventArgs e)
			{
				pill.CenterPoint = new Vector3((float)e.NewSize.Width / 2f, 0f, 0f);
			};
			layer.Children.Add(pill);
			if (Anim.ReduceMotion)
			{
				pill.Opacity = 0.0;
				Anim.Opacity(pill, 1f, 0.15, Easing.EaseOut(Ui.Compositor));
				return;
			}
			SwiftSpring spring = new SwiftSpring(0.32, 0.82);
			pill.Opacity = 0.0;
			pill.Scale = new Vector3(num * 0.8f, num * 0.8f, 1f);
			Anim.ScaleSpring(pill, num, spring, null);
			Anim.OpacitySpring(pill, 1f, spring);
		}
		else if ((object)grid != null)
		{
			grid.Tag = "voice-pill-leaving";
			if (grid.Children.LastOrDefault() is Border { Child: Grid { Tag: ActivitySurface tag3 } })
			{
				tag3.DetachPill();
			}
			Leave(layer, grid);
		}
	}

	private static void Leave(Grid layer, Grid pill)
	{
		SwiftSpring spring = new SwiftSpring(0.32, 0.82);
		float num = 0.86f;
		if (!Anim.ReduceMotion)
		{
			Anim.ScaleSpring(pill, num * 0.8f, spring, null);
		}
		Anim.Opacity(pill, 0f, Anim.ReduceMotion ? 0.15 : 0.2);
		DispatcherQueueTimer dispatcherQueueTimer = layer.DispatcherQueue.CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(0.3);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			layer.Children.Remove(pill);
		};
		dispatcherQueueTimer.Start();
	}
}

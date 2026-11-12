using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Dock;

internal sealed class ChatPlanFold : Grid
{
	private readonly ClickyArcPlan plan;

	private readonly HashSet<int> doneBefore;

	private readonly Action onFinished;

	private readonly List<PlanStepRow> rows = new List<PlanStepRow>();

	private readonly StackPanel ticking = new StackPanel
	{
		Spacing = 9.0
	};

	private readonly TextBlock answer;

	private readonly List<DispatcherQueueTimer> timers = new List<DispatcherQueueTimer>();

	public ChatPlanFold(ClickyArcPlan plan, HashSet<int> doneBefore, string line, Color ink, Color wash, Action onFinished)
	{
		this.plan = plan;
		this.doneBefore = doneBefore;
		this.onFinished = onFinished;
		base.MaxWidth = 420.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.RequestedTheme = ElementTheme.Light;
		base.Children.Add(ChatBlockViews.BubblePlate(Ui.Vertical((0.0, DS.Hex(15526893u)), (1.0, DS.Hex(16250871u))), null, tail: true, mirrored: false));
		base.Children.Add(ChatBlockViews.BubblePlate(Ui.Brush(wash.WithAlpha(0.45)), null, tail: true, mirrored: false));
		for (int i = 0; i < plan.Steps.Count; i++)
		{
			PlanStepRow item = new PlanStepRow(plan.Steps[i].Text, doneBefore.Contains(i), isCurrent: false, ink, chat: true);
			rows.Add(item);
			ticking.Children.Add(item);
		}
		answer = Ui.Text(line, 12.5, FontWeights.SemiBold, ink.WithAlpha(0.9), TextAlignment.Left, 3);
		answer.Opacity = 0.0;
		answer.Visibility = Visibility.Collapsed;
		Grid item2 = new Grid
		{
			Padding = new Thickness(14.0, 12.0, 14.0, 19.0),
			Children = 
			{
				(UIElement)ticking,
				(UIElement)answer
			}
		};
		base.Children.Add(item2);
		base.Loaded += delegate
		{
			Play();
		};
		base.Unloaded += delegate
		{
			foreach (DispatcherQueueTimer timer in timers)
			{
				timer.Stop();
			}
			timers.Clear();
		};
	}

	private void After(double seconds, Action action)
	{
		timers.Add(DockTimer.After(seconds, action));
	}

	private void Play()
	{
		if (timers.Count > 0)
		{
			return;
		}
		List<int> list = (from i in Enumerable.Range(0, plan.Steps.Count)
			where !doneBefore.Contains(i)
			select i).ToList();
		for (int j = 0; j < list.Count; j++)
		{
			int index = list[j];
			After((double)j * 0.09 + 0.001, delegate
			{
				rows[index].Set(done: true, current: false);
			});
		}
		double num = 0.35 + (double)list.Count * 0.09 + 0.35;
		After(num, delegate
		{
			SwiftSpring spring = new SwiftSpring(0.42, 0.82);
			Anim.OpacitySpring(ticking, 0f, spring);
			Anim.TranslationSpring(ticking, new Vector3(0f, -6f, 0f), spring);
			answer.Visibility = Visibility.Visible;
			answer.Translation = new Vector3(0f, 8f, 0f);
			Anim.OpacitySpring(answer, 1f, spring);
			Anim.TranslationSpring(answer, Vector3.Zero, spring);
			After(0.45, delegate
			{
				ticking.Visibility = Visibility.Collapsed;
			});
		});
		After(num + 1.35, delegate
		{
			SwiftSpring spring2 = new SwiftSpring(0.42, 0.86);
			base.CenterPoint = new Vector3((float)base.ActualWidth / 2f, 0f, 0f);
			Anim.ScaleSpring(this, 0.97f, spring2, null);
			Anim.OpacitySpring(this, 0f, spring2);
		});
		After(num + 1.35 + 0.12, onFinished);
	}
}

using System;
using HeyClicky.App.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public sealed class ClickyInkTimeline : Grid
{
	private readonly Func<DateTimeOffset, FrameworkElement> build;

	private readonly DispatcherQueue dispatcher;

	private DispatcherQueueTimer? timer;

	private FrameworkElement? shown;

	private TimeSpan? interval;

	private bool isLoaded;

	public TimeSpan? MinimumInterval
	{
		get
		{
			return interval;
		}
		set
		{
			if (!(interval == value))
			{
				interval = value;
				Draw();
				Schedule();
			}
		}
	}

	public ClickyInkTimeline(Func<DateTimeOffset, FrameworkElement> build, TimeSpan? minimumInterval)
	{
		this.build = build;
		dispatcher = DispatcherQueue.GetForCurrentThread();
		interval = minimumInterval;
		Draw();
		base.Loaded += delegate
		{
			isLoaded = true;
			Schedule();
		};
		base.Unloaded += delegate
		{
			isLoaded = false;
			StopTimer();
		};
	}

	public static TimeSpan? Interval(double? seconds)
	{
		if (seconds.HasValue)
		{
			double valueOrDefault = seconds.GetValueOrDefault();
			return TimeSpan.FromSeconds(valueOrDefault);
		}
		return null;
	}

	public void Refresh()
	{
		Draw();
	}

	private void Draw()
	{
		DateTimeOffset now = DateTimeOffset.Now;
		if (shown is IClickyInkClocked clickyInkClocked)
		{
			clickyInkClocked.Render(now);
			return;
		}
		FrameworkElement frameworkElement = build(now);
		if ((object)frameworkElement != shown)
		{
			base.Children.Clear();
			base.Children.Add(frameworkElement);
			shown = frameworkElement;
		}
	}

	private void Schedule()
	{
		if (isLoaded)
		{
			TimeSpan? timeSpan = interval;
			if (timeSpan.HasValue && !Anim.ReduceMotion)
			{
				TimeSpan value = interval.Value;
				if ((object)timer == null)
				{
					timer = dispatcher.CreateTimer();
					timer.IsRepeating = true;
					timer.Tick += delegate
					{
						Draw();
					};
				}
				timer.Interval = value;
				if (!timer.IsRunning)
				{
					timer.Start();
				}
				return;
			}
		}
		StopTimer();
	}

	private void StopTimer()
	{
		timer?.Stop();
	}
}

using System;
using System.Collections.Generic;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Ink;

public static class ClickyInkRoutineEditor
{
	private static readonly Dictionary<string, bool> pausedOverrides = new Dictionary<string, bool>();

	private static readonly Dictionary<string, int> intervalOverrides = new Dictionary<string, int>();

	private static readonly HashSet<string> startedRunIDs = new HashSet<string>();

	public static FrameworkElement Create(IReadOnlyList<ClickyInkRoutineControl> routines, ClickyInkStyle style, ClickyInkActions actions, Action? changed = null)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0,
			Margin = new Thickness(14.0, 12.0, 14.0, 12.0)
		};
		for (int i = 0; i < routines.Count; i++)
		{
			if (i > 0)
			{
				stackPanel.Children.Add(new Border
				{
					Height = 1.0,
					Background = Ui.Brush(style.Ink.Opacity(0.08))
				});
			}
			stackPanel.Children.Add(Row(routines[i], style, actions, changed));
		}
		return new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Children = 
			{
				(UIElement)ClickyShapeView.Fill(ClickyShape.RoundedRectangle(9.0), style.Ink.Opacity(0.035)),
				(UIElement)stackPanel,
				(UIElement)ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(9.0), new PaperStroke(style.Ink.Opacity(0.09)))
			}
		};
	}

	private static FrameworkElement Row(ClickyInkRoutineControl routine, ClickyInkStyle style, ClickyInkActions actions, Action? changed)
	{
		bool value2;
		bool flag = !(pausedOverrides.TryGetValue(routine.Id, out value2) ? value2 : routine.IsPaused);
		int value3;
		int num = (intervalOverrides.TryGetValue(routine.Id, out value3) ? value3 : routine.IntervalMinutes);
		bool hasStarted = startedRunIDs.Contains(routine.Id) || routine.IsRunning;
		InkTextView inkTextView = InkTextView.Plain((routine.Title.Length == 0) ? "Routine" : routine.Title, ClickyPaperFonts.Typed(13.5, FontWeights.SemiBold), style.Ink).With(delegate(InkTextView t)
		{
			t.LineLimit = 2;
		});
		InkTextView content = InkTextView.Plain(flag ? "On" : "Paused", ClickyPaperFonts.Typed(11.5, FontWeights.Medium), style.Quiet);
		FrameworkElement frameworkElement = MiniSwitch(flag, flag ? ("Pause " + routine.Title) : ("Turn " + routine.Title + " back on"), delegate(bool value)
		{
			pausedOverrides[routine.Id] = !value;
			actions.SetRoutinePaused(routine.Id, !value);
			changed?.Invoke();
		});
		ClickyInkHStack item = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, inkTextView, new ClickyInkSpacer(), InkDashboardLayout.Fixed(content), frameworkElement);
		InkTextView item2 = InkTextView.Plain("HOW OFTEN", ClickyPaperFonts.Typed(9.5, FontWeights.Bold), style.Quiet).With(delegate(InkTextView t)
		{
			t.Tracking = 1.0;
		});
		ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
		{
			Spacing = 6.0,
			LineSpacing = 6.0,
			Opacity = (flag ? 1.0 : 0.55)
		};
		foreach (int item3 in ClickyInkDashboardViewMath.RoutineIntervalOptions(num))
		{
			bool isChosen = item3 == num;
			int m = item3;
			clickyNotebookFlow.Children.Add(new ClickyPaperGelButton(ClickyInkRoutineControl.CadenceWords(item3), delegate
			{
				if (!isChosen)
				{
					intervalOverrides[routine.Id] = m;
					actions.SetRoutineInterval(routine.Id, m);
					changed?.Invoke();
				}
			}, (!isChosen) ? ClickyPaperGel.White : ClickyPaperGel.Blue, 24.0, 10.0, 11.5, isChosen ? "checkmark" : null));
		}
		ClickyPaperGelButton clickyPaperGelButton = new ClickyPaperGelButton(hasStarted ? "Running" : "Run now", delegate
		{
			if (!hasStarted && actions.RunRoutineNow(routine.Id))
			{
				startedRunIDs.Add(routine.Id);
				changed?.Invoke();
			}
		}, ClickyPaperGel.White, 24.0, 10.0, 11.5, hasStarted ? "checkmark" : "play.fill")
		{
			Enabled = !hasStarted
		};
		ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center, clickyPaperGelButton);
		if (routine.RunCount > 0)
		{
			clickyInkHStack.Children.Add(InkTextView.Plain(ClickyInkDashboardViewMath.RunCountWords(routine.RunCount), ClickyPaperFonts.Typed(11.0, null), style.Quiet));
		}
		clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
		return new StackPanel
		{
			Spacing = 9.0,
			Children = 
			{
				(UIElement)item,
				(UIElement)item2,
				(UIElement)clickyNotebookFlow,
				(UIElement)clickyInkHStack
			}
		};
	}

	private static FrameworkElement MiniSwitch(bool isOn, string accessibilityLabel, Action<bool> set)
	{
		Pressable obj = new Pressable
		{
			Width = 26.0,
			Height = 15.0,
			AccessibleToggleState = isOn,
			VerticalAlignment = VerticalAlignment.Center
		};
		AutomationProperties.SetName(obj, accessibilityLabel);
		ClickyShapeView item = ClickyShapeView.Fill(ClickyShape.Capsule, isOn ? PaperColor.Hex("#007AFF") : PaperColor.Black.Opacity(0.1));
		Grid item2 = new Grid
		{
			Width = 13.0,
			Height = 13.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(isOn ? 12.0 : 1.0, 0.0, 0.0, 0.0),
			Children = 
			{
				(UIElement)ClickyShapeView.Fill(ClickyShape.Circle, PaperColor.White),
				(UIElement)ClickyShapeView.StrokeBorder(ClickyShape.Circle, new PaperStroke(PaperColor.Black.Opacity(0.12), 0.5))
			}
		};
		obj.Children.Add(item);
		obj.Children.Add(item2);
		obj.Click += delegate
		{
			set(!isOn);
		};
		return obj;
	}
}

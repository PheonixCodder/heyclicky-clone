using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class RestingMarkSurface : NotchSurface
{
	private readonly Grid root = new Grid
	{
		Margin = new Thickness(14.0, 0.0, 14.0, 0.0),
		ColumnSpacing = 8.0,
		IsHitTestVisible = false
	};

	private readonly TextBlock label;

	private readonly ComputerMark mark = new ComputerMark
	{
		VerticalAlignment = VerticalAlignment.Center
	};

	public override FrameworkElement View => root;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Center;

	public RestingMarkSurface()
	{
		root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star),
			MinWidth = 6.0
		});
		root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		label = Ui.Semibold("Working", 11.0, DS.Colors.TextPrimary);
		label.VerticalAlignment = VerticalAlignment.Center;
		Typography.SetNumeralAlignment(label, FontNumeralAlignment.Tabular);
		root.Children.Add(label);
		Grid.SetColumn(mark, 2);
		root.Children.Add(mark);
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		if (p is NotchPresentation.LiveEvent liveEvent)
		{
			return liveEvent.Event is NotchLiveEvent.RestingMark;
		}
		return false;
	}

	public override void Update(NotchPresentation p)
	{
		Refresh();
	}

	protected override void OnAttached()
	{
		base.Host.BindToEdges(label, -1.0, 0.5);
		base.Host.BindToEdges(mark, 1.0, 0.5);
		NotchExtensions.RestingMarkChanged += OnChanged;
		Refresh();
	}

	public override void Detached()
	{
		NotchExtensions.RestingMarkChanged -= OnChanged;
	}

	private void OnChanged()
	{
		root.DispatcherQueue.TryEnqueue(Refresh);
	}

	private void Refresh()
	{
		NotchRestingMarkState notchRestingMarkState = NotchExtensions.RestingMarkState?.Invoke() ?? new NotchRestingMarkState(1, "idle");
		label.Text = ((notchRestingMarkState.WorkingCount > 1) ? $"{notchRestingMarkState.WorkingCount} working" : "Working");
		ComputerMark computerMark = mark;
		bool isWorking = notchRestingMarkState.WorkingCount > 0;
		string alertPhase = notchRestingMarkState.AlertPhase;
		string attention = ((alertPhase == "needsYou") ? "hand" : ((!(alertPhase == "failed")) ? null : "warning"));
		computerMark.Set(isWorking, attention);
		List<string> list = new List<string> { "HeyClicky" };
		if (notchRestingMarkState.WorkingCount > 0)
		{
			list.Add($"{notchRestingMarkState.WorkingCount} working");
		}
		AutomationProperties.SetName(root, string.Join(", ", list));
	}
}

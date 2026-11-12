using System;
using System.Collections.Generic;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch.Peek;

public interface IChatLiveWork
{
	event Action? Changed;

	FrameworkElement? WorkCard(ChatHandOff entry);

	FrameworkElement? WorkCard(ChatHandOff entry, bool hidesFinalizing)
	{
		return WorkCard(entry);
	}

	FrameworkElement? NextStepButton(ChatHandOff entry)
	{
		return null;
	}

	bool IsWorking(ChatHandOff entry);

	string? DisplayName(ChatHandOff entry);

	FrameworkElement? Chip(ChatHandOff entry, double size);

	FrameworkElement? FileCards(IReadOnlyList<string> entries, Action<string> open);

	FrameworkElement? PlanFold(ChatHandOff entry, Action onFinished)
	{
		return null;
	}
}

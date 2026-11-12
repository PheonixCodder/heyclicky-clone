using System;
using System.Collections.Generic;
using HeyClicky.Agents;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;

namespace HeyClicky.App;

internal sealed class WorkChatLiveWork(AgentService agents, Func<OneBrainJobReports?> reports) : IChatLiveWork
{
	private readonly Dictionary<Action, EventHandler> handlers = new Dictionary<Action, EventHandler>();

	public event Action? Changed
	{
		add
		{
			if (value != null)
			{
				EventHandler value2 = delegate
				{
					value();
				};
				handlers[value] = value2;
				agents.TasksChanged += value2;
				agents.Jobs.Changed += value2;
				OneBrainJobReports oneBrainJobReports = reports();
				if (oneBrainJobReports != null)
				{
					oneBrainJobReports.PresentingChanged += value;
				}
			}
		}
		remove
		{
			if (value != null && handlers.Remove(value, out EventHandler value2))
			{
				agents.TasksChanged -= value2;
				agents.Jobs.Changed -= value2;
				OneBrainJobReports oneBrainJobReports = reports();
				if (oneBrainJobReports != null)
				{
					oneBrainJobReports.PresentingChanged -= value;
				}
			}
		}
	}

	public FrameworkElement? WorkCard(ChatHandOff entry)
	{
		return null;
	}

	public bool IsWorking(ChatHandOff entry)
	{
		string jobId = entry.JobId;
		if (jobId != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(jobId);
			if ((object)clickyJob != null)
			{
				ClickyJobRailPhase clickyJobRailPhase = agents.RailPhase(clickyJob);
				if ((uint)clickyJobRailPhase > 1u)
				{
					if (clickyJobRailPhase == ClickyJobRailPhase.Done)
					{
						return reports()?.IsPresenting(clickyJob.ThreadId) ?? false;
					}
					return false;
				}
				return true;
			}
		}
		string clickySlug = entry.ClickySlug;
		if (clickySlug != null)
		{
			return agents.LiveStatusForClicky(clickySlug).Phase == ClickyAppLiveStatus.Phases.Working;
		}
		return false;
	}

	public string? DisplayName(ChatHandOff entry)
	{
		string jobId = entry.JobId;
		if (jobId == null)
		{
			string clickySlug = entry.ClickySlug;
			if (clickySlug == null)
			{
				return null;
			}
			return agents.FindClicky(clickySlug)?.Name;
		}
		return agents.Jobs.Job(jobId)?.DisplayName;
	}

	public FrameworkElement? Chip(ChatHandOff entry, double size)
	{
		return null;
	}

	public FrameworkElement? FileCards(IReadOnlyList<string> entries, Action<string> open)
	{
		return null;
	}
}

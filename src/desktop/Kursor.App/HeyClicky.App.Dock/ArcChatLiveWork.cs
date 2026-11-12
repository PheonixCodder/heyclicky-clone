using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using HeyClicky.Agents;
using HeyClicky.Agents.Clickys;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Ink;
using HeyClicky.App.Notch.Peek;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Dock;

internal sealed class ArcChatLiveWork(ArcDock dock, AgentService agents, IChatLiveWork? fallback) : IChatLiveWork
{
	public event Action? Changed
	{
		add
		{
			dock.ContentChanged += value;
		}
		remove
		{
			dock.ContentChanged -= value;
		}
	}

	private static string? Key(ChatHandOff entry)
	{
		string jobId = entry.JobId;
		if (jobId == null)
		{
			return entry.ClickySlug;
		}
		return "job:" + jobId;
	}

	public FrameworkElement? WorkCard(ChatHandOff entry)
	{
		return WorkCard(entry, hidesFinalizing: false);
	}

	public FrameworkElement? WorkCard(ChatHandOff entry, bool hidesFinalizing)
	{
		string text = Key(entry);
		if (text != null)
		{
			ArcCardContent arcCardContent = dock.ContentForKey(text);
			if ((object)arcCardContent != null)
			{
				return new ChatWorkCard(dock, text, arcCardContent, hidesFinalizing);
			}
		}
		return null;
	}

	public FrameworkElement? NextStepButton(ChatHandOff entry)
	{
		string text = Key(entry);
		ArcCardContent content;
		Border holder;
		if (text != null)
		{
			content = dock.ContentForKey(text);
			if ((object)content != null && content.IsDone)
			{
				holder = new Border();
				Fill(copied: false);
				if ((object)holder.Child != null)
				{
					return holder;
				}
				return null;
			}
		}
		return null;
		void Fill(bool copied)
		{
			holder.Child = ArcNextStep.Button(dock, content, copied, delegate
			{
				Fill(copied: true);
			}, delegate(string action, JsonObject properties)
			{
				properties["surface"] = "chat";
				ArcDock.TrackWindowAction(action, content, properties);
			}, canShowFullResponse: true, 30.0, 13.0);
		}
	}

	public bool IsWorking(ChatHandOff entry)
	{
		string text = Key(entry);
		if (text != null)
		{
			ArcCardContent arcCardContent = dock.ContentForKey(text);
			if ((object)arcCardContent != null)
			{
				return arcCardContent.IsWorking;
			}
		}
		return fallback?.IsWorking(entry) ?? false;
	}

	public string? DisplayName(ChatHandOff entry)
	{
		return fallback?.DisplayName(entry);
	}

	public FrameworkElement? Chip(ChatHandOff entry, double size)
	{
		int tapeSeed = NotePaperColors.Seed(entry.Id);
		string clickySlug = entry.ClickySlug;
		if (clickySlug != null)
		{
			ClickyInfo clickyInfo = agents.FindClicky(clickySlug);
			if ((object)clickyInfo != null)
			{
				return new NoteChipView("clicky:" + clickySlug, ClickyInkIcon.Inferred(clickyInfo.Name + " " + clickyInfo.RoleText) ?? "sparkles", NotePaperColors.Seed(clickySlug), tapeSeed, size);
			}
		}
		string jobId = entry.JobId;
		if (jobId != null)
		{
			ClickyJob clickyJob = agents.Jobs.Job(jobId);
			if ((object)clickyJob != null)
			{
				return new NoteChipView("job:" + clickyJob.Id, ClickyInkIcon.Inferred(clickyJob.Title) ?? "checklist", NotePaperColors.Seed(clickyJob.RailKey), tapeSeed, size);
			}
		}
		return null;
	}

	public FrameworkElement? FileCards(IReadOnlyList<string> entries, Action<string> open)
	{
		return new FileCardRow(entries, open);
	}

	public FrameworkElement? PlanFold(ChatHandOff entry, Action onFinished)
	{
		string jobId = entry.JobId;
		object obj;
		if (jobId == null)
		{
			string clickySlug = entry.ClickySlug;
			obj = ((clickySlug != null) ? ArcLiveNotes.ClickyNotePath(agents, clickySlug) : null);
		}
		else
		{
			obj = "job:" + jobId;
		}
		string text = (string)obj;
		if (text != null)
		{
			ClickyArcPlan clickyArcPlan = agents.LiveSteps.LastPlan(text);
			if ((object)clickyArcPlan != null && !clickyArcPlan.IsEmpty)
			{
				string text2 = Key(entry);
				if (text2 != null)
				{
					ArcCardContent arcCardContent = dock.ContentForKey(text2);
					if ((object)arcCardContent != null)
					{
						string line = ((string?)entry.Answer?["line"]) ?? entry.ReplyText ?? "";
						return new ChatPlanFold(clickyArcPlan, PlanCascade.Done(clickyArcPlan), line, arcCardContent.Paper.Ink, arcCardContent.Paper.TitleWash, onFinished);
					}
				}
				return null;
			}
		}
		return null;
	}
}

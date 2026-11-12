using System;
using System.Collections.Generic;
using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public sealed class ArcDockHost
{
	public Func<(int X, int Y, int W, int H)?> NotchSurfacePx { get; set; } = () => ((int X, int Y, int W, int H)?)null;

	public Func<bool> NotchIsExpanded { get; set; } = () => false;

	public Func<bool> NotchIsOpenOnHome { get; set; } = () => false;

	public Func<bool> NotchTabIsHome { get; set; } = () => true;

	public Func<bool> NotchHoldsOpenWithoutArc { get; set; } = () => false;

	public Action<bool> SetNotchHeldOpenByArc { get; set; } = delegate
	{
	};

	public Action OpenNotchHome { get; set; } = delegate
	{
	};

	public Action DismissNotch { get; set; } = delegate
	{
	};

	public Action<string> OpenAgentInWindow { get; set; } = delegate
	{
	};

	public Func<bool> IsFullscreenOnDockDisplay { get; set; } = () => false;

	public Func<string?, string?, bool> HasLatestReply { get; set; } = (string? _, string? _) => false;

	public Action<string?, string?> ShowFullReply { get; set; } = delegate
	{
	};

	public Action<string?, string?> ShowChat { get; set; } = delegate
	{
	};

	public Action RequestComposerFocus { get; set; } = delegate
	{
	};

	public Func<string, DateTimeOffset?> LatestChatEventAt { get; set; } = (string _) => (DateTimeOffset?)null;

	public Func<string?, string?, ClickyAnswer?> LatestReplyAnswer { get; set; } = (string? _, string? _) => (ClickyAnswer?)null;

	public Func<string, bool> OpensInNotchDocumentViewer { get; set; } = (string _) => false;

	public Func<string, string?, string?, bool> OpenInNotchViewer { get; set; } = (string _, string? _, string? _) => false;

	public Action<string, string>? PresentNotchUpdate { get; set; }

	public Action<string> DismissNotchUpdate { get; set; } = delegate
	{
	};

	public Func<bool> HasCompletedOnboarding { get; set; } = () => true;

	public Func<bool> OnboardingHidesDock { get; set; } = () => false;

	public Func<bool> SuppressesVisualSurfaces { get; set; } = () => false;

	public Func<bool> IsTourRunning { get; set; } = () => false;

	public Func<bool> PlaysSounds { get; set; } = () => true;

	public Func<string> VoiceHoldDescription { get; set; } = () => "Ctrl + Alt";

	public Func<bool> IsListening { get; set; } = () => false;

	public Func<IReadOnlyList<string>> UnreadClickySlugs { get; set; } = () => Array.Empty<string>();

	public Action<string> MarkClickyRead { get; set; } = delegate
	{
	};

	public Func<ClickyJob, bool> StartWaitingJob { get; set; } = (ClickyJob _) => false;

	public Func<ISet<Guid>?> LiveSuggestionCardIds { get; set; } = () => (ISet<Guid>?)null;

	public Func<string, string?> NoteTitle { get; set; } = (string _) => (string?)null;

	public Func<string, ClickyArcPlan?> NoteChecklistPlan { get; set; } = (string _) => (ClickyArcPlan?)null;
}

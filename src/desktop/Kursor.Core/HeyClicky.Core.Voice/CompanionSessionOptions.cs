using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Brain;
using HeyClicky.Core.Drive;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Platform;
using HeyClicky.Core.Tools;

namespace HeyClicky.Core.Voice;

public sealed class CompanionSessionOptions
{
	public SynchronizationContext? UiContext { get; set; }

	public IDesktopActions DesktopActions { get; set; } = new WindowsDesktopActions();

	public Func<BrainToolCall, CompanionToolContext, CancellationToken, Task<string>>? ToolCallHandler { get; set; }

	public Func<BrainUserContext>? UserContext { get; set; }

	public Func<string?>? RosterNote { get; set; }

	public Func<string?>? PlanNote { get; set; }

	public IPageReader? PageReader { get; set; }

	public Func<string, bool>? DocumentAccessDenied { get; set; }

	public Func<string?>? OutputSilenceKind { get; set; }

	public Func<bool> IsOnboardingTutorialRestrictingNormalUse { get; set; } = () => false;

	public Func<string> UsageContext { get; set; } = () => "companion";

	public TimeSpan MinimumHold { get; set; } = TimeSpan.FromSeconds(0.5);

	public Func<string?>? LiveStateNote { get; set; }

	public Func<bool, string?>? EdgeDockFocusNote { get; set; }

	public Func<string, Task<bool>>? TakeSpokenWordsDirectly { get; set; }

	public Func<string?>? TurnFlow { get; set; }

	public Func<RectD, DisplayInfo, CancellationToken, Task<RectD?>>? SnapTargetToControl { get; set; }

	public IPointerInput? PointerInput { get; set; }

	public Func<string, bool>? RefuseTurnOverMessageCap { get; set; }

	public Action<bool>? MessageCounted { get; set; }

	public Func<bool>? KeepMouthWarm { get; set; }

	public Func<IReadOnlyList<HighlightStroke>, CancellationToken, Task<string?>>? HighlightText { get; set; }

	public Func<bool>? MutedSpeakerReplyExempt { get; set; }

	public Func<JsonObject?>? TakeFlowBrief { get; set; }

	public Func<string?>? TakePillPlanNote { get; set; }

	public Func<bool, string?>? BrowserStateNote { get; set; }

	public Func<bool>? IsOnboardingTurn { get; set; }

	public Func<double>? CaptureSettleSeconds { get; set; }

	public Func<bool>? RendersTextInNotchChat { get; set; }

	public Func<bool>? StepNotchChatAsideForDrawing { get; set; }

	public Func<ConversationHistory?>? ChatHistory { get; set; }

	public Func<IReadOnlyList<string>>? SystemLanguageTags { get; set; }

	public IClickyDriveTurns? Drive { get; set; }
}

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
	public SynchronizationContext? UiContext { get; init; }

	public IDesktopActions DesktopActions { get; init; } = new WindowsDesktopActions();

	public Func<BrainToolCall, CompanionToolContext, CancellationToken, Task<string>>? ToolCallHandler { get; init; }

	public Func<BrainUserContext>? UserContext { get; init; }

	public Func<string?>? RosterNote { get; init; }

	public Func<string?>? PlanNote { get; init; }

	public IPageReader? PageReader { get; init; }

	public Func<string, bool>? DocumentAccessDenied { get; init; }

	public Func<string?>? OutputSilenceKind { get; init; }

	public Func<bool> IsOnboardingTutorialRestrictingNormalUse { get; init; } = () => false;

	public Func<string> UsageContext { get; init; } = () => "companion";

	public TimeSpan MinimumHold { get; init; } = TimeSpan.FromSeconds(0.5);

	public Func<string?>? LiveStateNote { get; init; }

	public Func<bool, string?>? EdgeDockFocusNote { get; init; }

	public Func<string, Task<bool>>? TakeSpokenWordsDirectly { get; init; }

	public Func<string?>? TurnFlow { get; init; }

	public Func<RectD, DisplayInfo, CancellationToken, Task<RectD?>>? SnapTargetToControl { get; init; }

	public IPointerInput? PointerInput { get; init; }

	public Func<string, bool>? RefuseTurnOverMessageCap { get; init; }

	public Action<bool>? MessageCounted { get; init; }

	public Func<bool>? KeepMouthWarm { get; init; }

	public Func<IReadOnlyList<HighlightStroke>, CancellationToken, Task<string?>>? HighlightText { get; init; }

	public Func<bool>? MutedSpeakerReplyExempt { get; init; }

	public Func<JsonObject?>? TakeFlowBrief { get; init; }

	public Func<string?>? TakePillPlanNote { get; init; }

	public Func<bool, string?>? BrowserStateNote { get; init; }

	public Func<bool>? IsOnboardingTurn { get; init; }

	public Func<double>? CaptureSettleSeconds { get; init; }

	public Func<bool>? RendersTextInNotchChat { get; init; }

	public Func<bool>? StepNotchChatAsideForDrawing { get; init; }

	public Func<ConversationHistory?>? ChatHistory { get; init; }

	public Func<IReadOnlyList<string>>? SystemLanguageTags { get; init; }

	public IClickyDriveTurns? Drive { get; set; }
}

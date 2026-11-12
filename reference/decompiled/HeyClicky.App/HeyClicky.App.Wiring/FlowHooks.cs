using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace HeyClicky.App.Wiring;

public static class FlowHooks
{
	public static volatile Func<string?>? LiveStateNote;

	public static volatile Func<string?>? TurnFlow;

	public static volatile Func<string?>? TourStepNote;

	public static volatile Func<string?>? NotchNote;

	public static volatile Func<bool>? RendersTextInNotchChat;

	public static volatile Func<bool>? StepNotchChatAsideForDrawing;

	public static volatile Func<bool, string?>? EdgeDockFocusNote;

	public static volatile Func<string, Task<bool>>? TakeSpokenWordsDirectly;

	public static volatile Func<bool>? SkipInterview;

	public static volatile Action? SuppressDocumentAccessNotice;

	public static volatile Func<bool>? MutedSpeakerReplyExempt;

	public static volatile Func<JsonObject?>? TakeFlowBrief;

	public static volatile Func<string?>? TakePillPlanNote;

	public static volatile Func<bool, string?>? BrowserStateNote;

	public static volatile Func<bool>? IsDrawnTourRunning;

	public static volatile Func<bool>? IsInterviewSendOffInProgress;

	public static volatile Func<double>? CaptureSettleSeconds;
}

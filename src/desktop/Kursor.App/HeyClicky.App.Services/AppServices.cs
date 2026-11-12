using HeyClicky.App.Billing;
using HeyClicky.App.Services.Updates;

namespace HeyClicky.App.Services;

public sealed class AppServices
{
	public required INotchPresenter Presenter { get; set; }

	public required ICompanionSession Companion { get; set; }

	public required IAgentService Agents { get; set; }

	public required IAuthService Auth { get; set; }

	public required IPermissionService Permissions { get; set; }

	public required IMicrophoneCapture Microphone { get; set; }

	public required IAudioOutput Audio { get; set; }

	public required IShortcutMonitor Shortcuts { get; set; }

	public required IScreenCaptureService Capture { get; set; }

	public required ICursorOverlay Cursor { get; set; }

	public required IHighlightOverlay Highlight { get; set; }

	public required IAppSettings Settings { get; set; }

	public IUpdateService Updates { get; set; } = NullUpdateService.Instance;

	public BillingUi? Billing { get; set; }
}

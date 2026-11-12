using System.Threading;
using HeyClicky.App.DevFakes;
using HeyClicky.App.Services;
using HeyClicky.App.Wiring;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App;

public static class CompositionRoot
{
	public static (AppServices Services, RealStack? Real) Build(AppOptions options, DispatcherQueue ui, SynchronizationContext? uiContext)
	{
		if (options.UseFakes)
		{
			return (Services: BuildFakes(options), Real: null);
		}
		RealStack realStack = RealStack.CreateCore();
		return (Services: new AppServices
		{
			Presenter = realStack.Core.Notch,
			Auth = realStack.Core.Auth,
			Permissions = realStack.Permissions,
			Settings = new CoreAppSettings(realStack.Core.Settings),
			Companion = null,
			Agents = null,
			Microphone = null,
			Audio = null,
			Shortcuts = null,
			Capture = null,
			Cursor = null,
			Highlight = null
		}, Real: realStack);
	}

	public static void FillFromRealStack(AppServices services, RealStack real)
	{
		services.Companion = real.Companion;
		services.Agents = real.Agents;
		services.Microphone = real.Microphone;
		services.Audio = real.Audio;
		services.Shortcuts = real.Shortcuts;
		services.Capture = real.Capture;
		services.Cursor = real.Cursor;
		services.Highlight = real.Highlight;
	}

	private static AppServices BuildFakes(AppOptions options)
	{
		string directory = AppPaths.Combine("demo");
		FakeNotchPresenter presenter = new FakeNotchPresenter();
		return new AppServices
		{
			Presenter = presenter,
			Companion = new FakeCompanionSession
			{
				Presenter = presenter
			},
			Agents = new FakeAgentService(),
			Auth = new FakeAuthService(!options.SignedOut),
			Permissions = new FakePermissionService(),
			Microphone = new FakeMicrophone(),
			Audio = new FakeAudioOutput(),
			Shortcuts = new DevShortcutMonitor(),
			Capture = new FakeScreenCapture(),
			Cursor = new FakeCursorOverlay(),
			Highlight = new FakeHighlightOverlay(),
			Settings = new JsonAppSettings(directory)
		};
	}
}

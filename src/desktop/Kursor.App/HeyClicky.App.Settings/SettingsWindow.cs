using HeyClicky.App.Services;
using HeyClicky.App.Shell;
using Microsoft.UI;

namespace HeyClicky.App.Settings;

public sealed class SettingsWindow
{
	public enum Page
	{
		Account,
		General,
		Referral,
		Voice,
		Microphone,
		Dictation,
		Shortcuts,
		Cursor,
		Agents,
		Routines,
		Integrations,
		Developer
	}

	private readonly PanelWindow window;

	private readonly SettingsContent content;

	public PanelWindow Window => window;

	public SettingsContent Content => content;

	public SettingsWindow(AppServices services, IAppActions actions, bool excludeFromCapture)
	{
		window = new PanelWindow("Kursor Settings", excludeFromCapture, showInTaskbar: true, 14.0, Colors.Black, 0.12);
		window.SetPanelSize(900.0, 640.0);
		content = new SettingsContent(services, actions, delegate
		{
			window.HideWindow();
		});
		window.Panel.Children.Add(content);
	}

	public void Show(Page? at = null)
	{
		content.Show(at);
		window.ShowCentered();
	}
}

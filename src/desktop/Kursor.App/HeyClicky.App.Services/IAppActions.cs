using System.Threading.Tasks;
using HeyClicky.Core.Notch;

namespace HeyClicky.App.Services;

public interface IAppActions
{
	bool DevSignInAvailable { get; }

	void OpenSettings();

	void OpenSettings(NotchSettingsSection section);

	void OpenNotchHome();

	void StartTutorial();

	void ShowIntro();

	void Quit();

	void Relaunch();

	void ResetAllDataAndQuit();

	void SetPaused(bool paused);

	void OpenUrl(string url);

	void OnboardingCardAction(int step);

	void DismissOnboardingPin();

	void SignIn();

	Task<bool> DevSignInAsync();
}

using HeyClicky.App.Design;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class TutorialInviteSurface : NotchSurface
{
	public enum Mode
	{
		Invite,
		Resume
	}

	private readonly StackPanel view;

	public override FrameworkElement View => view;

	public TutorialInviteSurface(Mode mode, bool showsContinue = true)
	{
		TutorialInviteSurface tutorialInviteSurface = this;
		(string, string, string, string, bool) obj = ((mode != 0) ? ("Quick tour in progress", "Finish it and I'm all yours", "Continue", "\uf5b0", true) : ("Okay, let me show you around", "I'm sorry my CEO yaps so much.", "Show me", "\uf5b0", true));
		string item = obj.Item1;
		string item2 = obj.Item2;
		string item3 = obj.Item3;
		string item4 = obj.Item4;
		bool item5 = obj.Item5;
		TextBlock item6 = Ui.Text(item, 14.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Center, 0, wrap: true);
		TextBlock item7 = Ui.Text(item2, 12.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Center, 0, wrap: true);
		view = new StackPanel
		{
			Spacing = 12.0,
			Margin = new Thickness(40.0, 34.0, 40.0, 0.0),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		view.Children.Add(new StackPanel
		{
			Spacing = 5.0,
			Children = 
			{
				(UIElement)item6,
				(UIElement)item7
			}
		});
		if (!showsContinue)
		{
			return;
		}
		GelButton gelButton = new GelButton(item3, GelAppearance.PrimaryBlue, item5 ? GelButton.Sizing.Large : GelButton.Sizing.Normal, 100.0, item4, null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		gelButton.Click += delegate
		{
			INotchCardActions notchCardActions = tutorialInviteSurface.Host.Actions as INotchCardActions;
			if (mode == Mode.Invite)
			{
				tutorialInviteSurface.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.OnboardingTutorialInvite));
				tutorialInviteSurface.Host.Actions.StartTutorial();
			}
			else if (notchCardActions != null)
			{
				notchCardActions.ResumeTutorial();
			}
			else
			{
				tutorialInviteSurface.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.OnboardingTutorialResume));
				tutorialInviteSurface.Host.Actions.StartTutorial();
			}
		};
		view.Children.Add(gelButton);
	}
}

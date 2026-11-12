using System;
using HeyClicky.App.OneBrain;

namespace HeyClicky.App;

internal static class DrawnOnboardingNotch
{
	public static void DismissPeek(AppHost host)
	{
		Action foldNotch = DrawnOnboardingNotchShowcase.FoldNotch;
		if (foldNotch != null && DrawnOnboardingNotchShowcase.Shared.IsShowing)
		{
			foldNotch();
		}
		host.CorePresenter?.DismissNotchAgentPeek();
	}
}

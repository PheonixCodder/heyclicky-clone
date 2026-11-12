using System;
using HeyClicky.App.Services;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.UI;

public static class DevSignIn
{
	private static bool Shown => Environment.GetEnvironmentVariable("HEYCLICKY_SHOW_DEV_SIGNIN") == "1";

	public static GelButton? Button(IAppActions actions, GelButton.Sizing size = GelButton.Sizing.Normal)
	{
		if (!Shown || !actions.DevSignInAvailable)
		{
			return null;
		}
		GelButton gelButton = new GelButton("Sign in with local test account", GelAppearance.SecondaryOutline, size, 100.0, null, null, null);
		gelButton.HorizontalAlignment = HorizontalAlignment.Center;
		gelButton.Click += delegate
		{
			actions.DevSignInAsync();
		};
		return gelButton;
	}
}

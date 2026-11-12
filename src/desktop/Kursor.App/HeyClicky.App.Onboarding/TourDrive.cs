using System;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace HeyClicky.App.Onboarding;

public static class TourDrive
{
	public static Func<Color, FrameworkElement, Func<bool>, FrameworkElement>? StepContent;

	public static Action? Run;

	public static Func<bool>? IsDriving;

	public static Action? Cancel;

	public static Action? Completed;
}

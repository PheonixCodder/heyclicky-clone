using System;
using System.Runtime.CompilerServices;
using HeyClicky.App.Design;
using Microsoft.Win32;
using Windows.UI;

namespace HeyClicky.App.Billing;

public sealed record PaywallPalette(bool IsDark, Color Accent, Color ActionInk, Color CardSurface, Color CardBorder, Color BenefitSurface, Color BenefitBorder, Color Primary, Color Secondary, Color ErrorText)
{
	public static readonly PaywallPalette Dark = new PaywallPalette(IsDark: true, DS.Hex(10668031u), DS.Hex(1518925u), DS.Hex(1645087u), DS.White(0.1), DS.White(0.055), DS.White(0.09), DS.White(0.85), DS.White(0.55), DS.Rgb(0.95, 0.45, 0.45));

	public static readonly PaywallPalette Light = new PaywallPalette(IsDark: false, DS.Hex(3168395u), DS.Hex(1518925u), DS.Hex(16448511u), DS.Black(0.12), DS.Hex(16054271u), DS.Hex(2846719u, 0.14), DS.Black(0.85), DS.Black(0.5), DS.Rgb(0.95, 0.45, 0.45));

	public static PaywallPalette Current()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_PAYWALL_THEME");
		if (string.Equals(environmentVariable, "light", StringComparison.OrdinalIgnoreCase))
		{
			return Light;
		}
		if (string.Equals(environmentVariable, "dark", StringComparison.OrdinalIgnoreCase))
		{
			return Dark;
		}
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			object obj = registryKey?.GetValue("AppsUseLightTheme");
			return (obj is int && (int)obj != 0) ? Light : Dark;
		}
		catch (Exception)
		{
			return Dark;
		}
	}

	[CompilerGenerated]
	private PaywallPalette(PaywallPalette original)
	{
		IsDark = original.IsDark;
		Accent = original.Accent;
		ActionInk = original.ActionInk;
		CardSurface = original.CardSurface;
		CardBorder = original.CardBorder;
		BenefitSurface = original.BenefitSurface;
		BenefitBorder = original.BenefitBorder;
		Primary = original.Primary;
		Secondary = original.Secondary;
		ErrorText = original.ErrorText;
	}
}

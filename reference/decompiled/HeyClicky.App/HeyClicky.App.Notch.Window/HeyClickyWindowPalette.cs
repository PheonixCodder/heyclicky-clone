using System;
using System.Runtime.CompilerServices;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using Microsoft.Win32;
using Windows.UI;

namespace HeyClicky.App.Notch.Window;

internal sealed record HeyClickyWindowPalette(bool IsDark, Color Ground, Color Ink, Color PrimaryText, Color SecondaryText, Color TertiaryText, Color AvatarPlate, Color CardPlate)
{
	public MessagesPalette Messages
	{
		get
		{
			if (!IsDark)
			{
				return MessagesPalette.Light;
			}
			return MessagesPalette.Dark;
		}
	}

	public static readonly HeyClickyWindowPalette Dark = new HeyClickyWindowPalette(IsDark: true, DS.Hex(1315860u), DS.White(1.0), HomeTheme.PrimaryText, HomeTheme.SecondaryText, HomeTheme.TertiaryText, HomeTheme.AvatarPlate, HomeTheme.IncomingBubble);

	public static readonly HeyClickyWindowPalette Light = new HeyClickyWindowPalette(IsDark: false, DS.Hex(15198185u), DS.Black(1.0), DS.Hex(1579034u), DS.Hex(7237235u), DS.Hex(8421509u), DS.Hex(13816534u), DS.Hex(15329771u));

	public Color InkAt(double alpha)
	{
		return Ink.WithAlpha(alpha);
	}

	public static HeyClickyWindowPalette Current()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_WINDOW_THEME");
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
	private HeyClickyWindowPalette(HeyClickyWindowPalette original)
	{
		IsDark = original.IsDark;
		Ground = original.Ground;
		Ink = original.Ink;
		PrimaryText = original.PrimaryText;
		SecondaryText = original.SecondaryText;
		TertiaryText = original.TertiaryText;
		AvatarPlate = original.AvatarPlate;
		CardPlate = original.CardPlate;
	}
}

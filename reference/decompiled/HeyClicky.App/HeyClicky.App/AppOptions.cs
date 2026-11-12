using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Platform.Win32;

namespace HeyClicky.App;

public sealed record AppOptions(bool DemoStates, bool SignedOut, bool AllowCapture, bool Background, bool ShowIntro, bool ShowTutorial, Uri? DeepLink, string? DemoOnly, bool Fakes, string? E2E)
{
	public bool UseFakes
	{
		get
		{
			if (!Fakes && !DemoStates)
			{
				return DemoOnly != null;
			}
			return true;
		}
	}

	public static AppOptions Parse(IReadOnlyList<string> args)
	{
		Uri deepLink = ProtocolRegistration.FindDeepLink(args);
		bool flag = Has("--demo-states");
		return new AppOptions(flag, Has("--signed-out"), flag || Has("--allow-capture") || Value("--demo-only") != null, Has("--background") || Has("--launched-at-login"), Has("--intro"), Has("--tutorial"), deepLink, Value("--demo-only"), Has("--fakes"), Value("--e2e"));
		bool Has(string f)
		{
			return args.Any((string a) => string.Equals(a, f, StringComparison.OrdinalIgnoreCase));
		}
		string? Value(string f)
		{
			int num = args.ToList().FindIndex((string a) => string.Equals(a, f, StringComparison.OrdinalIgnoreCase));
			if (num < 0 || num + 1 >= args.Count)
			{
				return null;
			}
			return args[num + 1];
		}
	}
}

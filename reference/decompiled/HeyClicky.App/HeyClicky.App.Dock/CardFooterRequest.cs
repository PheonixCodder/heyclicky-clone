using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.App.Dock;

public sealed record CardFooterRequest(Kinds Kind, string Text = "", CardReadyOutput? Ready = null)
{
	public enum Kinds
	{
		ComputerUse,
		ExtraUsage,
		UserStep,
		ConnectChrome,
		Failed,
		Stopped,
		Ready
	}

	public static Func<(string BrowserName, bool AddsAgain)> ChromeSetup { get; set; } = () => (BrowserName: "Chrome", AddsAgain: false);

	public string Title => Kind switch
	{
		Kinds.ComputerUse => "One step on your PC", 
		Kinds.ExtraUsage => "Needs more agent messages", 
		Kinds.ConnectChrome => ConnectChromeTitle(ChromeSetup().Item1, ChromeSetup().Item2), 
		Kinds.UserStep => "Your turn", 
		Kinds.Failed => "Didn't finish", 
		Kinds.Stopped => "Interrupted", 
		_ => Ready?.Title ?? "", 
	};

	public string Detail
	{
		get
		{
			switch (Kind)
			{
			case Kinds.ComputerUse:
			{
				string text = Text.Trim();
				return (text != null && text.Length > 0) ? text : "HeyClicky can do this step on your screen while you watch.";
			}
			case Kinds.ExtraUsage:
				return "This one's big. Let it keep going?";
			case Kinds.ConnectChrome:
				return ConnectChromeDetail(ChromeSetup().Item1, ChromeSetup().Item2);
			case Kinds.UserStep:
				return Text;
			case Kinds.Failed:
				return FirstLine(Text) ?? "Something went wrong partway through.";
			case Kinds.Stopped:
				return (Text.Trim() == "Stopped by you.") ? "You stopped it before it finished." : (FirstLine(Text) ?? "It was cut off before it finished.");
			default:
				return Ready?.Detail ?? "";
			}
		}
	}

	public HandoffGlyph? Glyph => Kind switch
	{
		Kinds.ComputerUse => HandoffGlyph.MacStep, 
		Kinds.ConnectChrome => HandoffGlyph.ConnectChrome, 
		Kinds.UserStep => HandoffGlyph.YourTurn, 
		_ => null, 
	};

	public string Symbol => Kind switch
	{
		Kinds.ComputerUse => "cursorarrow.click.2", 
		Kinds.ExtraUsage => "bolt.fill", 
		Kinds.ConnectChrome => "globe", 
		Kinds.UserStep => "hand.point.up.left.fill", 
		Kinds.Failed => "exclamationmark.triangle.fill", 
		Kinds.Stopped => "stop.fill", 
		_ => Ready?.Symbol ?? "checkmark", 
	};

	public (uint Rgb, double Opacity) IconBackground => Kind switch
	{
		Kinds.Stopped => (Rgb: 1842724u, Opacity: 0.45), 
		Kinds.UserStep => (Rgb: 15241996u, Opacity: 1.0), 
		Kinds.ConnectChrome => (Rgb: 3375359u, Opacity: 1.0), 
		Kinds.Ready => (Rgb: Ready?.IconBackgroundRgb ?? 2269291, Opacity: 1.0), 
		_ => (Rgb: 14242877u, Opacity: 1.0), 
	};

	public static string ConnectChromeTitle(string browserName, bool addsAgain)
	{
		if (!addsAgain)
		{
			return "Needs the " + browserName + " extension";
		}
		return "Upgrade the " + browserName + " extension";
	}

	public static string ConnectChromeDetail(string browserName, bool addsAgain)
	{
		if (!addsAgain)
		{
			return "This runs in your " + browserName + ". HeyClicky's extension goes in once, a couple of clicks.";
		}
		return "The new version updates itself, so this is the last time you'll do this. Takes a few seconds.";
	}

	public static CardFooterRequest? FromTask(bool hasTask, string? pendingComputerUse, string? pendingComputerUseSummary, string speakableRequest, bool requiresExtraUsage, bool needsChrome, bool isCompleted, string? userStep, bool canRetryAfterFailure, bool canRetryAfterInterruption, string? failureSummary)
	{
		if (!hasTask)
		{
			return null;
		}
		string text = failureSummary?.Trim() ?? "";
		if (pendingComputerUse != null)
		{
			return new CardFooterRequest(Kinds.ComputerUse, pendingComputerUseSummary ?? speakableRequest);
		}
		if (requiresExtraUsage)
		{
			return new CardFooterRequest(Kinds.ExtraUsage);
		}
		if (needsChrome)
		{
			return new CardFooterRequest(Kinds.ConnectChrome);
		}
		if (isCompleted && userStep != null)
		{
			return new CardFooterRequest(Kinds.UserStep, userStep);
		}
		if (canRetryAfterFailure)
		{
			return new CardFooterRequest(Kinds.Failed, text);
		}
		if (canRetryAfterInterruption)
		{
			return new CardFooterRequest(Kinds.Stopped, text);
		}
		return null;
	}

	private static string? FirstLine(string text)
	{
		string text2 = text.Split(RuntimeHelpers.CreateSpan<char>((RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/)).FirstOrDefault((string l) => l.Length > 0)?.Trim() ?? "";
		if (text2.Length != 0)
		{
			return text2;
		}
		return null;
	}

	[CompilerGenerated]
	private CardFooterRequest(CardFooterRequest original)
	{
		Kind = original.Kind;
		Text = original.Text;
		Ready = original.Ready;
	}
}

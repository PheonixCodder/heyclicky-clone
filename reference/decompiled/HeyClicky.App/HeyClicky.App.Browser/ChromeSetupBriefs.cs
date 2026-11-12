using HeyClicky.Core.Browser;

namespace HeyClicky.App.Browser;

public static class ChromeSetupBriefs
{
	public static string StoreSteps(string browserName)
	{
		switch (browserName)
		{
		case "Edge":
			return "1) Edge first has to allow extensions from other stores: a bar at the top of the page says \"Allow extensions from other stores\" with an Allow button, then Edge confirms with another Allow (skip both when the bar isn't there: Edge already allows them). 2) \"Add to Chrome\" on the page (some Edge versions say \"Get\"). 3) \"Add extension\" in the small dialog Edge shows.";
		case "Brave":
			return "1) \"Add to Brave\" on the page (it may say \"Add to Chrome\"). 2) \"Add extension\" in the small dialog Brave shows.";
		case "Chrome":
		case "Chrome Beta":
		case "Chrome Dev":
		case "Chrome Canary":
			return "1) \"Add to Chrome\" on the page. 2) \"Add extension\" in the small dialog Chrome shows.";
		default:
			return $"1) The page's blue add button (it may say \"Add to Chrome\" or \"Add to {browserName}\"). 2) The confirmation in the small dialog {browserName} shows (\"Add extension\" or \"Add\").";
		}
	}

	public static string StoreCaveats(string browserName)
	{
		return "The labels can differ by version: follow what's on screen and skip a step that's already done. If " + browserName + " shows a profile picker first, ask which profile they use for the sites the job needs. If the page says HeyClicky is blocked by their administrator, or that extensions from other stores aren't allowed, say so in one line and stop: the job can go on without it.";
	}

	public static string RemoveOldCopySteps(string browserName)
	{
		return "1) On " + browserName + "'s extensions page, find the old HeyClicky: the second HeyClicky card, the one that's turned off, marked unpacked or showing an error (its ID starts with pijeb), never the new one from the Chrome Web Store. If there's only one HeyClicky, that part is done. 2) Its \"Remove\" button, then \"Remove\" again in the confirmation. Nothing else on the page: leave Developer mode and every other extension as they are, and don't mention them.";
	}

	public static string ShowHowFact(string browserName, bool addsAgain, bool afterDrive)
	{
		string value = (afterDrive ? ("[Chrome setup] Your try at adding HeyClicky's " + browserName + " extension on their screen stopped partway, so now they do the rest while you show them how.") : ("[Chrome setup] They're adding HeyClicky's " + browserName + " extension themselves, with you showing them each click."));
		string value2 = (afterDrive ? "First, one short line on where things are now." : (addsAgain ? "First, one short line on what you're doing together and why: swapping in the new version of your extension, which keeps itself up to date (their old preview copy no longer connects)." : "First, one short line on what you're doing together and why: adding your extension, so their jobs can work in the sites they're signed into."));
		string value3 = (addsAgain ? " Once it's added, HeyClicky says it's connected and opens the extensions page, and you'll show them how to remove the old one." : "");
		return $"{value} {value2} HeyClicky's page in the Chrome Web Store is open in {browserName}, in front. Then point at each step with a click target, one short line per step: {StoreSteps(browserName)} {StoreCaveats(browserName)} Don't do the steps yourself, and never press {browserName}'s confirmation for them.{value3} Stop once HeyClicky says it's connected.";
	}

	public static string RemoveOldCopyFact(string browserName)
	{
		return $"[Chrome setup] The new HeyClicky extension is connected. One more thing: their old preview copy of HeyClicky is still in {browserName} and no longer works, so they remove it. {browserName}'s extensions page is open in front. First, one short line: the new one's in, so the old one can go, one click and you're done. Then point at each step with a click target, one short line per step: {RemoveOldCopySteps(browserName)} Don't do the steps yourself. End with one short line that it's all set.";
	}

	private static string WhyHere(string browserName)
	{
		return $"This is HeyClicky's own setup, and only you can do it on screen: a job can't use {browserName} until the extension is in. They pressed \"Get new extension\" (or said yes), which is their yes to removing their old preview copy and adding the new one from the Chrome Web Store, {browserName}'s own Remove and Add confirmations included.";
	}

	private static string LeaveAlone(string browserName)
	{
		return "Do nothing else in " + browserName + ": don't sign in, don't touch Developer mode or any other setting, don't touch any other extension (unpacked or not) or open other pages. If something unexpected is in the way (a sign-in, an error, a profile picker you can't place), stop and say what it is.";
	}

	public static string SwapGoal(string browserName, string storeUrl)
	{
		return $"Swap their old preview copy of HeyClicky in {browserName} for the new one from the Chrome Web Store, in one go. {browserName}'s extensions page is open in front (if it isn't, open {ChromiumBrowsers.ExtensionsPageUrl(browserName)} in {browserName} by typing it in the address bar). 1) Find the old HeyClicky: the HeyClicky card that's turned off, marked unpacked or showing an error (its ID starts with pijeb). That card is the only thing you touch on this page. If there's no old HeyClicky, go to step 3. 2) Click that card's \"Remove\" button. {browserName} asks to confirm: press \"Remove\" in that dialog only when it names HeyClicky and you opened it from that card; if it names anything else, press \"Cancel\", end the take-over and say what you see. 3) In the same tab, type {storeUrl} in the address bar and press Return. 4) On HeyClicky's store page: {StoreSteps(browserName)} Once {browserName} says HeyClicky was added, or the button turns into a Remove button, it's done: end the take-over. {StoreCaveats(browserName)} Say what you're doing as you go: first one short line on what and why (swapping in the new version, which updates itself: the old one goes first), then a few words before each step (\"Removing the old one\", \"Opening the Chrome Web Store\", \"Adding the new one\", \"Confirming\"). {WhyHere(browserName)} {LeaveAlone(browserName)}";
	}

	public static string StoreGoal(string browserName, string storeUrl)
	{
		return $"Add the HeyClicky extension to {browserName} from the Chrome Web Store. Its HeyClicky page is open in front (if it isn't, open {storeUrl} in {browserName}). {StoreSteps(browserName)} Once {browserName} says HeyClicky was added, or the button turns into a Remove button, it's done: end the take-over. {StoreCaveats(browserName)} Say what you're doing as you go: first one short line on what and why (adding the new version of your extension, which updates itself), then a few words before each step (\"Adding it from the Chrome Web Store\", \"Confirming\"). {WhyHere(browserName)} {LeaveAlone(browserName)}";
	}
}

using System;
using System.IO;

namespace HeyClicky.Core.Browser;

public sealed class ChromiumBrowserRegistrationException : IOException
{
	public string BrowserName { get; }

	public string KeyPath { get; }

	public ChromiumBrowserRegistrationException(string browserName, string keyPath, Exception inner)
	{
		BrowserName = browserName;
		KeyPath = keyPath;
		base._002Ector($"HeyClicky couldn't write to {browserName}'s registry key {keyPath}. Check that your account can change it, then try again.", inner);
	}
}

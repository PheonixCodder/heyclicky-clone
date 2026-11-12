using System;

namespace HeyClicky.Core.Dictation;

public sealed class DictationPaywallReachedException : Exception
{
	public DictationPaywallReachedException()
		: base("Dictation word limit reached")
	{
	}
}

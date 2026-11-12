using System;

namespace HeyClicky.Core.Voice;

public sealed class RealtimeSocketOpenException : Exception
{
	public RealtimeSocketOpenException(Exception inner)
		: base("the realtime socket didn't open: " + inner.Message, inner)
	{
	}
}

using System;

namespace HeyClicky.Core.Brain;

public sealed class BrainTurnException : Exception
{
	public BrainTurnException(string message)
		: base(message)
	{
	}
}

using System;

namespace HeyClicky.Core.Brain;

public static class BrainCompactionPolicy
{
	public const int CompactionConversationTokenThreshold = 45000;

	public const int CompactionInputTokenThreshold = 75000;

	public static bool ShouldAskWorker(int inputTokens, int baselineInputTokens, int nextCheckInputTokens)
	{
		if (inputTokens < nextCheckInputTokens)
		{
			return false;
		}
		if (baselineInputTokens <= 0)
		{
			return inputTokens >= 75000;
		}
		return inputTokens - baselineInputTokens >= 45000;
	}

	public static int NextCheckInputTokens(int afterWaitAtInputTokens, int tokensUntilFold)
	{
		return afterWaitAtInputTokens + Math.Max(1, tokensUntilFold);
	}
}

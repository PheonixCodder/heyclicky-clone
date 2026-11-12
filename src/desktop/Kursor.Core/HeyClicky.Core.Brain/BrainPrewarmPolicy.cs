using System;

namespace HeyClicky.Core.Brain;

public static class BrainPrewarmPolicy
{
	public const int PrewarmMinimumConversationTokens = 40000;

	public static readonly TimeSpan PrewarmColdCacheGap = TimeSpan.FromMinutes(25L);

	public static readonly TimeSpan MinimumTimeBetweenPrewarms = TimeSpan.FromSeconds(60L);

	public static int ConversationTokens(int lastTurnInputTokens, int baselineInputTokens)
	{
		if (lastTurnInputTokens > 0 && baselineInputTokens > 0)
		{
			return Math.Max(0, lastTurnInputTokens - baselineInputTokens);
		}
		return 0;
	}

	public static bool ShouldPrewarm(int threadItemCount, bool isFirstKeyDownAfterReload, TimeSpan sinceLastTurn, int conversationTokens, bool usesOpenAIBrain, TimeSpan sinceLastPrewarm)
	{
		if (!usesOpenAIBrain || threadItemCount <= 0)
		{
			return false;
		}
		if (conversationTokens < 40000)
		{
			return false;
		}
		if (sinceLastPrewarm <= MinimumTimeBetweenPrewarms)
		{
			return false;
		}
		if (!isFirstKeyDownAfterReload)
		{
			return sinceLastTurn > PrewarmColdCacheGap;
		}
		return true;
	}
}

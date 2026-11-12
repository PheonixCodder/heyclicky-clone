using System;

namespace HeyClicky.Core.Billing;

public readonly record struct AgentModelPricing(double InputUsdPerMillion, double CachedInputUsdPerMillion, double OutputUsdPerMillion)
{
	public static readonly AgentModelPricing Gpt56Sol = new AgentModelPricing(4.0, 0.4, 20.0);

	public static readonly AgentModelPricing Gpt56Terra = new AgentModelPricing(2.0, 0.2, 12.0);

	public static readonly AgentModelPricing Gpt56Luna = new AgentModelPricing(0.2, 0.02, 1.2);

	public static readonly AgentModelPricing Gpt6Astra = new AgentModelPricing(10.0, 1.0, 50.0);

	public static readonly AgentModelPricing Gpt61Sol = new AgentModelPricing(2.0, 0.1, 10.0);

	public static readonly AgentModelPricing Gpt6Sol = new AgentModelPricing(2.0, 0.2, 10.0);

	public static readonly AgentModelPricing Gpt6Luna = new AgentModelPricing(0.1, 0.01, 0.5);

	public static readonly (string Label, AgentModelPricing Pricing)[] ComparisonTiers = new(string, AgentModelPricing)[2]
	{
		("Sol", Gpt61Sol),
		("Luna", Gpt6Luna)
	};

	public static AgentModelPricing? For(string model)
	{
		string text = model.ToLowerInvariant();
		if (text.Contains("astra"))
		{
			return Gpt6Astra;
		}
		if (text.Contains("gpt-6-luna"))
		{
			return Gpt6Luna;
		}
		if (text.Contains("gpt-6.1-sol"))
		{
			return Gpt61Sol;
		}
		if (text.Contains("gpt-6-sol"))
		{
			return Gpt6Sol;
		}
		if (text.Contains("sol"))
		{
			return Gpt56Sol;
		}
		if (text.Contains("terra"))
		{
			return Gpt56Terra;
		}
		if (text.Contains("luna"))
		{
			return Gpt56Luna;
		}
		return null;
	}

	public double CostUsd(long inputTokens, long cachedInputTokens, long outputTokens)
	{
		return ((double)Math.Max(0L, inputTokens - cachedInputTokens) * InputUsdPerMillion + (double)cachedInputTokens * CachedInputUsdPerMillion + (double)outputTokens * OutputUsdPerMillion) / 1000000.0;
	}
}

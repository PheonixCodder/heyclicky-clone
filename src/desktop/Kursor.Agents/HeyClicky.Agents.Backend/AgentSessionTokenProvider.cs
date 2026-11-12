using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Backend;

internal sealed class AgentSessionTokenProvider(AgentWorkerApi api, Func<CancellationToken, Task<string?>> supabaseAccessToken, TimeSpan refreshMargin)
{
	private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);

	private string? token;

	private DateTimeOffset expiresAt;

	private int generation;

	public DateTimeOffset? ExpiresAt
	{
		get
		{
			if (token != null)
			{
				return expiresAt;
			}
			return null;
		}
	}

	public bool AllowsDeveloperModelSelection { get; private set; }

	public bool NeedsRefresh
	{
		get
		{
			if (token != null)
			{
				return expiresAt - DateTimeOffset.UtcNow <= refreshMargin;
			}
			return true;
		}
	}

	public void Invalidate()
	{
		Interlocked.Increment(ref generation);
		token = null;
		expiresAt = default(DateTimeOffset);
	}

	public async Task<string> GetAsync(CancellationToken ct, bool forceRefresh = false)
	{
		if (!forceRefresh && !NeedsRefresh)
		{
			return token;
		}
		await gate.WaitAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (!forceRefresh && !NeedsRefresh)
			{
				return token;
			}
			int mintGeneration = Volatile.Read(in generation);
			string supabase = (await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false)) ?? throw new InvalidOperationException("Sign in to HeyClicky to use agents (no Supabase access token).");
			(string Token, DateTimeOffset ExpiresAt, bool Dev) minted;
			try
			{
				minted = await api.MintSessionTokenAsync(supabase, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex) when (AgentWorkerApi.IsUnauthorized(ex))
			{
				string text = await supabaseAccessToken(ct).ConfigureAwait(continueOnCapturedContext: false);
				if (text == null || text == supabase)
				{
					throw;
				}
				minted = await api.MintSessionTokenAsync(text, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (mintGeneration != Volatile.Read(in generation))
			{
				throw new InvalidOperationException("Signed out while the agent session was being made.");
			}
			(token, expiresAt, AllowsDeveloperModelSelection) = minted;
			Log.Info("Agents", $"agent session token minted, expires {expiresAt:u}");
			return token;
		}
		finally
		{
			gate.Release();
		}
	}
}

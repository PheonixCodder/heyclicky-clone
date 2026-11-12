using System;
using System.Security.Cryptography;
using System.Text;

namespace HeyClicky.Core.Auth;

public static class SignInCallbackGuard
{
	public enum Verdict
	{
		Accept,
		AcceptLocalDevelopment,
		RefuseNoSignInStarted,
		RefuseSignInExpired,
		RefuseMissingState,
		RefuseStateMismatch
	}

	public static readonly TimeSpan PendingSignInLifetime = TimeSpan.FromMinutes(15L);

	public static readonly TimeSpan ToleratedClockRollback = TimeSpan.FromMinutes(5L);

	public static bool AcceptsCallback(Verdict verdict)
	{
		if ((uint)verdict <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool ConsumesPendingSignIn(Verdict verdict)
	{
		if (!AcceptsCallback(verdict))
		{
			return verdict == Verdict.RefuseSignInExpired;
		}
		return true;
	}

	public static string AnalyticsReason(Verdict verdict)
	{
		return verdict switch
		{
			Verdict.Accept => "accepted", 
			Verdict.AcceptLocalDevelopment => "accepted_local_development", 
			Verdict.RefuseNoSignInStarted => "no_sign_in_started", 
			Verdict.RefuseSignInExpired => "sign_in_expired", 
			Verdict.RefuseMissingState => "missing_state", 
			_ => "state_mismatch", 
		};
	}

	public static string MakeState()
	{
		return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-')
			.Replace('/', '_');
	}

	public static Verdict Decide(string? pendingState, DateTimeOffset? pendingStartedAt, string? callbackState, bool isLocalSupabaseBackend, DateTimeOffset now)
	{
		if (isLocalSupabaseBackend)
		{
			return Verdict.AcceptLocalDevelopment;
		}
		if (!string.IsNullOrEmpty(pendingState) && pendingStartedAt.HasValue)
		{
			DateTimeOffset valueOrDefault = pendingStartedAt.GetValueOrDefault();
			TimeSpan timeSpan = now - valueOrDefault;
			if (timeSpan < -ToleratedClockRollback || timeSpan > PendingSignInLifetime)
			{
				return Verdict.RefuseSignInExpired;
			}
			if (string.IsNullOrEmpty(callbackState))
			{
				return Verdict.RefuseMissingState;
			}
			if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(callbackState), Encoding.UTF8.GetBytes(pendingState)))
			{
				return Verdict.RefuseStateMismatch;
			}
			return Verdict.Accept;
		}
		return Verdict.RefuseNoSignInStarted;
	}
}

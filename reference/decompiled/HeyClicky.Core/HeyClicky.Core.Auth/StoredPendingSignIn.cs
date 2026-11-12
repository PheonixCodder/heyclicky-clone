namespace HeyClicky.Core.Auth;

public sealed class StoredPendingSignIn
{
	public string? State { get; set; }

	public double? StartedAtEpochSeconds { get; set; }
}

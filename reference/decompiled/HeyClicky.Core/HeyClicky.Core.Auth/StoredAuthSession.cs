namespace HeyClicky.Core.Auth;

public sealed class StoredAuthSession
{
	public string? AccessToken { get; set; }

	public string? RefreshToken { get; set; }

	public string? UserId { get; set; }

	public string? Email { get; set; }

	public string? DisplayName { get; set; }

	public string? AvatarUrl { get; set; }

	public double? SignupDateEpochSeconds { get; set; }
}

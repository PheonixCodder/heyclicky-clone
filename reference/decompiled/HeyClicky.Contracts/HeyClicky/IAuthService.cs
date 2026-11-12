using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky;

public interface IAuthService
{
	bool IsSignedIn { get; }

	UserProfile? User { get; }

	string? LastSignInErrorMessage => null;

	event EventHandler? Changed;

	event EventHandler? SignInErrorChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default(CancellationToken));

	Task<bool> SignInWithGoogleAsync(CancellationToken ct = default(CancellationToken));

	Task<bool> HandleDeepLinkAsync(Uri uri);

	Task SignOutAsync();
}

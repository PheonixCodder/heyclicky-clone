using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.App.DevFakes;

public sealed class FakeAuthService : IAuthService
{
	public bool IsSignedIn { get; private set; }

	public UserProfile? User { get; private set; }

	public event EventHandler? Changed;

	public FakeAuthService(bool signedIn)
	{
		if (signedIn)
		{
			SignInAs("test@heyclicky.local", "Test User");
		}
	}

	private void SignInAs(string email, string name)
	{
		IsSignedIn = true;
		User = new UserProfile("dev-user", email, name, null);
		this.Changed?.Invoke(this, EventArgs.Empty);
	}

	public Task<string?> GetValidAccessTokenAsync(CancellationToken ct = default(CancellationToken))
	{
		return Task.FromResult(IsSignedIn ? "dev-token" : null);
	}

	public async Task<bool> SignInWithGoogleAsync(CancellationToken ct = default(CancellationToken))
	{
		await Task.Delay(400, ct);
		SignInAs("test@heyclicky.local", "Test User");
		return true;
	}

	public Task<bool> HandleDeepLinkAsync(Uri uri)
	{
		Log.Info("auth", "fake deep link " + uri.GetLeftPart(UriPartial.Path));
		return Task.FromResult(result: true);
	}

	public Task SignOutAsync()
	{
		IsSignedIn = false;
		User = null;
		this.Changed?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}
}

namespace HeyClicky.App.Settings;

public enum ConnectorSignInKind
{
	Unknown,
	Checking,
	SignInUnavailable,
	NotNeeded,
	NeedsSignIn,
	AwaitingBrowser,
	SignedIn,
	Failed,
	TokenRejected
}

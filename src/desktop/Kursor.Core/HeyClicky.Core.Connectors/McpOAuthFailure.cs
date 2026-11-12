namespace HeyClicky.Core.Connectors;

public enum McpOAuthFailure
{
	ServerDoesNotRequireOAuth,
	ServerUnreachable,
	DiscoveryFailed,
	CallbackPortUnavailable,
	AuthorizationDenied,
	StateMismatch,
	RegistrationFailed,
	TokenExchangeFailed,
	TimedOut
}

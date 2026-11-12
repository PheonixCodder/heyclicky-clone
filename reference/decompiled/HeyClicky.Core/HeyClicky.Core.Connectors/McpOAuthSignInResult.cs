using System;

namespace HeyClicky.Core.Connectors;

public sealed record McpOAuthSignInResult(McpOAuthTokens Tokens, Uri TokenEndpoint, McpOAuthClientRegistration Client, bool RegisteredDynamically);

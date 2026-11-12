using System;

namespace HeyClicky.Core.Connectors;

public sealed class McpOAuthException : Exception
{
	public McpOAuthFailure Failure { get; }

	public McpOAuthException(McpOAuthFailure failure, string message)
		: base(message)
	{
		Failure = failure;
	}
}

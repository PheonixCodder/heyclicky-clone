namespace HeyClicky.Core.Auth;

public sealed record AuthCallbackError(string? Error, string? ErrorCode, string? ErrorDescription)
{
	public string RedactedDescription => AuthDeepLink.RedactSignInErrorText(ErrorDescription);

	public string UserFacingMessage
	{
		get
		{
			if (Error == "access_denied")
			{
				return "Google sign-in was canceled. Try again when you're ready.";
			}
			string redactedDescription = RedactedDescription;
			if (redactedDescription.Length == 0)
			{
				return "Sign-in didn't finish. Try again.";
			}
			return "Sign-in didn't finish: " + redactedDescription.TrimEnd('.') + ". Try again.";
		}
	}

	public string DiagnosticMessage
	{
		get
		{
			string redactedDescription = RedactedDescription;
			return $"error={Error ?? "none"} error_code={ErrorCode ?? "none"} error_description={((redactedDescription.Length == 0) ? "none" : redactedDescription)}";
		}
	}

	public string ReportedErrorDomain => "supabase_auth." + (ErrorCode ?? Error ?? "no_access_token");
}

namespace HeyClicky.Core.Worker;

public sealed record ErrorReport(string Stage, string ErrorKind, string ErrorMessage, string UserFacingMessage, string AppVersion, int ErrorCode, string? ErrorDomain = null, string? SupabaseUserId = null, string? UserEmail = null);

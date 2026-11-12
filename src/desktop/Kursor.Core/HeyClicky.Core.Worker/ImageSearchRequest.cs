namespace HeyClicky.Core.Worker;

public sealed record ImageSearchRequest(string Query, int Limit, bool? SubjectPhoto = null, string? PageUrl = null);

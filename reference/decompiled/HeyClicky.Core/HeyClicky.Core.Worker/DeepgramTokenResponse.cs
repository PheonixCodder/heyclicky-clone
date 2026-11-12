namespace HeyClicky.Core.Worker;

public sealed record DeepgramTokenResponse(string AccessToken, string? Model, string? Receipt);

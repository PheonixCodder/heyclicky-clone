namespace HeyClicky.Core.Onboarding;

public sealed record HostedVideoFailure(string VideoKind, string Phase, string Reason, string AssetUrl, string ErrorDomain, int ErrorCode, string ErrorMessage, string PlayerStatus = "none", string ItemStatus = "none", string TimeControlStatus = "none", int? HttpStatus = null);

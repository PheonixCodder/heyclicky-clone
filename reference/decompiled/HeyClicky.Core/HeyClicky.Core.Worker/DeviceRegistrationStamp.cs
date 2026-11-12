namespace HeyClicky.Core.Worker;

public sealed record DeviceRegistrationStamp(string Fingerprint, long RegisteredAtEpochSeconds);

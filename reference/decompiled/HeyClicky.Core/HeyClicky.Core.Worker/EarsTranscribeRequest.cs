namespace HeyClicky.Core.Worker;

public sealed record EarsTranscribeRequest(string AudioWavBase64, string Language);

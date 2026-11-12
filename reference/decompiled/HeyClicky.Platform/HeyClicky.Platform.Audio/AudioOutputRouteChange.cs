namespace HeyClicky.Platform.Audio;

public sealed record AudioOutputRouteChange(string StreamName, string FromTransport, string FromDevice, string ToTransport, string ToDevice, int DroppedBuffers, bool EngineWasRunning, bool RestartSucceeded);

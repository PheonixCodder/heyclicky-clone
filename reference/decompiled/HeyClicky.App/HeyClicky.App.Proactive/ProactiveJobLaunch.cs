using HeyClicky.Core.Proactive;

namespace HeyClicky.App.Proactive;

public sealed record ProactiveJobLaunch(ProactiveSuggestionCard Card, string Transcript, string LaunchContext);

namespace HeyClicky.App.Services.Updates;

public readonly record struct IdleInstallContext(bool AgentWorking, bool VoiceActive, bool Driving, bool NotchOpen, bool Recording, bool Onboarding, bool FullScreen, bool Presentation, bool OnCall, bool NotchAnswering, bool ShowingOnScreen);

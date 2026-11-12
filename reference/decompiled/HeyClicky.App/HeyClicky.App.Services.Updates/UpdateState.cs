namespace HeyClicky.App.Services.Updates;

public enum UpdateState
{
	Unavailable,
	Idle,
	Checking,
	UpToDate,
	Downloading,
	ReadyToInstall,
	Failed
}

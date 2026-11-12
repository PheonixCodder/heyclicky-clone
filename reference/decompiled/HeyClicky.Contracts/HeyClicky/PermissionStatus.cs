namespace HeyClicky;

public enum PermissionStatus
{
	NotDetermined,
	Prompting,
	WaitingInSettings,
	WaitingForPicker,
	RestartRequired,
	Granted,
	Denied
}

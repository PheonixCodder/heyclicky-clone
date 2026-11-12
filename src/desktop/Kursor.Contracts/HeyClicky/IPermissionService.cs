using System;
using System.Threading.Tasks;

namespace HeyClicky;

public interface IPermissionService
{
	MicrophoneBlock MicrophoneBlockedBy => MicrophoneBlock.None;

	event EventHandler? Changed;

	PermissionStatus GetStatus(PermissionKind kind);

	Task<PermissionStatus> RequestAsync(PermissionKind kind);

	void OpenSystemSettings(PermissionKind kind);

	void StartPolling();

	void StopPolling();
}

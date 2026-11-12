using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HeyClicky.App.DevFakes;

public sealed class FakePermissionService : IPermissionService
{
	private readonly Dictionary<PermissionKind, PermissionStatus> status = new Dictionary<PermissionKind, PermissionStatus>
	{
		[PermissionKind.Microphone] = PermissionStatus.NotDetermined,
		[PermissionKind.ScreenCapture] = PermissionStatus.NotDetermined
	};

	public MicrophoneBlock MicrophoneBlockedBy { get; set; }

	public event EventHandler? Changed;

	public PermissionStatus GetStatus(PermissionKind kind)
	{
		return status[kind];
	}

	public void Set(PermissionKind kind, PermissionStatus s)
	{
		status[kind] = s;
		this.Changed?.Invoke(this, EventArgs.Empty);
	}

	public async Task<PermissionStatus> RequestAsync(PermissionKind kind)
	{
		Set(kind, PermissionStatus.Prompting);
		await Task.Delay(800);
		Set(kind, PermissionStatus.Granted);
		return PermissionStatus.Granted;
	}

	public void OpenSystemSettings(PermissionKind kind)
	{
		string fileName = ((kind == PermissionKind.Microphone) ? "ms-settings:privacy-microphone" : "ms-settings:privacy-graphicsCaptureProgrammatic");
		try
		{
			Process.Start(new ProcessStartInfo(fileName)
			{
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}

	public void StartPolling()
	{
	}

	public void StopPolling()
	{
	}
}

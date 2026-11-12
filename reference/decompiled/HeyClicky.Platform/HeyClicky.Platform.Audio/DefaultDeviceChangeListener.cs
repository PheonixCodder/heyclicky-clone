using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

[ComVisible(true)]
internal sealed class DefaultDeviceChangeListener : IMMNotificationClient
{
	private readonly EDataFlow watchedFlow;

	private readonly ERole watchedRole;

	private readonly AutoResetEvent deviceChangedEvent;

	public DefaultDeviceChangeListener(EDataFlow flow, ERole role, AutoResetEvent deviceChangedEvent)
	{
		watchedFlow = flow;
		watchedRole = role;
		this.deviceChangedEvent = deviceChangedEvent;
	}

	public void OnDeviceStateChanged(PCWSTR pwstrDeviceId, DEVICE_STATE dwNewState)
	{
		if (AudioDevices.IsPreferred(pwstrDeviceId.ToString()))
		{
			deviceChangedEvent.Set();
		}
	}

	public void OnDeviceAdded(PCWSTR pwstrDeviceId)
	{
		if (AudioDevices.IsPreferred(pwstrDeviceId.ToString()))
		{
			deviceChangedEvent.Set();
		}
	}

	public void OnDeviceRemoved(PCWSTR pwstrDeviceId)
	{
		if (AudioDevices.IsPreferred(pwstrDeviceId.ToString()))
		{
			deviceChangedEvent.Set();
		}
	}

	public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, PCWSTR pwstrDefaultDeviceId)
	{
		if (flow == watchedFlow && role == watchedRole)
		{
			deviceChangedEvent.Set();
		}
	}

	public void OnPropertyValueChanged(PCWSTR pwstrDeviceId, PROPERTYKEY key)
	{
	}
}

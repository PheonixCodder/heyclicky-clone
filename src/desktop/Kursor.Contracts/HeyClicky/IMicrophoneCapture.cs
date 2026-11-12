using System;

namespace HeyClicky;

public interface IMicrophoneCapture : IDisposable
{
	int SampleRate { get; }

	float Level { get; }

	bool IsRunning { get; }

	string? DeviceName => null;

	bool IsBluetoothDevice => false;

	event Action<ReadOnlyMemory<byte>>? Frame;

	event Action<string>? DeviceProvedDead
	{
		add
		{
		}
		remove
		{
		}
	}

	event Action? StreamStarted
	{
		add
		{
		}
		remove
		{
		}
	}

	void Start();

	void Stop();

	string? FailOverToBuiltInMicrophone(string deadDeviceName)
	{
		return null;
	}

	void StopKeepingDeviceOpen()
	{
		Stop();
	}

	void ReleaseHeldDevice()
	{
	}
}

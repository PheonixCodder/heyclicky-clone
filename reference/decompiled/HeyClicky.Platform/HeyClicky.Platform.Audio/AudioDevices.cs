using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

public static class AudioDevices
{
	private static string? preferredCaptureId;

	private static string? preferredRenderId;

	public static string? PreferredCaptureId
	{
		get
		{
			return Volatile.Read(in preferredCaptureId);
		}
		set
		{
			value = (string.IsNullOrWhiteSpace(value) ? null : value);
			if (!(Interlocked.Exchange(ref preferredCaptureId, value) == value))
			{
				Log.Info("AudioDevices", "microphone pick: " + (value ?? "System Default"));
				AudioDevices.PreferenceChanged?.Invoke(obj: true);
			}
		}
	}

	public static string? PreferredRenderId
	{
		get
		{
			return Volatile.Read(in preferredRenderId);
		}
		set
		{
			value = (string.IsNullOrWhiteSpace(value) ? null : value);
			if (!(Interlocked.Exchange(ref preferredRenderId, value) == value))
			{
				Log.Info("AudioDevices", "output pick: " + (value ?? "System Default"));
				AudioDevices.PreferenceChanged?.Invoke(obj: false);
			}
		}
	}

	internal static event Action<bool>? PreferenceChanged;

	public static IReadOnlyList<AudioDeviceInfo> List(bool capture)
	{
		DeviceClass deviceClass = (capture ? DeviceClass.AudioCapture : DeviceClass.AudioRender);
		string b = null;
		try
		{
			AudioDeviceRole role = AudioDeviceRole.Default;
			b = EndpointIdFromInterfacePath(capture ? MediaDevice.GetDefaultAudioCaptureId(role) : MediaDevice.GetDefaultAudioRenderId(role));
		}
		catch (Exception ex)
		{
			Log.Warn("AudioDevices", "default device lookup failed: " + ex.Message);
		}
		List<AudioDeviceInfo> list = new List<AudioDeviceInfo>();
		try
		{
			foreach (DeviceInformation item in DeviceInformation.FindAllAsync(deviceClass).AsTask().GetAwaiter()
				.GetResult())
			{
				if (item.IsEnabled)
				{
					string text = EndpointIdFromInterfacePath(item.Id);
					if (text != null)
					{
						list.Add(new AudioDeviceInfo(text, item.Name, string.Equals(text, b, StringComparison.OrdinalIgnoreCase)));
					}
				}
			}
		}
		catch (Exception ex2)
		{
			Log.Warn("AudioDevices", "listing " + (capture ? "microphones" : "outputs") + " failed: " + ex2.Message);
		}
		return list.OrderBy<AudioDeviceInfo, string>((AudioDeviceInfo d) => d.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
	}

	internal static string? EndpointIdFromInterfacePath(string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		int num = path.IndexOf("MMDEVAPI#", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			if (!path.StartsWith("{", StringComparison.Ordinal))
			{
				return null;
			}
			return path;
		}
		num += "MMDEVAPI#".Length;
		int num2 = path.IndexOf('#', num);
		int num3;
		if (num2 >= 0)
		{
			num3 = num;
			return path.Substring(num3, num2 - num3);
		}
		num3 = num;
		return path.Substring(num3, path.Length - num3);
	}

	internal unsafe static IMMDevice? Resolve(IMMDeviceEnumerator enumerator, EDataFlow flow, ERole role, out bool usedPreferred, string? overrideId = null)
	{
		usedPreferred = false;
		if (flow == EDataFlow.eCapture && overrideId != null)
		{
			IMMDevice iMMDevice = TryActive(enumerator, overrideId);
			if (iMMDevice != null)
			{
				usedPreferred = true;
				return iMMDevice;
			}
		}
		string text = ((flow == EDataFlow.eCapture) ? PreferredCaptureId : PreferredRenderId);
		if (text != null)
		{
			try
			{
				IMMDevice ppDevice;
				fixed (char* value = text)
				{
					enumerator.GetDevice(new PCWSTR(value), out ppDevice);
				}
				DEVICE_STATE dEVICE_STATE = default(DEVICE_STATE);
				ppDevice.GetState(&dEVICE_STATE);
				if (dEVICE_STATE == DEVICE_STATE.DEVICE_STATE_ACTIVE)
				{
					usedPreferred = true;
					return ppDevice;
				}
				CoreAudio.ReleaseComObject(ppDevice);
				Log.Warn("AudioDevices", $"picked {((flow == EDataFlow.eCapture) ? "microphone" : "output")} is {dEVICE_STATE}; using the default");
			}
			catch (COMException ex)
			{
				Log.Warn("AudioDevices", $"picked {((flow == EDataFlow.eCapture) ? "microphone" : "output")} unavailable (0x{ex.HResult:X8}); using the default");
			}
		}
		IMMDevice iMMDevice2 = CoreAudio.TryGetDefaultDevice(enumerator, flow, role);
		if (flow != EDataFlow.eCapture || iMMDevice2 == null)
		{
			return iMMDevice2;
		}
		AudioEndpointInfo audioEndpointInfo = AudioEndpoints.Describe(iMMDevice2);
		if ((object)audioEndpointInfo != null && audioEndpointInfo.IsBluetooth)
		{
			AudioEndpointInfo audioEndpointInfo2 = AudioEndpoints.ActiveEndpoints(capture: true).FirstOrDefault((AudioEndpointInfo e) => e.IsBuiltInMicrophone);
			if ((object)audioEndpointInfo2 != null)
			{
				IMMDevice iMMDevice3 = TryActive(enumerator, audioEndpointInfo2.Id);
				if (iMMDevice3 != null)
				{
					Log.Info("AudioDevices", $"System Default is the Bluetooth mic '{audioEndpointInfo.Name}': using the built-in '{audioEndpointInfo2.Name}' so the headset stays in A2DP");
					CoreAudio.ReleaseComObject(iMMDevice2);
					return iMMDevice3;
				}
			}
		}
		return iMMDevice2;
	}

	private unsafe static IMMDevice? TryActive(IMMDeviceEnumerator enumerator, string endpointId)
	{
		try
		{
			IMMDevice ppDevice;
			fixed (char* value = endpointId)
			{
				enumerator.GetDevice(new PCWSTR(value), out ppDevice);
			}
			DEVICE_STATE dEVICE_STATE = default(DEVICE_STATE);
			ppDevice.GetState(&dEVICE_STATE);
			if (dEVICE_STATE == DEVICE_STATE.DEVICE_STATE_ACTIVE)
			{
				return ppDevice;
			}
			CoreAudio.ReleaseComObject(ppDevice);
		}
		catch (COMException)
		{
		}
		return null;
	}

	public static AudioEndpointInfo? BuiltInMicrophone()
	{
		return AudioEndpoints.ActiveEndpoints(capture: true).FirstOrDefault((AudioEndpointInfo e) => e.IsBuiltInMicrophone);
	}

	internal static bool IsPreferred(string? endpointId)
	{
		if (endpointId != null)
		{
			if (!string.Equals(endpointId, PreferredCaptureId, StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(endpointId, PreferredRenderId, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace HeyClicky.Platform.Audio;

public static class AudioEndpoints
{
	private static readonly Guid DevicePropertiesFormat = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");

	private static readonly Guid AudioEndpointFormat = new Guid("1da5d803-d492-4edd-8c23-e0c0ffee7f0e");

	private const uint FriendlyNamePid = 14u;

	private const uint EnumeratorNamePid = 24u;

	private const uint FormFactorPid = 0u;

	public static AudioTransport TransportFor(string? enumeratorName, int formFactor)
	{
		string text = (enumeratorName ?? "").ToUpperInvariant();
		if (text.StartsWith("BTH", StringComparison.Ordinal) || text.Contains("BLUETOOTH", StringComparison.Ordinal))
		{
			return AudioTransport.Bluetooth;
		}
		if (text.StartsWith("USB", StringComparison.Ordinal))
		{
			return AudioTransport.Usb;
		}
		bool flag;
		switch (text)
		{
		case "ROOT":
		case "SWD":
		case "SW":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag || text.StartsWith("VIRTUAL", StringComparison.Ordinal))
		{
			return AudioTransport.Virtual;
		}
		if (formFactor == 9)
		{
			return AudioTransport.Hdmi;
		}
		if (text.Length == 0)
		{
			return AudioTransport.Unknown;
		}
		switch (formFactor)
		{
		case 2:
		case 3:
		case 5:
		case 8:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return AudioTransport.Wired;
		}
		return AudioTransport.BuiltIn;
	}

	internal static AudioEndpointInfo? Describe(IMMDevice device)
	{
		try
		{
			PWSTRHolder.GetId(device, out string id);
			device.OpenPropertyStore(STGM.STGM_DIRECT, out var ppProperties);
			try
			{
				string name = ReadString(ppProperties, DevicePropertiesFormat, 14u) ?? "Microphone";
				string enumeratorName = ReadString(ppProperties, DevicePropertiesFormat, 24u) ?? "";
				uint? num = ReadUInt(ppProperties, AudioEndpointFormat, 0u);
				int num2;
				if (num.HasValue)
				{
					uint valueOrDefault = num.GetValueOrDefault();
					num2 = (int)valueOrDefault;
				}
				else
				{
					num2 = 10;
				}
				int formFactor = num2;
				return new AudioEndpointInfo(id, name, TransportFor(enumeratorName, formFactor), formFactor, enumeratorName);
			}
			finally
			{
				CoreAudio.ReleaseComObject(ppProperties);
			}
		}
		catch (Exception ex) when (((ex is COMException || ex is InvalidCastException) ? 1 : 0) != 0)
		{
			Log.Warn("AudioDevices", $"endpoint properties unreadable (0x{ex.HResult:X8})");
			return null;
		}
	}

	public static IReadOnlyList<AudioEndpointInfo> ActiveEndpoints(bool capture)
	{
		List<AudioEndpointInfo> list = new List<AudioEndpointInfo>();
		IMMDeviceEnumerator iMMDeviceEnumerator = null;
		IMMDeviceCollection ppDevices = null;
		try
		{
			iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
			iMMDeviceEnumerator.EnumAudioEndpoints(capture ? EDataFlow.eCapture : EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE, out ppDevices);
			ppDevices.GetCount(out var pcDevices);
			for (uint num = 0u; num < pcDevices; num++)
			{
				ppDevices.Item(num, out var ppDevice);
				try
				{
					AudioEndpointInfo audioEndpointInfo = Describe(ppDevice);
					if ((object)audioEndpointInfo != null)
					{
						list.Add(audioEndpointInfo);
					}
				}
				finally
				{
					CoreAudio.ReleaseComObject(ppDevice);
				}
			}
		}
		catch (COMException ex)
		{
			Log.Warn("AudioDevices", $"listing endpoints failed (0x{ex.HResult:X8})");
		}
		finally
		{
			CoreAudio.ReleaseComObject(ppDevices);
			CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
		}
		return list;
	}

	public static AudioEndpointInfo? DefaultEndpoint(bool capture)
	{
		IMMDeviceEnumerator iMMDeviceEnumerator = null;
		IMMDevice iMMDevice = null;
		try
		{
			iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
			iMMDevice = CoreAudio.TryGetDefaultDevice(iMMDeviceEnumerator, capture ? EDataFlow.eCapture : EDataFlow.eRender, ERole.eConsole);
			return (iMMDevice == null) ? null : Describe(iMMDevice);
		}
		catch (COMException)
		{
			return null;
		}
		finally
		{
			CoreAudio.ReleaseComObject(iMMDevice);
			CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
		}
	}

	private unsafe static string? ReadString(IPropertyStore store, Guid format, uint pid)
	{
		PROPERTYKEY pROPERTYKEY = default(PROPERTYKEY);
		pROPERTYKEY.fmtid = format;
		pROPERTYKEY.pid = pid;
		PROPERTYKEY pROPERTYKEY2 = pROPERTYKEY;
		store.GetValue(&pROPERTYKEY2, out var pv);
		try
		{
			if (pv.Anonymous.Anonymous.vt != VARENUM.VT_LPWSTR)
			{
				return null;
			}
			string text = pv.Anonymous.Anonymous.Anonymous.pwszVal.ToString();
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		finally
		{
			PInvoke.PropVariantClear(ref pv);
		}
	}

	private unsafe static uint? ReadUInt(IPropertyStore store, Guid format, uint pid)
	{
		PROPERTYKEY pROPERTYKEY = default(PROPERTYKEY);
		pROPERTYKEY.fmtid = format;
		pROPERTYKEY.pid = pid;
		PROPERTYKEY pROPERTYKEY2 = pROPERTYKEY;
		store.GetValue(&pROPERTYKEY2, out var pv);
		try
		{
			return (pv.Anonymous.Anonymous.vt == VARENUM.VT_UI4) ? new uint?(pv.Anonymous.Anonymous.Anonymous.ulVal) : ((uint?)null);
		}
		finally
		{
			PInvoke.PropVariantClear(ref pv);
		}
	}
}

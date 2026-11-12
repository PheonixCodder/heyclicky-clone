using System.Runtime.CompilerServices;

namespace HeyClicky.Platform.Audio;

public sealed record AudioEndpointInfo(string Id, string Name, AudioTransport Transport, int FormFactor, string EnumeratorName)
{
	public bool IsBluetooth => Transport == AudioTransport.Bluetooth;

	public bool IsBuiltInMicrophone
	{
		get
		{
			bool flag = Transport == AudioTransport.BuiltIn;
			if (flag)
			{
				int formFactor = FormFactor;
				bool flag2 = ((formFactor == 4 || formFactor == 10) ? true : false);
				flag = flag2;
			}
			return flag;
		}
	}

	public bool IsHeadphones
	{
		get
		{
			int formFactor = FormFactor;
			if ((formFactor != 3 && (uint)(formFactor - 5) > 1u) || 1 == 0)
			{
				return Transport == AudioTransport.Bluetooth;
			}
			return true;
		}
	}

	public string TransportCategory => Transport switch
	{
		AudioTransport.BuiltIn => "built_in", 
		AudioTransport.Bluetooth => "bluetooth", 
		AudioTransport.Usb => "usb", 
		AudioTransport.Hdmi => "hdmi", 
		AudioTransport.Virtual => "virtual", 
		AudioTransport.Wired => "wired", 
		_ => "unknown", 
	};

	public const int FormFactorRemoteNetwork = 0;

	public const int FormFactorSpeakers = 1;

	public const int FormFactorLineLevel = 2;

	public const int FormFactorHeadphones = 3;

	public const int FormFactorMicrophone = 4;

	public const int FormFactorHeadset = 5;

	public const int FormFactorHandset = 6;

	public const int FormFactorDigitalPassthrough = 7;

	public const int FormFactorSpdif = 8;

	public const int FormFactorDisplayAudio = 9;

	public const int FormFactorUnknown = 10;

	[CompilerGenerated]
	private AudioEndpointInfo(AudioEndpointInfo original)
	{
		Id = original.Id;
		Name = original.Name;
		Transport = original.Transport;
		FormFactor = original.FormFactor;
		EnumeratorName = original.EnumeratorName;
	}
}

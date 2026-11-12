using System.Runtime.CompilerServices;

namespace HeyClicky.Platform.Audio;

public sealed record AudioTopologySnapshot(string OutputDeviceName, string OutputTransport, string InputDeviceName, string InputTransport, bool OutputIsHeadphones)
{
	public static readonly AudioTopologySnapshot Unknown = new AudioTopologySnapshot("unknown", "unknown", "unknown", "unknown", OutputIsHeadphones: false);

	public override string ToString()
	{
		return $"output='{OutputDeviceName}' ({OutputTransport}) input='{InputDeviceName}' ({InputTransport}) headphones={OutputIsHeadphones}";
	}

	[CompilerGenerated]
	private AudioTopologySnapshot(AudioTopologySnapshot original)
	{
		OutputDeviceName = original.OutputDeviceName;
		OutputTransport = original.OutputTransport;
		InputDeviceName = original.InputDeviceName;
		InputTransport = original.InputTransport;
		OutputIsHeadphones = original.OutputIsHeadphones;
	}
}

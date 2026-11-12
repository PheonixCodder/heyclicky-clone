using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

internal sealed class EarconPlayer : WasapiRenderStream
{
	private const int OutputSampleRate = 48000;

	private const int OutputChannels = 2;

	private const int MaxConcurrentVoices = 8;

	private static readonly Dictionary<string, (string FileStem, float DefaultVolume)> ChimeTable = new Dictionary<string, (string, float)>(StringComparer.OrdinalIgnoreCase)
	{
		["textOpen"] = ("clicky-text-open", 0.35f),
		["textSend"] = ("clicky-text-send", 0.5f),
		["textReceive"] = ("clicky-text-receive", 0.35f),
		["textClose"] = ("clicky-text-close", 0.5f),
		["agentDone"] = ("agent-done", 0.7f),
		["agentClose"] = ("agent-close", 0.7f),
		["agentLaunch"] = ("agent-launch", 0.55f),
		["agentNeedsYou"] = ("agent-needs-you", 0.8f),
		["hatching"] = ("hatching", 0.4f),
		["clickyQuestion"] = ("clicky-question", 0.4f),
		["clickySurprised"] = ("clicky-surprised", 0.7f),
		["connectionQuestion"] = ("connection-question", 0.85f),
		["skillUp"] = ("skill-up", 0.5f),
		["skillDown"] = ("skill-down", 0.5f),
		["steerTapback"] = ("tapback-thumbs-up", 0.6f),
		["homeReveal"] = ("home-reveal", 0.7f),
		["clickyReveal"] = ("skill-up", 0.45f),
		["integrationPop"] = ("skill-up", 0.3f),
		["revealBoot"] = ("reveal-boot", 0.6f)
	};

	private readonly Dictionary<string, float[]?> decodedByFileStem = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

	private readonly object voicesGate = new object();

	private readonly float[]?[] voiceSamples = new float[8][];

	private readonly int[] voicePosition = new int[8];

	private readonly float[] voiceVolume = new float[8];

	private volatile bool deviceCold = true;

	private const int ColdLeadMilliseconds = 20;

	private const int BluetoothColdLeadMilliseconds = 150;

	public static IReadOnlyCollection<string> KnownNames => ChimeTable.Keys;

	protected override double IdleStopSeconds => 8.0;

	private static string EarconDirectory => Path.Combine(AppContext.BaseDirectory, "Assets", "Earcons");

	protected override bool HasPendingAudio
	{
		get
		{
			lock (voicesGate)
			{
				float[][] array = voiceSamples;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null)
					{
						return true;
					}
				}
				return false;
			}
		}
	}

	public EarconPlayer()
		: base("earcon", 48000, 8, 60)
	{
	}

	protected override void OnStreamReset(bool causedByFlush)
	{
		deviceCold = true;
	}

	public void PreloadAll()
	{
		foreach (var value in ChimeTable.Values)
		{
			GetDecoded(value.FileStem);
		}
		EnsureStarted();
	}

	public void Play(string name, float? volume)
	{
		if (!TryResolve(name, out string fileStem, out float defaultVolume))
		{
			Log.Warn("Earcon", "unknown earcon '" + name + "'");
			return;
		}
		float[] decoded = GetDecoded(fileStem);
		if (decoded == null)
		{
			return;
		}
		float num = Math.Clamp(volume ?? defaultVolume, 0f, 1f);
		lock (voicesGate)
		{
			int num2 = Array.IndexOf(voiceSamples, null);
			if (num2 < 0)
			{
				num2 = NearestToItsEnd();
			}
			voiceSamples[num2] = decoded;
			voicePosition[num2] = 0;
			voiceVolume[num2] = num;
		}
		Wake();
	}

	private int NearestToItsEnd()
	{
		int result = 0;
		int num = int.MaxValue;
		for (int i = 0; i < 8; i++)
		{
			float[] array = voiceSamples[i];
			if (array == null)
			{
				return i;
			}
			int num2 = array.Length - voicePosition[i];
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private static bool TryResolve(string name, out string fileStem, out float defaultVolume)
	{
		if (ChimeTable.TryGetValue(name, out (string, float) value))
		{
			(fileStem, defaultVolume) = value;
			return true;
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(name);
		foreach (var value2 in ChimeTable.Values)
		{
			if (string.Equals(value2.FileStem, fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
			{
				(fileStem, defaultVolume) = value2;
				return true;
			}
		}
		if (File.Exists(Path.Combine(EarconDirectory, fileNameWithoutExtension + ".wav")))
		{
			fileStem = fileNameWithoutExtension;
			defaultVolume = 0.5f;
			return true;
		}
		fileStem = "";
		defaultVolume = 0f;
		return false;
	}

	private float[]? GetDecoded(string fileStem)
	{
		lock (decodedByFileStem)
		{
			if (decodedByFileStem.TryGetValue(fileStem, out float[] value))
			{
				return value;
			}
			float[] array = null;
			try
			{
				array = DecodeWavToStereo48k(File.ReadAllBytes(Path.Combine(EarconDirectory, fileStem + ".wav")));
			}
			catch (Exception ex)
			{
				Log.Error("Earcon", "could not load " + fileStem + ".wav", ex);
			}
			decodedByFileStem[fileStem] = array;
			return array;
		}
	}

	protected unsafe override void InitializeClient(IAudioClient client, long bufferDurationHns)
	{
		deviceCold = true;
		WAVEFORMATEXTENSIBLE wAVEFORMATEXTENSIBLE = CoreAudio.Float32(48000, 2);
		client.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 2281963520u, bufferDurationHns, 0L, (WAVEFORMATEX*)(&wAVEFORMATEXTENSIBLE), null);
	}

	protected override int Fill(Span<byte> destination, int frameCount)
	{
		Span<float> span = MemoryMarshal.Cast<byte, float>(destination.Slice(0, frameCount * BytesPerFrame));
		span.Clear();
		int num = 0;
		lock (voicesGate)
		{
			if (deviceCold)
			{
				deviceCold = false;
				int num2 = ((base.OpenDeviceTransport == "bluetooth") ? 150 : 20);
				int num3 = 48000 * num2 / 1000 * 2;
				for (int i = 0; i < 8; i++)
				{
					if (voiceSamples[i] != null && voicePosition[i] == 0)
					{
						voicePosition[i] = -num3;
					}
				}
			}
			for (int j = 0; j < 8; j++)
			{
				float[] array = voiceSamples[j];
				if (array != null)
				{
					int num4 = voicePosition[j];
					int num5 = 0;
					if (num4 < 0)
					{
						num5 = Math.Min(span.Length, -num4);
						num4 += num5;
					}
					int num6 = Math.Max(0, Math.Min(span.Length - num5, array.Length - num4));
					float num7 = voiceVolume[j];
					for (int k = 0; k < num6; k++)
					{
						span[num5 + k] += array[num4 + k] * num7;
					}
					num = Math.Max(num, (num5 + num6) / 2);
					num4 += num6;
					if (num4 >= array.Length)
					{
						voiceSamples[j] = null;
					}
					else
					{
						voicePosition[j] = num4;
					}
				}
			}
		}
		if (num == 0)
		{
			return 0;
		}
		SoftLimit(span.Slice(0, num * 2));
		return frameCount;
	}

	internal static void SoftLimit(Span<float> mix)
	{
		for (int i = 0; i < mix.Length; i++)
		{
			float num = mix[i];
			float num2 = MathF.Abs(num);
			if (!(num2 <= 0.9f))
			{
				mix[i] = MathF.CopySign(0.9f + 0.100000024f * MathF.Tanh((num2 - 0.9f) / 0.100000024f), num);
			}
		}
	}

	internal static float[] DecodeWavToStereo48k(byte[] wav)
	{
		if (wav.Length < 44 || BinaryPrimitives.ReadUInt32BigEndian(wav) != 1380533830)
		{
			throw new InvalidDataException("not a RIFF file");
		}
		int formatTag = 0;
		int channels = 0;
		int num = 0;
		int bitsPerSample = 0;
		int dataOffset = -1;
		int num2 = 0;
		int num4;
		for (int i = 12; i + 8 <= wav.Length; i += 8 + num4 + (num4 & 1))
		{
			uint num3 = BinaryPrimitives.ReadUInt32BigEndian(wav.AsSpan(i));
			num4 = (int)BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(i + 4));
			switch (num3)
			{
			case 1718449184u:
				formatTag = BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(i + 8));
				channels = BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(i + 10));
				num = (int)BinaryPrimitives.ReadUInt32LittleEndian(wav.AsSpan(i + 12));
				bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(i + 22));
				if (formatTag == 65534 && num4 >= 40)
				{
					formatTag = BinaryPrimitives.ReadUInt16LittleEndian(wav.AsSpan(i + 8 + 24));
				}
				continue;
			case 1684108385u:
				break;
			default:
				continue;
			}
			dataOffset = i + 8;
			num2 = Math.Min(num4, wav.Length - dataOffset);
			break;
		}
		if (dataOffset < 0 || channels == 0)
		{
			throw new InvalidDataException("missing fmt/data chunk");
		}
		int bytesPerSample = bitsPerSample / 8;
		int num5 = num2 / (bytesPerSample * channels);
		int num6 = (int)((long)num5 * 48000L / num);
		float[] array = new float[num6 * 2];
		double num7 = (double)num / 48000.0;
		for (int j = 0; j < num6; j++)
		{
			double num8 = (double)j * num7;
			int num9 = (int)num8;
			float num10 = (float)(num8 - (double)num9);
			int frame2 = Math.Min(num9 + 1, num5 - 1);
			for (int k = 0; k < 2; k++)
			{
				float num11 = ReadSample(num9, k);
				float num12 = ((num7 == 1.0) ? num11 : ReadSample(frame2, k));
				array[j * 2 + k] = num11 + (num12 - num11) * num10;
			}
		}
		return array;
		float ReadSample(int frame, int channel)
		{
			int num13 = dataOffset + (frame * channels + Math.Min(channel, channels - 1)) * bytesPerSample;
			if (formatTag == 3)
			{
				int num14 = bitsPerSample;
				if (num14 <= 16)
				{
					if (num14 == 8)
					{
						goto IL_010f;
					}
					if (num14 == 16)
					{
						goto IL_009e;
					}
				}
				else
				{
					if (num14 == 24)
					{
						goto IL_00be;
					}
					if (num14 == 32)
					{
						return BinaryPrimitives.ReadSingleLittleEndian(wav.AsSpan(num13));
					}
				}
			}
			else
			{
				int num14 = bitsPerSample;
				if (num14 == 16)
				{
					goto IL_009e;
				}
				if (num14 == 24)
				{
					goto IL_00be;
				}
				if (num14 == 32)
				{
					return (float)BinaryPrimitives.ReadInt32LittleEndian(wav.AsSpan(num13)) / 2.1474836E+09f;
				}
				if (bitsPerSample == 8)
				{
					goto IL_010f;
				}
			}
			return 0f;
			IL_009e:
			return (float)BinaryPrimitives.ReadInt16LittleEndian(wav.AsSpan(num13)) / 32768f;
			IL_010f:
			return (float)(wav[num13] - 128) / 128f;
			IL_00be:
			return (float)((wav[num13] | (wav[num13 + 1] << 8) | (wav[num13 + 2] << 16)) << 8 >> 8) / 8388608f;
		}
	}
}

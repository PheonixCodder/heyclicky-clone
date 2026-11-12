using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.Drawing.Interop;
using Microsoft.Win32.SafeHandles;
using Vortice.Multimedia;
using Vortice.XAudio2;

namespace HeyClicky.Drawing.Audio;

internal sealed class SoundPlayer : IDisposable
{
	private struct Voice
	{
		public object Owner;

		public int Clip;

		public double BeginTime;

		public double Duration;

		public double LengthInPoints;

		public Vector4 Timing;

		public bool IsStroke;

		public float Gain;

		public double Rate;

		public double Position;

		public float Envelope;

		public float TargetEnvelope;

		public int BlockSamplesLeft;

		public bool IsStopping;

		public bool IsFinished;
	}

	private sealed class TimerWaitHandle : WaitHandle
	{
		public TimerWaitHandle(nint handle)
		{
			base.SafeWaitHandle = new SafeWaitHandle(handle, ownsHandle: false);
		}
	}

	private static SoundPlayer? shared;

	private const int SampleRate = 48000;

	private const int BlockFrames = 480;

	private const int BlocksAhead = 3;

	private const int MaximumVoices = 48;

	private const float MasterGain = 0.9f;

	private const double QuietSecondsBeforeStopping = 5.0;

	private static readonly HashSet<string> KnownSoundIds = new HashSet<string>
	{
		"pen-screen-bed", "pencil-paper-bed", "handwriting-bed", "eraser-bed", "pen-flick", "gel-pop", "badge-pop", "paper-place", "sticky-press", "tape",
		"photo-drop", "highlighter", "slide", "page-clear", "target-ping", "type-keys", "copy-tick", "button-appear", "build-3d", "tada",
		"ding", "nope", "stamp", "sparkle", "whoosh", "boing", "paper-unfold", "page-turn", "scissor-snip"
	};

	private static readonly Dictionary<string, string> FallbackSoundIds = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["paper-unfold"] = "page-clear",
		["page-turn"] = "slide",
		["scissor-snip"] = "paper-place"
	};

	private readonly string soundsDirectory;

	private readonly object gate = new object();

	private float[][] clips = Array.Empty<float[]>();

	private readonly Dictionary<string, List<int>> clipIndexesBySoundId = new Dictionary<string, List<int>>(StringComparer.Ordinal);

	private bool hasLoadedClips;

	private readonly List<Voice> voices = new List<Voice>();

	private double lastActiveTime;

	private readonly Random random = new Random();

	private Thread? mixThread;

	private volatile bool running;

	private readonly AutoResetEvent wake = new AutoResetEvent(initialState: false);

	private IXAudio2? audio;

	private IXAudio2MasteringVoice? master;

	private IXAudio2SourceVoice? source;

	private long submittedFrames;

	private readonly float[][] blocks = new float[4][];

	private readonly GCHandle[] blockHandles = new GCHandle[4];

	private int nextBlock;

	private double nextStartAttempt;

	public static SoundPlayer Shared => shared ?? (shared = new SoundPlayer(Path.Combine(ScreenDrawingCanvas.AssetsDirectory, "Sounds")));

	private static string TextureSound(string texture)
	{
		return texture switch
		{
			"pencil" => "pencil-paper-bed", 
			"writing" => "handwriting-bed", 
			"eraser" => "eraser-bed", 
			_ => "pen-screen-bed", 
		};
	}

	private static float TextureGain(string texture)
	{
		return texture switch
		{
			"pencil" => 1.45f, 
			"writing" => 1.25f, 
			"eraser" => 0.9f, 
			_ => 1.5f, 
		};
	}

	public SoundPlayer(string soundsDirectory)
	{
		this.soundsDirectory = soundsDirectory;
		for (int i = 0; i < blocks.Length; i++)
		{
			blocks[i] = new float[480];
			blockHandles[i] = GCHandle.Alloc(blocks[i], GCHandleType.Pinned);
		}
	}

	public void Prepare()
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			LoadClipsIfNeeded();
			lock (gate)
			{
				lastActiveTime = Math.Max(lastActiveTime, DrawingThread.Now);
			}
			StartIfNeeded();
		});
	}

	public void Schedule(List<SoundItem> items, object owner)
	{
		if (items.Count == 0)
		{
			return;
		}
		LoadClipsIfNeeded();
		double now = DrawingThread.RealNow;
		double playbackClockShift = DrawingThread.PlaybackClockShift;
		lock (gate)
		{
			voices.RemoveAll((Voice v) => v.IsFinished || v.BeginTime + Math.Max(v.Duration, 4.0) < now - 1.0);
			foreach (SoundItem item2 in items)
			{
				SoundItem item = ((playbackClockShift == 0.0) ? item2 : item2 with
				{
					BeginTime = item2.BeginTime + playbackClockShift
				});
				if (!(item.IsStroke ? (item.BeginTime + item.Duration <= now) : (item.BeginTime <= now - 0.1)))
				{
					if (voices.Count >= 48)
					{
						break;
					}
					Voice? voice = MakeVoice(item, owner);
					if (voice.HasValue)
					{
						Voice valueOrDefault = voice.GetValueOrDefault();
						voices.Add(valueOrDefault);
						lastActiveTime = Math.Max(lastActiveTime, valueOrDefault.BeginTime + valueOrDefault.Duration + 2.0);
					}
				}
			}
		}
		StartIfNeeded();
		wake.Set();
	}

	private Voice? MakeVoice(SoundItem item, object owner)
	{
		Voice value2;
		if (item.IsStroke)
		{
			if (item.Duration <= 0.02 || !clipIndexesBySoundId.TryGetValue(TextureSound(item.Texture), out List<int> value) || value.Count == 0)
			{
				return null;
			}
			int num = value[random.Next(value.Count)];
			value2 = default(Voice);
			value2.Owner = owner;
			value2.Clip = num;
			value2.BeginTime = item.BeginTime;
			value2.Duration = item.Duration;
			value2.LengthInPoints = Math.Max(1.0, item.LengthInPoints);
			value2.Timing = item.Timing;
			value2.IsStroke = true;
			value2.Gain = TextureGain(item.Texture);
			value2.Rate = 1.0;
			value2.Position = random.NextDouble() * (double)Math.Max(1, clips[num].Length);
			return value2;
		}
		if ((!clipIndexesBySoundId.TryGetValue(item.SoundId, out List<int> value3) || value3.Count == 0) && (!FallbackSoundIds.TryGetValue(item.SoundId, out string value4) || !clipIndexesBySoundId.TryGetValue(value4, out value3) || value3.Count <= 0))
		{
			return null;
		}
		value2 = default(Voice);
		value2.Owner = owner;
		value2.Clip = value3[random.Next(value3.Count)];
		value2.BeginTime = item.BeginTime;
		value2.Duration = 0.0;
		value2.Gain = item.Gain * (0.85f + 0.15f * (float)random.NextDouble());
		value2.Rate = 0.97 + 0.06 * random.NextDouble();
		value2.Timing = new Vector4(0f, 0f, 1f, 1f);
		return value2;
	}

	public void StopAll(object owner)
	{
		lock (gate)
		{
			for (int i = 0; i < voices.Count; i++)
			{
				if (voices[i].Owner == owner)
				{
					Voice value = voices[i];
					value.IsStopping = true;
					voices[i] = value;
				}
			}
		}
	}

	private void LoadClipsIfNeeded()
	{
		lock (gate)
		{
			if (hasLoadedClips)
			{
				return;
			}
			hasLoadedClips = true;
			List<float[]> list = new List<float[]>();
			try
			{
				foreach (string item in Directory.GetFiles(soundsDirectory, "*.wav").OrderBy<string, string>((string p) => p, StringComparer.Ordinal))
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
					int num = fileNameWithoutExtension.LastIndexOf('-');
					if (num < 0)
					{
						continue;
					}
					string text = fileNameWithoutExtension;
					int num2 = num + 1;
					if (!int.TryParse(text.Substring(num2, text.Length - num2), out var _))
					{
						continue;
					}
					string text2 = fileNameWithoutExtension.Substring(0, num);
					if (!KnownSoundIds.Contains(text2))
					{
						continue;
					}
					float[] array = MonoSamples(File.ReadAllBytes(item));
					if (array != null)
					{
						if (!clipIndexesBySoundId.TryGetValue(text2, out List<int> value))
						{
							value = (clipIndexesBySoundId[text2] = new List<int>());
						}
						value.Add(list.Count);
						list.Add(array);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Drawing", "drawing sounds did not load: " + ex.Message);
			}
			clips = list.ToArray();
		}
	}

	private static float[]? MonoSamples(byte[] file)
	{
		if (file.Length < 44 || BinaryPrimitives.ReadUInt32BigEndian(file) != 1380533830)
		{
			return null;
		}
		int num = 1;
		int num2 = 48000;
		int num3 = 16;
		int num4 = 1;
		int num5 = 12;
		while (num5 + 8 <= file.Length)
		{
			uint num6 = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(num5));
			int num7 = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(num5 + 4));
			int num8 = num5 + 8;
			switch (num6)
			{
			case 1718449184u:
				num4 = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(num8));
				num = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(num8 + 2));
				num2 = BinaryPrimitives.ReadInt32LittleEndian(file.AsSpan(num8 + 4));
				num3 = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(num8 + 14));
				break;
			case 1684108385u:
			{
				num7 = Math.Min(num7, file.Length - num8);
				int num9 = num3 / 8;
				int num10 = num7 / (num9 * num);
				float[] array = new float[num10];
				for (int i = 0; i < num10; i++)
				{
					float num11 = 0f;
					for (int j = 0; j < num; j++)
					{
						int start = num8 + (i * num + j) * num9;
						num11 += ((num4 == 3 && num3 == 32) ? BinaryPrimitives.ReadSingleLittleEndian(file.AsSpan(start)) : ((num3 == 16) ? ((float)BinaryPrimitives.ReadInt16LittleEndian(file.AsSpan(start)) / 32768f) : 0f));
					}
					array[i] = num11 / (float)num;
				}
				if (num2 == 48000)
				{
					return array;
				}
				float[] array2 = new float[(int)((long)num10 * 48000L / num2)];
				for (int k = 0; k < array2.Length; k++)
				{
					double num12 = (double)k * (double)num2 / 48000.0;
					int num13 = (int)num12;
					float num14 = (float)(num12 - (double)num13);
					array2[k] = ((num13 + 1 < num10) ? (array[num13] + (array[num13 + 1] - array[num13]) * num14) : array[Math.Min(num13, num10 - 1)]);
				}
				return array2;
			}
			}
			num5 = num8 + num7 + (num7 & 1);
		}
		return null;
	}

	private void StartIfNeeded()
	{
		lock (gate)
		{
			if (!running && !(DrawingThread.Now < nextStartAttempt))
			{
				running = true;
				mixThread = new Thread(MixLoop)
				{
					Name = "drawing sounds",
					IsBackground = true,
					Priority = ThreadPriority.AboveNormal
				};
				mixThread.Start();
			}
		}
	}

	private void MixLoop()
	{
		try
		{
			audio = XAudio2.XAudio2Create(ProcessorSpecifier.Processor1, registerCallback: false);
			master = audio.CreateMasteringVoice(1u, 48000u, AudioStreamCategory.SoundEffects, null);
			WaveFormat sourceFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
			source = audio.CreateSourceVoice(sourceFormat, VoiceFlags.None, 2f);
			source.Start();
			submittedFrames = 0L;
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "drawing sounds: the output did not start (" + ex.Message + ")");
			Teardown();
			lock (gate)
			{
				running = false;
				nextStartAttempt = DrawingThread.Now + 10.0;
				return;
			}
		}
		nint num = NativeMethods.CreateWaitableTimerExW(IntPtr.Zero, IntPtr.Zero, 2u, 2031619u);
		WaitHandle[] waitHandles = new WaitHandle[2]
		{
			new TimerWaitHandle(num),
			wake
		};
		try
		{
			while (true)
			{
				Vortice.XAudio2.VoiceState state = source.State;
				while (state.BuffersQueued < 3)
				{
					long val = submittedFrames - (long)state.SamplesPlayed;
					double heardAt = DrawingThread.Now + (double)Math.Max(0L, val) / 48000.0 + 0.01;
					float[] output = blocks[nextBlock];
					Render(output, heardAt);
					AudioBuffer buffer = new AudioBuffer(blockHandles[nextBlock].AddrOfPinnedObject(), 1920u);
					source.SubmitSourceBuffer(buffer);
					submittedFrames += 480L;
					nextBlock = (nextBlock + 1) % blocks.Length;
					state = source.State;
				}
				bool flag;
				lock (gate)
				{
					voices.RemoveAll((Voice v) => v.IsFinished);
					flag = voices.Count == 0 && DrawingThread.Now - lastActiveTime > 5.0;
					if (flag)
					{
						running = false;
					}
				}
				if (!flag)
				{
					long dueTime = -40000L;
					NativeMethods.SetWaitableTimerEx(num, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0u);
					WaitHandle.WaitAny(waitHandles, 10);
					continue;
				}
				break;
			}
		}
		catch (Exception ex2)
		{
			Log.Warn("Drawing", "drawing sounds stopped: " + ex2.Message);
			lock (gate)
			{
				running = false;
			}
		}
		finally
		{
			NativeMethods.CloseHandle(num);
			Teardown();
		}
	}

	private void Teardown()
	{
		try
		{
			source?.Stop(PlayFlags.None, 0u);
			source?.DestroyVoice();
			source?.Dispose();
		}
		catch
		{
		}
		try
		{
			master?.DestroyVoice();
			master?.Dispose();
		}
		catch
		{
		}
		try
		{
			audio?.Dispose();
		}
		catch
		{
		}
		source = null;
		master = null;
		audio = null;
	}

	private void Render(float[] output, double heardAt)
	{
		Array.Clear(output);
		if (!Monitor.TryEnter(gate))
		{
			return;
		}
		try
		{
			for (int i = 0; i < voices.Count; i++)
			{
				Voice voice = voices[i];
				if (!voice.IsFinished)
				{
					if (voice.Clip >= clips.Length)
					{
						voice.IsFinished = true;
						voices[i] = voice;
					}
					else
					{
						Mix(ref voice, clips[voice.Clip], output, heardAt);
						voices[i] = voice;
					}
				}
			}
		}
		finally
		{
			Monitor.Exit(gate);
		}
		for (int j = 0; j < output.Length; j++)
		{
			output[j] = Math.Clamp(output[j] * 0.9f, -1f, 1f);
		}
	}

	private static double BezierSlope(double x, Vector4 curve)
	{
		double num = curve.X;
		double num2 = curve.Y;
		double num3 = curve.Z;
		double num4 = curve.W;
		double num5 = x;
		for (int i = 0; i < 5; i++)
		{
			double num6 = 3.0 * (1.0 - num5) * (1.0 - num5) * num5 * num + 3.0 * (1.0 - num5) * num5 * num5 * num3 + num5 * num5 * num5 - x;
			double num7 = 3.0 * (1.0 - num5) * (1.0 - num5) * num + 6.0 * (1.0 - num5) * num5 * (num3 - num) + 3.0 * num5 * num5 * (1.0 - num3);
			if (Math.Abs(num7) < 1E-06)
			{
				break;
			}
			num5 = Math.Clamp(num5 - num6 / num7, 0.0, 1.0);
		}
		double val = 3.0 * (1.0 - num5) * (1.0 - num5) * num + 6.0 * (1.0 - num5) * num5 * (num3 - num) + 3.0 * num5 * num5 * (1.0 - num3);
		double num8 = 3.0 * (1.0 - num5) * (1.0 - num5) * num2 + 6.0 * (1.0 - num5) * num5 * (num4 - num2) + 3.0 * num5 * num5 * (1.0 - num4);
		return Math.Max(0.0, num8 / Math.Max(val, 0.001));
	}

	private static void Mix(ref Voice voice, float[] clip, float[] output, double heardAt)
	{
		int num = clip.Length;
		if (num <= 1)
		{
			voice.IsFinished = true;
			return;
		}
		if (!voice.IsStroke)
		{
			for (int i = 0; i < output.Length; i++)
			{
				if (heardAt + (double)i * 2.0833333333333333E-05 < voice.BeginTime)
				{
					continue;
				}
				if (voice.IsStopping)
				{
					if (voice.Position <= 0.0)
					{
						voice.IsFinished = true;
						break;
					}
					voice.Gain *= 0.995f;
					if (voice.Gain < 0.0005f)
					{
						voice.IsFinished = true;
						break;
					}
				}
				int num2 = (int)voice.Position;
				if (num2 + 1 >= num)
				{
					voice.IsFinished = true;
					break;
				}
				float num3 = (float)(voice.Position - (double)num2);
				output[i] += (clip[num2] + (clip[num2 + 1] - clip[num2]) * num3) * voice.Gain;
				voice.Position += voice.Rate;
			}
			return;
		}
		for (int j = 0; j < output.Length; j++)
		{
			double num4 = heardAt + (double)j * 2.0833333333333333E-05;
			if (voice.BlockSamplesLeft <= 0)
			{
				voice.BlockSamplesLeft = 32;
				double num5 = (num4 - voice.BeginTime) / Math.Max(voice.Duration, 0.001);
				if (voice.IsStopping || num5 > 1.0)
				{
					voice.TargetEnvelope = 0f;
					if (voice.Envelope < 0.0005f)
					{
						voice.IsFinished = true;
						break;
					}
				}
				else if (num5 < 0.0)
				{
					voice.TargetEnvelope = 0f;
				}
				else
				{
					double num6 = voice.LengthInPoints * BezierSlope(num5, voice.Timing) / Math.Max(voice.Duration, 0.001);
					double num7 = Math.Min(1.6, num6 / 700.0);
					voice.TargetEnvelope = voice.Gain * (float)Math.Min(1.2, Math.Pow(num7, 0.55));
					voice.Rate = 0.86 + 0.24 * Math.Min(1.2, num7);
				}
			}
			voice.BlockSamplesLeft--;
			if (!(voice.TargetEnvelope <= 0f) || !(voice.Envelope <= 0.0005f))
			{
				voice.Envelope += (voice.TargetEnvelope - voice.Envelope) * ((voice.TargetEnvelope > voice.Envelope) ? 0.02f : 0.004f);
				int num8 = (int)voice.Position % num;
				int num9 = (num8 + 1) % num;
				float num10 = (float)(voice.Position - Math.Floor(voice.Position));
				output[j] += (clip[num8] + (clip[num9] - clip[num8]) * num10) * voice.Envelope;
				voice.Position += voice.Rate;
				if (voice.Position >= (double)num)
				{
					voice.Position -= num;
				}
			}
		}
	}

	public float[] MixOffline(List<SoundItem> items, double startTime, double seconds)
	{
		LoadClipsIfNeeded();
		List<Voice> list = new List<Voice>();
		lock (gate)
		{
			foreach (SoundItem item in items)
			{
				Voice? voice = MakeVoice(item, this);
				if (voice.HasValue)
				{
					Voice valueOrDefault = voice.GetValueOrDefault();
					list.Add(valueOrDefault);
				}
			}
		}
		float[] array = new float[(int)(seconds * 48000.0)];
		float[] array2 = new float[480];
		for (int i = 0; i < array.Length; i += 480)
		{
			Array.Clear(array2);
			for (int j = 0; j < list.Count; j++)
			{
				Voice voice2 = list[j];
				if (!voice2.IsFinished)
				{
					Mix(ref voice2, clips[voice2.Clip], array2, startTime + (double)i / 48000.0);
					list[j] = voice2;
				}
			}
			int num = Math.Min(480, array.Length - i);
			for (int k = 0; k < num; k++)
			{
				array[i + k] = Math.Clamp(array2[k] * 0.9f, -1f, 1f);
			}
		}
		return array;
	}

	public void Dispose()
	{
		lock (gate)
		{
			voices.Clear();
			lastActiveTime = 0.0;
		}
		wake.Set();
		mixThread?.Join(500);
		GCHandle[] array = blockHandles;
		for (int i = 0; i < array.Length; i++)
		{
			GCHandle gCHandle = array[i];
			if (gCHandle.IsAllocated)
			{
				gCHandle.Free();
			}
		}
	}
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace HeyClicky.App.DevFakes;

public sealed class WavFileMicrophone : IMicrophoneCapture, IDisposable
{
	public const int Rate = 24000;

	private const int FrameSamples = 480;

	private short[] samples;

	private readonly string path;

	private DateTime loadedWriteTime;

	private CancellationTokenSource? run;

	private float level;

	public int SampleRate => 24000;

	public float Level => Volatile.Read(in level);

	public bool IsRunning => run != null;

	public double DurationSeconds => (double)samples.Length / 24000.0;

	public event Action<ReadOnlyMemory<byte>>? Frame;

	public event Action? Finished;

	public WavFileMicrophone(string path)
	{
		this.path = path;
		samples = Load(path);
		loadedWriteTime = File.GetLastWriteTimeUtc(path);
		Log.Info("Mic", $"test WAV microphone: {Path.GetFileName(path)} ({DurationSeconds:0.00} s)");
	}

	public void Start()
	{
		if (run != null)
		{
			return;
		}
		try
		{
			DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
			if (lastWriteTimeUtc != loadedWriteTime)
			{
				samples = Load(path);
				loadedWriteTime = lastWriteTimeUtc;
				Log.Info("Mic", $"test WAV reloaded ({DurationSeconds:0.00} s)");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Mic", "test WAV reload: " + ex.Message);
		}
		CancellationTokenSource cts = (run = new CancellationTokenSource());
		Thread thread = new Thread((ThreadStart)delegate
		{
			Pump(cts.Token);
		});
		thread.IsBackground = true;
		thread.Name = "WavFileMicrophone";
		thread.Start();
	}

	public void Stop()
	{
		run?.Cancel();
		run = null;
		Volatile.Write(ref level, 0f);
	}

	private void Pump(CancellationToken ct)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		byte[] array = new byte[960];
		int num = 0;
		int num2 = 0;
		bool flag = false;
		while (!ct.IsCancellationRequested)
		{
			double num3 = 0.0;
			for (int i = 0; i < 480; i++)
			{
				short num4 = (short)((num < samples.Length) ? samples[num++] : 0);
				array[i * 2] = (byte)num4;
				array[i * 2 + 1] = (byte)(num4 >> 8);
				num3 += (double)num4 * (double)num4;
			}
			Volatile.Write(ref level, (float)Math.Min(1.0, Math.Sqrt(num3 / 480.0) / 6000.0));
			this.Frame?.Invoke(array.ToArray());
			if (!flag && num >= samples.Length)
			{
				flag = true;
				this.Finished?.Invoke();
			}
			num2++;
			long num5 = num2 * 20 - stopwatch.ElapsedMilliseconds;
			if (num5 > 0)
			{
				Thread.Sleep((int)num5);
			}
		}
	}

	private static short[] Load(string path)
	{
		using BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
		if (new string(binaryReader.ReadChars(4)) != "RIFF")
		{
			throw new InvalidDataException("not a WAV file");
		}
		binaryReader.ReadInt32();
		if (new string(binaryReader.ReadChars(4)) != "WAVE")
		{
			throw new InvalidDataException("not a WAV file");
		}
		int num = 1;
		int num2 = 24000;
		int num3 = 16;
		byte[] array = null;
		while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
		{
			string text = new string(binaryReader.ReadChars(4));
			int num4 = binaryReader.ReadInt32();
			if (text == "fmt ")
			{
				binaryReader.ReadInt16();
				num = binaryReader.ReadInt16();
				num2 = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt16();
				num3 = binaryReader.ReadInt16();
				if (num4 > 16)
				{
					binaryReader.ReadBytes(num4 - 16);
				}
			}
			else
			{
				if (text == "data")
				{
					array = binaryReader.ReadBytes(num4);
					break;
				}
				binaryReader.ReadBytes(num4);
			}
		}
		if (array == null)
		{
			throw new InvalidDataException("WAV has no data chunk");
		}
		int num5 = num3 / 8;
		int num6 = array.Length / (num5 * num);
		double[] array2 = new double[num6];
		for (int i = 0; i < num6; i++)
		{
			double num7 = 0.0;
			for (int j = 0; j < num; j++)
			{
				int num8 = (i * num + j) * num5;
				num7 += (double)((num3 == 8) ? ((array[num8] - 128) * 256) : BitConverter.ToInt16(array, num8));
			}
			array2[i] = num7 / (double)num;
		}
		int num9 = (int)((long)num6 * 24000L / num2);
		short[] array3 = new short[num9];
		for (int k = 0; k < num9; k++)
		{
			double num10 = (double)k * (double)num2 / 24000.0;
			int num11 = (int)num10;
			double num12 = num10 - (double)num11;
			double value = ((num11 + 1 < num6) ? (array2[num11] * (1.0 - num12) + array2[num11 + 1] * num12) : array2[Math.Min(num11, num6 - 1)]);
			array3[k] = (short)Math.Clamp(value, -32768.0, 32767.0);
		}
		return array3;
	}

	public void Dispose()
	{
		Stop();
	}
}

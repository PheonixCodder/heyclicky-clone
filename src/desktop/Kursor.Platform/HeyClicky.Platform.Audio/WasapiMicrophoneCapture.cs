using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

public sealed class WasapiMicrophoneCapture : IMicrophoneCapture, IDisposable
{
	private enum SessionOutcome
	{
		Disposed,
		DeviceChanged,
		DeviceLost
	}

	public const int CaptureSampleRate = 24000;

	public const int FrameDurationMs = 20;

	public const int SamplesPerFrame = 480;

	public const int BytesPerFrame = 960;

	public const int FrameRingLength = 32;

	private const long BufferDurationHns = 1000000L;

	private const int LivenessCheckMilliseconds = 500;

	private const int StallThresholdMilliseconds = 1000;

	private const int RebuildCooldownMilliseconds = 1500;

	private const int MaximumStallRebuilds = 3;

	private const bool TestStall = false;

	private const bool TestTreatAsBluetooth = false;

	private readonly byte[][] frameRing = new byte[32][];

	private int frameRingIndex;

	private int frameFillBytes;

	private float smoothedLevel;

	private AudioEndpointInfo? currentDevice;

	private readonly object lifecycleGate = new object();

	private readonly object deliveryGate = new object();

	private readonly object policyGate = new object();

	private Thread? captureThread;

	private volatile bool wantRunning;

	private volatile bool holdOpen;

	private volatile bool disposed;

	private string? failoverDeviceId;

	private string? failoverReplacedSelection;

	private readonly AutoResetEvent controlChangedEvent = new AutoResetEvent(initialState: false);

	private readonly AutoResetEvent deviceChangedEvent = new AutoResetEvent(initialState: false);

	private readonly ManualResetEventSlim stoppedEvent = new ManualResetEventSlim(initialState: true);

	private long lastPacketTicks;

	private bool packetSinceStreamStart;

	private long lastRebuildTicks;

	private int stallRebuilds;

	private bool reportedDeadThisStart;

	private bool announcedStreamStarted;

	public int SampleRate => 24000;

	public float Level => Volatile.Read(in smoothedLevel);

	public bool IsRunning => wantRunning;

	public bool UseCommunicationsDevice { get; init; }

	private ERole DeviceRole
	{
		get
		{
			if (!UseCommunicationsDevice)
			{
				return ERole.eConsole;
			}
			return ERole.eCommunications;
		}
	}

	public string? CurrentDeviceId { get; private set; }

	public AudioEndpointInfo? CurrentDevice => Volatile.Read(in currentDevice);

	public string? DeviceName => CurrentDevice?.Name;

	public bool IsBluetoothDevice => CurrentDevice?.IsBluetooth ?? false;

	public string? FailoverDeviceId
	{
		get
		{
			lock (policyGate)
			{
				return failoverDeviceId;
			}
		}
	}

	public bool IsHoldingDeviceOpen => holdOpen;

	public event Action<ReadOnlyMemory<byte>>? Frame;

	public event Action<string?>? Trouble;

	public event Action<string>? DeviceProvedDead;

	public event Action? StreamStarted;

	public WasapiMicrophoneCapture()
	{
		for (int i = 0; i < 32; i++)
		{
			frameRing[i] = new byte[960];
		}
		AudioDevices.PreferenceChanged += OnDevicePreferenceChanged;
	}

	private void OnDevicePreferenceChanged(bool capture)
	{
		if (!capture || disposed)
		{
			return;
		}
		lock (policyGate)
		{
			if (failoverDeviceId != null)
			{
				Log.Info("Mic", "microphone pick changed in Settings: dropping the built-in failover pin");
			}
			failoverDeviceId = null;
			failoverReplacedSelection = null;
		}
		try
		{
			deviceChangedEvent.Set();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	public void Prewarm()
	{
		EnsureThread();
	}

	public void Start()
	{
		if (!disposed)
		{
			EnsureThread();
			stoppedEvent.Reset();
			lock (deliveryGate)
			{
				reportedDeadThisStart = false;
				Volatile.Write(ref stallRebuilds, 0);
				wantRunning = true;
			}
			controlChangedEvent.Set();
		}
	}

	public void Stop()
	{
		bool flag = holdOpen;
		holdOpen = false;
		if (wantRunning || flag)
		{
			lock (deliveryGate)
			{
				wantRunning = false;
			}
			controlChangedEvent.Set();
			stoppedEvent.Wait(1000);
			Volatile.Write(ref smoothedLevel, 0f);
		}
	}

	public void StopKeepingDeviceOpen()
	{
		if (wantRunning)
		{
			holdOpen = true;
			lock (deliveryGate)
			{
				wantRunning = false;
			}
			controlChangedEvent.Set();
			Volatile.Write(ref smoothedLevel, 0f);
			Log.Info("Mic", "deliveries muted: holding '" + DeviceName + "' open so the headset stays in its current profile");
		}
	}

	public void ReleaseHeldDevice()
	{
		if (holdOpen)
		{
			holdOpen = false;
			controlChangedEvent.Set();
			Log.Info("Mic", "the held microphone is closed");
		}
	}

	public string? FailOverToBuiltInMicrophone(string deadDeviceName)
	{
		AudioEndpointInfo audioEndpointInfo = AudioDevices.BuiltInMicrophone();
		lock (policyGate)
		{
			bool flag = string.Equals(audioEndpointInfo?.Id, CurrentDeviceId, StringComparison.OrdinalIgnoreCase) || string.Equals(audioEndpointInfo?.Name, deadDeviceName, StringComparison.Ordinal);
			if (failoverDeviceId != null || (object)audioEndpointInfo == null || flag)
			{
				return null;
			}
			failoverDeviceId = audioEndpointInfo.Id;
			failoverReplacedSelection = AudioDevices.PreferredCaptureId;
		}
		Log.Warn("Mic", $"mic failover: '{deadDeviceName}' proved dead, using '{audioEndpointInfo.Name}'");
		try
		{
			deviceChangedEvent.Set();
		}
		catch (ObjectDisposedException)
		{
		}
		return audioEndpointInfo.Name;
	}

	private string? CurrentFailoverDeviceId()
	{
		lock (policyGate)
		{
			if (failoverDeviceId == null)
			{
				return null;
			}
			if (!string.Equals(failoverReplacedSelection, AudioDevices.PreferredCaptureId, StringComparison.OrdinalIgnoreCase))
			{
				failoverDeviceId = null;
				failoverReplacedSelection = null;
				return null;
			}
			return failoverDeviceId;
		}
	}

	public void Dispose()
	{
		if (!disposed)
		{
			wantRunning = false;
			holdOpen = false;
			disposed = true;
			AudioDevices.PreferenceChanged -= OnDevicePreferenceChanged;
			controlChangedEvent.Set();
			captureThread?.Join(2000);
			controlChangedEvent.Dispose();
			deviceChangedEvent.Dispose();
		}
	}

	private void EnsureThread()
	{
		lock (lifecycleGate)
		{
			if (captureThread == null && !disposed)
			{
				captureThread = new Thread(CaptureThreadMain)
				{
					IsBackground = true,
					Name = "HeyClicky mic capture",
					Priority = ThreadPriority.Highest
				};
				captureThread.SetApartmentState(ApartmentState.MTA);
				captureThread.Start();
			}
		}
	}

	private void CaptureThreadMain()
	{
		uint TaskIndex = 0u;
		using (PInvoke.AvSetMmThreadCharacteristics("Audio", ref TaskIndex))
		{
			IMMDeviceEnumerator iMMDeviceEnumerator = null;
			DefaultDeviceChangeListener defaultDeviceChangeListener = null;
			try
			{
				iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
				defaultDeviceChangeListener = new DefaultDeviceChangeListener(EDataFlow.eCapture, DeviceRole, deviceChangedEvent);
				try
				{
					iMMDeviceEnumerator.RegisterEndpointNotificationCallback(defaultDeviceChangeListener);
				}
				catch (Exception ex)
				{
					defaultDeviceChangeListener = null;
					Log.Warn("Mic", "device-change notifications unavailable: " + ex.Message);
				}
				bool reportedTrouble = false;
				while (!disposed)
				{
					SessionOutcome sessionOutcome = RunOneDeviceSession(iMMDeviceEnumerator, ref reportedTrouble);
					stoppedEvent.Set();
					if (sessionOutcome == SessionOutcome.Disposed)
					{
						break;
					}
					Volatile.Write(ref smoothedLevel, 0f);
					if (sessionOutcome != SessionOutcome.DeviceChanged)
					{
						while (!disposed && WaitHandle.WaitAny(new WaitHandle[2] { controlChangedEvent, deviceChangedEvent }, wantRunning ? 1000 : (-1)) == 0 && !wantRunning)
						{
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Log.Error("Mic", "capture thread failed", ex2);
				this.Trouble?.Invoke("Microphone capture failed");
			}
			finally
			{
				stoppedEvent.Set();
				if (defaultDeviceChangeListener != null && iMMDeviceEnumerator != null)
				{
					try
					{
						iMMDeviceEnumerator.UnregisterEndpointNotificationCallback(defaultDeviceChangeListener);
					}
					catch
					{
					}
				}
				CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
			}
		}
	}

	private unsafe SessionOutcome RunOneDeviceSession(IMMDeviceEnumerator deviceEnumerator, ref bool reportedTrouble)
	{
		bool usedPreferred;
		IMMDevice iMMDevice = AudioDevices.Resolve(deviceEnumerator, EDataFlow.eCapture, DeviceRole, out usedPreferred, CurrentFailoverDeviceId());
		if (iMMDevice == null)
		{
			if (!reportedTrouble)
			{
				reportedTrouble = true;
				Log.Warn("Mic", "no capture device");
				this.Trouble?.Invoke("No microphone found");
			}
			return SessionOutcome.DeviceLost;
		}
		IAudioClient audioClient = null;
		IAudioCaptureClient audioCaptureClient = null;
		using AutoResetEvent autoResetEvent = new AutoResetEvent(initialState: false);
		bool flag = false;
		try
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			PWSTRHolder.GetId(iMMDevice, out string id);
			CurrentDeviceId = id;
			Volatile.Write(ref currentDevice, AudioEndpoints.Describe(iMMDevice));
			audioClient = CoreAudio.ActivateAudioClient(iMMDevice);
			WAVEFORMATEX wAVEFORMATEX = CoreAudio.Pcm16(24000, 1);
			audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 2281963520u, 1000000L, 0L, &wAVEFORMATEX, null);
			audioClient.SetEventHandle(new HANDLE(autoResetEvent.SafeWaitHandle.DangerousGetHandle()));
			audioCaptureClient = CoreAudio.GetService<IAudioCaptureClient>(audioClient, CoreAudio.IID_IAudioCaptureClient);
			if (reportedTrouble)
			{
				reportedTrouble = false;
				this.Trouble?.Invoke(null);
			}
			AudioEndpointInfo audioEndpointInfo = CurrentDevice;
			Log.Info("Mic", $"opened {(usedPreferred ? "picked" : "default")} '{audioEndpointInfo?.Name ?? id}' ({audioEndpointInfo?.TransportCategory ?? "unknown"}) at {24000} Hz mono in {stopwatch.ElapsedMilliseconds} ms");
			WaitHandle[] array = new WaitHandle[2] { controlChangedEvent, deviceChangedEvent };
			WaitHandle[] array2 = new WaitHandle[3] { controlChangedEvent, deviceChangedEvent, autoResetEvent };
			while (true)
			{
				if (disposed)
				{
					if (flag)
					{
						audioClient.Stop();
					}
					return SessionOutcome.Disposed;
				}
				bool flag2 = wantRunning || holdOpen;
				if (flag2 && !flag)
				{
					frameFillBytes = 0;
					audioClient.Start();
					flag = true;
					lastPacketTicks = Environment.TickCount64;
					packetSinceStreamStart = false;
				}
				else if (!flag2 && flag)
				{
					audioClient.Stop();
					audioClient.Reset();
					flag = false;
					Volatile.Write(ref smoothedLevel, 0f);
					stoppedEvent.Set();
				}
				if (flag && !wantRunning)
				{
					stoppedEvent.Set();
				}
				else if (flag)
				{
					stoppedEvent.Reset();
				}
				if (wantRunning && flag && !announcedStreamStarted)
				{
					announcedStreamStarted = true;
					try
					{
						this.StreamStarted?.Invoke();
					}
					catch (Exception ex)
					{
						Log.Warn("Mic", "stream-started handler: " + ex.Message);
					}
				}
				else if (!wantRunning)
				{
					announcedStreamStarted = false;
				}
				switch (WaitHandle.WaitAny(flag ? array2 : array, flag ? 500 : (-1)))
				{
				case 0:
					continue;
				case 1:
					if (flag)
					{
						audioClient.Stop();
					}
					Log.Info("Mic", "capture device changed; reopening");
					return SessionOutcome.DeviceChanged;
				default:
					DrainCapturePackets(audioCaptureClient);
					break;
				case 258:
					break;
				}
				if (wantRunning && flag && Environment.TickCount64 - lastPacketTicks > 1000)
				{
					if (TryRebuildAfterStall())
					{
						break;
					}
				}
				else if (wantRunning && packetSinceStreamStart && Environment.TickCount64 - lastPacketTicks <= 1000 && stallRebuilds > 0)
				{
					stallRebuilds = 0;
				}
			}
			audioClient.Stop();
			return SessionOutcome.DeviceChanged;
		}
		catch (Exception ex2) when (CoreAudio.IsDeviceGone(ex2))
		{
			Log.Warn("Mic", $"capture device lost (0x{ex2.HResult:X8}); waiting for a device");
			if (!reportedTrouble)
			{
				reportedTrouble = true;
				this.Trouble?.Invoke("Microphone disconnected");
			}
			return SessionOutcome.DeviceLost;
		}
		catch (UnauthorizedAccessException)
		{
			if (!reportedTrouble)
			{
				reportedTrouble = true;
				Log.Warn("Mic", "microphone access is off in Windows; retrying until it's on");
				this.Trouble?.Invoke("Microphone access is off");
			}
			return SessionOutcome.DeviceLost;
		}
		catch (COMException ex4)
		{
			Log.Error("Mic", $"could not open capture device (0x{ex4.HResult:X8})", ex4);
			if (!reportedTrouble)
			{
				reportedTrouble = true;
				this.Trouble?.Invoke("Couldn't open the microphone");
			}
			return SessionOutcome.DeviceLost;
		}
		finally
		{
			CoreAudio.ReleaseComObject(audioCaptureClient);
			CoreAudio.ReleaseComObject(audioClient);
			CoreAudio.ReleaseComObject(iMMDevice);
		}
	}

	private bool TryRebuildAfterStall()
	{
		long tickCount = Environment.TickCount64;
		if (tickCount - lastRebuildTicks < 1500)
		{
			return false;
		}
		double value = (double)(tickCount - lastPacketTicks) / 1000.0;
		if (stallRebuilds >= 3)
		{
			bool flag;
			lock (deliveryGate)
			{
				flag = !reportedDeadThisStart;
				reportedDeadThisStart = true;
			}
			if (flag)
			{
				string text = DeviceName ?? "Your microphone";
				Log.Warn("Mic", $"capture gave up after {stallRebuilds} rebuilds: '{text}' delivers no audio");
				try
				{
					this.DeviceProvedDead?.Invoke(text);
				}
				catch (Exception ex)
				{
					Log.Error("Mic", "DeviceProvedDead handler threw", ex);
				}
			}
			return false;
		}
		stallRebuilds++;
		lastRebuildTicks = tickCount;
		Log.Warn("Mic", $"mic stalled: rebuilding capture idle={value:0.00}s attempt={stallRebuilds}/{3}");
		return true;
	}

	private unsafe void DrainCapturePackets(IAudioCaptureClient captureClient)
	{
		byte* source = default(byte*);
		while (true)
		{
			captureClient.GetNextPacketSize(out var pNumFramesInNextPacket);
			if (pNumFramesInNextPacket == 0)
			{
				break;
			}
			captureClient.GetBuffer(&source, out var pNumFramesToRead, out var pdwFlags, null, null);
			lastPacketTicks = Environment.TickCount64;
			packetSinceStreamStart = true;
			int byteCount = (int)(pNumFramesToRead * 2);
			bool isSilent = (pdwFlags & 2) != 0;
			if (wantRunning)
			{
				AppendToFrames(source, byteCount, isSilent);
			}
			captureClient.ReleaseBuffer(pNumFramesToRead);
		}
	}

	private unsafe void AppendToFrames(byte* source, int byteCount, bool isSilent)
	{
		int num = 0;
		while (num < byteCount)
		{
			byte[] array = frameRing[frameRingIndex];
			int num2 = Math.Min(960 - frameFillBytes, byteCount - num);
			Span<byte> destination = array.AsSpan(frameFillBytes, num2);
			if (isSilent)
			{
				destination.Clear();
			}
			else
			{
				new ReadOnlySpan<byte>(source + num, num2).CopyTo(destination);
			}
			frameFillBytes += num2;
			num += num2;
			if (frameFillBytes != 960)
			{
				continue;
			}
			UpdateLevel(array);
			frameFillBytes = 0;
			frameRingIndex = (frameRingIndex + 1) % 32;
			lock (deliveryGate)
			{
				if (wantRunning)
				{
					try
					{
						this.Frame?.Invoke(array);
					}
					catch (Exception ex)
					{
						Log.Error("Mic", "Frame handler threw", ex);
					}
				}
			}
		}
	}

	private void UpdateLevel(byte[] frame)
	{
		Span<short> span = MemoryMarshal.Cast<byte, short>(frame.AsSpan());
		double num = 0.0;
		Span<short> span2 = span;
		for (int i = 0; i < span2.Length; i++)
		{
			double num2 = (double)span2[i] / 32768.0;
			num += num2 * num2;
		}
		float val = Math.Min((float)Math.Sqrt(num / (double)span.Length) * 10.2f, 1f);
		float num3 = Volatile.Read(in smoothedLevel);
		Volatile.Write(ref smoothedLevel, Math.Max(val, num3 * 0.72f));
	}
}
